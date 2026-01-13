using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

class BoosteroidDebug
{
    static string LOG_FILE = "";
    
    static void Main()
    {
        try
        {
            // Log no Desktop
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            LOG_FILE = Path.Combine(desktop, "boosteroid_debug.txt");
            
            Log("=== BOOSTEROID DEBUG LOG ===");
            Log("Started at: " + DateTime.Now.ToString());
            Log("");
            
            Console.Title = "Boosteroid Debug - Press key to exit";
            
            Console.WriteLine("[*] Boosteroid Debug Mode");
            Console.WriteLine("[*] Log: " + LOG_FILE);
            Console.WriteLine("");
            
            // Testes
            TestBasicFunctions();
            TestStorage();
            TestServices();
            TestCommands();
            TestFilebeat();
            
            Log("");
            Log("=== COMPLETED ===");
            Console.WriteLine("");
            Console.WriteLine("[+] Done! Check: " + LOG_FILE);
            Console.WriteLine("");
            Console.WriteLine("Press ANY KEY to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            try
            {
                Log("FATAL ERROR: " + ex.ToString());
                Console.WriteLine("ERROR: " + ex.Message);
                Console.WriteLine("Log: " + LOG_FILE);
                Thread.Sleep(5000);
            }
            catch
            {
                try
                {
                    File.WriteAllText("C:\\CRASH.txt", ex.ToString());
                }
                catch { }
            }
        }
    }
    
    static void TestBasicFunctions()
    {
        try
        {
            Log("[TEST 1] Basic Functions");
            Log("  User: " + Environment.UserName);
            Log("  Machine: " + Environment.MachineName);
            Log("  OS: " + Environment.OSVersion.ToString());
            
            Console.WriteLine("  [1/5] Basic functions: OK");
        }
        catch (Exception ex)
        {
            Log("[ERROR] Basic: " + ex.Message);
            Console.WriteLine("  [1/5] Basic functions: FAILED");
        }
    }
    
    static void TestStorage()
    {
        try
        {
            Log("[TEST 2] Storage");
            
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                try
                {
                    if (drive.IsReady)
                    {
                        long gb = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                        Log("  " + drive.Name + " - " + gb.ToString() + "GB free");
                    }
                }
                catch { }
            }
            
            Console.WriteLine("  [2/5] Storage: OK");
        }
        catch (Exception ex)
        {
            Log("[ERROR] Storage: " + ex.Message);
            Console.WriteLine("  [2/5] Storage: FAILED");
        }
    }
    
    static void TestServices()
    {
        try
        {
            Log("[TEST 3] Services");
            
            string[] services = new string[] { "filebeat", "Schedule", "Winmgmt" };
            
            foreach (string svc in services)
            {
                try
                {
                    ServiceController sc = new ServiceController(svc);
                    Log("  " + svc + ": " + sc.Status.ToString());
                }
                catch
                {
                    Log("  " + svc + ": NOT FOUND");
                }
            }
            
            Console.WriteLine("  [3/5] Services: OK");
        }
        catch (Exception ex)
        {
            Log("[ERROR] Services: " + ex.Message);
            Console.WriteLine("  [3/5] Services: FAILED");
        }
    }
    
    static void TestCommands()
    {
        try
        {
            Log("[TEST 4] Commands");
            
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = "/c echo test";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;
            
            Process proc = Process.Start(psi);
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            
            Log("  CMD output: " + output.Trim());
            
            Console.WriteLine("  [4/5] Commands: OK");
        }
        catch (Exception ex)
        {
            Log("[ERROR] Commands: " + ex.Message);
            Console.WriteLine("  [4/5] Commands: FAILED");
        }
    }
    
    static void TestFilebeat()
    {
        try
        {
            Log("[TEST 5] Filebeat Detection");
            
            // Procurar processo
            Process[] procs = Process.GetProcessesByName("filebeat");
            if (procs.Length > 0)
            {
                Log("  Process FOUND: PID " + procs[0].Id.ToString());
                try
                {
                    Log("  Path: " + procs[0].MainModule.FileName);
                }
                catch { }
            }
            else
            {
                Log("  Process: NOT RUNNING");
            }
            
            // Procurar config
            string[] configPaths = new string[] {
                "C:\\ProgramData\\filebeat\\filebeat.yml",
                "C:\\Program Files\\filebeat\\filebeat.yml"
            };
            
            foreach (string path in configPaths)
            {
                if (File.Exists(path))
                {
                    Log("  Config FOUND: " + path);
                    
                    // Ler primeiras linhas
                    try
                    {
                        string[] lines = File.ReadAllLines(path);
                        int max = Math.Min(10, lines.Length);
                        for (int i = 0; i < max; i++)
                        {
                            Log("    " + lines[i]);
                        }
                    }
                    catch { }
                }
            }
            
            Console.WriteLine("  [5/5] Filebeat: OK");
        }
        catch (Exception ex)
        {
            Log("[ERROR] Filebeat: " + ex.Message);
            Console.WriteLine("  [5/5] Filebeat: FAILED");
        }
    }
    
    static void Log(string msg)
    {
        try
        {
            if (!string.IsNullOrEmpty(LOG_FILE))
            {
                File.AppendAllText(LOG_FILE, msg + "\r\n");
            }
        }
        catch { }
    }
}
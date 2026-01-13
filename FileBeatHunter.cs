using System;
using System.Diagnostics;
using System.IO;
using System.Text;

class FilebeatHunter
{
    static string LOG = "";
    
    static void Main()
    {
        try
        {
            LOG = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "filebeat_hunt.txt");
            
            File.WriteAllText(LOG, "=== FILEBEAT HUNTER ===\r\n");
            File.AppendAllText(LOG, "Time: " + DateTime.Now.ToString() + "\r\n\r\n");
            
            Console.Title = "Filebeat Hunter";
            Console.WriteLine("[*] Hunting for filebeat config...\n");
            
            // 1. Procurar processo e argumentos
            FindProcessArgs();
            
            // 2. Procurar arquivos .yml
            SearchYmlFiles();
            
            // 3. Procurar na pasta do executável
            SearchExeFolder();
            
            // 4. Procurar registros comuns
            SearchCommonPaths();
            
            Console.WriteLine("\n[+] Hunt complete! Check: " + LOG);
            Console.WriteLine("\nPress ANY KEY...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
            File.AppendAllText(LOG, "ERROR: " + ex.ToString());
            Console.ReadKey();
        }
    }
    
    static void FindProcessArgs()
    {
        Console.WriteLine("[1/4] Checking process arguments...");
        Log("[PROCESS ARGUMENTS]");
        
        try
        {
            Process[] procs = Process.GetProcessesByName("filebeat");
            
            if (procs.Length > 0)
            {
                Process fb = procs[0];
                
                try
                {
                    // Path do executável
                    string exePath = fb.MainModule.FileName;
                    Console.WriteLine("  [+] EXE: " + exePath);
                    Log("EXE: " + exePath);
                    
                    // Working directory
                    try
                    {
                        string workDir = Path.GetDirectoryName(exePath);
                        Console.WriteLine("  [+] Working Dir: " + workDir);
                        Log("Working Dir: " + workDir);
                    }
                    catch { }
                    
                    // Tentar pegar command line via WMI
                    try
                    {
                        string query = "SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + fb.Id.ToString();
                        System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher(query);
                        
                        foreach (System.Management.ManagementObject obj in searcher.Get())
                        {
                            string cmdLine = obj["CommandLine"].ToString();
                            Console.WriteLine("  [+] Command Line: " + cmdLine);
                            Log("Command Line: " + cmdLine);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("  [!] Cannot get command line: " + ex.Message);
                        Log("Command line error: " + ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  [!] Error: " + ex.Message);
                    Log("Error: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("  [!] Filebeat not running");
                Log("Process not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [!] Failed: " + ex.Message);
            Log("Failed: " + ex.Message);
        }
    }
    
    static void SearchYmlFiles()
    {
        Console.WriteLine("\n[2/4] Searching for .yml files...");
        Log("\n[YML FILES SEARCH]");
        
        string[] searchPaths = new string[] {
            "C:\\Users\\user\\boosteroid-experience",
            "C:\\ProgramData",
            "C:\\Program Files",
            "S:\\"
        };
        
        foreach (string basePath in searchPaths)
        {
            try
            {
                if (!Directory.Exists(basePath))
                {
                    continue;
                }
                
                Console.WriteLine("  [*] Searching: " + basePath);
                
                string[] ymlFiles = Directory.GetFiles(basePath, "*.yml", SearchOption.AllDirectories);
                
                foreach (string file in ymlFiles)
                {
                    if (file.ToLower().Contains("filebeat"))
                    {
                        Console.WriteLine("  [+] FOUND: " + file);
                        Log("FOUND: " + file);
                        
                        // Tentar ler
                        try
                        {
                            string[] lines = File.ReadAllLines(file);
                            Log("\n--- Content of " + file + " ---");
                            
                            int max = Math.Min(50, lines.Length);
                            for (int i = 0; i < max; i++)
                            {
                                Log(lines[i]);
                            }
                            Log("--- End (" + lines.Length.ToString() + " lines total) ---\n");
                        }
                        catch (Exception ex)
                        {
                            Log("Cannot read: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [!] Error in " + basePath + ": " + ex.Message);
            }
        }
    }
    
    static void SearchExeFolder()
    {
        Console.WriteLine("\n[3/4] Searching filebeat folder...");
        Log("\n[FILEBEAT FOLDER]");
        
        try
        {
            Process[] procs = Process.GetProcessesByName("filebeat");
            if (procs.Length == 0)
            {
                Console.WriteLine("  [!] Process not found");
                return;
            }
            
            string exePath = procs[0].MainModule.FileName;
            string folder = Path.GetDirectoryName(exePath);
            
            Console.WriteLine("  [+] Folder: " + folder);
            Log("Folder: " + folder);
            
            // Listar TODOS os arquivos
            string[] allFiles = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            
            Console.WriteLine("  [+] Files in folder:");
            Log("\n--- All files ---");
            
            foreach (string file in allFiles)
            {
                string relativePath = file.Replace(folder, "");
                Console.WriteLine("    " + relativePath);
                Log(relativePath);
            }
            Log("--- End ---\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [!] Failed: " + ex.Message);
            Log("Failed: " + ex.Message);
        }
    }
    
    static void SearchCommonPaths()
    {
        Console.WriteLine("\n[4/4] Checking common config locations...");
        Log("\n[COMMON PATHS]");
        
        string[] paths = new string[] {
            "C:\\Users\\user\\boosteroid-experience\\misc\\fb\\filebeat.yml",
            "C:\\Users\\user\\boosteroid-experience\\misc\\filebeat.yml",
            "C:\\Users\\user\\boosteroid-experience\\filebeat.yml",
            "C:\\ProgramData\\filebeat\\filebeat.yml",
            "C:\\Program Files\\filebeat\\filebeat.yml",
            "C:\\filebeat.yml",
            "S:\\filebeat.yml"
        };
        
        foreach (string path in paths)
        {
            if (File.Exists(path))
            {
                Console.WriteLine("  [+] FOUND: " + path);
                Log("FOUND: " + path);
            }
            else
            {
                Console.WriteLine("  [ ] Not found: " + path);
            }
        }
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
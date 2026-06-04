using System;
using System.IO;
using GerberLibrary;
using ICSharpCode.SharpZipLib.Zip;

namespace MigrationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing GerberLibrary Migration...");

            string testDir = Path.GetFullPath("TestGerbers");
            string outDir = Path.GetFullPath("Output");
            
            if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            
            Directory.CreateDirectory(testDir);
            Directory.CreateDirectory(outDir);
            
            // Create dummy files
            File.WriteAllText(Path.Combine(testDir, "board.gtl"), "G04 This is a test gerber file*");
            File.WriteAllText(Path.Combine(testDir, "board.drl"), "M48*"); // Drill file
            
            Console.WriteLine($"Created test files in {testDir}");

            try 
            {
                // Test Zipping
                Console.WriteLine("Calling ZipGerberFolderToFactoryFolder...");
                Gerber.ZipGerberFolderToFactoryFolder("TestBoard", testDir, outDir);
                
                string zipPath = Path.Combine(outDir, "TestBoard_gerbers.zip");
                if (File.Exists(zipPath))
                {
                    Console.WriteLine($"SUCCESS: Zip file created at {zipPath}");
                    
                    // Verify contents
                    using (ZipFile zf = new ZipFile(zipPath))
                    {
                        Console.WriteLine($"Zip contains {zf.Count} entries:");
                        foreach (ZipEntry e in zf)
                        {
                            Console.WriteLine($" - {e.Name}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("FAILURE: Zip file not found.");
                    Environment.Exit(1);
                }

                // Test Reading (simulating reading logic)
                Console.WriteLine("\nTesting Reading back...");
                using (ZipFile zipRead = new ZipFile(zipPath))
                {
                    foreach (ZipEntry e in zipRead)
                    {
                        if (!e.IsDirectory)
                        {
                             using (var s = zipRead.GetInputStream(e))
                             {
                                 Console.WriteLine($" + Successfully opened stream for {e.Name}");
                             }
                        }
                    }
                }
                Console.WriteLine("SUCCESS: Reading Zip works.");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }
    }
}

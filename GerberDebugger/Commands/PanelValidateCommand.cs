using GerberLibrary;
using GerberLibrary.Core;
using GerberLibrary.Core.Primitives;

namespace GerberDebugger.Commands;

public class PanelValidateCommand
{
    public int Run(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: GerberDebugger panel-validate <folder>");
            return 1;
        }

        string folder = args[0];
        if (!Directory.Exists(folder))
        {
            Console.Error.WriteLine($"Folder not found: {folder}");
            return 1;
        }

        Console.WriteLine($"=== Panel Validation: {folder} ===");

        var log = new StandardConsoleLog();
        var panel = new GerberPanel();
        int errors = 0;

        // Try to load as a board folder
            try
            {
                var result = panel.AddGerberFolder(log, folder, true, false);
                if (result == null || result.Count == 0)
                {
                    Console.WriteLine("WARNING: No boards loaded from this folder.");
                    errors++;
                }
                else
                {
                    Console.WriteLine($"Loaded: {string.Join(", ", result)}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FAIL: Could not load folder as board: {ex.Message}");
                errors++;
            }

        // Check outline files
        Console.WriteLine();
        Console.WriteLine("--- Files ---");
        var files = Directory.GetFiles(folder);
        foreach (var f in files)
        {
            var ft = Gerber.FindFileType(f);
            BoardSide side = BoardSide.Unknown;
            BoardLayer layer = BoardLayer.Unknown;
            Gerber.DetermineBoardSideAndLayer(f, out side, out layer);
            Console.WriteLine($"  {Path.GetFileName(f),-20} type={ft,-10} side={side,-8} layer={layer}");
        }

        // Check for outline
        bool hasOutline = files.Any(f =>
        {
            BoardSide side = BoardSide.Unknown;
            BoardLayer layer = BoardLayer.Unknown;
            Gerber.DetermineBoardSideAndLayer(f, out side, out layer);
            return layer == BoardLayer.Outline || layer == BoardLayer.Mill;
        });

        Console.WriteLine();
        if (hasOutline)
        {
            Console.WriteLine("Outline: PRESENT");
        }
        else
        {
            Console.WriteLine("WARNING: No outline/mill file detected.");
            errors++;
        }

        // Try to parse each file
        Console.WriteLine();
        Console.WriteLine("--- Parse Check ---");
        foreach (var f in files)
        {
            var ft = Gerber.FindFileType(f);
            if (ft == BoardFileType.Gerber || ft == BoardFileType.Drill)
            {
                try
                {
                    var parsed = ft == BoardFileType.Drill
                        ? PolyLineSet.LoadExcellonDrillFile(log, f)
                        : PolyLineSet.LoadGerberFile(log, f);

                    bool nonEmpty = parsed.Shapes.Count + parsed.DisplayShapes.Count +
                                    parsed.OutlineShapes.Count > 0;
                    string status = nonEmpty ? "OK" : "EMPTY";
                    Console.WriteLine($"  {Path.GetFileName(f),-20} -> {ft,-10} -> PASS ({status})");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"  {Path.GetFileName(f),-20} -> FAIL: {ex.Message}");
                    errors++;
                }
            }
        }

        Console.WriteLine();
        if (errors == 0)
        {
            Console.WriteLine("Result: PASS - Board folder looks valid");
            return 0;
        }
        else
        {
            Console.WriteLine($"Result: FAIL - {errors} issue(s) found");
            return 1;
        }
    }
}

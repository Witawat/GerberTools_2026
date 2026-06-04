using GerberLibrary;
using GerberLibrary.Core;
using GerberLibrary.Core.Primitives;

namespace GerberDebugger.Commands;

public class ValidateCommand
{
    public int Run(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: GerberDebugger validate <file>");
            return 1;
        }

        string path = args[0];
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"File not found: {path}");
            return 1;
        }

        var fileType = Gerber.FindFileType(path);
        Console.WriteLine($"File: {Path.GetFileName(path)}");
        Console.WriteLine($"Type: {fileType}");
        Console.WriteLine($"Size: {new FileInfo(path).Length} bytes");

        var log = new StandardConsoleLog();
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            if (fileType == BoardFileType.Drill)
            {
                var excellon = new ExcellonFile();
                excellon.Load(log, path);
                Console.WriteLine("Status: PASS (Excellon parsed successfully)");
                Console.WriteLine($"Tools: {excellon.Tools.Count}");
                Console.WriteLine($"Drills: {excellon.Tools.Values.Sum(t => t.Drills.Count)}");
            }
            else if (fileType == BoardFileType.Gerber)
            {
                var parsed = PolyLineSet.LoadGerberFile(log, path);
                Console.WriteLine("Status: PASS (Gerber parsed successfully)");
                Console.WriteLine($"Shapes: {parsed.Shapes.Count} (copper)");
                Console.WriteLine($"DisplayShapes: {parsed.DisplayShapes.Count} (silk)");
                Console.WriteLine($"OutlineShapes: {parsed.OutlineShapes.Count} (outline)");
                Console.WriteLine($"ZerosizePoints: {parsed.ZerosizePoints.Count}");
                Console.WriteLine($"Apertures defined: {parsed.State?.Apertures?.Count ?? 0}");
                Console.WriteLine($"BoundingBox: {parsed.BoundingBox}");
            }
            else
            {
                Console.Error.WriteLine("Status: FAIL - Unrecognized file type");
                return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Status: FAIL - {ex.Message}");
            return 1;
        }
    }
}

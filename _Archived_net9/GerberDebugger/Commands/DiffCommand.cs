using GerberLibrary;
using GerberLibrary.Core;
using GerberLibrary.Core.Primitives;

namespace GerberDebugger.Commands;

public class DiffCommand
{
    public int Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: GerberDebugger diff <file1> <file2>");
            return 1;
        }

        string file1 = args[0];
        string file2 = args[1];

        if (!File.Exists(file1)) { Console.Error.WriteLine($"File not found: {file1}"); return 1; }
        if (!File.Exists(file2)) { Console.Error.WriteLine($"File not found: {file2}"); return 1; }

        Console.WriteLine($"=== Diff: {Path.GetFileName(file1)} vs {Path.GetFileName(file2)} ===");

        var log = new StandardConsoleLog();
        int differences = 0;

        var p1 = LoadParsed(log, file1);
        var p2 = LoadParsed(log, file2);

        if (p1 == null || p2 == null) return 1;

        // Compare shape counts
        differences += CompareField("Copper shapes", p1.Shapes.Count, p2.Shapes.Count);
        differences += CompareField("Display shapes", p1.DisplayShapes.Count, p2.DisplayShapes.Count);
        differences += CompareField("Outline shapes", p1.OutlineShapes.Count, p2.OutlineShapes.Count);
        differences += CompareField("Zerosize points", p1.ZerosizePoints.Count, p2.ZerosizePoints.Count);

        // Compare bounding box
        differences += CompareBounds(p1.BoundingBox, p2.BoundingBox);

        // Compare aperture counts
        int ap1 = p1.State?.Apertures?.Count ?? 0;
        int ap2 = p2.State?.Apertures?.Count ?? 0;
        differences += CompareField("Apertures defined", ap1, ap2);

        Console.WriteLine();
        if (differences == 0)
        {
            Console.WriteLine("Result: FILES MATCH (no differences found)");
            return 0;
        }
        else
        {
            Console.WriteLine($"Result: DIFFERENT ({differences} difference(s) found)");
            return 1;
        }
    }

    ParsedGerber LoadParsed(ProgressLog log, string path)
    {
        try
        {
            var parsed = PolyLineSet.LoadGerberFile(log, path);
            return parsed;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse {path}: {ex.Message}");
            return null;
        }
    }

    int CompareField(string label, int v1, int v2)
    {
        if (v1 == v2)
        {
            Console.WriteLine($"  {label,-20}: {v1,6}  (match)");
            return 0;
        }
        else
        {
            Console.WriteLine($"  {label,-20}: {v1,6} vs {v2,6}  <<< DIFFERENT >>>");
            return 1;
        }
    }

    int CompareBounds(Bounds b1, Bounds b2)
    {
        int diffs = 0;
        if (Math.Abs(b1.TopLeft.X - b2.TopLeft.X) > 0.001 || Math.Abs(b1.BottomRight.X - b2.BottomRight.X) > 0.001 ||
            Math.Abs(b1.TopLeft.Y - b2.TopLeft.Y) > 0.001 || Math.Abs(b1.BottomRight.Y - b2.BottomRight.Y) > 0.001)
        {
            Console.WriteLine($"  BoundingBox:");
            Console.WriteLine($"    File1: ({b1.TopLeft.X:F4},{b1.TopLeft.Y:F4})-({b1.BottomRight.X:F4},{b1.BottomRight.Y:F4})");
            Console.WriteLine($"    File2: ({b2.TopLeft.X:F4},{b2.TopLeft.Y:F4})-({b2.BottomRight.X:F4},{b2.BottomRight.Y:F4})");
            Console.WriteLine($"    <<< DIFFERENT >>>");
            diffs = 1;
        }
        else
        {
            Console.WriteLine($"  BoundingBox: ({b1.TopLeft.X:F4},{b1.TopLeft.Y:F4})-({b1.BottomRight.X:F4},{b1.BottomRight.Y:F4})  (match)");
        }
        return diffs;
    }
}

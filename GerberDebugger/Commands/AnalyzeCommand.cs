using GerberLibrary;
using GerberLibrary.Core;
using GerberLibrary.Core.Primitives;

namespace GerberDebugger.Commands;

public class AnalyzeCommand
{
    public int Run(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: GerberDebugger analyze <file>");
            return 1;
        }

        string path = args[0];
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"File not found: {path}");
            return 1;
        }

        var fileType = Gerber.FindFileType(path);
        Console.WriteLine($"=== Analysis Report ===");
        Console.WriteLine($"File: {Path.GetFileName(path)}");
        Console.WriteLine($"Type: {fileType}");
        Console.WriteLine($"Full Path: {Path.GetFullPath(path)}");
        Console.WriteLine($"Size: {new FileInfo(path).Length} bytes");

        BoardSide side = BoardSide.Unknown;
        BoardLayer layer = BoardLayer.Unknown;
        Gerber.DetermineBoardSideAndLayer(path, out side, out layer);
        Console.WriteLine($"Detected Side: {side}");
        Console.WriteLine($"Detected Layer: {layer}");

        var log = new StandardConsoleLog();

        try
        {
            if (fileType == BoardFileType.Drill)
            {
                AnalyzeExcellon(log, path);
            }
            else if (fileType == BoardFileType.Gerber)
            {
                AnalyzeGerber(log, path);
            }
            else
            {
                Console.Error.WriteLine("Unrecognized file type.");
                return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Analysis failed: {ex.Message}");
            return 1;
        }
    }

    void AnalyzeGerber(ProgressLog log, string path)
    {
        var parsed = PolyLineSet.LoadGerberFile(log, path);

        Console.WriteLine();
        Console.WriteLine("--- Shapes Summary ---");
        Console.WriteLine($"  Copper shapes:    {parsed.Shapes.Count}");
        Console.WriteLine($"  Display shapes:   {parsed.DisplayShapes.Count}");
        Console.WriteLine($"  Outline shapes:   {parsed.OutlineShapes.Count}");
        Console.WriteLine($"  Zero-size points: {parsed.ZerosizePoints.Count}");

        int totalVerts = 0;
        foreach (var s in parsed.Shapes) totalVerts += s.Count();
        foreach (var s in parsed.DisplayShapes) totalVerts += s.Count();
        foreach (var s in parsed.OutlineShapes) totalVerts += s.Count();
        Console.WriteLine($"  Total vertices:   {totalVerts}");

        Console.WriteLine();
        Console.WriteLine("--- Bounding Box ---");
        var bb = parsed.BoundingBox;
        Console.WriteLine($"  Min: ({bb.TopLeft.X:F4}, {bb.TopLeft.Y:F4})");
        Console.WriteLine($"  Max: ({bb.BottomRight.X:F4}, {bb.BottomRight.Y:F4})");
        Console.WriteLine($"  Width:  {bb.Width():F4}");
        Console.WriteLine($"  Height: {bb.Height():F4}");

        if (parsed.State != null && parsed.State.Apertures != null)
        {
            Console.WriteLine();
            Console.WriteLine("--- Apertures ---");
            Console.WriteLine($"  Count: {parsed.State.Apertures.Count}");
            foreach (var kv in parsed.State.Apertures)
            {
                var ap = kv.Value;
                Console.WriteLine($"  D{ap.ID}: type={ap.ShapeType}");
            }
        }

        if (parsed.State != null)
        {
            Console.WriteLine();
            Console.WriteLine("--- Format ---");
            Console.WriteLine($"  FS: {parsed.State.CoordinateFormat}");
            Console.WriteLine($"  Unit: {(parsed.State.CoordinateFormat.CurrentNumberScale == GerberNumberFormat.NumberScale.Metric ? "mm" : "inch")}");
            Console.WriteLine($"  Zero omission: {(parsed.State.CoordinateFormat.OmitLeading ? "leading" : "trailing")}");
        }
    }

    void AnalyzeExcellon(ProgressLog log, string path)
    {
        var excellon = new ExcellonFile();
        excellon.Load(log, path);

        Console.WriteLine();
        Console.WriteLine("--- Tools ---");
        Console.WriteLine($"  Count: {excellon.Tools.Count}");
        foreach (var kv in excellon.Tools)
        {
            Console.WriteLine($"  T{kv.Key}: diameter={kv.Value.Radius * 2:F4}, drills={kv.Value.Drills.Count}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Drills ---");
        int totalDrills = 0;
        foreach (var kv in excellon.Tools)
        {
            Console.WriteLine($"  T{kv.Key}: {kv.Value.Drills.Count} drills");
            totalDrills += kv.Value.Drills.Count;
        }
        Console.WriteLine($"  Total: {totalDrills} drills");
    }

    public int RunCheckApertures(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: GerberDebugger check-apertures <file>");
            return 1;
        }

        string path = args[0];
        if (!File.Exists(path)) { Console.Error.WriteLine($"File not found: {path}"); return 1; }

        var log = new StandardConsoleLog();
        var parsed = PolyLineSet.LoadGerberFile(log, path);

        if (parsed.State?.Apertures == null || parsed.State.Apertures.Count == 0)
        {
            Console.WriteLine("No apertures defined.");
            return 0;
        }

        Console.WriteLine($"Apertures defined: {parsed.State.Apertures.Count}");
        foreach (var kv in parsed.State.Apertures)
        {
            var ap = kv.Value;
            Console.WriteLine($"  D{ap.ID}: {ap.ShapeType}");
            if (ap.ShapeType == GerberApertureShape.Macro && !string.IsNullOrEmpty(ap.MacroName))
            {
                Console.WriteLine($"    -> macro: {ap.MacroName}");
            }
        }

        return 0;
    }
}

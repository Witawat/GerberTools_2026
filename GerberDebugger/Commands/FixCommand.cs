using GerberLibrary;
using GerberLibrary.Core;
using System.Text.RegularExpressions;

namespace GerberDebugger.Commands;

public class FixCommand
{
    public int Run(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: GerberDebugger fix <file>");
            return 1;
        }

        string path = args[0];
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"File not found: {path}");
            return 1;
        }

        string output = Path.ChangeExtension(path, null) + "_fixed.gbr";
        Console.WriteLine($"Fixing: {path}");
        Console.WriteLine($"Output: {output}");

        var fileType = Gerber.FindFileType(path);
        if (fileType != BoardFileType.Gerber && fileType != BoardFileType.Drill)
        {
            Console.Error.WriteLine($"Unsupported file type: {fileType}");
            return 1;
        }

        int issuesFixed = 0;
        var lines = File.ReadAllLines(path);
        var fixedLines = new List<string>();

        foreach (var rawLine in lines)
        {
            string line = rawLine;

            // Fix 1: Remove trailing whitespace
            string trimmed = line.TrimEnd();
            if (trimmed != line) { issuesFixed++; line = trimmed; }

            // Fix 2: Ensure line ends with asterisk (Gerber only)
            if (fileType == BoardFileType.Gerber && line.Length > 0 && !line.EndsWith("*") && !line.StartsWith(";"))
            {
                // Check if it's a command line that should have *
                if (Regex.IsMatch(line, @"^[GMD%XYIJ]"))
                {
                    line += "*";
                    issuesFixed++;
                }
            }

            // Fix 3: Remove spaces within coordinate pairs (e.g. "X 10Y 20" -> "X10Y20")
            string before = line;
            line = Regex.Replace(line, @"([XYIJ])\s+(\d)", "$1$2");
            if (line != before) issuesFixed++;

            // Fix 4: Fix %FS format spacing
            before = line;
            line = Regex.Replace(line, @"%\s*FS", "%FS");
            line = Regex.Replace(line, @"FS\s+", "FS");
            if (line != before) issuesFixed++;

            fixedLines.Add(line);
        }

        File.WriteAllLines(output, fixedLines);

        Console.WriteLine($"Issues fixed: {issuesFixed}");
        Console.WriteLine($"Done");

        // Try to validate the fixed file
        try
        {
            var log = new StandardConsoleLog();
            var parsed = PolyLineSet.LoadGerberFile(log, output);
            Console.WriteLine("Fixed file: PARSE OK");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fixed file: PARSE FAILED - {ex.Message}");
            return 1;
        }

        return 0;
    }
}

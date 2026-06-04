using GerberLibrary;
using GerberLibrary.Core;
using GerberLibrary.Core.Primitives;
using GerberDebugger.Commands;
using GerberDebugger.Knowledge;

namespace GerberDebugger;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            PrintUsage();
            return 1;
        }

        string command = args[0].ToLower();
        string[] cmdArgs = args.Skip(1).ToArray();

        Gerber.ThrowExceptions = true;
        Gerber.ShowProgress = false;
        Gerber.SaveIntermediateImages = false;
        Gerber.ExtremelyVerbose = false;

        try
        {
            return command switch
            {
                "validate" => new ValidateCommand().Run(cmdArgs),
                "analyze" => new AnalyzeCommand().Run(cmdArgs),
                "visualize" => new VisualizeCommand().Run(cmdArgs),
                "panel-validate" => new PanelValidateCommand().Run(cmdArgs),
                "diff" => new DiffCommand().Run(cmdArgs),
                "fix" => new FixCommand().Run(cmdArgs),
                "check-apertures" => new AnalyzeCommand().RunCheckApertures(cmdArgs),
                "patterns" => new DebugPatterns().ListPatterns(),
                "help" or "--help" or "-h" => PrintUsage(),
                _ => PrintUnknown(command),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
                Console.Error.WriteLine($"  Cause: {ex.InnerException.Message}");
            return 1;
        }
    }

    static int PrintUsage()
    {
        Console.WriteLine("GerberDebugger - Gerber RS-274X Debug Utility");
        Console.WriteLine();
        Console.WriteLine("Usage: GerberDebugger <command> [args]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  validate <file>            - Parse and validate a Gerber/Excellon file");
        Console.WriteLine("  analyze <file>             - Report file statistics and properties");
        Console.WriteLine("  visualize <file> [--dpi N] - Render Gerber to PNG for visual inspection");
        Console.WriteLine("  panel-validate <folder>    - Load folder as board, verify outline/layers");
        Console.WriteLine("  diff <file1> <file2>       - Compare two Gerber files");
        Console.WriteLine("  fix <file>                 - Sanitize and fix common issues");
        Console.WriteLine("  check-apertures <file>     - Validate aperture definitions");
        Console.WriteLine("  patterns                   - List known debug patterns");
        Console.WriteLine("  help                       - Show this help");
        return 0;
    }

    static int PrintUnknown(string cmd)
    {
        Console.Error.WriteLine($"Unknown command: {cmd}");
        Console.Error.WriteLine("Use 'GerberDebugger help' for usage.");
        return 1;
    }
}

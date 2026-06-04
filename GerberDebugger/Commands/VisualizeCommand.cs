using GerberLibrary;
using GerberLibrary.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GerberDebugger.Commands;

public class VisualizeCommand
{
    public int Run(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: GerberDebugger visualize <file> [--dpi N]");
            return 1;
        }

        string path = args[0];
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"File not found: {path}");
            return 1;
        }

        int dpi = 400;
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--dpi" && i + 1 < args.Length)
                int.TryParse(args[++i], out dpi);
        }

        var fileType = Gerber.FindFileType(path);
        if (fileType != BoardFileType.Gerber)
        {
            Console.Error.WriteLine($"Visualize only supports Gerber files (got {fileType})");
            return 1;
        }

        string output = Path.ChangeExtension(path, null) + "_debug.png";
        Console.WriteLine($"Rendering {path} -> {output} (dpi={dpi})");

        try
        {
            Gerber.SaveGerberFileToImage(new StandardConsoleLog(), path, output, dpi,
                Color.FromRgb(0, 0, 0), // background black
                Color.FromRgb(255, 255, 255)); // foreground white

            Console.WriteLine($"Done: {output}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Render failed: {ex.Message}");
            return 1;
        }
    }
}

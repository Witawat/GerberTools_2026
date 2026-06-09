using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Globalization;

using GerberLibrary;
using GerberLibrary.Core;
using GerberLibrary.Core.Primitives;

namespace GerberToDXF
{
    class Program
    {

        static string GetLayerName(string filename, BoardLayer layer, BoardSide side)
        {
            if (layer == BoardLayer.Outline) return "Outline";
            if (layer == BoardLayer.Drill) return "Drill";

            string sideName = side == BoardSide.Top ? "Top" : side == BoardSide.Bottom ? "Bottom" : "Both";
            string layerName = layer switch
            {
                BoardLayer.Copper => "Copper",
                BoardLayer.Silk => "Silk",
                BoardLayer.SolderMask => "Mask",
                BoardLayer.Paste => "Paste",
                _ => layer.ToString()
            };
            return sideName + layerName;
        }

        static void EmitLine(List<string> dxf, double x1, double y1, double x2, double y2, string layer)
        {
            var ci = CultureInfo.InvariantCulture;
            dxf.Add("0");
            dxf.Add("LINE");
            dxf.Add("8");
            dxf.Add(layer);
            dxf.Add("10");
            dxf.Add(x1.ToString(ci));
            dxf.Add("20");
            dxf.Add(y1.ToString(ci));
            dxf.Add("11");
            dxf.Add(x2.ToString(ci));
            dxf.Add("21");
            dxf.Add(y2.ToString(ci));
        }

        static void EmitPolyLineAsLines(List<string> dxf, PolyLine poly, string layer)
        {
            if (poly.Vertices.Count < 2) return;
            for (int i = 0; i < poly.Vertices.Count - 1; i++)
            {
                var v1 = poly.Vertices[i];
                var v2 = poly.Vertices[i + 1];
                EmitLine(dxf, v1.X, v1.Y, v2.X, v2.Y, layer);
            }
            // Close the polygon
            var first = poly.Vertices[0];
            var last = poly.Vertices[poly.Vertices.Count - 1];
            if (first != last)
                EmitLine(dxf, last.X, last.Y, first.X, first.Y, layer);
        }

        static void ConvertFile(string from, string to, bool displayshapes, bool outlineshapes)
        {

            ParsedGerber PLS = PolyLineSet.LoadGerberFile(new StandardConsoleLog(), from, true, State: new GerberParserState() { PreCombinePolygons = true, SkipThinShapeProcessing = true });

            var layerName = GetLayerName(Path.GetFileName(from), PLS.Layer, PLS.Side);

            var dxf = new List<string>();
            dxf.Add("0");
            dxf.Add("SECTION");
            dxf.Add("2");
            dxf.Add("ENTITIES");

            if (outlineshapes)
                foreach (var a in PLS.OutlineShapes)
                    EmitPolyLineAsLines(dxf, a, layerName);

            if (displayshapes)
                foreach (var a in PLS.DisplayShapes)
                    EmitPolyLineAsLines(dxf, a, layerName);

            dxf.Add("0");
            dxf.Add("ENDSEC");
            dxf.Add("0");
            dxf.Add("EOF");

            File.WriteAllLines(to, dxf);
        }

        static void BatchConvert(string folder, string outdir, bool displayshapes, bool outlineshapes)
        {
            var files = Directory.GetFiles(folder);
            int count = 0;
            foreach (var f in files)
            {
                var ext = Path.GetExtension(f).ToLower();
                if (ext == ".dxf" || ext == ".ai" || ext == ".gpi")
                    continue;

                var outfile = Path.Combine(outdir, Path.GetFileName(f) + ".dxf");
                Console.WriteLine("Converting: {0}", Path.GetFileName(f));
                ConvertFile(f, outfile, displayshapes, outlineshapes);
                count++;
            }
            Console.WriteLine("Done. {0} files converted to DXF.", count);
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("GerberToDxf - Convert Gerber/Drill files to DXF format");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  GerberToDxf <file.gerber> <output.dxf> [-nooutline] [-nodisplay]");
                Console.WriteLine("  GerberToDxf <folder>");
                Console.WriteLine("  GerberToDxf <archive.zip>");
                Console.WriteLine();
                Console.WriteLine("  Drag-and-drop a Gerber file, folder, or ZIP onto the EXE.");
                return;
            }

            bool outlineshapes = true;
            bool displayshapes = true;
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-nooutline") outlineshapes = false;
                if (args[i] == "-nodisplay") displayshapes = false;
            }

            // --- ZIP handling ---
            if (args[0].ToLower().EndsWith(".zip"))
            {
                if (!File.Exists(args[0]))
                {
                    Console.WriteLine("ZIP file not found: {0}", args[0]);
                    return;
                }

                var zipName = Path.GetFileNameWithoutExtension(args[0]);
                var zipDir = Path.GetDirectoryName(Path.GetFullPath(args[0]));
                var outDir = Path.Combine(zipDir, zipName + "_dxf");
                Directory.CreateDirectory(outDir);

                var tempDir = Path.Combine(Path.GetTempPath(), "GerberToDxf_" + Guid.NewGuid().ToString("N"));
                try
                {
                    Console.WriteLine("Extracting: {0}", Path.GetFileName(args[0]));
                    ZipFile.ExtractToDirectory(args[0], tempDir);

                    // Find gerber files (may be in subdirectory)
                    var gerberDir = tempDir;
                    var subDirs = Directory.GetDirectories(tempDir);
                    if (subDirs.Length == 1)
                        gerberDir = subDirs[0];

                    BatchConvert(gerberDir, outDir, displayshapes, outlineshapes);

                    Console.WriteLine("Output: {0}", outDir);
                }
                finally
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
                return;
            }

            // --- Directory handling ---
            if (Directory.Exists(args[0]))
            {
                var outDir = args.Length > 1 ? args[1] : args[0] + "_dxf";
                Directory.CreateDirectory(outDir);
                BatchConvert(args[0], outDir, displayshapes, outlineshapes);
                return;
            }

            // --- Single file handling ---
            if (File.Exists(args[0]))
            {
                var outfile = args.Length > 1 ? args[1] : args[0] + ".dxf";
                ConvertFile(args[0], outfile, displayshapes, outlineshapes);
                return;
            }

            Console.WriteLine("file/folder not found: {0}", args[0]);
        }
    }
}

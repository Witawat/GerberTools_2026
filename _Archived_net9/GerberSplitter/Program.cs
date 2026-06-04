using GerberLibrary;
using GerberLibrary.Core;
using GerberLibrary.Core.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberSplitter
{
    class Program
    {

     

        static void Main(string[] args)
        {
            if (args.Count() >= 2)
            {
                string slicefile = args[0];
                List<string> gerbers = new List<string>();
                gerbers.AddRange(args.Skip(1));
                Slice(slicefile, gerbers);
            }
        }

        static void Slice(string slicefile, List<string> inputgerbers)
        { 
            List<PolyLine> SliceSet = new List<PolyLine>();

            var OutputFolder = Path.Combine(Path.GetDirectoryName(slicefile), "Output", Path.GetFileNameWithoutExtension(slicefile));

            var state = new GerberParserState() { PreCombinePolygons = true };
             ParsedGerber P = PolyLineSet.LoadGerberFile(new StandardConsoleLog(), slicefile, false, false, state);

            foreach (var l in P.Shapes)
            {
                SliceSet.Add(l);
            }
            foreach (var l in P.OutlineShapes)
            {
                SliceSet.Add(l);
            }

            int slid = 1;

            foreach (var S in SliceSet)
            {
                Console.WriteLine("Slicing {0}/{1}", slid, SliceSet.Count); 
                var SliceOutputFolder = Path.Combine(OutputFolder, "Slice" + slid.ToString());
                if (Directory.Exists(SliceOutputFolder) == false) Directory.CreateDirectory(SliceOutputFolder);

                foreach (var a in inputgerbers)
                {
                    try
                    {

                        var bf = GerberLibrary.Gerber.FindFileType(a);
                        if (bf == BoardFileType.Gerber)
                        {
                            BoardSide bs;
                            BoardLayer L;
                            GerberLibrary.Gerber.DetermineBoardSideAndLayer(a, out bs, out L);

                            GerberMerger.WriteContainedOnly(a, S, Path.Combine(SliceOutputFolder, Path.GetFileName(a)), new StandardConsoleLog());
                        }
                    }
                    catch (Exception) { };
                }
                slid++;
            }

        }
    }
}

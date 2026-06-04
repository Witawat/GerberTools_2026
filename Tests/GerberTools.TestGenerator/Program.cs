using System;
using System.IO;

namespace GerberTools.TestGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Generating Test Gerber Files...");
            string outputDir = "TestFiles";
            if (args.Length > 0) outputDir = args[0];
            
            if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
            Directory.CreateDirectory(outputDir);

            // 1. Simple Square Outline (Gerber X2)
            string outlineContent = @"%TF.FileFunction,Profile,NP*%
%FSLAX46Y46*%
%MOMM*%
%LPD*%
G36*
X0Y0D02*
X100000000Y0D01*
X100000000Y100000000D01*
X0Y100000000D01*
X0Y0D01*
G37*
M02*";
            File.WriteAllText(Path.Combine(outputDir, "board_outline.gko"), outlineContent);

            // 2. Top Copper (Simple Tracks)
            string topCopperContent = @"%TF.FileFunction,Copper,L1,Top*%
%FSLAX46Y46*%
%MOMM*%
%LPD*%
%ADD10C,0.500*%
D10*
X10000000Y10000000D02*
X90000000Y90000000D01*
M02*";
            File.WriteAllText(Path.Combine(outputDir, "top_copper.gtl"), topCopperContent);

            // 3. Bottom Copper (Simple Tracks)
            string bottomCopperContent = @"%TF.FileFunction,Copper,L2,Bot*%
%FSLAX46Y46*%
%MOMM*%
%LPD*%
%ADD10C,0.500*%
D10*
X10000000Y90000000D02*
X90000000Y10000000D01*
M02*";
            File.WriteAllText(Path.Combine(outputDir, "bottom_copper.gbl"), bottomCopperContent);

            // 4. Top Silk (Simple Lines)
            string topSilkContent = @"%TF.FileFunction,Legend,Top*%
%FSLAX46Y46*%
%MOMM*%
%LPD*%
%ADD10C,0.200*%
D10*
X20000000Y20000000D02*
X20000000Y80000000D01*
X80000000Y80000000D01*
X80000000Y20000000D01*
X20000000Y20000000D01*
M02*";
            File.WriteAllText(Path.Combine(outputDir, "top_silk.gto"), topSilkContent);

            // 5. Bottom Silk (Simple Lines)
            string bottomSilkContent = @"%TF.FileFunction,Legend,Bot*%
%FSLAX46Y46*%
%MOMM*%
%LPD*%
%ADD10C,0.200*%
D10*
X30000000Y30000000D02*
X70000000Y70000000D01*
M02*";
            File.WriteAllText(Path.Combine(outputDir, "bottom_silk.gbo"), bottomSilkContent);

            // 3. Drill File (Excellon)
            string drillContent = @"M48
METRIC
T01C0.800
%
T01
X500000Y500000
M30";
            File.WriteAllText(Path.Combine(outputDir, "drills.drl"), drillContent);

            Console.WriteLine($"Generated 3 test files in {Path.GetFullPath(outputDir)}");
        }
    }
}

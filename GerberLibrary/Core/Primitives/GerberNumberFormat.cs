using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberLibrary.Core.Primitives
{
    public class GerberNumberFormat
    {
        public int DigitsBefore = 4;
        public int DigitsAfter = 6;
        public bool OmitLeading; // otherwise trailing;
        public bool Relativemode;

        public override string ToString()
        {
            return String.Format("before: {0}, after: {1}, omitleading: {2}, relativemode: {3}, scale: {4}", DigitsBefore, DigitsAfter, OmitLeading, Relativemode, CurrentNumberScale);

        }

        public double Decode(string Numbers, bool hasdecimalpoint)
        {

            double R = 0;
            bool invert = false;
            if (Numbers[0] == '-')
            {
                invert = true;
                Numbers = Numbers.Substring(1);
            }
            if (Numbers.IndexOf('.') > -1 && hasdecimalpoint == true)
            {
                double D = 0;
                Double.TryParse(Numbers, NumberStyles.Any, CultureInfo.InvariantCulture, out D);
                if (invert) D = -D;
                return D;
            }
            while (Numbers.Length < DigitsAfter + DigitsBefore)
            {
                if (OmitLeading)
                {
                    Numbers = "0" + Numbers;
                }
                else
                {
                    Numbers = Numbers + "0";
                }
            }

            for (int i = 0; i < Numbers.Length; i++)
            {
                R = R + int.Parse(Numbers[i].ToString());
                R = R * 10;
            }

            R = R / Math.Pow(10, DigitsAfter + 1);

            if (invert) return -R;
            return R;
        }

        public void Parse(string p)
        {
            // Extract %FS format: check for Xab Yab pattern (e.g., %FSLAX46Y46*%)
            // Do NOT use GerberSplitter; it decodes numbers through the NumberFormat
            // which corrupts the format digits. Use plain regex instead.
            var match = System.Text.RegularExpressions.Regex.Match(p, @"X(\d+)Y(\d+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            int X1 = 0, X2 = 0;
            if (match.Success)
            {
                int.TryParse(match.Groups[1].Value, out X1);
                int.TryParse(match.Groups[2].Value, out X2);
            }
            else
            {
                var match2 = System.Text.RegularExpressions.Regex.Match(p, @"X(\d+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match2.Success)
                    int.TryParse(match2.Groups[1].Value, out X1);
                X2 = X1;
            }
            if (true)//GCC.charcommands[2] == 'S')
            {
                if (p.IndexOf('L') > p.IndexOf('T'))
                {
                    OmitLeading = true;
                }
                else
                {
                    OmitLeading = false;
                    // omit trailing zeroes
                }
                if (p.IndexOf('A') > p.IndexOf('I'))
                {
                    Relativemode = false;
                }
                else
                {
                    Relativemode = true;
                }


                if (X1 > 0 && X2 > 0)
                {
                    if (X1 != X2)
                    {
                        Console.WriteLine("Format has different precisions for X and Y coordinates - not supported yet.");
                    }

                    DigitsBefore = X1 / 10;
                    DigitsAfter = X1 % 10;
                }
                else
                {
                    Console.WriteLine("Warning: Could not parse coordinate format from '%FS' line, using default 4.6. Line: {0}", p);
                    DigitsBefore = 4;
                    DigitsAfter = 6;
                }
            }

        }

        public string Format(double p)
        {
            Int64 R = (Int64)(p * Math.Pow(10, DigitsAfter));
            return R.ToString("D" + (DigitsAfter + DigitsBefore).ToString());
        }

        public string BuildGerberFormatLine()
        {
            return string.Format("%FSLAX{0}{1}Y{0}{1}*%", DigitsBefore.ToString("D1"), DigitsAfter.ToString("D1"));

        }

        // public void DecodeGerber(string GerberLine)
        // {
        // }

        public double Multiplier = 25.4;

        public double ScaleFileToMM(double Val)
        {
            return Val * Multiplier;
        }

        public double _ScaleMMToFile(double Val)
        {
            return Val / Multiplier;
        }

        public enum NumberScale
        {
            Metric,
            Imperial
        }

        public NumberScale CurrentNumberScale = NumberScale.Imperial;

        public void SetImperialMode()
        {
            CurrentNumberScale = NumberScale.Imperial;
            Multiplier = 25.4d;
        }

        public void SetMetricMode()
        {
            CurrentNumberScale = NumberScale.Metric;
            Multiplier = 1.0d;
        }

        public string BuildMetricImperialFormatLine()
        {
            if (CurrentNumberScale == NumberScale.Imperial) return Gerber.INCH;
            return Gerber.MM;
        }



        public GerberQuadrantMode CurrentQuadrantMode = GerberQuadrantMode.Single;

        internal void SetSingleQuadrantMode()
        {
            CurrentQuadrantMode = GerberQuadrantMode.Single;
            if (Gerber.ShowProgress) Console.WriteLine("QuadrantMode: Single");

        }

        internal void SetMultiQuadrantMode()
        {
            CurrentQuadrantMode = GerberQuadrantMode.Multi;
            if (Gerber.ShowProgress) Console.WriteLine("QuadrantMode: Multi");
        }
    }

}

namespace GerberDebugger.Knowledge;

public class DebugPatterns
{
    public int ListPatterns()
    {
        Console.WriteLine("=== Known Gerber Debug Patterns ===");
        Console.WriteLine();
        Console.WriteLine("1. MISSING_OUTLINE");
        Console.WriteLine("   Symptom: Panel says 'no outline available'");
        Console.WriteLine("   Check:  File has .gko/.oln/.gm1 extension, or outline drawn with G36/G37");
        Console.WriteLine("   Fix:    Use GerberSanitize, or manually add outline layer");
        Console.WriteLine();
        Console.WriteLine("2. APERTURE_UNDEFINED");
        Console.WriteLine("   Symptom: Parser error 'aperture not defined' or missing shapes");
        Console.WriteLine("   Check:  Run 'check-apertures' to list all D-codes vs defined apertures");
        Console.WriteLine("   Fix:    Add missing %AD definition, or use GerberCombiner to merge");
        Console.WriteLine();
        Console.WriteLine("3. COORDINATE_FORMAT_MISMATCH");
        Console.WriteLine("   Symptom: Board dimensions orders of magnitude off");
        Console.WriteLine("   Check:  FS line digits vs actual coordinate values");
        Console.WriteLine("   Fix:    Correct %FS leading/trailing zero and digit count");
        Console.WriteLine();
        Console.WriteLine("4. POLARITY_INVERSION");
        Console.WriteLine("   Symptom: Copper appears as holes, holes as copper");
        Console.WriteLine("   Check:  LPD vs LPC commands, polygon winding direction");
        Console.WriteLine("   Fix:    Use GerberSubtract or fix %LPD/%LPC in source");
        Console.WriteLine();
        Console.WriteLine("5. BROKEN_LINE_FORMAT");
        Console.WriteLine("   Symptom: Parser stops early or skips data");
        Console.WriteLine("   Check:  Whitespace in coordinates, missing asterisks, long lines");
        Console.WriteLine("   Fix:    Use 'fix' command to auto-correct");
        Console.WriteLine();
        Console.WriteLine("6. EXCELLON_HEADER_MISSING");
        Console.WriteLine("   Symptom: Drill file not detected as Excellon");
        Console.WriteLine("   Check:  File starts with M48; has METRIC/INCH; has tool defs");
        Console.WriteLine("   Fix:    Add M48 header with proper tool definitions");
        Console.WriteLine();
        Console.WriteLine("7. LAYER_CLASSIFICATION_WRONG");
        Console.WriteLine("   Symptom: Silkscreen shows as copper, or wrong side");
        Console.WriteLine("   Check:  File extension vs Gerber.DetermineBoardSideAndLayer()");
        Console.WriteLine("   Fix:    Rename file to standard extension (.gtl/.gbl/.gto/.gbo etc.)");
        Console.WriteLine();
        Console.WriteLine("8. NO_SHAPES_IN_OUTPUT");
        Console.WriteLine("   Symptom: Gerber file parses OK but produces no visible shapes");
        Console.WriteLine("   Check:  All D01 commands reference valid apertures; polarity is LPD");
        Console.WriteLine("   Fix:    Inspect with 'visualize' command to see what renders");
        Console.WriteLine();
        Console.WriteLine("Use: GerberDebugger <command> <file> to diagnose");
        return 0;
    }
}

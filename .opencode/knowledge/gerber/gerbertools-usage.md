# GerberTools CLI Usage

## GerberAnalyse
Analyzes Gerber files or directories/ZIPs and reports board stats.

```
GerberAnalyse.exe <inputfile_or_folder>
```

Output: board dimensions, layer names/types, file sizes, drill hit counts per tool.

**Code reference**: `GerberAnalyse/GerberAnalyse.cs`

---

## GerberSanitize
Fixes common Gerber formatting issues.

```
GerberSanitize.exe <file1> [file2...]
```

Output: writes `.sanitized.txt` files with fixed content.
Fixes: trailing zeros, whitespace normalization, line splitting.

**Code reference**: `GerberSanitize/Program.cs`

---

## GerberCombiner
Merges multiple Gerber/Excellon files into one output file.

```
GerberCombiner.exe <outputfile> <inputfile1> <inputfile2> ...
```

Handles aperture deduplication and format harmonization.
**Code reference**: `GerberCombiner/CombinerProgram.cs`, `GerberLibrary.Core/Core/GerberMerger.cs`

---

## GerberMover
Translates and/or rotates a Gerber file.

```
GerberMover.exe <inputfile> <outputfile> [X Y CX CY Angle]
```

- X, Y: translation offset
- CX, CY: rotation pivot point
- Angle: rotation angle in degrees

**Code reference**: `GerberMover/GerberMover.cs`, `GerberLibrary.Core/Core/GerberTransposer.cs`

---

## GerberClipper
Clips a Gerber file to an outline polygon.

```
GerberClipper.exe <outlinegerber> <subjectfile> <outputfile>
```

Keeps only shapes inside the outline.
**Code reference**: `GerberClipper/GerberClipper.cs`

---

## GerberSubtract
Subtracts one Gerber layer from another (boolean operation).

```
GerberSubtract.exe <sourcefile> <subtractfile> <outputfile>
```

Uses Clipper library `ctDifference`.
**Code reference**: `GerberSubtract/GerberSubtract.cs`

---

## GerberSplitter
Splits Gerber files into horizontal slices.

```
GerberSplitter.exe <slicefile> <gerberfile1> [gerberfile2 ...]
```

The slice file defines cut positions.
**Code reference**: `GerberSplitter/Program.cs`

---

## GerberToOutline
Extracts board outline to SVG.

```
GerberToOutline.exe <infile> <outfile>
```

Output: SVG file with board outline paths.
**Code reference**: `GerberToOutline/GerberToOutline.cs`

---

## AutoPanelBuilder
CLI panel builder for automated panelization.

```
AutoPanelBuilder.exe [--settings {xml_file}] [--files {folder_list}] [--dumpsample] output_dir
```

- `--settings` : XML config file with panel dimensions, margins
- `--files` : text file with list of board folders to panelize
- `--dumpsample` : generates a sample settings XML file
- `output_dir` : where to write panel output

**Code reference**: `AutoPanelBuilder/AutoPanelBuilder.cs`
**Core engine**: `GerberLibrary.Core/Core/GerberPanel.cs`

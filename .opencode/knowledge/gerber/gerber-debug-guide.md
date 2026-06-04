# Gerber Debug Guide

## Common Issues & Fixes

### 1. Missing or Invalid Outline
**Symptom**: Panelizer shows "no outline available", GerberToImage fails to clip.

**Check**:
- File contains board outline layer (GKO, .gko, .oln, .gm1)
- Outline is properly drawn with G36/G37 polygon mode or closed D01
- Outline shapes are in `DisplayShapes` or `OutlineShapes` lists

**Fix**: Use `GerberDebugger analyze <file>` to check parsed layers.
Use `GerberSanitize` to normalize format.

### 2. Aperture / D-Code Mismatch
**Symptom**: Shapes missing, wrong size, or parser errors.

**Check**:
- Every D01/D03 references a defined aperture (D10+)
- No duplicate D-code definitions
- Aperture macro variables match the expected count

**Fix**: `GerberDebugger check-apertures <file>`

### 3. Coordinate Format Errors
**Symptom**: Board dimensions are wildly wrong (e.g. 1000x instead of 100x).

**Check**:
- FS line: check leading/trailing zero omission, integer/decimal digits
- MO line: IN vs MM
- Double-check: LS (scale) in header

**Fix**: `GerberDebugger analyze <file>` reports bounds in mm.

### 4. Polygon Winding / Polarity
**Symptom**: Copper fills are inverted (holes fill, copper clears).

**Check**:
- `LPD` (dark) vs `LPC` (clear) polarity
- G36/G37 polygon direction: Clipper requires correct orientation
- `PolyLine.FixPolygonWindings()` in GerberLibrary handles this

### 5. Broken Gerber (Line length, whitespace)
**Symptom**: Parser crashes or misses data.

**Check**:
- Lines should end with `*`
- No embedded whitespace in coordinates (e.g. `X 10Y 20` instead of `X10Y20`)
- Cadence/OrCAD format uses different line structure

**Fix**: `GerberSanitize.exe <file>` - fixes whitespace, line endings, trailing zeros.

### 6. Excellon Drill Issues
**Symptom**: Drill hits missing or misplaced.

**Check**:
- M48 header present
- Tool definitions T1C... match drill hits
- METRIC/INCH mode matches the coordinates
- G90/G91 (absolute/incremental) mode

**Fix**: `GerberDebugger analyze <file>` reports drill count per tool.

## KiCad v8/v9/v10 Compatibility Notes

GerberTools now supports KiCad v8+ default filenames (`F_Cu.gbr`, `Edge_Cuts.gbr`, etc.):

| KiCad File | Detected Side | Detected Layer |
|------------|---------------|----------------|
| `F_Cu.gbr` | Top | Copper |
| `B_Cu.gbr` | Bottom | Copper |
| `F_Mask.gbr` | Top | SolderMask |
| `B_Mask.gbr` | Bottom | SolderMask |
| `F_Silkscreen.gbr` | Top | Silk |
| `B_Silkscreen.gbr` | Bottom | Silk |
| `F_Paste.gbr` | Top | Paste |
| `B_Paste.gbr` | Bottom | Paste |
| `Edge_Cuts.gbr` | Both | Outline |
| `In{N}_Cu.gbr` | Internal{N} | Copper |

**Format support**: `%FSLAX46Y46*%` (3 integer + 6 decimal digits in mm) is now correctly parsed.
**X2 Attributes**: `%TF`, `%TA`, `%TO`, `%TD` commands are silently skipped (do not corrupt state).
**Excellon**: Standard KiCad drill format with G85 slots and G01 routing supported.

### KiCad v10-Specific Notes
- No format changes from v8/v9 - same `%FSLAX46Y46*%` default
- Gerber X2 attributes unchanged
- If using "Protel filename extensions", the existing `.GTL`/`.GBL`/`.GKO` mapping already works

## Debug Workflow

### Step 1: Analyze
```
GerberAnalyse.exe <file_or_folder>
GerberDebugger analyze <file>
```

### Step 2: Visualize
```
GerberToImage.exe <file> --dpi 600 --noxray
GerberDebugger visualize <file> --dpi 600
```

### Step 3: Validate & Fix
```
GerberSanitize.exe <file>
GerberDebugger validate <file>
GerberDebugger fix <file>
```

### Step 4: Compare
```
GerberDebugger diff <original> <fixed>
```

## GerberLibrary.Core Parser Notes

- `PolyLineSet.LoadGerberFile()` reads and parses a file
- `GerberParserState` tracks: active apertures, coordinate format, interpolation mode, polarity, polygon mode, SR state
- `ParsedGerber` stores: `Shapes` (copper), `DisplayShapes` (silk), `OutlineShapes` (outline), `ZerosizePoints`
- `Gerber.FindFileType()` auto-detects Gerber vs Excellon
- `Gerber.DetermineBoardSideAndLayer()` classifies by filename
- Exceptions are thrown when `Gerber.ThrowExceptions = true`

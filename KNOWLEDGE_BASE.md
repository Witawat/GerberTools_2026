# GerberTools — Knowledge Base

## Overview

**GerberTools** is a suite of 49 tools and libraries for PCB (Printed Circuit Board) fabrication file processing, panelization, rendering, and manufacturing automation. All 38 legacy `.csproj` projects have been migrated to SDK-style format, enabling `dotnet build` without Visual Studio. The solution spans two generations:

| Generation | Target | UI | Libraries |
|-----------|--------|----|-----------|
| **Legacy** | .NET Framework 4.8 | Windows Forms + OpenTK | `GerberLibrary` (System.Drawing, DotNetZip) |
| **Modern** | .NET 9.0 (cross-platform) | Avalonia UI (MVVM) | `GerberLibrary.Core` (ImageSharp, SharpZipLib) |

A migration from legacy WinForms to modern Avalonia is in progress (tracked in `ProjectProgress/`).

---

## Core Libraries

### GerberLibrary (`GerberLibrary/`)
- **Type:** DLL, .NET Framework 4.8
- **Purpose:** The original Windows-only Gerber processing library. Parses RS-274X Gerber, Excellon drill files. Renders PCB images via System.Drawing. Merges/clips Gerber geometries. Provides panel packing algorithms (MaxRects, RectanglePack). Supplies BOM/CPL data structures for JLCPCB assembly.
- **Key deps:** System.Drawing, DotNetZip (Ionic.Zip), System.Windows.Forms, ClipperLib, Triangle

### GerberLibrary.Core (`GerberLibrary.Core/`)
- **Type:** DLL, .NET 9.0 (cross-platform)
- **Purpose:** The modern cross-platform replacement for `GerberLibrary`. Same functionality but uses SixLabors.ImageSharp for rendering and SharpZipLib for archives. All new tools should target this library.
- **Key deps:** SixLabors.ImageSharp, SharpZipLib, ClipperLib, Triangle, ExcelDataReader
- **Key classes:** `Gerber.cs` (1465 lines — main entry), `PolyLineSet` (Gerber/DXF parser), `ExcellonFile`, `GerberImageCreator` (multi-layer rendering), `GerberMerger`, `GerberPanel`, `MaxRectPacker`, `RectanglePacker`, `SVGWriter`, `GerberOutlineWriter`

### EagleLoaders (`EagleLoaders/`)
- **Type:** DLL, .NET Framework 4.8
- **Purpose:** Parses and renders Autodesk Eagle `.brd`/`.lbr` files. Contains auto-generated XML serialization classes from Eagle XSD schema. Provides a `BoardRenderer` that draws Eagle PCB designs as Bitmap images.
- **Key classes:** `eagle.cs` (3466 lines — auto-generated), `LibraryLoader`, `BoardRenderer`

### TilingLibrary.Core (`TilingLibrary.Core/`)
- **Type:** DLL, .NET 9.0
- **Purpose:** Mathematical tiling and subdivision engine for generating artistic geometric patterns. Implements 15+ tiling types (Penrose, Danzer 7-fold, Ammann-Beenker, Pinwheel, Sphinx, Conway, Chair, etc.) plus QuadTree and Delaunay triangulation art modes.
- **Key deps:** SixLabors.ImageSharp, GlmNet, ClipperLib
- **Key files:** `Tiling.cs` (2041 lines), `TINRSArtWorkRenderer.cs` (933 lines), `SVGThings.cs` (1346 lines)

---

## Gerber Processing CLI Tools

### GerberAnalyse (`GerberAnalyse/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Analyzes Gerber/Excellon files. Reports board dimensions (width/height), bounding box corners, total drill count. Accepts single files, directories, or ZIP archives.

### GerberClipper (`GerberClipper/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Clips a Gerber file to a board outline polygon. Takes outline + subject + output filenames; writes only geometry inside the outline.

### GerberCombiner (`GerberCombiner/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Merges multiple Gerber files or multiple Excellon drill files into a single output file. Auto-detects file type (Gerber vs Drill) and uses appropriate merger.

### GerberDebugger (`GerberDebugger/`)
- **Type:** CLI (.NET 9.0)
- **Purpose:** Multi-command diagnostic utility. Sub-commands: `validate`, `analyze`, `visualize` (render to PNG), `panel-validate`, `diff`, `fix`, `check-apertures`, `patterns`. The Swiss Army knife for Gerber troubleshooting.

### GerberMover (`GerberMover/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Translates, rotates, and transforms Gerber/Excellon files. Supports X/Y translation, center-of-rotation offset, and angle. Can process individual files or entire directories. Generates PNG render of output.

### GerberSanitize (`GerberSanitize/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Cleans/normalizes Gerber RS-274X line formatting. Reads a Gerber file, passes lines through `PolyLineSet.SanitizeInputLines()`, writes `<filename>.sanitized.txt`.

### GerberSplitter (`GerberSplitter/`)
- **Type:** CLI (.NET 9.0)
- **Purpose:** Splits a Gerber set using polygon outline templates ("slice file"). Each outline polygon produces a separate output folder (`Slice1/`, `Slice2/`, ...) with clipped Gerber layers. Useful for separating panel sub-boards.

### GerberSubtract (`GerberSubtract/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Subtracts overlapping aperture flashes from a source Gerber using a subtract Gerber file. **WARNING:** Experimental — labeled "very dangerous" by the author. Always manually verify output.

### GerberToDxf (`GerberToDxf/`)
- **Type:** CLI (.NET 9.0)
- **Purpose:** Converts Gerber to DXF mechanical CAD format. Supports batch conversion of directories. Flags: `-nooutline`, `-nodisplay`.

### GerberToImage (`GerberToImage/`)
- **Type:** CLI (.NET 9.0)
- **Purpose:** Renders Gerber layer sets to high-quality PNG images. Options: `--dpi N`, `--noxray`, `--nopcb`, `--silk <color>`, `--trace <color>`, `--copper <color>`, `--mask <color>`.

### GerberToOutline (`GerberToOutline/`)
- **Type:** CLI (.NET 9.0)
- **Purpose:** Converts Gerber/Excellon to SVG outline preview. Auto-opens the SVG in the default browser on Windows.

### AutoPanelBuilder (`AutoPanelBuilder/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Automated batch PCB panelization. Takes a list of Gerber folders + XML settings file, runs MaxRectPack to fit boards onto a panel, inserts auto-tabs (mousebites) using Delaunay triangulation, exports merged Gerbers and `.gerberset` files.

---

## GUI Applications

### GerberPanelizer (`GerberPanelizer/`)
- **Type:** WinExe (.NET 4.8, Windows Forms + OpenTK)
- **Purpose:** **Flagship application.** Full-featured visual PCB panelizer. MDI interface with OpenGL-accelerated canvas. Features:
  - Drag-and-drop board instances, zoom/pan, undo/redo (20 levels)
  - Multi-select with box-select and Ctrl+click
  - Snap modes: 1mm, 0.5mm, 100mil, 50mil, Off
  - Arrange tools: Align Left/Right/Top/Bottom, Center Horizontally/Vertically
  - Break-tab management: Insert, Create Auto, Delete All/Errors, Merge Overlapping
  - Autopack algorithms: Naive RectanglePack, MaxRects (with rotation)
  - Export merged Gerbers, save/load `.gerberset` project files
  - Recent files list, drag-and-drop panels from Explorer
  - Instance properties editor, tree view, autofit canvas dialog
- **Key deps:** OpenTK + GLControl (GPU rendering), FarseerPhysics, ClipperLib, Triangle
- **Key files:** `GerberPanelizerParent.cs` (MDI parent, 527 lines), `GerberPanelize.cs` (main canvas, 2061 lines)

### GerberViewer (`GerberViewer/`)
- **Type:** WinExe (.NET 4.8, Windows Forms + OpenTK)
- **Purpose:** OpenGL-accelerated Gerber file viewer. DockPanelSuite IDE-like docking interface with side-by-side Top/Bottom board views. Layer list panel for toggling individual Gerber layer visibility. Supports loading folders or individual files. Features FBO (Framebuffer Object) rendering for high-quality anti-aliased display.

### QuickGerberRender (`QuickGerberRender/`)
- **Type:** WinExe (.NET 4.8, Windows Forms)
- **Purpose:** Lightweight drag-and-drop Gerber-to-image renderer. Drop a Gerber folder, choose colors (solder mask, silk, copper, traces), set DPI, and get rendered PNG images. Options for X-Ray mode and PCB overlay.

### GerberDrop (`GerberDrop/`)
- **Type:** WinExe (.NET 9.0, Avalonia UI)
- **Purpose:** Modern cross-platform replacement for QuickGerberRender. Same functionality (drag-drop Gerber → PNG) but with MVVM architecture, live color preview, and hardware-accelerated Avalonia UI.

### JLCDrop (`JLCDrop/`)
- **Type:** WinExe + CLI (.NET 4.8, Windows Forms)
- **Purpose:** Dual-mode JLCPCB fabrication packager. Drag-and-drop a KiCad/Eagle/Diptrace PCB folder to auto-generate BOM + Pick-and-Place files + Gerber ZIP archive ready for JLCPCB upload. CLI mode with `-zip`/`-dontzip`, `-frame` (add production frame).

### VScorePanel (`VScorePanel/`)
- **Type:** WinExe (.NET 4.8, Windows Forms)
- **Purpose:** Drag-and-drop PCB panel creator with two modes:
  - **"Tabby"** — Break-tabs (mousebites) between boards with panel frame
  - **"Groovy"** — V-score/V-groove lines for snap-apart separation
  - Configurable grid (X×Y), gap, frame width, title text. Generates merged Gerbers, frame files, and optional jig files.

### PnP_Processor (`PnP_Processor/`)
- **Type:** WinExe + CLI (.NET 4.8, Windows Forms + DockPanelSuite)
- **Purpose:** Pick-and-Place assembly processor. Loads BOM, PnP placement, Gerber ZIP, and optional stock/inventory files. Validates component placements against the board. Supports board flipping (none/diagonal/horizontal) for double-sided assembly. Dockable GUI with board display, BOM list with search, and actions panel.

### SolderTool (`SolderTool/`)
- **Type:** WinExe (.NET 4.8, Windows Forms + DockPanelSuite)
- **Purpose:** Hand-soldering assistant. Loads BOM + placement data from Gerber ZIP. Displays visual component map on the board. Track soldered/unsoldered parts with keyboard navigation (Up/Down + Enter). Shows progress as soldered/total count. Multi-document tabbed interface.

### CaseBuilder (`CaseBuilder/`)
- **Type:** WinExe (.NET 4.8, Windows Forms)
- **Purpose:** Generates laser-cut enclosure DXF outlines from PCB Gerbers ("Sick Of Beige" concept). Drag-and-drop Gerber folder, adjust offset and hole diameter, auto-opens DXF for the enclosure.

### EagleBoardToCHeader (`EagleBoardToCHeader/`)
- **Type:** WinExe (.NET 4.8, Windows Forms)
- **Purpose:** Converts Eagle `.brd` files to C/C++ header files. Extracts component placement data (x, y, rotation, name, library, package, value) and generates C-header arrays for embedded firmware reference.

### FrontPanelBuilder (`FrontPanelBuilder/`)
- **Type:** WinExe (.NET 4.8, Windows Forms)
- **Purpose:** Generates front panel Gerbers (typically Eurorack/synth modules). Processes PCB silkscreen + PNG overlay images (silk and gold layers) to produce front panel manufacturing files. Handles both top and bottom panels.

### FitBitmapToOutlineAndMerge (`FitBitmapToOutlineAndMerge/`)
- **Type:** WinExe (.NET 4.8, Windows Forms)
- **Purpose:** Merges bitmap artwork (logos/graphics) onto PCB silkscreen within board outline bounds. Select bitmap, Gerber outline, and Gerber silkscreen; tool scales bitmap to fit and merges onto the silkscreen layer.

---

## PCB Design Generators

### AntennaBuilder (`AntennaBuilder/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Generates complete Gerber + Excellon drill files for NFC (Near Field Communication) antenna PCBs. Produces rectangular boards with multi-turn spiral antenna traces on both top/bottom copper layers, vias, mounting holes, solder mask, silkscreen, and outline.

### ProtoBoardGenerator (`ProtoBoardGenerator/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Generates complete Gerber files for custom prototyping/perfboard PCBs. Two styles: standard grid protoboard and "flower-style" protoboard. Configurable width, height, mounting holes, corner rounding, pad spacing.

### ProductionFrame (`ProductionFrame/`)
- **Type:** WinExe (.NET 4.8, Windows Forms)
- **Purpose:** GUI for generating production panel/frame Gerbers with configurable margins, mounting holes, and fiducial markers. Outputs all standard layers (outline, silkscreen, copper, solder mask, drill).

### LightPipeBuilder (`LightPipeBuilder/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Reads KiCad PCB files and generates OpenSCAD `.scad` files for 3D-printable light pipe supports. Locates LED_0603 components and LightpipeHole footprints, outputs SCAD modules at board coordinates plus CSV position table.

---

## Artwork / Tiling Tools (TINRS — "This Is Not Rocket Science" brand)

### TINRS-ArtWorkGenerator (`TINRS-ArtWorkGenerator/`)
- **Type:** WinExe (.NET 4.8, Windows Forms)
- **Purpose:** **Legacy** GUI for generating geometric tiling artwork from bitmap masks. Uses the older `TINRS-ArtWork` library. Load a mask image, configure tiling type/depth/threshold/symmetry, exports as SVG.

### TiNRS-Tiler (`TiNRS-Tiler/`)
- **Type:** WinExe (.NET 9.0, Avalonia UI)
- **Purpose:** **Modern replacement** for TINRS-ArtWorkGenerator. Cross-platform Avalonia GUI with MVVM pattern. Same geometric tiling artwork generation with reactive property system, background rendering, file watcher, and debounced auto-update.

### ImageToGerber (`ImageToGerber/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Converts PNG bitmap images (silkscreen + gold/copper patterns) into Gerber files for front panel production. Reads board outline, matches `*Silk.png` and `*Gold.png` overlays, outputs `.gto`, `.gbl`/`.gtl`, `.gbs`/`.gts` layers.

### MakeIcon (`MakeIcon/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Generates multi-resolution Windows `.ico` files using the TilingLibrary's geometric art engine. Used for branding/UI icons.

### IconBuilder (`Project_Utilities/IconBuilder/`)
- **Type:** WinExe (.NET 4.8, Windows Forms)
- **Purpose:** GUI for interactively designing and previewing custom icons rendered with TilingLibrary artwork. Hosts multiple IconFrame sub-forms at different sizes.

### IconScanner (`Project_Utilities/IconScanner/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Batch icon generator. Scans a directory (or `.svgsort`/`.svgs` files), collects labels, generates multi-resolution `.ico` files with unique color tints.

---

## DirtyPCBs Suite

A collection of 6 CLI tools for the DirtyPCBs fabrication service:

| Tool | Purpose |
|------|---------|
| **SickOfBeige** | CLI enclosure/case DXF generator from Gerbers (configurable offset + hole diameter) |
| **BoardStats** | Extract PCB dimensions + drill count as JSON |
| **BoardRender** | Render PCB preview images with customizable colors (for web display) |
| **DXFStats** | Extract total trace length + bounding box from DXF files |
| **Base64Extractor** | Decode Base64 email attachments to ZIP |
| **LocaleTest** | Debug locale-related Gerber coordinate parsing issues |

---

## Manufacturing Support

### BOMConsolidator (`BOMConsolidator/`)
- **Type:** CLI (.NET 4.8)
- **Purpose:** Consolidates and condenses JLCPCB-format BOM files. Takes BOM + CPL file paths, outputs `*_newcondens` condensed BOM.

---

## Infrastructure & Utilities

### Project_Utilities
- **TilingLibrary (TINRS-ArtWork)** — Legacy .NET 4.8 artwork library used by TINRS-ArtWorkGenerator, MakeIcon, IconBuilder, IconScanner
- **ReleaseBuilder** — CLI tool that collects build outputs into timestamped ZIP archives
- **IconBuilder** / **IconScanner** — Icon design and batch generation tools (see above)

### ProjectProgress (`ProjectProgress/`)
- **Type:** Documentation folder (not executable)
- **Purpose:** Tracks the TiNRS-Tiler migration from .NET 4.8 WinForms → .NET 9.0 Avalonia UI. Contains migration checklists, architecture docs, quickstart guides, and code pattern references.

### Tests (`Tests/`)
- **Type:** CLI (.NET 9.0)
- **Purpose:** Synthetic test file generator (`GerberTools.TestGenerator`). Produces a standard 100×100mm board with outline, top/bottom copper, silk, and drill files as raw Gerber X2/Excellon ASCII for testing other tools.

### Build Scripts
- `build_all.ps1` — PowerShell build script. Builds all ~46 projects (skips net9.0 if SDK not available), copies GUI apps to `Build/Output/<AppName>/` and CLI tools to `Build/Output/CommandLine/`. Usage: `.\build_all.ps1` or `.\build_all.ps1 -Config Release`
- `build_all.bat` — Legacy batch build (deprecated; use build_all.ps1)
- `build.sh` — Linux shell build script
- `build_QuickRender.bat` — Quick render-only build
- `BuildRelease.bat` — Full release build

All projects use SDK-style csproj format — buildable with `dotnet build <project>.csproj` without Visual Studio.

### ButtonsAndIcons (`ButtonsAndIcons/`)
- **Type:** Resource folder (not a project)
- **Purpose:** 16 JPG button icon images (Up, Down, Left, Right, RotateLeft, RotateRight, ScaleUp, ScaleDown + hover variants). Used by GUI applications.

### CarvedOut (`CarvedOut/`)
- **Type:** WinExe (VB.NET, .NET 5.0) — **Empty stub**
- **Purpose:** Placeholder Windows Forms app. No implemented functionality.

### MigrationTest (`MigrationTest/`)
- **Type:** CLI (.NET 9.0)
- **Purpose:** Smoke test for GerberLibrary → GerberLibrary.Core migration. Creates test Gerber/drill files, calls `ZipGerberFolderToFactoryFolder()`, verifies SharpZipLib can read the ZIP back.

---

## Architectural Notes

1. **Two library generations coexist:**
   - `GerberLibrary` (.NET 4.8) → used by all legacy WinForms apps
   - `GerberLibrary.Core` (.NET 9.0) → used by new Avalonia/CLI tools
   - Many projects still need migration from old to new library

2. **UI migration in progress:**
   - Legacy: Windows Forms + OpenTK + DockPanelSuite
   - Modern: Avalonia UI + MVVM (CommunityToolkit.Mvvm)
   - Examples: `QuickGerberRender` → `GerberDrop`, `TINRS-ArtWorkGenerator` → `TiNRS-Tiler`

3. **Dependency pattern:**
   - All rendering/processing flows through `GerberLibrary.Core.Gerber` (or `GerberLibrary.Gerber`)
   - ClipperLib and Triangle are used throughout for polygon clipping and triangulation
   - Image libraries: System.Drawing (legacy) vs SixLabors.ImageSharp (modern)

4. **TINRS brand tools:**
   - "This Is Not Rocket Science" — artistic PCB design
   - `TiNRS-Tiler` / `TINRS-ArtWorkGenerator` — geometric tiling artwork
   - `TilingLibrary.Core` — cross-platform tiling engine
   - MakeIcon / IconBuilder / IconScanner — icon generation from tilings

---

## Quick Reference: All Projects

| # | Folder | Name | Type | .NET | Purpose |
|---|--------|------|------|------|---------|
| 1 | GerberLibrary | GerberLibrary | Library | 4.8 | Core Gerber/PCB processing (Windows) |
| 2 | GerberLibrary.Core | GerberLibrary.Core | Library | 9.0 | Core Gerber/PCB processing (cross-platform) |
| 3 | EagleLoaders | EagleLoaders | Library | 4.8 | Eagle BRD/LBR parser + renderer |
| 4 | TilingLibrary.Core | TilingLibrary.Core | Library | 9.0 | Geometric tiling artwork engine |
| 5 | GerberPanelizer | GerberPanelizer | WinExe | 4.8 | Visual PCB panelizer (flagship) |
| 6 | GerberViewer | GerberViewer | WinExe | 4.8 | OpenGL Gerber layer viewer |
| 7 | QuickGerberRender | QuickGerberRender | WinExe | 4.8 | Drag-drop Gerber → PNG renderer |
| 8 | GerberDrop | GerberDrop | WinExe | 9.0 | Avalonia Gerber → PNG renderer |
| 9 | JLCDrop | JLCDrop | WinExe | 4.8 | JLCPCB fab packager (BOM+Gerber ZIP) |
| 10 | VScorePanel | FrameDrop | WinExe | 4.8 | PCB panelizer (tabs + V-score) |
| 11 | PnP_Processor | PnP_Processor | WinExe | 4.8 | Pick-and-Place assembly processor |
| 12 | SolderTool | SolderTool | WinExe | 4.8 | Hand-soldering assistant |
| 13 | CaseBuilder | CaseBuilder | WinExe | 4.8 | Enclosure DXF generator |
| 14 | EagleBoardToCHeader | EagleBoardToCHeader | WinExe | 4.8 | Eagle → C header converter |
| 15 | FrontPanelBuilder | FrontPanelBuilder | WinExe | 4.8 | Front panel Gerber generator |
| 16 | FitBitmapToOutlineAndMerge | FitBitmapToOutlineAndMerge | WinExe | 4.8 | Bitmap → PCB silkscreen merger |
| 17 | ProductionFrame | ProductionFrame | WinExe | 4.8 | Production panel frame generator |
| 18 | TINRS-ArtWorkGenerator | TINRS-ArtWorkGenerator | WinExe | 4.8 | Tiling artwork generator (legacy) |
| 19 | TiNRS-Tiler | TiNRS.Tiler | WinExe | 9.0 | Tiling artwork generator (Avalonia) |
| 20 | IconBuilder | IconBuilder | WinExe | 4.8 | Interactive icon designer |
| 21 | CarvedOut | CarvedOut | WinExe | 5.0 | Empty VB.NET stub |
| 22 | GerberAnalyse | GerberAnalyse | Exe | 4.8 | Gerber/drill stats analyzer |
| 23 | GerberClipper | GerberClipper | Exe | 4.8 | Clip Gerber to outline |
| 24 | GerberCombiner | GerberCombiner | Exe | 4.8 | Merge multiple Gerber/drill files |
| 25 | GerberDebugger | GerberDebugger | Exe | 9.0 | Multi-command debug utility |
| 26 | GerberMover | GerberMover | Exe | 4.8 | Translate/rotate Gerber files |
| 27 | GerberSanitize | GerberSanitize | Exe | 4.8 | Clean Gerber line formatting |
| 28 | GerberSplitter | GerberSplitter | Exe | 9.0 | Split Gerbers by outline polygons |
| 29 | GerberSubtract | GerberSubtract | Exe | 4.8 | Subtract Gerber features (experimental) |
| 30 | GerberToDxf | GerberToDxf | Exe | 9.0 | Gerber → DXF converter |
| 31 | GerberToImage | GerberToImage | Exe | 9.0 | Gerber → PNG renderer |
| 32 | GerberToOutline | GerberToOutline | Exe | 9.0 | Gerber → SVG outline preview |
| 33 | AutoPanelBuilder | AutoPanelBuilder | Exe | 4.8 | Batch auto-panelizer |
| 34 | AntennaBuilder | NFCAntennaBuilder | Exe | 4.8 | NFC antenna PCB generator |
| 35 | ProtoBoardGenerator | ProtoBoardGenerator | Exe | 4.8 | Protoboard Gerber generator |
| 36 | LightPipeBuilder | LightPipeBuilder | Exe | 4.8 | KiCad → OpenSCAD light pipe |
| 37 | ImageToGerber | ImageToGerber | Exe | 4.8 | PNG → Gerber front panel |
| 38 | MakeIcon | MakeIcon | Exe | 4.8 | Tiling-based icon generator |
| 39 | IconScanner | IconScanner | Exe | 4.8 | Batch icon scanner/generator |
| 40 | BOMConsolidator | BOMConsolidator | Exe | 4.8 | JLCPCB BOM condenser |
| 41 | SickOfBeige | SickOfBeige | Exe | 4.8 | CLI enclosure DXF generator |
| 42 | BoardStats | BoardStats | Exe | 4.8 | PCB dimension + drill JSON |
| 43 | BoardRender | BoardRender | Exe | 4.8 | PCB preview image renderer |
| 44 | DXFStats | DXFStats | Exe | 4.8 | DXF trace length analyzer |
| 45 | Base64Extractor | Base64Extractor | Exe | 4.8 | Email Base64 → ZIP decoder |
| 46 | LocaleTest | LocaleTest | Exe | 4.8 | Locale debug tool |
| 47 | ReleaseBuilder | ReleaseBuilder | Exe | 4.8 | Build output → ZIP packager |
| 48 | MigrationTest | MigrationTest | Exe | 9.0 | Library migration smoke test |
| 49 | Tests | TestGenerator | Exe | 9.0 | Synthetic Gerber test file generator |

---

## Changelog

### 2026-06-21 — Fixed Drill (Excellon) BoundingBox returning `(0,0)-(0,0)`

**Root Cause:** `LoadExcellonDrillFileFromStream` and `LoadExcellonDrillFile` in `PolyLineSet.cs` did not call `CalcPathBounds()` after loading, leaving `BoundingBox.Valid = false` and `TopLeft`/`BottomRight` at `(0,0)-(0,0)`.

**Fix:** Added `Gerb.CalcPathBounds();` before `return` in both methods:
- `LoadExcellonDrillFileFromStream` — `PolyLineSet.cs:300`
- `LoadExcellonDrillFile` — `PolyLineSet.cs:374`

**Affected tools before fix:**
- `GerberViewer` — zoom/fit-to-view broken
- `GerberToOutline` — `Width()`/`Height()` returns 0
- `GerberAnalyse` — board dimensions shown as 0
- `FrontPanelBuilder` / `ImageToGerber` — bounding box overlay positions wrong

**No impact on:**
- `GerberPanelizer` — uses `GerberInstance.BoundingBox` computed separately in `RebuildTransformed`
- Raw .drl/.xln files sent to fab — NC Drill format stores absolute coordinates in-file, independent of BoundingBox

# GerberTools

A suite of 49 C# tools and libraries for loading, editing, creating, panelizing, and rendering Gerber PCB files. All projects use SDK-style `.csproj` format — buildable with `dotnet build` without Visual Studio.

## Quick Start

```powershell
# Build all projects
.\build_all.ps1

# Output goes to:
#   Build\Output\<AppName>\      (GUI applications)
#   Build\Output\CommandLine\    (CLI tools)
```

## Main GUI Applications

| Application | Description |
|------------|-------------|
| **GerberPanelizer** | Flagship visual PCB panelizer with OpenGL canvas. Arrange boards, add break-tabs, export merged Gerbers. |
| **GerberViewer** | OpenGL-accelerated Gerber layer viewer with dockable interface. |
| **QuickGerberRender** | Drag-and-drop Gerber → PNG renderer with customizable colors and DPI. |
| **GerberDrop** | Cross-platform Avalonia version of QuickGerberRender. |
| **JLCDrop** | Drag-and-drop JLCPCB fabrication packager (BOM + PnP + Gerber ZIP). |
| **VScorePanel (FrameDrop)** | PCB panelizer with two modes: Tabby (break-tabs) and Groovy (V-score). |
| **PnP_Processor** | Pick-and-Place assembly processor with dockable BOM viewer. |
| **SolderTool** | Hand-soldering assistant — track soldered/unsoldered parts visually. |
| **CaseBuilder** | Generate laser-cut enclosure DXF from PCB outline ("Sick Of Beige"). |
| **FrontPanelBuilder** | Generate front panel Gerbers from PCB silkscreen + PNG overlays. |
| **FitBitmapToOutlineAndMerge** | Merge bitmap artwork onto PCB silkscreen within board outline. |
| **EagleBoardToCHeader** | Convert Eagle `.brd` to C/C++ header with component placements. |
| **ProductionFrame** | GUI for production panel/frame Gerbers with fiducials. |
| **TINRS-ArtWorkGenerator** | Generate geometric tiling artwork from bitmap masks (legacy WinForms). |
| **TiNRS-Tiler** | Modern Avalonia version of TINRS-ArtWorkGenerator with MVVM. |
| **IconBuilder** | Interactive icon designer using geometric tiling artwork. |
| **OpampCalculator** | Op-amp calculator utility. |

## Command Line Tools

### Gerber Processing
| Tool | Description |
|------|-------------|
| **GerberAnalyse** | Report board dimensions, drill counts, bounding box. |
| **GerberClipper** | Clip Gerber to board outline polygon. |
| **GerberCombiner** | Merge multiple Gerber or Excellon files into one. |
| **GerberDebugger** | Multi-command diagnostic utility (validate, analyze, visualize, diff, fix). |
| **GerberMover** | Translate, rotate, and transform Gerber/Excellon files. |
| **GerberSanitize** | Clean/normalize Gerber line formatting. |
| **GerberSplitter** | Split Gerber set using polygon outline templates. |
| **GerberSubtract** | Subtract overlapping aperture flashes (experimental). |

### Format Conversion
| Tool | Description |
|------|-------------|
| **GerberToDxf** | Convert Gerber to DXF mechanical CAD format. |
| **GerberToImage** | Render Gerber layers to high-quality PNG images. |
| **GerberToOutline** | Convert Gerber to SVG outline preview. |
| **ImageToGerber** | Convert PNG bitmaps to Gerber files for front panels. |

### PCB Generators
| Tool | Description |
|------|-------------|
| **AntennaBuilder** | Generate complete Gerber + drill files for NFC antenna PCBs. |
| **ProtoBoardGenerator** | Generate custom protoboard/perfboard Gerbers (grid + flower styles). |
| **LightPipeBuilder** | KiCad PCB → OpenSCAD 3D-printable light pipe models. |
| **AutoPanelBuilder** | Automated batch PCB panelization with break-tab insertion. |

### DirtyPCBs Suite
| Tool | Description |
|------|-------------|
| **SickOfBeige** | CLI enclosure/case DXF generator from Gerbers. |
| **BoardStats** | Extract PCB dimensions + drill count as JSON. |
| **BoardRender** | Render PCB preview images with customizable colors. |
| **DXFStats** | Extract trace length + bounding box from DXF files. |
| **Base64Extractor** | Decode Base64 email attachments to ZIP. |
| **LocaleTest** | Debug locale-related Gerber coordinate parsing. |

### Utilities
| Tool | Description |
|------|-------------|
| **BOMConsolidator** | Consolidate JLCPCB-format BOM files. |
| **MakeIcon** | Generate multi-resolution `.ico` files using tiling artwork. |
| **IconScanner** | Batch icon generator from directory/label lists. |
| **ReleaseBuilder** | Collect build outputs into timestamped ZIP archives. |
| **TestGenerator** | Generate synthetic Gerber test files for validation. |
| **MigrationTest** | Smoke test for GerberLibrary → GerberLibrary.Core migration. |

## Core Libraries

| Library | Framework | Description |
|---------|-----------|-------------|
| **GerberLibrary** | .NET 4.8 | Original Gerber/PCB processing (parsing, rendering, panel packing). |
| **GerberLibrary.Core** | .NET 9.0 | Modern cross-platform replacement (ImageSharp, SharpZipLib). |
| **EagleLoaders** | .NET 4.8 | Eagle `.brd`/`.lbr` parser and renderer. |
| **TilingLibrary.Core** | .NET 9.0 | Mathematical tiling engine for geometric artwork generation. |

## Building

```powershell
# Build everything (Debug)
.\build_all.ps1

# Build everything (Release)
.\build_all.ps1 -Config Release

# Build a single project
dotnet build <Project>\<Project>.csproj
```

**Requirements:**
- .NET SDK 8.0+ (for net48 projects)
- .NET SDK 9.0+ (for net9.0 cross-platform projects — optional)
- No Visual Studio required — all projects use SDK-style `.csproj`

**Output:**
- `Build\Output\<AppName>\` — GUI applications with all dependencies
- `Build\Output\CommandLine\` — CLI tools

## Architecture

The solution spans two generations:
- **Legacy:** .NET Framework 4.8, Windows Forms + OpenTK, `GerberLibrary`
- **Modern:** .NET 9.0 cross-platform, Avalonia UI (MVVM), `GerberLibrary.Core`

A migration from legacy WinForms to modern Avalonia is in progress (tracked in `ProjectProgress/`).

## Documentation

- [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) — Complete project reference (English)
- [KNOWLEDGE_BASE_TH.md](KNOWLEDGE_BASE_TH.md) — Complete project reference (Thai)

## License

See the [LICENSE](LICENSE) file for details.

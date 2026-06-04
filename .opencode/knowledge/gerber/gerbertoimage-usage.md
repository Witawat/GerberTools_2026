# GerberToImage Usage

## Overview

CLI tool that renders Gerber RS-274X files to PNG images for visual inspection. Uses `GerberLibrary.Core` with `SixLabors.ImageSharp` for cross-platform rendering.

**Entry point**: `GerberToImage/GerberToImage.cs`

## Usage

```
GerberToImage.exe <files> [options]
```

### Arguments

| Argument | Description |
|----------|-------------|
| `<files>` | One or more files, folders, or ZIP archives |
| `--dpi N` | Output resolution (default: 400) |
| `--noxray` | Disable x-ray mode (show only top layer) |
| `--nopcb` | Disable PCB rendering |
| `--silk <color>` | Silkscreen color (default: white) |
| `--mask <color>` | Solder mask color (default: green) |
| `--trace <color>` | Trace color (default: auto) |
| `--copper <color>` | Copper color (default: gold) |

### Color Values

Any named color or hex: `red`, `blue`, `#FF0000`, `darkgreen`

### Single File Mode

If a single non-ZIP file is provided, renders just that file (white foreground, black background) for quick inspection:
```
GerberToImage.exe board.gbr --dpi 1000
```
Output: `board.gbr_render.png`

### Multi-File Mode

Processes all Gerber files (from files/folders/ZIP) as a board set:
```
GerberToImage.exe ./gerber_folder/ --dpi 600 --silk white --mask green
```
Output: multiple PNG files named after the input files.

### Debug Mode

Set `Gerber.SaveDebugImageOutput = true` in code to generate debug visualization images.

## Code Reference

- `GerberToImage.cs` (170 lines) - Entry point, argument parsing
- `GerberLibrary.Core/Core/ImageCreator.cs` (1788 lines) - `GerberImageCreator` class:
  - `AddBoardsToSet()` - loads files and parses
  - `WriteImageFiles()` - renders to PNG via ImageSharp
  - `ClipBoard()` - clips render to board outline
- `GerberLibrary.Core/Core/ParsedGerber.cs` - parsed shape containers
- `GerberLibrary.Core/Core/GraphicsInterface.cs` - abstract render interface

## Debug Usage

- Render a suspect Gerber file to visually inspect it
- Compare renders before/after sanitization
- Use `--dpi 1000` for high-detail inspection
- Use single-file mode to isolate problematic layers

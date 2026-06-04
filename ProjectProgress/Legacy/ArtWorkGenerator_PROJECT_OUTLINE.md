# TINRS-ArtWorkGenerator Project Outline

## Overview

TINRS-ArtWorkGenerator is a WinForms application (.NET Framework 4.8) for generating geometric artwork patterns based on mathematical tiling systems (Danzer, Penrose, Conway, etc.). It takes a bitmap mask as input and applies various tiling algorithms to create intricate, subdivision-based artwork that can be exported as SVG, Gerber, or bitmap files.

## Project Structure

```
TINRS-ArtWorkGenerator/
├── Program.cs                    # Application entry point
├── TinrsArtWork.cs               # Main form - artwork display and file operations
├── TinrsArtWork.Designer.cs      # Main form designer-generated code
├── SettingsDialog.cs             # Settings panel for configuring artwork generation
├── SettingsDialog.Designer.cs    # Settings dialog designer-generated code
├── Settings.cs                   # (Empty namespace placeholder)
├── SVGWriter.cs                  # Legacy SVG export utility
├── TINRS-ArtWorkGenerator.csproj # Project file (WinForms, .NET 4.8)
└── favicon.ico                   # Application icon
```

## Dependencies

### Internal Projects
- **GerberLibrary** (`../GerberLibrary/GerberLibrary.csproj`)
  - Provides Gerber file export capabilities (`GerberArtWriter`, `PolyLine`)
  - Contains `MathHelpers` for color interpolation
  
- **TINRS-ArtWork (TilingLibrary)** (`../Project_Utilities/TilingLibrary/TINRS-ArtWork.csproj`)
  - Core rendering engine (`TINRSArtWorkRenderer`)
  - Settings class and art modes
  - Tiling definitions (Danzer7Fold, Penrose, Conway, etc.)
  - QuadTree implementation for efficient subdivision
  - Delaunay triangulation support

### External Packages
- **GlmNet** (0.7.0) - Vector/matrix math library

---

## Architecture

### Main Components

#### 1. TinrsArtWork (Main Form)
**File:** `TinrsArtWork.cs` (590 lines)

**Responsibilities:**
- Window management and UI layout
- Mask loading (bitmap files, drag & drop)
- File watching for auto-reload
- Artwork rendering (via PictureBox paint events)
- Export to SVG/Gerber/BMP formats
- Settings save/load (XML serialization)

**Key Fields:**
```csharp
Bitmap Mask;                           // Input mask image
Bitmap Output;                         // Rendered output
SettingsDialog TheSettingsDialog;      // Settings panel instance
TINRSArtWorkRenderer ArtRender;        // Core rendering engine
Settings TheSettings;                  // Current settings
FileSystemWatcher watcher;             // Auto-reload on file change
```

**Key Methods:**
- `LoadMask(string filename)` - Loads mask, rebuilds tree, triggers update
- `UpdateFunc()` - Rebuilds artwork based on current settings
- `ProcessFunc(string file)` - Batch processing callback
- `SaveSVG(string filename)` - SVG export with mode-specific logic
- `CreateCustomMask(MaskType)` - Generates procedural masks

#### 2. SettingsDialog
**File:** `SettingsDialog.cs` (295 lines)

**Responsibilities:**
- UI for all artwork generation parameters
- Real-time update triggering on value changes
- Batch file processing (drag & drop)

**Key Fields:**
```csharp
Settings SettingsTarget;               // Reference to main settings
Action TheCallback;                    // UpdateFunc reference
Action<string> TheProcessCallback;     // ProcessFunc reference
bool UpdateCheckbox.Checked;           // Auto-update toggle
```

**Update Flow:**
```
User Changes Setting → DoUpdate() → TheCallback() → UpdateFunc() → pictureBox1.Invalidate()
```

#### 3. Settings Class (in TilingLibrary)
**File:** `../Project_Utilities/TilingLibrary/Tiling.cs` (lines 13-72)

**Art Modes:**
- `QuadTree` - Pixel-by-pixel quadtree subdivision
- `Tiling` - Geometric tiling with subdivision rules
- `Delaunay` - Delaunay triangulation

**Key Parameters:**
| Parameter            | Type       | Description                            |
| -------------------- | ---------- | -------------------------------------- |
| `Mode`               | ArtMode    | Rendering algorithm selection          |
| `TileType`           | TilingType | Tiling pattern (Danzer, Penrose, etc.) |
| `MaxSubDiv`          | int        | Maximum subdivision depth              |
| `DegreesOff`         | float      | Rotation angle in degrees              |
| `Threshold`          | int        | Mask brightness threshold (%)          |
| `InvertSource`       | bool       | Invert mask interpretation             |
| `InvertOutput`       | bool       | Invert final colors                    |
| `Symmetry`           | bool       | Enable symmetrical tiling              |
| `MarcelPlating`      | bool       | Apply "Marcel" style edge effects      |
| `BallRadius`         | float      | Ball radius for Marcel plating         |
| `Gap`                | float      | Gap size for Marcel plating            |
| `scalesmallerfactor` | float      | Global scale adjustment                |

#### 4. TINRSArtWorkRenderer (in TilingLibrary)
**File:** `../Project_Utilities/TilingLibrary/TINRSArtWorkRenderer.cs` (844 lines)

**Responsibilities:**
- Core rendering logic for all art modes
- QuadTree management
- Tiling subdivision
- Delaunay triangulation

**Key Methods:**
- `BuildTree(Bitmap, Settings)` - Builds mask quadtree from bitmap pixels
- `BuildStuff(Bitmap, Settings)` - Main rendering routine (~250 lines)
- `DrawTiling(Settings, Bitmap, Graphics, ...)` - Renders to Graphics context
- `DrawIcon(...)` / `SaveMultiIcon(...)` - Icon generation utilities

**Expensive Operations in `BuildStuff()`:**
1. Pixel-by-pixel iteration over entire mask
2. QuadTree insertion for matching pixels
3. Tiling subdivision (recursive)
4. Marcel plating polygon clipping
5. Distance-to-mask calculations (40-point radial sampling per polygon)

---

## Rendering Pipeline

```
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│   Load Mask     │──────│   BuildTree     │──────│   BuildStuff    │
│ (Bitmap file)   │      │ (QuadTree from  │      │ (Generate       │
│                 │      │  mask pixels)   │      │  geometry)      │
└─────────────────┘      └─────────────────┘      └─────────────────┘
                                                          │
                         ┌────────────────────────────────┘
                         ▼
       ┌─────────────────────────────────────────────────────────────┐
       │                     BuildStuff Logic                        │
       ├─────────────────────────────────────────────────────────────┤
       │ QuadTree Mode:                                              │
       │   - Iterate all pixels                                      │
       │   - Insert into rotated quadtree based on brightness        │
       ├─────────────────────────────────────────────────────────────┤
       │ Delaunay Mode:                                              │
       │   - Same + Delaunay.Build(ArtTree)                          │
       ├─────────────────────────────────────────────────────────────┤
       │ Tiling Mode:                                                │
       │   - Create base triangle from TilingDefinition              │
       │   - SubdivideAdaptive based on mask                         │
       │   - Apply symmetry transforms                               │
       │   - Apply scaling modifiers                                 │
       │   - Optional: Marcel plating with Clipper                   │
       └─────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
                    ┌─────────────────────────────┐
                    │      pictureBox1.Paint()    │
                    │      DrawTiling() →         │
                    │      Graphics output        │
                    └─────────────────────────────┘
```

---

## Supported Tiling Types

| Type                  | Description                                           |
| --------------------- | ----------------------------------------------------- |
| `Danzer7Fold`         | 7-fold rotational symmetry tiling                     |
| `Danzer7FoldOriginal` | Original Danzer 7-fold variant                        |
| `Maloney`             | Maloney aperiodic tiling                              |
| `Conway`              | Conway pinwheel tiling                                |
| `Walton`              | Dale Walton tiling                                    |
| `Penrose`             | Penrose P2 tiling (golden ratio)                      |
| `RegularTriangle`     | Simple triangular grid (4-subdivision)                |
| `SameSameDifferent`   | Experimental mixed tiling                             |
| `TriangleMultiscale`  | Multi-scale triangular                                |
| `SVG14Fold`           | SVG-defined 14-fold pattern                           |
| `HexaTest`            | Hexagonal test pattern                                |
| **`Pinwheel`**        | Conway/Radin 1-2-√5 triangle (infinite rotations)     |
| **`Sphinx`**          | Rep-4 sphinx hexiamond tile                           |
| **`AmmannBeenker`**   | Octagonal quasicrystal (8-fold symmetry, silver mean) |
| **`HalfHex`**         | Half-hexagon with 3-subdivision                       |
| **`Chair`**           | L-shaped rep-4 chair tile                             |

---

## Export Formats

1. **SVG** - Vector graphics with stroke paths per polygon
2. **Gerber** - PCB artwork format via GerberArtWriter
3. **Bitmap** - Direct PNG/BMP export of rendered output
4. **Multi-level SVG** - Separate SVG per subdivision level

---

## Current Performance Issue

### Problem
When "Auto Update" is enabled in the settings dialog, every slider/checkbox change triggers a full re-render on the UI thread, causing:
- UI freezes during computation
- No feedback during long operations
- Unable to cancel long-running operations
- No progressive rendering feedback

### Root Cause
```csharp
// SettingsDialog.cs
void DoUpdate() {
    if (UpdateCheckbox.Checked) TheCallback();  // Calls UpdateFunc synchronously
}

// TinrsArtWork.cs
public void UpdateFunc() {
    pictureBox1.Invalidate();
    ArtRender.BuildStuff(Mask, TheSettings);  // BLOCKING - can take seconds
}
```

### Affected Code Paths
| File                      | Method                        | Issue                             |
| ------------------------- | ----------------------------- | --------------------------------- |
| `SettingsDialog.cs`       | All `*_ValueChanged` handlers | Call `DoUpdate()` synchronously   |
| `TinrsArtWork.cs`         | `UpdateFunc()`                | Calls `BuildStuff()` on UI thread |
| `TINRSArtWorkRenderer.cs` | `BuildStuff()`                | CPU-intensive, no cancellation    |

---

## Technology Stack

- **Framework:** .NET Framework 4.8
- **UI:** Windows Forms
- **Graphics:** System.Drawing, System.Drawing.Drawing2D
- **Math:** GlmNet (vec2, vec3)
- **Polygon Clipping:** ClipperLib (for Marcel plating)
- **Serialization:** System.Xml.Serialization

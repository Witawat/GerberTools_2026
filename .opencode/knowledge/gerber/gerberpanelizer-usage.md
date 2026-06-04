# GerberPanelizer Usage

## Overview

Interactive WinForms GUI application for PCB panelization. Uses OpenGL for real-time rendering of board instances on a panel.

**Entry point**: `GerberPanelizer/Program.cs` -> `GerberPanelizerParent` (MDI parent form)

## Basic Workflow

1. **Launch**: Run `GerberPanelizer.exe`
2. **Add board**: File > New, then drag-drop a folder of Gerber files onto the viewport
3. **Panel setup**: Configure panel dimensions, margins (Panel Properties)
4. **Arrange**: Drag instances, use auto-pack (MaxRectPacker), or manual placement
5. **Tabs**: Generate break tabs (mousebites) via auto-tab (Delaunay triangulation)
6. **V-score**: Add V-cut scoring lines if needed
7. **Export**: Export merged Gerber files for manufacturing

## Key Forms & Classes

| Form/Class | File | Purpose |
|------------|------|---------|
| `GerberPanelizerParent` | `GerberPanelizerParent.cs` | MDI parent, menu bar, tree panel, instance dialog panels |
| `GerberPanelize` | `GerberPanelize.cs` (1680 lines) | Main child form: OpenGL viewport, mouse interaction, undo/redo, selection, rendering |
| `Treeview` | `Treeview.cs` | Instance tree listing all boards on the panel |
| `InstanceDialog` | `InstanceDialog.cs` | Properties editor for selected instance (position, rotation) |
| `PanelProperties` | `PanelProperties.cs` | Panel dimensions, margins, fill settings |
| `AutofitDialog` | `AutofitDialog.cs` | Auto-pack configuration |
| `GLGraphicsInterface` | `GLGraphicsInterface.cs` | OpenGL rendering engine |

## GerberPanel Core Engine (GerberLibrary.Core)

The panelization logic lives in `GerberLibrary.Core/Core/GerberPanel.cs` (2623 lines):

| Method | Description |
|--------|-------------|
| `AddGerberFolder()` | Loads all Gerber files from a folder, detects board outline |
| `AddGerberZip()` | Loads from ZIP archive |
| `MaxRectPack()` | Auto-packs instances using Maximal Rectangles algorithm |
| `BuildAutoTabs()` | Creates break tabs using Delaunay triangulation of instance centers |
| `AddTab()` | Adds a mousebite tab at a position |
| `UpdateShape()` | Rebuilds panel geometry (positive/negative polygons) |
| `SaveFile()` | Saves panel set file (.gerberset) |
| `SaveOutlineTo()` | Saves outline Gerber for verification |

## Instance Types

- **GerberInstance**: A board loaded from a Gerber folder
- **AngledThing**: Base class with position, rotation, bounding box
- **NegativePolygonInstance**: The scrap/fill area around boards

## Debug Usage

- Use `GerberPanelizer` to visually inspect boards before panelizing
- Drag-drop a single board folder to verify it loads correctly
- Check outline rendering (white/colored boundary) to confirm outline detection
- Use zoom (mouse wheel) and pan (middle mouse) to inspect details
- The treeview shows all instances with their layer breakdown

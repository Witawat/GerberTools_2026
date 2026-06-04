# TiNRS-Tiler

A modern, cross-platform GUI application for generating geometric artwork patterns based on mathematical tiling systems. This is a complete rewrite of the TINRS-ArtWorkGenerator using Avalonia UI for true cross-platform support.

## Features

- **Cross-Platform**: Runs on Windows, macOS, and Linux
- **Modern UI**: Built with Avalonia UI using MVVM architecture
- **Multiple Tiling Systems**: Support for Danzer, Penrose, Conway, Sphinx, Ammann-Beenker, and more
- **Real-time Preview**: Live updates as you adjust parameters
- **Background Rendering**: Non-blocking UI with progress reporting
- **Multiple Export Formats**: SVG, Gerber, PNG, and BMP
- **Mask-based Generation**: Use bitmap masks to control tiling density and patterns

## Supported Tiling Types

- **Danzer 7-Fold**: 7-fold rotational symmetry tiling
- **Penrose**: Classic Penrose P2 tiling with golden ratio
- **Conway Pinwheel**: 1-2-√5 triangle with infinite rotations
- **Sphinx**: Rep-4 sphinx hexiamond tile
- **Ammann-Beenker**: Octagonal quasicrystal with 8-fold symmetry
- **Chair**: L-shaped rep-4 chair tile
- **Half-Hex**: Half-hexagon with 3-subdivision
- And many more...

## Architecture

### Technology Stack

- **.NET 9.0**: Modern .NET runtime
- **Avalonia UI 11.3**: Cross-platform XAML-based UI framework
- **MVVM Pattern**: Clean separation of concerns using CommunityToolkit.Mvvm
- **SkiaSharp**: High-performance 2D graphics rendering
- **Reactive UI**: Responsive, reactive user interface

### Project Structure

```
TiNRS-Tiler/
├── Models/              # Data models
├── ViewModels/          # MVVM view models
├── Views/               # XAML views
├── Services/            # Business logic services
├── Controls/            # Custom UI controls
└── Assets/              # Images, icons, and resources
```

### Key Components

#### MainViewModel
The primary view model that manages:
- Mask loading and file watching
- Settings management
- Rendering coordination
- Export operations

#### RenderService
Background rendering service that:
- Executes tiling algorithms on background threads
- Reports progress updates
- Supports cancellation
- Provides progressive rendering

#### SettingsViewModel
Manages all artwork generation parameters:
- Tiling type selection
- Subdivision depth
- Rotation and scaling
- Color and style options
- Marcel plating effects

## Building

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022, VS Code, or Rider (optional)

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run

# Publish for distribution
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r osx-x64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained
```

## Usage

1. **Load a Mask**: Click "Open Mask" or drag-and-drop an image file
2. **Select Tiling Type**: Choose from the dropdown menu
3. **Adjust Parameters**: Use sliders and checkboxes to customize
4. **Watch Live Preview**: The artwork updates automatically
5. **Export**: Save as SVG, Gerber, or bitmap format

## Dependencies

### Internal Libraries
- **GerberLibrary**: Gerber file export and polygon operations
- **TINRS-ArtWork (TilingLibrary)**: Core tiling algorithms and rendering engine

### External Packages
- **Avalonia**: Cross-platform UI framework
- **CommunityToolkit.Mvvm**: MVVM helpers and base classes
- **SkiaSharp**: 2D graphics rendering
- **GlmNet**: Vector and matrix mathematics

## Differences from Original

### Improvements
- ✅ **True Cross-Platform**: Works on Windows, macOS, and Linux
- ✅ **Modern UI**: Clean, responsive interface with Fluent design
- ✅ **Better Performance**: Optimized rendering pipeline
- ✅ **Non-blocking UI**: All rendering happens on background threads
- ✅ **Better UX**: Progress indicators, cancellation support
- ✅ **Maintainable Code**: MVVM architecture with clear separation

### Migration Notes
- Uses Avalonia instead of WinForms
- SkiaSharp for rendering instead of System.Drawing
- Async/await patterns throughout
- Reactive property bindings
- Modern C# features (nullable reference types, records, etc.)

## License

Same license as the parent GerberTools project.

## Contributing

This is part of the GerberTools suite. See the main repository for contribution guidelines.

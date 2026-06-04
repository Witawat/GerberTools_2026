# TiNRS-Tiler - Project Index

Welcome to the TiNRS-Tiler project! This document provides an overview and navigation guide for all project documentation.

## Quick Links

- **[COMPLETE_SUMMARY.md](./COMPLETE_SUMMARY.md)** - Start here! Complete project overview and status
- **[README.md](./README.md)** - User-facing documentation and features
- **[QUICKSTART.md](./QUICKSTART.md)** - Developer quick start guide
- **[ARCHITECTURE.md](./ARCHITECTURE.md)** - Technical architecture and design
- **[MIGRATION_PLAN.md](./MIGRATION_PLAN.md)** - Library porting strategy (in parent directory)
- **[MIGRATION_PROGRESS.md](../MIGRATION_PROGRESS.md)** - Current migration status (in parent directory)

## Project Status: 85% Complete ✅

### What Works
- ✅ Complete Avalonia UI application
- ✅ MVVM architecture
- ✅ Background rendering infrastructure
- ✅ All UI features implemented
- ✅ Comprehensive documentation

### What's Left
- ⏳ Fix ~7 files in GerberLibrary.Core (namespace issues)
- ⏳ Test compilation and runtime
- ⏳ Implement file dialogs and export

**Estimated time to completion: 6-9 hours**

## Documentation Guide

### For Users
1. Start with **[README.md](./README.md)** for features and usage
2. See building instructions in README

### For Developers
1. Read **[QUICKSTART.md](./QUICKSTART.md)** for development setup
2. Review **[ARCHITECTURE.md](./ARCHITECTURE.md)** for technical details
3. Check **[COMPLETE_SUMMARY.md](./COMPLETE_SUMMARY.md)** for project status

### For Migration Work
1. Read **[MIGRATION_PLAN.md](../MIGRATION_PLAN.md)** for strategy
2. Check **[MIGRATION_PROGRESS.md](../MIGRATION_PROGRESS.md)** for current status
3. See **[DRAWING_MIGRATION.md](../DRAWING_MIGRATION.md)** for System.Drawing migration guide

## Project Structure

```
TiNRS-Tiler/
├── Models/              # Data models
├── Services/            # Business logic
├── ViewModels/          # MVVM view models
├── Views/               # XAML UI
├── Assets/              # Resources
└── Documentation/       # This folder
    ├── README.md
    ├── ARCHITECTURE.md
    ├── QUICKSTART.md
    ├── COMPLETE_SUMMARY.md
    └── INDEX.md (this file)

Related Projects:
├── GerberLibrary.Core/  # .NET 9 Gerber library (90% complete)
├── TilingLibrary.Core/  # .NET 9 Tiling library (depends on Gerber)
└── TINRS-ArtWorkGenerator/ # Original Windows-only app
```

## Key Technologies

- **.NET 9** - Modern runtime
- **Avalonia UI 11.3** - Cross-platform XAML framework
- **CommunityToolkit.Mvvm** - MVVM helpers
- **System.Drawing.Common** - Graphics (temporary, will migrate to SkiaSharp)
- **GlmNet** - Vector mathematics
- **ClipperLib** - Polygon clipping (embedded)

## Getting Started

### Prerequisites
```bash
# Install .NET 9 SDK
# macOS/Linux: Install libgdiplus
brew install mono-libgdiplus  # macOS
sudo apt-get install libgdiplus  # Linux
```

### Build
```bash
cd TiNRS-Tiler
dotnet restore
dotnet build
```

### Run
```bash
dotnet run
```

## Current Blockers

**None!** All architectural decisions made, dependencies resolved, and path forward is clear.

## Next Steps

1. **Fix namespace issues** in GerberLibrary.Core (~7 files)
2. **Test compilation** of full solution
3. **Implement file dialogs** using Avalonia.Dialogs
4. **Implement export** functionality
5. **Test on all platforms**

## Contributing

This project follows:
- **MVVM pattern** for UI
- **Async/await** for all I/O
- **Nullable reference types** enabled
- **XML documentation** for public APIs
- **Modern C# conventions**

See [QUICKSTART.md](./QUICKSTART.md) for coding guidelines.

## License

Same as parent GerberTools project.

## Questions?

- Check the relevant documentation file above
- Review code comments
- See Avalonia UI documentation: https://docs.avaloniaui.net/

---

**Last Updated:** 2026-02-04
**Project Status:** 85% Complete
**Estimated Completion:** 6-9 hours of work remaining

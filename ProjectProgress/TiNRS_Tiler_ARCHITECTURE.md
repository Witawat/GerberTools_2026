# TiNRS-Tiler Architecture

## Application Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         TiNRS-Tiler                             │
│                    (Avalonia UI Application)                    │
└─────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┴───────────────┐
                │                               │
        ┌───────▼────────┐             ┌────────▼────────┐
        │     Views      │             │   ViewModels    │
        │   (XAML UI)    │◄────────────│   (MVVM Logic)  │
        └────────────────┘             └────────┬────────┘
                                                │
                                        ┌───────▼────────┐
                                        │     Models     │
                                        │   (Data DTOs)  │
                                        └───────┬────────┘
                                                │
                                        ┌───────▼────────┐
                                        │    Services    │
                                        │ (Business Logic)│
                                        └───────┬────────┘
                                                │
                ┌───────────────────────────────┼───────────────────┐
                │                               │                   │
        ┌───────▼────────┐            ┌────────▼────────┐  ┌──────▼──────┐
        │ TilingLibrary  │            │ GerberLibrary   │  │  SkiaSharp  │
        │ (Tiling Algs)  │            │ (Gerber Export) │  │  (Graphics) │
        └────────────────┘            └─────────────────┘  └─────────────┘
```

## MVVM Data Flow

```
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│     View     │         │  ViewModel   │         │    Model     │
│  (MainWindow)│         │ (MainViewModel)│        │(TilerSettings)│
└──────┬───────┘         └──────┬───────┘         └──────┬───────┘
       │                        │                        │
       │  User Interaction      │                        │
       │  (Slider Change)       │                        │
       ├───────────────────────►│                        │
       │                        │                        │
       │                        │  Update Property       │
       │                        ├───────────────────────►│
       │                        │                        │
       │                        │  PropertyChanged       │
       │                        │◄───────────────────────┤
       │                        │                        │
       │  PropertyChanged       │                        │
       │◄───────────────────────┤                        │
       │                        │                        │
       │  UI Updates            │                        │
       │  Automatically         │                        │
       └────────────────────────┘                        │
                                                         │
```

## Rendering Pipeline

```
┌─────────────────────────────────────────────────────────────────┐
│                      User Changes Setting                       │
└────────────────────────────┬────────────────────────────────────┘
                             │
                    ┌────────▼────────┐
                    │  Auto Update?   │
                    └────────┬────────┘
                             │ Yes
                    ┌────────▼────────┐
                    │ Debounce Timer  │
                    │    (100ms)      │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │  QueueRender()  │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │  RenderAsync()  │
                    └────────┬────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
┌───────▼────────┐  ┌────────▼────────┐  ┌───────▼────────┐
│ Cancel Previous│  │  Clone Mask     │  │ Create Progress│
│    Render      │  │ (Thread Safety) │  │    Reporter    │
└───────┬────────┘  └────────┬────────┘  └───────┬────────┘
        │                    │                    │
        └────────────────────┼────────────────────┘
                             │
                    ┌────────▼────────┐
                    │  RenderService  │
                    │.RenderWithAuto  │
                    │   CancelAsync() │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │  Task.Run(() => │
                    │   Background    │
                    │    Thread)      │
                    └────────┬────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
┌───────▼────────┐  ┌────────▼────────┐  ┌───────▼────────┐
│   BuildTree()  │  │  BuildStuff()   │  │Report Progress │
│ (QuadTree from │  │ (Generate Tiling│  │   (0-100%)     │
│     Mask)      │  │   Geometry)     │  │                │
└───────┬────────┘  └────────┬────────┘  └───────┬────────┘
        │                    │                    │
        └────────────────────┼────────────────────┘
                             │
                    ┌────────▼────────┐
                    │  DrawTiling()   │
                    │ (Render to      │
                    │   Bitmap)       │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │ Convert to      │
                    │ Avalonia.Bitmap │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │ Update Preview  │
                    │  (UI Thread)    │
                    └─────────────────┘
```

## Threading Model

```
┌─────────────────────────────────────────────────────────────────┐
│                          UI Thread                              │
│  - User interactions                                            │
│  - Property change notifications                                │
│  - UI updates                                                   │
│  - Progress updates (via Dispatcher)                            │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             │ Task.Run()
                             │
┌────────────────────────────▼────────────────────────────────────┐
│                      Background Thread                          │
│  - BuildTree() - QuadTree construction                          │
│  - BuildStuff() - Tiling generation                             │
│  - SubdivideAdaptive() - Recursive subdivision                  │
│  - Polygon clipping (Marcel plating)                            │
│  - Distance calculations                                        │
│                                                                 │
│  Cancellation Checks:                                           │
│  - At each column during pixel iteration                        │
│  - At each subdivision level                                    │
│  - Before expensive operations                                  │
│                                                                 │
│  Progress Reports:                                              │
│  - Every 5% during pixel processing                             │
│  - At each major stage                                          │
│  - With intermediate results                                    │
└─────────────────────────────────────────────────────────────────┘
```

## Class Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        MainViewModel                            │
├─────────────────────────────────────────────────────────────────┤
│ - _renderService: RenderService                                 │
│ - _debounceTimer: Timer                                         │
│ - _currentMask: Bitmap                                          │
│ - _fileWatcher: FileSystemWatcher                               │
│ + Settings: TilerSettings                                       │
│ + PreviewImage: Avalonia.Bitmap                                 │
│ + ProgressPercent: int                                          │
│ + ProgressStage: string                                         │
│ + IsRendering: bool                                             │
│ + StatusMessage: string                                         │
├─────────────────────────────────────────────────────────────────┤
│ + OpenMaskCommand: ICommand                                     │
│ + SaveSvgCommand: ICommand                                      │
│ + SaveGerberCommand: ICommand                                   │
│ + SaveImageCommand: ICommand                                    │
│ + CancelRenderCommand: ICommand                                 │
│ + TriggerUpdateCommand: ICommand                                │
├─────────────────────────────────────────────────────────────────┤
│ + LoadMaskAsync(filePath): Task                                 │
│ + RenderAsync(): Task                                           │
│ + QueueRender(): void                                           │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         │ uses
                         │
┌────────────────────────▼────────────────────────────────────────┐
│                       RenderService                             │
├─────────────────────────────────────────────────────────────────┤
│ - _renderer: TINRSArtWorkRenderer                               │
│ - _currentCts: CancellationTokenSource                          │
├─────────────────────────────────────────────────────────────────┤
│ + RenderAsync(mask, settings, progress, ct): Task<RenderResult>│
│ + RenderWithAutoCancelAsync(mask, settings, progress):         │
│     Task<RenderResult>                                          │
│ + CancelCurrentRender(): void                                   │
│ + GetRenderer(): TINRSArtWorkRenderer                           │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         │ uses
                         │
┌────────────────────────▼────────────────────────────────────────┐
│                     TilerSettings                               │
├─────────────────────────────────────────────────────────────────┤
│ + CoreSettings: Settings                                        │
│ + MaskFilePath: string                                          │
│ + AutoReloadMask: bool                                          │
│ + AutoUpdate: bool                                              │
│ + OutputWidth: int                                              │
│ + OutputHeight: int                                             │
│ + BackgroundColor: uint                                         │
│ + ForegroundColor: uint                                         │
│ + StrokeWidth: float                                            │
│ + FillPolygons: bool                                            │
├─────────────────────────────────────────────────────────────────┤
│ + Clone(): TilerSettings                                        │
└─────────────────────────────────────────────────────────────────┘
```

## File Organization

```
TiNRS-Tiler/
│
├── Models/                      # Data models and DTOs
│   ├── TilerSettings.cs         # Wraps TilingLibrary.Settings
│   └── RenderProgressInfo.cs    # Progress tracking
│
├── Services/                    # Business logic layer
│   └── RenderService.cs         # Background rendering orchestration
│
├── ViewModels/                  # MVVM view models
│   ├── ViewModelBase.cs         # Base class with IDisposable
│   └── MainViewModel.cs         # Main application logic
│
├── Views/                       # XAML UI definitions
│   ├── MainWindow.axaml         # Main window layout
│   └── MainWindow.axaml.cs      # Code-behind (minimal)
│
├── Assets/                      # Resources
│   └── avalonia-logo.ico        # Application icon
│
├── App.axaml                    # Application resources
├── App.axaml.cs                 # Application startup
├── Program.cs                   # Entry point
├── ViewLocator.cs               # View/ViewModel mapping
│
└── Documentation/
    ├── README.md                # User documentation
    ├── MIGRATION_PLAN.md        # Technical migration guide
    ├── PROJECT_SUMMARY.md       # Project overview
    └── ARCHITECTURE.md          # This file
```

## Key Design Decisions

### 1. MVVM Pattern
**Why:** Clean separation of concerns, testability, maintainability
- Views are pure XAML with minimal code-behind
- ViewModels contain all UI logic
- Models are simple data containers
- Commands for all user actions

### 2. Background Rendering
**Why:** Responsive UI, better UX
- All rendering happens on background threads
- Progress reporting keeps user informed
- Cancellation prevents wasted work
- Debouncing prevents rapid re-renders

### 3. Avalonia UI
**Why:** True cross-platform support
- Works on Windows, macOS, Linux
- Modern XAML-based UI
- Good performance
- Active development

### 4. SkiaSharp for Graphics
**Why:** Cross-platform, high performance
- Used by Avalonia internally
- Similar API to System.Drawing
- GPU acceleration support
- Excellent documentation

### 5. Dependency Injection Ready
**Why:** Future extensibility
- Services are loosely coupled
- Easy to add DI container later
- Testable architecture

## Performance Characteristics

### Memory Usage
- Mask bitmap: ~Width × Height × 4 bytes
- QuadTree: ~O(n) where n = matching pixels
- Polygon list: ~O(subdivisions)
- Preview bitmap: ~OutputWidth × OutputHeight × 4 bytes

### CPU Usage
- BuildTree: O(Width × Height) - pixel iteration
- BuildStuff: O(subdivisions × complexity)
- Drawing: O(polygons)

### Optimizations
- Bitmap cloning for thread safety (small overhead)
- Debouncing reduces unnecessary renders
- Cancellation stops wasted work
- Progressive rendering shows intermediate results

## Extension Points

### Adding New Tiling Types
1. Add to `TilingType` enum in TilingLibrary
2. Implement subdivision rules
3. UI automatically updates (enum binding)

### Adding New Export Formats
1. Add command to MainViewModel
2. Implement export logic in service
3. Add menu item in MainWindow.axaml

### Adding New UI Features
1. Add properties to MainViewModel
2. Add UI elements to MainWindow.axaml
3. Bind with `{Binding PropertyName}`

## Testing Strategy

### Unit Tests
- Models: Serialization, cloning
- Services: Rendering, cancellation
- ViewModels: Commands, property changes

### Integration Tests
- Full render pipeline
- Export functionality
- File operations

### UI Tests
- Avalonia.Headless for automated UI testing
- Screenshot comparisons
- Interaction testing

## Deployment

### Windows
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### macOS
```bash
dotnet publish -c Release -r osx-x64 --self-contained
```

### Linux
```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

### App Bundles
- Windows: Create installer with WiX or Inno Setup
- macOS: Create .app bundle with icon
- Linux: Create .deb or .rpm package

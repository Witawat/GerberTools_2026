# TiNRS-Tiler Quick Start Guide

## For Developers

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022, VS Code, or JetBrains Rider
- Git

### Getting Started

#### 1. Clone and Navigate
```bash
cd /Users/admin/Projects/GerberTools/TiNRS-Tiler
```

#### 2. Current Status
⚠️ **The project does not build yet** because it depends on .NET Framework 4.8 libraries (GerberLibrary and TilingLibrary) which are Windows-only.

**Next step required:** Port the libraries to .NET 9 (see MIGRATION_PLAN.md)

#### 3. Project Structure
```
TiNRS-Tiler/
├── Models/           # Data models
├── Services/         # Business logic
├── ViewModels/       # MVVM view models
├── Views/            # XAML UI
└── Assets/           # Resources
```

### Key Files

| File | Purpose |
|------|---------|
| `MainViewModel.cs` | Main application logic |
| `MainWindow.axaml` | UI layout |
| `RenderService.cs` | Background rendering |
| `TilerSettings.cs` | Settings model |

### Common Tasks

#### Adding a New Setting

1. **Add to TilerSettings.cs**
```csharp
[ObservableProperty]
private bool _myNewSetting = true;
```

2. **Add to MainWindow.axaml**
```xml
<CheckBox Content="My New Setting" 
          IsChecked="{Binding Settings.MyNewSetting}" />
```

3. **Use in rendering**
```csharp
if (Settings.MyNewSetting)
{
    // Do something
}
```

#### Adding a New Command

1. **Add to MainViewModel.cs**
```csharp
[RelayCommand]
private async Task MyNewCommandAsync()
{
    // Implementation
}
```

2. **Add to MainWindow.axaml**
```xml
<MenuItem Header="My Action" 
          Command="{Binding MyNewCommandCommand}" />
```

#### Triggering a Render

```csharp
// From anywhere in MainViewModel
QueueRender(); // Debounced
// or
await RenderAsync(); // Immediate
```

### MVVM Pattern

#### Property Binding
```xml
<!-- Two-way binding -->
<Slider Value="{Binding Settings.MaxSubDiv}" />

<!-- One-way binding -->
<TextBlock Text="{Binding StatusMessage}" />

<!-- Command binding -->
<Button Command="{Binding OpenMaskCommand}" />
```

#### Observable Properties
```csharp
// Using CommunityToolkit.Mvvm
[ObservableProperty]
private string _statusMessage = "Ready";

// Generates:
// - private string _statusMessage
// - public string StatusMessage { get; set; }
// - OnPropertyChanged("StatusMessage")
```

#### Commands
```csharp
// Using CommunityToolkit.Mvvm
[RelayCommand]
private async Task SaveAsync()
{
    // Implementation
}

// Generates:
// - public IAsyncRelayCommand SaveCommand { get; }
```

### Debugging

#### Run the Application
```bash
dotnet run
```

#### Debug in VS Code
1. Open folder in VS Code
2. Press F5
3. Select ".NET Core Launch"

#### Debug in Visual Studio
1. Open TiNRS.Tiler.csproj
2. Press F5

#### Enable Avalonia DevTools
Already enabled in Debug builds. Press F12 in the running app to open DevTools.

### Testing

#### Manual Testing Checklist
- [ ] Load a mask image
- [ ] Change tiling type
- [ ] Adjust subdivision depth
- [ ] Enable/disable auto-update
- [ ] Cancel a long render
- [ ] Export to SVG
- [ ] Export to Gerber
- [ ] Export to image

#### Unit Testing (Future)
```bash
dotnet test
```

### Building for Release

#### Windows
```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

#### macOS
```bash
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
```

#### Linux
```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

### Troubleshooting

#### "The reference assemblies for .NETFramework,Version=v4.8 were not found"
**Solution:** Port GerberLibrary and TilingLibrary to .NET 9 (see MIGRATION_PLAN.md)

#### "Cannot find type 'MainViewModel'"
**Solution:** Build the project first. The MVVM toolkit generates code at build time.

#### UI not updating
**Solution:** Ensure properties use `[ObservableProperty]` or call `OnPropertyChanged()`

#### Render not triggering
**Solution:** Check that `Settings.AutoUpdate` is true, or call `TriggerUpdate()`

### Code Style

#### Naming Conventions
- Classes: `PascalCase`
- Methods: `PascalCase`
- Properties: `PascalCase`
- Fields: `_camelCase` (private)
- Constants: `UPPER_CASE`

#### Async Methods
- Always suffix with `Async`
- Always return `Task` or `Task<T>`
- Use `await` instead of `.Result` or `.Wait()`

#### Disposal
- Implement `IDisposable` for classes with unmanaged resources
- Call `Dispose()` in ViewModelBase
- Use `using` statements where appropriate

### Performance Tips

#### Debouncing
Already implemented for settings changes (100ms delay)

#### Background Threading
All rendering happens on background threads automatically

#### Memory Management
- Bitmaps are cloned for thread safety
- Dispose of old bitmaps when loading new ones
- Use `using` statements for Graphics objects

### Resources

#### Documentation
- [Avalonia UI Docs](https://docs.avaloniaui.net/)
- [CommunityToolkit.Mvvm Docs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [SkiaSharp Docs](https://learn.microsoft.com/en-us/dotnet/api/skiasharp)

#### Project Docs
- `README.md` - User documentation
- `ARCHITECTURE.md` - Technical architecture
- `MIGRATION_PLAN.md` - Library porting guide
- `PROJECT_SUMMARY.md` - Project overview

### Git Workflow

#### Commit Messages
```
feat: Add new tiling type
fix: Correct rendering bug
docs: Update README
refactor: Simplify RenderService
test: Add unit tests for settings
```

#### Branching
```bash
# Feature branch
git checkout -b feature/my-feature

# Bug fix
git checkout -b fix/my-bug

# Merge back
git checkout main
git merge feature/my-feature
```

### Next Steps

1. **Port Libraries** (see MIGRATION_PLAN.md)
   - GerberLibrary to .NET 9
   - TilingLibrary to .NET 9

2. **Implement File Dialogs**
   - Use Avalonia.Dialogs
   - Add to MainViewModel

3. **Implement Export**
   - SVG export
   - Gerber export
   - Image export

4. **Add Features**
   - Drag-and-drop
   - Zoom/pan
   - Keyboard shortcuts
   - Recent files

5. **Polish**
   - Icons
   - Themes
   - Localization
   - Help system

### Getting Help

- Check existing documentation
- Review code comments
- Look at Avalonia samples
- Ask in project discussions

### Contributing

1. Create a feature branch
2. Make your changes
3. Test thoroughly
4. Update documentation
5. Submit a pull request

---

**Happy coding! 🚀**

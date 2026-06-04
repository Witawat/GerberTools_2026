using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TiNRS.Tiler.Models;
using TiNRS.Tiler.Services;
using TilingLibrary;
using Artwork;
using System.Text;
using System.Globalization;
using Avalonia.Platform.Storage;

namespace TiNRS.Tiler.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly RenderService _renderService;
    private readonly Timer _debounceTimer;
    private const int DEBOUNCE_MS = 100;
    private Image<Rgba32>? _currentMask;
    private FileSystemWatcher? _fileWatcher;
    private TINRSArtWorkRenderer? _lastRenderer;

    // Delegates for file dialogs - assigned by the View
    public Func<Task<IStorageFile?>>? OpenFileDelegate { get; set; }
    public Func<Task<IStorageFile?>>? OpenDistanceMaskDelegate { get; set; }
    public Func<string, string, string, Task<IStorageFile?>>? SaveFileDelegate { get; set; }


    [ObservableProperty]
    private TilerSettings _settings = new();

    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap? _previewImage;

    [ObservableProperty]
    private int _progressPercent;

    [ObservableProperty]
    private string _progressStage = "Ready";

    [ObservableProperty]
    private bool _isRendering;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _windowTitle = "TiNRS-Tiler";

    [ObservableProperty]
    private bool _hasPreview;

    // Tiling type options for the UI
    public List<TilingTypeOption> TilingTypes { get; } = Enum.GetValues<Artwork.Tiling.TilingType>()
        .Select(t => new TilingTypeOption { Type = t, DisplayName = t.ToString() })
        .ToList();

    // Art mode options for the UI
    public List<ArtModeOption> ArtModes { get; } = Enum.GetValues<Artwork.Settings.ArtMode>()
        .Select(m => new ArtModeOption { Mode = m, DisplayName = m.ToString() })
        .ToList();

    // Scale mode options for the UI
    public List<ScaleModeOption> ScalingModes { get; } = Enum.GetValues<Artwork.Settings.TriangleScaleMode>()
        .Select(m => new ScaleModeOption { Mode = m, DisplayName = m.ToString() })
        .ToList();

    public MainViewModel()
    {
        _renderService = new RenderService();
        _debounceTimer = new Timer(DEBOUNCE_MS);
        _debounceTimer.Elapsed += OnDebounceTimerElapsed;
        _debounceTimer.AutoReset = false;

        _settings.PropertyChanged += Settings_PropertyChanged;

        // Create a default circular mask
        CreateDefaultMask();
    }

    private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (Settings.AutoUpdate)
        {
            QueueRender();
        }
    }

    [RelayCommand]
    private async Task OpenMaskAsync()
    {
        if (OpenFileDelegate == null) return;

        var file = await OpenFileDelegate();
        if (file != null)
        {
            if (file.TryGetLocalPath() is string path)
            {
                await LoadMaskAsync(path);
            }
            else
            {
                StatusMessage = "Failed to get local path for mask file.";
            }
        }
    }

    [RelayCommand]
    private async Task OpenDistanceMaskAsync()
    {
        if (OpenDistanceMaskDelegate == null) return;

        var file = await OpenDistanceMaskDelegate();
        if (file != null)
        {
            try 
            {
                // Get the path from the file (local file system)
                if (file.TryGetLocalPath() is string path)
                {
                    Settings.DistanceMaskFile = path;
                    StatusMessage = $"Loaded distance mask: {Path.GetFileName(path)}";
                    // Queue render? DistanceMask might affect render if DistanceToMaskScale != 0
                    if (Settings.AutoUpdate) QueueRender();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading distance mask: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task SaveSvgAsync()
    {
        if (_lastRenderer == null || _lastRenderer.SubDivPoly == null || _lastRenderer.SubDivPoly.Count == 0)
        {
            StatusMessage = "No tiling data to export";
            return;
        }

        if (SaveFileDelegate == null) return;

        var file = await SaveFileDelegate("Save SVG", "tiling.svg", "svg");
        if (file == null) return;
        if (file.TryGetLocalPath() is not string path) 
        {
             StatusMessage = "Could not get local path for saving";
             return;
        }

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" standalone=\"no\"?>");
            sb.AppendLine("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">");
            sb.AppendLine($"<svg width=\"{Settings.OutputWidth}\" height=\"{Settings.OutputHeight}\" viewBox=\"0 0 {Settings.OutputWidth} {Settings.OutputHeight}\" xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\">");
            
            // Background
            var bg = Settings.BackgroundColor;
            sb.AppendLine($"<rect width=\"100%\" height=\"100%\" fill=\"rgb({bg.R},{bg.G},{bg.B})\" />");

            var fg = Settings.ForegroundColor;
            string stroke = $"rgb({fg.R},{fg.G},{fg.B})";
            string fill = Settings.FillPolygons ? stroke : "none";
            string strokeWidth = Settings.StrokeWidth.ToString(System.Globalization.CultureInfo.InvariantCulture);

            foreach (var poly in _lastRenderer.SubDivPoly)
            {
                sb.Append($"<polygon points=\"");
                for(int i=0; i<poly.Vertices.Count; i++)
                {
                    var p = poly.Vertices[i];
                    sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1} ", p.x, p.y));
                }
                sb.AppendLine($"\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{strokeWidth}\" />");
            }

            sb.AppendLine("</svg>");
            await File.WriteAllTextAsync(path, sb.ToString());
            StatusMessage = $"Exported SVG to {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting SVG: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveGerberAsync()
    {
        if (_lastRenderer == null || _lastRenderer.SubDivPoly == null || _lastRenderer.SubDivPoly.Count == 0)
        {
            StatusMessage = "No tiling data to export";
            return;
        }

        if (SaveFileDelegate == null) return;

        var file = await SaveFileDelegate("Save Gerber", "tiling.gbr", "gbr");
        if (file == null) return;
        if (file.TryGetLocalPath() is not string path)
        {
             StatusMessage = "Could not get local path for saving";
             return;
        }

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("%FSLAX46Y46*%");
            sb.AppendLine("%MOMM*%"); // Metric units
            sb.AppendLine("%LPD*%");  // Dark polarity
            sb.AppendLine("G04 TiNRS Tiler Generated*");
            
            // Define aperture 10 as circle
            double apertureSize = Settings.StrokeWidth * 0.1; // Scale factor approximation
            if (apertureSize < 0.1) apertureSize = 0.1;
            sb.AppendLine($"%ADD10C,{apertureSize.ToString(System.Globalization.CultureInfo.InvariantCulture)}*%");
            sb.AppendLine("D10*"); // Select aperture 10

            foreach (var poly in _lastRenderer.SubDivPoly)
            {
                // Start region (polygon)
                sb.AppendLine("G36*");
                
                if (poly.Vertices.Count > 0)
                {
                    // Move to first point
                    var p0 = poly.Vertices[0];
                    sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "X{0:000000}Y{1:000000}D02*", p0.x * 10000, p0.y * 10000)); // D02 = Move

                    // Draw to subsequent points
                    for (int i = 1; i < poly.Vertices.Count; i++)
                    {
                        var p = poly.Vertices[i];
                        sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "X{0:000000}Y{1:000000}D01*", p.x * 10000, p.y * 10000)); // D01 = Draw
                    }
                    
                    // Close polygon (draw back to start)
                     sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "X{0:000000}Y{1:000000}D01*", p0.x * 10000, p0.y * 10000));
                }

                // End region
                sb.AppendLine("G37*");
            }
            sb.AppendLine("M02*");

            await File.WriteAllTextAsync(path, sb.ToString());
            StatusMessage = $"Exported Gerber to {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting Gerber: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveImageAsync()
    {
        if (PreviewImage == null || SaveFileDelegate == null)
        {
            StatusMessage = "No image to save or dialog not hooked up";
            return;
        }

        var file = await SaveFileDelegate("Save Image", "image.png", "png");
        if (file != null && file.TryGetLocalPath() is string path)
        {
            try
            {
                PreviewImage.Save(path);
                StatusMessage = $"Image saved to {Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving image: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void CancelRender()
    {
        _renderService.CancelCurrentRender();
        StatusMessage = "Render cancelled";
    }

    [RelayCommand]
    private void TriggerUpdate()
    {
        if (Settings.AutoUpdate)
        {
            QueueRender();
        }
    }

    /// <summary>
    /// Queues a render with debouncing to prevent rapid re-renders
    /// </summary>
    private void QueueRender()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async void OnDebounceTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        await RenderAsync();
    }

    /// <summary>
    /// Performs the actual rendering
    /// </summary>
    private async Task RenderAsync()
    {
        if (_currentMask == null) return;

        IsRendering = true;
        StatusMessage = "Rendering...";

        var progress = new Progress<RenderProgressInfo>(info =>
        {
            ProgressPercent = info.PercentComplete;
            ProgressStage = info.Stage;
            WindowTitle = $"TiNRS-Tiler - {info.Stage} ({info.PercentComplete}%)";

            if (info.IsComplete)
            {
                if (info.WasCancelled)
                {
                    StatusMessage = "Render cancelled";
                }
                else if (info.ErrorMessage != null)
                {
                    StatusMessage = $"Error: {info.ErrorMessage}";
                }
                else
                {
                    StatusMessage = "Render complete";
                }
            }
        });

        try 
        {
            var result = await _renderService.RenderWithAutoCancelAsync(_currentMask, Settings.Clone(), progress);

            if (result.Success && result.Renderer != null)
            {
                _lastRenderer = result.Renderer;
                StatusMessage = "Render finished, updating preview...";
                // Convert the rendered output to an Avalonia bitmap
                await UpdatePreviewImageAsync(result.Renderer);
                StatusMessage = "Render complete";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Render/Update failed: {ex.Message}";
        }
        finally
        {
            IsRendering = false;
            WindowTitle = "TiNRS-Tiler";
        }
    }

    /// <summary>
    /// Updates the preview image from the renderer
    /// </summary>
    private async Task UpdatePreviewImageAsync(TINRSArtWorkRenderer renderer)
    {
        try
        {
            // Clone the mask to avoid thread conflicts if possible, or lock. 
            // For now, simple access. If it crashes, we catch it.
            
            await Task.Run(async () =>
            {
                try 
                {
                    // Create a bitmap to render into
                    using var bitmap = new Image<Rgba32>(Settings.OutputWidth, Settings.OutputHeight);
                    // Use the Compatibility interface
                    var graphics = new TilingLibrary.Compatibility.ImageSharpGraphicsInterface(bitmap);

                    // Clear background
                    var bgAv = Settings.BackgroundColor;
                    var bg = Color.FromRgba(bgAv.R, bgAv.G, bgAv.B, bgAv.A);
                    
                    graphics.Clear(bg);

                    // Draw the tiling
                    if (_currentMask != null)
                    {
                        var fgAv = Settings.ForegroundColor;
                        var fg = Color.FromRgba(fgAv.R, fgAv.G, fgAv.B, fgAv.A);

                        renderer.DrawTiling(Settings.CoreSettings, _currentMask, graphics,
                            fg,
                            bg,
                            (float)Settings.StrokeWidth, false);
                    }

                    // Convert ImageSharp Image to Avalonia Bitmap via MemoryStream
                    using var memoryStream = new MemoryStream();
                    bitmap.SaveAsPng(memoryStream);
                    memoryStream.Position = 0;

                    // Update on UI thread
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try
                        {
                            PreviewImage = new Avalonia.Media.Imaging.Bitmap(memoryStream);
                            HasPreview = true;
                        }
                        catch (Exception ex)
                        {
                            StatusMessage = $"Failed to load bitmap: {ex.Message}";
                        }
                    });
                }
                catch (Exception ex)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        StatusMessage = $"Drawing failed: {ex.Message}";
                    });
                }
            });
        }
        catch (Exception ex)
        {
             StatusMessage = $"UpdatePreview failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Creates a default circular mask
    /// </summary>
    private void CreateDefaultMask()
    {
        const int size = 500;
        _currentMask = new Image<Rgba32>(size, size);

        var graphics = new TilingLibrary.Compatibility.ImageSharpGraphicsInterface(_currentMask);
        graphics.Clear(Color.White);
        graphics.FillEllipse(new TilingLibrary.Compatibility.Primitives.SolidBrush(Color.Black),
            size / 4, size / 4, size / 2, size / 2);

        StatusMessage = "Default circular mask created";
        QueueRender();
    }

    /// <summary>
    /// Loads a mask from a file
    /// </summary>
    public async Task LoadMaskAsync(string filePath)
    {
        try
        {
            _currentMask?.Dispose();
            _currentMask = Image.Load<Rgba32>(filePath);

            Settings.MaskFilePath = filePath;
            StatusMessage = $"Loaded mask: {Path.GetFileName(filePath)}";

            // Set up file watcher if auto-reload is enabled
            if (Settings.AutoReloadMask)
            {
                SetupFileWatcher(filePath);
            }

            await RenderAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading mask: {ex.Message}";
        }
    }

    /// <summary>
    /// Sets up a file watcher to auto-reload the mask when it changes
    /// </summary>
    private void SetupFileWatcher(string filePath)
    {
        _fileWatcher?.Dispose();

        var directory = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileName(filePath);

        if (directory == null) return;

        _fileWatcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _fileWatcher.Changed += async (s, e) =>
        {
            await Task.Delay(100); // Small delay to ensure file is fully written
            await LoadMaskAsync(filePath);
        };

        _fileWatcher.EnableRaisingEvents = true;
    }

    public override void Dispose()
    {
        _debounceTimer?.Dispose();
        _fileWatcher?.Dispose();
        _currentMask?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Helper class for tiling type dropdown
/// </summary>
public class TilingTypeOption
{
    public Artwork.Tiling.TilingType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Helper class for art mode dropdown
/// </summary>
public class ArtModeOption
{
    public Artwork.Settings.ArtMode Mode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public class ScaleModeOption
{
    public Artwork.Settings.TriangleScaleMode Mode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

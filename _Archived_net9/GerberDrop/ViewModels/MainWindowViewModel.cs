using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia;
using System.Collections.Generic;
using GerberLibrary;
using GerberDrop.Views;
using Avalonia.Controls.ApplicationLifetimes;

namespace GerberDrop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = "Welcome to GerberDrop";

    // Collections
    public ObservableCollection<string> SilkScreenColors { get; } = new() { "White", "Black" };
    public ObservableCollection<string> SolderMaskColors { get; } = new() { "Red", "Green", "Blue", "Yellow", "Black", "White", "Purple" };
    public ObservableCollection<string> TraceColors { get; } = new() { "Auto", "Red", "Green", "Blue", "Yellow", "Black", "White", "Purple" };
    public ObservableCollection<string> CopperColors { get; } = new() { "Silver", "Gold" };
    public ObservableCollection<string> DpiOptions { get; } = new() { "10", "25", "100", "200", "300", "400", "800" };

    // Selected Items
    [ObservableProperty] private string _selectedSilkScreenColor = "White";
    [ObservableProperty] private string _selectedSolderMaskColor = "Red";
    [ObservableProperty] private string _selectedTraceColor = "Auto";
    [ObservableProperty] private string _selectedCopperColor = "Gold";
    [ObservableProperty] private string _selectedDpi = "400";

    // Toggles
    [ObservableProperty] private bool _xRay = true;
    [ObservableProperty] private bool _pCB = true;

    // Preview
    [ObservableProperty] private IImage? _previewImage;

    public MainWindowViewModel()
    {
        UpdatePreview();
    }

    partial void OnSelectedSilkScreenColorChanged(string value) => UpdatePreview();
    partial void OnSelectedSolderMaskColorChanged(string value) => UpdatePreview();
    partial void OnSelectedTraceColorChanged(string value) => UpdatePreview();
    partial void OnSelectedCopperColorChanged(string value) => UpdatePreview();

    private void UpdatePreview()
    {
        int W = 500;
        int H = 300;
        
        var bitmap = new RenderTargetBitmap(new PixelSize(W, H), new Vector(96, 96));
        
        using (var ctx = bitmap.CreateDrawingContext())
        {
            var solderColor = ParseAvaloniaColor(SelectedSolderMaskColor);
            var silkColor = ParseAvaloniaColor(SelectedSilkScreenColor);
            var copperColor = ParseAvaloniaColor(SelectedCopperColor);
            var traceColorStr = SelectedTraceColor == "Auto" ? SelectedSolderMaskColor : SelectedTraceColor; 
            
            var traceColor = ParseAvaloniaColor(traceColorStr);
            
            // Background (Black)
            ctx.DrawRectangle(Brushes.Black, null, new Rect(0, 0, W, H));

            int Y1 = H / 2 + 40;
            int Y2 = H / 2 - 40;
            int XL = W / 2 + (-6) * 30;
            int XR = W / 2 + (6) * 30;

            // Board
            ctx.DrawRectangle(new SolidColorBrush(solderColor), null, new Rect(XL, Y2-10, XR - XL, Y1-Y2+20));

            // Traces
            var traceBrush = new SolidColorBrush(traceColor);
            var tracePen = new Pen(traceBrush, 4);
            int XL_Trace = W / 2 + (-4) * 30;
            int XR_Trace = W / 2 + ( 4) * 30;
            
            ctx.DrawLine(tracePen, new Point(XL_Trace, Y1), new Point(XR_Trace, Y1));
            ctx.DrawLine(tracePen, new Point(XL_Trace, Y2), new Point(XR_Trace, Y2));

            // Pads
            var padBrush = new SolidColorBrush(copperColor);
            for (int i = 0; i < 10; i++)
            {
                int X = W / 2 + (i - 5) * 30;
                // DrawEllipse takes center, rx, ry
                // Original rect: X - 5, Y1 - 5, 10, 10 => Center: X, Y1, Radius: 5
                ctx.DrawEllipse(padBrush, null, new Point(X, Y1), 5, 5);
                // Hole: X - 2, Y1 - 2, 4, 4 => Center: X, Y1, Radius: 2
                ctx.DrawEllipse(Brushes.Black, null, new Point(X, Y1), 2, 2);

                ctx.DrawEllipse(padBrush, null, new Point(X, Y2), 5, 5);
                ctx.DrawEllipse(Brushes.Black, null, new Point(X, Y2), 2, 2);
            }

            // Text
            // Text
            var formattedText = new FormattedText(
                "Drop your gerber folder here!",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                20,
                new SolidColorBrush(silkColor));
                
            // Center the text
            var textPos = new Point((W - formattedText.Width) / 2, (H - formattedText.Height) / 2);
            ctx.DrawText(formattedText, textPos);
        }
        
        PreviewImage = bitmap;
    }

    private Color ParseAvaloniaColor(string name)
    {
        return name.ToLower() switch
        {
            "red" => Colors.Red,
            "green" => Colors.Green,
            "blue" => Colors.Blue,
            "yellow" => Colors.Yellow,
            "black" => Colors.Black,
            "white" => Colors.White,
            "purple" => Colors.Purple,
            "silver" => Colors.Silver,
            "gold" => Colors.Gold,
            _ => Colors.Gray
        };
    }

    public void DropFiles(IEnumerable<string> paths)
    {
        var fileList = new List<string>(paths);
        if (fileList.Count == 0) return;

        // Prepare Colors
        var colors = new BoardRenderColorSet();
        colors.BoardRenderColor = Gerber.ParseColor(SelectedSolderMaskColor);
        colors.BoardRenderSilkColor = Gerber.ParseColor(SelectedSilkScreenColor);
        colors.BoardRenderPadColor = Gerber.ParseColor(SelectedCopperColor);
        
        var traceColor = SelectedTraceColor.ToLower() == "auto" ? SelectedSolderMaskColor : SelectedTraceColor;
        colors.BoardRenderTraceColor = Gerber.ParseColor(traceColor);

        int dpi = int.Parse(SelectedDpi);

        // Spawn Progress Window
        // Use Application.Current to find the main window as owner
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new ProgressWindowViewModel(fileList, colors, dpi, XRay, PCB);
            var window = new ProgressWindow
            {
                DataContext = vm
            };
            window.Show(desktop.MainWindow); // Show as child of main window, non-modal to allow multiple Drops?
            // "each dropped item should spawn its own thread" - handled by VM Task.Run
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using GerberLibrary;
using Avalonia.Threading;
using System.IO;

namespace GerberDrop.ViewModels;

public partial class ProgressWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string _logText = "";
    [ObservableProperty] private double _progressValue = 0;
    [ObservableProperty] private string _statusText = "Starting...";
    [ObservableProperty] private bool _isIndeterminate = true;

    private readonly List<string> _files;
    private readonly BoardRenderColorSet _colors;
    private readonly int _dpi;
    private readonly bool _xRay;
    private readonly bool _pcb;

    public Action? CloseAction { get; set; }

    public ProgressWindowViewModel(List<string> files, BoardRenderColorSet colors, int dpi, bool xRay, bool pcb)
    {
        _files = files;
        _colors = colors;
        _dpi = dpi;
        _xRay = xRay;
        _pcb = pcb;
    }

    public void StartProcessing()
    {
        Task.Run(() => Process());
    }

    private void Process()
    {
        try
        {
            var logAdapter = new ViewModelProgressLog(this);
            var GIC = new GerberImageCreator();
            GIC.SetColors(_colors);
            
            AddLog("Image generation started");
            
            Gerber.SaveIntermediateImages = true;

            bool fixgroup = true;
            string? ext1 = Path.GetExtension(_files[0]);
            if (_files.Count == 1 && ext1 != ".zip") fixgroup = false;

            GIC.AddBoardsToSet(_files, logAdapter, fixgroup);

            if (GIC.Errors.Count > 0)
            {
                foreach (var a in GIC.Errors)
                {
                    AddLog($"Error: {a}");
                }
            }

            if (GIC.Count() > 1)
            {
                if (_files.Count == 1)
                {
                   string justthefilename = Path.Combine(Path.GetDirectoryName(_files[0])!, Path.GetFileNameWithoutExtension(_files[0]));
                   GIC.WriteImageFiles(justthefilename, _dpi, true, _xRay, _pcb, logAdapter);
                }
                else
                {
                    // For multiple files, we usually assume they are in the same folder or we use the first one's folder
                     GIC.WriteImageFiles(Path.GetDirectoryName(_files[0]) + ".png", _dpi, true, _xRay, _pcb, logAdapter);
                }
            }
            else
            {
                 // Single board found
                 GIC.DrawAllFiles(_files[0] + "_Layer", _dpi, logAdapter);
            }
            
            AddLog("Done!");
            
            // Auto close if successful (optional, user requested "progress view should auto-close after done")
             Dispatcher.UIThread.InvokeAsync(async () => 
             {
                 await Task.Delay(1000); // Give user a moment to see "Done"
                 CloseAction?.Invoke();
             });

        }
        catch (Exception ex)
        {
            AddLog("Exception occurred:");
            AddLog(ex.Message);
            AddLog(ex.StackTrace ?? "");
        }
    }

    public void AddLog(string text)
    {
        Dispatcher.UIThread.Invoke(() => 
        {
            LogText = text + "\n" + LogText;
            StatusText = text; // Update status line
        });
    }

    public void UpdateProgress(float progress)
    {
         Dispatcher.UIThread.Invoke(() => 
        {
            if (progress < 0)
            {
                IsIndeterminate = true;
            }
            else
            {
                IsIndeterminate = false;
                ProgressValue = progress * 100;
            }
        });
    }

    // Inner class to adapt GerberLibrary logging
    private class ViewModelProgressLog : ProgressLog
    {
        private readonly ProgressWindowViewModel _vm;
        public ViewModelProgressLog(ProgressWindowViewModel vm)
        {
            _vm = vm;
        }

        public override void AddString(string text, float progress = -1)
        {
            _vm.AddLog(text);
            _vm.UpdateProgress(progress);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Input;
using GerberDrop.ViewModels;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;

namespace GerberDrop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
            e.DragEffects = DragDropEffects.Copy;
        else
            e.DragEffects = DragDropEffects.None;
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null && DataContext is MainWindowViewModel vm)
            {
                var paths = files.Select(f => f.Path.LocalPath).ToList(); // Use LocalPath for file system paths
                vm.DropFiles(paths);
            }
        }
    }
}
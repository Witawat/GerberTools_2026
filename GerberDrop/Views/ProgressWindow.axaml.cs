using Avalonia.Controls;
using Avalonia.Interactivity;
using GerberDrop.ViewModels;
using System;

namespace GerberDrop.Views;

public partial class ProgressWindow : Window
{
    public ProgressWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is ProgressWindowViewModel vm)
        {
            vm.CloseAction = Close;
            vm.StartProcessing();
        }
    }
}

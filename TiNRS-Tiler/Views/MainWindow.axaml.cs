using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Linq;
using TiNRS.Tiler.ViewModels;

namespace TiNRS.Tiler.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Wait for DataContext to be set
        this.DataContextChanged += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.OpenFileDelegate = async () =>
                {
                    var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Open Mask Image",
                        AllowMultiple = false,
                        FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
                    });

                    return files.Count >= 1 ? files[0] : null;
                };

                vm.OpenDistanceMaskDelegate = async () =>
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel == null) return null;

                    var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Open Distance Mask Image",
                        AllowMultiple = false,
                        FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
                    });

                    return files.Count >= 1 ? files[0] : null;
                };

                vm.SaveFileDelegate = async (title, defaultName, extension) =>
                {
                    var file = await this.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = title,
                        SuggestedFileName = defaultName,
                        DefaultExtension = extension,
                        FileTypeChoices = new[]
                        {
                            new FilePickerFileType(extension.ToUpper()) { Patterns = new[] { $"*.{extension}" } }
                        }
                    });

                    return file;
                };
            }
        };
    }
}
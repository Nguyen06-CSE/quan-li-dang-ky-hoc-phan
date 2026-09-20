using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using QuanLyDKHP.App.ViewModels;

namespace QuanLyDKHP.App.Views;

public partial class HocPhiView : UserControl
{
    public HocPhiView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is HocPhiViewModel vm)
        {
            vm.SaveFileDialogFunc = ShowSaveFileDialogAsync;
        }
    }

    private void OnPhamViRadioChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag && DataContext is HocPhiViewModel vm)
        {
            vm.PhamVi = tag;
        }
    }

    private async Task<string?> ShowSaveFileDialogAsync(string suggestedFileName, string extension, byte[] data)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Lưu tệp",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = extension,
            FileTypeChoices = new[]
            {
                new FilePickerFileType(extension.ToUpper())
                {
                    Patterns = new[] { $"*.{extension}" }
                }
            }
        });

        if (file != null)
        {
            await using var stream = await file.OpenWriteAsync();
            await stream.WriteAsync(data);
            return file.Path.LocalPath;
        }

        return null;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

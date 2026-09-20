using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using QuanLyDKHP.App.ViewModels;

namespace QuanLyDKHP.App.Views;

public partial class ImportExcelView : UserControl
{
    public ImportExcelView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ImportExcelViewModel vm)
        {
            vm.OpenFileDialogFunc = ShowOpenFileDialogAsync;
            vm.SaveFileDialogFunc = ShowSaveFileDialogAsync;
        }
    }

    private async Task<string?> ShowOpenFileDialogAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Chọn file Excel dữ liệu ĐKHP",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Excel Workbook")
                {
                    Patterns = new[] { "*.xlsx" }
                }
            }
        });

        if (files.Count > 0)
        {
            return files[0].Path.LocalPath;
        }

        return null;
    }

    private async Task<string?> ShowSaveFileDialogAsync(string suggestedFileName, string extension, byte[] data)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Lưu file log import",
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

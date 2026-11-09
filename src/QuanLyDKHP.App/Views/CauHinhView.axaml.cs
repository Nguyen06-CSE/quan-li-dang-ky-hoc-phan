using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using QuanLyDKHP.App.ViewModels;

namespace QuanLyDKHP.App.Views;

public partial class CauHinhView : UserControl
{
    public CauHinhView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is CauHinhViewModel vm)
        {
            vm.SaveFileDialogFunc = ShowSaveBackupFileDialogAsync;
            vm.OpenBackupFileDialogFunc = ShowOpenBackupFileDialogAsync;
        }
    }

    private async Task<string?> ShowSaveBackupFileDialogAsync(string suggestedFileName, string extension, byte[] data)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Xuất gói sao lưu CSDL (QuanLyDKHP Backup)",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = extension,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Gói sao lưu QuanLyDKHP (*.dkhp-backup.zip, *.zip)")
                {
                    Patterns = new[] { "*.dkhp-backup.zip", "*.zip" }
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

    private async Task<(string FileName, Stream Content)?> ShowOpenBackupFileDialogAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Chọn tệp sao lưu QuanLyDKHP để ghi đè CSDL",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Gói sao lưu QuanLyDKHP (*.dkhp-backup.zip, *.zip)")
                {
                    Patterns = new[] { "*.dkhp-backup.zip", "*.zip" }
                }
            }
        });

        var selected = files.FirstOrDefault();
        if (selected == null) return null;

        var stream = await selected.OpenReadAsync();
        return (selected.Name, stream);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using QuanLyDKHP.App.ViewModels;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.App.Views;

public partial class SinhVienView : UserControl
{
    public SinhVienView()
    {
        try
        {
            InitializeComponent();
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine("=== LỖI KHỞI TẠO SINHVIENVIEW ===");
            System.Console.WriteLine(ex.ToString());
            Content = new TextBlock
            {
                Text = "=== LỖI KHỞI TẠO SINHVIENVIEW ===\n\n" + ex.ToString(),
                Foreground = Avalonia.Media.Brushes.Red,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(20)
            };
        }
    }

    private void OnDataGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is DataGrid dg && dg.SelectedItem is SinhVienDto sv && DataContext is SinhVienViewModel vm)
        {
            vm.DangKyHocPhanCommand.Execute(sv);
        }
    }

    private void OnLopFlyoutOpened(object? sender, EventArgs e)
    {
        var txt = this.FindControl<TextBox>("TxtSearchLop")
                  ?? ((sender as Flyout)?.Content as ILogical)?.GetLogicalDescendants().OfType<TextBox>().FirstOrDefault();
        if (txt != null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                txt.Focus();
                txt.SelectAll();
            });
        }
    }

    private void OnLopFilterItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox lb && lb.SelectedItem != null &&
            this.FindControl<Button>("BtnFilterLop")?.Flyout is Flyout flyout)
        {
            flyout.Hide();
        }
    }

    private void OnKhoaHocFlyoutOpened(object? sender, EventArgs e)
    {
        var txt = this.FindControl<TextBox>("TxtSearchKhoaHoc")
                  ?? ((sender as Flyout)?.Content as ILogical)?.GetLogicalDescendants().OfType<TextBox>().FirstOrDefault();
        if (txt != null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                txt.Focus();
                txt.SelectAll();
            });
        }
    }

    private void OnKhoaHocFilterItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox lb && lb.SelectedItem != null &&
            this.FindControl<Button>("BtnFilterKhoaHoc")?.Flyout is Flyout flyout)
        {
            flyout.Hide();
        }
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is SinhVienViewModel vm)
        {
            vm.ShowEditDialogFunc = ShowEditDialogAsync;
            vm.ShowConfirmDeleteFunc = ShowConfirmDeleteAsync;
            vm.SaveFileDialogFunc = ShowSaveFileDialogAsync;
            vm.OpenFileDialogFunc = ShowOpenFileDialogAsync;
        }
    }

    private async Task<string?> ShowOpenFileDialogAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Chọn file Excel dữ liệu sinh viên / ĐKHP",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Excel Files (*.xlsx)")
                {
                    Patterns = new[] { "*.xlsx" }
                }
            }
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }


    private async Task<string?> ShowSaveFileDialogAsync(string suggestedFileName, string extension, byte[] data)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Lưu file Excel",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = extension,
            FileTypeChoices = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType(extension.ToUpper())
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

    private async Task<SinhVien?> ShowEditDialogAsync(SinhVien? sv)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return null;

        var dialogVm = new SinhVienEditDialogViewModel(sv);
        var dialog = new SinhVienEditDialog(dialogVm);

        await dialog.ShowDialog(topLevel);

        if (dialogVm.IsSuccess)
        {
            return dialogVm.ToEntity();
        }
        return null;
    }

    private async Task<bool> ShowConfirmDeleteAsync(string message)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return false;

        // Simple confirmation window dialog
        var dialog = new Window
        {
            Title = "Xác nhận xóa",
            Width = 380,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        bool result = false;

        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(16)
        };

        var btnCancel = new Button
        {
            Content = "Hủy",
            Width = 80,
            Margin = new Avalonia.Thickness(0, 0, 8, 0)
        };
        btnCancel.Click += (s, e) => { result = false; dialog.Close(); };

        var btnOk = new Button
        {
            Content = "Xóa",
            Width = 80
        };
        btnOk.Click += (s, e) => { result = true; dialog.Close(); };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(16)
        };
        buttonPanel.Children.Add(btnCancel);
        buttonPanel.Children.Add(btnOk);

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto")
        };
        Grid.SetRow(textBlock, 0);
        Grid.SetRow(buttonPanel, 1);
        grid.Children.Add(textBlock);
        grid.Children.Add(buttonPanel);

        dialog.Content = grid;
        await dialog.ShowDialog(topLevel);

        return result;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

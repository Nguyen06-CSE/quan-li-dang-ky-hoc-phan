// src/QuanLyDKHP.App/Views/DangKyHocPhanView.axaml.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using QuanLyDKHP.App.ViewModels;
using QuanLyDKHP.Core.Dtos;

namespace QuanLyDKHP.App.Views;

public partial class DangKyHocPhanView : UserControl
{
    private bool _isSyncingSelection;

    public DangKyHocPhanView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is DangKyHocPhanViewModel vm)
        {
            vm.ShowConfirmFunc = ShowConfirmDialogAsync;
            vm.ShowWarningConfirmFunc = ShowWarningDialogAsync;
            vm.RequestSelectStudents = SyncListBoxSelection;
        }
    }

    /// <summary>
    /// Đồng bộ lựa chọn sinh viên từ ViewModel xuống ListBox 
    /// (ví dụ khi nhận NavigateToRegistrationMessage)
    /// </summary>
    private void SyncListBoxSelection(List<SinhVienDto> students)
    {
        var lb = this.FindControl<ListBox>("SinhVienListBox");
        if (lb == null) return;

        _isSyncingSelection = true;
        try
        {
            lb.SelectedItems?.Clear();
            foreach (var sv in students)
            {
                lb.SelectedItems?.Add(sv);
            }
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    /// <summary>
    /// Xử lý sự kiện thay đổi lựa chọn trên ListBox (hỗ trợ Single & Multi-select).
    /// </summary>
    private void OnSinhVienListBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingSelection) return;
        if (DataContext is DangKyHocPhanViewModel vm && sender is ListBox listBox)
        {
            var selected = listBox.SelectedItems?.Cast<SinhVienDto>().ToList() ?? [];
            _ = vm.HandleSelectionChanged(selected);
        }
    }

    /// <summary>
    /// Xử lý Toggle bỏ chọn khi click vào chính sinh viên đang chọn duy nhất.
    /// </summary>
    private void OnSinhVienItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Lấy đối tượng SinhVienDto từ DataContext của Border
        if (sender is Control control && control.DataContext is SinhVienDto clickedSv)
        {
            var listBox = this.FindControl<ListBox>("SinhVienListBox");
            if (listBox == null) return;

            // Nếu đang chọn duy nhất sinh viên này và click lại vào chính nó -> Toggle bỏ chọn
            if (listBox.SelectedItems != null && 
                listBox.SelectedItems.Count == 1 && 
                listBox.SelectedItems.Contains(clickedSv))
            {
                e.Handled = true;

                Dispatcher.UIThread.Post(async () =>
                {
                    listBox.SelectedItems.Clear();

                    if (DataContext is DangKyHocPhanViewModel vm)
                    {
                        await vm.HandleSelectionChanged(Array.Empty<SinhVienDto>());
                    }
                });
            }
        }
    }
    private async Task<bool> ShowConfirmDialogAsync(string message)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return false;

        var dialog = new Window
        {
            Title = "Xác nhận hủy đăng ký",
            Width = 420,
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
            Content = "Đồng ý",
            Width = 80
        };
        btnOk.Classes.Add("btn-danger");
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

    private async Task<bool> ShowWarningDialogAsync(string message)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return false;

        var dialog = new Window
        {
            Title = "Cảnh báo trùng môn học",
            Width = 450,
            Height = 180,
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
            Content = "Vẫn đăng ký",
            Width = 110
        };
        btnOk.Classes.Add("btn-primary");
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
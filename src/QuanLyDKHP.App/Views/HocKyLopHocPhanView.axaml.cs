using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using QuanLyDKHP.App.ViewModels;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.App.Views;

public partial class HocKyLopHocPhanView : UserControl
{
    public HocKyLopHocPhanView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is HocKyLopHocPhanViewModel vm)
        {
            vm.ShowHocKyEditDialogFunc = ShowHocKyEditDialogAsync;
            vm.ShowLhpEditDialogFunc = ShowLhpEditDialogAsync;
            vm.ShowConfirmDeleteFunc = ShowConfirmDeleteAsync;
        }
    }

    private async Task<HocKy?> ShowHocKyEditDialogAsync(HocKy? hocKy)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return null;

        var dialogVm = new HocKyEditDialogViewModel(hocKy);
        var dialog = new HocKyEditDialog(dialogVm);

        await dialog.ShowDialog(topLevel);

        if (dialogVm.IsSuccess)
        {
            return dialogVm.ToEntity();
        }
        return null;
    }

    private async Task<LopHocPhan?> ShowLhpEditDialogAsync(
        string maHocKy,
        List<MonHoc> danhSachMonHoc,
        List<NguoiDung> danhSachGiangVien,
        LopHocPhan? lhp)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return null;

        var dialogVm = new LopHocPhanEditDialogViewModel(maHocKy, danhSachMonHoc, danhSachGiangVien, lhp);
        var dialog = new LopHocPhanEditDialog(dialogVm);

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

        var dialog = new Window
        {
            Title = "Xác nhận xóa",
            Width = 400,
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

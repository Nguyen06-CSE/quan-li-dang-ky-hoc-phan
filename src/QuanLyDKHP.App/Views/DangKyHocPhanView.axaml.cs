using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using QuanLyDKHP.App.ViewModels;

namespace QuanLyDKHP.App.Views;

public partial class DangKyHocPhanView : UserControl
{
    public DangKyHocPhanView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is DangKyHocPhanViewModel vm)
        {
            vm.ShowConfirmFunc = ShowConfirmDialogAsync;
            vm.ShowWarningConfirmFunc = ShowWarningDialogAsync;
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

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using QuanLyDKHP.App.ViewModels;

namespace QuanLyDKHP.App;

public partial class MainWindow : Window
{
    private Border? _themeOverlay;
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _themeOverlay = this.FindControl<Border>("ThemeFadeOverlay");
        DataContextChanged += OnDataContextChanged;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel == null) return;

        // Đồng bộ nút Sáng/Tối với theme hiện tại của app (tránh lệch sau khi đăng xuất/đăng nhập lại)
        _viewModel.IsDarkMode = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsDarkMode) && _viewModel != null)
            _ = ApplyThemeAsync(_viewModel.IsDarkMode);
    }

    /// <summary>
    /// Đổi theme kèm hiệu ứng fade 0.3s: phủ tức thì màu nền cũ lên toàn cửa sổ,
    /// đổi theme phía sau, rồi cho lớp phủ mờ dần để lộ giao diện mới — không bị chớp gắt.
    /// </summary>
    private async Task ApplyThemeAsync(bool dark)
    {
        var app = Application.Current;
        if (app == null) return;

        if (_themeOverlay == null)
        {
            app.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
            return;
        }

        IBrush oldBackground = Brushes.Transparent;
        if (this.TryFindResource("BrushBackground", ActualThemeVariant, out var res) && res is IBrush brush)
            oldBackground = brush;

        _themeOverlay.Transitions = null;
        _themeOverlay.Background = oldBackground;
        _themeOverlay.Opacity = 1;

        app.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;

        // Chờ 1 nhịp render để theme mới áp dụng xong rồi mới mờ dần lớp phủ
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

        _themeOverlay.Transitions = new Transitions
        {
            new DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(300) }
        };
        _themeOverlay.Opacity = 0;
    }
}

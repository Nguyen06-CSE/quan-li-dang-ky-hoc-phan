// src/QuanLyDKHP.App/MainWindow.axaml.cs

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using QuanLyDKHP.App.ViewModels;

namespace QuanLyDKHP.App;

public partial class MainWindow : Window
{
    private Border? _themeOverlay;
    private TextBox? _paletteSearchBox;
    private ListBox? _paletteResultsListBox;
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _themeOverlay = this.FindControl<Border>("ThemeFadeOverlay");
        _paletteSearchBox = this.FindControl<TextBox>("PaletteSearchBox");
        _paletteResultsListBox = this.FindControl<ListBox>("PaletteResultsListBox");

        DataContextChanged += OnDataContextChanged;

        // Lắng nghe phím tắt toàn cục ở chế độ Tunnel để Ctrl+K / ⌘K và phím mũi tên hoạt động mượt kể cả khi đang focus trong TextBox/DataGrid
        AddHandler(KeyDownEvent, OnWindowTunnelKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.CommandPaletteOpened -= OnCommandPaletteOpened;
        }

        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel == null) return;

        // Đồng bộ nút Sáng/Tối với theme hiện tại của app
        _viewModel.IsDarkMode = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.CommandPaletteOpened += OnCommandPaletteOpened;
    }

    private void OnCommandPaletteOpened(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _paletteSearchBox ??= this.FindControl<TextBox>("PaletteSearchBox");
            if (_paletteSearchBox != null)
            {
                _paletteSearchBox.Focus();
                _paletteSearchBox.SelectAll();
            }
        }, DispatcherPriority.Input);
    }

    private void OnWindowTunnelKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel == null) return;

        // 1. Tổ hợp phím Ctrl + K hoặc Cmd + K (Meta + K) mở/đóng Universal Command Palette từ bất kỳ đâu
        bool isCtrlOrMeta = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        if (isCtrlOrMeta && e.Key == Key.K)
        {
            e.Handled = true;
            _viewModel.ToggleCommandPalette();
            return;
        }

        // 2. Khi Command Palette đang mở: xử lý điều hướng bàn phím ↑ ↓ Enter Esc chuẩn Raycast
        if (_viewModel.IsCommandPaletteOpen)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                _viewModel.CloseCommandPalette();
                return;
            }

            if (e.Key == Key.Down)
            {
                e.Handled = true;
                _viewModel.MovePaletteSelectionDown();
                ScrollSelectedPaletteItemIntoView();
                return;
            }

            if (e.Key == Key.Up)
            {
                e.Handled = true;
                _viewModel.MovePaletteSelectionUp();
                ScrollSelectedPaletteItemIntoView();
                return;
            }

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                _ = _viewModel.ExecuteSelectedPaletteItemAsync();
                return;
            }
        }
    }

    private void ScrollSelectedPaletteItemIntoView()
    {
        _paletteResultsListBox ??= this.FindControl<ListBox>("PaletteResultsListBox");
        if (_paletteResultsListBox != null && _viewModel?.SelectedPaletteItem != null)
        {
            _paletteResultsListBox.ScrollIntoView(_viewModel.SelectedPaletteItem);
        }
    }

    private void OnPaletteBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _viewModel?.CloseCommandPalette();
    }

    private void OnPaletteResultDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel != null)
        {
            _ = _viewModel.ExecuteSelectedPaletteItemAsync();
        }
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

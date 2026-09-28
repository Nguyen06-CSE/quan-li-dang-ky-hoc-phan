using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using QuanLyDKHP.App.ViewModels;

namespace QuanLyDKHP.App.Views;

public partial class LoginWindow : Window
{
    private Border? _passwordShakeTarget;
    private TextBox? _tenDangNhapBox;
    private TextBox? _passwordHiddenBox;
    private bool _dangRung;

    public LoginWindow()
    {
        InitializeComponent();

        _passwordShakeTarget = this.FindControl<Border>("PasswordShakeTarget");
        _tenDangNhapBox = this.FindControl<TextBox>("TenDangNhapBox");
        _passwordHiddenBox = this.FindControl<TextBox>("PasswordHiddenBox");

        Opened += OnWindowOpened;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public LoginWindow(LoginViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.ShakeRequested += OnShakeRequested;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        // Ưu tiên focus vào ô trống trước: nếu MSSV đã được điền sẵn (nhờ "Lưu đăng nhập"),
        // focus thẳng vào ô mật khẩu để người dùng gõ ngay, đỡ phải click chuột trước.
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is LoginViewModel vm && !string.IsNullOrWhiteSpace(vm.TenDangNhap))
            {
                _passwordHiddenBox?.Focus();
            }
            else
            {
                _tenDangNhapBox?.Focus();
            }
        }, DispatcherPriority.Loaded);
    }

    private void OnShakeRequested(object? sender, EventArgs e)
    {
        // Chạy rung ô mật khẩu (0.5s) để báo hiệu lỗi đăng nhập theo spec Module 1.
        _ = RunShakeAnimationAsync();
    }

    /// <summary>
    /// Rung nhẹ Border chứa ô mật khẩu trong ~0.5s bằng cách dịch chuyển ngang liên tiếp.
    /// Không dùng Avalonia keyframe Animation để tránh phụ thuộc thêm — đủ mượt cho hiệu ứng lỗi.
    /// </summary>
    private async Task RunShakeAnimationAsync()
    {
        if (_passwordShakeTarget == null || _dangRung)
            return;

        _dangRung = true;
        try
        {
            var transform = new TranslateTransform();
            _passwordShakeTarget.RenderTransform = transform;

            // Biên độ giảm dần: trái - phải - trái - phải - về giữa, tổng ~0.5s
            double[] offsets = { -8, 8, -6, 6, -3, 3, 0 };
            const int stepMs = 70;

            foreach (var offset in offsets)
            {
                transform.X = offset;
                await Task.Delay(stepMs);
            }
        }
        finally
        {
            _dangRung = false;
        }
    }
}

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDKHP.App.ViewModels;
using QuanLyDKHP.App.Views;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;
using QuanLyDKHP.Infrastructure.Repositories;
using QuanLyDKHP.Services;

namespace QuanLyDKHP.App;

public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ShowLoginWindow(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 1. Khởi tạo Configuration để đọc file appsettings.json
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Không tìm thấy chuỗi kết nối 'DefaultConnection' trong appsettings.json.");

        // 2. Đăng ký AppDbContext sử dụng PostgreSQL (Npgsql)
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<INguoiDungRepository, NguoiDungRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ISinhVienRepository, SinhVienRepository>();
        services.AddScoped<IMonHocRepository, MonHocRepository>();
        services.AddScoped<IHocKyRepository, HocKyRepository>();
        services.AddScoped<ILopHocPhanRepository, LopHocPhanRepository>();
        services.AddScoped<IDangKyHocPhanRepository, DangKyHocPhanRepository>();
        services.AddScoped<ICauHinhRepository, CauHinhRepository>();

        // Services
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBaoCaoService, BaoCaoService>();
        services.AddScoped<ISinhVienService, SinhVienService>();
        services.AddScoped<IMonHocService, MonHocService>();
        services.AddScoped<IHocKyService, HocKyService>();
        services.AddScoped<ILopHocPhanService, LopHocPhanService>();
        services.AddScoped<ICauHinhService, CauHinhService>();
        services.AddScoped<IHocPhiService, HocPhiService>();
        services.AddScoped<IDangKyHocPhanService, DangKyHocPhanService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<SinhVienViewModel>();
        services.AddTransient<MonHocViewModel>();
        services.AddTransient<HocKyLopHocPhanViewModel>();
        services.AddTransient<DangKyHocPhanViewModel>();
        services.AddSingleton<Func<DashboardViewModel>>(sp => () => sp.GetRequiredService<DashboardViewModel>());
        services.AddSingleton<Func<SinhVienViewModel>>(sp => () => sp.GetRequiredService<SinhVienViewModel>());
        services.AddSingleton<Func<MonHocViewModel>>(sp => () => sp.GetRequiredService<MonHocViewModel>());
        services.AddSingleton<Func<HocKyLopHocPhanViewModel>>(sp => () => sp.GetRequiredService<HocKyLopHocPhanViewModel>());
        services.AddSingleton<Func<DangKyHocPhanViewModel>>(sp => () => sp.GetRequiredService<DangKyHocPhanViewModel>());

        // Views
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<DashboardView>();
        services.AddTransient<SinhVienView>();
        services.AddTransient<MonHocView>();
        services.AddTransient<HocKyLopHocPhanView>();
        services.AddTransient<DangKyHocPhanView>();
    }

    private static void ShowLoginWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var loginViewModel = ServiceProvider!.GetRequiredService<LoginViewModel>();
        var loginWindow = new LoginWindow(loginViewModel);

        loginViewModel.LoginSuccess += (sender, args) =>
        {
            var mainViewModel = ServiceProvider!.GetRequiredService<MainWindowViewModel>();
            mainViewModel.RefreshMenuForCurrentUser();

            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            mainViewModel.LogoutRequested += (s, e) =>
            {
                ShowLoginWindow(desktop);
                mainWindow.Close();
            };

            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            loginWindow.Close();
        };

        desktop.MainWindow = loginWindow;
        loginWindow.Show();
    }
}
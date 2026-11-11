using System;
using System.IO;
using System.Threading.Tasks;
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
using QuanLyDKHP.Infrastructure.Services;
using QuanLyDKHP.Services;
using QuanLyDKHP.App.Services;

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

        // 1. Khởi tạo CSDL SQLite Local, đảm bảo đầy đủ các bảng và bật chế độ WAL
        using (var initScope = ServiceProvider.CreateScope())
        {
            var appDbFactory = initScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = appDbFactory.CreateDbContext();
            AppDbContext.EnsureTablesAndSeedAsync(db).GetAwaiter().GetResult();
            try
            {
                db.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SQLite PRAGMA Warning]: {ex.Message}");
            }
        }

        // 2. CHẠY NỀN: Tự động nạp dữ liệu từ Neon nếu SQLite trống & nạp In-Memory Cache Store & Sao lưu hàng ngày (Git)
        var cacheStore = ServiceProvider.GetRequiredService<IMemoryCacheStore>();
        _ = Task.Run(async () =>
        {
            try
            {
                using (var syncScope = ServiceProvider.CreateScope())
                {
                    var syncService = syncScope.ServiceProvider.GetRequiredService<ISyncService>();
                    if (!await syncService.HasLocalDataAsync())
                    {
                        System.Diagnostics.Debug.WriteLine("[Startup]: SQLite trống, đang tự động đồng bộ dữ liệu ban đầu từ DB Host (Neon)...");
                        await syncService.InitialSeedAsync();
                    }
                }

                await cacheStore.InitializeAsync();

                using var backupScope = ServiceProvider.CreateScope();
                var backupService = backupScope.ServiceProvider.GetRequiredService<IBackupService>();
                await backupService.EnsureDailyBackupAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Startup Background Error]: {ex.Message}");
            }
        });

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Tự động sao lưu khi tắt ứng dụng nếu có dữ liệu mới (chạy ngầm trên ThreadPool với timeout 2s để không bao giờ treo giao diện khi thoát)
            desktop.ShutdownRequested += (s, e) =>
            {
                try
                {
                    Task.Run(async () =>
                    {
                        using var exitScope = ServiceProvider.CreateScope();
                        var backupService = exitScope.ServiceProvider.GetRequiredService<IBackupService>();
                        await backupService.CreateLocalGitBackupAsync("Auto-OnExit", skipIfNoChanges: true);
                    }).Wait(TimeSpan.FromSeconds(2));
                }
                catch { }
            };

            ShowLoginWindow(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 1. Khởi tạo Configuration
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // 2. Đăng ký CSDL chính: SQLite Local (chạy độc lập tại máy tính văn phòng)
        string sqlitePath = AppDbContext.GetPrimarySqlitePath();
        string sqliteConnStr = $"Data Source={sqlitePath}";

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(sqliteConnStr));

        services.AddDbContextFactory<LocalAppDbContext>(options =>
            options.UseSqlite(sqliteConnStr));

        // 3. Đăng ký DB Host từ xa nếu cấu hình có chuỗi kết nối PostgreSQL (Backup tầng 2)
        string? remoteConn = configuration.GetConnectionString("RemoteBackupConnection") 
            ?? configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrWhiteSpace(remoteConn) && remoteConn.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContextFactory<RemoteBackupDbContext>(options =>
                options.UseNpgsql(remoteConn));
        }

        // Repositories
        services.AddScoped<INguoiDungRepository, NguoiDungRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IBaoCaoRepository, BaoCaoRepository>();
        services.AddScoped<ISinhVienRepository, SinhVienRepository>();
        services.AddScoped<IMonHocRepository, MonHocRepository>();
        services.AddScoped<IHocKyRepository, HocKyRepository>();
        services.AddScoped<ILopHocPhanRepository, LopHocPhanRepository>();
        services.AddScoped<IDangKyHocPhanRepository, DangKyHocPhanRepository>();
        services.AddScoped<ICauHinhRepository, CauHinhRepository>();

        // Services
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INguoiDungService, NguoiDungService>();
        services.AddScoped<IBaoCaoService, BaoCaoService>();
        services.AddScoped<ISinhVienService, SinhVienService>();
        services.AddScoped<IMonHocService, MonHocService>();
        services.AddScoped<IHocKyService, HocKyService>();
        services.AddScoped<ILopHocPhanService, LopHocPhanService>();
        services.AddScoped<ICauHinhService, CauHinhService>();
        services.AddScoped<IHocPhiService, HocPhiService>();
        services.AddScoped<IDangKyHocPhanService, DangKyHocPhanService>();
        services.AddScoped<IImportExcelRepository, QuanLyDKHP.Infrastructure.Repositories.ImportExcelRepository>();
        services.AddScoped<IImportExcelService, ImportExcelService>();
        services.AddScoped<IPdfExportService, QuanLyDKHP.Infrastructure.Export.PdfExportService>();
        services.AddScoped<IExcelExportService, QuanLyDKHP.Infrastructure.Export.ExcelExportService>();
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<ILocalReadService, LocalReadService>();
        services.AddScoped<IBackupService, BackupService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<SinhVienViewModel>();
        services.AddTransient<MonHocViewModel>();
        services.AddTransient<HocKyLopHocPhanViewModel>();
        services.AddTransient<DangKyHocPhanViewModel>();
        services.AddTransient<HocPhiViewModel>();
        services.AddTransient<BaoCaoViewModel>();
        services.AddTransient<CauHinhViewModel>();
        services.AddTransient<NguoiDungViewModel>();
        services.AddTransient<ImportExcelViewModel>();
        services.AddSingleton<Func<DashboardViewModel>>(sp => () => sp.GetRequiredService<DashboardViewModel>());
        services.AddSingleton<Func<SinhVienViewModel>>(sp => () => sp.GetRequiredService<SinhVienViewModel>());
        services.AddSingleton<Func<MonHocViewModel>>(sp => () => sp.GetRequiredService<MonHocViewModel>());
        services.AddSingleton<Func<HocKyLopHocPhanViewModel>>(sp => () => sp.GetRequiredService<HocKyLopHocPhanViewModel>());
        services.AddSingleton<Func<DangKyHocPhanViewModel>>(sp => () => sp.GetRequiredService<DangKyHocPhanViewModel>());
        services.AddSingleton<Func<HocPhiViewModel>>(sp => () => sp.GetRequiredService<HocPhiViewModel>());
        services.AddSingleton<Func<BaoCaoViewModel>>(sp => () => sp.GetRequiredService<BaoCaoViewModel>());
        services.AddSingleton<Func<CauHinhViewModel>>(sp => () => sp.GetRequiredService<CauHinhViewModel>());
        services.AddSingleton<Func<NguoiDungViewModel>>(sp => () => sp.GetRequiredService<NguoiDungViewModel>());
        services.AddSingleton<Func<ImportExcelViewModel>>(sp => () => sp.GetRequiredService<ImportExcelViewModel>());
       
        // Views
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<DashboardView>();
        services.AddTransient<SinhVienView>();
        services.AddTransient<MonHocView>();
        services.AddTransient<HocKyLopHocPhanView>();
        services.AddTransient<DangKyHocPhanView>();
        services.AddTransient<HocPhiView>();
        services.AddTransient<BaoCaoView>();
        services.AddTransient<CauHinhView>();
        services.AddTransient<NguoiDungView>();
        services.AddTransient<ImportExcelView>();

        services.AddSingleton<IMemoryCacheStore, MemoryCacheStore>();
    }

    private static void ShowLoginWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var loginViewModel = ServiceProvider!.GetRequiredService<LoginViewModel>();
        var loginWindow = new LoginWindow(loginViewModel);

        loginViewModel.LoginSuccess += (sender, args) =>
        {
            var cacheStore = ServiceProvider!.GetRequiredService<IMemoryCacheStore>();
            _ = Task.Run(() => cacheStore.InitializeAsync());

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

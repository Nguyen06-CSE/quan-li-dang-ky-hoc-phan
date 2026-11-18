using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
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
        var sqlitePath = AppDbContext.GetPrimarySqlitePath();
        bool isNewDatabase = !File.Exists(sqlitePath) || new FileInfo(sqlitePath).Length == 0;

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

        LogSqliteStartup(sqlitePath, isNewDatabase);

        // 2. CHẠY NỀN: nạp In-Memory Cache Store từ SQLite local & sao lưu Git hằng ngày.
        // Không seed/pull dữ liệu từ bất kỳ DB host nào.
        var cacheStore = ServiceProvider.GetRequiredService<IMemoryCacheStore>();
        _ = Task.Run(async () =>
        {
            try
            {
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
        // 1. CSDL chính duy nhất: SQLite Local (chạy độc lập tại máy tính văn phòng).
        string sqlitePath = AppDbContext.GetPrimarySqlitePath();
        string sqliteConnStr = $"Data Source={sqlitePath}";

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(sqliteConnStr));

        services.AddDbContextFactory<LocalAppDbContext>(options =>
            options.UseSqlite(sqliteConnStr));

        // 2. Cấu hình PostgreSQL backup: đọc/ghi file local, không nằm trong appsettings.
        services.AddSingleton<IBackupConfigStore, BackupConfigStore>();

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
        services.AddScoped<ISmartImportService, SmartImportService>();
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

    private static void LogSqliteStartup(string sqlitePath, bool isNewDatabase)
    {
        var fileName = Path.GetFileName(sqlitePath);
        var message = isNewDatabase
            ? $"Đã tạo CSDL SQLite local mới: {fileName} | Đường dẫn đầy đủ: {sqlitePath}"
            : $"Đang sử dụng CSDL SQLite local: {fileName} | Đường dẫn đầy đủ: {sqlitePath}";

        Console.WriteLine($"[SQLite] {message}");
        Debug.WriteLine($"[SQLite] {message}");

        try
        {
            var logPath = Path.Combine(Path.GetDirectoryName(sqlitePath) ?? AppContext.BaseDirectory, "sqlite-startup.log");
            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
        }
        catch
        {
            // Log file không bắt buộc; không làm hỏng khởi động.
        }
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.App.Services;

public class MemoryCacheStore : IMemoryCacheStore
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);

    public IReadOnlyList<HocKy> DanhSachHocKy { get; private set; } = Array.Empty<HocKy>();
    public IReadOnlyList<string> DanhSachLopSinhHoat { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> DanhSachKhoaHoc { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<MonHoc> DanhSachMonHoc { get; private set; } = Array.Empty<MonHoc>();

    public double LoadingProgress { get; private set; }
    public string LoadingStatus { get; private set; } = string.Empty;
    public event Action<double, string>? ProgressChanged;

    public bool IsInitialized { get; private set; }

    public MemoryCacheStore(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private void ReportProgress(double percent, string status)
    {
        LoadingProgress = percent;
        LoadingStatus = status;
        ProgressChanged?.Invoke(percent, status);
    }

    public async Task InitializeAsync(bool forceReload = false)
    {
        if (IsInitialized && !forceReload) return;

        await _initializeLock.WaitAsync();
        try
        {
            if (IsInitialized && !forceReload) return;

            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
            var localFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LocalAppDbContext>>();

            if (forceReload)
            {
                await syncService.ForceFullRefreshAsync(new Progress<(double Percent, string Status)>(p =>
                    ReportProgress(p.Percent, p.Status)));
            }
            else if (!await syncService.HasLocalDataAsync())
            {
                await syncService.InitialSeedAsync(new Progress<(double Percent, string Status)>(p =>
                    ReportProgress(p.Percent, p.Status)));
            }

            await LoadFromLocalCacheAsync(localFactory);
            IsInitialized = true;

            if (!forceReload)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var backgroundScope = _serviceProvider.CreateScope();
                        var backgroundSyncService = backgroundScope.ServiceProvider.GetRequiredService<ISyncService>();
                        var backgroundLocalFactory = backgroundScope.ServiceProvider.GetRequiredService<IDbContextFactory<LocalAppDbContext>>();

                        await backgroundSyncService.SyncDeltaAsync();
                        await LoadFromLocalCacheAsync(backgroundLocalFactory);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Cache Refresh Error]: {ex.Message}");
                    }
                });
            }
        }
        catch
        {
            ReportProgress(0, "Lỗi nạp dữ liệu.");
            throw;
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    private async Task LoadFromLocalCacheAsync(IDbContextFactory<LocalAppDbContext> localFactory)
    {
        await using var local = await localFactory.CreateDbContextAsync();
        await local.Database.EnsureCreatedAsync();

        ReportProgress(20, "Đang nạp học kỳ từ bộ nhớ cục bộ...");
        DanhSachHocKy = await local.HocKys
            .AsNoTracking()
            .OrderByDescending(h => h.DangMo)
            .ThenByDescending(h => h.NgayBatDau)
            .ToListAsync();

        ReportProgress(45, "Đang nạp môn học từ bộ nhớ cục bộ...");
        DanhSachMonHoc = await local.MonHocs
            .AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.TenMon)
            .ToListAsync();

        ReportProgress(70, "Đang nạp lớp sinh hoạt từ bộ nhớ cục bộ...");
        DanhSachLopSinhHoat = await local.SinhViens
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.LopSinhHoat != null && s.LopSinhHoat != "")
            .Select(s => s.LopSinhHoat!)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync();

        ReportProgress(90, "Đang nạp khóa học từ bộ nhớ cục bộ...");
        DanhSachKhoaHoc = await local.SinhViens
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.KhoaHoc != null && s.KhoaHoc != "")
            .Select(s => s.KhoaHoc!)
            .Distinct()
            .OrderBy(k => k)
            .ToListAsync();

        ReportProgress(100, "Hoàn tất nạp dữ liệu.");
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.Services;

public class MemoryCacheStore : IMemoryCacheStore
{
    private readonly IServiceProvider _serviceProvider;

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

    /// <summary>
    /// Trả về list gốc nếu khác null, ngược lại trả về mảng rỗng.
    /// Ép về IReadOnlyList&lt;T&gt; để tránh lỗi CS0019 khi dùng toán tử ??.
    /// </summary>
    private static IReadOnlyList<T> OrEmpty<T>(List<T>? list)
        => list ?? (IReadOnlyList<T>)Array.Empty<T>();

    public async Task InitializeAsync(bool forceReload = false)
    {
        if (IsInitialized && !forceReload) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var hocKyService    = scope.ServiceProvider.GetRequiredService<IHocKyService>();
            var sinhVienService = scope.ServiceProvider.GetRequiredService<ISinhVienService>();
            var monHocService   = scope.ServiceProvider.GetRequiredService<IMonHocService>();

            ReportProgress(10, "Đang nạp danh mục Học kỳ...");
            await LoadHocKysAsync(hocKyService);

            ReportProgress(40, "Đang nạp danh mục Môn học...");
            await LoadMonHocsAsync(monHocService);

            ReportProgress(70, "Đang nạp danh mục Lớp học phần...");
            await LoadLopHocPhansAsync(sinhVienService);

            ReportProgress(90, "Đang nạp cấu hình hệ thống...");
            await LoadMetadataAsync(sinhVienService);

            ReportProgress(100, "Hoàn tất nạp dữ liệu.");
            IsInitialized = true;
        }
        catch
        {
            ReportProgress(0, "Lỗi nạp dữ liệu.");
            throw;
        }
    }

    private async Task LoadHocKysAsync(IHocKyService service)
    {
        var list = await service.LayTatCaAsync();
        DanhSachHocKy = OrEmpty(list);
    }

    private async Task LoadMonHocsAsync(IMonHocService service)
    {
        var list = await service.LayDanhSachAsync(null, "TenMon");
        DanhSachMonHoc = OrEmpty(list);
    }

    private async Task LoadLopHocPhansAsync(ISinhVienService service)
    {
        // Theo dữ liệu hiện có của store: danh mục Lớp sinh hoạt.
        // Nếu sau này có ILopHocPhanService riêng thì chỉ cần đổi nguồn ở đây.
        var list = await service.GetDanhSachLopSinhHoatAsync();
        DanhSachLopSinhHoat = OrEmpty(list);
    }

    private async Task LoadMetadataAsync(ISinhVienService service)
    {
        var list = await service.GetDanhSachKhoaHocAsync();
        DanhSachKhoaHoc = OrEmpty(list);
    }
}
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

    public bool IsInitialized { get; private set; }

    public MemoryCacheStore(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync(bool forceReload = false)
    {
        if (IsInitialized && !forceReload) return;

        using var scope = _serviceProvider.CreateScope();
        var hocKyService = scope.ServiceProvider.GetRequiredService<IHocKyService>();
        var sinhVienService = scope.ServiceProvider.GetRequiredService<ISinhVienService>();
        var monHocService = scope.ServiceProvider.GetRequiredService<IMonHocService>();

        var taskHocKy = hocKyService.LayTatCaAsync();
        var taskLop = sinhVienService.GetDanhSachLopSinhHoatAsync();
        var taskKhoa = sinhVienService.GetDanhSachKhoaHocAsync();
        var taskMon = monHocService.LayDanhSachAsync(null, "TenMon");

        await Task.WhenAll(taskHocKy, taskLop, taskKhoa, taskMon);

        var hocKyList = await taskHocKy;
        var lopList = await taskLop;
        var khoaList = await taskKhoa;
        var monList = await taskMon;

        DanhSachHocKy = hocKyList != null ? hocKyList : Array.Empty<HocKy>();
        DanhSachLopSinhHoat = lopList != null ? lopList : Array.Empty<string>();
        DanhSachKhoaHoc = khoaList != null ? khoaList : Array.Empty<string>();
        DanhSachMonHoc = monList != null ? monList : Array.Empty<MonHoc>();

        IsInitialized = true;
    }
}
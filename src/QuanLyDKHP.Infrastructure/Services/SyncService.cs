using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Services;

public class SyncService : ISyncService
{
    private static readonly string[] MasterTables =
    [
        nameof(SinhVien),
        nameof(MonHoc),
        nameof(HocKy),
        nameof(LopHocPhan),
        nameof(DangKyHocPhan)
    ];

    private readonly IDbContextFactory<AppDbContext> _remoteFactory;
    private readonly IDbContextFactory<LocalAppDbContext> _localFactory;

    public SyncService(
        IDbContextFactory<AppDbContext> remoteFactory,
        IDbContextFactory<LocalAppDbContext> localFactory)
    {
        _remoteFactory = remoteFactory;
        _localFactory = localFactory;
    }

    public async Task<bool> HasLocalDataAsync()
    {
        await using var local = await _localFactory.CreateDbContextAsync();
        await local.Database.EnsureCreatedAsync();

        return await local.SyncMetadatas.AnyAsync()
            && await local.HocKys.AnyAsync()
            && await local.MonHocs.AnyAsync();
    }

    public async Task InitialSeedAsync(IProgress<(double Percent, string Status)>? progress = null)
    {
        if (await HasLocalDataAsync()) return;
        await RefreshLocalStoreAsync(progress);
    }

    public async Task ForceFullRefreshAsync(IProgress<(double Percent, string Status)>? progress = null)
    {
        await RefreshLocalStoreAsync(progress);
    }

    public async Task SyncDeltaAsync()
    {
        try
        {
            await using var local = await _localFactory.CreateDbContextAsync();
            await local.Database.EnsureCreatedAsync();

            if (!await HasLocalDataAsync())
            {
                await InitialSeedAsync();
                return;
            }

            await using var remote = await _remoteFactory.CreateDbContextAsync();
            var syncStartedAt = DateTime.UtcNow;
            var hocKyLastSync = await GetLastSyncUtcAsync(local, nameof(HocKy));
            var monHocLastSync = await GetLastSyncUtcAsync(local, nameof(MonHoc));
            var sinhVienLastSync = await GetLastSyncUtcAsync(local, nameof(SinhVien));
            var lopHocPhanLastSync = await GetLastSyncUtcAsync(local, nameof(LopHocPhan));
            var dangKyHocPhanLastSync = await GetLastSyncUtcAsync(local, nameof(DangKyHocPhan));

            await using var transaction = await local.Database.BeginTransactionAsync();

            await SyncHocKysAsync(
                local,
                await remote.HocKys.AsNoTracking()
                    .Where(e => e.NgayTao > hocKyLastSync
                        || (e.NgayCapNhat != null && e.NgayCapNhat > hocKyLastSync))
                    .ToListAsync());

            await SyncMonHocsAsync(
                local,
                await remote.MonHocs.AsNoTracking()
                    .Where(e => e.NgayTao > monHocLastSync
                        || (e.NgayCapNhat != null && e.NgayCapNhat > monHocLastSync))
                    .ToListAsync());

            await SyncSinhViensAsync(
                local,
                await remote.SinhViens.AsNoTracking()
                    .Where(e => e.NgayTao > sinhVienLastSync
                        || (e.NgayCapNhat != null && e.NgayCapNhat > sinhVienLastSync))
                    .ToListAsync());

            await SyncLopHocPhansAsync(
                local,
                await remote.LopHocPhans.AsNoTracking()
                    .Where(e => e.NgayTao > lopHocPhanLastSync
                        || (e.NgayCapNhat != null && e.NgayCapNhat > lopHocPhanLastSync))
                    .ToListAsync());

            await SyncDangKyHocPhansAsync(
                local,
                await remote.DangKyHocPhans.AsNoTracking()
                    .Where(e => e.NgayTao > dangKyHocPhanLastSync
                        || (e.NgayCapNhat != null && e.NgayCapNhat > dangKyHocPhanLastSync))
                    .ToListAsync());

            foreach (var table in MasterTables)
            {
                await UpsertMetadataAsync(local, table, syncStartedAt);
            }

            await local.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Local Sync Error]: {ex.Message}");
        }
    }

    private async Task RefreshLocalStoreAsync(IProgress<(double Percent, string Status)>? progress)
    {
        await using var remote = await _remoteFactory.CreateDbContextAsync();
        await using var local = await _localFactory.CreateDbContextAsync();
        await local.Database.EnsureDeletedAsync();
        await local.Database.EnsureCreatedAsync();

        var syncStartedAt = DateTime.UtcNow;
        await using var transaction = await local.Database.BeginTransactionAsync();

        progress?.Report((10, "Đang tải học kỳ từ máy chủ..."));
        var hocKys = await remote.HocKys.AsNoTracking().ToListAsync();
        await local.HocKys.AddRangeAsync(hocKys.Select(CloneHocKy));

        progress?.Report((30, "Đang tải môn học từ máy chủ..."));
        var monHocs = await remote.MonHocs.AsNoTracking().ToListAsync();
        await local.MonHocs.AddRangeAsync(monHocs.Select(CloneMonHoc));

        progress?.Report((50, "Đang tải sinh viên từ máy chủ..."));
        var sinhViens = await remote.SinhViens.AsNoTracking().ToListAsync();
        await local.SinhViens.AddRangeAsync(sinhViens.Select(CloneSinhVien));

        progress?.Report((70, "Đang tải lớp học phần từ máy chủ..."));
        var lopHocPhans = await remote.LopHocPhans.AsNoTracking().ToListAsync();
        await local.LopHocPhans.AddRangeAsync(lopHocPhans.Select(CloneLopHocPhan));

        progress?.Report((85, "Đang tải đăng ký học phần từ máy chủ..."));
        var dangKyHocPhans = await remote.DangKyHocPhans.AsNoTracking().ToListAsync();
        await local.DangKyHocPhans.AddRangeAsync(dangKyHocPhans.Select(CloneDangKyHocPhan));

        foreach (var table in MasterTables)
        {
            await local.SyncMetadatas.AddAsync(new SyncMetadata
            {
                TableName = table,
                LastSyncUtc = syncStartedAt,
                RecordCount = GetRecordCount(table, hocKys, monHocs, sinhViens, lopHocPhans, dangKyHocPhans)
            });
        }

        await local.SaveChangesAsync();
        await transaction.CommitAsync();
        progress?.Report((100, "Hoàn tất đồng bộ dữ liệu cục bộ."));
    }

    private static async Task<DateTime> GetLastSyncUtcAsync(LocalAppDbContext local, string tableName)
    {
        var metadata = await local.SyncMetadatas.FindAsync(tableName);
        return metadata?.LastSyncUtc ?? DateTime.MinValue;
    }

    private static async Task UpsertMetadataAsync(LocalAppDbContext local, string tableName, DateTime syncStartedAt)
    {
        var metadata = await local.SyncMetadatas.FindAsync(tableName);
        if (metadata == null)
        {
            metadata = new SyncMetadata { TableName = tableName };
            await local.SyncMetadatas.AddAsync(metadata);
        }

        metadata.LastSyncUtc = syncStartedAt;
        metadata.RecordCount = tableName switch
        {
            nameof(SinhVien) => await local.SinhViens.CountAsync(),
            nameof(MonHoc) => await local.MonHocs.CountAsync(),
            nameof(HocKy) => await local.HocKys.CountAsync(),
            nameof(LopHocPhan) => await local.LopHocPhans.CountAsync(),
            nameof(DangKyHocPhan) => await local.DangKyHocPhans.CountAsync(),
            _ => 0
        };
    }

    private static async Task SyncSinhViensAsync(LocalAppDbContext local, IEnumerable<SinhVien> items)
    {
        foreach (var item in items)
        {
            var existing = await local.SinhViens.FindAsync(item.MaSV);
            if (existing == null)
            {
                await local.SinhViens.AddAsync(CloneSinhVien(item));
            }
            else
            {
                local.Entry(existing).CurrentValues.SetValues(CloneSinhVien(item));
            }
        }
    }

    private static async Task SyncMonHocsAsync(LocalAppDbContext local, IEnumerable<MonHoc> items)
    {
        foreach (var item in items)
        {
            var existing = await local.MonHocs.FindAsync(item.MaMon);
            if (existing == null)
            {
                await local.MonHocs.AddAsync(CloneMonHoc(item));
            }
            else
            {
                local.Entry(existing).CurrentValues.SetValues(CloneMonHoc(item));
            }
        }
    }

    private static async Task SyncHocKysAsync(LocalAppDbContext local, IEnumerable<HocKy> items)
    {
        foreach (var item in items)
        {
            var existing = await local.HocKys.FindAsync(item.MaHocKy);
            if (existing == null)
            {
                await local.HocKys.AddAsync(CloneHocKy(item));
            }
            else
            {
                local.Entry(existing).CurrentValues.SetValues(CloneHocKy(item));
            }
        }
    }

    private static async Task SyncLopHocPhansAsync(LocalAppDbContext local, IEnumerable<LopHocPhan> items)
    {
        foreach (var item in items)
        {
            var existing = await local.LopHocPhans.FindAsync(item.MaLHP);
            if (existing == null)
            {
                await local.LopHocPhans.AddAsync(CloneLopHocPhan(item));
            }
            else
            {
                local.Entry(existing).CurrentValues.SetValues(CloneLopHocPhan(item));
            }
        }
    }

    private static async Task SyncDangKyHocPhansAsync(LocalAppDbContext local, IEnumerable<DangKyHocPhan> items)
    {
        foreach (var item in items)
        {
            var existing = await local.DangKyHocPhans.FindAsync(item.Id);
            if (existing == null)
            {
                await local.DangKyHocPhans.AddAsync(CloneDangKyHocPhan(item));
            }
            else
            {
                local.Entry(existing).CurrentValues.SetValues(CloneDangKyHocPhan(item));
            }
        }
    }

    private static int GetRecordCount(
        string tableName,
        List<HocKy> hocKys,
        List<MonHoc> monHocs,
        List<SinhVien> sinhViens,
        List<LopHocPhan> lopHocPhans,
        List<DangKyHocPhan> dangKyHocPhans)
    {
        return tableName switch
        {
            nameof(SinhVien) => sinhViens.Count,
            nameof(MonHoc) => monHocs.Count,
            nameof(HocKy) => hocKys.Count,
            nameof(LopHocPhan) => lopHocPhans.Count,
            nameof(DangKyHocPhan) => dangKyHocPhans.Count,
            _ => 0
        };
    }

    private static SinhVien CloneSinhVien(SinhVien item) => new()
    {
        MaSV = item.MaSV,
        HoTen = item.HoTen,
        LopSinhHoat = item.LopSinhHoat,
        KhoaHoc = item.KhoaHoc,
        NgayTao = item.NgayTao,
        NgayCapNhat = item.NgayCapNhat,
        IsDeleted = item.IsDeleted
    };

    private static MonHoc CloneMonHoc(MonHoc item) => new()
    {
        MaMon = item.MaMon,
        TenMon = item.TenMon,
        SoTinChiLT = item.SoTinChiLT,
        SoTinChiTH = item.SoTinChiTH,
        BacDaoTao = item.BacDaoTao,
        NgayTao = item.NgayTao,
        NgayCapNhat = item.NgayCapNhat,
        IsDeleted = item.IsDeleted
    };

    private static HocKy CloneHocKy(HocKy item) => new()
    {
        MaHocKy = item.MaHocKy,
        TenHocKy = item.TenHocKy,
        NgayBatDau = item.NgayBatDau,
        NgayKetThuc = item.NgayKetThuc,
        DangMo = item.DangMo,
        NgayTao = item.NgayTao,
        NgayCapNhat = item.NgayCapNhat
    };

    private static LopHocPhan CloneLopHocPhan(LopHocPhan item) => new()
    {
        MaLHP = item.MaLHP,
        MaMon = item.MaMon,
        MaHocKy = item.MaHocKy,
        MaGV = item.MaGV,
        LoaiHinhDT = item.LoaiHinhDT,
        SiSoToiDa = item.SiSoToiDa,
        GiangDayOnline = item.GiangDayOnline,
        Thu = item.Thu,
        TietBatDau = item.TietBatDau,
        SoTiet = item.SoTiet,
        Phong = item.Phong,
        NgayTao = item.NgayTao,
        NgayCapNhat = item.NgayCapNhat,
        IsDeleted = item.IsDeleted
    };

    private static DangKyHocPhan CloneDangKyHocPhan(DangKyHocPhan item) => new()
    {
        Id = item.Id,
        MaSV = item.MaSV,
        MaLHP = item.MaLHP,
        HinhThucDK = item.HinhThucDK,
        NgayDK = item.NgayDK,
        NguoiDK = item.NguoiDK,
        DiemSo = item.DiemSo,
        DiemChu = item.DiemChu,
        TrangThai = item.TrangThai,
        SoTienPhaiDong = item.SoTienPhaiDong,
        SoTienDaDong = item.SoTienDaDong,
        NgayTao = item.NgayTao,
        NgayCapNhat = item.NgayCapNhat
    };
}

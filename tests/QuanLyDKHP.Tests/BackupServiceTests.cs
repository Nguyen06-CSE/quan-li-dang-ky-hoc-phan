using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Infrastructure.Data;
using QuanLyDKHP.Infrastructure.Services;
using Xunit;

namespace QuanLyDKHP.Tests;

public class BackupServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _dbPath;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    private class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public AppDbContext CreateDbContext() => new(_options);
    }

    public BackupServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "DKHP_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _dbPath = Path.Combine(_testDir, "test.db");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _dbContextFactory = new TestDbContextFactory(options);

        // Khởi tạo bảng
        using var db = _dbContextFactory.CreateDbContext();
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch { }
    }

    [Fact]
    public void BackupCommitInfoDto_ShortSha_ReturnsFirst7Characters()
    {
        var dto = new BackupCommitInfoDto
        {
            CommitSha = "4a5b6c7d8e9f0123456789abcdef0123456789ab"
        };

        Assert.Equal("4a5b6c7", dto.ShortSha);
    }

    [Fact]
    public void BackupDatabaseStatusDto_FormattedSize_CalculatesCorrectly()
    {
        var dtoSmall = new BackupDatabaseStatusDto { DatabaseSizeBytes = 512 };
        Assert.Equal("512 B", dtoSmall.FormattedSize);

        var dtoKb = new BackupDatabaseStatusDto { DatabaseSizeBytes = 2048 };
        Assert.Equal("2.0 KB", dtoKb.FormattedSize);

        var dtoMb = new BackupDatabaseStatusDto { DatabaseSizeBytes = 5 * 1024 * 1024 };
        Assert.Equal("5.00 MB", dtoMb.FormattedSize);
    }

    [Fact]
    public async Task CreateLocalGitBackup_And_Restore_WorksEndToEnd()
    {
        var backupService = new BackupService(_dbContextFactory, null, _testDir);

        // 1. Thêm 1 sinh viên vào DB
        using (var db = _dbContextFactory.CreateDbContext())
        {
            db.SinhViens.Add(new SinhVien
            {
                MaSV = "SV_TEST_01",
                HoTen = "Nguyen Van Test",
                LopSinhHoat = "CNTT1",
                KhoaHoc = "K26"
            });
            await db.SaveChangesAsync();
        }

        // 2. Tạo bản sao lưu Git đầu tiên
        var commit1 = await backupService.CreateLocalGitBackupAsync("Manual", "Commit 1 with SV01");
        Assert.NotNull(commit1);
        Assert.False(string.IsNullOrEmpty(commit1.CommitSha));

        // 3. Kiểm tra lịch sử
        var history = await backupService.GetCommitHistoryAsync();
        Assert.NotEmpty(history);
        Assert.Equal(commit1.CommitSha, history[0].CommitSha);

        // 4. Thêm sinh viên thứ hai
        using (var db = _dbContextFactory.CreateDbContext())
        {
            db.SinhViens.Add(new SinhVien
            {
                MaSV = "SV_TEST_02",
                HoTen = "Tran Thi Test",
                LopSinhHoat = "CNTT2",
                KhoaHoc = "K26"
            });
            await db.SaveChangesAsync();
        }

        var commit2 = await backupService.CreateLocalGitBackupAsync("Manual", "Commit 2 with SV02");
        Assert.NotNull(commit2);

        // 5. Khôi phục về commit 1 (chỉ có SV_TEST_01)
        await backupService.RestoreFromGitCommitAsync(commit1.CommitSha);

        // 6. Xác minh SV_TEST_02 đã biến mất và SV_TEST_01 vẫn tồn tại
        using (var db = _dbContextFactory.CreateDbContext())
        {
            var sv1 = await db.SinhViens.FindAsync("SV_TEST_01");
            var sv2 = await db.SinhViens.FindAsync("SV_TEST_02");

            Assert.NotNull(sv1);
            Assert.Null(sv2);
        }
    }

    [Fact]
    public async Task GitBackup_CanonicalJsonl_LineLevelDeltaDiff_TracksExactRowChanges()
    {
        var backupService = new BackupService(_dbContextFactory, null, _testDir);

        // 1. Chèn 3 sinh viên theo thứ tự ngẫu nhiên (SV003 -> SV001 -> SV002)
        using (var db = _dbContextFactory.CreateDbContext())
        {
            db.SinhViens.AddRange(
                new SinhVien { MaSV = "SV003", HoTen = "Lê Văn C", LopSinhHoat = "CNTT1", KhoaHoc = "K26" },
                new SinhVien { MaSV = "SV001", HoTen = "Nguyễn Văn A", LopSinhHoat = "CNTT1", KhoaHoc = "K26" },
                new SinhVien { MaSV = "SV002", HoTen = "Trần Thị B", LopSinhHoat = "CNTT1", KhoaHoc = "K26" }
            );
            await db.SaveChangesAsync();
        }

        // Tạo Commit 1 (Baseline)
        var commit1 = await backupService.CreateLocalGitBackupAsync("Auto-Daily", "Baseline ngày 1");
        Assert.NotNull(commit1);
        Assert.Contains("SinhVien (+3)", commit1.ChangedTablesSummary);

        // Kiểm tra file SinhVien.jsonl đã được chuẩn hóa thứ tự (ORDER BY MaSV: SV001 -> SV002 -> SV003)
        var svJsonlPath = Path.Combine(_testDir, "GitRepo", "tables", "SinhVien.jsonl");
        var linesDay1 = await File.ReadAllLinesAsync(svJsonlPath);
        Assert.Equal(3, linesDay1.Length);
        Assert.Contains("\"MaSV\":\"SV001\"", linesDay1[0]);
        Assert.Contains("\"MaSV\":\"SV002\"", linesDay1[1]);
        Assert.Contains("\"MaSV\":\"SV003\"", linesDay1[2]);

        // 2. Nếu gọi lại Auto-Daily khi không có dữ liệu gì thay đổi (skipIfNoChanges: true) -> Bỏ qua, không tạo commit rác
        var skippedCommit = await backupService.CreateLocalGitBackupAsync("Auto-Daily", "Không đổi", skipIfNoChanges: true);
        Assert.Null(skippedCommit);

        // 3. Ngày 2: Sửa lớp của SV002 ("CNTT1" -> "KTPM2") và thêm mới SV004
        using (var db = _dbContextFactory.CreateDbContext())
        {
            var sv2 = await db.SinhViens.FindAsync("SV002");
            Assert.NotNull(sv2);
            sv2.LopSinhHoat = "KTPM2";

            db.SinhViens.Add(new SinhVien { MaSV = "SV004", HoTen = "Phạm Văn D", LopSinhHoat = "HTTT1", KhoaHoc = "K27" });
            await db.SaveChangesAsync();
        }

        // Tạo Commit 2 (Delta): Sửa 1 dòng (-1 dòng cũ, +1 dòng mới) + Thêm 1 dòng mới (+1) => Tổng cộng (+2, -1) trên bảng SinhVien
        var commit2 = await backupService.CreateLocalGitBackupAsync("Auto-Daily", "Cập nhật cuối ngày 2");
        Assert.NotNull(commit2);
        Assert.Equal("SinhVien (+2, -1)", commit2.ChangedTablesSummary);
    }

    [Fact]
    public async Task RestoreFromGitCommit_CreatesPreRestoreSafetyCommit()
    {
        var backupService = new BackupService(_dbContextFactory, null, _testDir);

        // 1. Tạo Commit 1 với SV001
        using (var db = _dbContextFactory.CreateDbContext())
        {
            db.SinhViens.Add(new SinhVien { MaSV = "SV001", HoTen = "Nguyễn Văn A", LopSinhHoat = "CNTT1", KhoaHoc = "K26" });
            await db.SaveChangesAsync();
        }
        var commit1 = await backupService.CreateLocalGitBackupAsync("Manual", "Bản lưu 1");
        Assert.NotNull(commit1);

        // 2. Thêm SV002 vào DB nhưng CHƯA bấm sao lưu thủ công
        using (var db = _dbContextFactory.CreateDbContext())
        {
            db.SinhViens.Add(new SinhVien { MaSV = "SV002", HoTen = "Trần Thị B", LopSinhHoat = "CNTT2", KhoaHoc = "K26" });
            await db.SaveChangesAsync();
        }

        // 3. Khôi phục về Commit 1 -> Hệ thống phải tự động chụp 1 bản [Pre-Restore] chứa SV002 trước khi quay về Commit 1
        await backupService.RestoreFromGitCommitAsync(commit1.CommitSha);

        var history = await backupService.GetCommitHistoryAsync();
        Assert.Contains(history, c => c.BackupType == "Pre-Restore");

        // Và nếu người dùng đổi ý, có thể khôi phục lại ngay bản Pre-Restore để lấy lại SV002!
        var safetyCommit = System.Linq.Enumerable.First(history, c => c.BackupType == "Pre-Restore");
        await backupService.RestoreFromGitCommitAsync(safetyCommit.CommitSha);

        using (var dbAfterUndo = _dbContextFactory.CreateDbContext())
        {
            Assert.NotNull(await dbAfterUndo.SinhViens.FindAsync("SV001"));
            Assert.NotNull(await dbAfterUndo.SinhViens.FindAsync("SV002"));
        }
    }
}

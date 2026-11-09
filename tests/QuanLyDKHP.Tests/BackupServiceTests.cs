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
}

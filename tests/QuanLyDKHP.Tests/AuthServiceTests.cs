using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Services;
using Xunit;

namespace QuanLyDKHP.Tests;

public class AuthServiceTests
{
    private readonly Mock<INguoiDungRepository> _mockRepo;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockRepo = new Mock<INguoiDungRepository>();
        _authService = new AuthService(_mockRepo.Object);
    }

    [Fact]
    public async Task DangNhapAsync_EmptyCredentials_ReturnsNull()
    {
        var result1 = await _authService.DangNhapAsync("", "password");
        var result2 = await _authService.DangNhapAsync("admin", "");

        Assert.Null(result1);
        Assert.Null(result2);
    }

    [Fact]
    public async Task DangNhapAsync_UserNotFound_ReturnsNull()
    {
        _mockRepo.Setup(r => r.GetByTenDangNhapAsync("unknown"))
                 .ReturnsAsync((NguoiDung?)null);

        var result = await _authService.DangNhapAsync("unknown", "password");

        Assert.Null(result);
    }

    [Fact]
    public async Task DangNhapAsync_WrongPassword_ReturnsNull()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("correct_password");
        var user = new NguoiDung
        {
            Id = 1,
            TenDangNhap = "admin",
            MatKhauHash = hash,
            Role = "Admin"
        };

        _mockRepo.Setup(r => r.GetByTenDangNhapAsync("admin"))
                 .ReturnsAsync(user);

        var result = await _authService.DangNhapAsync("admin", "wrong_password");

        Assert.Null(result);
    }

    [Fact]
    public async Task DangNhapAsync_WithSqliteDatabase_AdminUser_Succeeds()
    {
        var tempDb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AuthTest_" + System.Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<QuanLyDKHP.Infrastructure.Data.AppDbContext>()
                .UseSqlite($"Data Source={tempDb}")
                .Options;

            var factoryMock = new Mock<Microsoft.EntityFrameworkCore.IDbContextFactory<QuanLyDKHP.Infrastructure.Data.AppDbContext>>();
            factoryMock.Setup(f => f.CreateDbContextAsync(default))
                .ReturnsAsync(() => new QuanLyDKHP.Infrastructure.Data.AppDbContext(options));

            using (var db = new QuanLyDKHP.Infrastructure.Data.AppDbContext(options))
            {
                await QuanLyDKHP.Infrastructure.Data.AppDbContext.EnsureTablesAndSeedAsync(db);
            }

            var repo = new QuanLyDKHP.Infrastructure.Repositories.NguoiDungRepository(factoryMock.Object);
            var authService = new AuthService(repo);

            var user = await authService.DangNhapAsync("admin", "admin");

            Assert.NotNull(user);
            Assert.Equal("admin", user.TenDangNhap);
            Assert.Equal("Admin", user.Role);
        }
        finally
        {
            if (System.IO.File.Exists(tempDb)) System.IO.File.Delete(tempDb);
        }
    }

    [Fact]
    public async Task DangNhapAsync_ValidPassword_ReturnsUser()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("correct_password");
        var user = new NguoiDung
        {
            Id = 1,
            TenDangNhap = "admin",
            MatKhauHash = hash,
            Role = "Admin"
        };

        _mockRepo.Setup(r => r.GetByTenDangNhapAsync("admin"))
                 .ReturnsAsync(user);

        var result = await _authService.DangNhapAsync("admin", "correct_password");

        Assert.NotNull(result);
        Assert.Equal("admin", result!.TenDangNhap);
    }

    [Fact]
    public async Task DangNhapAsync_LockedUser_ReturnsUser()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("correct_password");
        var user = new NguoiDung
        {
            Id = 2,
            TenDangNhap = "locked_user",
            MatKhauHash = hash,
            Role = "GiangVien",
            TrangThai = "DaKhoa"
        };

        _mockRepo.Setup(r => r.GetByTenDangNhapAsync("locked_user"))
                 .ReturnsAsync(user);

        var result = await _authService.DangNhapAsync("locked_user", "correct_password");

        // AuthService trả về user để ViewModel nhận biết được trạng thái DaKhoa
        Assert.NotNull(result);
        Assert.Equal("DaKhoa", result!.TrangThai);
    }

    [Fact]
    public async Task DangNhapAsync_WhitespaceUsername_Trimmed()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("correct_password");
        var user = new NguoiDung
        {
            Id = 1,
            TenDangNhap = "admin",
            MatKhauHash = hash,
            Role = "Admin"
        };

        _mockRepo.Setup(r => r.GetByTenDangNhapAsync("admin"))
                 .ReturnsAsync(user);

        var result = await _authService.DangNhapAsync("  admin  ", "correct_password");

        Assert.NotNull(result);
        Assert.Equal("admin", result!.TenDangNhap);
    }
}

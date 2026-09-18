using System.Threading.Tasks;
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

}

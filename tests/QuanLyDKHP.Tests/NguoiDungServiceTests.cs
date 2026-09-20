using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Enums;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Services;
using Xunit;

namespace QuanLyDKHP.Tests;

public class NguoiDungServiceTests
{
    private readonly Mock<INguoiDungRepository> _mockRepo;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly NguoiDungService _service;

    public NguoiDungServiceTests()
    {
        _mockRepo = new Mock<INguoiDungRepository>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _service = new NguoiDungService(_mockRepo.Object, _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task ThemAsync_DuplicateUsername_ThrowsInvalidOperationException()
    {
        var existing = new NguoiDung { Id = 1, TenDangNhap = "admin" };
        _mockRepo.Setup(r => r.GetByTenDangNhapAsync("admin")).ReturnsAsync(existing);

        var newUser = new NguoiDung { TenDangNhap = "admin", HoTen = "Admin User" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ThemAsync(newUser, "123456"));
    }

    [Fact]
    public async Task ThemAsync_ValidUser_HashesPasswordAndCallsRepo()
    {
        _mockRepo.Setup(r => r.GetByTenDangNhapAsync("newuser")).ReturnsAsync((NguoiDung?)null);

        var newUser = new NguoiDung { TenDangNhap = "newuser", HoTen = "New User", Role = "GiangVien", MaGV = "GV01" };

        await _service.ThemAsync(newUser, "password123");

        _mockRepo.Verify(r => r.ThemAsync(It.Is<NguoiDung>(u => 
            u.TenDangNhap == "newuser" && 
            !string.IsNullOrEmpty(u.MatKhauHash)
        )), Times.Once);
    }

    [Fact]
    public async Task DoiMatKhauAsync_WrongOldPassword_ThrowsInvalidOperationException()
    {
        var userInDb = new NguoiDung 
        { 
            Id = 3, 
            TenDangNhap = "gv01", 
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword("correct_old_pass") 
        };
        _mockRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(userInDb);

        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.DoiMatKhauAsync(3, "wrong_old_pass", "new_pass_123"));
    }

    [Fact]
    public async Task DoiMatKhauAsync_CorrectOldPassword_Success()
    {
        var userInDb = new NguoiDung 
        { 
            Id = 3, 
            TenDangNhap = "gv01", 
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword("correct_old_pass") 
        };
        _mockRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(userInDb);

        await _service.DoiMatKhauAsync(3, "correct_old_pass", "new_pass_123");

        _mockRepo.Verify(r => r.CapNhatAsync(It.Is<NguoiDung>(u => 
            u.Id == 3 && 
            !string.IsNullOrEmpty(u.MatKhauHash)
        )), Times.Once);
    }

    [Fact]
    public async Task DoiTrangThaiAsync_SelfLockAdmin_ThrowsInvalidOperationException()
    {
        var currentInfo = new CurrentUserInfo { Id = 2, TenDangNhap = "admin", Role = UserRole.Admin };
        _mockCurrentUserService.Setup(s => s.CurrentUser).Returns(currentInfo);

        var userInDb = new NguoiDung { Id = 2, TenDangNhap = "admin", Role = "Admin" };
        _mockRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(userInDb);

        // Cố gắng tự khóa chính tài khoản của mình
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DoiTrangThaiAsync(2, "DaKhoa"));
    }

    [Fact]
    public async Task DoiTrangThaiAsync_OtherUser_Success()
    {
        var currentInfo = new CurrentUserInfo { Id = 2, TenDangNhap = "admin", Role = UserRole.Admin };
        _mockCurrentUserService.Setup(s => s.CurrentUser).Returns(currentInfo);

        var targetUser = new NguoiDung { Id = 3, TenDangNhap = "gv01", Role = "GiangVien", TrangThai = "HoatDong" };
        _mockRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(targetUser);

        await _service.DoiTrangThaiAsync(3, "DaKhoa");

        Assert.Equal("DaKhoa", targetUser.TrangThai);
        _mockRepo.Verify(r => r.CapNhatAsync(targetUser), Times.Once);
    }
}

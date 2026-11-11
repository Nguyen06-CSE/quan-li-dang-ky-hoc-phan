using System;
using System.Threading.Tasks;
using Moq;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Services;
using Xunit;

namespace QuanLyDKHP.Tests;

public class HocKyServiceTests
{
    [Fact]
    public void HocKy_Equality_WorksCorrectlyForComboBox()
    {
        var hk1 = new HocKy { MaHocKy = "HK20261", TenHocKy = "Học kỳ 1 2026" };
        var hk2 = new HocKy { MaHocKy = "hk20261", TenHocKy = "HK1 2026 (Khác tên)" };
        var hk3 = new HocKy { MaHocKy = "HK20262", TenHocKy = "Học kỳ 2 2026" };

        Assert.Equal(hk1, hk2);
        Assert.True(hk1 == hk2);
        Assert.Equal(hk1.GetHashCode(), hk2.GetHashCode());
        Assert.NotEqual(hk1, hk3);
        Assert.True(hk1 != hk3);
    }

    [Fact]
    public async Task HocKyService_CapNhatAsync_CanCloseSemester()
    {
        var mockRepo = new Mock<IHocKyRepository>();
        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(u => u.CurrentUser).Returns(new CurrentUserInfo { Role = QuanLyDKHP.Core.Enums.UserRole.Admin });

        var existingHk = new HocKy
        {
            MaHocKy = "HK20261",
            TenHocKy = "Học kỳ 1 2026",
            DangMo = true
        };

        mockRepo.Setup(r => r.GetByIdAsync("HK20261")).ReturnsAsync(existingHk);

        var service = new HocKyService(mockRepo.Object, mockCurrentUser.Object);

        // Đóng học kỳ
        var updatedHk = new HocKy
        {
            MaHocKy = "HK20261",
            TenHocKy = "Học kỳ 1 2026 (Đã đóng)",
            DangMo = false
        };

        await service.CapNhatAsync(updatedHk);

        // Đảm bảo existingHk.DangMo được chuyển thành false và gọi CapNhatAsync
        Assert.False(existingHk.DangMo);
        mockRepo.Verify(r => r.CapNhatAsync(It.Is<HocKy>(h => h.DangMo == false)), Times.Once);
        // Không được gọi DatHocKyHienHanhAsync khi đóng học kỳ
        mockRepo.Verify(r => r.DatHocKyHienHanhAsync(It.IsAny<string>()), Times.Never);
    }
}

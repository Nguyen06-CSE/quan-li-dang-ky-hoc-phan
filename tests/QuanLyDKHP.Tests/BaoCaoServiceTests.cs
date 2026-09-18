using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Enums;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Services;
using Xunit;

namespace QuanLyDKHP.Tests;

public class BaoCaoServiceTests
{
    private readonly Mock<IDashboardRepository> _mockRepo;
    private readonly BaoCaoService _baoCaoService;

    public BaoCaoServiceTests()
    {
        _mockRepo = new Mock<IDashboardRepository>();
        _baoCaoService = new BaoCaoService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_ReturnsCorrectStats()
    {
        var expectedStats = new DashboardStatsDto
        {
            TongSinhVien = 500,
            TongMonHoc = 30,
            TongLopHocPhan = 100,
            TongLuotDangKy = 4500
        };

        _mockRepo.Setup(r => r.GetStatsAsync("HK2026_1"))
                 .ReturnsAsync(expectedStats);

        var result = await _baoCaoService.GetDashboardStatsAsync("HK2026_1");

        Assert.NotNull(result);
        Assert.Equal(500, result.TongSinhVien);
        Assert.Equal(30, result.TongMonHoc);
        Assert.Equal(100, result.TongLopHocPhan);
        Assert.Equal(4500, result.TongLuotDangKy);
    }

    [Fact]
    public async Task GetLhpGiangVienAsync_EmptyMaGV_ReturnsEmptyList()
    {
        _mockRepo.Setup(r => r.GetLhpByGiangVienAsync("", "HK2026_1"))
                 .ReturnsAsync(new List<GiangVienLhpDto>());

        var result = await _baoCaoService.GetLhpGiangVienAsync("", "HK2026_1");

        Assert.Empty(result);
    }
}

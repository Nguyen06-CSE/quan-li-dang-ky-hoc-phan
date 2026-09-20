using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Services;
using Xunit;

namespace QuanLyDKHP.Tests;

public class CauHinhServiceTests
{
    private readonly Mock<ICauHinhRepository> _mockRepo;
    private readonly CauHinhService _service;

    public CauHinhServiceTests()
    {
        _mockRepo = new Mock<ICauHinhRepository>();
        _service = new CauHinhService(_mockRepo.Object);
    }

    [Fact]
    public async Task LayTatCaAsync_ReturnsDictionary()
    {
        var dict = new Dictionary<string, string>
        {
            ["SoTinChiToiThieu"] = "12",
            ["SoTinChiToiDa"] = "24",
            ["DonGiaTinChiLT"] = "550000",
            ["DonGiaTinChiTH"] = "750000"
        };

        _mockRepo.Setup(r => r.LayTatCaAsync()).ReturnsAsync(dict);

        var result = await _service.LayTatCaAsync();

        Assert.Equal(4, result.Count);
        Assert.Equal("12", result["SoTinChiToiThieu"]);
    }

    [Fact]
    public async Task CapNhatAsync_CallsRepository()
    {
        var dict = new Dictionary<string, string>
        {
            ["SoTinChiToiThieu"] = "14",
            ["SoTinChiToiDa"] = "28"
        };

        _mockRepo.Setup(r => r.CapNhatAsync(dict)).Returns(Task.CompletedTask);

        await _service.CapNhatAsync(dict);

        _mockRepo.Verify(r => r.CapNhatAsync(dict), Times.Once);
    }

    [Fact]
    public async Task GetInt_ReturnsValueOrFallback()
    {
        _mockRepo.Setup(r => r.LayGiaTriIntAsync("SoTinChiToiThieu", 10)).ReturnsAsync(15);

        var result = await _service.GetInt("SoTinChiToiThieu");

        Assert.Equal(15, result);
    }

    [Fact]
    public async Task GetDecimal_ReturnsValueOrFallback()
    {
        _mockRepo.Setup(r => r.LayGiaTriDecimalAsync("DonGiaTinChiLT", 500000m)).ReturnsAsync(600000m);

        var result = await _service.GetDecimal("DonGiaTinChiLT");

        Assert.Equal(600000m, result);
    }
}

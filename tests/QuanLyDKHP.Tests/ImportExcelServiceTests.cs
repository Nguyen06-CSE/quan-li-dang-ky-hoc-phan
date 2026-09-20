using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Services;
using Xunit;

namespace QuanLyDKHP.Tests;

public class ImportExcelServiceTests
{
    private readonly Mock<IImportExcelService> _mockRepo;
    private readonly ImportExcelService _service;

    public ImportExcelServiceTests()
    {
        _mockRepo = new Mock<IImportExcelService>();
        _service = new ImportExcelService(_mockRepo.Object);
    }

    [Fact]
    public async Task ImportFileAsync_DelegatesToRepository()
    {
        var expectedResult = new KetQuaImportDto
        {
            TongSoDong = 10,
            SoDongThanhCong = 10,
            SoDongLoi = 0,
            SoSinhVienMoi = 5,
            SoMonHocMoi = 2
        };

        using var ms = new MemoryStream();

        _mockRepo.Setup(r => r.ImportFileAsync(ms, "HK20251", null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expectedResult);

        var result = await _service.ImportFileAsync(ms, "HK20251");

        Assert.NotNull(result);
        Assert.Equal(10, result.TongSoDong);
        Assert.Equal(10, result.SoDongThanhCong);
        Assert.Equal(5, result.SoSinhVienMoi);
    }
}

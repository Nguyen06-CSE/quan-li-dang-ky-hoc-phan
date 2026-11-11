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
    private readonly Mock<IImportExcelRepository> _mockRepo;
    private readonly ImportExcelService _service;

    public ImportExcelServiceTests()
    {
        _mockRepo = new Mock<IImportExcelRepository>();
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

    [Fact]
    public async Task ImportService_CanImportWithoutSpecifyingSemester()
    {
        var expectedResult = new KetQuaImportDto
        {
            MaHocKy = "HK20261",
            TenHocKy = "Học kỳ 1 2026",
            TongSoDong = 5,
            SoDongThanhCong = 5
        };

        using var ms = new MemoryStream();

        _mockRepo.Setup(r => r.ImportFileAsync(ms, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expectedResult);

        var result = await _service.ImportFileAsync(ms);

        Assert.NotNull(result);
        Assert.Equal("HK20261", result.MaHocKy);
        Assert.Equal(5, result.SoDongThanhCong);
        _mockRepo.Verify(r => r.ImportFileAsync(ms, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("15/06/2026 9:43:00 SA", 2026, 6, 15, 9, 43, 0)]
    [InlineData("19/06/2026 2:00:00 CH", 2026, 6, 19, 14, 0, 0)]
    [InlineData("18/06/2026 9:28:00 CH", 2026, 6, 18, 21, 28, 0)]
    [InlineData("15/06/2026", 2026, 6, 15, 0, 0, 0)]
    public void VietnameseDate_WithSACH_ParsesAccurately(string input, int year, int month, int day, int hour, int minute, int second)
    {
        string cleanText = input.Trim();
        bool isPM = cleanText.Contains("CH", StringComparison.OrdinalIgnoreCase);
        bool isAM = cleanText.Contains("SA", StringComparison.OrdinalIgnoreCase);

        if (isPM || isAM)
        {
            cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"\bCH\b", "PM", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"\bSA\b", "AM", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        string[] formats = new[]
        {
            "d/M/yyyy h:mm:ss tt",
            "dd/MM/yyyy h:mm:ss tt",
            "d/M/yyyy hh:mm:ss tt",
            "dd/MM/yyyy hh:mm:ss tt",
            "d/M/yyyy h:mm tt",
            "dd/MM/yyyy h:mm tt",
            "d/M/yyyy hh:mm tt",
            "dd/MM/yyyy hh:mm tt",
            "d/M/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "d/M/yyyy HH:mm",
            "dd/MM/yyyy HH:mm",
            "d/M/yyyy",
            "dd/MM/yyyy",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd",
            "M/d/yyyy h:mm:ss tt",
            "MM/dd/yyyy h:mm:ss tt",
            "M/d/yyyy HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss"
        };

        const System.Globalization.DateTimeStyles st = System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal;
        bool success = DateTime.TryParseExact(cleanText, formats, System.Globalization.CultureInfo.InvariantCulture, st, out var parsedDate);

        Assert.True(success, $"Failed to parse date: {input}");
        Assert.Equal(year, parsedDate.Year);
        Assert.Equal(month, parsedDate.Month);
        Assert.Equal(day, parsedDate.Day);
        Assert.Equal(hour, parsedDate.Hour);
        Assert.Equal(minute, parsedDate.Minute);
        Assert.Equal(second, parsedDate.Second);
    }
}

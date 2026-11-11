using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;

namespace QuanLyDKHP.Core.Interfaces;

public interface ISmartImportService
{
    List<SinhVienImportPreviewDto> ParseClipboardOrText(string rawText, ColumnMappingDto? mapping = null);
    Task<List<SinhVienImportPreviewDto>> ParseExcelStreamAsync(Stream stream, ColumnMappingDto? mapping = null, CancellationToken cancellationToken = default);
    Task<ClassInsightsDto> AnalyzeAndValidateAsync(
        string maHocKy,
        string maLHP,
        List<SinhVienImportPreviewDto> items,
        bool tuDongTaoSv = true);
    Task<KetQuaSmartEnrollDto> EnrollStudentsAsync(
        string maHocKy,
        string maLHP,
        List<SinhVienImportPreviewDto> items,
        bool tuDongTaoSv = true,
        bool boQuaTrungMon = true,
        CancellationToken cancellationToken = default);
}

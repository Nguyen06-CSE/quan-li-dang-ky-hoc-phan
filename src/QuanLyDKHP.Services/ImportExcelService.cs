using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

/// <summary>
/// Tầng nghiệp vụ cho import Excel. Hiện chỉ kiểm tra đầu vào rồi chuyển xuống repository;
/// mọi thao tác đọc file/ghi DB nằm ở IImportExcelRepository.
/// </summary>
public class ImportExcelService : IImportExcelService
{
    private readonly IImportExcelRepository _importExcelRepository;

    public ImportExcelService(IImportExcelRepository importExcelRepository)
    {
        _importExcelRepository = importExcelRepository;
    }

    public Task<KetQuaImportDto> ImportFileAsync(
        Stream stream,
        string maHocKy,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return _importExcelRepository.ImportFileAsync(stream, maHocKy, progress, cancellationToken);
    }
}

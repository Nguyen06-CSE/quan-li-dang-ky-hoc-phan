using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

public class ImportExcelService : IImportExcelService
{
    private readonly IImportExcelService _importExcelRepository;

    public ImportExcelService(IImportExcelService importExcelRepository)
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

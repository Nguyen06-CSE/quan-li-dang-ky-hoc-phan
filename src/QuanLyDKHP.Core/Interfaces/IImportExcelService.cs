using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;

namespace QuanLyDKHP.Core.Interfaces;

public interface IImportExcelService
{
    Task<KetQuaImportDto> ImportFileAsync(
        Stream stream, 
        string maHocKy, 
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default);
}

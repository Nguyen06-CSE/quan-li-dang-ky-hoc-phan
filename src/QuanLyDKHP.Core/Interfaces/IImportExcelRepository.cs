using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;

namespace QuanLyDKHP.Core.Interfaces;

/// <summary>Tầng truy cập dữ liệu cho import Excel (đọc file + ghi DB theo lô).</summary>
public interface IImportExcelRepository
{
    Task<KetQuaImportDto> ImportFileAsync(
        Stream stream,
        string maHocKy,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}

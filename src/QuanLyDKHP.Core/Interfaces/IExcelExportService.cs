using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyDKHP.Core.Interfaces;

public interface IExcelExportService
{
    Task<byte[]> XuatExcelAsync<T>(string sheetName, IEnumerable<T> duLieu, List<(string TieuDe, Func<T, object?> LayGiaTri)> cotMap);
}

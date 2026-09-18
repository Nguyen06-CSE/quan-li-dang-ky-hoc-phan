using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyDKHP.Core.Interfaces;

public interface ICauHinhService
{
    Task<Dictionary<string, string>> LayTatCaAsync();
    Task CapNhatAsync(Dictionary<string, string> cauHinhMoi);
    Task<int> GetInt(string key);
    Task<decimal> GetDecimal(string key);
}

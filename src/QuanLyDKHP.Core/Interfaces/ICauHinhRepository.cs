using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyDKHP.Core.Interfaces;

public interface ICauHinhRepository
{
    Task<Dictionary<string, string>> LayTatCaAsync();
    Task<string?> LayGiaTriAsync(string key);
    Task<int> LayGiaTriIntAsync(string key, int defaultValue);
    Task<decimal> LayGiaTriDecimalAsync(string key, decimal defaultValue);
    Task CapNhatAsync(Dictionary<string, string> cauHinhMoi);
}

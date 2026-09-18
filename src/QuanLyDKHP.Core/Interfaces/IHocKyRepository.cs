using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface IHocKyRepository
{
    Task<List<HocKy>> LayTatCaAsync();
    Task<HocKy?> GetByIdAsync(string maHocKy);
    Task ThemAsync(HocKy hocKy);
    Task CapNhatAsync(HocKy hocKy);
    Task DatHocKyHienHanhAsync(string maHocKy);
    Task<bool> TonTaiAsync(string maHocKy);
}

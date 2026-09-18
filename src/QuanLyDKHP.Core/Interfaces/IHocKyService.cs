using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface IHocKyService
{
    Task<List<HocKy>> LayTatCaAsync();
    Task DatHocKyHienHanhAsync(string maHocKy);
    Task ThemAsync(HocKy hocKy);
    Task CapNhatAsync(HocKy hocKy);
}

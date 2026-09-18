using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface IMonHocService
{
    Task<List<MonHocDto>> LayDanhSachDtoAsync(string? tuKhoa, string sortBy = "TenMon");
    Task<List<MonHoc>> LayDanhSachAsync(string? tuKhoa, string sortBy = "TenMon");
    Task ThemAsync(MonHoc mon);
    Task CapNhatAsync(MonHoc mon);
    Task XoaAsync(string maMon);
    Task<bool> TonTaiAsync(string maMon);
}

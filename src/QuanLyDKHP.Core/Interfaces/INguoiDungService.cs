using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface INguoiDungService
{
    Task<List<NguoiDung>> LayTatCaAsync();
    Task ThemAsync(NguoiDung nd, string matKhauBanDau);
    Task CapNhatAsync(NguoiDung nd);
    Task DoiMatKhauAsync(int id, string matKhauCu, string matKhauMoi);
    Task DoiTrangThaiAsync(int id, string trangThaiMoi);
}

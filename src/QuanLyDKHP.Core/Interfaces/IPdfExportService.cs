using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;

namespace QuanLyDKHP.Core.Interfaces;

public interface IPdfExportService
{
    Task<byte[]> XuatPhieuHocPhiPdfAsync(HocPhiChiTietDto chiTiet);
}

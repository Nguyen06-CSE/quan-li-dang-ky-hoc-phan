using System.Collections.Generic;

namespace QuanLyDKHP.Core.Dtos;

public class LogDongImportDto
{
    public int DongSo { get; set; }
    public string MaSV { get; set; } = string.Empty;
    public string MaMon { get; set; } = string.Empty;
    public string MaLHP { get; set; } = string.Empty;
    public string LoaiLog { get; set; } = "ThongTin"; // "Loi", "CanhBao", "ThongTin"
    public string NoiDung { get; set; } = string.Empty;
}

public class KetQuaImportDto
{
    public int TongSoDong { get; set; }
    public int SoDongThanhCong { get; set; }
    public int SoDongLoi { get; set; }
    public int SoSinhVienMoi { get; set; }
    public int SoMonHocMoi { get; set; }
    public int SoLopHocPhanMoi { get; set; }
    public int SoDangKyMoi { get; set; }
    public List<LogDongImportDto> DanhSachLog { get; set; } = new();
}

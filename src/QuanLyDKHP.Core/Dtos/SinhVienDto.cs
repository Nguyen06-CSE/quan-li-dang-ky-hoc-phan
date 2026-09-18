namespace QuanLyDKHP.Core.Dtos;

public class SinhVienDto
{
    public string MaSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? LopSinhHoat { get; set; }
    public string? KhoaHoc { get; set; }
    public int SoTinChiDangKy { get; set; }
}

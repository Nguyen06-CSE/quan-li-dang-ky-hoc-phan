namespace QuanLyDKHP.Core.Dtos;

public class MonHocDto
{
    public string MaMon { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public int SoTinChiLT { get; set; }
    public int SoTinChiTH { get; set; }
    public int TongTinChi => SoTinChiLT + SoTinChiTH;
    public string? BacDaoTao { get; set; }
    public int SoLhpDangMo { get; set; }
}

using System;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.App.Dtos;

public class DangKyHocPhanDisplayDto
{
    public Guid Id { get; set; }
    public string MaSV { get; set; } = string.Empty;
    public string MaLHP { get; set; } = string.Empty;
    public string MaMon { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public int SoTinChiLT { get; set; }
    public int SoTinChiTH { get; set; }
    public int TongTinChi => SoTinChiLT + SoTinChiTH;
    public string TenGiangVien { get; set; } = "Chưa phân công";
    public string? HinhThucDK { get; set; }
    public DateTime NgayDK { get; set; }
    public string TrangThai { get; set; } = "DangHoc";
    public string TrangThaiHienThi => TrangThai == "DangHoc" ? "Đang học" : "Đã hủy";
    public bool IsDangHoc => TrangThai == "DangHoc";
    public decimal? SoTienPhaiDong { get; set; }
    public decimal SoTienDaDong { get; set; }
    public DangKyHocPhan Entity { get; set; } = null!;
}

namespace QuanLyDKHP.Core.Authorization;

/// <summary>
/// Hằng chuỗi tên các chức năng dùng làm key trong PermissionMatrix.
/// Tập trung 1 chỗ, tránh magic string rải rác.
/// </summary>
public static class ChucNang
{
    public const string DangNhap = "DangNhap";
    public const string XemDashboard = "XemDashboard";
    public const string CrudSinhVien = "CrudSinhVien";
    public const string CrudMonHoc = "CrudMonHoc";
    public const string CrudHocKyLopHocPhan = "CrudHocKyLopHocPhan";
    public const string DangKyHocPhan = "DangKyHocPhan";
    public const string TinhHocPhi = "TinhHocPhi";
    public const string XemHocPhi = "XemHocPhi";
    public const string XemDsSvDangKyTheoMon = "XemDsSvDangKyTheoMon";
    public const string LapDanhSachThi = "LapDanhSachThi";
    public const string NhapDiem = "NhapDiem";
    public const string InPhieuDKHP = "InPhieuDKHP";
    public const string ThongKeSvTheoMon = "ThongKeSvTheoMon";
    public const string CauHinhHeThong = "CauHinhHeThong";
    public const string QuanLyNguoiDung = "QuanLyNguoiDung";
    public const string ImportExcel = "ImportExcel";
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using QuanLyDKHP.Core.Enums;

namespace QuanLyDKHP.Core.Authorization;

/// <summary>
/// Ma trận quyền tập trung — nguồn sự thật duy nhất cho toàn bộ hệ thống phân quyền.
/// Map tên chức năng (constant string) → danh sách role được phép.
///
/// ⚠️ Quyền chi tiết là đề xuất tạm thời, chủ dự án sẽ xác nhận lại với giảng viên.
/// Chỉ cần sửa file này khi có thay đổi — không cần sửa rải rác các Service/ViewModel.
/// </summary>
public static class PermissionMatrix
{
    private static readonly ReadOnlyDictionary<string, UserRole[]> _matrix = new(
        new Dictionary<string, UserRole[]>
        {
            [ChucNang.DangNhap] = new[]
            {
                UserRole.Admin, UserRole.TroLyGiaoVu, UserRole.GiaoVuBoMon, UserRole.GiangVien
            },
            [ChucNang.XemDashboard] = new[]
            {
                UserRole.Admin, UserRole.TroLyGiaoVu, UserRole.GiaoVuBoMon, UserRole.GiangVien
            },
            [ChucNang.CrudSinhVien] = new[]
            {
                UserRole.Admin, UserRole.TroLyGiaoVu,
                UserRole.GiaoVuBoMon // TODO: xác nhận phạm vi quyền GiaoVuBoMon — tạm cho phép = TroLyGiaoVu
            },
            [ChucNang.CrudMonHoc] = new[]
            {
                UserRole.Admin, UserRole.TroLyGiaoVu, UserRole.GiaoVuBoMon
            },
            [ChucNang.CrudHocKyLopHocPhan] = new[]
            {
                UserRole.Admin, UserRole.TroLyGiaoVu, UserRole.GiaoVuBoMon
            },
            [ChucNang.DangKyHocPhan] = new[]
            {
                UserRole.Admin, UserRole.TroLyGiaoVu, UserRole.GiaoVuBoMon
            },
            [ChucNang.TinhHocPhi] = new[]
            {
                UserRole.Admin, UserRole.TroLyGiaoVu, UserRole.GiaoVuBoMon
            },
            [ChucNang.XemHocPhi] = new[]
            {
                UserRole.Admin, UserRole.TroLyGiaoVu, UserRole.GiaoVuBoMon, UserRole.GiangVien
            },
            [ChucNang.XemDsSvDangKyTheoMon] = new[]
            {
                UserRole.Admin, UserRole.TroLyGiaoVu, UserRole.GiaoVuBoMon, UserRole.GiangVien
            },
            [ChucNang.LapDanhSachThi] = new[]
            {
                UserRole.Admin, UserRole.TroLyGiaoVu, UserRole.GiaoVuBoMon, UserRole.GiangVien
            },
            [ChucNang.NhapDiem] = new[]
            {
                UserRole.GiaoVuBoMon, // TODO: xác nhận GiaoVuBoMon có quyền nhập điểm không
                UserRole.GiangVien    // Giảng viên chỉ nhập điểm LHP mình dạy (lọc ở Service)
            },
            [ChucNang.InPhieuDKHP] = new[]
            {
                UserRole.Admin, UserRole.TroLyGiaoVu, UserRole.GiaoVuBoMon
            },
            [ChucNang.ThongKeSvTheoMon] = new[]
            {
                UserRole.Admin, UserRole.TroLyGiaoVu, UserRole.GiaoVuBoMon, UserRole.GiangVien
            },
            [ChucNang.CauHinhHeThong] = new[]
            {
                UserRole.Admin
            },
            [ChucNang.QuanLyNguoiDung] = new[]
            {
                UserRole.Admin
            },
            [ChucNang.ImportExcel] = new[]
            {
                UserRole.Admin, UserRole.TroLyGiaoVu
            },
        });

    /// <summary>
    /// Trả về toàn bộ ma trận quyền (readonly).
    /// </summary>
    public static IReadOnlyDictionary<string, UserRole[]> Matrix => _matrix;

    /// <summary>
    /// Kiểm tra role có quyền thực hiện chức năng hay không.
    /// </summary>
    /// <param name="chucNang">Tên chức năng (dùng constant từ <see cref="ChucNang"/>).</param>
    /// <param name="role">Vai trò cần kiểm tra.</param>
    /// <returns>true nếu role có trong danh sách được phép.</returns>
    public static bool HasPermission(string chucNang, UserRole role)
    {
        if (_matrix.TryGetValue(chucNang, out var allowedRoles))
        {
            return allowedRoles.Contains(role);
        }
        // Chức năng không tồn tại trong ma trận → mặc định từ chối
        return false;
    }

    /// <summary>
    /// Kiểm tra role có quyền không — throw UnauthorizedAccessException nếu không có.
    /// Dùng ở tầng Service để đảm bảo bảo mật kép (UI + Service).
    /// </summary>
    /// <param name="chucNang">Tên chức năng.</param>
    /// <param name="role">Vai trò cần kiểm tra.</param>
    /// <exception cref="UnauthorizedAccessException">Nếu role không được phép.</exception>
    public static void Authorize(string chucNang, UserRole role)
    {
        if (!HasPermission(chucNang, role))
        {
            throw new UnauthorizedAccessException(
                $"Vai trò '{role}' không có quyền thực hiện chức năng '{chucNang}'.");
        }
    }
}

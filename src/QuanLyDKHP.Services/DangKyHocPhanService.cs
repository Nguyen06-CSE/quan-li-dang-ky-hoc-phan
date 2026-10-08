using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

/// <summary>
/// Service xử lý nghiệp vụ Đăng ký / Điều chỉnh học phần.
/// LƯU Ý KIẾN TRÚC: Tính năng "Điều chỉnh học phần" được hiện thực bằng tổ hợp:
/// 1. Hủy LHP cũ (qua HuyDangKyAsync)
/// 2. Đăng ký LHP mới (qua DangKyAsync)
/// Không cần màn hình hay API riêng cho việc điều chỉnh học phần.
/// </summary>
public class DangKyHocPhanService : IDangKyHocPhanService
{
    private readonly IDangKyHocPhanRepository _dangKyRepo;
    private readonly ILopHocPhanRepository _lopHocPhanRepo;
    private readonly ISinhVienRepository _sinhVienRepo;
    private readonly ICauHinhService _cauHinhService;
    private readonly IHocPhiService _hocPhiService;
    private readonly ICurrentUserService _currentUserService;

    public DangKyHocPhanService(
        IDangKyHocPhanRepository dangKyRepo,
        ILopHocPhanRepository lopHocPhanRepo,
        ISinhVienRepository sinhVienRepo,
        ICauHinhService cauHinhService,
        IHocPhiService hocPhiService,
        ICurrentUserService currentUserService)
    {
        _dangKyRepo = dangKyRepo;
        _lopHocPhanRepo = lopHocPhanRepo;
        _sinhVienRepo = sinhVienRepo;
        _cauHinhService = cauHinhService;
        _hocPhiService = hocPhiService;
        _currentUserService = currentUserService;
    }

    public async Task<List<DangKyHocPhan>> LayDanhSachDangKyAsync(string maSV, string maHocKy)
    {
        return await _dangKyRepo.LayTheoMaSVVaMaHocKyAsync(maSV, maHocKy);
    }

    public async Task<int> TinhTongTinChiHienTaiAsync(string maSV, string maHocKy)
    {
        var danhSach = await _dangKyRepo.LayTheoMaSVVaMaHocKyAsync(maSV, maHocKy);
        return danhSach
            .Where(dk => dk.TrangThai == "DangHoc" && dk.LopHocPhan?.MonHoc != null)
            .Sum(dk => dk.LopHocPhan.MonHoc.SoTinChiLT + dk.LopHocPhan.MonHoc.SoTinChiTH);
    }

    /// <summary>
    /// Thực hiện đăng ký học phần theo đúng thứ tự 5 bước kiểm tra:
    /// Bước 1: Kiểm tra trùng LHP (chặn cứng)
    /// Bước 2: Kiểm tra trùng môn học (cảnh báo mềm cho học lại/cải thiện)
    /// Bước 3: Kiểm tra sĩ số lớp học phần (chặn cứng)
    /// Bước 3.5: Kiểm tra trùng lịch học (chặn cứng) — chỉ áp dụng khi cả 2 LHP đã có lịch học
    /// (LHP nào "chưa xếp lịch" — Thu/TietBatDau/SoTiet NULL — thì bỏ qua, không coi là trùng)
    /// Bước 4: Kiểm tra tổng số tín chỉ tối đa (chặn cứng)
    /// Bước 5: Lưu đăng ký (tạo mới hoặc tái kích hoạt) và gọi TinhLaiHocPhiAsync
    /// </summary>
    public async Task<DangKyResult> DangKyAsync(string maSV, string maLHP, bool boQuaCanhBaoTrungMon = false)
    {
        if (string.IsNullOrWhiteSpace(maSV))
            return DangKyResult.Fail("Mã sinh viên không được để trống.");

        if (string.IsNullOrWhiteSpace(maLHP))
            return DangKyResult.Fail("Mã lớp học phần không được để trống.");

        var lhp = await _lopHocPhanRepo.GetByIdAsync(maLHP);
        if (lhp == null)
            return DangKyResult.Fail($"Không tìm thấy lớp học phần '{maLHP}'.");

        var sv = await _sinhVienRepo.GetByIdAsync(maSV);
        if (sv == null)
            return DangKyResult.Fail($"Không tìm thấy sinh viên '{maSV}'.");

        // 1. Kiểm tra trùng LHP: SV đã đăng ký đúng MaLHP này với TrangThai = DangHoc chưa?
        var existingDkLhp = await _dangKyRepo.LayTheoMaSVVaMaLHPAsync(maSV, maLHP);
        if (existingDkLhp != null && existingDkLhp.TrangThai == "DangHoc")
        {
            return DangKyResult.Fail("Sinh viên đã đăng ký lớp học phần này.");
        }

        // 2. Kiểm tra trùng môn (cảnh báo mềm): SV đã có đăng ký TrangThai = DangHoc ở 1 LHP khác cùng Mã môn?
        if (!boQuaCanhBaoTrungMon)
        {
            var dkCungMon = await _dangKyRepo.LayDangKyCungMonDangHocAsync(maSV, lhp.MaMon, lhp.MaHocKy);
            if (dkCungMon != null && dkCungMon.MaLHP != maLHP)
            {
                string tenMon = lhp.MonHoc?.TenMon ?? lhp.MaMon;
                return DangKyResult.CanhBaoTrungMon(
                    dkCungMon.MaLHP,
                    tenMon,
                    $"Sinh viên đã đăng ký môn {tenMon} ở lớp khác ({dkCungMon.MaLHP}). Đây có phải trường hợp học lại/học cải thiện?");
            }
        }

        // 3. Kiểm tra sĩ số LHP: nếu SiSoToiDa đã đặt và SoLuongDaDangKy >= SiSoToiDa
        if (lhp.SiSoToiDa.HasValue && lhp.SiSoToiDa.Value > 0)
        {
            int soLuongDaDangKy = await _dangKyRepo.DemSiSoDangKyAsync(maLHP);
            if (soLuongDaDangKy >= lhp.SiSoToiDa.Value)
            {
                return DangKyResult.Fail("Lớp học phần đã đủ sĩ số.");
            }
        }

        // Lấy 1 lần danh sách đăng ký hiện tại của SV trong học kỳ này — dùng chung cho bước 3.5
        // (check trùng lịch) và bước 4 (tính tổng tín chỉ), tránh query DB 2 lần cho cùng 1 dữ liệu.
        var dangKyHienTai = await _dangKyRepo.LayTheoMaSVVaMaHocKyAsync(maSV, lhp.MaHocKy);

        // 3.5. Kiểm tra trùng lịch học: so Thứ + khoảng Tiết của LHP mới với các LHP SV đang học.
        // Chỉ áp dụng khi LHP mới ĐÃ xếp lịch đầy đủ (Thu/TietBatDau/SoTiet đều có giá trị).
        // LHP nào (mới hoặc đang học) "chưa xếp lịch" (còn NULL) thì bỏ qua, không coi là trùng với gì cả
        // — đúng theo nguyên tắc không bịa dữ liệu giả (xem docs/05-modules/M3-dang-ky-hoc-phan.md).
        if (lhp.Thu.HasValue && lhp.TietBatDau.HasValue && lhp.SoTiet.HasValue)
        {
            int batDauMoi = lhp.TietBatDau.Value;
            int ketThucMoi = batDauMoi + lhp.SoTiet.Value - 1;

            foreach (var dk in dangKyHienTai)
            {
                if (dk.TrangThai != "DangHoc") continue;
                if (dk.MaLHP == maLHP) continue;

                var lhpKhac = dk.LopHocPhan;
                if (lhpKhac == null) continue;
                if (!lhpKhac.Thu.HasValue || !lhpKhac.TietBatDau.HasValue || !lhpKhac.SoTiet.HasValue) continue;
                if (lhpKhac.Thu.Value != lhp.Thu.Value) continue;

                int batDauKhac = lhpKhac.TietBatDau.Value;
                int ketThucKhac = batDauKhac + lhpKhac.SoTiet.Value - 1;

                bool trung = batDauMoi <= ketThucKhac && batDauKhac <= ketThucMoi;
                if (trung)
                {
                    string tenMonKhac = lhpKhac.MonHoc?.TenMon ?? lhpKhac.MaMon;
                    return DangKyResult.Fail(
                        $"Trùng lịch học với lớp '{lhpKhac.MaLHP}' ({tenMonKhac}) — cùng Thứ {lhp.Thu.Value}, " +
                        $"tiết {batDauKhac}-{ketThucKhac}.");
                }
            }
        }

        // 4. Kiểm tra tổng số tín chỉ: TongTCHienTai + SoTCMonMoi > SoTinChiToiDa
        int soTCMonMoi = lhp.MonHoc != null ? (lhp.MonHoc.SoTinChiLT + lhp.MonHoc.SoTinChiTH) : 0;
        int tongTCHienTai = dangKyHienTai
            .Where(dk => dk.TrangThai == "DangHoc" && dk.LopHocPhan?.MonHoc != null)
            .Sum(dk => dk.LopHocPhan.MonHoc.SoTinChiLT + dk.LopHocPhan.MonHoc.SoTinChiTH);
        int soTinChiToiDa = await _cauHinhService.GetInt("SoTinChiToiDa");
        if (soTinChiToiDa <= 0) soTinChiToiDa = 25;

        if (tongTCHienTai + soTCMonMoi > soTinChiToiDa)
        {
            return DangKyResult.Fail($"Vượt quá số tín chỉ tối đa cho phép ({soTinChiToiDa} tín chỉ).");
        }

        // 5. Tạo mới hoặc cập nhật bản ghi DangKyHocPhan
        string nguoiDk = _currentUserService.CurrentUser?.TenDangNhap ?? "admin";

        if (existingDkLhp != null)
        {
            // Tái kích hoạt bản ghi đã từng hủy để bảo toàn Unique Constraint (MaSV, MaLHP)
            existingDkLhp.TrangThai = "DangHoc";
            existingDkLhp.NgayDK = DateTime.UtcNow;
            existingDkLhp.NguoiDK = nguoiDk;
            existingDkLhp.HinhThucDK = "KH";
            await _dangKyRepo.CapNhatAsync(existingDkLhp);
        }
        else
        {
            var newDk = new DangKyHocPhan
            {
                Id = Guid.NewGuid(),
                MaSV = maSV,
                MaLHP = maLHP,
                TrangThai = "DangHoc",
                NgayDK = DateTime.UtcNow,
                NguoiDK = nguoiDk,
                HinhThucDK = "KH",
                SoTienDaDong = 0
            };
            await _dangKyRepo.ThemAsync(newDk);
        }

        // 6. Gọi HocPhiService.TinhLaiHocPhiAsync
        await _hocPhiService.TinhLaiHocPhiAsync(maSV, lhp.MaHocKy);

        // 7. Hoàn tất
        return DangKyResult.Ok("Đăng ký thành công.");
    }

    /// <summary>
    /// Hủy đăng ký học phần (soft-cancel, chuyển trạng thái sang DaHuy, không xóa bản ghi)
    /// </summary>
    public async Task HuyDangKyAsync(string maSV, string maLHP)
    {
        if (string.IsNullOrWhiteSpace(maSV) || string.IsNullOrWhiteSpace(maLHP))
            throw new ArgumentException("Mã sinh viên và Mã lớp học phần không được để trống.");

        var dk = await _dangKyRepo.LayTheoMaSVVaMaLHPAsync(maSV, maLHP);
        if (dk == null || dk.TrangThai != "DangHoc")
        {
            throw new InvalidOperationException("Không tìm thấy đăng ký học phần đang học để hủy.");
        }

        // Lấy MaHocKy TRƯỚC khi gọi CapNhatAsync: CapNhatAsync sẽ gán dk.LopHocPhan = null
        // (cắt navigation để tránh ghi đè nhầm bảng LopHocPhan — xem comment trong
        // DangKyHocPhanRepository.CapNhatAsync) ngay trên CHÍNH tham chiếu "dk" này, nên nếu đọc
        // dk.LopHocPhan SAU khi gọi CapNhatAsync thì luôn luôn là null, dẫn tới việc phải truy vấn
        // lại LHP một cách không cần thiết (không sai kết quả nhờ fallback bên dưới, nhưng dư 1
        // lần query DB và dễ gây nhầm lẫn khi đọc code).
        string maHocKy = dk.LopHocPhan?.MaHocKy ?? string.Empty;

        dk.TrangThai = "DaHuy";
        await _dangKyRepo.CapNhatAsync(dk);

        if (string.IsNullOrEmpty(maHocKy))
        {
            var lhp = await _lopHocPhanRepo.GetByIdAsync(maLHP);
            maHocKy = lhp?.MaHocKy ?? string.Empty;
        }

        if (!string.IsNullOrEmpty(maHocKy))
        {
            await _hocPhiService.TinhLaiHocPhiAsync(maSV, maHocKy);
        }
    }
}

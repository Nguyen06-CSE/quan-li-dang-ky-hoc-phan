using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Repositories;

/// <summary>
/// Import Excel theo lô:
///  - Đọc toàn bộ dòng ra bộ nhớ trước (tách khỏi việc ghi DB).
///  - Validate từng dòng (độ dài cột, khoảng điểm, ngày UTC...) TRƯỚC khi đưa vào DbContext.
///  - Ghi DB theo lô (mặc định 500 dòng/lần SaveChanges). Nếu 1 lô lỗi ở tầng DB thì hoàn tác lô đó
///    và thử lại TỪNG DÒNG, để 1 dòng xấu chỉ làm hỏng đúng dòng đó (đúng spec 015).
///  - Tính học phí hàng loạt sau khi ghi xong.
/// </summary>
public class ImportExcelRepository : IImportExcelRepository
{
    private const int KichThuocLo = 500;

    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IHocPhiService _hocPhiService;

    public ImportExcelRepository(IDbContextFactory<AppDbContext> contextFactory, IHocPhiService hocPhiService)
    {
        _contextFactory = contextFactory;
        _hocPhiService = hocPhiService;
    }

    // ------------------------------------------------------------------ kiểu nội bộ

    private sealed class DongImport
    {
        public int SoDong;
        public string MaSV = "", TenSV = "", LopSinhHoat = "", KhoaHoc = "", MaMon = "", TenMon = "";
        public string MaLHP = "", SoTc = "", MaGV = "", BacDT = "", LoaiHinhDT = "", HinhThucDK = "";
        public string NgayDkText = "", NguoiDK = "", DiemSoText = "", DiemChu = "", GiangDayOnline = "";
        public DateTime? NgayDkCell;      // nếu ô Excel là kiểu ngày thật
        public decimal? DiemSoCell;       // nếu ô Excel là kiểu số thật
    }

    /// <summary>Số liệu + log của 1 lô; chỉ được cộng vào kết quả khi lô đã lưu thành công.</summary>
    private sealed class ThongKeLo
    {
        public int ThanhCong, Loi, SvMoi, MonMoi, LhpMoi, DkMoi;
        public List<LogDongImportDto> Logs = new();
        public HashSet<string> SvCanTinhHocPhi = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class PhienImport
    {
        public AppDbContext Ctx = null!;
        public string MaHocKy = "";
        public Dictionary<string, SinhVien> SvCache = new();
        public Dictionary<string, MonHoc> MonCache = new();
        public Dictionary<string, LopHocPhan> LhpCache = new();
        public HashSet<string> DkCache = new();
    }

    // ------------------------------------------------------------------ luồng chính

    public async Task<KetQuaImportDto> ImportFileAsync(
        Stream stream,
        string maHocKy,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var ketQua = new KetQuaImportDto();

        if (string.IsNullOrWhiteSpace(maHocKy))
        {
            ThemLoiChung(ketQua, "Chưa chọn học kỳ đích để import.");
            return ketQua;
        }

        var dong = DocFileExcel(stream, ketQua);
        if (dong == null) return ketQua;

        ketQua.TongSoDong = dong.Count;

        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        // Ta tự Add/Attach tường minh nên tắt tự dò thay đổi: tránh chậm dần O(n²) khi nhiều entity.
        ctx.ChangeTracker.AutoDetectChangesEnabled = false;

        if (!await ctx.HocKys.AsNoTracking().AnyAsync(h => h.MaHocKy == maHocKy, cancellationToken))
        {
            ThemLoiChung(ketQua, $"Học kỳ '{maHocKy}' không tồn tại trong hệ thống.");
            return ketQua;
        }

        var phien = await TaiCacheAsync(ctx, maHocKy, cancellationToken);
        var svCanTinh = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int daXuLy = 0;
        for (int i = 0; i < dong.Count; i += KichThuocLo)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lo = dong.Skip(i).Take(KichThuocLo).ToList();

            var loi = await XuLyVaLuuAsync(phien, lo, ketQua, svCanTinh, cancellationToken);

            if (loi != null)
            {
                // Lô lỗi ở tầng DB -> đã hoàn tác cả lô, thử lại từng dòng để cô lập dòng xấu.
                foreach (var d in lo)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var loiDong = await XuLyVaLuuAsync(phien, new List<DongImport> { d }, ketQua, svCanTinh, cancellationToken);
                    if (loiDong != null)
                    {
                        ketQua.SoDongLoi++;
                        ketQua.DanhSachLog.Add(new LogDongImportDto
                        {
                            DongSo = d.SoDong,
                            MaSV = d.MaSV,
                            MaMon = d.MaMon,
                            MaLHP = d.MaLHP,
                            LoaiLog = "Loi",
                            NoiDung = $"Không lưu được dòng {d.SoDong}: {loiDong}"
                        });
                    }
                }
            }

            daXuLy += lo.Count;
            progress?.Report((int)((double)daXuLy / dong.Count * 85));
        }

        // Tính học phí hàng loạt (ít truy vấn, ghi theo lô) thay vì từng sinh viên / từng dòng.
        if (svCanTinh.Count > 0)
        {
            try
            {
                await _hocPhiService.TinhLaiHocPhiHangLoatAsync(svCanTinh, maHocKy);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                ketQua.DanhSachLog.Add(new LogDongImportDto
                {
                    DongSo = 0,
                    LoaiLog = "CanhBao",
                    NoiDung = $"Dữ liệu đã lưu nhưng tính lại học phí tự động bị lỗi: {LayThongDiepLoi(ex)}. Vào màn Học phí → Tính lại học phí để chạy lại."
                });
            }
        }

        progress?.Report(100);
        return ketQua;
    }

    // ------------------------------------------------------------------ đọc Excel

    private static List<DongImport>? DocFileExcel(Stream stream, KetQuaImportDto ketQua)
    {
        using var workbook = new XLWorkbook(stream);
        // Ưu tiên sheet "CT", nếu không có thì lấy sheet đầu tiên
        var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("CT", StringComparison.OrdinalIgnoreCase))
                        ?? workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
        {
            ThemLoiChung(ketQua, "Không tìm thấy sheet dữ liệu trong file Excel.");
            return null;
        }

        var rows = worksheet.RangeUsed()?.RowsUsed()?.ToList();
        if (rows == null || rows.Count <= 1)
        {
            ThemLoiChung(ketQua, "File Excel không có dòng dữ liệu nào.");
            return null;
        }

        var ds = new List<DongImport>(rows.Count - 1);
        foreach (var row in rows.Skip(1)) // bỏ dòng tiêu đề
        {
            var d = new DongImport
            {
                SoDong = row.RowNumber(),
                MaSV = row.Cell(1).GetString().Trim(),
                TenSV = row.Cell(2).GetString().Trim(),
                LopSinhHoat = row.Cell(3).GetString().Trim(),
                KhoaHoc = row.Cell(4).GetString().Trim(),
                MaMon = row.Cell(5).GetString().Trim(),
                TenMon = row.Cell(6).GetString().Trim(),
                MaLHP = row.Cell(7).GetString().Trim(),
                SoTc = row.Cell(8).GetString().Trim(),
                MaGV = row.Cell(9).GetString().Trim(),
                BacDT = row.Cell(11).GetString().Trim(),
                LoaiHinhDT = row.Cell(12).GetString().Trim(),
                HinhThucDK = row.Cell(13).GetString().Trim(),
                NgayDkText = row.Cell(14).GetString().Trim(),
                NguoiDK = row.Cell(15).GetString().Trim(),
                DiemSoText = row.Cell(16).GetString().Trim(),
                DiemChu = row.Cell(17).GetString().Trim(),
                GiangDayOnline = row.Cell(21).GetString().Trim(),
            };

            var cNgay = row.Cell(14);
            if (cNgay.DataType == XLDataType.DateTime) d.NgayDkCell = cNgay.GetDateTime();

            var cDiem = row.Cell(16);
            if (cDiem.DataType == XLDataType.Number) d.DiemSoCell = Math.Round((decimal)cDiem.GetDouble(), 2);

            ds.Add(d);
        }
        return ds;
    }

    private static void ThemLoiChung(KetQuaImportDto kq, string noiDung) =>
        kq.DanhSachLog.Add(new LogDongImportDto { DongSo = 0, LoaiLog = "Loi", NoiDung = noiDung });

    // ------------------------------------------------------------------ cache (không theo dõi thay đổi)

    private static async Task<PhienImport> TaiCacheAsync(AppDbContext ctx, string maHocKy, CancellationToken ct)
    {
        var p = new PhienImport { Ctx = ctx, MaHocKy = maHocKy };

        foreach (var s in await ctx.SinhViens.AsNoTracking().ToListAsync(ct))
            p.SvCache.TryAdd(s.MaSV.Trim().ToUpperInvariant(), s);
        foreach (var m in await ctx.MonHocs.AsNoTracking().ToListAsync(ct))
            p.MonCache.TryAdd(m.MaMon.Trim().ToUpperInvariant(), m);
        foreach (var l in await ctx.LopHocPhans.AsNoTracking().ToListAsync(ct))
            p.LhpCache.TryAdd(l.MaLHP.Trim().ToUpperInvariant(), l);

        var cacDk = await ctx.DangKyHocPhans.AsNoTracking()
            .Select(dk => new { dk.MaSV, dk.MaLHP })
            .ToListAsync(ct);
        foreach (var dk in cacDk)
            p.DkCache.Add(dk.MaSV.Trim().ToUpperInvariant() + "_" + dk.MaLHP.Trim().ToUpperInvariant());

        return p;
    }

    // ------------------------------------------------------------------ xử lý + lưu 1 lô

    /// <returns>null nếu lưu thành công; ngược lại là thông điệp lỗi (lô đã được hoàn tác).</returns>
    private static async Task<string?> XuLyVaLuuAsync(
        PhienImport p, List<DongImport> lo, KetQuaImportDto ketQua, HashSet<string> svCanTinh, CancellationToken ct)
    {
        var tk = new ThongKeLo();
        var hoanTac = new List<Action>();

        foreach (var d in lo)
        {
            try
            {
                XuLyMotDong(p, d, tk, hoanTac);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                tk.Loi++;
                tk.Logs.Add(new LogDongImportDto
                {
                    DongSo = d.SoDong, MaSV = d.MaSV, MaMon = d.MaMon, MaLHP = d.MaLHP,
                    LoaiLog = "Loi", NoiDung = $"Lỗi xử lý dòng {d.SoDong}: {ex.Message}"
                });
            }
        }

        try
        {
            await p.Ctx.SaveChangesAsync(ct);
            p.Ctx.ChangeTracker.Clear(); // giải phóng bộ nhớ theo dõi sau mỗi lô

            ketQua.SoDongThanhCong += tk.ThanhCong;
            ketQua.SoDongLoi += tk.Loi;
            ketQua.SoSinhVienMoi += tk.SvMoi;
            ketQua.SoMonHocMoi += tk.MonMoi;
            ketQua.SoLopHocPhanMoi += tk.LhpMoi;
            ketQua.SoDangKyMoi += tk.DkMoi;
            ketQua.DanhSachLog.AddRange(tk.Logs);
            foreach (var m in tk.SvCanTinhHocPhi) svCanTinh.Add(m);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Hoàn tác: bỏ mọi entity chờ lưu + trả cache về trạng thái trước lô
            p.Ctx.ChangeTracker.Clear();
            for (int i = hoanTac.Count - 1; i >= 0; i--) hoanTac[i]();
            return LayThongDiepLoi(ex);
        }
    }

    private static void XuLyMotDong(PhienImport p, DongImport d, ThongKeLo tk, List<Action> hoanTac)
    {
        var ctx = p.Ctx;

        // ---- 1. Validate bắt buộc
        if (string.IsNullOrWhiteSpace(d.MaSV) || string.IsNullOrWhiteSpace(d.TenSV) ||
            string.IsNullOrWhiteSpace(d.MaMon) || string.IsNullOrWhiteSpace(d.TenMon) ||
            string.IsNullOrWhiteSpace(d.MaLHP))
        {
            LoiDong(tk, d, "Thiếu thông tin bắt buộc (Mã SV, Tên SV, Mã môn, Tên môn hoặc Mã LHP bị rỗng).");
            return;
        }

        // ---- 2. Validate độ dài theo đúng kích thước cột trong DB (vượt là cả lô bị từ chối)
        var quaDai = KiemTraDoDai(d);
        if (quaDai != null) { LoiDong(tk, d, quaDai); return; }

        // ---- 3. Parse giá trị
        int soTC = 3; // mặc định
        if (!string.IsNullOrWhiteSpace(d.SoTc))
        {
            if (TryParseSo(d.SoTc, out var tcDec) && tcDec > 0 && tcDec == Math.Floor(tcDec)) soTC = (int)tcDec;
            else CanhBao(tk, d, $"Số tín chỉ '{d.SoTc}' không hợp lệ, dùng mặc định 3.");
        }

        DateTime ngayDK;
        if (d.NgayDkCell.HasValue) ngayDK = DateTime.SpecifyKind(d.NgayDkCell.Value, DateTimeKind.Utc);
        else if (!string.IsNullOrWhiteSpace(d.NgayDkText) && TryParseNgay(d.NgayDkText, out var dt)) ngayDK = dt;
        else
        {
            if (!string.IsNullOrWhiteSpace(d.NgayDkText))
                CanhBao(tk, d, $"Ngày đăng ký '{d.NgayDkText}' không đọc được, dùng ngày hiện tại.");
            ngayDK = DateTime.UtcNow;
        }

        decimal? diemSo = d.DiemSoCell;
        if (diemSo == null && !string.IsNullOrWhiteSpace(d.DiemSoText))
        {
            if (TryParseSo(d.DiemSoText, out var ds)) diemSo = Math.Round(ds, 2);
            else CanhBao(tk, d, $"Điểm số '{d.DiemSoText}' không đọc được, bỏ qua điểm.");
        }
        if (diemSo.HasValue && (diemSo < 0 || diemSo >= 100))
        {
            LoiDong(tk, d, $"Điểm số {diemSo} nằm ngoài khoảng cho phép (0 – 99.99).");
            return;
        }

        // ---- 4. Upsert SinhVien
        string svKey = d.MaSV.ToUpperInvariant();
        if (!p.SvCache.TryGetValue(svKey, out var sv))
        {
            sv = new SinhVien
            {
                MaSV = d.MaSV,
                HoTen = d.TenSV,
                LopSinhHoat = NullNeuRong(d.LopSinhHoat),
                KhoaHoc = NullNeuRong(d.KhoaHoc),
                NgayTao = DateTime.UtcNow
            };
            ctx.SinhViens.Add(sv);
            p.SvCache[svKey] = sv;
            hoanTac.Add(() => p.SvCache.Remove(svKey));
            tk.SvMoi++;
        }
        else if (!string.Equals(sv.HoTen, d.TenSV, StringComparison.OrdinalIgnoreCase))
        {
            CanhBao(tk, d, $"Họ tên SV trong file ('{d.TenSV}') khác tên hiện tại trong hệ thống ('{sv.HoTen}'). Giữ nguyên dữ liệu hiện có.");
        }

        // ---- 5. Upsert MonHoc
        string monKey = d.MaMon.ToUpperInvariant();
        if (!p.MonCache.TryGetValue(monKey, out var mon))
        {
            mon = new MonHoc
            {
                MaMon = d.MaMon,
                TenMon = d.TenMon,
                SoTinChiLT = soTC,
                SoTinChiTH = 0,
                BacDaoTao = NullNeuRong(d.BacDT),
                NgayTao = DateTime.UtcNow
            };
            ctx.MonHocs.Add(mon);
            p.MonCache[monKey] = mon;
            hoanTac.Add(() => p.MonCache.Remove(monKey));
            tk.MonMoi++;
            CanhBao(tk, d, $"Môn [{d.MaMon}] — {d.TenMon}: số TC LT/TH được gán mặc định ({soTC} LT / 0 TH), vui lòng vào màn hình Môn học để chỉnh lại cho đúng.", maMon: d.MaMon);
        }
        else
        {
            int tongTcHeThong = mon.SoTinChiLT + mon.SoTinChiTH;
            if (tongTcHeThong != soTC)
                CanhBao(tk, d, $"Môn [{d.MaMon}] — {d.TenMon}: tổng số TC trong file ({soTC}) khác tổng TC hệ thống ({tongTcHeThong}). Giữ nguyên cấu hình hệ thống.", maMon: d.MaMon);
        }

        // ---- 6. Upsert LopHocPhan (dùng đúng mã "chuẩn" của entity đã có để không lệch hoa/thường khi làm khóa ngoại)
        string lhpKey = d.MaLHP.ToUpperInvariant();
        if (!p.LhpCache.TryGetValue(lhpKey, out var lhp))
        {
            lhp = new LopHocPhan
            {
                MaLHP = d.MaLHP,
                MaMon = mon.MaMon,
                MaHocKy = p.MaHocKy,
                MaGV = NullNeuRong(d.MaGV),
                LoaiHinhDT = NullNeuRong(d.LoaiHinhDT),
                GiangDayOnline = !string.IsNullOrWhiteSpace(d.GiangDayOnline),
                NgayTao = DateTime.UtcNow
            };
            ctx.LopHocPhans.Add(lhp);
            p.LhpCache[lhpKey] = lhp;
            hoanTac.Add(() => p.LhpCache.Remove(lhpKey));
            tk.LhpMoi++;
        }
        else
        {
            if (!string.Equals(lhp.MaHocKy, p.MaHocKy, StringComparison.OrdinalIgnoreCase))
                CanhBao(tk, d, $"Lớp học phần [{lhp.MaLHP}] đang thuộc học kỳ '{lhp.MaHocKy}', không phải học kỳ đích '{p.MaHocKy}'.");

            if (string.IsNullOrWhiteSpace(lhp.MaGV) && !string.IsNullOrWhiteSpace(d.MaGV))
            {
                var cu = lhp.MaGV;
                lhp.MaGV = d.MaGV;
                // Entity trong cache không được theo dõi. Chỉ gắn 1 "khung" chứa khóa + cột cần đổi
                // (không Attach cả entity vì sẽ kéo theo toàn bộ đồ thị quan hệ đã fix-up).
                var stub = new LopHocPhan { MaLHP = lhp.MaLHP, MaGV = d.MaGV };
                ctx.LopHocPhans.Attach(stub);
                ctx.Entry(stub).Property(x => x.MaGV).IsModified = true;
                hoanTac.Add(() => lhp.MaGV = cu);
            }
        }

        // ---- 7. Upsert DangKyHocPhan (unique MaSV + MaLHP)
        string dkKey = svKey + "_" + lhpKey;
        if (!p.DkCache.Contains(dkKey))
        {
            ctx.DangKyHocPhans.Add(new DangKyHocPhan
            {
                Id = Guid.NewGuid(),
                MaSV = sv.MaSV,
                MaLHP = lhp.MaLHP,
                HinhThucDK = NullNeuRong(d.HinhThucDK),
                NgayDK = ngayDK,
                NguoiDK = NullNeuRong(d.NguoiDK),
                DiemSo = diemSo,
                DiemChu = NullNeuRong(d.DiemChu),
                TrangThai = "DangHoc",
                SoTienPhaiDong = 0,
                SoTienDaDong = 0,
                NgayTao = DateTime.UtcNow
            });
            p.DkCache.Add(dkKey);
            hoanTac.Add(() => p.DkCache.Remove(dkKey));
            tk.DkMoi++;
        }

        tk.SvCanTinhHocPhi.Add(sv.MaSV);
        tk.ThanhCong++;
    }

    // ------------------------------------------------------------------ tiện ích

    private static string? KiemTraDoDai(DongImport d)
    {
        (string Ten, string Gia, int Max)[] gioiHan =
        {
            ("Mã SV", d.MaSV, 20), ("Họ tên SV", d.TenSV, 100), ("Lớp sinh hoạt", d.LopSinhHoat, 30),
            ("Khóa học", d.KhoaHoc, 20), ("Mã môn", d.MaMon, 20), ("Tên môn", d.TenMon, 150),
            ("Mã LHP", d.MaLHP, 30), ("Mã GV", d.MaGV, 20), ("Bậc đào tạo", d.BacDT, 10),
            ("Loại hình ĐT", d.LoaiHinhDT, 10), ("Hình thức ĐK", d.HinhThucDK, 10),
            ("Người ĐK", d.NguoiDK, 50), ("Điểm chữ", d.DiemChu, 5),
        };
        foreach (var (ten, gia, max) in gioiHan)
            if (gia.Length > max)
                return $"Cột '{ten}' dài {gia.Length} ký tự, vượt giới hạn {max} của cơ sở dữ liệu.";
        return null;
    }

    private static string? NullNeuRong(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static bool TryParseSo(string text, out decimal value)
    {
        // Không cho phép dấu phân cách hàng nghìn: tránh hiểu "8,5" thành 85.
        const NumberStyles ns = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;
        return decimal.TryParse(text, ns, new CultureInfo("vi-VN"), out value)
            || decimal.TryParse(text, ns, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseNgay(string text, out DateTime value)
    {
        // Dữ liệu trường ở VN: ưu tiên dd/MM/yyyy (vi-VN) trước, rồi mới tới định dạng quốc tế.
        // Kết quả luôn là UTC vì Npgsql không cho ghi DateTime Kind=Unspecified vào timestamptz.
        const DateTimeStyles st = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
        return DateTime.TryParse(text, new CultureInfo("vi-VN"), st, out value)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, st, out value);
    }

    private static void LoiDong(ThongKeLo tk, DongImport d, string noiDung)
    {
        tk.Loi++;
        tk.Logs.Add(new LogDongImportDto
        {
            DongSo = d.SoDong, MaSV = d.MaSV, MaMon = d.MaMon, MaLHP = d.MaLHP,
            LoaiLog = "Loi", NoiDung = noiDung
        });
    }

    private static void CanhBao(ThongKeLo tk, DongImport d, string noiDung, string? maMon = null)
    {
        tk.Logs.Add(new LogDongImportDto
        {
            DongSo = d.SoDong, MaSV = d.MaSV, MaMon = maMon ?? string.Empty,
            LoaiLog = "CanhBao", NoiDung = noiDung
        });
    }

    private static string LayThongDiepLoi(Exception ex)
    {
        var goc = ex;
        while (goc.InnerException != null) goc = goc.InnerException;
        return goc.Message;
    }
}

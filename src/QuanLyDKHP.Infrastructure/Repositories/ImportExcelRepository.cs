using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
///  - Đọc đủ 21 cột của file mẫu.
///  - Validate từng dòng (độ dài cột, khoảng điểm, ngày UTC...) TRƯỚC khi đưa vào DbContext.
///  - Parse ngày tiếng Việt hỗ trợ SA/CH (Sáng/Chiều) bảo toàn 100% lịch sử đăng ký.
///  - Ghi DB theo lô (500 dòng/lần SaveChanges) với cơ chế cách ly lỗi (fault isolation).
///  - Hỗ trợ Upsert đầy đủ khi re-import: cập nhật điểm số, điểm chữ, học phí và ngày đăng ký.
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
        public string MaLHP = "", SoTc = "", MaGV = "", TenGV = "", BacDT = "", LoaiHinhDT = "", HinhThucDK = "";
        public string NgayDkText = "", NguoiDK = "", DiemSoText = "", DiemChu = "", GiangDayOnline = "";
        public string TinhTrangHocPhi = "", DaDongText = "", PhaiDongText = "";
        public DateTime? NgayDkCell;      // nếu ô Excel là kiểu ngày thật
        public decimal? DiemSoCell;       // nếu ô Excel là kiểu số thật
        public decimal? DaDongCell;       // nếu ô Excel là kiểu số thật
        public decimal? PhaiDongCell;     // nếu ô Excel là kiểu số thật
    }

    /// <summary>Số liệu + log của 1 lô; chỉ được cộng vào kết quả khi lô đã lưu thành công.</summary>
    private sealed class ThongKeLo
    {
        public int ThanhCong, Loi, SvMoi, MonMoi, LhpMoi, DkMoi, DkCapNhat;
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
        public Dictionary<string, DangKyHocPhan> DkCache = new();
        public Dictionary<string, NguoiDung> GvByMa = new();
        public Dictionary<string, NguoiDung> GvByTen = new();
    }

    // ------------------------------------------------------------------ luồng chính

    public async Task<KetQuaImportDto> ImportFileAsync(
        Stream stream,
        string? maHocKy = null,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var ketQua = new KetQuaImportDto();

        var dong = DocFileExcel(stream, ketQua);
        if (dong == null) return ketQua;

        ketQua.TongSoDong = dong.Count;

        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        // Tắt tự dò thay đổi: tránh O(n²) khi nhiều entity.
        ctx.ChangeTracker.AutoDetectChangesEnabled = false;

        HocKy? targetHocKy = null;

        if (string.IsNullOrWhiteSpace(maHocKy))
        {
            // 1. Thử tìm từ các mã LHP trong file xem có lớp nào đã tồn tại trong DB không
            var firstFewMlLhps = dong.Take(50).Select(d => d.MaLHP).Where(m => !string.IsNullOrEmpty(m)).Distinct().ToList();
            var existingLhp = await ctx.LopHocPhans.AsNoTracking()
                .Include(l => l.HocKy)
                .FirstOrDefaultAsync(l => firstFewMlLhps.Contains(l.MaLHP), cancellationToken);

            if (existingLhp?.HocKy != null)
            {
                targetHocKy = existingLhp.HocKy;
            }
            else
            {
                // 2. Thử lấy học kỳ đang mở trong hệ thống
                targetHocKy = await ctx.HocKys.AsNoTracking().FirstOrDefaultAsync(h => h.DangMo, cancellationToken);

                // 3. Nếu không có học kỳ mở, thử dò theo tiền tố mã LHP (ví dụ "261" từ "26120CT4201D02")
                if (targetHocKy == null && firstFewMlLhps.Count > 0)
                {
                    string sampleLhp = firstFewMlLhps[0];
                    string prefix3 = sampleLhp.Length >= 3 ? sampleLhp.Substring(0, 3) : sampleLhp;
                    targetHocKy = await ctx.HocKys.AsNoTracking().FirstOrDefaultAsync(h => h.MaHocKy.Contains(prefix3), cancellationToken);

                    if (targetHocKy == null)
                    {
                        // 4. Nếu chưa có học kỳ nào khớp, tự động tạo học kỳ mới theo tiền tố để nạp dữ liệu trơn tru
                        string newMaHk = prefix3.Length >= 2 ? prefix3 : "HK_AUTO";
                        targetHocKy = new HocKy
                        {
                            MaHocKy = newMaHk,
                            TenHocKy = $"Học kỳ {newMaHk}",
                            DangMo = true,
                            NgayTao = DateTime.UtcNow
                        };
                        ctx.HocKys.Add(targetHocKy);
                        await ctx.SaveChangesAsync(cancellationToken);
                    }
                }
            }

            if (targetHocKy == null)
            {
                // Dự phòng cuối: lấy bất kỳ học kỳ nào có sẵn hoặc tạo mới
                targetHocKy = await ctx.HocKys.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
                if (targetHocKy == null)
                {
                    targetHocKy = new HocKy
                    {
                        MaHocKy = "HK20261",
                        TenHocKy = "Học kỳ 1 (2025 - 2026)",
                        DangMo = true,
                        NgayTao = DateTime.UtcNow
                    };
                    ctx.HocKys.Add(targetHocKy);
                    await ctx.SaveChangesAsync(cancellationToken);
                }
            }

            maHocKy = targetHocKy.MaHocKy;
            ketQua.MaHocKy = maHocKy;
            ketQua.TenHocKy = targetHocKy.TenHocKy;
            ketQua.DanhSachLog.Add(new LogDongImportDto
            {
                DongSo = 0,
                LoaiLog = "ThongTin",
                NoiDung = $"Hệ thống tự động nhận diện và nạp vào học kỳ: {targetHocKy.TenHocKy} ({targetHocKy.MaHocKy})."
            });
        }
        else
        {
            targetHocKy = await ctx.HocKys.AsNoTracking().FirstOrDefaultAsync(h => h.MaHocKy == maHocKy, cancellationToken);
            if (targetHocKy == null)
            {
                targetHocKy = new HocKy
                {
                    MaHocKy = maHocKy,
                    TenHocKy = $"Học kỳ {maHocKy}",
                    DangMo = true,
                    NgayTao = DateTime.UtcNow
                };
                ctx.HocKys.Add(targetHocKy);
                await ctx.SaveChangesAsync(cancellationToken);
            }
            ketQua.MaHocKy = maHocKy;
            ketQua.TenHocKy = targetHocKy.TenHocKy;
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

        // Tính học phí hàng loạt sau khi ghi xong
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

    private static string GetCellText(IXLCell cell)
    {
        if (cell == null || cell.IsEmpty()) return string.Empty;
        try
        {
            var str = cell.GetFormattedString();
            if (!string.IsNullOrEmpty(str)) return str.Trim();
            return cell.Value.ToString()?.Trim() ?? string.Empty;
        }
        catch
        {
            try { return cell.Value.ToString()?.Trim() ?? string.Empty; }
            catch { return string.Empty; }
        }
    }

    private static List<DongImport>? DocFileExcel(Stream stream, KetQuaImportDto ketQua)
    {
        try
        {
            if (stream.CanSeek) stream.Position = 0;
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
                string maSv = GetCellText(row.Cell(1));
                string tenSv = GetCellText(row.Cell(2));
                string maMon = GetCellText(row.Cell(5));
                string tenMon = GetCellText(row.Cell(6));
                string maLhp = GetCellText(row.Cell(7));

                // Bỏ qua dòng trống hoàn toàn ở cuối file
                if (string.IsNullOrWhiteSpace(maSv) && string.IsNullOrWhiteSpace(maMon) && string.IsNullOrWhiteSpace(maLhp))
                {
                    continue;
                }

                var d = new DongImport
                {
                    SoDong = row.RowNumber(),
                    MaSV = maSv,
                    TenSV = tenSv,
                    LopSinhHoat = GetCellText(row.Cell(3)),
                    KhoaHoc = GetCellText(row.Cell(4)),
                    MaMon = maMon,
                    TenMon = tenMon,
                    MaLHP = maLhp,
                    SoTc = GetCellText(row.Cell(8)),
                    MaGV = GetCellText(row.Cell(9)),
                    TenGV = GetCellText(row.Cell(10)),
                    BacDT = GetCellText(row.Cell(11)),
                    LoaiHinhDT = GetCellText(row.Cell(12)),
                    HinhThucDK = GetCellText(row.Cell(13)),
                    NgayDkText = GetCellText(row.Cell(14)),
                    NguoiDK = GetCellText(row.Cell(15)),
                    DiemSoText = GetCellText(row.Cell(16)),
                    DiemChu = GetCellText(row.Cell(17)),
                    TinhTrangHocPhi = GetCellText(row.Cell(18)),
                    DaDongText = GetCellText(row.Cell(19)),
                    PhaiDongText = GetCellText(row.Cell(20)),
                    GiangDayOnline = GetCellText(row.Cell(21)),
                };

                var cNgay = row.Cell(14);
                if (cNgay.DataType == XLDataType.DateTime)
                {
                    try { d.NgayDkCell = cNgay.GetDateTime(); } catch { }
                }

                var cDiem = row.Cell(16);
                if (cDiem.DataType == XLDataType.Number)
                {
                    try { d.DiemSoCell = Math.Round((decimal)cDiem.GetDouble(), 2); } catch { }
                }

                var cDaDong = row.Cell(19);
                if (cDaDong.DataType == XLDataType.Number)
                {
                    try { d.DaDongCell = Math.Round((decimal)cDaDong.GetDouble(), 2); } catch { }
                }

                var cPhaiDong = row.Cell(20);
                if (cPhaiDong.DataType == XLDataType.Number)
                {
                    try { d.PhaiDongCell = Math.Round((decimal)cPhaiDong.GetDouble(), 2); } catch { }
                }

                ds.Add(d);
            }
            return ds;
        }
        catch (Exception ex)
        {
            ThemLoiChung(ketQua, $"Lỗi khi đọc file Excel: {ex.Message}");
            return null;
        }
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

        var giangViens = await ctx.NguoiDungs.AsNoTracking()
            .Where(u => u.Role == "GiangVien" || !string.IsNullOrEmpty(u.MaGV))
            .ToListAsync(ct);
        foreach (var gv in giangViens)
        {
            if (!string.IsNullOrWhiteSpace(gv.MaGV))
                p.GvByMa.TryAdd(gv.MaGV.Trim().ToUpperInvariant(), gv);
            if (!string.IsNullOrWhiteSpace(gv.TenDangNhap))
                p.GvByMa.TryAdd(gv.TenDangNhap.Trim().ToUpperInvariant(), gv);
            if (!string.IsNullOrWhiteSpace(gv.HoTen))
                p.GvByTen.TryAdd(gv.HoTen.Trim().ToUpperInvariant(), gv);
        }

        // Tải các đăng ký của học kỳ hiện tại để hỗ trợ Upsert
        var lhpKeys = await ctx.LopHocPhans.AsNoTracking()
            .Where(lhp => lhp.MaHocKy == maHocKy)
            .Select(lhp => lhp.MaLHP)
            .ToListAsync(ct);

        var cacDk = await ctx.DangKyHocPhans.AsNoTracking()
            .Where(dk => lhpKeys.Contains(dk.MaLHP))
            .ToListAsync(ct);
        foreach (var dk in cacDk)
        {
            string key = dk.MaSV.Trim().ToUpperInvariant() + "_" + dk.MaLHP.Trim().ToUpperInvariant();
            p.DkCache.TryAdd(key, dk);
        }

        return p;
    }

    // ------------------------------------------------------------------ xử lý + lưu 1 lô

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

            // Giới hạn log để tránh tràn RAM giao diện khi nạp dữ liệu siêu lớn
            if (ketQua.DanhSachLog.Count < 5000)
            {
                ketQua.DanhSachLog.AddRange(tk.Logs.Take(5000 - ketQua.DanhSachLog.Count));
            }

            foreach (var m in tk.SvCanTinhHocPhi) svCanTinh.Add(m);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
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

        // ---- 2. Validate độ dài theo đúng kích thước cột trong DB
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
        bool ngayHopLe = false;
        if (d.NgayDkCell.HasValue)
        {
            ngayDK = DateTime.SpecifyKind(d.NgayDkCell.Value, DateTimeKind.Utc);
            ngayHopLe = true;
        }
        else if (!string.IsNullOrWhiteSpace(d.NgayDkText) && TryParseNgay(d.NgayDkText, out var dt))
        {
            ngayDK = dt;
            ngayHopLe = true;
        }
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

        // Parse Đã đóng (cột 19) và Phải đóng (cột 20)
        decimal soTienDaDong = 0;
        if (d.DaDongCell.HasValue) soTienDaDong = d.DaDongCell.Value;
        else if (!string.IsNullOrWhiteSpace(d.DaDongText) && TryParseSo(d.DaDongText, out var dd)) soTienDaDong = dd;

        decimal? soTienPhaiDong = null;
        if (d.PhaiDongCell.HasValue) soTienPhaiDong = d.PhaiDongCell.Value;
        else if (!string.IsNullOrWhiteSpace(d.PhaiDongText) && TryParseSo(d.PhaiDongText, out var pd)) soTienPhaiDong = pd;

        // Xử lý Giảng viên từ cột 9 (Mã GV) và cột 10 (Tên GV)
        string? maGv = NullNeuRong(d.MaGV);
        if (string.IsNullOrWhiteSpace(maGv) && !string.IsNullOrWhiteSpace(d.TenGV))
        {
            if (p.GvByTen.TryGetValue(d.TenGV.Trim().ToUpperInvariant(), out var gvMatched))
            {
                maGv = gvMatched.MaGV ?? gvMatched.TenDangNhap;
            }
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

        // ---- 6. Upsert LopHocPhan
        string lhpKey = d.MaLHP.ToUpperInvariant();
        if (!p.LhpCache.TryGetValue(lhpKey, out var lhp))
        {
            lhp = new LopHocPhan
            {
                MaLHP = d.MaLHP,
                MaMon = mon.MaMon,
                MaHocKy = p.MaHocKy,
                MaGV = maGv,
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

            if (string.IsNullOrWhiteSpace(lhp.MaGV) && !string.IsNullOrWhiteSpace(maGv))
            {
                var cu = lhp.MaGV;
                lhp.MaGV = maGv;
                var trackedLhp = ctx.ChangeTracker.Entries<LopHocPhan>().FirstOrDefault(e => e.Entity.MaLHP == lhp.MaLHP);
                if (trackedLhp != null)
                {
                    trackedLhp.Entity.MaGV = maGv;
                }
                else
                {
                    var stub = new LopHocPhan { MaLHP = lhp.MaLHP, MaGV = maGv };
                    ctx.LopHocPhans.Attach(stub);
                    ctx.Entry(stub).Property(x => x.MaGV).IsModified = true;
                }
                hoanTac.Add(() => lhp.MaGV = cu);
            }
        }

        // ---- 7. Upsert DangKyHocPhan (unique MaSV + MaLHP)
        string dkKey = svKey + "_" + lhpKey;
        if (!p.DkCache.TryGetValue(dkKey, out var existingDk))
        {
            var newDk = new DangKyHocPhan
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
                SoTienPhaiDong = soTienPhaiDong ?? 0,
                SoTienDaDong = soTienDaDong,
                NgayTao = DateTime.UtcNow
            };
            ctx.DangKyHocPhans.Add(newDk);
            p.DkCache[dkKey] = newDk;
            hoanTac.Add(() => p.DkCache.Remove(dkKey));
            tk.DkMoi++;
        }
        else
        {
            // Re-import: Cập nhật thông tin điểm số, ngày đăng ký, học phí nếu có
            bool coDoi = false;
            var banSaoDk = new DangKyHocPhan
            {
                Id = existingDk.Id,
                MaSV = existingDk.MaSV,
                MaLHP = existingDk.MaLHP,
                DiemSo = existingDk.DiemSo,
                DiemChu = existingDk.DiemChu,
                NgayDK = existingDk.NgayDK,
                NguoiDK = existingDk.NguoiDK,
                SoTienPhaiDong = existingDk.SoTienPhaiDong,
                SoTienDaDong = existingDk.SoTienDaDong
            };

            if (diemSo.HasValue && existingDk.DiemSo != diemSo)
            {
                existingDk.DiemSo = diemSo;
                coDoi = true;
            }
            if (!string.IsNullOrWhiteSpace(d.DiemChu) && existingDk.DiemChu != d.DiemChu)
            {
                existingDk.DiemChu = d.DiemChu;
                coDoi = true;
            }
            if (ngayHopLe && existingDk.NgayDK != ngayDK)
            {
                existingDk.NgayDK = ngayDK;
                coDoi = true;
            }
            if (!string.IsNullOrWhiteSpace(d.NguoiDK) && existingDk.NguoiDK != d.NguoiDK)
            {
                existingDk.NguoiDK = d.NguoiDK;
                coDoi = true;
            }
            if (soTienPhaiDong.HasValue && existingDk.SoTienPhaiDong != soTienPhaiDong)
            {
                existingDk.SoTienPhaiDong = soTienPhaiDong;
                coDoi = true;
            }
            if (soTienDaDong > 0 && existingDk.SoTienDaDong != soTienDaDong)
            {
                existingDk.SoTienDaDong = soTienDaDong;
                coDoi = true;
            }

            if (coDoi)
            {
                existingDk.NgayCapNhat = DateTime.UtcNow;
                var trackedDk = ctx.ChangeTracker.Entries<DangKyHocPhan>().FirstOrDefault(e => e.Entity.Id == existingDk.Id);
                if (trackedDk != null)
                {
                    trackedDk.Entity.DiemSo = existingDk.DiemSo;
                    trackedDk.Entity.DiemChu = existingDk.DiemChu;
                    trackedDk.Entity.NgayDK = existingDk.NgayDK;
                    trackedDk.Entity.NguoiDK = existingDk.NguoiDK;
                    trackedDk.Entity.SoTienPhaiDong = existingDk.SoTienPhaiDong;
                    trackedDk.Entity.SoTienDaDong = existingDk.SoTienDaDong;
                    trackedDk.Entity.NgayCapNhat = existingDk.NgayCapNhat;
                }
                else
                {
                    var stub = new DangKyHocPhan
                    {
                        Id = existingDk.Id,
                        MaSV = existingDk.MaSV,
                        MaLHP = existingDk.MaLHP,
                        DiemSo = existingDk.DiemSo,
                        DiemChu = existingDk.DiemChu,
                        NgayDK = existingDk.NgayDK,
                        NguoiDK = existingDk.NguoiDK,
                        SoTienPhaiDong = existingDk.SoTienPhaiDong,
                        SoTienDaDong = existingDk.SoTienDaDong,
                        NgayCapNhat = existingDk.NgayCapNhat
                    };
                    ctx.DangKyHocPhans.Attach(stub);
                    ctx.Entry(stub).Property(x => x.DiemSo).IsModified = true;
                    ctx.Entry(stub).Property(x => x.DiemChu).IsModified = true;
                    ctx.Entry(stub).Property(x => x.NgayDK).IsModified = true;
                    ctx.Entry(stub).Property(x => x.NguoiDK).IsModified = true;
                    ctx.Entry(stub).Property(x => x.SoTienPhaiDong).IsModified = true;
                    ctx.Entry(stub).Property(x => x.SoTienDaDong).IsModified = true;
                    ctx.Entry(stub).Property(x => x.NgayCapNhat).IsModified = true;
                }

                hoanTac.Add(() =>
                {
                    existingDk.DiemSo = banSaoDk.DiemSo;
                    existingDk.DiemChu = banSaoDk.DiemChu;
                    existingDk.NgayDK = banSaoDk.NgayDK;
                    existingDk.NguoiDK = banSaoDk.NguoiDK;
                    existingDk.SoTienPhaiDong = banSaoDk.SoTienPhaiDong;
                    existingDk.SoTienDaDong = banSaoDk.SoTienDaDong;
                });
                tk.DkCapNhat++;
            }
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
            ("Mã LHP", d.MaLHP, 30), ("Mã GV", d.MaGV, 20), ("Tên GV", d.TenGV, 100), ("Bậc đào tạo", d.BacDT, 10),
            ("Loại hình ĐT", d.LoaiHinhDT, 10), ("Hình thức ĐK", d.HinhThucDK, 10),
            ("Người ĐK", d.NguoiDK, 50), ("Điểm chữ", d.DiemChu, 5),
            ("Đã đóng", d.DaDongText, 20), ("Phải đóng", d.PhaiDongText, 20)
        };
        foreach (var (ten, gia, max) in gioiHan)
            if (gia.Length > max)
                return $"Cột '{ten}' dài {gia.Length} ký tự, vượt giới hạn {max} của cơ sở dữ liệu.";
        return null;
    }

    private static string? NullNeuRong(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static bool TryParseSo(string text, out decimal value)
    {
        const NumberStyles ns = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;
        return decimal.TryParse(text, ns, new CultureInfo("vi-VN"), out value)
            || decimal.TryParse(text, ns, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseNgay(string text, out DateTime value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        string cleanText = text.Trim();
        bool isPM = cleanText.Contains("CH", StringComparison.OrdinalIgnoreCase);
        bool isAM = cleanText.Contains("SA", StringComparison.OrdinalIgnoreCase);

        if (isPM || isAM)
        {
            cleanText = Regex.Replace(cleanText, @"\bCH\b", "PM", RegexOptions.IgnoreCase);
            cleanText = Regex.Replace(cleanText, @"\bSA\b", "AM", RegexOptions.IgnoreCase);
        }

        string[] formats = new[]
        {
            "d/M/yyyy h:mm:ss tt",
            "dd/MM/yyyy h:mm:ss tt",
            "d/M/yyyy hh:mm:ss tt",
            "dd/MM/yyyy hh:mm:ss tt",
            "d/M/yyyy h:mm tt",
            "dd/MM/yyyy h:mm tt",
            "d/M/yyyy hh:mm tt",
            "dd/MM/yyyy hh:mm tt",
            "d/M/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "d/M/yyyy HH:mm",
            "dd/MM/yyyy HH:mm",
            "d/M/yyyy",
            "dd/MM/yyyy",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd",
            "M/d/yyyy h:mm:ss tt",
            "MM/dd/yyyy h:mm:ss tt",
            "M/d/yyyy HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss"
        };

        const DateTimeStyles st = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
        if (DateTime.TryParseExact(cleanText, formats, CultureInfo.InvariantCulture, st, out value))
        {
            return true;
        }

        return DateTime.TryParse(cleanText, new CultureInfo("vi-VN"), st, out value)
            || DateTime.TryParse(cleanText, CultureInfo.InvariantCulture, st, out value);
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

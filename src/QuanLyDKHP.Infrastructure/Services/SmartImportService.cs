using System;
using System.Collections.Generic;
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

namespace QuanLyDKHP.Infrastructure.Services;

public class SmartImportService : ISmartImportService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IHocPhiService _hocPhiService;

    public SmartImportService(
        IDbContextFactory<AppDbContext> contextFactory,
        IHocPhiService hocPhiService)
    {
        _contextFactory = contextFactory;
        _hocPhiService = hocPhiService;
    }

    public List<SinhVienImportPreviewDto> ParseClipboardOrText(string rawText, ColumnMappingDto? mapping = null)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return new List<SinhVienImportPreviewDto>();

        var lines = rawText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrEmpty(l))
                           .ToList();

        if (lines.Count == 0)
            return new List<SinhVienImportPreviewDto>();

        // Phân tách từng dòng thành các cell
        var matrix = new List<string[]>();
        foreach (var line in lines)
        {
            string[] cells;
            if (line.Contains('\t'))
                cells = line.Split('\t').Select(c => c.Trim()).ToArray();
            else if (line.Contains(';') && !line.Contains(','))
                cells = line.Split(';').Select(c => c.Trim()).ToArray();
            else if (line.Contains(','))
                cells = Regex.Split(line, @",(?=(?:[^""]*""[^""]*"")*[^""]*$)").Select(c => c.Trim(' ', '"')).ToArray();
            else
                cells = new[] { line };

            matrix.Add(cells);
        }

        return ProcessParsedMatrix(matrix, mapping);
    }

    public async Task<List<SinhVienImportPreviewDto>> ParseExcelStreamAsync(Stream stream, ColumnMappingDto? mapping = null, CancellationToken cancellationToken = default)
    {
        if (stream.CanSeek) stream.Position = 0;

        // Sao chép sang MemoryStream để đọc an toàn trên Background Thread, tránh khóa UI Thread.
        var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        return await Task.Run(() =>
        {
            var matrix = new List<string[]>();

            using var workbook = new XLWorkbook(buffer);
            var worksheet = workbook.Worksheets.FirstOrDefault(w => w.RangeUsed() != null)
                            ?? workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
                return new List<SinhVienImportPreviewDto>();

            var range = worksheet.RangeUsed();
            var rows = range?.RowsUsed()?.ToList();
            if (rows == null || rows.Count == 0)
                return new List<SinhVienImportPreviewDto>();

            int maxCols = range?.ColumnCount() ?? 1;

            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var cellValues = row.Cells(1, maxCols)
                                    .Select(GetCellText)
                                    .ToArray();
                if (cellValues.Any(v => !string.IsNullOrEmpty(v)))
                {
                    matrix.Add(cellValues);
                }
            }

            return ProcessParsedMatrix(matrix, mapping);
        }, cancellationToken);
    }

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

    private static List<SinhVienImportPreviewDto> ProcessParsedMatrix(List<string[]> matrix, ColumnMappingDto? mapping)
    {
        if (matrix.Count == 0)
            return new List<SinhVienImportPreviewDto>();

        int maSvCol = mapping?.MaSVCol ?? -1;
        int hoTenCol = mapping?.HoTenCol ?? -1;
        int hoDemCol = mapping?.HoDemCol ?? -1;
        int tenCol = mapping?.TenCol ?? -1;
        int lopCol = mapping?.LopSHCol ?? -1;
        int khoaCol = mapping?.KhoaHocCol ?? -1;
        int maMonCol = mapping?.MaMonCol ?? -1;
        int tenMonCol = mapping?.TenMonCol ?? -1;
        int maLhpCol = mapping?.MaLHPCol ?? -1;
        int soTcCol = mapping?.SoTCCol ?? -1;
        int maGvCol = mapping?.MaGVCol ?? -1;
        int tenGvCol = mapping?.TenGVCol ?? -1;
        int bacDtCol = -1;
        int loaiHinhDtCol = -1;
        int hinhThucDkCol = -1;
        int nguoiDkCol = -1;
        int diemSoCol = -1;
        int diemChuCol = -1;
        int daDongCol = -1;
        int phaiDongCol = -1;
        int onlineCol = -1;

        int startIndex = 0;

        // Tự động nhận diện header tối đa 10 dòng đầu nếu mapping chưa chỉ định
        if (maSvCol == -1 && hoTenCol == -1)
        {
            for (int r = 0; r < Math.Min(10, matrix.Count); r++)
            {
                var row = matrix[r];
                int candMaSv = -1, candHoTen = -1, candHoDem = -1, candTen = -1, candLop = -1, candGenericLop = -1, candKhoa = -1;
                int candMaMon = -1, candTenMon = -1, candMaLhp = -1, candSoTc = -1, candMaGv = -1, candTenGv = -1;
                int candBacDt = -1, candLoaiHinhDt = -1, candHinhThucDk = -1, candNguoiDk = -1;
                int candDiemSo = -1, candDiemChu = -1, candDaDong = -1, candPhaiDong = -1, candOnline = -1;

                for (int c = 0; c < row.Length; c++)
                {
                    string header = row[c].Trim().ToLowerInvariant();
                    if (string.IsNullOrEmpty(header)) continue;

                    // Ưu tiên kiểm tra các cột học phần / môn học / giảng viên trước để không trùng từ khóa "mã", "tên", "lớp"
                    if (candMaLhp == -1 && IsMaLhpHeader(header))
                        candMaLhp = c;
                    else if (candMaMon == -1 && IsMaMonHeader(header))
                        candMaMon = c;
                    else if (candTenMon == -1 && IsTenMonHeader(header))
                        candTenMon = c;
                    else if (candSoTc == -1 && IsSoTcHeader(header))
                        candSoTc = c;
                    else if (candMaGv == -1 && IsMaGvHeader(header))
                        candMaGv = c;
                    else if (candTenGv == -1 && IsTenGvHeader(header))
                        candTenGv = c;
                    else if (candBacDt == -1 && (header.Contains("bậc đt") || header.Contains("bac dt") || header.Contains("bậc đào tạo")))
                        candBacDt = c;
                    else if (candLoaiHinhDt == -1 && (header.Contains("loại hình") || header.Contains("loai hinh")))
                        candLoaiHinhDt = c;
                    else if (candHinhThucDk == -1 && (header.Contains("hình thức") || header.Contains("hinh thuc")))
                        candHinhThucDk = c;
                    else if (candNguoiDk == -1 && (header.Contains("người đk") || header.Contains("nguoi dk") || header.Contains("người đăng ký")))
                        candNguoiDk = c;
                    else if (candDiemChu == -1 && (header.Contains("điểm chữ") || header.Contains("diem chu")))
                        candDiemChu = c;
                    else if (candDiemSo == -1 && (header.Contains("điểm số") || header.Contains("diem so") || header.Equals("điểm") || header.Equals("diem")))
                        candDiemSo = c;
                    else if (candDaDong == -1 && (header.Contains("đã đóng") || header.Contains("da dong")))
                        candDaDong = c;
                    else if (candPhaiDong == -1 && (header.Contains("phải đóng") || header.Contains("phai dong")))
                        candPhaiDong = c;
                    else if (candOnline == -1 && header.Contains("online"))
                        candOnline = c;
                    else if (candMaSv == -1 && IsMaSvHeader(header))
                        candMaSv = c;
                    else if (candHoTen == -1 && IsHoTenHeader(header))
                        candHoTen = c;
                    else if (candHoDem == -1 && IsHoDemHeader(header))
                        candHoDem = c;
                    else if (candTen == -1 && (header.Equals("tên") || header.Equals("ten")))
                        candTen = c;
                    else if (candLop == -1 && IsLopSinhHoatHeader(header))
                        candLop = c;
                    else if (candGenericLop == -1 && IsGenericLopHeader(header))
                        candGenericLop = c;
                    else if (candKhoa == -1 && IsKhoaHocHeader(header))
                        candKhoa = c;
                }

                if (candMaSv != -1 || candHoTen != -1)
                {
                    maSvCol = candMaSv;
                    hoTenCol = candHoTen;
                    hoDemCol = candHoDem;
                    tenCol = candTen;
                    lopCol = candLop != -1 ? candLop : candGenericLop;
                    khoaCol = candKhoa;
                    maMonCol = candMaMon;
                    tenMonCol = candTenMon;
                    maLhpCol = candMaLhp;
                    soTcCol = candSoTc;
                    maGvCol = candMaGv;
                    tenGvCol = candTenGv;
                    bacDtCol = candBacDt;
                    loaiHinhDtCol = candLoaiHinhDt;
                    hinhThucDkCol = candHinhThucDk;
                    nguoiDkCol = candNguoiDk;
                    diemSoCol = candDiemSo;
                    diemChuCol = candDiemChu;
                    daDongCol = candDaDong;
                    phaiDongCol = candPhaiDong;
                    onlineCol = candOnline;
                    startIndex = r + 1;
                    break;
                }
            }
        }
        else if (mapping != null && mapping.HasHeader)
        {
            startIndex = 1;
        }

        // Nếu vẫn chưa nhận diện được header (ví dụ người dùng paste thẳng dữ liệu không có dòng tiêu đề)
        if (maSvCol == -1)
        {
            int colCount = matrix[0].Length;
            if (colCount == 1)
            {
                maSvCol = 0;
            }
            else if (colCount == 2)
            {
                if (Regex.IsMatch(matrix[0][0], @"\d{4,}"))
                {
                    maSvCol = 0;
                    hoTenCol = 1;
                }
                else
                {
                    hoTenCol = 0;
                    maSvCol = 1;
                }
            }
            else
            {
                if (int.TryParse(matrix[0][0], out _))
                {
                    maSvCol = 1;
                    hoTenCol = 2;
                    if (colCount > 3) lopCol = 3;
                }
                else
                {
                    maSvCol = 0;
                    hoTenCol = 1;
                    if (colCount > 2) lopCol = 2;
                }
            }
        }

        var list = new List<SinhVienImportPreviewDto>();
        int stt = 1;

        for (int i = startIndex; i < matrix.Count; i++)
        {
            var row = matrix[i];
            if (row.Length == 0 || row.All(string.IsNullOrWhiteSpace))
                continue;

            string GetCol(int colIdx) => colIdx >= 0 && colIdx < row.Length ? row[colIdx].Trim() : string.Empty;

            string maSv = GetCol(maSvCol);
            string hoTen = string.Empty;

            if (hoTenCol >= 0 && hoTenCol < row.Length)
            {
                hoTen = row[hoTenCol].Trim();
            }
            else if (hoDemCol >= 0 && tenCol >= 0 && hoDemCol < row.Length && tenCol < row.Length)
            {
                string hoDem = row[hoDemCol].Trim();
                string ten = row[tenCol].Trim();
                hoTen = $"{hoDem} {ten}".Trim();
            }

            string lop = GetCol(lopCol);
            string khoa = GetCol(khoaCol);
            string maMon = GetCol(maMonCol);
            string tenMon = GetCol(tenMonCol);
            string maLhp = GetCol(maLhpCol);
            string soTcText = GetCol(soTcCol);
            string maGv = GetCol(maGvCol);
            string tenGv = GetCol(tenGvCol);
            string bacDt = GetCol(bacDtCol);
            string loaiHinhDt = GetCol(loaiHinhDtCol);
            string hinhThucDk = GetCol(hinhThucDkCol);
            string nguoiDk = GetCol(nguoiDkCol);
            string diemSoText = GetCol(diemSoCol);
            string diemChu = GetCol(diemChuCol);
            string daDongText = GetCol(daDongCol);
            string phaiDongText = GetCol(phaiDongCol);
            string onlineText = GetCol(onlineCol);

            // Bỏ qua dòng trống hoàn toàn
            if (string.IsNullOrWhiteSpace(maSv) && string.IsNullOrWhiteSpace(hoTen) && string.IsNullOrWhiteSpace(maLhp))
                continue;

            // Tự động suy luận Khóa học nếu trống
            if (string.IsNullOrEmpty(khoa))
            {
                if (maSv.Length >= 2 && char.IsDigit(maSv[0]) && char.IsDigit(maSv[1]))
                {
                    khoa = "K" + maSv.Substring(0, 2);
                }
                else if (!string.IsNullOrEmpty(lop))
                {
                    var match = Regex.Match(lop, @"K\d{2,3}", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        khoa = match.Value.ToUpper();
                    }
                }
            }

            int soTc = 3;
            if (!string.IsNullOrWhiteSpace(soTcText) && decimal.TryParse(soTcText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var tcDec) && tcDec > 0)
            {
                soTc = (int)Math.Round(tcDec);
            }

            decimal? diemSo = null;
            if (!string.IsNullOrWhiteSpace(diemSoText) && TryParseDecimal(diemSoText, out var parsedDiem) && parsedDiem >= 0 && parsedDiem < 100)
            {
                diemSo = Math.Round(parsedDiem, 2);
            }

            decimal daDong = 0;
            if (!string.IsNullOrWhiteSpace(daDongText) && TryParseDecimal(daDongText, out var parsedDaDong) && parsedDaDong >= 0)
            {
                daDong = parsedDaDong;
            }

            decimal? phaiDong = null;
            if (!string.IsNullOrWhiteSpace(phaiDongText) && TryParseDecimal(phaiDongText, out var parsedPhaiDong) && parsedPhaiDong >= 0)
            {
                phaiDong = parsedPhaiDong;
            }

            // Đánh giá ban đầu
            string status = "HopLe";
            string error = string.Empty;

            if (string.IsNullOrWhiteSpace(maSv))
            {
                status = "Loi";
                error = "Mã sinh viên không được để trống";
            }
            else if (maSv.Length > 20)
            {
                status = "Loi";
                error = "Mã sinh viên vượt quá 20 ký tự";
            }

            list.Add(new SinhVienImportPreviewDto
            {
                Stt = stt++,
                MaSV = maSv,
                HoTen = hoTen,
                LopSinhHoat = lop,
                KhoaHoc = khoa,
                MaMon = maMon,
                TenMon = tenMon,
                MaLHP = maLhp,
                SoTC = soTc,
                MaGV = maGv,
                TenGV = tenGv,
                BacDT = bacDt,
                LoaiHinhDT = loaiHinhDt,
                HinhThucDK = hinhThucDk,
                NguoiDK = nguoiDk,
                DiemSo = diemSo,
                DiemChu = diemChu,
                SoTienDaDong = daDong,
                SoTienPhaiDong = phaiDong,
                GiangDayOnline = !string.IsNullOrWhiteSpace(onlineText) && !onlineText.Equals("0") && !onlineText.Equals("false", StringComparison.OrdinalIgnoreCase),
                TrangThaiXacThuc = status,
                ThongBaoLoi = error,
                IsSelected = status == "HopLe"
            });
        }

        return list;
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        const System.Globalization.NumberStyles ns = System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingSign;
        return decimal.TryParse(text, ns, new System.Globalization.CultureInfo("vi-VN"), out value)
            || decimal.TryParse(text, ns, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static bool IsMaSvHeader(string h) =>
        h.Contains("mssv") || h.Contains("mã sv") || h.Contains("ma sv") ||
        h.Contains("mã sinh viên") || h.Contains("ma sinh vien") ||
        h.Contains("mã số sinh viên") || h.Contains("ma so sinh vien") ||
        h.Contains("masv") || h.Contains("student id") ||
        h.Equals("id") || h.Equals("mã");

    private static bool IsHoTenHeader(string h) =>
        h.Contains("họ và tên") || h.Contains("ho va ten") ||
        h.Contains("họ tên") || h.Contains("ho ten") ||
        h.Contains("tên sv") || h.Contains("ten sv") ||
        h.Contains("tên sinh viên") || h.Contains("ten sinh vien") ||
        h.Contains("fullname") || h.Contains("full name") || h.Contains("hoten");

    private static bool IsHoDemHeader(string h) =>
        h.Contains("họ đệm") || h.Contains("ho dem") ||
        h.Contains("họ lót") || h.Contains("ho lot") ||
        h.Equals("họ") || h.Equals("ho");

    private static bool IsLopSinhHoatHeader(string h) =>
        h.Contains("lớp sinh hoạt") || h.Contains("lop sinh hoat") ||
        h.Contains("lớp sh") || h.Contains("lop sh") ||
        h.Contains("lớp hành chính") || h.Contains("lop hanh chinh");

    private static bool IsGenericLopHeader(string h) =>
        (h.Contains("lớp") || h.Contains("lop") || h.Contains("class")) &&
        !h.Contains("học phần") && !h.Contains("hoc phan") && !h.Contains("lhp") && !h.Contains("môn");

    private static bool IsKhoaHocHeader(string h) =>
        h.Contains("khóa học") || h.Contains("khoa hoc") ||
        h.Contains("niên khóa") || h.Contains("nien khoa") ||
        h.Contains("cohort") || h.Contains("khóa") || h.Equals("khoa");

    private static bool IsMaLhpHeader(string h) =>
        h.Contains("mã lhp") || h.Contains("ma lhp") || h.Contains("malhp") ||
        h.Contains("mã lớp học phần") || h.Contains("ma lop hoc phan") ||
        h.Contains("lớp học phần") || h.Contains("lop hoc phan") || h.Equals("lhp");

    private static bool IsMaMonHeader(string h) =>
        h.Contains("mã môn") || h.Contains("ma mon") || h.Contains("mamon") ||
        h.Contains("mã học phần") || h.Contains("ma hoc phan") ||
        h.Contains("mã mh") || h.Contains("mamh");

    private static bool IsTenMonHeader(string h) =>
        h.Contains("tên môn") || h.Contains("ten mon") || h.Contains("tenmon") ||
        h.Contains("tên học phần") || h.Contains("ten hoc phan") ||
        h.Contains("tên mh") || h.Equals("môn học") || h.Equals("mon hoc");

    private static bool IsSoTcHeader(string h) =>
        h.Contains("số tc") || h.Contains("so tc") || h.Contains("sotc") ||
        h.Contains("số tín chỉ") || h.Contains("so tin chi") ||
        h.Equals("tín chỉ") || h.Equals("tin chi") || h.Equals("tc");

    private static bool IsMaGvHeader(string h) =>
        h.Contains("mã gv") || h.Contains("ma gv") || h.Contains("magv") ||
        h.Contains("mã giảng viên") || h.Contains("ma giang vien");

    private static bool IsTenGvHeader(string h) =>
        h.Contains("tên gv") || h.Contains("ten gv") || h.Contains("tengv") ||
        h.Contains("tên giảng viên") || h.Contains("ten giang vien") ||
        h.Equals("giảng viên") || h.Equals("giang vien") || h.Equals("cbgd");

    public async Task<ClassInsightsDto> AnalyzeAndValidateAsync(
        string maHocKy,
        string maLHP,
        List<SinhVienImportPreviewDto> items,
        bool tuDongTaoSv = true)
    {
        var insights = new ClassInsightsDto
        {
            MaLHP = maLHP ?? string.Empty
        };

        if (items == null || items.Count == 0)
            return insights;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var lhp = !string.IsNullOrWhiteSpace(maLHP)
            ? await context.LopHocPhans.AsNoTracking()
                .Include(l => l.MonHoc)
                .FirstOrDefaultAsync(l => l.MaLHP == maLHP && !l.IsDeleted)
            : null;

        if (lhp != null)
        {
            insights.TenMon = lhp.MonHoc?.TenMon ?? lhp.MaMon;
            insights.SiSoToiDa = lhp.SiSoToiDa ?? 0;
        }

        var hk = !string.IsNullOrWhiteSpace(maHocKy)
            ? await context.HocKys.AsNoTracking().FirstOrDefaultAsync(h => h.MaHocKy == maHocKy)
            : null;
        if (hk != null && !hk.DangMo)
        {
            insights.DanhSachCanhBao.Add($"Học kỳ {hk.TenHocKy} hiện đang ĐÓNG cổng — cần mở cổng học kỳ mới nạp được.");
        }

        // Lấy danh sách sinh viên hiện đang học trong lớp đang chọn trên toolbar (nếu có)
        var existingRegistrationsInTargetLhp = !string.IsNullOrWhiteSpace(maLHP)
            ? await context.DangKyHocPhans.AsNoTracking()
                .Where(dk => dk.MaLHP == maLHP && dk.TrangThai == "DangHoc")
                .Select(dk => dk.MaSV)
                .ToListAsync()
            : new List<string>();

        insights.SiSoHienTai = existingRegistrationsInTargetLhp.Count;

        // Nạp danh mục LHP và Môn học để tự động điền thông tin môn/lớp cho mối quan hệ nhiều-nhiều
        var allDbLhps = await context.LopHocPhans.AsNoTracking()
            .Include(l => l.MonHoc)
            .ToListAsync();
        var dbLhpMap = new Dictionary<string, LopHocPhan>(StringComparer.OrdinalIgnoreCase);
        foreach (var dbL in allDbLhps)
        {
            dbLhpMap.TryAdd(dbL.MaLHP.Trim(), dbL);
        }

        var allDbMons = await context.MonHocs.AsNoTracking().ToListAsync();
        var dbMonMap = new Dictionary<string, MonHoc>(StringComparer.OrdinalIgnoreCase);
        foreach (var dbM in allDbMons)
        {
            dbMonMap.TryAdd(dbM.MaMon.Trim(), dbM);
        }

        // Lấy danh sách mã SV chuẩn hóa trong danh sách import
        var upperImportMaSvs = items
            .Select(x => (x.MaSV ?? string.Empty).Trim().ToUpperInvariant())
            .Where(m => !string.IsNullOrEmpty(m))
            .Distinct()
            .ToList();

        // Kiểm tra sinh viên nào đã có hồ sơ trong bảng SinhVien (không phân biệt hoa/thường)
        var dbStudentsRaw = await context.SinhViens.AsNoTracking()
            .Where(s => upperImportMaSvs.Contains(s.MaSV.ToUpper()))
            .ToListAsync();

        var existingDbStudents = new Dictionary<string, SinhVien>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in dbStudentsRaw)
        {
            existingDbStudents.TryAdd(s.MaSV.Trim(), s);
        }

        // Tải toàn bộ đăng ký đang học của các sinh viên này để kiểm tra theo từng cặp (MaSV, MaLHP) nhiều-nhiều
        var studentActiveDks = await context.DangKyHocPhans.AsNoTracking()
            .Where(dk => upperImportMaSvs.Contains(dk.MaSV.ToUpper()) && dk.TrangThai == "DangHoc")
            .Include(dk => dk.LopHocPhan)
                .ThenInclude(l => l.MonHoc)
            .ToListAsync();

        // Tập hợp các cặp "MaSV|MaLHP" đã tồn tại trong DB
        var existingPairSet = new HashSet<string>(
            studentActiveDks.Select(dk => $"{dk.MaSV.Trim()}|{dk.MaLHP.Trim()}"),
            StringComparer.OrdinalIgnoreCase);

        var activeDkByStudent = studentActiveDks
            .GroupBy(dk => dk.MaSV.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        int hopLe = 0;
        int daCo = 0;
        int loi = 0;

        foreach (var item in items)
        {
            item.CanhBaoXungDot = null;
            string maSv = (item.MaSV ?? string.Empty).Trim();
            item.MaSV = maSv;

            if (string.IsNullOrWhiteSpace(maSv))
            {
                item.TrangThaiXacThuc = "Loi";
                item.ThongBaoLoi = "Mã sinh viên trống";
                item.IsSelected = false;
                loi++;
                continue;
            }

            if (maSv.Length > 20)
            {
                item.TrangThaiXacThuc = "Loi";
                item.ThongBaoLoi = "Mã sinh viên vượt quá 20 ký tự";
                item.IsSelected = false;
                loi++;
                continue;
            }

            // Bù đắp thông tin Sinh viên từ DB nếu trong file bị khuyết
            if (existingDbStudents.TryGetValue(maSv, out var dbSv))
            {
                if (string.IsNullOrWhiteSpace(item.HoTen))
                    item.HoTen = dbSv.HoTen;
                if (string.IsNullOrWhiteSpace(item.LopSinhHoat))
                    item.LopSinhHoat = dbSv.LopSinhHoat ?? "";
                if (string.IsNullOrWhiteSpace(item.KhoaHoc))
                    item.KhoaHoc = dbSv.KhoaHoc ?? "";
            }

            // Xác định lớp học phần đích của dòng này (ưu tiên cột MaLHP trong file, nếu không có thì lấy lớp chọn trên toolbar)
            string rowMaLhp = !string.IsNullOrWhiteSpace(item.MaLHP)
                ? item.MaLHP.Trim()
                : (maLHP ?? string.Empty).Trim();

            LopHocPhan? rowLhpEntity = null;
            if (!string.IsNullOrWhiteSpace(rowMaLhp))
            {
                dbLhpMap.TryGetValue(rowMaLhp, out rowLhpEntity);
            }
            rowLhpEntity ??= lhp;

            // Bù đắp thông tin Môn học / Tín chỉ nếu dòng chưa có nhưng DB đã có LHP hoặc Môn học
            if (rowLhpEntity != null)
            {
                if (string.IsNullOrWhiteSpace(item.MaMon))
                    item.MaMon = rowLhpEntity.MaMon;
                if (string.IsNullOrWhiteSpace(item.TenMon) && rowLhpEntity.MonHoc != null)
                    item.TenMon = rowLhpEntity.MonHoc.TenMon;
                if (item.SoTC <= 0 && rowLhpEntity.MonHoc != null)
                    item.SoTC = rowLhpEntity.MonHoc.SoTinChiLT + rowLhpEntity.MonHoc.SoTinChiTH;
            }
            else if (!string.IsNullOrWhiteSpace(item.MaMon) && dbMonMap.TryGetValue(item.MaMon.Trim(), out var dbMon))
            {
                if (string.IsNullOrWhiteSpace(item.TenMon))
                    item.TenMon = dbMon.TenMon;
                if (item.SoTC <= 0)
                    item.SoTC = dbMon.SoTinChiLT + dbMon.SoTinChiTH;
            }

            // Kiểm tra theo cặp (MaSV, MaLHP) chuẩn quan hệ nhiều-nhiều:
            // Cùng 1 MaSV xuất hiện nhiều lần cho các MaLHP/MaMon khác nhau hoàn toàn HỢP LỆ!
            if (!string.IsNullOrWhiteSpace(rowMaLhp) && existingPairSet.Contains($"{maSv}|{rowMaLhp}"))
            {
                item.TrangThaiXacThuc = "DaCoTrongLop";
                item.ThongBaoLoi = $"Đã có trong lớp {rowMaLhp}";
                item.IsSelected = false;
                daCo++;
                continue;
            }

            // Nếu tắt tự động tạo SV và SV chưa tồn tại trong danh mục trường
            if (!tuDongTaoSv && !existingDbStudents.ContainsKey(maSv))
            {
                item.TrangThaiXacThuc = "Loi";
                item.ThongBaoLoi = "Chưa có hồ sơ SV trong CSDL (tắt tự động tạo hồ sơ)";
                item.IsSelected = false;
                loi++;
                continue;
            }

            // Kiểm tra xung đột trùng môn hoặc trùng lịch học với các lớp khác đang học trong kỳ
            string targetMon = !string.IsNullOrWhiteSpace(item.MaMon)
                ? item.MaMon.Trim()
                : (rowLhpEntity?.MaMon ?? string.Empty);

            if (activeDkByStudent.TryGetValue(maSv, out var studentOtherDks))
            {
                var dksInSameSemester = studentOtherDks
                    .Where(d => (string.IsNullOrWhiteSpace(maHocKy) || d.LopHocPhan.MaHocKy == maHocKy)
                                && !string.Equals(d.MaLHP, rowMaLhp, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!string.IsNullOrWhiteSpace(targetMon))
                {
                    var sameMonDk = dksInSameSemester.FirstOrDefault(d =>
                        string.Equals(d.LopHocPhan.MaMon, targetMon, StringComparison.OrdinalIgnoreCase));
                    if (sameMonDk != null)
                    {
                        item.CanhBaoXungDot = $"Đã đăng ký lớp {sameMonDk.MaLHP} cùng môn {targetMon}";
                        insights.DanhSachCanhBao.Add($"SV {maSv} ({item.HoTen}): Đã đăng ký lớp {sameMonDk.MaLHP} cùng môn {targetMon}");
                    }
                }

                if (rowLhpEntity != null && rowLhpEntity.Thu.HasValue && rowLhpEntity.TietBatDau.HasValue && rowLhpEntity.SoTiet.HasValue)
                {
                    int lhpTietKetThuc = rowLhpEntity.TietBatDau.Value + rowLhpEntity.SoTiet.Value - 1;
                    foreach (var oDk in dksInSameSemester)
                    {
                        var oLhp = oDk.LopHocPhan;
                        if (oLhp.Thu == rowLhpEntity.Thu && oLhp.TietBatDau.HasValue && oLhp.SoTiet.HasValue)
                        {
                            int oKetThuc = oLhp.TietBatDau.Value + oLhp.SoTiet.Value - 1;
                            if (rowLhpEntity.TietBatDau.Value <= oKetThuc && oLhp.TietBatDau.Value <= lhpTietKetThuc)
                            {
                                string conflictMsg = $"Trùng lịch Thứ {rowLhpEntity.Thu} (tiết {rowLhpEntity.TietBatDau}-{lhpTietKetThuc}) với {oLhp.MaLHP}";
                                item.CanhBaoXungDot = string.IsNullOrEmpty(item.CanhBaoXungDot)
                                    ? conflictMsg
                                    : $"{item.CanhBaoXungDot}; {conflictMsg}";
                                insights.DanhSachCanhBao.Add($"SV {maSv} ({item.HoTen}): {conflictMsg}");
                            }
                        }
                    }
                }
            }

            item.TrangThaiXacThuc = "HopLe";
            item.ThongBaoLoi = string.Empty;
            item.IsSelected = true;
            hopLe++;
        }

        var validSelected = items.Where(x => x.IsHopLe && x.IsSelected).ToList();
        insights.SoLuongHopLe = hopLe;
        insights.SoLuongDaCo = daCo;
        insights.SoLuongLoi = loi;
        insights.SiSoThemVao = validSelected
            .Where(x => string.IsNullOrWhiteSpace(x.MaLHP) || string.Equals(x.MaLHP.Trim(), maLHP, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.MaSV)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        if (insights.SiSoThemVao == 0 && validSelected.Count > 0)
        {
            insights.SiSoThemVao = validSelected.Select(x => x.MaSV).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        }

        // Phân tích cơ cấu lớp (Class Demographics) theo số sinh viên duy nhất (không nhân bản theo số môn đăng ký)
        var uniqueStudentMap = new Dictionary<string, (string Lop, string Khoa)>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in validSelected)
        {
            uniqueStudentMap.TryAdd(s.MaSV.Trim(), (
                Lop: string.IsNullOrWhiteSpace(s.LopSinhHoat) ? "Khác" : s.LopSinhHoat.Trim(),
                Khoa: string.IsNullOrWhiteSpace(s.KhoaHoc) ? "Khác" : s.KhoaHoc.Trim()
            ));
        }

        if (existingRegistrationsInTargetLhp.Count > 0)
        {
            var existingSvInfo = await context.SinhViens.AsNoTracking()
                .Where(s => existingRegistrationsInTargetLhp.Contains(s.MaSV))
                .Select(s => new { s.MaSV, s.LopSinhHoat, s.KhoaHoc })
                .ToListAsync();

            foreach (var s in existingSvInfo)
            {
                uniqueStudentMap.TryAdd(s.MaSV.Trim(), (
                    Lop: string.IsNullOrWhiteSpace(s.LopSinhHoat) ? "Khác" : s.LopSinhHoat.Trim(),
                    Khoa: string.IsNullOrWhiteSpace(s.KhoaHoc) ? "Khác" : s.KhoaHoc.Trim()
                ));
            }
        }

        var allStudentsInClass = uniqueStudentMap.Values.ToList();
        int totalCount = allStudentsInClass.Count;
        if (totalCount > 0)
        {
            insights.CoCauLop = allStudentsInClass
                .GroupBy(s => s.Lop)
                .Select(g => new ThongKeCoCauDto
                {
                    Nhan = g.Key,
                    SoLuong = g.Count(),
                    PhanTram = (double)g.Count() / totalCount * 100.0
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(5)
                .ToList();

            insights.CoCauKhoa = allStudentsInClass
                .GroupBy(s => s.Khoa)
                .Select(g => new ThongKeCoCauDto
                {
                    Nhan = g.Key,
                    SoLuong = g.Count(),
                    PhanTram = (double)g.Count() / totalCount * 100.0
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(5)
                .ToList();
        }

        return insights;
    }

    public async Task<KetQuaSmartEnrollDto> EnrollStudentsAsync(
        string maHocKy,
        string maLHP,
        List<SinhVienImportPreviewDto> items,
        bool tuDongTaoSv = true,
        bool boQuaTrungMon = true,
        CancellationToken cancellationToken = default)
    {
        var ketQua = new KetQuaSmartEnrollDto
        {
            MaLHP = maLHP ?? string.Empty,
            TongSoLuong = items?.Count ?? 0
        };

        if (items == null || items.Count == 0)
        {
            ketQua.ThongBao = "Không có sinh viên nào để nạp.";
            return ketQua;
        }

        var selectedItems = items
            .Where(x => x.IsSelected && x.IsHopLe && !string.IsNullOrWhiteSpace(x.MaSV))
            .ToList();
        if (selectedItems.Count == 0)
        {
            ketQua.ThongBao = "Không có dòng đăng ký hợp lệ nào được chọn để nạp.";
            return ketQua;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Kiểm tra Học kỳ
        HocKy? hk = null;
        if (!string.IsNullOrWhiteSpace(maHocKy))
        {
            hk = await context.HocKys.FirstOrDefaultAsync(h => h.MaHocKy == maHocKy, cancellationToken);
        }
        else
        {
            hk = await context.HocKys.FirstOrDefaultAsync(h => h.DangMo, cancellationToken)
                 ?? await context.HocKys.FirstOrDefaultAsync(cancellationToken);
            if (hk != null) maHocKy = hk.MaHocKy;
        }

        if (hk != null && !hk.DangMo)
        {
            ketQua.ThongBao = $"Học kỳ {hk.TenHocKy} ({hk.MaHocKy}) đã đóng cổng, không thể nạp đăng ký.";
            ketQua.Loi = selectedItems.Count;
            ketQua.ChiTietLoi.Add(ketQua.ThongBao);
            return ketQua;
        }

        if (hk == null)
        {
            string newMaHk = !string.IsNullOrWhiteSpace(maHocKy) ? maHocKy.Trim() : "HK20251";
            hk = new HocKy
            {
                MaHocKy = newMaHk,
                TenHocKy = $"Học kỳ {newMaHk}",
                DangMo = true,
                NgayTao = DateTime.UtcNow
            };
            await context.HocKys.AddAsync(hk, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            maHocKy = hk.MaHocKy;
        }

        // Lớp học phần mặc định trên toolbar (dùng khi dòng trong file không có cột MaLHP riêng)
        LopHocPhan? defaultLhp = null;
        if (!string.IsNullOrWhiteSpace(maLHP))
        {
            defaultLhp = await context.LopHocPhans
                .Include(l => l.MonHoc)
                .FirstOrDefaultAsync(l => l.MaLHP == maLHP && !l.IsDeleted, cancellationToken);
        }

        bool anyRowMissingOwnLhp = selectedItems.Any(x => string.IsNullOrWhiteSpace(x.MaLHP) && string.IsNullOrWhiteSpace(x.MaMon));
        if (anyRowMissingOwnLhp && defaultLhp == null)
        {
            ketQua.ThongBao = $"Không tìm thấy lớp học phần '{maLHP}' trong CSDL.";
            ketQua.Loi = selectedItems.Count;
            ketQua.ChiTietLoi.Add(ketQua.ThongBao);
            return ketQua;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var enrolledStudents = new List<string>();
        var enrolledStudentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var upperIds = selectedItems
                .Select(x => x.MaSV.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            // 1. Tải các sinh viên hiện có
            var dbStudents = await context.SinhViens
                .Where(s => upperIds.Contains(s.MaSV.ToUpper()))
                .ToListAsync(cancellationToken);
            var existingStudents = new Dictionary<string, SinhVien>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in dbStudents)
            {
                existingStudents.TryAdd(s.MaSV.Trim(), s);
            }

            // 2. Tải danh mục Môn học và Lớp học phần hiện có để hỗ trợ nạp quan hệ nhiều-nhiều (SV - Môn - LHP)
            var allMons = await context.MonHocs.ToListAsync(cancellationToken);
            var existingMons = new Dictionary<string, MonHoc>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in allMons)
            {
                existingMons.TryAdd(m.MaMon.Trim(), m);
            }

            var allLhps = await context.LopHocPhans.Include(l => l.MonHoc).ToListAsync(cancellationToken);
            var existingLhps = new Dictionary<string, LopHocPhan>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in allLhps)
            {
                existingLhps.TryAdd(l.MaLHP.Trim(), l);
            }

            // 3. Tải danh mục Giảng viên để ánh xạ Mã GV / Tên GV (nếu file có cột giảng viên)
            var giangViens = await context.NguoiDungs.AsNoTracking()
                .Where(u => u.Role == "GiangVien" || !string.IsNullOrEmpty(u.MaGV))
                .ToListAsync(cancellationToken);
            var gvByTen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var gv in giangViens)
            {
                if (!string.IsNullOrWhiteSpace(gv.HoTen))
                {
                    gvByTen.TryAdd(gv.HoTen.Trim(), gv.MaGV ?? gv.TenDangNhap);
                }
            }

            // 4. Tải toàn bộ đăng ký hiện có của các sinh viên trong danh sách theo cặp khóa "MaSV|MaLHP"
            var dbRegistrations = await context.DangKyHocPhans
                .Include(dk => dk.LopHocPhan)
                .Where(dk => upperIds.Contains(dk.MaSV.ToUpper()))
                .ToListAsync(cancellationToken);

            var existingRegistrationsByPair = new Dictionary<string, DangKyHocPhan>(StringComparer.OrdinalIgnoreCase);
            var existingSubjectByStudent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dk in dbRegistrations)
            {
                string pairKey = $"{dk.MaSV.Trim()}|{dk.MaLHP.Trim()}";
                existingRegistrationsByPair.TryAdd(pairKey, dk);

                if (dk.TrangThai == "DangHoc" && dk.LopHocPhan != null && dk.LopHocPhan.MaHocKy == maHocKy)
                {
                    existingSubjectByStudent.Add($"{dk.MaSV.Trim()}|{dk.LopHocPhan.MaMon.Trim()}|{dk.MaLHP.Trim()}");
                }
            }

            var processedPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in selectedItems)
            {
                string rawMaSv = item.MaSV.Trim();

                if (rawMaSv.Length > 20)
                {
                    ketQua.Loi++;
                    ketQua.ChiTietLoi.Add($"Lỗi SV {rawMaSv}: Mã sinh viên vượt quá 20 ký tự.");
                    continue;
                }

                // Xác định Mã LHP và Mã Môn của dòng hiện tại (hỗ trợ cả file nhiều môn/nhiều LHP và file 1 lớp)
                string rowMaLhp = !string.IsNullOrWhiteSpace(item.MaLHP)
                    ? TrimMax(item.MaLHP, 30)
                    : (defaultLhp?.MaLHP ?? TrimMax(maLHP, 30));

                string rowMaMon = !string.IsNullOrWhiteSpace(item.MaMon)
                    ? TrimMax(item.MaMon, 20)
                    : string.Empty;

                if (string.IsNullOrWhiteSpace(rowMaLhp) && !string.IsNullOrWhiteSpace(rowMaMon))
                {
                    rowMaLhp = TrimMax($"{maHocKy}_{rowMaMon}_01", 30);
                }

                if (string.IsNullOrWhiteSpace(rowMaLhp))
                {
                    ketQua.Loi++;
                    ketQua.ChiTietLoi.Add($"Lỗi dòng SV {rawMaSv}: Chưa xác định được Lớp học phần đích.");
                    continue;
                }

                // Khử trùng lặp theo cặp (MaSV, MaLHP) — cho phép 1 MaSV đăng ký nhiều MaLHP khác nhau
                string pairKey = $"{rawMaSv}|{rowMaLhp}";
                if (!processedPairs.Add(pairKey))
                {
                    ketQua.BoQua++;
                    continue;
                }

                // A. Đảm bảo Lớp học phần & Môn học tồn tại (tự động tạo nếu file có môn/LHP mới)
                if (!existingLhps.TryGetValue(rowMaLhp, out var targetLhpEntity))
                {
                    if (string.IsNullOrWhiteSpace(rowMaMon))
                    {
                        rowMaMon = defaultLhp?.MaMon ?? TrimMax(rowMaLhp, 20);
                    }

                    if (!existingMons.TryGetValue(rowMaMon, out var monEntity))
                    {
                        string cleanTenMon = !string.IsNullOrWhiteSpace(item.TenMon)
                            ? TrimMax(item.TenMon, 150)
                            : $"Học phần {rowMaMon}";
                        int soTc = item.SoTC > 0 ? item.SoTC : 3;

                        monEntity = new MonHoc
                        {
                            MaMon = rowMaMon,
                            TenMon = cleanTenMon,
                            SoTinChiLT = soTc,
                            SoTinChiTH = 0,
                            BacDaoTao = string.IsNullOrWhiteSpace(item.BacDT) ? null : TrimMax(item.BacDT, 10),
                            NgayTao = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        await context.MonHocs.AddAsync(monEntity, cancellationToken);
                        existingMons[rowMaMon] = monEntity;
                        ketQua.SoMonHocMoiTao++;
                    }
                    else if (monEntity.IsDeleted)
                    {
                        monEntity.IsDeleted = false;
                        monEntity.NgayCapNhat = DateTime.UtcNow;
                    }

                    string? resolvedMaGv = string.IsNullOrWhiteSpace(item.MaGV) ? null : TrimMax(item.MaGV, 20);
                    if (string.IsNullOrWhiteSpace(resolvedMaGv) && !string.IsNullOrWhiteSpace(item.TenGV))
                    {
                        gvByTen.TryGetValue(item.TenGV.Trim(), out resolvedMaGv);
                    }

                    targetLhpEntity = new LopHocPhan
                    {
                        MaLHP = rowMaLhp,
                        MaMon = monEntity.MaMon,
                        MaHocKy = maHocKy,
                        MaGV = resolvedMaGv,
                        LoaiHinhDT = string.IsNullOrWhiteSpace(item.LoaiHinhDT) ? null : TrimMax(item.LoaiHinhDT, 10),
                        GiangDayOnline = item.GiangDayOnline,
                        NgayTao = DateTime.UtcNow,
                        IsDeleted = false,
                        MonHoc = monEntity
                    };
                    await context.LopHocPhans.AddAsync(targetLhpEntity, cancellationToken);
                    existingLhps[rowMaLhp] = targetLhpEntity;
                    ketQua.SoLopHocPhanMoiTao++;
                }
                else
                {
                    if (targetLhpEntity.IsDeleted)
                    {
                        targetLhpEntity.IsDeleted = false;
                        targetLhpEntity.NgayCapNhat = DateTime.UtcNow;
                    }
                    rowMaMon = targetLhpEntity.MaMon;
                }

                // Kiểm tra trùng môn nếu người dùng bỏ chọn cho phép học lại/cải thiện
                if (!boQuaTrungMon && !string.IsNullOrWhiteSpace(rowMaMon))
                {
                    bool hasOtherClassSameMon = existingSubjectByStudent.Any(entry =>
                        entry.StartsWith($"{rawMaSv}|{rowMaMon}|", StringComparison.OrdinalIgnoreCase) &&
                        !entry.EndsWith($"|{targetLhpEntity.MaLHP}", StringComparison.OrdinalIgnoreCase));

                    if (hasOtherClassSameMon)
                    {
                        ketQua.BoQua++;
                        ketQua.ChiTietLoi.Add($"Bỏ qua SV {rawMaSv} ({targetLhpEntity.MaLHP}): Đã đăng ký cùng môn {rowMaMon} ở lớp khác.");
                        continue;
                    }
                }

                string cleanHoTen = TrimMax(item.HoTen, 100);
                string? cleanLop = string.IsNullOrWhiteSpace(item.LopSinhHoat) ? null : TrimMax(item.LopSinhHoat, 30);
                string? cleanKhoa = string.IsNullOrWhiteSpace(item.KhoaHoc) ? null : TrimMax(item.KhoaHoc, 20);

                // B. Tạo hoặc cập nhật SinhVien (mỗi MaSV chỉ tạo 1 bản ghi SinhVien duy nhất, dùng chung cho nhiều LHP)
                if (!existingStudents.TryGetValue(rawMaSv, out var sv))
                {
                    if (!tuDongTaoSv)
                    {
                        ketQua.BoQua++;
                        ketQua.ChiTietLoi.Add($"Bỏ qua SV {rawMaSv}: Chưa có hồ sơ sinh viên trong hệ thống.");
                        continue;
                    }

                    sv = new SinhVien
                    {
                        MaSV = rawMaSv,
                        HoTen = !string.IsNullOrWhiteSpace(cleanHoTen) ? cleanHoTen : $"Sinh viên {rawMaSv}",
                        LopSinhHoat = cleanLop,
                        KhoaHoc = cleanKhoa,
                        NgayTao = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    await context.SinhViens.AddAsync(sv, cancellationToken);
                    existingStudents[rawMaSv] = sv;
                    ketQua.SoSinhVienMoiTao++;
                }
                else
                {
                    if (sv.IsDeleted)
                    {
                        sv.IsDeleted = false;
                        sv.NgayCapNhat = DateTime.UtcNow;
                    }
                    if (!string.IsNullOrWhiteSpace(cleanHoTen) && (string.IsNullOrWhiteSpace(sv.HoTen) || sv.HoTen.StartsWith("Sinh viên ", StringComparison.OrdinalIgnoreCase)))
                    {
                        sv.HoTen = cleanHoTen;
                        sv.NgayCapNhat = DateTime.UtcNow;
                    }
                    if (!string.IsNullOrWhiteSpace(cleanLop) && string.IsNullOrWhiteSpace(sv.LopSinhHoat))
                    {
                        sv.LopSinhHoat = cleanLop;
                        sv.NgayCapNhat = DateTime.UtcNow;
                    }
                    if (!string.IsNullOrWhiteSpace(cleanKhoa) && string.IsNullOrWhiteSpace(sv.KhoaHoc))
                    {
                        sv.KhoaHoc = cleanKhoa;
                        sv.NgayCapNhat = DateTime.UtcNow;
                    }
                }

                string canonicalMaSv = sv.MaSV;
                string canonicalMaLhp = targetLhpEntity.MaLHP;
                string canonicalPairKey = $"{canonicalMaSv}|{canonicalMaLhp}";

                // C. Đăng ký vào Lớp học phần (bảng trung gian nhiều-nhiều DangKyHocPhan)
                if (existingRegistrationsByPair.TryGetValue(canonicalPairKey, out var dk))
                {
                    if (dk.TrangThai == "DangHoc")
                    {
                        bool updatedExisting = false;
                        if (item.DiemSo.HasValue && dk.DiemSo != item.DiemSo)
                        {
                            dk.DiemSo = item.DiemSo;
                            updatedExisting = true;
                        }
                        if (!string.IsNullOrWhiteSpace(item.DiemChu) && dk.DiemChu != item.DiemChu)
                        {
                            dk.DiemChu = TrimMax(item.DiemChu, 5);
                            updatedExisting = true;
                        }
                        if (item.SoTienDaDong > 0 && dk.SoTienDaDong != item.SoTienDaDong)
                        {
                            dk.SoTienDaDong = item.SoTienDaDong;
                            updatedExisting = true;
                        }
                        if (item.SoTienPhaiDong.HasValue && dk.SoTienPhaiDong != item.SoTienPhaiDong)
                        {
                            dk.SoTienPhaiDong = item.SoTienPhaiDong;
                            updatedExisting = true;
                        }

                        if (updatedExisting)
                        {
                            dk.NgayCapNhat = DateTime.UtcNow;
                            ketQua.SoCapNhat++;
                        }
                        ketQua.BoQua++;
                        continue;
                    }

                    // Phục hồi đăng ký đã hủy
                    dk.TrangThai = "DangHoc";
                    dk.NgayDK = DateTime.UtcNow;
                    dk.NgayCapNhat = DateTime.UtcNow;
                    ketQua.ThanhCong++;
                    if (enrolledStudentSet.Add(canonicalMaSv))
                    {
                        enrolledStudents.Add(canonicalMaSv);
                    }
                    existingSubjectByStudent.Add($"{canonicalMaSv}|{rowMaMon}|{canonicalMaLhp}");
                }
                else
                {
                    var newDk = new DangKyHocPhan
                    {
                        Id = Guid.NewGuid(),
                        MaSV = canonicalMaSv,
                        MaLHP = canonicalMaLhp,
                        TrangThai = "DangHoc",
                        HinhThucDK = string.IsNullOrWhiteSpace(item.HinhThucDK) ? "KH" : TrimMax(item.HinhThucDK, 10),
                        NguoiDK = string.IsNullOrWhiteSpace(item.NguoiDK) ? "smart_import" : TrimMax(item.NguoiDK, 50),
                        DiemSo = item.DiemSo,
                        DiemChu = string.IsNullOrWhiteSpace(item.DiemChu) ? null : TrimMax(item.DiemChu, 5),
                        SoTienDaDong = item.SoTienDaDong,
                        SoTienPhaiDong = item.SoTienPhaiDong,
                        NgayDK = DateTime.UtcNow
                    };
                    await context.DangKyHocPhans.AddAsync(newDk, cancellationToken);
                    existingRegistrationsByPair[canonicalPairKey] = newDk;
                    existingSubjectByStudent.Add($"{canonicalMaSv}|{rowMaMon}|{canonicalMaLhp}");
                    ketQua.ThanhCong++;
                    if (enrolledStudentSet.Add(canonicalMaSv))
                    {
                        enrolledStudents.Add(canonicalMaSv);
                    }
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // 5. Tính toán lại học phí cho các sinh viên vừa đăng ký
            if (enrolledStudents.Count > 0)
            {
                try
                {
                    if (enrolledStudents.Count <= 5)
                    {
                        foreach (var maSv in enrolledStudents)
                        {
                            await _hocPhiService.TinhLaiHocPhiAsync(maSv, maHocKy);
                        }
                    }
                    else
                    {
                        await _hocPhiService.TinhLaiHocPhiHangLoatAsync(enrolledStudents, maHocKy);
                    }
                }
                catch (Exception ex)
                {
                    ketQua.ChiTietLoi.Add($"Lưu ý: Đã ghi nhận vào lớp nhưng tính lại học phí gặp lỗi ({ExtractInnerMessage(ex)}).");
                }
            }

            if (ketQua.ThanhCong > 0)
            {
                var extraParts = new List<string>();
                extraParts.Add($"Tạo mới {ketQua.SoSinhVienMoiTao} hồ sơ SV");
                if (ketQua.SoMonHocMoiTao > 0) extraParts.Add($"{ketQua.SoMonHocMoiTao} môn học mới");
                if (ketQua.SoLopHocPhanMoiTao > 0) extraParts.Add($"{ketQua.SoLopHocPhanMoiTao} LHP mới");
                if (ketQua.SoCapNhat > 0) extraParts.Add($"cập nhật {ketQua.SoCapNhat} dòng");
                if (ketQua.BoQua > 0) extraParts.Add($"bỏ qua {ketQua.BoQua} dòng đã có");

                string targetDesc = defaultLhp != null && selectedItems.All(x => string.IsNullOrWhiteSpace(x.MaLHP) || string.Equals(x.MaLHP.Trim(), defaultLhp.MaLHP, StringComparison.OrdinalIgnoreCase))
                    ? $"vào lớp {defaultLhp.MaLHP} ({defaultLhp.MonHoc?.TenMon ?? defaultLhp.MaMon})"
                    : $"({enrolledStudents.Count} sinh viên trên các lớp học phần)";

                ketQua.ThongBao = $"Đã nạp thành công {ketQua.ThanhCong} lượt đăng ký {targetDesc}. {string.Join(", ", extraParts)}.";
            }
            else if (ketQua.SoCapNhat > 0)
            {
                ketQua.ThongBao = $"Đã cập nhật thông tin cho {ketQua.SoCapNhat} lượt đăng ký đã có trong hệ thống.";
            }
            else if (ketQua.Loi == 0 && ketQua.BoQua > 0)
            {
                string detail = ketQua.ChiTietLoi.FirstOrDefault() ?? "Tất cả các cặp (Sinh viên - Lớp học phần) đã có sẵn trong CSDL.";
                ketQua.ThongBao = $"Không có lượt đăng ký mới nào cần thêm (đã có {ketQua.BoQua} dòng). {detail}";
            }
            else
            {
                string detail = ketQua.ChiTietLoi.FirstOrDefault() ?? "Vui lòng kiểm tra lại dữ liệu.";
                ketQua.ThongBao = $"Nạp thất bại: {detail}";
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            string innerMsg = ExtractInnerMessage(ex);
            ketQua.ThongBao = $"Lỗi khi nạp dữ liệu vào CSDL: {innerMsg}";
            ketQua.Loi = selectedItems.Count;
            ketQua.ChiTietLoi.Add(innerMsg);
        }

        return ketQua;
    }

    private static string TrimMax(string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLen ? trimmed : trimmed.Substring(0, maxLen);
    }

    private static string ExtractInnerMessage(Exception ex)
    {
        var current = ex;
        while (current.InnerException != null)
            current = current.InnerException;
        return current.Message;
    }
}

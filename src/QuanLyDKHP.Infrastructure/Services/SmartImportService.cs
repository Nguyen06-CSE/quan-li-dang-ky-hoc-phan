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

    public Task<List<SinhVienImportPreviewDto>> ParseExcelStreamAsync(Stream stream, ColumnMappingDto? mapping = null, CancellationToken cancellationToken = default)
    {
        var matrix = new List<string[]>();

        if (stream.CanSeek) stream.Position = 0;
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            return Task.FromResult(new List<SinhVienImportPreviewDto>());

        var range = worksheet.RangeUsed();
        var rows = range?.RowsUsed()?.ToList();
        if (rows == null || rows.Count == 0)
            return Task.FromResult(new List<SinhVienImportPreviewDto>());

        int maxCols = range?.ColumnCount() ?? 1;

        foreach (var row in rows)
        {
            var cellValues = row.Cells(1, maxCols)
                                .Select(c => c.GetString().Trim())
                                .ToArray();
            if (cellValues.Any(v => !string.IsNullOrEmpty(v)))
            {
                matrix.Add(cellValues);
            }
        }

        var result = ProcessParsedMatrix(matrix, mapping);
        return Task.FromResult(result);
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

        int startIndex = 0;

        // Tự động nhận diện header nếu mapping chưa chỉ định
        if (maSvCol == -1 && hoTenCol == -1)
        {
            for (int r = 0; r < Math.Min(3, matrix.Count); r++)
            {
                var row = matrix[r];
                for (int c = 0; c < row.Length; c++)
                {
                    string header = row[c].ToLowerInvariant();
                    if (maSvCol == -1 && (header.Contains("mssv") || header.Contains("mã sv") || header.Contains("mã sinh viên") || header.Equals("id") || header.Contains("masv")))
                        maSvCol = c;
                    else if (hoTenCol == -1 && (header.Contains("họ và tên") || header.Contains("họ tên") || header.Contains("fullname") || header.Contains("hoten")))
                        hoTenCol = c;
                    else if (hoDemCol == -1 && (header.Contains("họ đệm") || header.Contains("họ lót") || header.Equals("họ")))
                        hoDemCol = c;
                    else if (tenCol == -1 && (header.Equals("tên") || header.Equals("ten")))
                        tenCol = c;
                    else if (lopCol == -1 && (header.Contains("lớp") || header.Contains("lop") || header.Contains("class")))
                        lopCol = c;
                    else if (khoaCol == -1 && (header.Contains("khóa") || header.Contains("khoa") || header.Contains("cohort")))
                        khoaCol = c;
                }

                if (maSvCol != -1 || hoTenCol != -1)
                {
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
                // Kiểm tra xem cột nào là số/MSSV
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
                // Kiểm tra nếu cột 0 là số thứ tự 1, 2, 3...
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

            string maSv = maSvCol >= 0 && maSvCol < row.Length ? row[maSvCol].Trim() : string.Empty;
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

            string lop = lopCol >= 0 && lopCol < row.Length ? row[lopCol].Trim() : string.Empty;
            string khoa = khoaCol >= 0 && khoaCol < row.Length ? row[khoaCol].Trim() : string.Empty;

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
                TrangThaiXacThuc = status,
                ThongBaoLoi = error,
                IsSelected = status == "HopLe"
            });
        }

        return list;
    }

    public async Task<ClassInsightsDto> AnalyzeAndValidateAsync(
        string maHocKy,
        string maLHP,
        List<SinhVienImportPreviewDto> items,
        bool tuDongTaoSv = true)
    {
        var insights = new ClassInsightsDto
        {
            MaLHP = maLHP
        };

        if (items == null || items.Count == 0)
            return insights;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var lhp = await context.LopHocPhans.AsNoTracking()
            .Include(l => l.MonHoc)
            .FirstOrDefaultAsync(l => l.MaLHP == maLHP);

        if (lhp != null)
        {
            insights.TenMon = lhp.MonHoc?.TenMon ?? lhp.MaMon;
            insights.SiSoToiDa = lhp.SiSoToiDa ?? 0;
        }

        // Lấy danh sách sinh viên hiện đang học trong lớp này
        var existingRegistrations = await context.DangKyHocPhans.AsNoTracking()
            .Where(dk => dk.MaLHP == maLHP && dk.TrangThai == "DangHoc")
            .Select(dk => dk.MaSV)
            .ToListAsync();

        insights.SiSoHienTai = existingRegistrations.Count;
        var existingSet = new HashSet<string>(existingRegistrations, StringComparer.OrdinalIgnoreCase);

        // Lấy danh sách mã SV trong danh sách import
        var importMaSvs = items.Select(x => x.MaSV).Where(m => !string.IsNullOrEmpty(m)).Distinct().ToList();

        // Kiểm tra sinh viên nào đã có hồ sơ trong bảng SinhVien
        var existingDbStudents = await context.SinhViens.AsNoTracking()
            .Where(s => importMaSvs.Contains(s.MaSV))
            .ToDictionaryAsync(s => s.MaSV, StringComparer.OrdinalIgnoreCase);

        // Kiểm tra trùng lịch và trùng môn trong cùng học kỳ
        var otherDks = await context.DangKyHocPhans.AsNoTracking()
            .Where(dk => importMaSvs.Contains(dk.MaSV) 
                         && dk.LopHocPhan.MaHocKy == maHocKy 
                         && dk.MaLHP != maLHP 
                         && dk.TrangThai == "DangHoc")
            .Include(dk => dk.LopHocPhan)
                .ThenInclude(l => l.MonHoc)
            .ToListAsync();

        var otherDkGroup = otherDks.GroupBy(dk => dk.MaSV, StringComparer.OrdinalIgnoreCase)
                                   .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Kiểm tra từng sinh viên trong danh sách preview
        int hopLe = 0;
        int daCo = 0;
        int loi = 0;

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.MaSV))
            {
                item.TrangThaiXacThuc = "Loi";
                item.ThongBaoLoi = "Mã sinh viên trống";
                item.IsSelected = false;
                loi++;
                continue;
            }

            // Nếu sinh viên đã có trong hệ thống nhưng ở file thiếu họ tên, tự bù đắp thông tin
            if (existingDbStudents.TryGetValue(item.MaSV, out var dbSv))
            {
                if (string.IsNullOrWhiteSpace(item.HoTen))
                    item.HoTen = dbSv.HoTen;
                if (string.IsNullOrWhiteSpace(item.LopSinhHoat))
                    item.LopSinhHoat = dbSv.LopSinhHoat ?? "";
                if (string.IsNullOrWhiteSpace(item.KhoaHoc))
                    item.KhoaHoc = dbSv.KhoaHoc ?? "";
            }

            // Kiểm tra đã có trong lớp này chưa
            if (existingSet.Contains(item.MaSV))
            {
                item.TrangThaiXacThuc = "DaCoTrongLop";
                item.ThongBaoLoi = "Đã có trong lớp học phần này";
                item.IsSelected = false; // Mặc định bỏ chọn để tránh đăng ký trùng
                daCo++;
                continue;
            }

            // Kiểm tra xung đột trùng môn hoặc trùng lịch học
            item.CanhBaoXungDot = null;
            if (otherDkGroup.TryGetValue(item.MaSV, out var studentOtherDks))
            {
                // Trùng môn
                if (lhp != null)
                {
                    var sameMonDk = studentOtherDks.FirstOrDefault(d => d.LopHocPhan.MaMon == lhp.MaMon);
                    if (sameMonDk != null)
                    {
                        item.CanhBaoXungDot = $"Đã đăng ký lớp {sameMonDk.MaLHP} cùng môn {lhp.MaMon}";
                        insights.DanhSachCanhBao.Add($"SV {item.MaSV} ({item.HoTen}): Đã đăng ký lớp {sameMonDk.MaLHP} cùng môn {lhp.MaMon}");
                    }
                }

                // Trùng lịch ca học / thứ học
                if (lhp != null && lhp.Thu.HasValue && lhp.TietBatDau.HasValue && lhp.SoTiet.HasValue)
                {
                    int lhpTietKetThuc = lhp.TietBatDau.Value + lhp.SoTiet.Value - 1;
                    foreach (var oDk in studentOtherDks)
                    {
                        var oLhp = oDk.LopHocPhan;
                        if (oLhp.Thu == lhp.Thu && oLhp.TietBatDau.HasValue && oLhp.SoTiet.HasValue)
                        {
                            int oKetThuc = oLhp.TietBatDau.Value + oLhp.SoTiet.Value - 1;
                            // Hai khoảng thời gian giao nhau nếu startA <= endB và startB <= endA
                            if (lhp.TietBatDau.Value <= oKetThuc && oLhp.TietBatDau.Value <= lhpTietKetThuc)
                            {
                                string conflictMsg = $"Trùng lịch Thứ {lhp.Thu} (tiết {lhp.TietBatDau}-{lhpTietKetThuc}) với {oLhp.MaLHP}";
                                item.CanhBaoXungDot = string.IsNullOrEmpty(item.CanhBaoXungDot)
                                    ? conflictMsg
                                    : $"{item.CanhBaoXungDot}; {conflictMsg}";
                                insights.DanhSachCanhBao.Add($"SV {item.MaSV} ({item.HoTen}): {conflictMsg}");
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

        insights.SoLuongHopLe = hopLe;
        insights.SoLuongDaCo = daCo;
        insights.SoLuongLoi = loi;
        insights.SiSoThemVao = items.Count(x => x.IsHopLe && x.IsSelected);

        // Phân tích cơ cấu lớp (Class Demographics)
        var validSelected = items.Where(x => x.IsHopLe && x.IsSelected).ToList();
        var allStudentsInClass = new List<(string Lop, string Khoa)>();

        // Thêm các SV mới chuẩn bị nạp
        allStudentsInClass.AddRange(validSelected.Select(s => (
            Lop: string.IsNullOrWhiteSpace(s.LopSinhHoat) ? "Khác" : s.LopSinhHoat,
            Khoa: string.IsNullOrWhiteSpace(s.KhoaHoc) ? "Khác" : s.KhoaHoc
        )));

        // Thêm SV hiện có trong lớp
        var existingSvInfo = await context.SinhViens.AsNoTracking()
            .Where(s => existingRegistrations.Contains(s.MaSV))
            .Select(s => new { s.LopSinhHoat, s.KhoaHoc })
            .ToListAsync();

        allStudentsInClass.AddRange(existingSvInfo.Select(s => (
            Lop: string.IsNullOrWhiteSpace(s.LopSinhHoat) ? "Khác" : s.LopSinhHoat,
            Khoa: string.IsNullOrWhiteSpace(s.KhoaHoc) ? "Khác" : s.KhoaHoc
        )));

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
            MaLHP = maLHP,
            TongSoLuong = items?.Count ?? 0
        };

        if (items == null || items.Count == 0)
        {
            ketQua.ThongBao = "Không có sinh viên nào để nạp.";
            return ketQua;
        }

        var selectedItems = items.Where(x => x.IsSelected && x.IsHopLe).ToList();
        if (selectedItems.Count == 0)
        {
            ketQua.ThongBao = "Không có sinh viên hợp lệ nào được chọn để nạp.";
            return ketQua;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var lhp = await context.LopHocPhans
            .Include(l => l.MonHoc)
            .FirstOrDefaultAsync(l => l.MaLHP == maLHP, cancellationToken);

        if (lhp == null)
        {
            ketQua.ThongBao = $"Không tìm thấy lớp học phần {maLHP}.";
            ketQua.Loi = selectedItems.Count;
            return ketQua;
        }

        var hk = await context.HocKys.AsNoTracking()
            .FirstOrDefaultAsync(h => h.MaHocKy == maHocKy, cancellationToken);

        if (hk != null && !hk.DangMo)
        {
            ketQua.ThongBao = $"Học kỳ {hk.TenHocKy} đã đóng cổng, không thể nạp đăng ký.";
            ketQua.Loi = selectedItems.Count;
            return ketQua;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var studentIds = selectedItems.Select(x => x.MaSV).Distinct().ToList();

            // 1. Tải các sinh viên hiện có
            var existingStudents = await context.SinhViens
                .Where(s => studentIds.Contains(s.MaSV))
                .ToDictionaryAsync(s => s.MaSV, cancellationToken);

            // 2. Tải các đăng ký hiện có của lớp này
            var existingRegistrations = await context.DangKyHocPhans
                .Where(dk => dk.MaLHP == maLHP && studentIds.Contains(dk.MaSV))
                .ToDictionaryAsync(dk => dk.MaSV, cancellationToken);

            var enrolledStudents = new List<string>();

            foreach (var item in selectedItems)
            {
                // A. Tạo hoặc cập nhật SinhVien
                if (!existingStudents.TryGetValue(item.MaSV, out var sv))
                {
                    if (tuDongTaoSv)
                    {
                        sv = new SinhVien
                        {
                            MaSV = item.MaSV,
                            HoTen = !string.IsNullOrWhiteSpace(item.HoTen) ? item.HoTen : $"Sinh viên {item.MaSV}",
                            LopSinhHoat = item.LopSinhHoat,
                            KhoaHoc = item.KhoaHoc,
                            NgayTao = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        await context.SinhViens.AddAsync(sv, cancellationToken);
                        existingStudents[item.MaSV] = sv;
                        ketQua.SoSinhVienMoiTao++;
                    }
                    else
                    {
                        ketQua.BoQua++;
                        ketQua.ChiTietLoi.Add($"Bỏ qua SV {item.MaSV}: Chưa có hồ sơ sinh viên trong hệ thống.");
                        continue;
                    }
                }
                else
                {
                    // Nếu sinh viên đã có trong hệ thống nhưng đã bị xóa mềm, phục hồi
                    if (sv.IsDeleted)
                    {
                        sv.IsDeleted = false;
                        sv.NgayCapNhat = DateTime.UtcNow;
                    }
                    if (!string.IsNullOrWhiteSpace(item.HoTen) && string.IsNullOrWhiteSpace(sv.HoTen))
                    {
                        sv.HoTen = item.HoTen;
                        sv.NgayCapNhat = DateTime.UtcNow;
                    }
                }

                // B. Đăng ký vào Lớp học phần
                if (existingRegistrations.TryGetValue(item.MaSV, out var dk))
                {
                    if (dk.TrangThai == "DangHoc")
                    {
                        ketQua.BoQua++;
                        continue;
                    }
                    else
                    {
                        // Phục hồi đăng ký đã hủy
                        dk.TrangThai = "DangHoc";
                        dk.NgayDK = DateTime.UtcNow;
                        ketQua.ThanhCong++;
                        enrolledStudents.Add(item.MaSV);
                    }
                }
                else
                {
                    var newDk = new DangKyHocPhan
                    {
                        MaSV = item.MaSV,
                        MaLHP = maLHP,
                        TrangThai = "DangHoc",
                        HinhThucDK = "KH",
                        NguoiDK = "smart_import",
                        NgayDK = DateTime.UtcNow
                    };
                    await context.DangKyHocPhans.AddAsync(newDk, cancellationToken);
                    ketQua.ThanhCong++;
                    enrolledStudents.Add(item.MaSV);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // 3. Tính toán lại học phí cho các sinh viên vừa đăng ký
            foreach (var maSv in enrolledStudents)
            {
                try
                {
                    await _hocPhiService.TinhLaiHocPhiAsync(maSv, maHocKy);
                }
                catch { }
            }

            ketQua.ThongBao = $"Đã nạp thành công {ketQua.ThanhCong} sinh viên vào lớp {maLHP} ({lhp.MonHoc?.TenMon ?? lhp.MaMon}). Tạo mới {ketQua.SoSinhVienMoiTao} hồ sơ sinh viên.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            ketQua.ThongBao = $"Lỗi khi nạp sinh viên vào lớp: {ex.Message}";
            ketQua.Loi = selectedItems.Count;
            ketQua.ChiTietLoi.Add(ex.Message);
        }

        return ketQua;
    }
}

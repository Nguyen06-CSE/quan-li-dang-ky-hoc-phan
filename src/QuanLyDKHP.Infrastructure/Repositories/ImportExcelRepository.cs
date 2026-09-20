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

public class ImportExcelRepository : IImportExcelService
{
    private readonly AppDbContext _context;
    private readonly IHocPhiService _hocPhiService;

    public ImportExcelRepository(AppDbContext context, IHocPhiService hocPhiService)
    {
        _context = context;
        _hocPhiService = hocPhiService;
    }

    public async Task<KetQuaImportDto> ImportFileAsync(
        Stream stream,
        string maHocKy,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var ketQua = new KetQuaImportDto();

        if (string.IsNullOrWhiteSpace(maHocKy))
        {
            ketQua.DanhSachLog.Add(new LogDongImportDto
            {
                DongSo = 0,
                LoaiLog = "Loi",
                NoiDung = "Chưa chọn học kỳ đích để import."
            });
            return ketQua;
        }

        using var workbook = new XLWorkbook(stream);
        // Ưu tiên sheet "CT", nếu không có thì lấy sheet đầu tiên
        var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("CT", StringComparison.OrdinalIgnoreCase))
                        ?? workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
        {
            ketQua.DanhSachLog.Add(new LogDongImportDto
            {
                DongSo = 0,
                LoaiLog = "Loi",
                NoiDung = "Không tìm thấy sheet dữ liệu trong file Excel."
            });
            return ketQua;
        }

        var rows = worksheet.RangeUsed()?.RowsUsed()?.ToList();
        if (rows == null || rows.Count <= 1)
        {
            ketQua.DanhSachLog.Add(new LogDongImportDto
            {
                DongSo = 0,
                LoaiLog = "Loi",
                NoiDung = "File Excel không có dòng dữ liệu nào."
            });
            return ketQua;
        }

        // Bỏ dòng tiêu đề (Header row 1)
        var dataRows = rows.Skip(1).ToList();
        ketQua.TongSoDong = dataRows.Count;

        var danhSachMaSvCanTinhHocPhi = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Preload cache để tăng tốc độ kiểm tra và upsert
        var svCache = await _context.SinhViens.ToDictionaryAsync(s => s.MaSV.Trim().ToUpper(), s => s, cancellationToken);
        var monCache = await _context.MonHocs.ToDictionaryAsync(m => m.MaMon.Trim().ToUpper(), m => m, cancellationToken);
        var lhpCache = await _context.LopHocPhans.ToDictionaryAsync(l => l.MaLHP.Trim().ToUpper(), l => l, cancellationToken);
        
        // Cache cặp (MaSV, MaLHP)
        var dkList = await _context.DangKyHocPhans
            .Select(dk => (dk.MaSV.Trim().ToUpper() + "_" + dk.MaLHP.Trim().ToUpper()))
            .ToListAsync(cancellationToken);
        var dkCache = new HashSet<string>(dkList);

        int rowIndex = 1; // đếm số dòng dữ liệu

        foreach (var row in dataRows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowIndex++;
            int excelRowNumber = row.RowNumber();

            try
            {
                // Đọc 17 cột theo spec 015
                string maSV = row.Cell(1).GetString().Trim();
                string tenSV = row.Cell(2).GetString().Trim();
                string lopSinhHoat = row.Cell(3).GetString().Trim();
                string khoaHoc = row.Cell(4).GetString().Trim();
                string maMon = row.Cell(5).GetString().Trim();
                string tenMon = row.Cell(6).GetString().Trim();
                string maLHP = row.Cell(7).GetString().Trim();
                string soTcStr = row.Cell(8).GetString().Trim();
                string maGV = row.Cell(9).GetString().Trim();
                string tenGV = row.Cell(10).GetString().Trim();
                string bacDT = row.Cell(11).GetString().Trim();
                string loaiHinhDT = row.Cell(12).GetString().Trim();
                string hinhThucDK = row.Cell(13).GetString().Trim();
                string ngayDkStr = row.Cell(14).GetString().Trim();
                string nguoiDK = row.Cell(15).GetString().Trim();
                string diemSoStr = row.Cell(16).GetString().Trim();
                string diemChu = row.Cell(17).GetString().Trim();
                string giangDayOnlineStr = row.Cell(21).GetString().Trim();

                // 1. Validate bắt buộc
                if (string.IsNullOrWhiteSpace(maSV) || string.IsNullOrWhiteSpace(tenSV) ||
                    string.IsNullOrWhiteSpace(maMon) || string.IsNullOrWhiteSpace(tenMon) ||
                    string.IsNullOrWhiteSpace(maLHP))
                {
                    ketQua.SoDongLoi++;
                    ketQua.DanhSachLog.Add(new LogDongImportDto
                    {
                        DongSo = excelRowNumber,
                        MaSV = maSV,
                        MaMon = maMon,
                        MaLHP = maLHP,
                        LoaiLog = "Loi",
                        NoiDung = "Thiếu thông tin bắt buộc (Mã SV, Tên SV, Mã môn, Tên môn hoặc Mã LHP bị rỗng)."
                    });
                    continue;
                }

                // Parse số tín chỉ
                int soTC = 3; // default
                if (int.TryParse(soTcStr, out int tcParsed) && tcParsed > 0)
                {
                    soTC = tcParsed;
                }

                // Parse ngày ĐK
                DateTime ngayDK = DateTime.UtcNow;
                if (DateTime.TryParse(ngayDkStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtParsed))
                {
                    ngayDK = dtParsed;
                }
                else if (DateTime.TryParse(ngayDkStr, new CultureInfo("vi-VN"), DateTimeStyles.None, out DateTime dtVi))
                {
                    ngayDK = dtVi;
                }

                // Parse điểm số
                decimal? diemSo = null;
                if (decimal.TryParse(diemSoStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal ds))
                {
                    diemSo = ds;
                }
                else if (decimal.TryParse(diemSoStr, NumberStyles.Number, new CultureInfo("vi-VN"), out decimal dsVi))
                {
                    diemSo = dsVi;
                }

                // 2. Upsert SinhVien
                string svKey = maSV.ToUpper();
                if (!svCache.TryGetValue(svKey, out var svEntity))
                {
                    svEntity = new SinhVien
                    {
                        MaSV = maSV,
                        HoTen = tenSV,
                        LopSinhHoat = string.IsNullOrWhiteSpace(lopSinhHoat) ? null : lopSinhHoat,
                        KhoaHoc = string.IsNullOrWhiteSpace(khoaHoc) ? null : khoaHoc,
                        NgayTao = DateTime.UtcNow
                    };
                    _context.SinhViens.Add(svEntity);
                    svCache[svKey] = svEntity;
                    ketQua.SoSinhVienMoi++;
                }
                else
                {
                    // Cảnh báo nếu lệch thông tin, không ghi đè
                    if (!string.Equals(svEntity.HoTen, tenSV, StringComparison.OrdinalIgnoreCase))
                    {
                        ketQua.DanhSachLog.Add(new LogDongImportDto
                        {
                            DongSo = excelRowNumber,
                            MaSV = maSV,
                            LoaiLog = "CanhBao",
                            NoiDung = $"Họ tên SV trong file ('{tenSV}') khác tên hiện tại trong hệ thống ('{svEntity.HoTen}'). Giữ nguyên dữ liệu hiện có."
                        });
                    }
                }

                // 3. Upsert MonHoc
                string monKey = maMon.ToUpper();
                if (!monCache.TryGetValue(monKey, out var monEntity))
                {
                    monEntity = new MonHoc
                    {
                        MaMon = maMon,
                        TenMon = tenMon,
                        SoTinChiLT = soTC,
                        SoTinChiTH = 0,
                        BacDaoTao = string.IsNullOrWhiteSpace(bacDT) ? null : bacDT,
                        NgayTao = DateTime.UtcNow
                    };
                    _context.MonHocs.Add(monEntity);
                    monCache[monKey] = monEntity;
                    ketQua.SoMonHocMoi++;

                    ketQua.DanhSachLog.Add(new LogDongImportDto
                    {
                        DongSo = excelRowNumber,
                        MaMon = maMon,
                        LoaiLog = "CanhBao",
                        NoiDung = $"Môn [{maMon}] — {tenMon}: số TC LT/TH được gán mặc định ({soTC} LT / 0 TH), vui lòng vào màn hình Môn học để chỉnh lại cho đúng."
                    });
                }
                else
                {
                    int tongTcHeThong = monEntity.SoTinChiLT + monEntity.SoTinChiTH;
                    if (tongTcHeThong != soTC)
                    {
                        ketQua.DanhSachLog.Add(new LogDongImportDto
                        {
                            DongSo = excelRowNumber,
                            MaMon = maMon,
                            LoaiLog = "CanhBao",
                            NoiDung = $"Môn [{maMon}] — {tenMon}: tổng số TC trong file ({soTC}) khác tổng TC hệ thống ({tongTcHeThong}). Giữ nguyên cấu hình hệ thống."
                        });
                    }
                }

                // 4. Upsert LopHocPhan
                string lhpKey = maLHP.ToUpper();
                if (!lhpCache.TryGetValue(lhpKey, out var lhpEntity))
                {
                    lhpEntity = new LopHocPhan
                    {
                        MaLHP = maLHP,
                        MaMon = maMon,
                        MaHocKy = maHocKy,
                        MaGV = string.IsNullOrWhiteSpace(maGV) ? null : maGV,
                        LoaiHinhDT = string.IsNullOrWhiteSpace(loaiHinhDT) ? null : loaiHinhDT,
                        GiangDayOnline = !string.IsNullOrWhiteSpace(giangDayOnlineStr),
                        NgayTao = DateTime.UtcNow
                    };
                    _context.LopHocPhans.Add(lhpEntity);
                    lhpCache[lhpKey] = lhpEntity;
                    ketQua.SoLopHocPhanMoi++;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(lhpEntity.MaGV) && !string.IsNullOrWhiteSpace(maGV))
                    {
                        lhpEntity.MaGV = maGV;
                    }
                }

                // 5. Upsert DangKyHocPhan (unique MaSV, MaLHP)
                string dkKey = svKey + "_" + lhpKey;
                if (!dkCache.Contains(dkKey))
                {
                    var dkEntity = new DangKyHocPhan
                    {
                        Id = Guid.NewGuid(),
                        MaSV = maSV,
                        MaLHP = maLHP,
                        HinhThucDK = string.IsNullOrWhiteSpace(hinhThucDK) ? null : hinhThucDK,
                        NgayDK = ngayDK,
                        NguoiDK = string.IsNullOrWhiteSpace(nguoiDK) ? null : nguoiDK,
                        DiemSo = diemSo,
                        DiemChu = string.IsNullOrWhiteSpace(diemChu) ? null : diemChu,
                        TrangThai = "DangHoc",
                        SoTienPhaiDong = 0,
                        SoTienDaDong = 0,
                        NgayTao = DateTime.UtcNow
                    };
                    _context.DangKyHocPhans.Add(dkEntity);
                    dkCache.Add(dkKey);
                    ketQua.SoDangKyMoi++;
                }

                danhSachMaSvCanTinhHocPhi.Add(maSV);
                ketQua.SoDongThanhCong++;
            }
            catch (Exception ex)
            {
                ketQua.SoDongLoi++;
                ketQua.DanhSachLog.Add(new LogDongImportDto
                {
                    DongSo = excelRowNumber,
                    LoaiLog = "Loi",
                    NoiDung = $"Lỗi xử lý dòng {excelRowNumber}: {ex.Message}"
                });
            }

            if (progress != null && ketQua.TongSoDong > 0)
            {
                int pct = (int)((double)(rowIndex - 1) / ketQua.TongSoDong * 90);
                progress.Report(pct);
            }
        }

        // Lưu toàn bộ thay đổi vào CSDL
        await _context.SaveChangesAsync(cancellationToken);

        // 6. Tính lại học phí cho tất cả sinh viên vừa import trong học kỳ này
        if (danhSachMaSvCanTinhHocPhi.Count > 0 && _hocPhiService != null)
        {
            int svIndex = 0;
            foreach (var maSV in danhSachMaSvCanTinhHocPhi)
            {
                try
                {
                    await _hocPhiService.TinhLaiHocPhiAsync(maSV, maHocKy);
                }
                catch (Exception ex)
                {
                    ketQua.DanhSachLog.Add(new LogDongImportDto
                    {
                        DongSo = 0,
                        MaSV = maSV,
                        LoaiLog = "CanhBao",
                        NoiDung = $"Lỗi tự động tính học phí cho SV {maSV}: {ex.Message}"
                    });
                }
                svIndex++;
                if (progress != null)
                {
                    int pct = 90 + (int)((double)svIndex / danhSachMaSvCanTinhHocPhi.Count * 10);
                    progress.Report(pct);
                }
            }
        }

        progress?.Report(100);
        return ketQua;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

class Program
{
    static void Main()
    {
        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "sample_data_import_50.xlsx");
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("CT");

        // 1. Headers (21 cột chuẩn theo Spec 015)
        string[] headers = new string[]
        {
            "Mã SV", "Tên SV", "Lớp sinh hoạt", "Khóa Học",
            "Mã môn", "Tên môn", "Mã LHP", "Số TC",
            "Mã GV", "Tên GV", "Bậc ĐT", "Loại Hình ĐT",
            "Hình Thức ĐK", "Ngày ĐK", "Người ĐK",
            "Điểm số", "Điểm chữ",
            "T/Trạng học phí", "Đã đóng", "Phải đóng", "Giảng dạy Online"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#8CB450");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        // Danh sách Sinh viên
        var sinhViens = new (string MaSV, string HoTen, string Lop, string Khoa)[]
        {
            ("SV2025001", "Nguyễn Văn An", "21CNTT1", "K2021"),
            ("SV2025002", "Trần Thị Bích", "21CNTT1", "K2021"),
            ("SV2025003", "Lê Hoàng Cường", "21CNTT2", "K2021"),
            ("SV2025004", "Phạm Minh Đức", "21CNTT2", "K2021"),
            ("SV2025005", "Vũ Hải Đăng", "22CNTT1", "K2022"),
            ("SV2025006", "Hoàng Ngọc Hân", "22CNTT1", "K2022"),
            ("SV2025007", "Đỗ Quang Hùng", "22CNTT2", "K2022"),
            ("SV2025008", "Ngô Bảo Khánh", "22CNTT2", "K2022"),
            ("SV2025009", "Bùi Thanh Lâm", "23CNTT1", "K2023"),
            ("SV2025010", "Dương Thu Mai", "23CNTT1", "K2023"),
            ("SV2025011", "Trịnh Gia Nam", "23CNTT2", "K2023"),
            ("SV2025012", "Lý Tuyết Nhung", "23CNTT2", "K2023"),
            ("SV2025013", "Võ Minh Phúc", "23CNTT1", "K2023"),
            ("SV2025014", "Đặng Thùy Trang", "22CNTT2", "K2022"),
        };

        // Danh sách Môn học & LHP
        var monHocs = new (string MaMon, string TenMon, int SoTC, string MaLHP, string MaGV, string TenGV)[]
        {
            ("INT101", "Lập trình C/C++ cơ bản", 3, "LHP_INT101_01", "GV01", "TS. Nguyễn Văn Toàn"),
            ("INT102", "Cấu trúc dữ liệu & Giải thuật", 4, "LHP_INT102_01", "GV01", "TS. Nguyễn Văn Toàn"),
            ("INT103", "Cơ sở dữ liệu", 3, "LHP_INT103_01", "GV02", "ThS. Trần Thị Lan"),
            ("INT104", "Lập trình hướng đối tượng", 4, "LHP_INT104_01", "GV02", "ThS. Trần Thị Lan"),
            ("INT105", "Mạng máy tính", 3, "LHP_INT105_01", "GV03", "TS. Lê Quang Vũ"),
            ("INT106", "Kiến trúc máy tính & HĐH", 3, "LHP_INT106_01", "GV03", "TS. Lê Quang Vũ"),
            ("INT107", "Công nghệ phần mềm", 3, "LHP_INT107_01", "GV04", "ThS. Phạm Thu Hà"),
            ("INT108", "Trí tuệ nhân tạo", 3, "LHP_INT108_01", "GV04", "ThS. Phạm Thu Hà"),
            ("INT109", "Phát triển ứng dụng Web", 4, "LHP_INT109_01", "GV01", "TS. Nguyễn Văn Toàn"),
            ("INT110", "An toàn thông tin", 3, "LHP_INT110_01", "GV03", "TS. Lê Quang Vũ"),
        };

        int count = 0;
        int rowIdx = 2;
        var rand = new Random(42);

        foreach (var sv in sinhViens)
        {
            // Mỗi SV đăng ký 3-4 môn
            for (int m = 0; m < monHocs.Length; m++)
            {
                if (count >= 50) break;
                // chọn môn theo vòng lặp
                if ((count + m) % 2 == 0)
                {
                    var mon = monHocs[m];
                    count++;

                    decimal? diemSo = rand.NextDouble() > 0.3 ? Math.Round((decimal)(5.0 + rand.NextDouble() * 4.5), 1) : null;
                    string diemChu = diemSo.HasValue ? (diemSo >= 8.5m ? "A" : (diemSo >= 7.0m ? "B" : (diemSo >= 5.5m ? "C" : "D"))) : "";
                    string online = rand.NextDouble() > 0.7 ? "X" : "";

                    ws.Cell(rowIdx, 1).Value = sv.MaSV;
                    ws.Cell(rowIdx, 2).Value = sv.HoTen;
                    ws.Cell(rowIdx, 3).Value = sv.Lop;
                    ws.Cell(rowIdx, 4).Value = sv.Khoa;
                    ws.Cell(rowIdx, 5).Value = mon.MaMon;
                    ws.Cell(rowIdx, 6).Value = mon.TenMon;
                    ws.Cell(rowIdx, 7).Value = mon.MaLHP;
                    ws.Cell(rowIdx, 8).Value = mon.SoTC;
                    ws.Cell(rowIdx, 9).Value = mon.MaGV;
                    ws.Cell(rowIdx, 10).Value = mon.TenGV;
                    ws.Cell(rowIdx, 11).Value = "Đại học";
                    ws.Cell(rowIdx, 12).Value = "Chính quy";
                    ws.Cell(rowIdx, 13).Value = "Trực tuyến";
                    ws.Cell(rowIdx, 14).Value = "01/09/2025 08:30";
                    ws.Cell(rowIdx, 15).Value = "sv_online";
                    if (diemSo.HasValue) ws.Cell(rowIdx, 16).Value = diemSo.Value;
                    ws.Cell(rowIdx, 17).Value = diemChu;
                    ws.Cell(rowIdx, 18).Value = "Đã đóng";
                    ws.Cell(rowIdx, 19).Value = 1500000;
                    ws.Cell(rowIdx, 20).Value = 1500000;
                    ws.Cell(rowIdx, 21).Value = online;

                    // Borders
                    for (int c = 1; c <= 21; c++)
                    {
                        ws.Cell(rowIdx, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Cell(rowIdx, c).Style.Border.OutsideBorderColor = XLColor.FromHtml("#E0E6DA");
                    }

                    rowIdx++;
                }
            }
            if (count >= 50) break;
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(outputPath);
        Console.WriteLine($"Đã tạo thành công {count} dòng trong file: {outputPath}");
    }
}

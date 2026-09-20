using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Infrastructure.Export;

public class PdfExportService : IPdfExportService
{
    static PdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> XuatPhieuHocPhiPdfAsync(HocPhiChiTietDto chiTiet)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24, Unit.Point);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(header =>
                {
                    header.Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("TRƯỜNG ĐẠI HỌC CÔNG NGHỆ").Bold().FontSize(10);
                                c.Item().Text("PHÒNG KẾ HOẠCH - TÀI CHÍNH").Bold().FontSize(9);
                                c.Item().Text("-----------------------").FontSize(8);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Bold().FontSize(10);
                                c.Item().AlignCenter().Text("Độc lập - Tự do - Hạnh phúc").Bold().FontSize(9);
                                c.Item().AlignCenter().Text("-----------------------").FontSize(8);
                            });
                        });

                        col.Item().PaddingTop(12).AlignCenter().Text("PHIẾU HỌC PHÍ").Bold().FontSize(16).FontColor("#212521");
                        col.Item().AlignCenter().Text($"Học kỳ: {chiTiet.TenHocKy}").Italic().FontSize(11).FontColor("#6E766E");

                        col.Item().PaddingTop(10).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Mã sinh viên: ").Bold(); t.Span(chiTiet.MaSV); });
                                c.Item().Text(t => { t.Span("Họ và tên: ").Bold(); t.Span(chiTiet.HoTen); });
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Lớp sinh hoạt: ").Bold(); t.Span(chiTiet.LopSinhHoat ?? "--"); });
                                c.Item().Text(t => { t.Span("Ngày in: ").Bold(); t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")); });
                            });
                        });

                        col.Item().PaddingTop(8);
                    });
                });

                page.Content().Element(content =>
                {
                    content.PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);  // STT
                            columns.ConstantColumn(85);  // Mã LHP
                            columns.RelativeColumn(3);   // Tên môn
                            columns.ConstantColumn(40);  // TC LT
                            columns.ConstantColumn(40);  // TC TH
                            columns.ConstantColumn(75);  // Đơn giá LT
                            columns.ConstantColumn(75);  // Đơn giá TH
                            columns.ConstantColumn(85);  // Thành tiền
                        });

                        table.Header(h =>
                        {
                            static IContainer HeaderStyle(IContainer cell) =>
                                cell.Background("#8CB450").Border(0.5f).BorderColor("#6E766E").Padding(4).AlignCenter().AlignMiddle();

                            h.Cell().Element(HeaderStyle).Text("STT").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Mã LHP").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Tên môn học").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("TC LT").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("TC TH").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("ĐG LT").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("ĐG TH").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Thành tiền").Bold().FontColor(Colors.White).FontSize(9);
                        });

                        int stt = 1;
                        foreach (var m in chiTiet.DanhSachMon)
                        {
                            var bg = (stt % 2 == 0) ? "#F7F9F5" : "#FFFFFF";
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignCenter().AlignMiddle().Text(stt.ToString()).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignLeft().AlignMiddle().Text(m.MaLHP).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignLeft().AlignMiddle().Text(m.TenMon).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignCenter().AlignMiddle().Text(m.SoTinChiLT.ToString()).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignCenter().AlignMiddle().Text(m.SoTinChiTH.ToString()).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignRight().AlignMiddle().Text(m.DonGiaLT.ToString("N0")).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignRight().AlignMiddle().Text(m.DonGiaTH.ToString("N0")).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignRight().AlignMiddle().Text(m.ThanhTien.ToString("N0") + " đ").FontSize(9);
                            stt++;
                        }
                    });
                });

                page.Footer().Element(footer =>
                {
                    footer.Column(col =>
                    {
                        col.Item().PaddingTop(8).BorderTop(1).BorderColor("#8CB450").Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Tổng số tín chỉ: ").Bold(); t.Span($"{chiTiet.TongSoTinChi} tín chỉ"); });
                                c.Item().Text(t => { t.Span("Trạng thái: ").Bold(); t.Span(chiTiet.DaDongDu ? "Đã đóng đủ" : "Còn nợ").FontColor(chiTiet.DaDongDu ? "#60A358" : "#E0A83C"); });
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().AlignRight().Text(t => { t.Span("Tổng học phí: ").Bold(); t.Span($"{chiTiet.TongHocPhi:N0} đ").Bold().FontSize(11); });
                                c.Item().AlignRight().Text(t => { t.Span("Đã đóng: ").Bold(); t.Span($"{chiTiet.DaDong:N0} đ"); });
                                c.Item().AlignRight().Text(t => { t.Span("Còn phải nợ: ").Bold(); t.Span($"{chiTiet.ConNo:N0} đ").Bold().FontColor("#D64541"); });
                            });
                        });

                        col.Item().PaddingTop(12).Row(r =>
                        {
                            r.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("Người nộp tiền").Bold().FontSize(9);
                                c.Item().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8);
                            });
                            r.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text($"Ngày {DateTime.Now.Day:D2} tháng {DateTime.Now.Month:D2} năm {DateTime.Now.Year}").Italic().FontSize(9);
                                c.Item().Text("Người lập phiếu").Bold().FontSize(9);
                                c.Item().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8);
                            });
                        });

                        col.Item().PaddingTop(24).AlignCenter().Text(x =>
                        {
                            x.Span("Trang ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                });
            });
        });

        byte[] pdfBytes = document.GeneratePdf();
        return Task.FromResult(pdfBytes);
    }

    public Task<byte[]> XuatDanhSachThiPdfAsync(List<DanhSachThiDto> danhSach, string tenMon, string? maLHP, string tenHocKy)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24, Unit.Point);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(header =>
                {
                    header.Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("TRƯỜNG ĐẠI HỌC CÔNG NGHỆ").Bold().FontSize(10);
                                c.Item().Text("PHÒNG ĐÀO TẠO").Bold().FontSize(9);
                                c.Item().Text("-----------------------").FontSize(8);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Bold().FontSize(10);
                                c.Item().AlignCenter().Text("Độc lập - Tự do - Hạnh phúc").Bold().FontSize(9);
                                c.Item().AlignCenter().Text("-----------------------").FontSize(8);
                            });
                        });

                        col.Item().PaddingTop(12).AlignCenter().Text("DANH SÁCH THI KẾT THÚC HỌC PHẦN").Bold().FontSize(16).FontColor("#212521");
                        col.Item().AlignCenter().Text($"Học kỳ: {tenHocKy}").Italic().FontSize(11).FontColor("#6E766E");

                        col.Item().PaddingTop(10).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Môn học: ").Bold(); t.Span(tenMon); });
                                c.Item().Text(t => { t.Span("Mã LHP: ").Bold(); t.Span(string.IsNullOrWhiteSpace(maLHP) ? "Tất cả các lớp" : maLHP); });
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Tổng số thí sinh: ").Bold(); t.Span($"{danhSach.Count} sinh viên"); });
                                c.Item().Text(t => { t.Span("Ngày in: ").Bold(); t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")); });
                            });
                        });

                        col.Item().PaddingTop(8);
                    });
                });

                page.Content().Element(content =>
                {
                    content.PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);  // STT
                            columns.ConstantColumn(95);  // Mã SV
                            columns.RelativeColumn(3);   // Họ và tên
                            columns.ConstantColumn(100); // Lớp sinh hoạt
                            columns.ConstantColumn(95);  // Mã LHP
                            columns.ConstantColumn(50);  // Điểm số
                            columns.ConstantColumn(80);  // Ký tên
                            columns.ConstantColumn(70);  // Ghi chú
                        });

                        table.Header(h =>
                        {
                            static IContainer HeaderStyle(IContainer cell) =>
                                cell.Background("#8CB450").Border(0.5f).BorderColor("#6E766E").Padding(4).AlignCenter().AlignMiddle();

                            h.Cell().Element(HeaderStyle).Text("STT").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Mã SV").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Họ và tên").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Lớp SH").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Mã LHP").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Điểm").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Chữ ký").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Ghi chú").Bold().FontColor(Colors.White).FontSize(9);
                        });

                        int stt = 1;
                        foreach (var sv in danhSach)
                        {
                            var bg = (stt % 2 == 0) ? "#F7F9F5" : "#FFFFFF";
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(4).AlignCenter().AlignMiddle().Text(stt.ToString()).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(4).AlignCenter().AlignMiddle().Text(sv.MaSV).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(4).AlignLeft().AlignMiddle().Text(sv.HoTen).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(4).AlignLeft().AlignMiddle().Text(sv.LopSinhHoat ?? "--").FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(4).AlignCenter().AlignMiddle().Text(sv.MaLHP).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(4).AlignCenter().AlignMiddle().Text("").FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(4).AlignCenter().AlignMiddle().Text("").FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(4).AlignCenter().AlignMiddle().Text("").FontSize(9);
                            stt++;
                        }
                    });
                });

                page.Footer().Element(footer =>
                {
                    footer.Column(col =>
                    {
                        col.Item().PaddingTop(16).Row(r =>
                        {
                            r.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("Cán bộ coi thi 1").Bold().FontSize(9);
                                c.Item().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8);
                            });
                            r.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("Cán bộ coi thi 2").Bold().FontSize(9);
                                c.Item().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8);
                            });
                            r.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text($"Ngày {DateTime.Now.Day:D2} tháng {DateTime.Now.Month:D2} năm {DateTime.Now.Year}").Italic().FontSize(9);
                                c.Item().Text("Xác nhận Phòng Đào tạo").Bold().FontSize(9);
                                c.Item().Text("(Ký, đóng dấu)").Italic().FontSize(8);
                            });
                        });

                        col.Item().PaddingTop(24).AlignCenter().Text(x =>
                        {
                            x.Span("Trang ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                });
            });
        });

        byte[] pdfBytes = document.GeneratePdf();
        return Task.FromResult(pdfBytes);
    }

    public Task<byte[]> XuatPhieuKetQuaDKHPPdfAsync(PhieuDangKyDto phieu)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24, Unit.Point);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(header =>
                {
                    header.Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("TRƯỜNG ĐẠI HỌC CÔNG NGHỆ").Bold().FontSize(10);
                                c.Item().Text("PHÒNG ĐÀO TẠO").Bold().FontSize(9);
                                c.Item().Text("-----------------------").FontSize(8);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Bold().FontSize(10);
                                c.Item().AlignCenter().Text("Độc lập - Tự do - Hạnh phúc").Bold().FontSize(9);
                                c.Item().AlignCenter().Text("-----------------------").FontSize(8);
                            });
                        });

                        col.Item().PaddingTop(12).AlignCenter().Text("PHIẾU KẾT QUẢ ĐĂNG KÝ HỌC PHẦN").Bold().FontSize(16).FontColor("#212521");
                        col.Item().AlignCenter().Text($"Học kỳ: {phieu.TenHocKy}").Italic().FontSize(11).FontColor("#6E766E");

                        col.Item().PaddingTop(10).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Mã sinh viên: ").Bold(); t.Span(phieu.MaSV); });
                                c.Item().Text(t => { t.Span("Họ và tên: ").Bold(); t.Span(phieu.HoTen); });
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Lớp sinh hoạt: ").Bold(); t.Span(phieu.LopSinhHoat ?? "--"); });
                                c.Item().Text(t => { t.Span("Khóa học: ").Bold(); t.Span(phieu.KhoaHoc ?? "--"); });
                            });
                        });

                        col.Item().PaddingTop(8);
                    });
                });

                page.Content().Element(content =>
                {
                    content.PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);  // STT
                            columns.ConstantColumn(75);  // Mã môn
                            columns.RelativeColumn(3);   // Tên môn học
                            columns.ConstantColumn(40);  // Số TC
                            columns.ConstantColumn(85);  // Mã LHP
                            columns.RelativeColumn(2);   // GV giảng dạy
                            columns.ConstantColumn(50);  // Điểm số
                            columns.ConstantColumn(50);  // Điểm chữ
                        });

                        table.Header(h =>
                        {
                            static IContainer HeaderStyle(IContainer cell) =>
                                cell.Background("#8CB450").Border(0.5f).BorderColor("#6E766E").Padding(4).AlignCenter().AlignMiddle();

                            h.Cell().Element(HeaderStyle).Text("STT").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Mã môn").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Tên môn học").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("TC").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Mã LHP").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Giảng viên").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Điểm số").Bold().FontColor(Colors.White).FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Điểm chữ").Bold().FontColor(Colors.White).FontSize(9);
                        });

                        int stt = 1;
                        foreach (var m in phieu.DanhSachMon)
                        {
                            var bg = (stt % 2 == 0) ? "#F7F9F5" : "#FFFFFF";
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignCenter().AlignMiddle().Text(stt.ToString()).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignCenter().AlignMiddle().Text(m.MaMon).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignLeft().AlignMiddle().Text(m.TenMon).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignCenter().AlignMiddle().Text(m.SoTinChi.ToString()).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignCenter().AlignMiddle().Text(m.MaLHP).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignLeft().AlignMiddle().Text(m.TenGiangVien ?? "--").FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignCenter().AlignMiddle().Text(m.DiemSoFormat).FontSize(9);
                            table.Cell().Background(bg).Border(0.5f).BorderColor("#E0E6DA").Padding(3).AlignCenter().AlignMiddle().Text(m.DiemChuFormat).FontSize(9);
                            stt++;
                        }
                    });
                });

                page.Footer().Element(footer =>
                {
                    footer.Column(col =>
                    {
                        col.Item().PaddingTop(8).BorderTop(1).BorderColor("#8CB450").Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Tổng số môn đăng ký: ").Bold(); t.Span($"{phieu.DanhSachMon.Count} môn"); });
                                c.Item().Text(t => { t.Span("Tổng số tín chỉ: ").Bold(); t.Span($"{phieu.TongSoTinChi} tín chỉ"); });
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().AlignRight().Text(t => { t.Span("Ngày in: ").Bold(); t.Span(phieu.NgayIn.ToString("dd/MM/yyyy HH:mm")); });
                            });
                        });

                        col.Item().PaddingTop(16).Row(r =>
                        {
                            r.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("Sinh viên xác nhận").Bold().FontSize(9);
                                c.Item().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8);
                            });
                            r.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text($"Ngày {DateTime.Now.Day:D2} tháng {DateTime.Now.Month:D2} năm {DateTime.Now.Year}").Italic().FontSize(9);
                                c.Item().Text("Giáo vụ khoa / Phòng Đào tạo").Bold().FontSize(9);
                                c.Item().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8);
                            });
                        });

                        col.Item().PaddingTop(24).AlignCenter().Text(x =>
                        {
                            x.Span("Trang ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                });
            });
        });

        byte[] pdfBytes = document.GeneratePdf();
        return Task.FromResult(pdfBytes);
    }
}

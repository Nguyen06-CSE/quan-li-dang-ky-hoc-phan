using System;
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
}

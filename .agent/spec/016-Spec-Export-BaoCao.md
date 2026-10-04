# 016 — SPEC CHỨC NĂNG EXPORT (EXCEL / PDF)

## Export Excel (`ExcelExportService`, dùng ClosedXML)
Hàm dùng chung, nhận vào tiêu đề cột + dữ liệu (generic), sinh 1 sheet:
```csharp
Task<string> XuatExcelAsync<T>(string tenFile, IEnumerable<T> duLieu, List<(string Tieude, Func<T,object> LayGiaTri)> cotMap);
```
- Header row: nền `#ACD26B` (đổi sang hex Excel tương ứng), chữ trắng đậm, freeze row đầu.
- Auto-fit độ rộng cột.
- Lưu vào thư mục do người dùng chọn qua `SaveFileDialog`, trả về đường dẫn để hiện thông báo "Đã lưu tại: ...".

Áp dụng cho: Xuất DS Sinh viên, DS Môn học, DS SV theo môn, DS thi, Thống kê SV theo môn, DS học phí tổng hợp.

## Export PDF (`PdfExportService`, dùng QuestPDF)
Layout chung mọi phiếu PDF:
```
┌─────────────────────────────────────────┐
│ [Logo nhỏ]   TÊN TRƯỜNG / KHOA            │
│              (địa chỉ - placeholder)      │
├─────────────────────────────────────────┤
│           TIÊU ĐỀ PHIẾU (in hoa, đậm)     │
├─────────────────────────────────────────┤
│  Thông tin liên quan (SV, môn, học kỳ...) │
│                                           │
│  [Bảng dữ liệu chính]                    │
│                                           │
│  Ngày ... tháng ... năm ...               │
│                          [Chỗ ký tên]     │
└─────────────────────────────────────────┘
```
- Font tiếng Việt: dùng font hỗ trợ Unicode đầy đủ (VD nhúng font `Roboto` hoặc `Times New Roman` nếu có sẵn trên máy) — QuestPDF cần set `FontManager` để tránh lỗi hiển thị dấu tiếng Việt.
- Các loại phiếu cần implement: Phiếu kết quả ĐKHP (012.4), Danh sách thi (012.2 — bản PDF), Phiếu học phí (011).
- Màu accent trong PDF (viền tiêu đề, đường kẻ bảng) dùng `#ACD26B` cho đồng bộ thương hiệu với giao diện app.

## Checklist cho agent
- [ ] `ExcelExportService.XuatExcelAsync<T>` generic, dùng chung cho mọi màn hình cần xuất Excel.
- [ ] `PdfExportService` với 3 hàm riêng cho 3 loại phiếu (không cần generic hóa PDF vì layout mỗi loại khác nhau).
- [ ] Cấu hình `QuestPDF.Settings.License = LicenseType.Community` (bắt buộc từ QuestPDF bản mới) trong khởi tạo app.
- [ ] Test xuất thử cả 2 định dạng, mở file kiểm tra hiển thị tiếng Việt có dấu đúng.

# 012 — SPEC MÀN HÌNH BÁO CÁO / THỐNG KÊ

Gồm 4 báo cáo con, truy cập qua menu strip "Báo cáo" hoặc mục Sidebar "Báo cáo" (dạng sub-tab trong 1 View, hoặc 4 View riêng — khuyến nghị dùng `TabControl` trong `BaoCaoView.axaml` cho gọn).

## 12.1 Tab "DS Sinh viên theo môn" (yêu cầu đề bài #5)
- Bộ lọc: ComboBox chọn Học kỳ, ComboBox chọn Môn học (bắt buộc chọn để hiện kết quả).
- Bảng: Mã SV, Họ tên, Lớp sinh hoạt, Mã LHP, GV, Trạng thái đăng ký.
- Nút "Xuất Excel".
- Query: join `DangKyHocPhan` → `LopHocPhan` (where `MaMon` = đã chọn, `MaHocKy` = đã chọn) → `SinhVien`.

## 12.2 Tab "Danh sách thi" (yêu cầu đề bài #6)
- Bộ lọc: Học kỳ, Môn học, (tuỳ chọn) LHP cụ thể.
- Bảng: STT, Mã SV, Họ tên, Lớp sinh hoạt, Mã LHP — chỉ lấy `TrangThai = DangHoc` (SV đã hủy không có trong DS thi).
- Nút "Xuất Excel" và "Xuất PDF" (định dạng phiếu danh sách thi: header trường, tên môn, mã LHP, ngày in, bảng danh sách + cột ký tên trống để giám thị ký).

## 12.3 Tab "Thống kê SV theo môn" (yêu cầu đề bài #9)
- Bộ lọc: Học kỳ.
- Bảng: Mã môn, Tên môn, Số lượng SV đăng ký (COUNT DISTINCT MaSV qua các LHP của môn đó, `TrangThai = DangHoc`), Số LHP đang mở.
- Sort mặc định theo Số lượng giảm dần.
- (Giai đoạn 1 chỉ cần bảng số liệu, chưa cần biểu đồ — có thể để TODO thêm chart ở giai đoạn sau.)

## 12.4 Tab "In phiếu kết quả ĐKHP" (yêu cầu đề bài #8)
- Bộ lọc: chọn 1 SV (autocomplete) + Học kỳ.
- Xem trước nội dung phiếu ngay trong app (dùng `TextBlock`/layout mô phỏng khổ A4) trước khi xuất PDF.
- Nội dung phiếu: Header trường, tiêu đề "PHIẾU KẾT QUẢ ĐĂNG KÝ HỌC PHẦN", thông tin SV (Mã, Họ tên, Lớp, Khóa học), bảng: STT, Mã môn, Tên môn, Số TC, Mã LHP, GV, Điểm số, Điểm chữ (nếu đã có) — chỉ liệt kê `TrangThai = DangHoc`, dòng cuối "Tổng số tín chỉ: X", ngày in, chỗ ký tên SV + Giáo vụ.
- Nút "Xuất PDF".

## Service (`BaoCaoService`)
```csharp
Task<List<SinhVienTheoMonDto>> DsSinhVienTheoMonAsync(string maMon, string maHocKy);
Task<List<DanhSachThiDto>> DsThiTheoMonAsync(string maMon, string maHocKy, string? maLHP);
Task<List<ThongKeMonDto>> ThongKeSoLuongTheoMonAsync(string maHocKy);
Task<PhieuDangKyDto> LayPhieuDangKyAsync(string maSV, string maHocKy);
```

## Checklist cho agent
- [ ] `BaoCaoView.axaml` với `TabControl` 4 tab như trên.
- [ ] `BaoCaoService` đầy đủ 4 hàm.
- [ ] Xuất Excel dùng chung module ở `016-Spec-Export-BaoCao.md`.
- [ ] Xuất PDF: danh sách thi + phiếu ĐKHP, dùng chung module QuestPDF ở `016-Spec-Export-BaoCao.md`.

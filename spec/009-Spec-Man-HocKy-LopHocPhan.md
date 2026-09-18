# 009 — SPEC MÀN HÌNH HỌC KỲ / LỚP HỌC PHẦN (LHP)

## Mục tiêu
Quản lý danh mục Học kỳ và các Lớp học phần (LHP) mở trong từng học kỳ — nền tảng để màn hình Đăng ký học phần (010) hoạt động.

## Giao diện (`HocKyLopHocPhanView.axaml`)
1. Tiêu đề `.h1` "Học kỳ / Lớp học phần".
2. Khối trên: danh sách Học kỳ dạng Tab hoặc ComboBox lớn, có nút "+ Thêm học kỳ" cạnh bên (chỉ Admin/TroLyGiaoVu). Đánh dấu học kỳ nào `DangMo = true` bằng badge màu `BrushSuccess`.
3. Khối dưới: `DataGrid` danh sách LHP thuộc học kỳ đang chọn — cột: Mã LHP, Môn học, GV phụ trách (hiển thị "Chưa phân công" màu `BrushWarning` nếu null), Loại hình ĐT, Sĩ số đăng ký/Sĩ số tối đa (dùng `ProgressBar` nhỏ + text, màu đổi theo % lấp đầy: <80% Primary, 80-100% Warning, =100% Danger/khóa đăng ký mới), Giảng dạy Online (icon check), icon Sửa/Xóa.
4. Thanh công cụ trên bảng LHP: TextBox tìm theo Mã LHP/Tên môn, ComboBox lọc theo Môn học, Button "+ Thêm LHP".

## Dialog Thêm/Sửa Học kỳ
- Field: Mã học kỳ, Tên học kỳ, Ngày bắt đầu, Ngày kết thúc, checkbox "Đặt làm học kỳ hiện hành" (nếu check → set `DangMo=true` cho học kỳ này và `false` cho các học kỳ khác).

## Dialog Thêm/Sửa LHP
- Field: Mã LHP (bắt buộc, unique), ComboBox chọn Môn học (autocomplete theo tên), ComboBox chọn Giảng viên (load từ `NguoiDung` where Role=GiangVien, cho phép để trống = "Chưa phân công"), ComboBox Loại hình ĐT, NumericUpDown Sĩ số tối đa (nullable), checkbox Giảng dạy Online.
- Học kỳ của LHP = học kỳ đang chọn ở khối trên (không cho chọn khác trong dialog, tránh nhầm).

## Xóa
- Chặn xóa LHP nếu đã có `DangKyHocPhan` liên kết.

## Service (`HocKyService`, `LopHocPhanService`)
```csharp
// HocKyService
Task<List<HocKy>> LayTatCaAsync();
Task DatHocKyHienHanhAsync(string maHocKy);

// LopHocPhanService
Task<List<LopHocPhan>> LayTheoHocKyAsync(string maHocKy, string? tuKhoa, string? maMon);
Task<int> DemSiSoDangKyAsync(string maLHP);
Task ThemAsync(LopHocPhan lhp);
Task CapNhatAsync(LopHocPhan lhp);
Task XoaAsync(string maLHP);
```

## Checklist cho agent
- [ ] `HocKyLopHocPhanView.axaml` + ViewModel, 2 dialog tương ứng.
- [ ] `HocKyService`, `LopHocPhanService` như trên.
- [ ] Progress bar sĩ số tính đúng real-time (đếm `DangKyHocPhan` có `TrangThai = DangHoc`).
- [ ] Ràng buộc: chỉ 1 học kỳ có `DangMo = true` tại một thời điểm.

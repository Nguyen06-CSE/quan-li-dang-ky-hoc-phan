# 008 — SPEC MÀN HÌNH MÔN HỌC (CRUD)

## Giao diện (`MonHocView.axaml`)
1. Tiêu đề `.h1` "Môn học".
2. Thanh công cụ: TextBox tìm kiếm theo Mã môn/Tên môn, ComboBox sort ("Tên môn A→Z" mặc định / "Tên môn Z→A" / "Mã môn"), (đẩy phải) Button "Xuất Excel", Button "+ Thêm môn học".
3. `DataGrid` cột: Mã môn, Tên môn, Số TC LT, Số TC TH, Tổng TC (tính = LT+TH), Bậc ĐT, Số LHP đang mở (học kỳ hiện hành), icon Sửa/Xóa.
4. **Mặc định danh sách sort theo Tên môn tăng dần (thứ tự từ điển)** — đúng yêu cầu đề bài #7. Dùng `string.Compare(a, b, StringComparer.Create(new CultureInfo("vi-VN"), false))` để sort đúng theo bảng chữ cái tiếng Việt (không phải theo mã ASCII).

## Dialog Thêm/Sửa (`MonHocEditDialog.axaml`)
- Field: Mã môn (readonly khi Sửa, validate không trùng khi Thêm), Tên môn (bắt buộc), Số TC LT (NumericUpDown, min 0), Số TC TH (NumericUpDown, min 0), Bậc ĐT (ComboBox: DH/CD...).
- Validate: `SoTinChiLT + SoTinChiTH > 0` (không cho lưu môn 0 tín chỉ).

## Xóa
- Chặn xóa nếu môn đã có `LopHocPhan` liên kết → thông báo "Không thể xóa: môn học đã có lớp học phần." Soft delete nếu hợp lệ.

## Service (`MonHocService`)
```csharp
Task<List<MonHoc>> LayDanhSachAsync(string? tuKhoa, string sortBy = "TenMon");
Task ThemAsync(MonHoc mon);
Task CapNhatAsync(MonHoc mon);
Task XoaAsync(string maMon);
```

## Checklist cho agent
- [ ] `MonHocView.axaml`, `MonHocViewModel`, `MonHocEditDialog.axaml`.
- [ ] `MonHocService` đầy đủ, sort theo tiếng Việt đúng chuẩn.
- [ ] Validate ràng buộc tín chỉ > 0 khi thêm/sửa.
- [ ] Kiểm tra ràng buộc khi xóa (đã có LHP hay chưa).

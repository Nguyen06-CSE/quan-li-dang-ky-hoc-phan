# 007 — SPEC MÀN HÌNH SINH VIÊN (CRUD)

## Giao diện (`SinhVienView.axaml`)
1. Tiêu đề `.h1` "Sinh viên" + đếm tổng số bản ghi bên cạnh (`.caption`).
2. Thanh công cụ (hàng ngang, các control cách nhau 8px):
   - TextBox tìm kiếm (placeholder "Tìm theo Mã SV hoặc Họ tên...", debounce 300ms trước khi lọc).
   - ComboBox lọc theo `LopSinhHoat` (load distinct từ DB, có mục "Tất cả").
   - ComboBox lọc theo `KhoaHoc` (tương tự).
   - (đẩy phải) Button `.btn-secondary` "Xuất Excel", Button `.btn-secondary` "Import Excel", Button `.btn-primary` "+ Thêm sinh viên".
3. `DataGrid` cột: Mã SV, Họ tên, Lớp sinh hoạt, Khóa học, Số TC đang đăng ký (học kỳ hiện hành, tính join), 2 icon-button cuối hàng: Sửa / Xóa.
4. Phân trang cuối bảng nếu > 50 dòng (page size 50, có control chuyển trang).

## Dialog Thêm/Sửa (`SinhVienEditDialog.axaml`)
- Field: Mã SV (readonly khi Sửa, bắt buộc khi Thêm, validate không trùng), Họ tên (bắt buộc), Lớp sinh hoạt, Khóa học.
- Nút Lưu: validate → gọi `SinhVienService.ThemAsync`/`CapNhatAsync` → đóng dialog → refresh danh sách + toast thành công.

## Xóa
- Click icon Xóa → dialog xác nhận.
- `SinhVienService.XoaAsync(maSV)`:
  - Nếu SV có ít nhất 1 `DangKyHocPhan` với `TrangThai = DangHoc` → **chặn xóa**, thông báo "Không thể xóa: sinh viên đang có đăng ký học phần." (gợi ý hủy đăng ký trước).
  - Ngược lại → soft delete (`IsDeleted = true`), không xóa cứng.
- Danh sách chính chỉ hiển thị `IsDeleted = false`.

## ViewModel (`SinhVienViewModel`)
- `ObservableCollection<SinhVienDto> DanhSach`, properties filter (`TuKhoa`, `LopFilter`, `KhoaHocFilter`), `Command LoadCommand`, `ThemCommand`, `SuaCommand(SinhVienDto)`, `XoaCommand(SinhVienDto)`, `ExportExcelCommand`, `ImportExcelCommand` (mở dialog chọn file → gọi module Import, xem `015-Spec-Import-Excel.md`).
- Áp dụng quyền: nút Thêm/Sửa/Xóa/Import ẩn nếu role không có quyền (theo `PermissionMatrix`, mục "CRUD Sinh viên").

## Service (`SinhVienService`)
```csharp
Task<PagedResult<SinhVien>> TimKiemAsync(string? tuKhoa, string? lop, string? khoaHoc, int page, int pageSize);
Task ThemAsync(SinhVien sv);
Task CapNhatAsync(SinhVien sv);
Task XoaAsync(string maSV); // ném lỗi nghiệp vụ nếu đang có đăng ký active
Task<bool> TonTaiAsync(string maSV);
```

## Checklist cho agent
- [ ] `SinhVienView.axaml`, `SinhVienViewModel`, `SinhVienEditDialog.axaml` + ViewModel dialog.
- [ ] `SinhVienService` đầy đủ như trên, có kiểm tra ràng buộc nghiệp vụ khi xóa.
- [ ] Tìm kiếm/lọc/phân trang hoạt động đúng, có debounce khi gõ tìm kiếm.
- [ ] Nút Xuất Excel gọi module ở `016-Spec-Export-BaoCao.md`.

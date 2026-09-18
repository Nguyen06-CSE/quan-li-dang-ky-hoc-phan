# 014 — SPEC MÀN HÌNH QUẢN LÝ NGƯỜI DÙNG (chỉ Admin)

## Giao diện (`NguoiDungView.axaml`)
- Bảng: Tên đăng nhập, Họ tên, Role (badge màu khác nhau theo role), Trạng thái (Hoạt động/Đã khóa), icon Sửa / Đổi mật khẩu / Khóa-Mở khóa.
- Button "+ Thêm tài khoản".

## Dialog Thêm/Sửa
- Field: Tên đăng nhập (readonly khi Sửa, unique khi Thêm), Họ tên, ComboBox Role, ô Mã GV (chỉ hiện/enable khi Role = GiangVien, dùng để liên kết với `LopHocPhan.MaGV`).
- Khi Thêm: field Mật khẩu ban đầu (bắt buộc, hash bằng BCrypt trước khi lưu).

## Dialog Đổi mật khẩu
- Field: Mật khẩu mới, Xác nhận mật khẩu — validate khớp nhau.

## Khóa/Mở khóa tài khoản
- Toggle `TrangThai` giữa `HoatDong`/`DaKhoa`. Tài khoản `DaKhoa` không đăng nhập được (kiểm tra ở `AuthService`).

## Service (`NguoiDungService`)
```csharp
Task<List<NguoiDung>> LayTatCaAsync();
Task ThemAsync(NguoiDung nd, string matKhauBanDau);
Task CapNhatAsync(NguoiDung nd);
Task DoiMatKhauAsync(int id, string matKhauMoi);
Task DoiTrangThaiAsync(int id, string trangThaiMoi);
```

## Checklist cho agent
- [ ] `NguoiDungView.axaml` + ViewModel + 2 dialog (Thêm/Sửa, Đổi mật khẩu).
- [ ] `NguoiDungService` đầy đủ, hash mật khẩu bằng BCrypt khi tạo/đổi.
- [ ] Không cho Admin tự khóa chính tài khoản đang đăng nhập (validate ở Service).

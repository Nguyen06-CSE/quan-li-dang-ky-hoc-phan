# 005 — SPEC MÀN HÌNH ĐĂNG NHẬP

## Mục tiêu
Xác thực người dùng bằng Tên đăng nhập + Mật khẩu, xác định `Role`, khởi tạo `ICurrentUserService`, sau đó mở `MainWindow`.

## Giao diện (`LoginView.axaml`, hiển thị trong `Window` riêng trước khi mở `MainWindow`)
- Nền trắng toàn màn hình, card căn giữa (rộng ~380px), bo góc 10px, shadow nhẹ, viền trên card dày 4px màu `BrushPrimary`.
- Trong card, theo thứ tự dọc:
  1. Logo (placeholder nếu chưa có) + text "HỆ THỐNG QUẢN LÝ ĐĂNG KÝ HỌC PHẦN" (`.h1`, căn giữa).
  2. Label "Tên đăng nhập" + TextBox.
  3. Label "Mật khẩu" + TextBox (`PasswordChar='●'`).
  4. Dòng lỗi (TextBlock màu `BrushDanger`, ẩn khi không có lỗi).
  5. Button `.btn-primary` "Đăng nhập" (full width), có `IsDefault=True` để Enter cũng submit.
- Trạng thái loading khi đang xác thực: disable form + hiện `ProgressBar`/spinner nhỏ trong nút.

## ViewModel (`LoginViewModel`)
Properties: `TenDangNhap`, `MatKhau`, `LoiThongBao`, `DangXuLy` (bool).
Command: `DangNhapCommand` (async):
1. Validate 2 field không rỗng.
2. Gọi `AuthService.DangNhapAsync(tenDangNhap, matKhau)`.
3. Nếu thành công → set `ICurrentUserService.CurrentUser`, raise sự kiện/callback để `App` chuyển sang mở `MainWindow` và đóng `LoginWindow`.
4. Nếu thất bại → set `LoiThongBao = "Tên đăng nhập hoặc mật khẩu không đúng."`
5. Nếu `NguoiDung.TrangThai == DaKhoa` → `LoiThongBao = "Tài khoản đã bị khóa, liên hệ Admin."`

## Service (`AuthService` trong `Services`)
```csharp
Task<NguoiDung?> DangNhapAsync(string tenDangNhap, string matKhau);
```
- Lấy `NguoiDung` theo `TenDangNhap` (không phân biệt hoa thường), so khớp `MatKhauHash` bằng `BCrypt.Verify`.
- Trả về `null` nếu không khớp hoặc không tồn tại (không phân biệt rõ lỗi nào trong thông báo UI, để tránh lộ thông tin tài khoản tồn tại hay không).

## Yêu cầu bảo mật tối thiểu
- Mật khẩu không log ra console/file log.
- Giới hạn không bắt buộc ở giai đoạn 1: có thể bỏ qua rate-limit chống brute-force (ghi chú TODO cho giai đoạn sau).

## Checklist cho agent
- [ ] `LoginView.axaml` + `LoginViewModel`.
- [ ] `AuthService.DangNhapAsync`.
- [ ] `ICurrentUserService` lưu user đăng nhập, expose `CurrentUser`, `IsInRole(UserRole)`.
- [ ] Luồng khởi động app: mở `LoginWindow` trước → đăng nhập thành công → mở `MainWindow` → đóng `LoginWindow`.
- [ ] Test thủ công: đăng nhập bằng tài khoản Admin mặc định đã seed ở `002-Spec-CoSoDuLieu.md`.

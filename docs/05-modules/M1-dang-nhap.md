# M1 — Đăng nhập

**Trạng thái: ✅ Hoàn thành**

## Đã làm

- Layout chia 2 nửa (split-screen): panel trái giới thiệu hệ thống, panel phải form đăng nhập.
- Hiện/ẩn mật khẩu (toggle icon trong ô mật khẩu).
- "Ghi nhớ đăng nhập": chỉ nhớ **tên đăng nhập**, không lưu mật khẩu hay token — hệ thống hiện tại chưa có cơ chế
  session-token nên không lưu đăng nhập tự động an toàn được; đây là giới hạn đã báo trước, không phải thiếu sót.
- Hiệu ứng rung (shake) khi đăng nhập sai, viền sáng (focus-glow) khi focus vào ô nhập, label lỗi fade-in, nút Đăng nhập
  có spinner khi đang xử lý.
- Giao diện đã chuyển sang theme "Liquid glass" cùng với toàn bộ các màn hình khác (xem `M2-app-shell.md`).

## File liên quan

- `src/QuanLyDKHP.App/Views/LoginWindow.axaml` (+ `.axaml.cs`)
- `src/QuanLyDKHP.App/ViewModels/LoginViewModel.cs`
- `src/QuanLyDKHP.App/Services/LocalSessionStore.cs`
- `src/QuanLyDKHP.Services/AuthService.cs` (kiểm tra mật khẩu bằng BCrypt, khóa tài khoản `TrangThai = "DaKhoa"`)

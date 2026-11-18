---
id: huong-dan-cai-dat-khoi-chay
title: Hướng Dẫn Cài Đặt & Khởi Chạy Ứng Dụng
sidebar_position: 5
---

# Hướng Dẫn Cài Đặt & Khởi Chạy Ứng Dụng

## 1. Yêu Cầu Môi Trường Cài Đặt

Trước khi tiến hành biên dịch và chạy ứng dụng, môi trường máy tính cần đáp ứng các điều kiện sau:

* **Hệ điều hành:** Hỗ trợ Windows 10/11, macOS (Apple Silicon hoặc Intel), Ubuntu Linux 22.04 LTS trở lên.
* **Bộ phát triển phần mềm:** [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (phiên bản 8.0.100 trở lên).
* **Cơ sở dữ liệu:** Không bắt buộc cài PostgreSQL. Ứng dụng dùng SQLite local làm CSDL chính và chạy hoàn toàn offline.

---

## 2. Cấu Hình Cơ Sở Dữ Liệu & Backup

### 2.1. SQLite Local (mặc định)
Lần chạy đầu tiên tự tạo `quanlydkhp.db` **trống**, chỉ có tài khoản bootstrap `admin`. Không cần migration, không tự seed và không kết nối DB host.

Đường dẫn mặc định:
* **Windows:** `%LocalAppData%\QuanLyDKHP\quanlydkhp.db`
* **macOS:** `~/Library/Application Support/QuanLyDKHP/quanlydkhp.db`
* **Linux:** `~/.local/share/QuanLyDKHP/quanlydkhp.db`

Log khởi động ghi rõ tên file và đường dẫn đầy đủ (`sqlite-startup.log` trong cùng thư mục).

### 2.2. PostgreSQL Backup (tùy chọn)
Không cấu hình trong `appsettings.json`. Sau khi đăng nhập, vào **Cấu hình hệ thống → DB Backup (PostgreSQL)**:
1. Nhập chuỗi kết nối dạng `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require`.
2. Bấm **Kiểm tra kết nối** (không đọc/ghi dữ liệu).
3. Bấm **Lưu cấu hình**. Có thể để trống hoặc bấm **Xóa cấu hình**.

Cấu hình lưu tại `%LocalAppData%/QuanLyDKHP/backup-config.json`:
* **Windows:** secret mã hóa bằng DPAPI (CurrentUser).
* **macOS/Linux:** file quyền `0600`.
* Không ghi connection string ra log/diagnostic.

Các thao tác **Đẩy dữ liệu lên PostgreSQL Backup** và **Khôi phục từ PostgreSQL Backup** chỉ chạy khi người dùng bấm nút. Khôi phục sẽ fetch/validate đủ 8 bảng, tạo Git snapshot `[Pre-Restore]`, rồi mới ghi đè SQLite trong transaction.

---

## 3. Dữ Liệu Khởi Tạo & Tài Khoản Đăng Nhập

Không cần chạy `dotnet ef database update`. Ứng dụng tự tạo schema SQLite khi khởi động. Dữ liệu nghiệp vụ nhập từ Excel hoặc khôi phục từ backup đã có.

### Danh Sách Tài Khoản Đăng Nhập Mặc Định

| Tên Đăng Nhập | Mật Khẩu Khởi Tạo | Vai Trò (Role) | Thẩm Quyền |
|---|---|---|---|
| `admin` | `admin` | `Admin` | Toàn quyền hệ thống, cấu hình tham số, quản lý người dùng |

> Đổi mật khẩu `admin` ngay sau lần đăng nhập đầu tiên. Các tài khoản khác do Admin tạo tại màn hình Quản lý người dùng.

---

## 4. Lệnh Khởi Chạy Ứng Dụng Desktop

Thực hiện lệnh sau từ thư mục gốc để khởi chạy ứng dụng Avalonia UI:

```bash
dotnet run --project src/QuanLyDKHP.App
```

Cửa sổ đăng nhập sẽ hiển thị. Người dùng nhập tài khoản quản trị `admin` / `admin` để bắt đầu trải nghiệm hệ thống.

---

## 5. Lệnh Chạy Kiểm Thử Tự Động (Unit Tests)

Để kiểm tra độ toàn vẹn của mã nguồn và xác nhận toàn bộ unit test đều vượt qua:

```bash
dotnet test tests/QuanLyDKHP.Tests/QuanLyDKHP.Tests.csproj
```

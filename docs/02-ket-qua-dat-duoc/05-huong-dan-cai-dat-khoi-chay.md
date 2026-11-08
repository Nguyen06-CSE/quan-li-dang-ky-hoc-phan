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
* **Cơ sở dữ liệu:** PostgreSQL phiên bản 14 trở lên (Cài đặt cục bộ, chạy qua Docker, hoặc tài khoản Cloud Neon PostgreSQL).

---

## 2. Cấu Hình Cơ Sở Dữ Liệu

Tạo hoặc cập nhật tệp `src/QuanLyDKHP.App/appsettings.Local.json` (hoặc `appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=quanlydkhp;Username=postgres;Password=your_password;SSL Mode=Prefer;"
  }
}
```

:::note[Tùy chọn khởi chạy nhanh bằng Docker]
Nếu chưa có sẵn PostgreSQL trên máy trạm, có thể khởi động nhanh một container PostgreSQL cục bộ bằng lệnh sau:
```bash
docker run --name postgres-quanlydkhp \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=quanlydkhp \
  -p 5432:5432 \
  -d postgres:16-alpine
```
:::

---

## 3. Cập Nhật CSDL & Nạp Dữ Liệu Khởi Tạo (Migrations & Seeding)

Từ thư mục gốc của dự án, mở cửa sổ dòng lệnh (Terminal / PowerShell) và thực hiện lệnh Entity Framework Core:

```bash
dotnet ef database update --project src/QuanLyDKHP.Infrastructure --startup-project src/QuanLyDKHP.App
```

Lệnh này sẽ tự động:
1. Tạo đầy đủ các bảng dữ liệu, chỉ mục (Indexes) và ràng buộc khóa ngoại theo cấu trúc schema mới nhất.
2. Nạp dữ liệu cấu hình hệ thống mặc định (`CauHinhHeThong`).
3. Khởi tạo tài khoản quản trị viên và các tài khoản mẫu cho từng vai trò.

### Danh Sách Tài Khoản Đăng Nhập Mặc Định

| Tên Đăng Nhập | Mật Khẩu Khởi Tạo | Vai Trò (Role) | Thẩm Quyền |
|---|---|---|---|
| `admin` | `admin` | `Admin` | Toàn quyền hệ thống, cấu hình tham số, quản lý người dùng |
| `trolygiaovu` | `123456` | `TroLyGiaoVu` | Quản lý SV, môn học, đăng ký học phần, tính học phí, báo cáo |
| `giaovubomon` | `123456` | `GiaoVuBoMon` | Quản lý kế hoạch bộ môn, xem học phí, lập danh sách thi |
| `giangvien` | `123456` | `GiangVien` | Xem lịch dạy, danh sách sinh viên các lớp mình phụ trách |

---

## 4. Lệnh Khởi Chạy Ứng Dụng Desktop

Thực hiện lệnh sau từ thư mục gốc để khởi chạy ứng dụng Avalonia UI:

```bash
dotnet run --project src/QuanLyDKHP.App
```

Cửa sổ đăng nhập sẽ hiển thị. Người dùng nhập tài khoản quản trị `admin` / `admin` để bắt đầu trải nghiệm hệ thống.

---

## 5. Lệnh Chạy Kiểm Thử Tự Động (Unit Tests)

Để kiểm tra độ toàn vẹn của mã nguồn và xác nhận tất cả 42 unit test đều vượt qua:

```bash
dotnet test tests/QuanLyDKHP.Tests/QuanLyDKHP.Tests.csproj
```

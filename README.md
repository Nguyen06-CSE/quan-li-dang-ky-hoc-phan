# HỆ THỐNG QUẢN LÝ ĐĂNG KÝ HỌC PHẦN (QUANLYDKHP)

Ứng dụng Desktop chạy độc lập trên nền tảng **.NET 8** & **Avalonia UI**, phục vụ công tác quản lý đào tạo, đăng ký học phần, tính học phí và thống kê báo cáo cho khoa/trường đại học.

---

## 1. Công nghệ sử dụng
- **Ngôn ngữ & Nền tảng**: C# 12, .NET 8.0
- **Giao diện người dùng (UI)**: Avalonia UI 11.x, Kiến trúc MVVM (`CommunityToolkit.Mvvm`)
- **Cơ sở dữ liệu**: PostgreSQL (hỗ trợ Neon Cloud Serverless / Local PostgreSQL)
- **ORM**: Entity Framework Core 8.0, Npgsql
- **Báo cáo & Export/Import**:
  - **ClosedXML**: Xử lý đọc/ghi và Import file Excel dữ liệu lớn.
  - **QuestPDF**: Thiết kế và xuất phiếu PDF chuẩn khổ A4 (Phiếu học phí, Danh sách thi, Phiếu kết quả ĐKHP).
- **Bảo mật**: BCrypt.Net-Next (mã hóa mật khẩu an toàn).

---

## 2. Các chức năng chính (9 yêu cầu cốt lõi + tính năng mở rộng)
1. **Quản lý Sinh viên**: Thêm, sửa, xóa (soft delete), tìm kiếm đa tiêu chí, phân trang và xuất Excel.
2. **Quản lý Môn học**: Thêm, sửa, xóa môn học, sắp xếp theo thứ tự từ điển tiếng Việt A-Z / Z-A / Mã môn, quản lý số tín chỉ lý thuyết/thực hành, xuất Excel.
3. **Quản lý Học kỳ & Lớp học phần**: Quản lý học kỳ đang mở, danh sách lớp học phần kèm theo dõi sĩ số real-time.
4. **Đăng ký & Điều chỉnh học phần**: 
   - Đăng ký môn học theo luồng 5 bước kiểm tra tự động (Kiểm tra học kỳ mở, kiểm tra trùng LHP, kiểm tra sĩ số tối đa, kiểm tra giới hạn tín chỉ tối thiểu/tối đa).
   - Hủy đăng ký (soft cancel) và tự động tính toán lại học phí tức thì.
5. **Quản lý Học phí**: 
   - Tính toán học phí theo đơn giá LT và TH (`Học phí = (TC LT * ĐG LT + TC TH * ĐG TH)`).
   - Tra cứu học phí theo Sinh viên / Lớp sinh hoạt / Khóa học.
   - Xuất Phiếu học phí PDF chuẩn A4.
6. **Báo cáo & Thống kê**:
   - Tab 1: Danh sách sinh viên đăng ký theo từng môn học (Xuất Excel).
   - Tab 2: Lập danh sách thi kết thúc học phần theo môn/lớp học phần (Xuất Excel & Xuất PDF phiếu thi).
   - Tab 3: Thống kê tổng hợp số lượng sinh viên theo từng môn học trong kỳ (Xuất Excel).
   - Tab 4: In phiếu kết quả đăng ký học phần của từng sinh viên (Xuất PDF).
7. **Import Excel thông minh**:
   - Hỗ trợ import hàng nghìn dòng dữ liệu từ file Excel chuẩn đào tạo.
   - Tự động nhận diện / upsert Sinh viên, Môn học, Học kỳ, Lớp học phần, Phiếu đăng ký và tự động tính học phí.
8. **Phân quyền 4 vai trò (Roles)**:
   - **Admin**: Toàn quyền hệ thống, cấu hình tham số, quản trị tài khoản người dùng.
   - **Trợ lý giáo vụ**: Quản lý sinh viên, môn học, lớp học phần, đăng ký, tính học phí, báo cáo, import/export.
   - **Giáo vụ bộ môn**: Quản lý đào tạo cấp bộ môn, theo dõi sĩ số, lập danh sách thi, xem học phí.
   - **Giảng viên**: Tra cứu dashboard, xem môn học, xem danh sách sinh viên đăng ký, xem danh sách thi.
9. **Cấu hình hệ thống & Quản lý người dùng**:
   - Cấu hình số tín chỉ tối thiểu/tối đa, đơn giá tín chỉ LT/TH, cờ cho phép hiệu chỉnh khi ngoài hạn.
   - Thêm/sửa/khóa tài khoản người dùng, đổi mật khẩu (có xác thực mật khẩu cũ).

---

## 3. Cấu hình & Khởi chạy ứng dụng

### 3.1. Yêu cầu môi trường
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL Database (hoặc tài khoản Neon PostgreSQL)

### 3.2. Cấu hình chuỗi kết nối Database
---
# LƯU Í HIỆN TẠI CHƯƠNG TRÌNH MẶC ĐỊNH DÙNG DB HOST TRÊN NEON NÊN TỐC ĐỘ RẤT THẤP BẠN HÃY TỰ TẠO DB THEO HƯỚNG DẪN TẠI ĐƯỜNG LINK DƯỚI ĐÂY
[hướng dẫn cài đặt DB](/database-setup.md)
Tạo hoặc cập nhật file `src/QuanLyDKHP.App/appsettings.Local.json` (hoặc `appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-postgres-host;Port=5432;Database=quanlydkhp;Username=your-username;Password=your-password;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

### 3.3. Cập nhật Cơ sở dữ liệu (Migration & Seeding)
Chạy lệnh sau từ thư mục gốc của dự án:
```powershell
dotnet ef database update --project src/QuanLyDKHP.Infrastructure --startup-project src/QuanLyDKHP.App
```

### 3.4. Tài khoản đăng nhập mặc định
Sau khi migration và seed hoàn tất, hệ thống có sẵn tài khoản quản trị:
* **Tài khoản**: `admin`
* **Mật khẩu**: `admin`
* **Vai trò**: `Admin`

---

## 4. Hướng dẫn chạy và kiểm thử

### 4.1. Chạy ứng dụng Desktop
```powershell
cd src/QuanLyDKHP.App
dotnet run
```

### 4.2. Chạy toàn bộ Unit Tests
```powershell
dotnet test tests/QuanLyDKHP.Tests/QuanLyDKHP.Tests.csproj
```
*(Hiện tại toàn bộ 42/42 unit tests kiểm tra dịch vụ nghiệp vụ, tính toán học phí, xác thực đăng ký học phần, phân quyền và export Excel/PDF đều đạt 100% Passed).*

---

## 5. Cấu trúc Solution (Clean Architecture)
```
quan-li-dang-ky-hoc-phan/
├── src/
│   ├── QuanLyDKHP.Core/            # Entities, DTOs, Enums, Interfaces, Authorization Rules
│   ├── QuanLyDKHP.Infrastructure/  # AppDbContext, Repositories, Migrations, Export ClosedXML & QuestPDF
│   ├── QuanLyDKHP.Services/        # Business Logic Services (SinhVien, MonHoc, DangKy, HocPhi...)
│   └── QuanLyDKHP.App/             # Avalonia UI, Views, ViewModels, Styles, Dialogs
├── tests/
│   └── QuanLyDKHP.Tests/           # Unit Tests (xUnit, Moq)
└── spec/                           # Tài liệu đặc tả kỹ thuật chi tiết
```

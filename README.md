# HỆ THỐNG QUẢN LÝ ĐĂNG KÝ HỌC PHẦN (QUANLYDKHP)

Ứng dụng Desktop chạy độc lập trên nền tảng **.NET 8** & **Avalonia UI**, phục vụ công tác quản lý đào tạo, đăng ký học phần, tính học phí và thống kê báo cáo cho khoa/trường đại học.

---

## 1. Công nghệ sử dụng
- **Ngôn ngữ & Nền tảng**: C# 12, .NET 8.0
- **Giao diện người dùng (UI)**: Avalonia UI 11.x, Kiến trúc MVVM (`CommunityToolkit.Mvvm`)
- **Cơ sở dữ liệu**: SQLite Local (nguồn dữ liệu nghiệp vụ duy nhất). PostgreSQL chỉ là backup tầng 2 tùy chọn.
- **ORM**: Entity Framework Core 8.0, Npgsql (chỉ cho backup PostgreSQL tùy chọn)
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
- Không bắt buộc cài PostgreSQL. Ứng dụng chạy hoàn toàn offline với SQLite local.

### 3.2. Cơ sở dữ liệu & Cấu hình backup
**SQLite Local là CSDL làm việc duy nhất.** Lần chạy đầu tiên trên máy mới:
- Tự tạo file `quanlydkhp.db` **trống**, chỉ có tài khoản bootstrap `admin` để đăng nhập.
- Không tự seed, không pull delta, không full refresh và không đọc dữ liệu từ bất kỳ DB host nào.
- Đường dẫn mặc định: `%LocalAppData%/QuanLyDKHP/quanlydkhp.db` (Windows) hoặc `~/Library/Application Support/QuanLyDKHP/quanlydkhp.db` (macOS). Log khởi động ghi rõ tên file và đường dẫn đầy đủ.

Sau khi đăng nhập, nhập dữ liệu từ Excel hoặc vào **Cấu hình hệ thống → DB Backup (PostgreSQL)** nếu muốn dùng backup PostgreSQL:
- Nhập chuỗi kết nối dạng `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require`.
- Bấm **Kiểm tra kết nối**, sau đó **Lưu cấu hình**. Có thể để trống.
- Chuỗi kết nối **không** nằm trong `appsettings.json` và **không** được ghi log.
- Trên Windows, secret được mã hóa bằng **DPAPI (CurrentUser)**; trên macOS/Linux file cấu hình có quyền `0600`.
- **Đẩy dữ liệu lên PostgreSQL Backup** và **Khôi phục từ PostgreSQL Backup** chỉ chạy khi người dùng bấm nút. Khôi phục sẽ tạo Git snapshot `[Pre-Restore]` trước khi ghi đè local.

### 3.3. Khởi tạo dữ liệu
Không cần chạy migration/seeding thủ công. Ứng dụng tự tạo schema SQLite khi khởi động lần đầu. Dữ liệu nghiệp vụ được nhập từ Excel hoặc khôi phục từ bản backup đã có.

### 3.4. Tài khoản đăng nhập mặc định
* **Tài khoản**: `admin`
* **Mật khẩu**: `admin`
* **Vai trò**: `Admin`

> Đổi mật khẩu ngay sau lần đăng nhập đầu tiên tại màn hình Quản lý người dùng.

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
*(Toàn bộ unit tests kiểm tra dịch vụ nghiệp vụ, tính toán học phí, xác thực đăng ký học phần, phân quyền, export Excel/PDF, first-run SQLite rỗng, cấu hình backup và an toàn restore đều đạt 100% Passed).*

---

## 5. Cấu trúc Solution (Clean Architecture)
```
quan-li-dang-ky-hoc-phan/
├── src/
│   ├── QuanLyDKHP.Core/            # Entities, DTOs, Enums, Interfaces, Authorization Rules
│   ├── QuanLyDKHP.Infrastructure/  # AppDbContext, Repositories, Backup (Git/PostgreSQL), Export ClosedXML & QuestPDF
│   ├── QuanLyDKHP.Services/        # Business Logic Services (SinhVien, MonHoc, DangKy, HocPhi...)
│   └── QuanLyDKHP.App/             # Avalonia UI, Views, ViewModels, Styles, Dialogs
├── tests/
│   └── QuanLyDKHP.Tests/           # Unit Tests (xUnit, Moq)
├── scripts/                        # Script tiện ích (không auto-seed khi chạy app)
└── docs/                           # Tài liệu kiến trúc, đặc tả chức năng & nhật ký lỗi
```

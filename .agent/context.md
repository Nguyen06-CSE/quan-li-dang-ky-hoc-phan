# Cấu trúc và Kiến trúc Dự án (Context)

## Tech Stack
- **UI Framework:** Avalonia UI (.NET 8), MVVM (CommunityToolkit.Mvvm).
- **Database:** PostgreSQL (Neon / Local) via Entity Framework Core (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- **Thư viện phụ trợ:** ClosedXML (Excel), QuestPDF (PDF), BCrypt.Net-Next (Mật khẩu).

## Cấu trúc Project (Clean Architecture / N-Tier)
1. **QuanLyDKHP.Core:** Các Entity, Enum, Interface (IRepository, IServices). *Không phụ thuộc bất kì framework ngoài (EF, Avalonia).*
2. **QuanLyDKHP.Infrastructure:** Chứa `AppDbContext`, Repositories triển khai giao tiếp CSDL, Import/Export logic.
3. **QuanLyDKHP.Services:** Chứa logic nghiệp vụ. Chỉ gọi tới Interfaces của `Core`.
4. **QuanLyDKHP.App:** Project UI Avalonia (Executable). Là nơi nối dependency (DI) và điều hướng (ViewModel-first navigation).
5. **QuanLyDKHP.Tests:** Unit test cho các Services quan trọng.

*Dependency Rule:* App → Services → Core; App → Infrastructure → Core. 

## Nguyên tắc Code Bắt buộc
- **KHÔNG code nghiệp vụ trong code-behind của UI.** Toàn bộ logic (tính học phí, check trùng/điều kiện đăng ký) phải nằm ở project `Services`.
- **Luôn truy vấn qua Repository.** ViewModels gọi Services, Services gọi Repositories. Không dùng trực tiếp DbContext.
- **Soft Delete (Xóa mềm):** Dùng cờ `IsDeleted = true` (không xóa cứng) cho các bảng có lịch sử liên kết (SinhVien, MonHoc, LopHocPhan, NguoiDung...).
- **Đơn vị tiền tệ:** VNĐ, lưu DB kiểu `numeric(12,2)`, hiển thị có phân cách nghìn.
- **Ngôn ngữ UI & Database Entities:** Tiếng Việt (Entities/Properties dùng tiếng Việt không dấu, PascalCase).
- **Navigation:** Dùng `ViewModel-first navigation` thông qua `CurrentViewModel` ở `MainWindowViewModel`.
- **Biên dịch:** Đảm bảo build thành công và fix hết lỗi/cảnh báo trước khi qua bước tiếp theo.

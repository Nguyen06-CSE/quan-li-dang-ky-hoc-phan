# 001 — SPEC KIẾN TRÚC & CẤU TRÚC PROJECT

## Giải pháp (Solution) .NET

Tạo solution tên `QuanLyDangKyHocPhan.sln` gồm các project sau:

```
QuanLyDangKyHocPhan.sln
├── src/
│   ├── QuanLyDKHP.App/              (Avalonia UI, project khởi chạy - executable)
│   │   ├── Views/                   (file .axaml — 1 file/màn hình)
│   │   ├── ViewModels/              (1 ViewModel / màn hình, kế thừa ViewModelBase)
│   │   ├── Controls/                (UserControl dùng chung: DataGrid tùy biến, Dialog...)
│   │   ├── Converters/               (IValueConverter dùng trong binding)
│   │   ├── Styles/                  (file .axaml chứa style, theo 004-Spec-DesignSystem)
│   │   ├── Assets/                  (icon, logo, font)
│   │   └── App.axaml / Program.cs
│   │
│   ├── QuanLyDKHP.Core/              (class library — không phụ thuộc Avalonia/EF)
│   │   ├── Entities/                 (các entity: SinhVien, MonHoc, LopHocPhan...)
│   │   ├── Enums/                    (UserRole, TrangThaiDangKy...)
│   │   ├── Interfaces/               (IRepository<T>, ISinhVienRepository, IHocPhiService...)
│   │   └── DTOs/                     (đối tượng truyền dữ liệu giữa các tầng, nếu cần)
│   │
│   ├── QuanLyDKHP.Infrastructure/     (class library — EF Core, import/export, DB)
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/
│   │   ├── Repositories/              (triển khai các interface ở Core)
│   │   ├── Import/                    (đọc file Excel bằng ClosedXML)
│   │   └── Export/                    (xuất Excel bằng ClosedXML, PDF bằng QuestPDF)
│   │
│   └── QuanLyDKHP.Services/            (class library — logic nghiệp vụ)
│       ├── AuthService.cs
│       ├── SinhVienService.cs
│       ├── MonHocService.cs
│       ├── DangKyHocPhanService.cs
│       ├── HocPhiService.cs
│       ├── BaoCaoService.cs
│       └── CauHinhService.cs
│
└── tests/
    └── QuanLyDKHP.Tests/               (unit test cho tầng Services — bắt buộc test HocPhiService và validate số TC)
```

## Nguyên tắc phụ thuộc (Dependency Rule)

```
App  →  Services  →  Core
App  →  Infrastructure  →  Core
Services → Core (chỉ qua Interfaces, KHÔNG phụ thuộc trực tiếp Infrastructure)
```

- `Core` không được reference bất kỳ project nào khác (không phụ thuộc EF, không phụ thuộc Avalonia).
- `Services` chỉ biết `Core` (dùng interface `IXxxRepository` được định nghĩa trong `Core.Interfaces`), không biết `Infrastructure` triển khai cụ thể ra sao.
- `App` là nơi duy nhất "nối dây" (Dependency Injection) — đăng ký `Infrastructure` triển khai cụ thể cho các interface của `Core`.

## Dependency Injection

Dùng `Microsoft.Extensions.DependencyInjection`. Khởi tạo trong `Program.cs`/`App.axaml.cs`:
- Đăng ký `AppDbContext` (scoped hoặc theo pattern factory vì app desktop chạy 1 tiến trình dài).
- Đăng ký toàn bộ Repository, Service theo interface tương ứng.
- Đăng ký ViewModel (transient) để `App` resolve khi mở màn hình mới.

## Điều hướng màn hình (Navigation)

Dùng mô hình **ViewModel-first navigation** đơn giản:
- 1 `MainWindowViewModel` giữ property `CurrentViewModel` (kiểu `ViewModelBase`).
- Sidebar/menu strip khi click → gọi lệnh đổi `CurrentViewModel` sang ViewModel tương ứng.
- `MainWindow.axaml` dùng `ContentControl` + `DataTemplate` để map ViewModel → View tương ứng (theo type).
- Các dialog (form thêm/sửa, xác nhận xóa) dùng cửa sổ con (`Window`) mở qua service `IDialogService` (để ViewModel không phụ thuộc trực tiếp Avalonia Window — dễ test).

## Quy ước đặt tên

- Entity, property trong Core: đặt tên **tiếng Việt không dấu, PascalCase** (VD `SinhVien`, `MaSV`, `HoTen`) — khớp với ngôn ngữ nghiệp vụ đã dùng trong toàn bộ tài liệu spec, tránh nhầm lẫn khi đối chiếu.
- File View: `<TenManHinh>View.axaml` (VD `SinhVienView.axaml`).
- File ViewModel: `<TenManHinh>ViewModel.axaml` (VD `SinhVienViewModel.cs`).
- Namespace theo đúng cấu trúc thư mục.

## Cấu hình kết nối CSDL

- Chuỗi kết nối Neon PostgreSQL lưu trong `appsettings.json` (KHÔNG hard-code trong code), có file `appsettings.Development.json` mẫu (không commit connection string thật lên git — dùng `appsettings.Local.json` bị gitignore cho máy dev).
- Đọc cấu hình bằng `Microsoft.Extensions.Configuration`.

## Việc cần làm trong bước này (checklist cho agent)
- [ ] Tạo solution + toàn bộ project theo cấu trúc trên.
- [ ] Thiết lập tham chiếu project đúng theo Dependency Rule.
- [ ] Cài đặt các package NuGet: `Avalonia`, `Avalonia.Desktop`, `CommunityToolkit.Mvvm`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `ClosedXML`, `QuestPDF`, `BCrypt.Net-Next`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Configuration.Json`.
- [ ] Khởi tạo `Program.cs` chạy được app rỗng (cửa sổ trống) để xác nhận project build & chạy thành công trước khi sang bước 002.

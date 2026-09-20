# Nhật ký Quyết định (Decisions Log)

File này sẽ ghi lại các quyết định kỹ thuật, kiến trúc hoặc nghiệp vụ được đưa ra trong quá trình phát triển dự án.

## 2026-09-18 - 001-Spec-KienTrucDuAn.md
**Vấn đề:** Khi cài đặt các package NuGet như `Npgsql.EntityFrameworkCore.PostgreSQL` và `Microsoft.Extensions.*` mà không chỉ định version, hệ thống tự động tải bản mới nhất (v10.x) dẫn tới lỗi không tương thích (NU1202) do framework của dự án được chốt là .NET 8.
**Quyết định:** Chỉ định cứng version `8.x` cho các thư viện phụ thuộc .NET (như `Microsoft.Extensions.DependencyInjection`, `Npgsql.EntityFrameworkCore.PostgreSQL`...) khi thêm vào các project.
**Lý do:** Đảm bảo tính tương thích và build thành công với .NET 8 theo đúng yêu cầu công nghệ (Avalonia UI .NET 8).

## 2026-09-18 - 002-Spec-CoSoDuLieu.md
**Vấn đề 1:** Việc cấu hình các field Audit (`NgayTao`, `NgayCapNhat`, `IsDeleted`) cần phải nhất quán giữa tất cả các entity.
**Quyết định 1:** Tạo các interface `IAuditableEntity` và `ISoftDeleteEntity`. Dùng Fluent API để config kiểu `timestamptz` và giá trị mặc định (`now()`) ở hàm `OnModelCreating`, đồng thời override `SaveChanges/SaveChangesAsync` để tự động cập nhật `NgayCapNhat` thành `DateTime.UtcNow`.
**Lý do 1:** Giúp code trong Service ngắn gọn, tránh lặp lại việc gán ngày tháng thủ công mỗi khi thao tác DB.

**Vấn đề 2:** Yêu cầu kết nối và cập nhật cơ sở dữ liệu trên Neon PostgreSQL.
**Quyết định 2:** Đã cấu hình chuỗi kết nối Neon PostgreSQL trong `appsettings.json`, `appsettings.Local.json` và `AppDbContextFactory.cs` (chuyển sang dạng `Host=...;Database=...;Username=...;Password=...;SslMode=Require` theo định dạng của Npgsql). Đã chạy lệnh `dotnet ef database update` thành công tới Neon PostgreSQL cloud database.
**Lý do 2:** Npgsql Provider yêu cầu định dạng Key-Value hoặc chuỗi kết nối PostgreSQL chuẩn Npgsql để thực thi migration và truy vấn CSDL.

## 2026-09-18 - 003-Spec-PhanQuyen.md

**Vấn đề 1:** Spec có chức năng "Tính học phí" với GiảngViên ghi "(chỉ xem)" — cần phân biệt giữa "tính" (ghi/thực thi) và "xem" (đọc).
**Quyết định 1:** Tách thành 2 chức năng riêng trong PermissionMatrix: `TinhHocPhi` (Admin, TroLyGiaoVu, GiaoVuBoMon) và `XemHocPhi` (cả 4 role). Menu sidebar sẽ dùng `XemHocPhi` để hiển thị mục "Học phí"; các nút hành động tính/thay đổi sẽ check `TinhHocPhi`.
**Lý do 1:** Đảm bảo GiảngViên có thể xem thông tin học phí nhưng không thể thực hiện tính toán/cập nhật, đúng ý spec.

**Vấn đề 2:** Spec yêu cầu ICurrentUserService đặt ở "Infrastructure/App" — không rõ đặt ở project nào.
**Quyết định 2:** Đặt interface `ICurrentUserService` ở `Core/Interfaces` (tuân thủ Dependency Rule — Core không phụ thuộc gì), triển khai `CurrentUserService` ở `Services` (vì nó chứa logic nghiệp vụ kiểm tra quyền). DI đăng ký Singleton ở `App`.
**Lý do 2:** Clean Architecture: interface ở Core, implementation ở Services — App chỉ làm nhiệm vụ nối DI. Đặt ở Infrastructure chỉ nên chứa DB/IO.

## 2026-09-18 - 006-Spec-Man-TrangChu.md
**Vấn đề 1:** Spec yêu cầu query số liệu thẻ và danh sách lớp giảng dạy thông qua `BaoCaoService`, nhưng quy định dự án không cho phép Service gọi trực tiếp `DbContext`.
**Quyết định 1:** Tạo `IDashboardRepository` trong `QuanLyDKHP.Core/Interfaces` và triển khai `DashboardRepository` trong `QuanLyDKHP.Infrastructure/Repositories` để thực hiện các câu lệnh LINQ `CountAsync` và `Select`. `BaoCaoService` sẽ nhận `IDashboardRepository` qua Dependency Injection.
**Lý do 1:** Tuân thủ triệt để nguyên tắc truy vấn dữ liệu qua Repository & Clean Architecture (App -> Services -> Core <- Infrastructure).

**Vấn đề 2:** Đăng nhập báo lỗi `Cannot access a disposed context instance. Object name: 'AppDbContext'`.
**Quyết định 2:** Bỏ `using var scope = ServiceProvider.CreateScope();` trong hàm `ShowLoginWindow` của `App.axaml.cs`. Sử dụng `ServiceProvider` gốc trực tiếp để resolve `LoginViewModel` và `MainWindowViewModel`.
**Lý do 2:** Khối `using var scope` tự động dispose scope ngay khi hàm `ShowLoginWindow` kết thúc. Khi user bấm nút "Đăng nhập", event handler callback được gọi nhưng scope và DbContext đã bị dispose từ trước.

## 2026-09-18 - 007-Spec-Man-SinhVien.md & 008-Spec-Man-MonHoc.md
**Vấn đề 1:** Spec 007 yêu cầu tính "Số TC đang đăng ký (học kỳ hiện hành, tính join)" của từng Sinh viên trên DataGrid.
**Quyết định 1:** Tạo `SinhVienDto` và `MonHocDto` thuộc project `QuanLyDKHP.Core.Dtos` để chứa các trường tính toán (`SoTinChiDangKy`, `TongTinChi`, `SoLhpDangMo`). Query join và đếm ở `SinhVienRepository` và `MonHocRepository`.
**Lý do 1:** Giúp ViewModel nhận dữ liệu sạch, không mang entity DB lên UI và tuân thủ Clean Architecture.

**Vấn đề 2:** Spec 008 yêu cầu sắp xếp danh sách môn học theo thứ tự từ điển tiếng Việt (`CultureInfo("vi-VN")`).
**Quyết định 2:** Sử dụng `StringComparer.Create(new CultureInfo("vi-VN"), false)` trong LINQ LINQ `OrderBy` / `OrderByDescending` của `MonHocService`.
**Lý do 2:** Đảm bảo chữ cái có dấu tiếng Việt (đ, ê, ô, ă, â...) được sắp xếp chính xác theo bảng chữ cái tiếng Việt như spec quy định.

## 2026-09-18 - 009-Spec-Man-HocKy-LopHocPhan.md
**Vấn đề 1:** Bảng `NguoiDung` lưu thuộc tính `Role` và `TrangThai` dạng `string` ("GiangVien", "HoatDong"), trong khi trong code có enum `UserRole`. Ngoài ra, `DangKyHocPhan` lưu `TrangThai` dạng `string` ("DangHoc").
**Quyết định 1:** Cấu hình query LINQ trong `NguoiDungRepository.LayGiangVienAsync` so sánh chuỗi `u.Role == UserRole.GiangVien.ToString() && u.TrangThai == "HoatDong"`, và `LopHocPhanRepository.DemSiSoDangKyAsync` so sánh `dk.TrangThai == "DangHoc"`.
**Lý do 1:** Đảm bảo khớp chính exact data type của Entity DB (`string`), tránh lỗi biên dịch CS0019/CS0103.

**Vấn đề 2:** Ma trận phân quyền `PermissionMatrix` chưa khai báo sẵn hằng số chức năng cho màn hình "Học kỳ / Lớp học phần".
**Quyết định 2:** Bổ sung hằng `ChucNang.CrudHocKyLopHocPhan = "CrudHocKyLopHocPhan"` vào `ChucNang.cs` và cấu hình phân quyền cho Admin, TroLyGiaoVu, GiaoVuBoMon trong `PermissionMatrix.cs`.
**Lý do 2:** Tuân thủ cơ chế bảo mật phân quyền chung của toàn dự án (UI + Service layer).

## 2026-09-18 - 010-Spec-Man-DangKyHocPhan.md
**Vấn đề 1:** Cơ sở dữ liệu cấu hình Unique Index trên cặp `(MaSV, MaLHP)`. Khi sinh viên hủy đăng ký (chuyển trạng thái `DaHuy`), nếu sau đó đăng ký lại chính LHP đó, thao tác `Add` bản ghi mới sẽ gặp lỗi vi phạm Unique Constraint của PostgreSQL.
**Quyết định 1:** Trong `DangKyHocPhanService.DangKyAsync`, nếu tìm thấy bản ghi cũ của `(MaSV, MaLHP)` đang ở trạng thái `DaHuy`, ta thực hiện cập nhật (tái kích hoạt) bản ghi này sang `DangHoc`, cập nhật lại `NgayDK` và `NguoiDK` thay vì tạo Entity mới.
**Lý do 1:** Đảm bảo toàn vẹn dữ liệu, tránh crash ứng dụng khi đăng ký lại lớp học phần đã từng hủy.

**Vấn đề 2:** Kiểm tra trùng môn (cảnh báo mềm cho trường hợp học lại/cải thiện) cần giao tiếp linh hoạt giữa Service và ViewModel/View mà không vi phạm nguyên tắc Clean Architecture.
**Quyết định 2:** Định nghĩa DTO `DangKyResult` chứa `Success`, `Message`, `IsCanhBaoTrungMon`, `MaLHPCu`, `TenMon`. Service trả về kết quả cảnh báo mềm nếu `boQuaCanhBaoTrungMon == false`. ViewModel nhận kết quả và kích hoạt callback dialog xác nhận từ UI, nếu người dùng xác nhận "Vẫn đăng ký", ViewModel sẽ gọi lại `DangKyAsync` với `boQuaCanhBaoTrungMon: true`.
**Lý do 2:** Tách biệt hoàn toàn tầng nghiệp vụ (Service) khỏi UI, hỗ trợ kiểm thử Unit Test dễ dàng mà vẫn mang lại trải nghiệm tương tác mượt mà cho người dùng.

**Vấn đề 3:** Tự động tính toán lại học phí khi đăng ký/hủy đăng ký học phần (bước 6 của quy trình).
**Quyết định 3:** Tạo sẵn `IHocPhiService` & `HocPhiService` với method `TinhLaiHocPhiAsync(maSV, maHocKy)` tính toán số tiền phải đóng của từng LHP dựa trên cấu hình đơn giá LT/TH từ `CauHinhHeThong` và cập nhật vào trường `DangKyHocPhan.SoTienPhaiDong`.
**Lý do 3:** Đảm bảo đúng yêu cầu trong Spec 010 và chuẩn bị sẵn nền tảng cho Spec 011.

## 2026-09-19 - 011-Spec-Man-HocPhi.md
**Vấn đề 1:** Giao diện chi tiết học phí của sinh viên cần thể hiện bảng chi tiết từng môn kèm đơn giá LT/TH, thành tiền và có nút xuất PDF phiếu học phí trực tiếp.
**Quyết định 1:** Thiết kế `HocPhiView.axaml` với bảng tổng hợp danh sách SV và modal/panel overlay chi tiết trực tiếp trong View (thay vì popup Window con), liên kết với `XuatPhieuHocPhiPdfAsync` thông qua Avalonia `StorageProvider` (lưu file độc lập không phụ thuộc cứng đường dẫn).
**Lý do 1:** Trải nghiệm người dùng đồng nhất, mượt mà trên Avalonia MVVM và tuân thủ chặt chẽ mẫu hiển thị phiếu học phí trong spec.


---
id: nhat-ky-quyet-dinh-ky-thuat
title: Nhật Ký Quyết Định Kỹ Thuật (ADR)
sidebar_position: 3
---

# Nhật Ký Quyết Định Kỹ Thuật (Architectural Decision Records)

Tài liệu này ghi lại các quyết định kỹ thuật, kiến trúc và giải pháp công nghệ quan trọng được đưa ra trong quá trình phát triển dự án, bao gồm bối cảnh phát sinh vấn đề, quyết định được lựa chọn và phân tích lý do kỹ thuật.

---

## ADR-01: Cố Định Phiên Bản NuGet 8.x Cho .NET 8
* **Bối cảnh:** Khi cài đặt các gói NuGet (như `Npgsql.EntityFrameworkCore.PostgreSQL` và `Microsoft.Extensions.*`) mà không chỉ định rõ phiên bản, hệ thống tự động tải bản mới nhất (v10.x hoặc v9.x) dẫn tới lỗi không tương thích phiên bản framework (NU1202).
* **Quyết định:** Chỉ định cứng phiên bản `8.x` trong các tệp `.csproj` cho toàn bộ các thư viện phụ thuộc .NET.
* **Lý do:** Bảo đảm tính tương thích tuyệt đối và quá trình biên dịch (Build) thành công ổn định trên nền tảng .NET 8.

---

## ADR-02: Trừu Tượng Hóa Audit & Soft Delete Bằng Entity Interface
* **Bối cảnh:** Cần duy trì tính nhất quán cho các trường kiểm toán thời gian (`NgayTao`, `NgayCapNhat`) và cờ xóa mềm (`IsDeleted`) trên tất cả các thực thể CSDL.
* **Quyết định:** Tạo các giao diện `IAuditableEntity` và `ISoftDeleteEntity` tại tầng Core. Dùng Fluent API cấu hình kiểu `timestamptz` và giá trị mặc định `now()`, đồng thời override phương thức `SaveChangesAsync` trong `AppDbContext` để tự động gán `NgayCapNhat = DateTime.UtcNow`.
* **Lý do:** Tránh lặp lại mã gán ngày tháng thủ công trong các Service, bảo đảm tính toàn vẹn dữ liệu tự động ở mức ORM.

---

## ADR-03: Chuẩn Hóa Chuỗi Kết Nối PostgreSQL Theo Chuẩn Npgsql
* **Bối cảnh:** Chuỗi kết nối từ dịch vụ Cloud Neon dạng URL PostgreSQL không được Npgsql nhận diện chính xác trên một số môi trường chạy console.
* **Quyết định:** Chuyển đổi định dạng kết nối sang chuẩn Key-Value của Npgsql: `Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true`.
* **Lý do:** Npgsql yêu cầu cấu trúc tham số rõ ràng để khởi tạo vùng kết nối (Connection Pool) và thiết lập kênh mã hóa bảo mật SSL/TLS.

---

## ADR-04: Tách Biệt Quyền "Tính Học Phí" Và "Xem Học Phí"
* **Bối cảnh:** Theo đặc tả ban đầu, vai trò Giảng viên chỉ được phép xem thông tin học phí, trong khi Giáo vụ và Admin được phép thực hiện tính toán và thay đổi số tiền.
* **Quyết định:** Tách thành 2 chức năng độc lập trong `PermissionMatrix`: `TinhHocPhi` (chỉ dành cho Admin, Trợ lý giáo vụ, Giáo vụ bộ môn) và `XemHocPhi` (cấp cho cả 4 vai trò).
* **Lý do:** Bảo đảm nguyên tắc đặc quyền tối thiểu (Principle of Least Privilege). Menu điều hướng sử dụng `XemHocPhi` để hiển thị tab, trong khi các nút tính toán kiểm tra nghiêm ngặt `TinhHocPhi`.

---

## ADR-05: Vị Trí Của Interface `ICurrentUserService`
* **Bối cảnh:** Interface quản lý thông tin phiên người dùng hiện hành có thể được đặt tại Core, Infrastructure hoặc App.
* **Quyết định:** Đặt Interface `ICurrentUserService` tại `QuanLyDKHP.Core/Interfaces` và triển khai `CurrentUserService` tại tầng `Services`. Đăng ký vào DI container dưới dạng Singleton tại tầng `App`.
* **Lý do:** Tuân thủ quy tắc phụ thuộc Clean Architecture: Tầng Core không phụ thuộc bất kỳ thư viện ngoài nào; tầng Services nắm giữ logic xác thực; tầng App chỉ đóng vai trò ghép nối DI.

---

## ADR-06: Truy Vấn Dashboard Qua Repository Riêng Biệt
* **Bối cảnh:** Màn hình Dashboard cần đếm số lượng bản ghi của 4 bảng và lấy danh sách lớp học của giảng viên. Một số ý kiến đề xuất gọi trực tiếp `DbContext` từ Service để giảm số file.
* **Quyết định:** Từ chối gọi trực tiếp `DbContext`. Tạo riêng `IDashboardRepository` trong Core và hiện thực hóa tại Infrastructure, sau đó tiêm vào `BaoCaoService`.
* **Lý do:** Bảo vệ sự phân tách trách nhiệm (Separation of Concerns). Service tuyệt đối không thao tác trực tiếp với cơ chế lưu trữ của CSDL.

---

## ADR-07: Giải Quyết Lỗi Disposed DbContext Tại Màn Hình Đăng Nhập
* **Bối cảnh:** Khi đăng nhập, ứng dụng phát sinh lỗi: `Cannot access a disposed context instance. Object name: 'AppDbContext'`.
* **Quyết định:** Loại bỏ khối `using var scope = ServiceProvider.CreateScope();` trong hàm `ShowLoginWindow` của `App.axaml.cs`. Sử dụng `ServiceProvider` gốc trực tiếp để resolve các ViewModel.
* **Lý do:** Khối `using` tự động hủy scope ngay khi hàm `ShowLoginWindow` kết thúc, trong khi sự kiện bấm nút đăng nhập diễn ra sau đó dẫn đến việc truy cập vào DbContext đã bị hủy.

---

## ADR-08: Sử Dụng DTOs Đóng Gói Dữ Liệu Tính Toán Trên Giao Diện
* **Bối cảnh:** Màn hình Sinh viên và Môn học cần hiển thị các trường tổng hợp (Số tín chỉ đăng ký, Tổng số tín chỉ, Số lớp đang mở).
* **Quyết định:** Tạo các DTO (`SinhVienDto`, `MonHocDto`) chứa các trường tính toán, chuyển đổi dữ liệu thông qua LINQ Select tại Repository.
* **Lý do:** Ngăn chặn việc đưa trực tiếp Domain Entities lên tầng Giao diện, bảo vệ tính trừu tượng và tối ưu hóa lượng dữ liệu truyền qua bộ nhớ.

---

## ADR-09: Sắp Xếp Danh Mục Tiếng Việt Chuẩn Bằng `CultureInfo("vi-VN")`
* **Bối cảnh:** Thuật toán sắp xếp mặc định của máy tính sử dụng bảng mã ASCII/Unicode nhị phân, dẫn đến chữ cái tiếng Việt có dấu (như Đ, Ê, Ô) bị đẩy xuống cuối danh sách sai lệch với từ điển tiếng Việt.
* **Quyết định:** Cấu hình `StringComparer.Create(new CultureInfo("vi-VN"), false)` trong các biểu thức sắp xếp LINQ của `MonHocService` và xây dựng `VietnameseNameComparer` cho DataGrid.
* **Lý do:** Đảm bảo thứ tự danh mục tuân thủ tuyệt đối quy tắc ngôn ngữ học tiếng Việt theo yêu cầu số 7 của đề bài.

---

## ADR-10: Chuyển Đổi Sang Mô Hình `IDbContextFactory` Cho Ứng Dụng Desktop
* **Bối cảnh:** Phát sinh lỗi tranh chấp đa luồng EF Core khi người dùng chuyển màn hình nhanh hoặc các tác vụ nền chạy song song.
* **Quyết định:** Thay thế toàn bộ `services.AddDbContext` bằng `services.AddDbContextFactory<AppDbContext>`. Mỗi phương thức Repository tự tạo và giải phóng context cục bộ qua `await using var context = await _contextFactory.CreateDbContextAsync()`.
* **Lý do:** Ứng dụng Desktop có vòng đời dài (Long-lived application), mô hình factory-per-operation là giải pháp chuẩn công nghiệp để đảm bảo tính an toàn luồng và giải phóng bộ nhớ.

---

## ADR-11: Thay Thế Hoàn Toàn Emoji Văn Bản Bằng `Material.Icons.Avalonia`
* **Bối cảnh:** Emoji gõ phím hiển thị không đồng nhất trên các hệ điều hành khác nhau, có hiện tượng đè chữ lên nhãn tiêu đề khi Avalonia đo đạc font metrics, và thiếu tính nghiêm túc của một phần mềm quản trị đại học.
* **Quyết định:** Gỡ bỏ toàn bộ emoji văn bản; tích hợp thư viện vector icon tối giản `Material.Icons.Avalonia`.
* **Lý do:** Đồng bộ giao diện, nét vẽ thanh lịch, tự động đổi màu theo Light/Dark mode và sắc nét ở mọi độ phân giải màn hình.

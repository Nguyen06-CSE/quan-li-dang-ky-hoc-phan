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

---

## ADR-12: Chuyển Đổi CSDL Chính Sang SQLite Local (WAL Mode) Theo Yêu Cầu Bảo Mật Máy Trạm Văn Phòng
* **Bối cảnh:** Ở giai đoạn đầu (F10), dự án đặt CSDL chính trên Cloud (Neon PostgreSQL) và dùng SQLite làm bộ nhớ đệm đọc. Tuy nhiên, theo yêu cầu vận hành thực tế, cán bộ nội vụ chỉ được phép thực hiện nghiệp vụ trên máy tính cố định tại văn phòng để bảo mật dữ liệu đào tạo và học phí; đồng thời quy mô dữ liệu của trường/khoa (vài chục đến vài trăm nghìn bản ghi đăng ký học phần, tương đương 20MB–150MB) nằm hoàn toàn trong ngưỡng hiệu năng tối ưu của SQLite.
* **Quyết định:** Chuyển CSDL chính (Primary Read/Write Database) sang **SQLite Local (`quanlydkhp.db`)** chạy ở chế độ `PRAGMA journal_mode = WAL;` và `PRAGMA foreign_keys = ON;`. Hợp nhất luồng truy xuất của `AppDbContext` và `LocalAppDbContext` về cùng một tệp SQLite duy nhất trên máy trạm, đồng thời thiết kế sẵn khả năng tích hợp **SQLCipher (AES-256)** khi cần mã hóa tệp vật lý.
* **Lý do:** Đảm bảo cô lập bảo mật tại máy tính văn phòng, triệt tiêu 100% độ trễ mạng cho cả thao tác Đọc lẫn Ghi (0.1ms–2ms), loại bỏ sự phức tạp và nguy cơ lệch dữ liệu khi phải đồng bộ hai chiều giữa Cloud và Local, không cần cài đặt dịch vụ Database Server chạy ngầm trên máy văn phòng.

---

## ADR-13: Thiết Kế Hệ Thống Sao Lưu Đa Tầng Kiểu Git (`LibGit2Sharp` + Canonical JSONL) Kết Hợp DB Host Replica
* **Bối cảnh:** Khi CSDL chính đặt tại máy tính cố định ở văn phòng, hệ thống cần cơ chế sao lưu dữ liệu hằng ngày vừa chống mất mát dữ liệu vừa không làm đầy ổ cứng (nếu mỗi ngày sao chép nguyên tệp nhị phân `.db` nặng hàng chục MB thì sẽ tốn rất nhiều dung lượng và không biết dữ liệu gì đã thay đổi), đồng thời cho phép khôi phục về bất kỳ ngày nào.
* **Quyết định:** Triển khai kiến trúc sao lưu kết hợp 2 tầng (Lựa chọn C):
  1. **Tầng 1 (Local Git-Style Backup):** Xuất dữ liệu 8 bảng ra định dạng văn bản chuẩn hóa theo dòng (`Canonical Sorted JSONL`, sắp xếp cố định theo khóa chính `ORDER BY PrimaryKey`) và sử dụng thư viện `LibGit2Sharp` để tự động tạo bản `Commit` mỗi ngày vào kho Git nội bộ (`.db-backup-git`), kèm 1 bản chụp nhị phân nóng (`SqliteConnection.BackupDatabase`).
  2. **Tầng 2 (Remote DB Host Replica):** Đảo chiều cơ chế Delta Sync cũ thành đẩy vi sai một chiều (`Local SQLite -> Remote PostgreSQL Host`) dựa trên mốc thời gian `NgayCapNhat`.
* **Lý do:** Biểu diễn `Canonical Sorted JSONL` giúp thuật toán nén vi sai (Delta Compression) của Git chỉ lưu đúng những dòng bản ghi thay đổi trong ngày (tốn vài KB mỗi ngày thay vì hàng chục MB), hỗ trợ xem lịch sử thay đổi tường minh và khôi phục (`Time-Travel Restore` kèm `Pre-Restore Safety Commit`) về bất kỳ thời điểm nào, trong khi tầng DB Host bảo vệ dữ liệu trước rủi ro hỏng phần cứng máy trạm.

---

## ADR-14: Đóng Gói Định Dạng Sao Lưu Ngoại Tuyến Bằng Chữ Ký `QuanLyDKHP-Backup-v1` Và Mã Kiểm Tra SHA-256 Checksum
* **Bối cảnh:** Cán bộ quản lý cần xuất (Export) bất kỳ mốc sao lưu nào thành một tệp lưu trữ ngoại tuyến (`.dkhp-backup.zip`) để lưu vào USB/ổ cứng ngoài, và có thể nhập (Import) lại vào một máy tính khác để ghi đè CSDL. Tuy nhiên, việc cho phép người dùng nạp một tệp nén tự do từ bên ngoài tiềm ẩn rủi ro phá hỏng cấu trúc dữ liệu hoặc nạp tệp không đúng chuẩn.
* **Quyết định:** Định nghĩa cấu trúc gói sao lưu độc quyền chứa tệp chỉ mục metadata `manifest.json`:
  * Thuộc tính `FormatSignature = "QuanLyDKHP-Backup-v1"`.
  * Từ điển `TableSha256Checksums` lưu mã hash SHA-256 độc lập của từng bảng dữ liệu `tables/*.jsonl`.
  * Trước khi ghi đè CSDL, hệ thống giải nén trong bộ nhớ và kiểm tra chữ ký định dạng cùng toàn bộ 8 mã SHA-256. Bất kỳ sự sai lệch hoặc thiếu tệp nào đều bị từ chối với thông báo lỗi cụ thể.
  * Ngay trước khi ghi đè, hệ thống tự động tạo một commit Git `[Pre-Import]` để bảo vệ dữ liệu hiện tại, cho phép hoàn tác (Undo) bất kỳ lúc nào.
* **Lý do:** Bảo đảm tính toàn vẹn 100% của tệp sao lưu, chống giả mạo hoặc chỉnh sửa thủ công làm sai lệch dữ liệu, đồng thời mang lại cơ chế an toàn tuyệt đối cho người vận hành.

---

## ADR-15: Chuyển Đổi Tác Vụ Sao Lưu Khi Thoát Ứng Dụng Sang ThreadPool Có Giới Hạn Thời Gian & Loại Bỏ `Cache=Shared`
* **Bối cảnh:** Ứng dụng Desktop bị treo cứng (con trỏ chuột xoay vòng - beachball trên macOS / Not Responding trên Windows) mỗi khi người dùng tắt ứng dụng.
* **Quyết định:**
  1. Trong sự kiện tắt ứng dụng `desktop.ShutdownRequested` của Avalonia, chuyển toàn bộ lệnh gọi sao lưu sang chạy ngầm trên ThreadPool thông qua `Task.Run()` kết hợp giới hạn thời gian an toàn tối đa 2 giây (`.Wait(TimeSpan.FromSeconds(2))`).
  2. Loại bỏ thuộc tính `Cache=Shared` khỏi chuỗi kết nối SQLite tại `App.axaml.cs`, chuyển về chế độ Private Cache mặc định: `Data Source={sqlitePath}`.
* **Lý do:**
  * Luồng UI của Avalonia được quản lý bởi `AvaloniaSynchronizationContext`. Khi luồng UI gọi `.GetAwaiter().GetResult()` chặn đồng bộ, các tác vụ `await` I/O bên trong EF Core / SQLite cố gắng điều phối (marshal) tiếp tục chạy trên luồng UI dẫn tới **Async-over-Sync Deadlock**. Đưa tác vụ vào `Task.Run` làm biến mất `SynchronizationContext`, triệt tiêu hoàn toàn khả năng deadlock.
  * Trong chế độ WAL, SQLite hỗ trợ đọc ghi đồng thời rất tốt giữa các kết nối độc lập. Thuộc tính `Cache=Shared` gây khóa chéo cấp bảng (Table Lock) giữa kết nối của `AppDbContext` và kết nối của `BackupService`, làm chậm và nghẽn I/O khi dọn dẹp tiến trình. Private Cache giúp mỗi kết nối hoạt động mượt mà, độc lập.

---
id: nhat-ky-su-co-va-sua-loi
title: Nhật Ký Xử Lý Sự Cố & Sửa Lỗi (Troubleshooting)
sidebar_position: 4
---

# Nhật Ký Xử Lý Sự Cố & Sửa Lỗi (Troubleshooting)

Trong quá trình phát triển ứng dụng Desktop phức tạp với cơ sở dữ liệu phân tán và giao diện đồ họa hiện đại, nhóm đã gặp và giải quyết dứt điểm nhiều sự cố kỹ thuật chuyên sâu. Tài liệu này ghi nhận chi tiết nguyên nhân gốc rễ và giải pháp khắc phục.

---

## 1. Bảng Tổng Hợp Trạng Thái Các Sự Cố Kỹ Thuật

| Mã Lỗi | Phân Hệ Phát Sinh | Hiện Tượng / Triệu Chứng | Mức Độ | Trạng Thái |
|:---:|---|---|:---:|:---:|
| **ERR-01** | Môi trường macOS | Thông báo ứng dụng bị hỏng (Damaged and can't be opened) | Nghiêm trọng | [Đã xử lý] |
| **ERR-02** | Khởi chạy CLI | `Couldn't find a project to run` khi gõ `dotnet run` | Trung bình | [Đã xử lý] |
| **ERR-03** | EF Core / Data | `A second operation was started on this context instance...` | Rất nghiêm trọng | [Đã xử lý] |
| **ERR-04** | Import Excel | Chậm với tập tin lớn, nguy cơ mất dữ liệu khi lỗi 1 dòng | Nghiêm trọng | [Đã xử lý] |
| **ERR-05** | Menu Navigation | `NullReferenceException` khi bấm menu Import | Trung bình | [Đã xử lý] |
| **ERR-06** | UI Data Binding | `StackOverflowException` do vòng lặp Binding Loop tại ĐKHP | Rất nghiêm trọng | [Đã xử lý] |
| **ERR-07** | Điều hướng nhanh | Double-click sinh viên khi danh sách chưa tải xong gây lỗi | Trung bình | [Đã xử lý] |
| **ERR-08** | SQLite Local / Auth | `SQLite Error 1: no such table: NguoiDung` khi đăng nhập admin | Nghiêm trọng | [Đã xử lý] |
| **ERR-09** | Vòng đời / Tắt ứng dụng | Treo ứng dụng khi đóng/thoát (Deadlock luồng UI tại `ShutdownRequested`) | Nghiêm trọng | [Đã xử lý] |

---

## 2. Chi Tiết Các Sự Cố Trọng Yếu & Phương Án Khắc Phục

### Sự Cố 1: macOS Báo "QuanLyDKHP.App.app Bị Hỏng Và Không Thể Mở Được"
* **Triệu chứng:** Hộp thoại cảnh báo hệ thống của macOS hiển thị yêu cầu di chuyển ứng dụng vào Thùng rác (Move to Trash) khi chạy lệnh `dotnet run`, dù quá trình `dotnet build` hoàn toàn thành công.
* **Nguyên nhân gốc rễ:** Không phải do lỗi mã nguồn C#. Tệp tải về dưới dạng tệp nén `.zip` bị cơ chế bảo mật Gatekeeper của macOS gắn thuộc tính mở rộng cách ly (`com.apple.quarantine`), chặn thực thi các thư viện động `.dylib` chưa được ký số thương mại (Notarization).
* **Phương án khắc phục:**
  1. Gỡ bỏ thuộc tính cách ly toàn bộ thư mục:
     ```bash
     xattr -cr .
     ```
  2. Dọn dẹp các tệp nhị phân cũ:
     ```bash
     find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
     ```
  3. Ký chữ ký ad-hoc cục bộ cho các tệp nhị phân trên máy Mac:
     ```bash
     find bin -name "*.dylib" -exec codesign --force --sign - {} \;
     codesign --force --sign - bin/Debug/net8.0/QuanLyDKHP.App
     dotnet run --no-build --project src/QuanLyDKHP.App
     ```

---

### Sự Cố 2: EF Core "A Second Operation Was Started On This Context Instance..."
* **Triệu chứng:** Khi mở ứng dụng hoặc chuyển đổi nhanh giữa các màn hình Sinh viên, Môn học, thanh trạng thái báo lỗi: *"A second operation was started on this context instance before a previous operation completed"*, bảng dữ liệu bị trắng hoàn toàn.
* **Nguyên nhân gốc rễ:** Trong `App.axaml.cs`, hệ thống ban đầu đăng ký `AddDbContext` (mô hình Scoped). Tuy nhiên, ứng dụng Desktop không có cơ chế request-scope như Web ASP.NET mà dùng chung một DbContext Singleton trong suốt phiên. Khi 2 tác vụ bất đồng bộ (`LoadDataAsync` và `InitFiltersAsync`) chạy song song, chúng cùng truy cập vào một DbContext duy nhất. Entity Framework Core nghiêm cấm chạy đồng thời 2 truy vấn trên cùng 1 context.
* **Phương án khắc phục:**
  1. Chuyển đổi đăng ký sang Factory Pattern: `services.AddDbContextFactory<AppDbContext>(...)`.
  2. Tái cấu trúc toàn bộ 10 Repositories: Tiếp nhận `IDbContextFactory<AppDbContext>`, mỗi hàm tự tạo và giải phóng context cục bộ:
     ```csharp
     await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
     return await context.SinhViens.AsNoTracking().ToListAsync(cancellationToken);
     ```
  3. Thêm cờ `_dangKhoiTao` trong ViewModel để chặn các sự kiện thay đổi bộ lọc kích hoạt tải dữ liệu khi giao diện đang trong quá trình nạp khởi đầu.

---

### Sự Cố 3: Import Excel Bị Nghẽn & Dễ Hỏng Toàn Bộ Dữ Liệu
* **Triệu chứng:** Quá trình nạp tệp Excel 1.000 dòng mất hơn 1 phút; nếu chỉ có 1 dòng dữ liệu bị lỗi (ví dụ: ngày sinh sai định dạng), toàn bộ 999 dòng còn lại đều bị hủy bỏ và không lưu vào CSDL.
* **Nguyên nhân gốc rễ:**
  * Thuật toán cũ gọi `SaveChanges` cho từng dòng sinh viên và tính lại học phí ngay lập tức (N+1 Query).
  * Bộ theo dõi thay đổi (`Change Tracker`) của EF Core bị quá tải khi tích lũy hàng nghìn thực thể với cờ `AutoDetectChanges = true`.
  * Thiếu khối bao bọc xử lý ngoại lệ theo từng bản ghi.
* **Phương án khắc phục:**
  1. Triển khai xử lý theo lô (Chunk size = 500 dòng). Mỗi lô chạy trong 1 Transaction riêng biệt.
  2. Cơ chế cách ly lỗi (Fault Isolation): Bao bọc kiểm tra dữ liệu từng dòng; dòng lỗi được ghi vào danh sách thông báo riêng, các dòng hợp lệ vẫn được commit vào CSDL bình thường.
  3. Tắt `AutoDetectChanges` trong quá trình lặp và chỉ bật lại khi commit lô, đồng thời thực hiện tính học phí theo lô một lần duy nhất ở bước kết thúc.

---

### Sự Cố 4: Vòng Lặp Liên Kết Giao Diện (Binding Loop `StackOverflowException`)
* **Triệu chứng:** Ứng dụng Desktop bị crash đột ngột với mã lỗi `StackOverflowException` khi người dùng chuyển từ tab Sinh viên sang tab Đăng ký học phần.
* **Nguyên nhân gốc rễ:** Trong `DangKyHocPhanView.axaml`, việc liên kết hai chiều (`Mode=TwoWay`) giữa thuộc tính `SelectedItem` và `SelectedIndex` trên `ItemsControl` kết hợp với sự kiện nạp lại danh sách môn học tạo thành một vòng lặp sự kiện vô tận: Thuộc tính này thay đổi kích hoạt thuộc tính kia cập nhật và ngược lại, làm tràn ngăn xếp bộ nhớ (Call Stack).
* **Phương án khắc phục:**
  * Ngắt liên kết hai chiều dư thừa, chỉ dùng `OneWayToSource` cho sự kiện chọn dòng.
  * Tối ưu hóa việc cập nhật dữ liệu thông qua `ObservableCollection.Reset()` hoặc tạo mới danh sách thay vì thay đổi từng phần tử đơn lẻ.

---

### Sự Cố 5: Lỗi Không Tìm Thấy Bảng `NguoiDung` Khi Chuyển Sang CSDL SQLite Local (`ERR-08`)
* **Triệu chứng:** Sau khi chuyển đổi CSDL chính sang SQLite Local, người dùng nhập tài khoản `admin` / mật khẩu `admin` thì màn hình đăng nhập báo lỗi: `SQLite Error 1: no such table: NguoiDung`.
* **Nguyên nhân gốc rễ:**
  * Tệp `quanlydkhp.db` trên máy trạm trước đó được sinh ra bởi ngữ cảnh `LocalAppDbContext` cũ (vốn chỉ chứa các bảng cache cho F10 và chưa bao gồm bảng `NguoiDung`).
  * Trong Entity Framework Core, phương thức `Database.EnsureCreated()` có đặc tính: **Nếu tệp cơ sở dữ liệu đã tồn tại và đã có ít nhất 1 bảng bất kỳ, EF Core sẽ coi như CSDL đã được tạo xong và bỏ qua toàn bộ việc tạo các bảng mới còn thiếu**. Do đó, bảng `NguoiDung` không bao giờ được tạo tự động nếu tệp SQLite đã tồn tại từ trước.
* **Phương án khắc phục:**
  1. Hợp nhất `LocalAppDbContext` kế thừa trực tiếp từ `AppDbContext` để sử dụng chung một mô hình dữ liệu chuẩn.
  2. Xây dựng phương thức khởi tạo chủ động `AppDbContext.EnsureTablesAndSeedAsync(db)` thực thi câu lệnh SQL trực tiếp:
     ```sql
     CREATE TABLE IF NOT EXISTS "NguoiDung" (
         "Id" TEXT NOT NULL CONSTRAINT "PK_NguoiDung" PRIMARY KEY,
         "TenDangNhap" TEXT NOT NULL,
         "MatKhauHash" TEXT NOT NULL,
         "HoTen" TEXT NOT NULL,
         "Email" TEXT NOT NULL,
         "Role" INTEGER NOT NULL,
         "IsActive" INTEGER NOT NULL,
         "NgayTao" TEXT NOT NULL,
         "NgayCapNhat" TEXT NULL,
         "IsDeleted" INTEGER NOT NULL
     );
     ```
  3. Kiểm tra nếu bảng `NguoiDung` chưa có bản ghi nào thì tự động chèn tài khoản khởi tạo `admin` (mật khẩu băm BCrypt chuẩn, vai trò `Admin`).
  4. Gọi phương thức này ngay trong bước nạp đầu của `App.axaml.cs` trước khi hiển thị `LoginWindow`.

---

### Sự Cố 6: Ứng Dụng Bị Treo Khi Đóng Cửa Sổ / Thoát Chương Trình (`ERR-09`)
* **Triệu chứng:** Khi người dùng nhấn nút đóng cửa sổ (dấu 'X' đỏ trên macOS / nút Close trên Windows) hoặc nhấn phím tắt Cmd+Q để thoát ứng dụng, cửa sổ biến mất nhưng tiến trình ứng dụng vẫn chạy ngầm và rơi vào trạng thái "Not Responding" (con trỏ chuột xoay tròn beachball), buộc người dùng phải mở Activity Monitor để Force Quit.
* **Nguyên nhân gốc rễ:**
  * **Deadlock luồng giao diện (Async-over-Sync Deadlock):** Trong sự kiện `desktop.ShutdownRequested` của Avalonia, thao tác tự động sao lưu cuối phiên (`Auto-OnExit`) được gọi bằng cách:
    ```csharp
    // ❌ LỖI DEADLOCK:
    backupService.CreateLocalGitBackupAsync("Auto-OnExit", skipIfNoChanges: true).GetAwaiter().GetResult();
    ```
  * `ShutdownRequested` được gọi trực tiếp trên luồng giao diện UI, nơi đang có `AvaloniaSynchronizationContext` hoạt động. Lệnh `.GetAwaiter().GetResult()` khóa chặt luồng UI để đợi Task hoàn thành.
  * Trong khi đó, các tác vụ con bên trong `CreateLocalGitBackupAsync` (như truy vấn EF Core `ToListAsync()`, ghi tệp JSONL) khi hoàn thành I/O trên ThreadPool lại cố gắng điều phối ngữ cảnh trở lại luồng UI. Do luồng UI đang bị chặn bởi `.GetResult()`, nó không thể phân phối công việc tiếp theo $\rightarrow$ **Vòng lặp chờ vô tận (Deadlock)**.
  * **Tranh chấp khóa tệp SQLite (`Cache=Shared`):** Việc bật `Cache=Shared` trên chuỗi kết nối SQLite làm phát sinh khóa bảng chéo giữa luồng đọc của EF Core và luồng chụp snapshot `SqliteConnection.BackupDatabase`, khiến tiến trình dọn dẹp kết nối khi tắt bị kẹt.
* **Phương án khắc phục:**
  1. **Bọc Task.Run có timeout trên ThreadPool:**
     ```csharp
     desktop.ShutdownRequested += (s, e) =>
     {
         try
         {
             Task.Run(async () =>
             {
                 using var exitScope = ServiceProvider.CreateScope();
                 var backupService = exitScope.ServiceProvider.GetRequiredService<IBackupService>();
                 await backupService.CreateLocalGitBackupAsync("Auto-OnExit", skipIfNoChanges: true);
             }).Wait(TimeSpan.FromSeconds(2));
         }
         catch { }
     };
     ```
     `Task.Run` loại bỏ hoàn toàn `SynchronizationContext`, giải phóng luồng UI khỏi việc xếp hàng tiếp nối. Ngưỡng `Wait(2s)` đảm bảo ứng dụng luôn tắt êm ái ngay cả khi I/O đĩa bận rộn.
  2. **Chuyển SQLite sang Private Cache chuẩn:** Thay `Data Source={sqlitePath};Cache=Shared` bằng `Data Source={sqlitePath}`. Chế độ WAL tự động quản lý đồng thời giữa các kết nối độc lập mà không cần khóa bảng chéo.
  3. **Kiểm thử tự động:** Bổ sung bài test `AutoOnExitBackup_CompletesFast_WithoutDeadlock` xác thực quá trình thoát luôn hoàn thành dưới 2 giây.

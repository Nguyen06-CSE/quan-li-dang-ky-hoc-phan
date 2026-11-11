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
| **ERR-10** | Import Excel & Học kỳ | Mất lịch sử ngày ĐK do SA/CH, thiếu Upsert, bỏ sót cột và chặn kỳ | Nghiêm trọng | [Đã xử lý] |
| **ERR-11** | ĐKHP & Tính Học Phí | Race condition sĩ số, đăng ký kỳ đã đóng, lệch công thức học phí, kỳ đóng 0đ, thiếu nộp tiền | Nghiêm trọng | [Đã xử lý] |

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


---

### Sự Cố 7: Lỗi Mất Lịch Sử Ngày Đăng Ký, Bỏ Sót Cột & Không Upsert Khi Import Excel 21 Cột (`ERR-10`)
* **Triệu chứng:**
  * Toàn bộ ngày đăng ký thực tế của sinh viên (VD: `15/06/2026 9:43:00 SA`, `19/06/2026 2:00:00 CH`) bị nhảy về thời điểm import hiện tại (`DateTime.UtcNow`).
  * Khi giáo vụ nạp lại tệp Excel để cập nhật điểm thi hoặc tình trạng nộp học phí, các bản ghi đăng ký cũ bị bỏ qua hoàn toàn, không thể cập nhật điểm số.
  * Tên giảng viên (Cột 10) và thông tin nợ phí (Cột 18-20) bị bỏ trống dù trong file có dữ liệu.
  * Ứng dụng cưỡng bức người dùng phải chọn học kỳ trước khi đọc file; `ComboBox` học kỳ hay bị trắng hoặc mất lựa chọn sau khi tải lại danh sách.
  * Phát sinh lỗi xung đột dữ liệu (Race condition) và *Collection was modified* khi người dùng chuyển đổi học kỳ trong quá trình xuất báo cáo Excel.
  * Nút "Import Excel" trong màn hình Quản lý Sinh viên không hoạt động, hiển thị thông báo giả lập *"Mô-đun Import Excel đang được tích hợp (015-Spec)"*.
  * Đọc file Excel bị lỗi `InvalidCastException` khi gặp các ô kiểu Số (như Mã SV, Số TC, Tiền học phí) do gọi phương thức `.GetString()` đơn thuần của ClosedXML.
  * Ứng dụng khởi động không hiển thị dữ liệu (0 bản ghi) dù Neon có sẵn dữ liệu và tệp SQLite local đã tồn tại 6.661 bản ghi.
* **Nguyên nhân gốc rễ:**
  * Thư viện ClosedXML sẽ ném ngoại lệ kiểu dữ liệu nếu gọi `cell.GetString()` trên ô có `DataType == XLDataType.Number` hoặc chứa công thức.
  * Delegate `OpenFileDialogFunc` trong `SinhVienViewModel` chưa được gán trong code-behind của `SinhVienView`, dẫn tới nhánh fallback hiển thị chuỗi thông báo giả lập cũ.
  * Trên môi trường macOS/non-Windows, `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)` trả về chuỗi rỗng `""`, khiến đường dẫn tương đối tạo ra CSDL SQLite trắng mới thay vì trỏ tới tệp CSDL thực tế. Đồng thời luồng khởi động chưa tự động kích hoạt `SyncService.InitialSeedAsync()` khi phát hiện SQLite trống.
  * Hàm `DateTime.TryParse` chuẩn .NET không hỗ trợ định dạng thời gian 12h tiếng Việt với hậu tố `SA` (Sáng) và `CH` (Chiều) trên môi trường non-Windows, khiến hàm parse thất bại và rơi vào nhánh cứu nguy gán `DateTime.UtcNow`.
  * Bộ nhớ đệm `DkCache` chỉ dùng `HashSet<string>` kiểm tra tồn tại. Khi gặp bản ghi đã có thì bỏ qua (`continue`), không có cơ chế `Attach` để cập nhật các trường mới.
  * Phương thức `DocFileExcel` đọc thiếu Cột 10 và Cột 18, 19, 20.
  * Thực thể `HocKy` chưa cài đặt `IEquatable<HocKy>` và `Equals`, dẫn tới Avalonia ComboBox so sánh theo địa chỉ tham chiếu (Reference Equality) thay vì so sánh theo khóa chính `MaHocKy`.
  * Trong `HocKyService.CapNhatAsync`, thuộc tính `existing.DangMo` không được gán từ tham số truyền vào, khiến thao tác đóng học kỳ không bao giờ có hiệu lực.
* **Phương án khắc phục:**
  1. **Đọc ô Excel đa kiểu dữ liệu (`GetCellText`):** Sử dụng `cell.GetFormattedString()` kết hợp `cell.Value.ToString()`, đọc an toàn mọi ô kiểu số (Mã SV, Số TC, Tiền học phí) và tự động bỏ qua các dòng trắng ở cuối file.
  2. **Tích hợp nút Import Excel tại màn hình Sinh viên:** Kết nối `OpenFileDialogFunc` mở hộp thoại chọn file `.xlsx`, tự động import và tải lại danh sách sinh viên; đồng thời gửi `NavigateToImportExcelMessage` chuyển sang tab Import Excel khi cần thiết.
  3. **Định vị CSDL bền vững & Tự động Seed từ Neon:** Hoàn thiện `AppDbContext.GetPrimarySqlitePath()` tự động rà soát các đường dẫn dự phòng (`QuanLyDKHP/quanlydkhp.db`); kích hoạt `syncService.InitialSeedAsync()` trong background task khi khởi động để kéo toàn bộ dữ liệu từ Neon về SQLite nếu local chưa có dữ liệu.
  4. **Chuẩn hóa chuỗi thời gian tiếng Việt:** Dùng biểu thức chính quy chuyển đổi `SA` $\rightarrow$ `AM` và `CH` $\rightarrow$ `PM`, kết hợp `DateTime.TryParseExact` đa định dạng chuyển sang chuẩn UTC.
  5. **Cơ chế Upsert toàn diện:** Chuyển `DkCache` sang `Dictionary<string, DangKyHocPhan>`. Khi đã có đăng ký, hệ thống kiểm tra và cập nhật các trường thay đổi (`DiemSo`, `DiemChu`, `NgayDK`, `NguoiDK`, `SoTienPhaiDong`, `SoTienDaDong`).
  6. **Đọc đủ 21 cột:** Bổ sung Cột 10 (`Tên GV` - tự động tra cứu nếu thiếu `MaGV`), Cột 18-20 (Học phí, Đã đóng, Phải đóng).
  7. **Nạp dữ liệu Một Chạm (One-Click):** Cho phép nạp file mà không bắt buộc chọn học kỳ. Hệ thống tự động suy luận học kỳ từ mã lớp học phần hoặc học kỳ đang mở.
  8. **Cài đặt `IEquatable<HocKy>` & sửa `CapNhatAsync`:** Đảm bảo ComboBox nhận diện đúng đối tượng và cho phép đóng học kỳ thành công.
  9. **Snapshot danh sách xuất Excel:** Chụp nhanh danh sách bằng `.ToList()` và cố định học kỳ/môn học tại thời điểm bấm xuất, dùng `CancellationTokenSource` hủy tác vụ cũ khi đổi kỳ nhanh.

---

### Sự Cố 8: Xung Đột Đồng Thời Sĩ Số Lớp Học Phần & Bất Đồng Nhất Tính Toán Học Phí (`ERR-11`)
* **Triệu chứng:**
  * Sinh viên vẫn có thể đăng ký môn học hoặc hủy môn trong các học kỳ đã kết thúc/đã đóng (`DangMo = false`).
  * Phát sinh lỗi vượt quá sĩ số tối đa (`SiSoToiDa`) khi có nhiều yêu cầu đăng ký gửi đồng thời (Race Condition giữa bước đếm số lượng và bước lưu bản ghi).
  * Số tiền học phí hiển thị trên màn hình danh sách tổng hợp (`HocPhiViewModel`) bị lệch với số tiền hiển thị trong modal chi tiết của cùng sinh viên đó.
  * Khi chọn xem học phí của các học kỳ đã đóng, toàn bộ bảng danh sách bị hiển thị 0đ và 0 tín chỉ.
  * Chưa có chức năng cho phép kế toán/giáo vụ ghi nhận số tiền sinh viên nộp học phí trực tiếp trên ứng dụng.
* **Nguyên nhân gốc rễ:**
  * `DangKyHocPhanService.DangKyAsync` và `HuyDangKyAsync` chưa kiểm tra cờ `HocKy.DangMo` trước khi thực thi nghiệp vụ.
  * Quá trình kiểm tra sĩ số `DemSiSoDangKyAsync` và lệnh lưu `ThemAsync`/`CapNhatAsync` nằm ở hai truy vấn tách biệt, không nằm trong transaction hoặc cơ chế lock đồng bộ.
  * `LocalReadService.GetHocPhiTheoDanhSachLocalAsync` khi gặp `!dangMo` chỉ đọc từ bảng snapshot `HocPhiHocKys`. Với dữ liệu nạp từ Excel hoặc học kỳ đóng chưa khóa sổ, bản ghi snapshot chưa tồn tại dẫn đến trả về các giá trị 0đ.
  * Màn hình danh sách tự ý cộng thêm nợ cũ (`noCu`), trong khi modal chi tiết `TinhHocPhiSinhVienAsync` chỉ tính tổng tiền các môn học thực tế của kỳ, dẫn tới hai màn hình không khớp nhau.
  * Thiếu phương thức nghiệp vụ và giao diện ghi nhận tiền nộp học phí.
* **Phương án khắc phục:**
  1. **Chốt chặn Học kỳ mở (`Bước 0`):** Bổ sung bước kiểm tra `HocKy.DangMo == true` ngay đầu phương thức `DangKyAsync` và `HuyDangKyAsync`. Chặn cứng ngay từ đầu nếu kỳ học đã đóng.
  2. **Giao dịch nguyên tử chống Race Condition (`LuuDangKyAtomicAsync`):** Kết hợp `SemaphoreSlim` nội bộ và giao dịch CSDL `BeginTransactionAsync`. Kiểm tra sĩ số thực tế tức thời ngay trong transaction; nếu đã đủ chỗ thì tự động rollback và trả về thông báo lỗi.
  3. **Thống nhất 1 công thức tính học phí duy nhất:** Đồng bộ hóa công thức giữa `GetHocPhiTheoDanhSachLocalAsync` và `TinhHocPhiSinhVienAsync`: `Tổng Học Phí = Tổng các môn [SoTienPhaiDong hoặc (LT * ĐG_LT + TH * ĐG_TH)]`, `Còn Nợ = Tổng Học Phí - Đã Đóng`.
  4. **Cơ chế Fallback chống lỗi 0đ kỳ đã đóng:** Trong `LocalReadService`, nếu `!dangMo` mà chưa có snapshot `HocPhiHocKy`, hệ thống tự động fallback tính toán trực tiếp từ bảng `DangKyHocPhan` của học kỳ đó, bảo đảm dữ liệu luôn hiển thị đúng.
  5. **Bổ sung chức năng Ghi nhận nộp tiền:** Thêm phương thức `GhiNhanNopTienAsync` tự động phân bổ tiền nộp cho các môn còn nợ; tích hợp nút "Ghi nhận nộp tiền" trực tiếp trên hàng của bảng và modal xem chi tiết, đi kèm hộp thoại nhập số tiền nộp thân thiện.

---

### ERR-12: Sự Cố Quản Lý Danh Mục (Tái tạo mã sau Soft-Delete, Thống nhất Giảng viên, Chặn mở khóa tự ý & Chặn đổi môn LHP)
* **Thời điểm phát hiện:** 11/11/2026.
* **Mức độ nghiêm trọng:** 🔴 Nghiêm trọng (Gây crash ứng dụng do lỗi UNIQUE constraint trong CSDL, sai lệch thông tin giảng viên và vô tình mở khóa tài khoản bị vô hiệu hóa).
* **Mô tả hiện tượng:**
  1. Khi xóa mềm (`IsDeleted = true`) một sinh viên, môn học, lớp học phần hoặc tài khoản người dùng, sau đó tạo mới lại với cùng mã/tên đăng nhập, hàm `TonTaiAsync` chỉ lọc `!IsDeleted` nên báo không trùng. Khi CSDL lưu thì xảy ra crash lỗi vi phạm khóa chính hoặc chỉ mục duy nhất (`UNIQUE constraint failed` / `duplicate key value violates unique constraint`).
  2. Không thống nhất giữa `MaGV` và `TenDangNhap`: Khi giáo vụ chọn giảng viên cho LHP, hệ thống lưu `TenDangNhap` thay vì `MaGV` (hoặc ngược lại). Khi hiển thị trên bảng đăng ký và dashboard giảng viên, việc tìm kiếm theo từ điển chỉ hỗ trợ 1 loại khóa dẫn đến bị hiện "Chưa phân công" hoặc giảng viên không thấy lớp mình dạy.
  3. Mở khóa tài khoản tự ý: Tại hộp thoại sửa thông tin người dùng (`NguoiDungViewModel`), hàm lưu hardcode `TrangThai = "HoatDong"`. Nếu tài khoản trước đó đang bị khóa (`DaKhoa`), chỉ cần admin bấm sửa họ tên hoặc vai trò là tài khoản tự động được mở khóa.
  4. Lớp học phần đã có sinh viên đăng ký vẫn cho phép giáo vụ chỉnh sửa đổi sang môn học khác (`MaMon`), dẫn tới sai lệch toàn bộ danh mục đăng ký và học phí của sinh viên đã đăng ký trước đó.
* **Nguyên nhân gốc rễ:**
  * Lệnh `ThemAsync` ở các Repository luôn gọi `Add(entity)` mà không kiểm tra xem bản ghi đã từng tồn tại trong CSDL dưới dạng xóa mềm hay chưa.
  * Thiếu ánh xạ hai chiều giữa `MaGV` và `TenDangNhap` trong `LopHocPhanEditDialogViewModel`, `HocKyLopHocPhanViewModel`, `DangKyHocPhanViewModel` và `DashboardRepository`.
  * `NguoiDungViewModel.MoDialogSua` không lưu giữ `user.TrangThai` và `CapNhatAsync` ghi đè giá trị này.
  * `LopHocPhanService.CapNhatAsync` thiếu điều kiện chặn đổi `MaMon` khi `CoDangKyHocPhanAsync(maLHP) == true`.
* **Phương án khắc phục:**
  1. **Cơ chế Tái kích hoạt sau Xóa mềm (Soft-Delete Reactivation):** Trong `SinhVienRepository`, `MonHocRepository`, `LopHocPhanRepository`, `NguoiDungRepository`, phương thức `ThemAsync` kiểm tra xem bản ghi đã tồn tại chưa:
     * Nếu tồn tại và `!IsDeleted`: Báo lỗi mã đã tồn tại.
     * Nếu tồn tại và `IsDeleted`: Tự động phục hồi bản ghi (`IsDeleted = false`), cập nhật thông tin mới và gán `NgayCapNhat = DateTime.UtcNow`.
  2. **Thống nhất nhận diện Giảng viên:** 
     * Khi tạo người dùng Giảng viên, nếu bỏ trống `MaGV` thì tự động lấy `TenDangNhap`. Đồng thời kiểm tra tính duy nhất của `MaGV` qua `GetByMaGVAsync`.
     * `LopHocPhanEditDialogViewModel` lưu trữ cả `MaNguoiDung` và `TenDangNhap`, đối chiếu linh hoạt khi mở hộp thoại.
     * `gvDict` trong các ViewModel được nạp cả hai khóa `TenDangNhap` và `MaGV` vào cùng từ điển (case-insensitive).
     * `DashboardRepository.GetLhpByGiangVienAsync` đối chiếu cả `MaGV` lẫn `TenDangNhap`.
  3. **Bảo toàn trạng thái Khóa tài khoản:** `NguoiDungViewModel` lưu giữ `EditTrangThai` từ `user.TrangThai`. `NguoiDungService.CapNhatAsync` không tự ý mở khóa tài khoản, việc thay đổi trạng thái bắt buộc phải qua `DoiTrangThaiAsync`.
  4. **Chặn tuyệt đối đổi Môn học và hạ Sĩ số:**
     * Trong `LopHocPhanService.CapNhatAsync`, nếu `existing.MaMon != lhp.MaMon` mà `CoDangKyHocPhanAsync == true`, ném ngoại lệ chặn lưu.
     * Nếu `lhp.SiSoToiDa < DemSiSoDangKyAsync`, ném ngoại lệ chặn hạ sĩ số thấp hơn số người đang học.
     * Trên giao diện `LopHocPhanEditDialog`, ComboBox Môn học tự động bị khóa (`IsEnabled = false`) đi kèm ToolTip cảnh báo khi lớp đã có người đăng ký.

---
id: nhat-ky-cong-viec-daily
title: Nhật Ký Công Việc Hằng Ngày (Daily Logs)
sidebar_position: 2
---

# Nhật Ký Công Việc Hằng Ngày (Daily Logs)

## 1. Nhật Ký Ngày 09/11/2026

* **Thực hiện:** NGUYÊN
* **Nội dung trọng tâm:** Chuyển đổi CSDL chính sang SQLite Local (WAL Mode), Triển khai hệ thống sao lưu đa tầng kiểu Git (`LibGit2Sharp`), Nâng cấp UI Master-Detail sao lưu, Export/Import file zip & Sửa lỗi treo ứng dụng khi thoát.

### Các kết quả đạt được
1. **Chuyển đổi CSDL chính sang SQLite Local (WAL Mode) - F11:**
   * Cấu hình SQLite (`quanlydkhp.db`) làm CSDL chính duy nhất của máy trạm cố định tại văn phòng với `PRAGMA journal_mode = WAL;` và `PRAGMA foreign_keys = ON;`.
   * Hợp nhất `AppDbContext` và `LocalAppDbContext`, tự động khởi tạo bảng `NguoiDung` và seed tài khoản admin qua `EnsureTablesAndSeedAsync()`.
   * Đạt thời gian truy xuất đọc/ghi nghiệp vụ tức thì **0.1ms – 2ms** trên toàn bộ 9 Repositories.
2. **Hệ thống sao lưu đa tầng kiểu Git (Tier 1 Local Git + Tier 2 DB Host Replica):**
   * Tích hợp thư viện `LibGit2Sharp` (v0.30.0).
   * Chuẩn hóa kết xuất 8 bảng CSDL thành tệp phẳng **Canonical Sorted JSONL** (`ORDER BY PrimaryKey`), giúp Git phát huy tối đa thuật toán nén vi sai theo dòng (`Zlib + Myers Diff`).
   * Duy trì bản chụp nhị phân nóng `latest_snapshot.db` bằng `SqliteConnection.BackupDatabase()`.
   * Hỗ trợ khôi phục du hành thời gian (`Time-Travel Restore`) kèm cơ chế tạo điểm an toàn tự động `[Pre-Restore Safety Commit]`.
   * Hỗ trợ đồng bộ đẩy vi sai một chiều (`One-Way Push Delta Sync`) lên máy chủ PostgreSQL dự phòng qua `RemoteBackupDbContext`.
3. **Nâng cấp UI Master-Detail Sao lưu & Export/Import File Zip:**
   * Tách giao diện `CauHinhView` thành 2 Tab riêng biệt (`Thông số Tín chỉ & Học phí` và `Sao lưu & Khôi phục CSDL`) để tối đa hóa không gian hiển thị.
   * Thiết kế bố cục **Master-Detail**: Cột trái hiển thị danh sách commit Git; click vào bất kỳ commit nào để xem cột phải (Inspector Panel) với thống kê biến động 8 bảng (+/- dòng) và danh sách chi tiết bản ghi thay đổi bằng Tiếng Việt.
   * Xây dựng chức năng **Xuất bản sao lưu ra file** (`.dkhp-backup.zip`) chứa `manifest.json` có chữ ký `QuanLyDKHP-Backup-v1` và mã kiểm tra tính toàn vẹn `SHA-256` của từng bảng.
   * Xây dựng chức năng **Nhập file Backup & Ghi đè CSDL** kèm kiểm tra chữ ký nghiêm ngặt và tạo điểm lưu `[Pre-Import]` an toàn trước khi ghi đè.
4. **Chẩn đoán & Xử lý dứt điểm lỗi treo ứng dụng khi thoát:**
   * Xác định nguyên nhân gốc rễ gây lỗi treo: Async-over-Sync Deadlock do gọi `.GetAwaiter().GetResult()` trên luồng UI của Avalonia tại `desktop.ShutdownRequested`, kết hợp với thuộc tính `Cache=Shared` gây khóa chéo giữa các kết nối SQLite.
   * Khắc phục bằng cách điều phối tác vụ sao lưu sang `Task.Run` trên ThreadPool với `Wait(TimeSpan.FromSeconds(2))` và chuyển SQLite sang Private Cache chuẩn (`Data Source={sqlitePath}`).
5. **Đo lường dữ liệu thực tế & Hoàn thiện tài liệu:**
   * Đo lường trực tiếp trên máy trạm: **6.661 bản ghi nghiệp vụ**, CSDL SQLite chiếm **1,85 MB**, kho lịch sử Git chiếm **352 KB** (nén ~6,5x), tổng dung lượng ứng dụng trên đĩa: **8,7 MB**.
   * Bổ sung tài liệu chuyên sâu `F11b-co-che-git-backup-chi-tiet.md`, cập nhật `F11`, `F10`.
   * Viết thêm unit tests trong `BackupServiceTests.cs`, nâng tổng số test lên **53/53 tests passed (100%)**.

---

## 2. Nhật Ký Ngày 08/11/2026

* **Thực hiện:** NGUYÊN
* **Nội dung trọng tâm:** Nâng cấp toàn diện UI/UX phân hệ Học kỳ & Học phí đồng bộ tab Sinh viên; Thiết lập Docusaurus cho GitHub Pages.

### Các kết quả đạt được
1. **Nâng cấp UI/UX Học kỳ & Lớp học phần (`HocKyLopHocPhanView`):**
   * Đồng bộ ngôn ngữ thiết kế: Segmented bar, badge đếm số lượng, ContextMenu click chuột phải, thanh lọc nhanh môn học.
   * Thêm cột hiển thị sĩ số kèm ProgressBar trực quan hóa tỷ lệ lấp đầy lớp.
2. **Nâng cấp UI/UX Học phí (`HocPhiView`):**
   * Bố cục thẻ số liệu thống kê tổng học phí đã thu/còn nợ, bảng lịch sử nộp tiền, thanh lọc sinh viên nợ học phí.
3. **Tái cấu trúc tài liệu & Triển khai Docusaurus:**
   * Tái cấu trúc toàn bộ cây thư mục tài liệu thành chuẩn Docusaurus (`01-dac-ta-ung-dung`, `02-huong-dan-cai-dat`, `03-nhat-ky-va-minh-chung`).
   * Cấu hình Docusaurus sẵn sàng xuất bản lên GitHub Pages.

---

## 3. Nhật Ký Ngày 07/10/2026

* **Thực hiện:** NGUYÊN
* **Nội dung trọng tâm:** Tối ưu hóa tính học phí & Triển khai kiến trúc Hybrid Database SQLite

### Các kết quả đạt được
1. **Kiến trúc Hybrid SQLite Database (Chạy song song 2 luồng):**
   * Xây dựng tầng CSDL SQLite cục bộ chạy song song với Neon PostgreSQL.
   * Xây dựng luồng đồng bộ vi sai (Delta Sync) dựa trên nhãn thời gian `NgayCapNhat` (UTC), giúp ứng dụng có thể mở tức thì và đọc dữ liệu ngay cả khi mạng Internet gián đoạn.
   * Viết tài liệu đặc tả kỹ thuật chi tiết tại `docs/01-features/F06-local-database-cache-sync.md`.
2. **Tối ưu hóa tốc độ Phân hệ Học phí:**
   * Viết lại thuật toán `TinhLaiHocPhiAsync` theo cơ chế xử lý lô (Batch Processing).
   * Thay thế các câu truy vấn nạp lặp đơn lẻ bằng một câu truy vấn tổng hợp duy nhất, rút ngắn thời gian tính lại học phí toàn học kỳ từ 45 giây xuống **dưới 1 giây**.

---

## 4. Nhật Ký Ngày 04/10/2026

* **Thực hiện:** NGUYÊN
* **Nội dung trọng tâm:** Chuẩn hóa Vector Icons, In-Memory Caching & Tối ưu hiệu năng UI

### Các kết quả đạt được
1. **Chuẩn hóa Giao diện Icon với `Material.Icons.Avalonia`:**
   * Gỡ bỏ toàn bộ emoji văn bản khỏi hệ thống menu điều hướng.
   * Cấu hình nạp vector icon toàn cục trong `App.axaml`, chuyển đổi toàn bộ danh mục sang enum chuẩn `MaterialIconKind`.
2. **Triển khai Tải ngầm In-Memory Cache (F05):**
   * Định nghĩa `IMemoryCacheStore` tại tầng Core và triển khai `MemoryCacheStore` (Singleton) tại tầng App.
   * Nạp song song bằng `Task.WhenAll` các bảng danh mục (Học kỳ, Lớp, Khóa học) ngay sau khi đăng nhập thành công.
   * Triển khai bộ nhớ đệm giữ vết ViewModel (`_viewModelCache`) trong `MainWindowViewModel`, chuyển tab với độ trễ 0ms.
3. **Tối ưu Hiệu năng Tìm kiếm & Hiển thị:**
   * Áp dụng In-Memory Filtering trên RAM kết hợp bộ đếm Debounce 300ms cho các ô tìm kiếm.
   * Kích hoạt UI Virtualization trên DataGrid để cuộn mượt mà hàng vạn dòng mà không tràn RAM.
   * Triển khai bộ so sánh tên tiếng Việt `VietnameseNameComparer` sắp xếp chuẩn xác: Tên -> Tên lót -> Họ.
4. **Liên kết Module Sinh viên và Đăng ký học phần:**
   * Sử dụng `WeakReferenceMessenger` để hỗ trợ Double-click từ dòng sinh viên chuyển thẳng sang tab ĐKHP với sinh viên đã chọn sẵn.
5. **Sửa lỗi nghiêm trọng:** Khắc phục lỗi `StackOverflowException` phát sinh do vòng lặp Binding Loop giữa SelectedItem và SelectedIndex trên Avalonia ItemsControl.

---

## 5. Nhật Ký Ngày 28/09/2026

* **Thực hiện:** LIAM (ĐẠT)
* **Nội dung trọng tâm:** Module 2 App Shell, Theme Liquid Glass & Sửa lỗi đa luồng EF Core

### Các kết quả đạt được
1. **Hoàn thiện Module 2 (App Shell & Navigation):**
   * Xây dựng Sidebar co giãn linh hoạt với dải màu trượt bám theo tab đang chọn.
   * Thiết kế Top Bar tích hợp tìm kiếm nhanh, chuông thông báo, hồ sơ cá nhân và nút chuyển đổi Dark/Light mode tức thời.
   * Chuyển đổi toàn diện giao diện sang ngôn ngữ thiết kế "Liquid Glass" (kính mờ, góc bo tròn mềm, bóng đổ nhẹ).
2. **Sửa lỗi xung đột đa luồng EF Core (`IDbContextFactory`):**
   * Phát hiện nguyên nhân gây lỗi: `A second operation was started on this context instance before a previous operation completed`.
   * Tái cấu trúc toàn bộ 10 Repositories chuyển sang tiếp nhận `IDbContextFactory<AppDbContext>`, mỗi thao tác tự tạo và giải phóng context riêng biệt, triệt tiêu hoàn toàn lỗi xung đột luồng.
3. **Khởi tạo tài liệu sửa lỗi `FIX.md`:** Tổng hợp danh mục các lỗi kỹ thuật và phương án xử lý để toàn đội tham chiếu.

---

## 6. Nhật Ký Ngày 20/09/2026 Đến 24/09/2026

* **Thực hiện:** NGÔ QUYỀN LINH, NGUYÊN, LIAM
* **Nội dung trọng tâm:** Hoàn thiện đặc tả phân hệ và cấu hình cơ sở dữ liệu

### Các kết quả đạt được
* Soạn thảo và hoàn thiện tài liệu đặc tả chức năng từ Spec 011 đến Spec 016 (Học phí, Báo cáo, Cấu hình, Người dùng, Import/Export).
* Viết tài liệu hướng dẫn cấu hình cơ sở dữ liệu PostgreSQL cục bộ qua Docker và tài khoản đám mây Neon.
* Sửa lỗi giao diện xem trước phiếu kết quả ĐKHP (Tab 4 Báo cáo), bổ sung thông tin trường chủ quản: *TRƯỜNG ĐẠI HỌC ĐÀ LẠT*.

---

## 7. Nhật Ký Ngày 18/09/2026

* **Thực hiện:** NGUYÊN
* **Nội dung trọng tâm:** Khởi tạo Solution Clean Architecture & Triển khai 10 Spec đầu tiên

### Các kết quả đạt được
* Khởi tạo Solution .NET 8 gồm 5 projects (`Core`, `Infrastructure`, `Services`, `App`, `Tests`).
* Cài đặt gói thư viện phụ thuộc, cố định phiên bản 8.x để tương thích .NET 8.
* Khởi tạo CSDL với Fluent API, cấu hình các trường Audit (`IAuditableEntity`) và Xóa mềm (`ISoftDeleteEntity`), sinh migration đầu tiên `InitialCreate`.
* Thiết lập ma trận phân quyền `PermissionMatrix` cho 4 vai trò.
* Triển khai hoàn tất quy trình kiểm tra 6 bước đăng ký học phần (`DangKyHocPhanService`) và viết 19 Unit Tests đầu tiên đạt 100% Passed.

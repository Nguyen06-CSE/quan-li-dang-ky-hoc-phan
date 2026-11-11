---
id: nhat-ky-cong-viec-daily
title: Nhật Ký Công Việc Hằng Ngày (Daily Logs)
sidebar_position: 2
---

# Nhật Ký Công Việc Hằng Ngày (Daily Logs)

## 0. Nhật Ký Ngày 12/11/2026: Nâng Cấp Desktop Ứng Dụng Thực Tế (Upgrade 1)

* **Thực hiện:** LIAM (LÂM) & NGUYÊN
* **Nội dung trọng tâm:** Hiện thực hóa nâng cấp thực tế cho Giảng viên / Giáo viên bộ môn theo Đề xuất 1 (Smart Import Studio & Clipboard Paste, Auto Mapping, Inline Editing) và Đề xuất 3 (Class Insights Dashboard, Sức chứa & Trợ lý Xung Đột Thời Khóa Biểu). Tạm thời hoãn Đề xuất 2 theo yêu cầu.

### Các kết quả đạt được
1. **Kiến trúc Smart Import Studio & Clipboard Parser (`ISmartImportService`):**
   * **Dán trực tiếp từ bộ nhớ tạm (`Ctrl + V`):** Giảng viên chỉ cần copy danh sách sinh viên từ Excel, Google Sheets hoặc tin nhắn Zalo, nhấn *"Dán từ Clipboard"*, hệ thống tự động bóc tách các dòng và các ô dựa trên ký tự ngăn cách (Tab `\t`, phẩy `,`, chấm phẩy `;` hoặc xuống dòng).
   - **Tự động nhận diện cột (Auto Column Mapping):** Tự động phát hiện header hoặc dữ liệu thô (MSSV, Họ tên, Lớp sinh hoạt, Khóa học). Nếu có cột tách rời *Họ đệm* và *Tên*, tự động nối thành *Họ và tên* hoàn chỉnh. Tự động suy luận Khóa học từ tiền tố MSSV (ví dụ: `21...` $\rightarrow$ `K21` hoặc `K45`).
2. **Trợ lý Phân tích Sức chứa & Xung đột Lớp học (Class Insights - Đề xuất 3):**
   * **Đo lường sức chứa thời gian thực:** Hiển thị sĩ số hiện tại, số lượng nạp thêm, sĩ số dự kiến và thanh phần trăm lấp đầy lớp; tự động cảnh báo màu đỏ nếu vượt quá `SiSoToiDa`.
   * **Phát hiện trùng lịch học chéo thời gian thực:** Đối chiếu với toàn bộ thời khóa biểu các lớp khác của sinh viên trong cùng kỳ học; phát hiện chính xác các ca học trùng `Thu` và giao nhau về khoảng tiết `[TietBatDau, TietBatDau + SoTiet - 1]`.
   * **Phát hiện trùng môn:** Cảnh báo các sinh viên đã có lịch học ở một lớp học phần khác của cùng môn học này.
   * **Thống kê cơ cấu lớp (Demographics):** Phân tích cơ cấu sinh viên theo Lớp sinh hoạt (`CTK45A`, `CTK45B`...) và Khóa học.
3. **Bảng Xem Trước Hỗ Trợ Chỉnh Sửa Trực Tiếp (Inline Editing DataGrid):**
   * Cho phép giảng viên nhấp vào bảng DataGrid sửa trực tiếp `Mã Sinh Viên`, `Họ và Tên`, `Lớp Sinh Hoạt`, `Khóa Học` ngay tại chỗ mà không cần mở lại Excel.
   * Nút *"Kiểm chứng lại"* cho phép cập nhật lại toàn bộ cảnh báo và chỉ số ngay sau khi người dùng sửa lỗi.
4. **Giao Dịch Ghi Nhận Nguyên Tử & Tính Lại Học Phí Tự Động:**
   * Tự động khởi tạo hồ sơ `SinhVien` trong hệ thống nếu mã sinh viên chưa tồn tại; tự động phục hồi các bản ghi đã xóa mềm.
   * Ghi nhận `DangKyHocPhan` với trạng thái `DangHoc`, tự động kích hoạt `TinhLaiHocPhiAsync` cho toàn bộ sinh viên được nạp.
   * Phát sóng `DataImportedNotificationMessage` làm tươi tức thời danh sách ở các màn hình khác mà không cần khởi động lại.
   * Bổ sung nút phím tắt *"Nạp sinh viên vào lớp (Smart Import)"* trực tiếp trên mỗi dòng của bảng danh sách lớp học phần tại màn hình *Học kỳ / LHP*.
5. **Kiểm Thử Đạt 100% (81/81 Tests Passed):**
   * Bổ sung bộ kiểm thử `SmartImportServiceTests.cs` (8 bài test mới) bao phủ: Parsing TSV, Ghép Họ đệm + Tên, Suy luận khóa học, Tính toán sức chứa, Phát hiện trùng lịch & trùng môn, Giao dịch nạp sinh viên nguyên tử và chặn học kỳ đã đóng.

---

## 1. Nhật Ký Ngày 11/11/2026

* **Thực hiện:** LIAM (LÂM) & NGUYÊN
* **Nội dung trọng tâm:** Tinh chỉnh & Xử lý dứt điểm 5 Nhóm lỗi cốt lõi (Nhóm 1: Import/Export Excel 21 cột, Nhóm 2: Nghiệp vụ ĐKHP & Tính Học phí, Nhóm 3: Quản lý Danh mục & Soft-delete Reactivation, Nhóm 4: Báo cáo Thống kê & Xuất Ấn phẩm, Nhóm 5: Hạ tầng Dữ liệu, Cache & Đồng bộ An toàn).

### Các kết quả đạt được
1. **Nhóm 1: Xử lý Nhập/Xuất Excel 21 cột & Tự động Phân giải Học kỳ đích (F09):**
   * **Bỏ điều kiện bắt buộc chọn học kỳ:** Người dùng chỉ cần kéo thả tệp Excel (chuẩn 21 cột của trường ĐH Đà Lạt) vào là ứng dụng bắt đầu đọc và nhập dữ liệu ngay mà không bắt buộc phải chọn học kỳ trước.
   * **Tự động nhận diện học kỳ từ dữ liệu:** Bóc tách tiền tố từ Mã LHP (ví dụ `26120CT4201D02` -> tiền tố `261` -> Học kỳ `HK261`). Tự động khởi tạo học kỳ mới nếu chưa tồn tại trong hệ thống.
   * **Đồng bộ hóa tức thì:** Sau khi nạp Excel thành công, dữ liệu được ghi nhận vào cả Neon PostgreSQL và SQLite Local, đồng thời phát tín hiệu làm mới giao diện 0ms sang toàn bộ các tab Sinh viên, Môn học, Lớp học phần và Học phí mà không cần khởi động lại ứng dụng.
2. **Nhóm 2: Nghiệp vụ Đăng ký học phần & Tính học phí (F05 & F06):**
   * **Chốt chặn Học kỳ mở (Step 0):** Bổ sung bước kiểm tra cờ `HocKy.DangMo` trước khi thực hiện đăng ký hoặc hủy môn. Chặn tuyệt đối việc chỉnh sửa dữ liệu của kỳ học đã kết thúc.
   * **Giao dịch nguyên tử chống Race Condition (`LuuDangKyAtomicAsync`):** Kết hợp `SemaphoreSlim` nội bộ và giao dịch CSDL `BeginTransactionAsync`. Kiểm tra sĩ số thực tế tức thời ngay trong transaction; nếu đã đủ chỗ thì tự động rollback và trả về thông báo lỗi, ngăn chặn hoàn toàn tình trạng vượt sĩ số tối đa.
   * **Đồng nhất công thức tính học phí & Fallback kỳ đã đóng:** Đồng bộ công thức tính giữa bảng tổng hợp và modal chi tiết. Nếu học kỳ đã đóng mà chưa khóa sổ snapshot, hệ thống tự động fallback tính toán trực tiếp từ bảng đăng ký học phần, khắc phục triệt để lỗi hiển thị 0đ.
   * **Tích hợp tính năng Nộp tiền học phí:** Bổ sung phương thức `GhiNhanNopTienAsync` tự động phân bổ tiền nộp cho các môn còn nợ; tích hợp hộp thoại nhập số tiền nộp trực tiếp tại giao diện.
   * **Trau chuốt trải nghiệm người dùng (UX/UI):**
     * Mở rộng cột *Họ và tên* trong bảng học phí (`Width="2*" MinWidth="220"`) và bổ sung `ToolTip`, hiển thị đầy đủ 100% họ tên tiếng Việt mà không bị cắt ngắn.
     * Tách Header chi tiết sinh viên tại màn hình ĐKHP thành bố cục 2 tầng độc lập, giải quyết dứt điểm hiện tượng thông tin sinh viên đè lên thanh tiến độ tín chỉ.
     * Tái cấu trúc bảng môn học đã đăng ký: khôi phục hiển thị cột *Tên môn học*, bổ sung ToolTip và thanh cuộn ngang tự động.
3. **Nhóm 3: Quản lý Danh mục Môn học, Lớp HP, Sinh viên & Tài khoản (F02, F03, F04, F01):**
   * **Tái tạo mã sau khi Xóa mềm (Soft-Delete Reactivation):** Trên cả 4 bảng (`SinhVien`, `MonHoc`, `LopHocPhan`, `NguoiDung`), khi người dùng tạo mới một bản ghi trùng mã với bản ghi đã xóa mềm, hệ thống tự động phục hồi bản ghi (`IsDeleted = false`), cập nhật thông tin mới nhất và thời gian sửa đổi, loại bỏ triệt để lỗi crash `UNIQUE constraint failed`.
   * **Thống nhất định danh Giảng viên (`MaGV` vs `TenDangNhap`):** Tự động gán `MaGV = TenDangNhap` nếu bỏ trống khi tạo giảng viên; kiểm tra trùng mã giảng viên qua `GetByMaGVAsync`; từ điển giảng viên `gvDict` nạp cả 2 khóa giúp hiển thị chính xác tên giảng viên trên toàn bộ hệ thống.
   * **Bảo toàn trạng thái Khóa tài khoản:** `NguoiDungViewModel` lưu giữ `EditTrangThai` và `NguoiDungService.CapNhatAsync` bảo toàn trạng thái khóa, ngăn chặn việc vô tình mở khóa tài khoản khi admin chỉ sửa họ tên hoặc vai trò.
   * **Bảo vệ toàn vẹn Lớp học phần:** Chặn tuyệt đối việc đổi mã môn học (`MaMon`) khi LHP đã có sinh viên đăng ký; chặn hạ sĩ số tối đa xuống dưới số lượng sinh viên đang theo học; vô hiệu hóa ComboBox Môn học trên giao diện khi lớp đã có người đăng ký.
4. **Nhóm 4: Báo Cáo, Thống Kê & Xuất Ấn Phẩm (F08, ADR-21, ERR-13):**
   * **Sửa lỗi GroupBy Object Reference trên `AsNoTracking()`:** Chuyển đổi từ `GroupBy(l => l.MonHoc)` sang `GroupBy(l => l.MaMon)` trong `ThongKeSoSvTheoMonAsync`, loại bỏ triệt để nguy cơ phân mảnh nhóm trùng môn hoặc phát sinh `NullReferenceException` do EF Core không quản lý Identity Map khi dùng `AsNoTracking()`.
   * **Đồng bộ bộ lọc `DangHoc` & Xóa mềm trên toàn bộ 4 Tabs:**
     * Bổ sung điều kiện `dk.TrangThai == "DangHoc"` và `!sv.IsDeleted && !lhp.IsDeleted` trên `LayDsSvTheoMonAsync` và `GetDsSinhVienTheoMonLocalAsync`. Loại bỏ sinh viên đã hủy môn hoặc đã bị xóa mềm khỏi danh sách học phần.
     * Bổ sung `!lhp.IsDeleted && !sv.IsDeleted` trên `LayDsThiAsync`, ngăn sinh viên và lớp học phần đã bị xóa xuất hiện trong danh sách thi kết thúc học phần.
     * Đồng bộ số liệu đếm sĩ số giữa Tab 1 (Danh sách theo môn) và Tab 3 (Thống kê môn học).
   * **Nạp họ tên giảng viên lên toàn bộ báo cáo và ấn phẩm (Teacher Enrichment):**
     * Truy vấn từ điển `NguoiDung` (vai trò `GiangVien`) theo cả `MaGV` và `TenDangNhap`, nạp họ tên đầy đủ vào `SinhVienTheoMonDto`, `DanhSachThiDto` và `PhieuDangKyMonDto`.
     * Hiển thị họ tên giảng viên trên DataGrid Tab 1 & Tab 2, xuất cột *Giảng viên* sang tệp Excel.
     * Nạp họ tên cán bộ giảng dạy lên phần tiêu đề của Phiếu danh sách thi PDF và cột *Giảng viên* trên Phiếu kết quả ĐKHP PDF khổ A4.
   * **Kiểm thử tự động:** Viết bộ test `tests/QuanLyDKHP.Tests/BaoCaoRepositoryTests.cs` (4/4 passed) kiểm chứng toàn diện logic gom nhóm, lọc xóa mềm và nạp tên giảng viên trên CSDL in-memory SQLite. Toàn bộ 73 bài kiểm tra của giải pháp đạt 100% Passed.
5. **Nhóm 5: Hạ Tầng Dữ Liệu, Cache & Đồng Bộ An Toàn (F11, ADR-22, ERR-14):**
   * **Triệt tiêu hoàn toàn `EnsureDeletedAsync()`:** Loại bỏ lệnh hủy CSDL nguy hiểm khỏi `SyncService.RefreshLocalStoreAsync`. Thay vì xóa tệp vật lý gây mất dữ liệu offline và khóa file SQLite WAL, hệ thống dọn dẹp các bảng nghiệp vụ bên trong transaction CSDL an toàn.
   * **Bảo toàn và đồng bộ tài khoản người dùng (`NguoiDung`):** Thêm `NguoiDung` vào `MasterTables`, hỗ trợ cả đồng bộ đầy đủ và đồng bộ vi sai (Delta Sync), đảm bảo tài khoản Admin và Giảng viên không bao giờ bị xóa mất khi chuyển đổi chế độ offline.
   * **Khắc phục lỗi mốc thời gian Delta Sync:** Thiết lập mốc thời gian khởi điểm `new DateTime(2000, 1, 1, DateTimeKind.Utc)`, loại bỏ triệt để lỗi tràn số và xung đột `DateTimeKind.Unspecified` khi Npgsql PostgreSQL so sánh thời gian với SQLite.

---

## 2. Nhật Ký Ngày 09/11/2026

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

* **Thực hiện:** LIAM (LÂM)
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

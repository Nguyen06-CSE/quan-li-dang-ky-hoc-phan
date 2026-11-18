---
id: F11-local-sqlite-git-backup
title: F11 - CSDL SQLite Cục Bộ Chính & Sao Lưu Phiên Bản Kiểu Git
sidebar_position: 11
---

# F11 - CSDL SQLite Cục Bộ Chính (Local-First DB) & Hệ Thống Sao Lưu Đa Tầng Kiểu Git

## 1. Bối Cảnh Nghiệp Vụ & Yêu Cầu Bảo Mật Tại Văn Phòng

Sau khi kiểm tra tiến trình dự án và rà soát yêu cầu vận hành thực tế, bài toán đặt ra các ràng buộc quan trọng về bảo mật và môi trường làm việc:
* **Làm việc cố định tại văn phòng:** Cán bộ nội vụ thực hiện các công tác quản lý sinh viên, mở lớp học phần, đăng ký học phần và quyết toán học phí thông qua ứng dụng Desktop **chỉ trên máy tính cố định tại văn phòng**.
* **Độc lập đường truyền & Bảo mật nội bộ:** Cơ sở dữ liệu chính (Primary Database) phải được vận hành trực tiếp tại máy trạm cố định để cô lập bề mặt tấn công qua mạng Internet, đồng thời đảm bảo tốc độ đọc/ghi tức thì ngay cả khi văn phòng mất kết nối mạng.
* **Yêu cầu sao lưu & Khôi phục theo mốc thời gian:** Dữ liệu tại máy local cần một cơ chế **Backup tự động hằng ngày** có khả năng tối ưu hóa dung lượng và kiểm soát lịch sử phiên bản chặt chẽ **tương tự như cách Git quản lý mã nguồn**, kết hợp với phương án đồng bộ dự phòng lên một **DB Host** (Server văn phòng hoặc dịch vụ Host).

---

## 2. Đánh Giá Kỹ Thuật: Tại Sao Chọn SQLite (WAL Mode) Làm CSDL Chính?

### 2.1. Phân tích quy mô dữ liệu thực tế của dự án
Hệ thống quản lý 8 thực thể cốt lõi (`NguoiDung`, `SinhVien`, `MonHoc`, `HocKy`, `LopHocPhan`, `DangKyHocPhan`, `CauHinhHeThong`, `HocPhiHocKy`):
* **Số liệu đo lường thực tế trên thiết bị máy trạm hiện tại:**
  * Tổng cộng **6.661 bản ghi nghiệp vụ** (bao gồm 753 sinh viên, 84 môn học, 188 lớp học phần, 5.631 đăng ký học phần, 3 học kỳ, 4 tham số cấu hình, 1 tài khoản).
  * Dung lượng tệp SQLite CSDL chính (`quanlydkhp.db`): chỉ **1,85 MB**.
  * Dung lượng toàn bộ kho lịch sử Git nén vi sai (`.git/objects`): chỉ **352 KB**.
  * Tổng dung lượng lưu trữ toàn bộ ứng dụng trên ổ đĩa: **8,7 MB** (bao gồm cả bản sao lưu nhị phân nóng `latest_snapshot.db` 1,8 MB và tệp dữ liệu phẳng JSONL 2,3 MB).
* Với quy mô mở rộng trong tương lai từ **5.000 – 20.000 sinh viên**, **500 – 1.000 môn học** và **100.000 – 500.000 bản ghi đăng ký học phần** tích lũy qua nhiều năm học, tổng dung lượng cơ sở dữ liệu chỉ dao động trong khoảng **20 MB – 150 MB**, hoàn toàn nằm gọn trong khả năng xử lý tức thì của SQLite.
* Toàn bộ tầng truy xuất dữ liệu (`Repositories`) được xây dựng bằng **EF Core LINQ chuẩn**, không phụ thuộc vào Stored Procedure đặc thù của hệ quản trị CSDL rời.
* Mô hình vận hành trên máy tính cố định tại văn phòng là mô hình **Single-Workstation / Single-Writer** (một điểm ghi duy nhất tại một thời điểm).

### 2.2. Bảng đối chiếu lựa chọn hệ CSDL Local

| Tiêu chí đánh giá | SQLite Local (Chế độ WAL) *(Được chọn)* | PostgreSQL cài đặt Localhost | SQL Server LocalDB / LiteDB |
|---|---|---|---|
| **Chi phí cài đặt & Vận hành** | **Zero-Configuration:** Gọn trong 1 tệp `.db` duy nhất, không cần cài đặt hay chạy dịch vụ ngầm (Background Service). | Phải cài đặt PostgreSQL Server trên máy văn phòng, quản trị cổng 5432, dễ dừng dịch vụ khi cập nhật OS. | LocalDB chỉ hỗ trợ Windows; LiteDB là NoSQL không tương thích EF Core Relational. |
| **Tốc độ Đọc & Ghi (CRUD / Import)** | **0.1ms – 2ms:** Truy xuất trực tiếp qua lời gọi hệ thống tệp (File I/O) trong cùng tiến trình ứng dụng, không tốn chi phí giao thức TCP/IP. | **1ms – 5ms:** Phải đi qua giao thức mạng nội bộ `127.0.0.1` và tiến trình Server riêng. | Tương đương. |
| **Độ bền vững khi mất điện (ACID)** | Đạt chuẩn ACID tuyệt đối khi bật `PRAGMA journal_mode = WAL;` và `PRAGMA synchronous = NORMAL;`. | Đạt chuẩn ACID. | Đạt chuẩn ACID. |
| **Khả năng Backup kiểu Git & Snapshot** | **Rất cao:** Hỗ trợ Online Backup API (`BackupDatabase`) và dễ dàng kết xuất ra định dạng văn bản chuẩn hóa theo dòng để nén vi sai bằng Git. | Phức tạp hơn, phụ thuộc công cụ dòng lệnh ngoài (`pg_dump` / `pg_restore`). | Khó tích hợp vào kho lưu trữ Git nội bộ. |

### 2.3. Kiến trúc CSDL Local hợp nhất & Tối ưu hóa WAL
1. **Hợp nhất Ngữ cảnh Dữ liệu (`Unified SQLite Primary DB`):**
   * Toàn bộ 9 Repositories nghiệp vụ (`SinhVienRepository`, `DangKyHocPhanRepository`, `ImportExcelRepository`, ...) và `LocalReadService` cùng làm việc trực tiếp trên một cơ sở dữ liệu SQLite chính duy nhất (`quanlydkhp.db`).
   * Duy trì tầng **`MemoryCacheStore` (RAM - 0ms)** kế thừa từ [F10](./F10-hybrid-cache-delta-sync.md) cho các danh mục tĩnh (`HocKy`, `MonHoc`, `LopSinhHoat`, `KhoaHoc`).
2. **Kích hoạt chế độ Write-Ahead Logging (`WAL`):**
   * Ngay khi khởi tạo kết nối, hệ thống thiết lập:
     ```sql
     PRAGMA journal_mode = WAL;
     PRAGMA synchronous = NORMAL;
     PRAGMA foreign_keys = ON;
     ```
   * Chế độ `WAL` cho phép các luồng đọc giao diện và luồng ghi (như Import Excel hàng loạt 500 dòng/lô) hoạt động không khóa lẫn nhau, đồng thời bảo vệ tệp CSDL không bị hỏng (corrupt) nếu máy tính văn phòng mất điện đột ngột.

:::tip[Khả năng mở rộng bảo mật cấp độ tệp với SQLCipher (AES-256)]
Trong giai đoạn hiện tại, tệp `quanlydkhp.db` được bảo vệ bởi cơ chế phân quyền tài khoản hệ điều hành trên máy tính cố định tại văn phòng và mật khẩu băm `BCrypt` ở tầng ứng dụng.

Khi văn phòng có nhu cầu nâng cấp lên mức **mã hóa cứng toàn bộ tệp cơ sở dữ liệu trên ổ đĩa (Encryption-at-Rest)** để chống sao chép trái phép tệp `.db` qua USB, kiến trúc EF Core SQLite hiện tại cho phép tích hợp ngay **SQLCipher (Mã hóa AES-256)** chỉ bằng cách thay gói `SQLitePCLRaw.bundle_e_sqlcipher` và truyền khóa `Password=<OfficeSecretKey>` vào chuỗi kết nối mà **không phải sửa đổi bất kỳ dòng code Repository hay câu truy vấn LINQ nào**.
:::

---

## 3. Kiến Trúc Hệ Thống Sao Lưu Đa Tầng (Lựa Chọn C: Git-Style Local + DB Host)

Để giải quyết triệt để bài toán an toàn dữ liệu trên máy cố định, hệ thống triển khai **chiến lược sao lưu kép (Hybrid 2-Tier Backup)** kết hợp cả 2 phương án:
* **Tầng Backup 1 (Cốt lõi - Chạy tự động mỗi ngày trên máy Local):** Sao lưu quản lý phiên bản tối ưu kiểu Git (`Git-Style Versioned Local Backup`) sử dụng `LibGit2Sharp`.
* **Tầng Backup 2 (Tùy chọn - DB Host Replica):** Khi người dùng chủ động bấm nút, hệ thống đẩy toàn bộ 8 bảng từ SQLite Local lên một PostgreSQL DB Host (Server nội bộ văn phòng hoặc dịch vụ Cloud Host). Không có seed, pull hay đồng bộ tự động từ DB Host về máy local.

```mermaid
flowchart TD
    subgraph Workstation["Máy Tính Cố Định Văn Phòng (Ứng Dụng Desktop)"]
        UI["Giao Diện Người Dùng & Màn Hình Cấu Hình"]
        RAM["Tầng 1: MemoryCacheStore (RAM - 0ms)"]
        PrimaryDB[("CSDL Chính: SQLite Local (quanlydkhp.db)\nChế độ WAL + Foreign Keys ON")]
        Exporter["Bộ Xuất Chuẩn Hóa Dòng\n(Canonical Sorted JSONL Exporter)"]
        GitEngine["Động Cơ Git Nhúng\n(LibGit2Sharp Engine)"]
    end

    subgraph LocalBackup["Tầng Sao Lưu 1: Kho Git Cục Bộ (.db-backup-git)"]
        WorkingTree["Tệp dữ liệu phẳng theo bảng:\n- SinhVien.jsonl (ORDER BY MaSV)\n- DangKyHocPhan.jsonl (ORDER BY Id)\n- HocPhiHocKy.jsonl..."]
        Packfiles[("Git Object Database (.git/objects)\nLưu trữ Delta nén Zlib theo từng ngày")]
        HotSnap[("Bản Chụp Nhị Phân Nóng\n(latest_snapshot.db)")]
    end

    subgraph RemoteBackup["Tầng Sao Lưu 2: DB Host (Server Văn Phòng / Cloud)"]
        PostgresHost[("PostgreSQL Backup Host\n(One-Way Push Delta Replica)")]
    end

    UI <-->|Đọc/Ghi nghiệp vụ: 0.1ms - 2ms| PrimaryDB
    PrimaryDB -->|Nạp danh mục tĩnh| RAM
    PrimaryDB -->|1. Cuối ngày / Đóng App / Thủ công| Exporter
    Exporter -->|2. Ghi tệp văn bản theo dòng cố định| WorkingTree
    WorkingTree -->|3. Stage & Commit tự động| GitEngine
    GitEngine -->|4. Nén vi sai (Chỉ lưu dòng thay đổi)| Packfiles
    Packfiles -.->|Khôi phục theo mốc thời gian\n(Time-Travel Restore)| PrimaryDB
    PrimaryDB -->|SqliteConnection.BackupDatabase| HotSnap
    PrimaryDB -.->|5. Chỉ khi bấm nút: đẩy toàn bộ 8 bảng| PostgresHost
```

---

### 3.1. Tầng Sao Lưu 1: Cơ Chế Backup Cục Bộ Tối Ưu Kiểu Git (`LibGit2Sharp`)

#### Vấn đề của phương pháp sao chép tệp `.db` truyền thống
Tệp SQLite (`.db`) là tệp nhị phân có cấu trúc B-Tree theo trang (4KB/page). Nếu mỗi ngày chương trình sao chép nguyên một tệp `backup_YYYY-MM-DD.db` (ví dụ 50MB/ngày):
1. Sau 1 năm sẽ tiêu tốn hàng chục GB dung lượng ổ cứng (`50MB × 365 = ~18.2 GB`) dù mỗi ngày chỉ thay đổi vài chục dòng dữ liệu.
2. Không thể tra cứu tường minh ngày hôm đó những bảng nào, bản ghi nào đã được thêm mới hay chỉnh sửa.

#### Giải pháp "Git-Native Database Backup" của dự án
Để Git phát huy tối đa thuật toán **nén vi sai theo dòng (Line-based Delta Compression)**, hệ thống chuyển đổi trạng thái CSDL sang biểu diễn văn bản chuẩn hóa (**Canonical Line-Oriented JSONL**) trước khi đưa vào Git:

1. **Xuất dữ liệu chuẩn hóa (`Canonical Sorted JSONL`):**
   * Toàn bộ 8 bảng trong SQLite được xuất ra 8 tệp `.jsonl` tương ứng trong thư mục làm việc của kho Git (`%LocalAppData%/QuanLyDKHP/Backups/GitRepo/tables/`).
   * Mỗi bản ghi (row) được tuần tự hóa thành **đúng 1 dòng văn bản JSON duy nhất** với thứ tự trường cố định, và toàn bộ bảng được sắp xếp nghiêm ngặt theo khóa chính (`ORDER BY PrimaryKey`):
     * `SinhVien.jsonl` $\rightarrow$ `ORDER BY MaSV`
     * `MonHoc.jsonl` $\rightarrow$ `ORDER BY MaMon`
     * `LopHocPhan.jsonl` $\rightarrow$ `ORDER BY MaLHP`
     * `DangKyHocPhan.jsonl` $\rightarrow$ `ORDER BY MaSV, MaLHP, Id`
   * Nhờ thứ tự dòng bất biến này, nếu trong ngày làm việc cán bộ nội vụ chỉ sửa điểm của 3 sinh viên và thêm 15 đăng ký học phần trên tổng số 100.000 dòng, **chỉ đúng 18 dòng văn bản trong tệp `.jsonl` thay đổi**.
2. **Đóng gói Commit tự động bằng `LibGit2Sharp`:**
   * Ứng dụng nhúng trực tiếp thư viện `LibGit2Sharp` (thuần .NET, máy tính văn phòng không cần cài đặt Git CLI bên ngoài).
   * Trước khi tạo Commit, hệ thống chạy `repo.RetrieveStatus()`:
     * Nếu trong ngày **không có bất kỳ thay đổi dữ liệu nào** so với bản Commit gần nhất (`!status.IsDirty`), hệ thống tự động bỏ qua để không sinh ra các bản lưu trùng lặp.
     * Nếu có thay đổi, hệ thống phân tích `Diff` trên từng tệp `.jsonl` để tự động tạo **Commit Message giàu ngữ cảnh**:
       ```text
       [Auto-Daily 2026-11-08 17:05] Thay đổi: SinhVien (+2), DangKyHocPhan (+15, -3)
       ```
   * Thuật toán nén đối tượng của Git (`Zlib` + `Packfile Delta`) chỉ lưu trữ phần chênh lệch (tốn vài KB cho mỗi ngày làm việc thay vì hàng chục MB).
3. **Kết hợp Bản chụp nhị phân nóng (`Hot Binary Snapshot`):**
   * Song song với kho Git, hệ thống gọi `SqliteConnection.BackupDatabase()` để duy trì 1 tệp `latest_snapshot.db` nguyên vẹn (được đặt trong `.gitignore`). Tệp này đảm bảo có thể chép nhanh ra ổ cứng ngoài bất kỳ lúc nào mà không bị lỗi khóa tệp (`Database Locked`).
4. **Khôi phục du hành thời gian (`Time-Travel Restore`) an toàn tuyệt đối:**
   * Cán bộ có thể chọn bất kỳ bản Commit nào trong lịch sử (hôm qua, tuần trước, hoặc trước khi Import Excel) để khôi phục.
   * **Cơ chế bảo vệ kép (`Pre-Restore Safety Commit`):** Ngay trước khi nạp lại dữ liệu từ bản Commit cũ, nếu CSDL hiện tại đang có thay đổi chưa lưu, hệ thống tự động chụp một bản Commit `[Pre-Restore Safety Point]`. Nhờ đó, ngay cả khi cán bộ khôi phục nhầm phiên bản, họ vẫn có thể quay lại trạng thái vừa rồi chỉ với 1 cú nhấp chuột.

:::info[Tài liệu giải phẫu chuyên sâu & Ví dụ thực tế từ Unit Test]
Để xem chi tiết từng bước cách `LibGit2Sharp` chuyển đổi bảng SQLite sang dòng văn bản `.jsonl`, tính toán `git diff` từng dòng (`+LinesAdded / -LinesDeleted`), luồng khôi phục nguyên tử và **ví dụ minh họa thực tế trích từ bộ kiểm thử `BackupServiceTests.cs`**, mời xem tài liệu chuyên đề:
👉 **[F11b - Giải Phẫu Cơ Chế Sao Lưu CSDL Kiểu Git & Ví Dụ Thực Tế](./F11b-co-che-git-backup-chi-tiet.md)**
:::

---

### 3.2. Tầng Sao Lưu 2 (Tùy Chọn): PostgreSQL Backup & Khôi Phục Chủ Động

PostgreSQL chỉ là **bản sao lưu tầng 2, không bắt buộc**. Ứng dụng chạy bình thường khi không có mạng hoặc chưa cấu hình DB Host. Mọi thao tác với PostgreSQL đều do người dùng chủ động thực hiện:

1. **Cấu hình chuỗi kết nối trên máy:**
   * Nhập/sửa tại tab **"DB Backup (PostgreSQL)"** trong màn hình Cấu hình hệ thống. Có thể để trống.
   * Chuỗi kết nối **không** nằm trong `appsettings.json` đã commit và **không** được ghi ra log/diagnostic.
   * Lưu tại `%LocalAppData%/QuanLyDKHP/backup-config.json`:
     * **Windows:** secret được mã hóa bằng **DPAPI (CurrentUser)**; máy/tài khoản khác không đọc được.
     * **macOS/Linux:** file có quyền `0600` (chỉ chủ sở hữu đọc/ghi).
     * File cấu hình cũ dạng plaintext vẫn đọc được và được nâng cấp khi lưu lại.
2. **Đẩy dữ liệu lên PostgreSQL (Local → Host):**
   * Chỉ chạy khi người dùng bấm **"Đẩy dữ liệu lên PostgreSQL Backup"**. Không có push tự động lúc khởi động, đăng nhập, import hay đóng ứng dụng.
   * Hệ thống upsert **đủ 8 bảng** (`NguoiDung`, `CauHinhHeThong`, `HocKy`, `MonHoc`, `SinhVien`, `LopHocPhan`, `DangKyHocPhan`, `HocPhiHocKy`) để bản backup không bị thiếu dữ liệu.
   * Không tự tạo database hay bảng trên Host ngoài thao tác người dùng; nếu kết nối lỗi, dữ liệu local không bị ảnh hưởng.
3. **Khôi phục từ PostgreSQL (Host → Local) — thao tác riêng, có xác nhận:**
   * Nút **"Khôi phục từ PostgreSQL Backup..."** đọc trước số dòng từng bảng để hiển thị **preview** và yêu cầu xác nhận.
   * Khi xác nhận, hệ thống **fetch và kiểm tra đầy đủ 8 bảng trước**, sau đó tạo Git snapshot `[Pre-Restore]` cho trạng thái local hiện có, rồi mới ghi đè SQLite trong một transaction. Nếu bất kỳ bước đọc nào lỗi, dữ liệu local giữ nguyên.
   * Nếu bản backup không có tài khoản người dùng nào, hệ thống **hủy khôi phục** và giữ nguyên dữ liệu local để tránh mất khả năng đăng nhập.
   * Không đẩy ngược dữ liệu lên PostgreSQL sau khi restore.
4. **Kiểm tra kết nối tách biệt với khôi phục:**
   * Nút **"Kiểm tra kết nối"** chỉ mở kết nối thử, không đọc/ghi dữ liệu và không thay đổi SQLite local.

:::warning[First run trên máy mới]
Lần chạy đầu tiên tạo `quanlydkhp.db` **trống** (chỉ có tài khoản bootstrap `admin` để đăng nhập). Hệ thống **không** tự seed từ PostgreSQL, không pull delta, không full refresh và không đọc bất kỳ DB host nào. Người dùng nhập dữ liệu từ Excel, hoặc cấu hình PostgreSQL backup rồi chủ động khôi phục nếu đã có bản backup.
:::

---

## 4. Giao Diện Quản Lý Sao Lưu & Phục Hồi (Màn Hình Cấu Hình)

Tính năng quản lý sao lưu được tích hợp trực tiếp vào màn hình **Cấu hình hệ thống** (`CauHinhView.axaml`) dành cho Quản trị viên và Cán bộ giáo vụ:

1. **Bảng thông tin trạng thái CSDL Local:**
   * Hiển thị đường dẫn tệp SQLite chính (`quanlydkhp.db`), dung lượng tệp hiện tại, trạng thái chế độ `WAL Mode`, tổng số bản lưu Git (`Commits`) và mốc thời gian sao lưu gần nhất.
2. **Các thao tác điều khiển nhanh:**
   * **Sao lưu ngay (Git Commit):** Cho phép nhập ghi chú (ví dụ: *"Lưu trước khi khóa sổ học phí HK1"*) và tạo một điểm khôi phục tức thì.
   * **Nhập file Backup & Ghi đè:** Chọn tệp `.dkhp-backup.zip` do chương trình xuất ra; preview và xác nhận trước khi ghi đè.
3. **Tab "DB Backup (PostgreSQL)" — tùy chọn:**
   * Nhập/sửa chuỗi kết nối, **Kiểm tra kết nối**, **Lưu cấu hình**, **Xóa cấu hình**. Có thể để trống.
   * **Đẩy dữ liệu lên PostgreSQL Backup:** chỉ chạy khi bấm nút, upsert đủ 8 bảng.
   * **Khôi phục từ PostgreSQL Backup...:** hiển thị preview số dòng từng bảng, yêu cầu xác nhận, tạo Git snapshot `[Pre-Restore]`, fetch đủ dữ liệu rồi mới ghi đè local.
   * Nút **"Làm mới"** trên giao diện chỉ reload từ SQLite local; không kéo dữ liệu từ PostgreSQL.
4. **Danh sách Lịch sử Phiên bản (`Git Commit Log`):**
   * Hiển thị danh sách các bản lưu theo trình tự thời gian giảm dần với: Mã phiên bản rút gọn (`Short SHA`), Thời điểm lưu, Loại bản lưu (`Auto-Daily`, `Manual`, `Pre-Restore`, `Imported`), Tóm tắt biến động dữ liệu từng bảng và nút **"Khôi phục về bản này"**.

---

## 5. Tối Ưu Hóa Hạ Tầng Dữ Liệu & Bảo Toàn Toàn Vẹn Cục Bộ (Nhóm 5, ADR-22)

Nhằm nâng cao tính bền vững của tầng lưu trữ SQLite và ngăn ngừa mọi rủi ro mất mát dữ liệu ngoài ý muốn:
1. **Không bao giờ xóa tệp CSDL vật lý:** Loại bỏ hoàn toàn lệnh `EnsureDeletedAsync()` khỏi mọi chu trình. Không xóa `quanlydkhp.db` hay hủy schema SQLite đang chạy ở chế độ WAL.
2. **Reload giao diện chỉ đọc SQLite local:** `forceReload` trong `MemoryCacheStore` chỉ nạp lại danh mục từ SQLite, không xóa bảng và không gọi DB host. Nhờ đó thao tác "Làm mới" không thể làm mất dữ liệu local khi mất mạng.
3. **Ghi đè local chỉ trong transaction có kiểm soát:** Restore từ Git, Import file backup và Restore từ PostgreSQL đều tạo điểm lưu `[Pre-Restore]`/`[Pre-Import]` trước, fetch/validate đầy đủ, rồi ghi đè trong transaction; lỗi sẽ rollback.
4. **Không tự seed/pull từ DB host:** First run chỉ tạo schema + tài khoản bootstrap `admin`. Không có `InitialSeedAsync`, `SyncDeltaAsync` hay full refresh từ PostgreSQL trong startup, đăng nhập, CRUD, import hoặc restore.

---

## 6. Các Tệp Mã Nguồn Triển Khai

* **Hợp đồng & DTOs Sao lưu:**
  * `src/QuanLyDKHP.Core/Interfaces/IBackupService.cs`
  * `src/QuanLyDKHP.Core/Interfaces/IBackupConfigStore.cs`
  * `src/QuanLyDKHP.Core/Dtos/BackupDtos.cs`
* **Ngữ cảnh CSDL Local chính & Remote Replica:**
  * `src/QuanLyDKHP.Infrastructure/Data/AppDbContext.cs` (SQLite WAL Primary & Provider-agnostic mapping)
  * `src/QuanLyDKHP.Infrastructure/Data/RemoteBackupDbContext.cs` (kết nối PostgreSQL backup, tạo theo cấu hình người dùng)
* **Động cơ Sao lưu Git (`LibGit2Sharp`), cấu hình & đồng bộ DB Host:**
  * `src/QuanLyDKHP.Infrastructure/Services/BackupService.cs`
  * `src/QuanLyDKHP.Infrastructure/Services/BackupConfigStore.cs`
* **Giao diện & ViewModel Quản lý Sao lưu:**
  * `src/QuanLyDKHP.App/ViewModels/CauHinhViewModel.cs`
  * `src/QuanLyDKHP.App/Views/CauHinhView.axaml`
  * `src/QuanLyDKHP.App/App.axaml.cs` (Khởi tạo SQLite WAL, log first-run & lịch Auto-Backup Git hằng ngày)

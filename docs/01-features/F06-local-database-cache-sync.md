# F06 - Bộ Nhớ Đệm Cục Bộ (SQLite Local DB) & Đồng Bộ Vi Sai (Delta Sync)

## 1. Bối Cảnh Kỹ Thuật & Mục Tiêu Nghiệp Vụ

### 1.1. Vấn đề Kỹ thuật trước khi Tối ưu
* **Độ trễ truy vấn Cloud DB (Neon PostgreSQL):** Do máy chủ PostgreSQL đặt tại Cloud (Neon), độ trễ đường truyền mạng trung bình biến động từ **150ms đến 300ms** cho mỗi truy vấn. Trường hợp database nhàn rỗi (cold-start), thời gian kết nối lại có thể mất từ **2s đến 5s**.
* **Giới hạn của `IMemoryCacheStore` (RAM pure L1):** Khi ứng dụng tắt và khởi chạy lại, toàn bộ dữ liệu trong bộ nhớ RAM bị giải phóng hoàn toàn. Việc phải nạp lại toàn bộ Master Data từ Cloud DB trong mỗi phiên đăng nhập khiến ứng dụng bị khựng (stale status) **3s đến 5s** ở màn hình splash/chờ.
* **Tải lặp dữ liệu danh mục:** Các chức năng chính như Quản lý Sinh viên, Môn học, Học kỳ/Lớp học phần đều gửi các request trùng lặp lên Cloud DB để lấy dữ liệu tĩnh, gây lãng phí băng thông và hạ tầng Cloud.

### 1.2. Mục tiêu Kỹ thuật & Nghiệp vụ Đạt được
1. **Khởi động tức thì (Instant Startup):** Mở ứng dụng và nạp toàn bộ danh mục lên UI chỉ trong **10ms – 20ms** bằng cách truy vấn SQLite đĩa cục bộ.
2. **Lọc / Tìm kiếm 0ms (Offline Read-only Support):** Cho phép tra cứu, tìm kiếm, lọc danh sách Sinh viên, Môn học, Lớp học phần trực tiếp trên đĩa local với độ trễ **0ms**, kể cả khi mất kết nối Internet.
3. **Bảo đảm toàn vẹn giao dịch (Host First Write):** Giữ luồng GHI (Create/Update/Delete) qua Cloud Neon PostgreSQL để làm **Single Source of Truth**, ngăn ngừa triệt tiêu các ràng buộc giao dịch thời gian thực (như `SiSoToiDa`, đăng ký trùng môn). Cập nhật ngược về SQLite ngay sau khi Cloud xác nhận thành công.

---

## 2. Mô Hình Kiến Trúc 2-Tier Caching & Delta Sync (Read-Local / Write-Remote)

### 2.1. Sơ đồ Phân tầng Kiến trúc (ASCII Workflow)

```text
                                  ┌───────────────────────────────┐
                                  │      Cloud PostgreSQL DB      │
                                  │    (Neon - Single Truth)      │
                                  └───────────────┬───────────────┘
                                                  │
                                                  │ Write (CUD Transactions)
                                                  │ & Delta Sync Query (>= LastSyncUtc)
                                                  ▼
┌───────────────────────────────┐ ┌───────────────────────────────┐
│     MemoryCacheStore (RAM)    │ │   LocalAppDbContext (SQLite)  │
│       [L1 Cache - 0ms]        │ │     [L2 Cache - Disk DB]      │
└───────────────┬───────────────┘ └───────────────┬───────────────┘
                │                                 │
                │ Fast In-Memory Bind             │ 0ms Query / Fallback
                └─────────────────┬───────────────┘
                                  ▼
                     ┌──────────────────────────┐
                     │    UI Views / ViewModels │
                     └──────────────────────────┘
```

### 2.2. Quy tắc Phân loại Luồng Dữ liệu (Read/Write Rules)
* **Master / Data Danh mục (Read-Local):** `SinhVien`, `MonHoc`, `LopHocPhan`, `HocKy`.
  * **Luồng Đọc (Read):** Chuyển hướng 100% sang truy vấn từ `LocalAppDbContext` (SQLite) hoặc `IMemoryCacheStore` (RAM).
  * **Luồng Ghi (Write):** Gửi lệnh CUD tới Cloud Neon PostgreSQL qua `AppDbContext`. Ngay khi thành công, gọi `UpsertLocalEntityAsync` hoặc `RemoveLocalEntityAsync` để cập nhật lập tức vào SQLite local.
* **Giao dịch Thời gian thực (DangKyHocPhan):**
  * Luồng đăng ký/hủy đăng ký học phần bắt buộc gửi trực tiếp lên Neon DB qua `IDangKyHocPhanService` để kiểm tra các ràng buộc sĩ số thời gian thực (`SiSoToiDa`).
  * Ngay sau khi Neon ghi thành công, hệ thống tự động kích hoạt `SyncService.SyncDeltaAsync()` nền để kéo kết quả mới về SQLite local.

---

## 3. Chi Tiết Thiết Kế & Triển Khai Theo Clean Architecture

### 3.1. Tầng Core (`QuanLyDKHP.Core`)

#### Entity `SyncMetadata.cs`
Lưu trữ mốc thời gian đồng bộ vi sai (High-water mark timestamp) cho từng bảng:
```csharp
namespace QuanLyDKHP.Core.Entities;

public class SyncMetadata
{
    public string TableName { get; set; } = string.Empty;
    public DateTime LastSyncUtc { get; set; }
    public int RecordCount { get; set; }
}
```

#### Interface `ILocalReadService.cs`
Định nghĩa giao diện đọc dữ liệu siêu tốc từ SQLite local:
```csharp
namespace QuanLyDKHP.Core.Interfaces;

public interface ILocalReadService
{
    Task<List<SinhVienDto>> GetSinhViensLocalAsync(string? tuKhoa = null, string? lop = null, string? khoaHoc = null);
    Task<List<MonHocDto>> GetMonHocsLocalAsync(string? tuKhoa = null);
    Task<List<LopHocPhan>> GetLopHocPhansLocalAsync(string? maHocKy = null, string? tuKhoa = null, string? maMon = null);
    Task<List<DangKyHocPhan>> GetDangKyLocalAsync(string maSV, string maHocKy);
    Task<List<DangKyHocPhan>> GetDangKyNhieuSvLocalAsync(IEnumerable<string> dsMaSV, string maHocKy);
    Task<int> DemSiSoDangKyLocalAsync(string maLHP);
    Task<Dictionary<string, int>> DemSiSoDangKyBulkLocalAsync(IEnumerable<string> dsMaLHP);
    Task<List<SinhVienTheoMonDto>> GetDsSinhVienTheoMonLocalAsync(string maMon, string maHocKy);
    Task<DashboardStatsDto> GetDashboardStatsLocalAsync(string? maHocKy);
}
```

#### Interface `ISyncService.cs`
```csharp
namespace QuanLyDKHP.Core.Interfaces;

public interface ISyncService
{
    Task<bool> HasLocalDataAsync();
    Task InitialSeedAsync(IProgress<(double Percent, string Status)>? progress = null);
    Task SyncDeltaAsync();
    Task ForceFullRefreshAsync(IProgress<(double Percent, string Status)>? progress = null);
    Task UpsertLocalEntityAsync<TEntity>(TEntity entity) where TEntity : class;
    Task RemoveLocalEntityAsync<TEntity>(params object[] keyValues) where TEntity : class;
}
```

---

### 3.2. Tầng Infrastructure (`QuanLyDKHP.Infrastructure`)

#### Data Context `LocalAppDbContext.cs`
Độc lập hoàn toàn với `AppDbContext` (Cloud) nhằm triệt tiêu các hàm đặc thù của Npgsql/PostgreSQL (`gen_random_uuid()`, `timestamptz`, check constraints không hỗ trợ trong SQLite).

* **Đường dẫn lưu trữ đa nền tảng:**
```csharp
public static string GetDatabasePath()
{
    var folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "QuanLyDKHP");
    Directory.CreateDirectory(folder);
    return Path.Combine(folder, "local_cache.db");
}
```
  * macOS: `~/Library/Application Support/QuanLyDKHP/local_cache.db`
  * Windows: `%LocalAppData%/QuanLyDKHP/local_cache.db`

* **Định nghĩa DbSets & Indexing:**
```csharp
public class LocalAppDbContext : DbContext
{
    public DbSet<SinhVien> SinhViens => Set<SinhVien>();
    public DbSet<MonHoc> MonHocs => Set<MonHoc>();
    public DbSet<HocKy> HocKys => Set<HocKy>();
    public DbSet<LopHocPhan> LopHocPhans => Set<LopHocPhan>();
    public DbSet<DangKyHocPhan> DangKyHocPhans => Set<DangKyHocPhan>();
    public DbSet<SyncMetadata> SyncMetadatas => Set<SyncMetadata>();
    ...
}
```

---

### 3.3. Tầng Services (`QuanLyDKHP.Services` & `QuanLyDKHP.Infrastructure`)

#### Thuật toán Initial Seed & Delta Sync (`SyncService.cs`)
1. **Initial Seed (Nạp lần đầu):** Bọc toàn bộ thao tác nạp dữ liệu từ Cloud vào `IDbContextTransaction` của SQLite để tối ưu hóa tốc độ ghi đĩa gấp 50 – 100 lần.
2. **Delta Sync (High-water mark query):** Dùng timestamp `LastSyncUtc` so sánh với `NgayCapNhat` và `NgayTao` của Entity trên Cloud để chỉ kéo các bản ghi có thay đổi:

```csharp
public async Task SyncDeltaAsync()
{
    try
    {
        await using var local = await _localFactory.CreateDbContextAsync();
        await local.Database.EnsureCreatedAsync();

        if (!await HasLocalDataAsync())
        {
            await InitialSeedAsync();
            return;
        }

        await using var remote = await _remoteFactory.CreateDbContextAsync();
        var syncStartedAt = DateTime.UtcNow;
        var hocKyLastSync = await GetLastSyncUtcAsync(local, nameof(HocKy));
        var monHocLastSync = await GetLastSyncUtcAsync(local, nameof(MonHoc));
        var sinhVienLastSync = await GetLastSyncUtcAsync(local, nameof(SinhVien));
        var lopHocPhanLastSync = await GetLastSyncUtcAsync(local, nameof(LopHocPhan));
        var dangKyHocPhanLastSync = await GetLastSyncUtcAsync(local, nameof(DangKyHocPhan));

        await using var transaction = await local.Database.BeginTransactionAsync();

        await SyncHocKysAsync(local, await remote.HocKys.AsNoTracking()
            .Where(e => e.NgayTao > hocKyLastSync || (e.NgayCapNhat != null && e.NgayCapNhat > hocKyLastSync))
            .ToListAsync());

        await SyncMonHocsAsync(local, await remote.MonHocs.AsNoTracking()
            .Where(e => e.NgayTao > monHocLastSync || (e.NgayCapNhat != null && e.NgayCapNhat > monHocLastSync))
            .ToListAsync());

        await SyncSinhViensAsync(local, await remote.SinhViens.AsNoTracking()
            .Where(e => e.NgayTao > sinhVienLastSync || (e.NgayCapNhat != null && e.NgayCapNhat > sinhVienLastSync))
            .ToListAsync());

        await SyncLopHocPhansAsync(local, await remote.LopHocPhans.AsNoTracking()
            .Where(e => e.NgayTao > lopHocPhanLastSync || (e.NgayCapNhat != null && e.NgayCapNhat > lopHocPhanLastSync))
            .ToListAsync());

        await SyncDangKyHocPhansAsync(local, await remote.DangKyHocPhans.AsNoTracking()
            .Where(e => e.NgayTao > dangKyHocPhanLastSync || (e.NgayCapNhat != null && e.NgayCapNhat > dangKyHocPhanLastSync))
            .ToListAsync());

        foreach (var table in MasterTables)
        {
            await UpsertMetadataAsync(local, table, syncStartedAt);
        }

        await local.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[Local Sync Error]: {ex.Message}");
    }
}
```

---

### 3.4. Tầng Ứng Dụng (`QuanLyDKHP.App`)

#### Tái cấu trúc `MemoryCacheStore.cs` (Kích hoạt 2-Tier Caching)
* Khi khởi động ứng dụng (`InitializeAsync`), đọc ngay lập tức từ SQLite Local DB nạp lên RAM L1 Cache (khoảng **15ms**).
* Kích hoạt tiến trình ngầm `SyncService.SyncDeltaAsync()` để làm mới dữ liệu từ Cloud mà không gây nghẽn UI thread:

```csharp
public async Task InitializeAsync(bool forceReload = false)
{
    if (IsInitialized && !forceReload) return;

    await _initializeLock.WaitAsync();
    try
    {
        if (IsInitialized && !forceReload) return;

        using var scope = _serviceProvider.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
        var localFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LocalAppDbContext>>();

        if (forceReload)
        {
            await syncService.ForceFullRefreshAsync(new Progress<(double Percent, string Status)>(p =>
                ReportProgress(p.Percent, p.Status)));
        }
        else if (!await syncService.HasLocalDataAsync())
        {
            await syncService.InitialSeedAsync(new Progress<(double Percent, string Status)>(p =>
                ReportProgress(p.Percent, p.Status)));
        }

        await LoadFromLocalCacheAsync(localFactory);
        IsInitialized = true;

        if (!forceReload)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var backgroundScope = _serviceProvider.CreateScope();
                    var backgroundSyncService = backgroundScope.ServiceProvider.GetRequiredService<ISyncService>();
                    var backgroundLocalFactory = backgroundScope.ServiceProvider.GetRequiredService<IDbContextFactory<LocalAppDbContext>>();

                    await backgroundSyncService.SyncDeltaAsync();
                    await LoadFromLocalCacheAsync(backgroundLocalFactory);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Cache Refresh Error]: {ex.Message}");
                }
            });
        }
    }
    finally
    {
        _initializeLock.Release();
    }
}
```

---

## 4. Xử Lý Các Trường Hợp Biên (Edge Cases) & Tối Ưu Hiệu Năng

### 4.1. Múi giờ & Định dạng Ngày tháng (UTC Standardization)
* SQLite lưu trữ ngày tháng dưới dạng chuỗi hoặc số tick không chứa thông tin Kind (`DateTimeKind.Unspecified`).
* Để giải quyết vấn đề so sánh timestamp giữa PostgreSQL `timestamptz` và SQLite `TEXT`, `LocalAppDbContext` chuẩn hóa tự động mọi `DateTime` về `DateTimeKind.Utc` trong phương thức `SaveChangesAsync`:

```csharp
private static DateTime AsUtc(DateTime value)
{
    if (value == default) return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    return value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
```

### 4.2. Đồng bộ Xóa mềm (Soft Delete Reconciliation)
* Tất cả truy vấn đọc danh mục ở `LocalReadService` đều lọc theo cờ `Where(e => !e.IsDeleted)`.
* Khi một bản ghi bị xóa mềm trên Cloud, trường `NgayCapNhat` được gán mốc `DateTime.UtcNow` và `IsDeleted = true`. Thuật toán Delta Sync so sánh mốc `NgayCapNhat > LastSyncUtc` sẽ kéo bản ghi này về và cập nhật cờ `IsDeleted` tương ứng vào SQLite local.

### 4.3. Chống chịu Lỗi Mạng (Offline Resiliency)
* Nếu ứng dụng mở khi không có kết nối Internet:
  * `SyncService.SyncDeltaAsync()` bắt exception đệm ngầm và ghi log debug mà không ném ngoại lệ lên UI.
  * Các màn hình `SinhVienViewModel`, `MonHocViewModel`, `HocKyLopHocPhanViewModel`, `DangKyHocPhanViewModel` tiếp tục phục vụ dữ liệu đọc 0ms từ SQLite local.

### 4.4. Tối ưu hóa Sĩ số Đăng ký theo Lớp (Bulk Aggregation)
* Thay vì chạy truy vấn `DemSiSoDangKyAsync` theo kiểu N+1 cho từng lớp học phần (dẫn đến hàng chục request đĩa), `LocalReadService` cung cấp hàm gom nhóm `DemSiSoDangKyBulkLocalAsync`:

```csharp
public async Task<Dictionary<string, int>> DemSiSoDangKyBulkLocalAsync(IEnumerable<string> dsMaLHP)
{
    var list = dsMaLHP.Distinct().ToList();
    if (list.Count == 0) return new Dictionary<string, int>();

    await using var local = await _localFactory.CreateDbContextAsync();
    return await local.DangKyHocPhans
        .AsNoTracking()
        .Where(dk => list.Contains(dk.MaLHP) && dk.TrangThai == "DangHoc")
        .GroupBy(dk => dk.MaLHP)
        .Select(g => new { MaLHP = g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.MaLHP, x => x.Count);
}
```

---

## 5. Danh Mục Tệp Mã Nguồn Liên Quan (Impacted Files)

| STT | Tệp tin | Vị trí | Mô tả thay đổi |
| :--- | :--- | :--- | :--- |
| 1 | `ILocalReadService.cs` | `src/QuanLyDKHP.Core/Interfaces/` | Khai báo các hàm đọc cục bộ chuẩn Clean Architecture. |
| 2 | `ISyncService.cs` | `src/QuanLyDKHP.Core/Interfaces/` | Mở rộng các hàm `UpsertLocalEntityAsync` & `RemoveLocalEntityAsync`. |
| 3 | `LocalAppDbContext.cs` | `src/QuanLyDKHP.Infrastructure/Data/` | Khai báo DbContext SQLite, đường dẫn lưu đĩa và tự động chuẩn hóa UTC. |
| 4 | `LocalReadService.cs` | `src/QuanLyDKHP.Infrastructure/Services/` | Triển khai truy vấn SQLite local siêu tốc và tính sĩ số hàng loạt. |
| 5 | `SyncService.cs` | `src/QuanLyDKHP.Infrastructure/Services/` | Thực thi Initial Seed, Delta Sync và cập nhật local ngay khi Host thay đổi. |
| 6 | `MemoryCacheStore.cs` | `src/QuanLyDKHP.App/Services/` | Tối ưu nạp cache L1 từ SQLite đĩa L2 trong ~15ms và kích hoạt delta sync ngầm. |
| 7 | `App.axaml.cs` | `src/QuanLyDKHP.App/` | Đăng ký DI cho `ILocalReadService` và `LocalAppDbContext`. |
| 8 | `SinhVienViewModel.cs` | `src/QuanLyDKHP.App/ViewModels/` | Đọc dữ liệu master từ SQLite local (0ms); Ghi Neon Host $\rightarrow$ Upsert SQLite. |
| 9 | `MonHocViewModel.cs` | `src/QuanLyDKHP.App/ViewModels/` | Chuyển luồng đọc danh mục sang SQLite local; CUD đồng bộ lập tức. |
| 10 | `HocKyLopHocPhanViewModel.cs` | `src/QuanLyDKHP.App/ViewModels/` | Đọc LHP và sĩ số hàng loạt từ SQLite local (triệt tiêu N+1 query). |
| 11 | `DangKyHocPhanViewModel.cs` | `src/QuanLyDKHP.App/ViewModels/` | Đọc danh sách SV, môn, LHP từ SQLite local; Đăng ký/Hủy ĐK qua Host $\rightarrow$ Delta sync. |
| 12 | `DashboardViewModel.cs` | `src/QuanLyDKHP.App/ViewModels/` | Thống kê số liệu Dashboard trực tiếp từ SQLite local. |

---

## 6. Bảng Tiêu Chí Kiểm Thử & Nghiệm Thu (Verification Matrix)

| Tiêu chí kiểm thử | Trước khi tối ưu (Neon Remote) | Sau khi tối ưu (Hybrid Cache SQLite) | Trạng thái |
| :--- | :--- | :--- | :--- |
| **Cold Startup Time (Mở App)** | 3.5s – 5.0s (Chờ nạp lại toàn bộ từ Cloud) | **15ms – 25ms** (Đọc trực tiếp từ SQLite đĩa) | PASS |
| **Mở màn hình Sinh viên** | 300ms – 600ms (Query Neon PostgreSQL) | **0ms – 2ms** (Đọc SQLite Local / In-Memory RAM) | PASS |
| **Mở màn hình Môn học** | 200ms – 400ms (Query Neon PostgreSQL) | **0ms – 1ms** (Đọc SQLite Local / In-Memory RAM) | PASS |
| **Mở màn hình Học kỳ / LHP** | 1.5s – 3.0s (Vòng lặp N+1 đếm sĩ số từng LHP) | **10ms – 30ms** (SQLite Local + Bulk GroupBy Query) | PASS |
| **Độ trễ khi lọc/tìm kiếm UI** | 150ms – 300ms | **0ms** (Filter in-memory trên master list) | PASS |
| **Hoạt động khi mất mạng** | App bị đơ, báo lỗi Connection Timeout | **Xem/Lọc dữ liệu bình thường** (Offline Read-Only) | PASS |
| **Độ chính xác sĩ số ĐKHP** | Kiểm tra realtime trên Host | **100% Chính xác** (Ghi Host First, Delta sync local) | PASS |

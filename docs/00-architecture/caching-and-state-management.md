# Thiết Kế Kiến Trúc Cache & Quản Lý Trạng Thái Giao Diện (In-Memory Cache & State Management)

## 1. Bối cảnh & Vấn đề (Problem Statement)

* **Hiện trạng hạ tầng:** Ứng dụng kết nối trực tiếp đến PostgreSQL host trên Cloud (Neon). Do độ trễ mạng (Network Latency), các truy vấn đọc dữ liệu gặp độ trễ đáng kể.
* **Hạn chế của luồng Navigation cũ:**
  * `MainWindowViewModel` dùng `Func<TViewModel>` (Factory Delegate) để khởi tạo ViewModel mới mỗi lần người dùng bấm chuyển chức năng trên Sidebar Menu.
  * Mỗi lần mở một chức năng (ví dụ: *Học kỳ / Lớp học phần*), ViewModel gọi xuống Service/Repository để query lại từ Neon DB.
  * Khi người dùng rời tab và quay lại, toàn bộ trạng thái và dữ liệu cũ bị hủy, dẫn đến việc phải tải lại từ đầu, gây ức chế trải nghiệm người dùng (UX).

## 2. Mục tiêu giải pháp

* Giảm thời gian phản hồi (Load Time) khi chuyển đổi qua lại giữa các tab xuống **0ms** (lấy ngay lập tức từ bộ nhớ).
* Thực hiện tải ngầm dữ liệu danh mục (Background Pre-fetching) ngay từ giai đoạn khởi tạo/đăng nhập.
* Hạn chế tối đa số lượng request dư thừa lên Neon DB.
* Giữ nguyên tính toàn vẹn của Clean Architecture: ViewModel không phụ thuộc DB trực tiếp, mọi thay đổi tập trung ở tầng Quản lý ViewModel và Tầng Service/Cache.

---

## 3. Kiến trúc Đề xuất: Hai Trụ Cột Chính

### Trụ cột 1: ViewModel Instance Caching (Giữ trạng thái View)

Thay vì tạo mới ViewModel mỗi lần chuyển tab, ứng dụng duy trì một bộ nhớ đệm các ViewModel đã từng được khởi tạo.

* **Cơ chế hoạt động:**
  * Sử dụng một cấu trúc từ điển `Dictionary<string, ObservableObject>` (hoặc cấu hình Lifetime dạng `Singleton` cho các ViewModels phù hợp trong DI Container).
  * Khi chuyển tab bằng `NavigateTo(menuItem)`:
    * Nếu ViewModel tương ứng đã tồn tại trong Cache: Lấy trực tiếp thể hiện cũ gán vào `CurrentViewModel`. Dữ liệu trên bảng, vị trí cuộn, input nhập dở giữ nguyên 100%.
    * Nếu chưa từng mở: Gọi Factory Delegate để khởi tạo lần đầu và lưu vào Cache.

### Trụ cột 2: In-Memory Data Store (Bộ nhớ đệm dữ liệu trên RAM)

Tạo một lớp dịch vụ `MemoryDataStore` đăng ký `Singleton` trong DI Container, lưu trữ các tập dữ liệu danh mục tĩnh hoặc ít biến động.

* **Phân loại dữ liệu Cache:**
  * **Master Data (Cache chủ động):** Môn học, Học kỳ, Danh mục hệ đào tạo, Cấu hình.
  * **Transactional Data (Cache theo ngữ cảnh):** Sinh viên, Đăng ký học phần, Học phí (kết hợp với cơ chế làm mới khi thao tác Create/Update/Delete).

---

## 4. Luồng Xử Lý (Sequence Workflow)

```text
[Khởi động / Login Thành Công]
│
├─── (Background Worker) ───> Gọi Remote DB (Neon) lấy Master Data
│                                  │
│                                  ▼
│                            Lưu vào MemoryDataStore (Singleton)
▼
[Người dùng Chuyển Tab (Sidebar)]
│
├──> Kiểm tra ViewModel Cache?
│       ├─ Đã có ──> Render ngay lập tức (0ms)
│       └─ Chưa có ─> Tạo mới ViewModel từ Factory -> Lưu vào Cache
│
└──> ViewModel nạp dữ liệu:
        ├─ Lấy trực tiếp từ MemoryDataStore (nếu có sẵn)
        └─ Cung cấp nút [Làm mới] để chủ động nạp lại từ Neon khi cần
```
## 5. Chiến Lược Đồng Bộ & Làm Mới (Cache Invalidation)

1. **Thao tác Ghi (Create / Update / Delete):**
   * Gửi lệnh ghi lên Remote DB (Neon).
   * Khi ghi thành công, cập nhật trực tiếp bản ghi vào tập dữ liệu trong `MemoryDataStore` và phát thông báo qua `CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger` để các ViewModel liên quan tự động cập nhật UI.
2. **Nút Làm Mới (Manual Refresh):**
   * Mỗi màn hình trang bị nút bấm icon "Làm mới", cho phép người dùng chủ động tải đè dữ liệu mới nhất từ Neon khi nghi ngờ có người khác chỉnh sửa dữ liệu trên hệ thống.

---

## 6. Kiến Trúc Thực Tế: Bộ Nhớ Đệm 2 Tầng (2-Tier Caching: RAM L1 + SQLite L2)

Hệ thống đã triển khai thành công mô hình **Hybrid Local Cache** kết hợp giữa RAM và cơ sở dữ liệu SQLite đĩa cục bộ:

* **Tầng L1 Cache (RAM):** `MemoryCacheStore` lưu các danh mục tĩnh trên bộ nhớ để cung cấp dữ liệu tức thì cho UI binding (0ms).
* **Tầng L2 Cache (Disk Storage):** `LocalAppDbContext` (SQLite) lưu dữ liệu bền vững tại thư mục người dùng (`SpecialFolder.LocalApplicationData`), cho phép nạp dữ liệu tức thì (10ms - 20ms) ngay khi vừa mở ứng dụng mà không cần chờ nạp lại từ Cloud.
* **Cơ chế Đồng bộ Vi sai (Delta Sync & Read-Local / Write-Remote):**
  * **Luồng Đọc (Read):** Mọi thao tác tìm kiếm, tra cứu, hiển thị danh sách từ ViewModel đều đọc trực tiếp từ SQLite Local DB qua `ILocalReadService` (hỗ trợ Offline Read-Only).
  * **Luồng Ghi (Write):** Thao tác Thêm/Sửa/Xóa và Đăng ký học phần được gửi trực tiếp lên Cloud Neon PostgreSQL làm Single Source of Truth, sau đó lập tức được cập nhật bản sao về SQLite Local DB qua `ISyncService.UpsertLocalEntityAsync` / `SyncDeltaAsync`.

> 🔗 Xem tài liệu đặc tả kỹ thuật chi tiết tại [`../01-features/F06-local-database-cache-sync.md`](../01-features/F06-local-database-cache-sync.md).
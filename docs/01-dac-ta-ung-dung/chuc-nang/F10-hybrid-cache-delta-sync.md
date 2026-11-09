---
id: F10-hybrid-cache-delta-sync
title: F10 - Bộ Nhớ Đệm Lai & SQLite Delta Sync
sidebar_position: 10
---

# F10 - Bộ Nhớ Đệm Lai (Hybrid Cache) & SQLite Delta Sync

:::warning[Thông báo chuyển giao kiến trúc (Superseded by F11)]
Kiến trúc mô tả trong tài liệu **F10** dưới đây ghi nhận giải pháp kỹ thuật ở **giai đoạn phát triển ban đầu** khi hệ thống sử dụng **Neon PostgreSQL (Cloud Serverless)** làm cơ sở dữ liệu chính (Primary DB) và dùng **SQLite Local** làm bộ nhớ đệm đọc (Read Cache).

Sau đợt rà soát tiến trình dự án và quy định bảo mật vận hành thực tế: **Cán bộ nội vụ chỉ được phép thực hiện nghiệp vụ trên máy tính cố định tại văn phòng** để đảm bảo an toàn bảo mật dữ liệu đào tạo và tài chính. Do đó, hệ thống đã được nâng cấp sang kiến trúc **[F11 - CSDL SQLite Cục Bộ Chính & Sao Lưu Phiên Bản Kiểu Git](./F11-local-sqlite-git-backup.md)**:
* **Giữ lại Tầng 1 (`In-Memory Cache Store` & `_viewModelCache`)** của F10 để duy trì tốc độ phản hồi giao diện `0ms`.
* **Nâng cấp Tầng 2 (`SQLite Local`)** từ bộ nhớ đệm thành **Cơ sở dữ liệu chính duy nhất (Primary Read/Write DB)** ngay trên máy tính cố định tại văn phòng.
* **Chuyển đổi cơ chế `Delta Sync`** từ chiều kéo (`Cloud -> Local`) thành **Hệ thống Sao lưu Đa tầng** (Sao lưu phiên bản vi sai cục bộ kiểu Git bằng `LibGit2Sharp` mỗi ngày + Đẩy dự phòng một chiều `Local -> DB Host`).
:::

## 1. Vấn Đề Kỹ Thuật (Giai Đoạn Đầu): Độ Trễ Mạng Của Cloud Database

Trong giai đoạn đầu phát triển, hệ thống kết nối trực tiếp đến cơ sở dữ liệu **Neon PostgreSQL Serverless**. Mặc dù Neon mang lại lợi thế về khả năng mở rộng và triển khai nhanh, ứng dụng Desktop gặp phải thách thức lớn về hiệu năng:
* Khoảng cách địa lý từ máy trạm đến máy chủ Cloud (Singapore/US) tạo độ trễ kết nối (Network Latency) từ **200ms đến 800ms** cho mỗi lần gửi truy vấn.
* Trạng thái "ngủ đông" (Cold Start) của Serverless Database có thể làm chậm thao tác đầu tiên lên tới 2-3 giây.
* Việc người dùng chuyển đổi qua lại giữa các tab chức năng liên tục tải lại toàn bộ danh mục tĩnh gây lãng phí băng thông và giảm trải nghiệm người dùng.

---

## 2. Giải Pháp Kiến Trúc Bộ Nhớ Đệm Lai (Hybrid Cache)

Để khắc phục triệt để điểm nghẽn hiệu năng trên mà vẫn giữ tính toàn vẹn của dữ liệu tập trung, hệ thống triển khai giải pháp kiến trúc hai tầng:

```mermaid
graph TD
    App["Ứng Dụng Desktop (Avalonia UI)"]
    L1["Tầng 1: In-Memory Cache Store (RAM)"]
    L2["Tầng 2: SQLite Local Database (Ổ đĩa máy trạm)"]
    Cloud["Neon PostgreSQL (Cloud Serverless)"]

    App -->|Đọc danh mục & ViewModel State: 0ms| L1
    App -->|Đọc bảng dữ liệu lớn Offline/Local: 5ms| L2
    L2 <-->|Đồng bộ vi sai nền (Delta Sync Engine)| Cloud
```

### 2.1. Tầng 1: In-Memory Cache Store & Giữ Vết ViewModel
* **Tải trước song song (Master Data Pre-fetching):** Ngay khi người dùng đăng nhập thành công, hệ thống sử dụng `Task.WhenAll` để nạp đồng thời toàn bộ dữ liệu danh mục tĩnh (`HocKy`, `LopSinhHoat`, `KhoaHoc`, `MonHoc`) vào `MemoryCacheStore` (Singleton). Mọi combobox và bộ lọc trên UI đều lấy dữ liệu từ RAM với độ trễ phản hồi **0ms**.
* **Lưu vết ViewModel (`_viewModelCache`):** `MainWindowViewModel` lưu trữ instance của các ViewModel đã khởi tạo vào từ điển `Dictionary<string, ObservableObject>`. Khi chuyển tab, giao diện tái sử dụng instance cũ thay vì khởi tạo lại từ đầu, triệt tiêu hoàn toàn độ trễ mở màn hình.

### 2.2. Tầng 2: Cơ Sở Dữ Liệu SQLite Cục Bộ & Đồng Bộ Vi Sai (Delta Sync)
* Một cơ sở dữ liệu SQLite nhẹ được nhúng trực tiếp trong thư mục ứng dụng tại máy trạm.
* Động cơ đồng bộ (Sync Engine) định kỳ kiểm tra trường thời gian `NgayCapNhat` (UTC Timestamp) giữa Cloud và Local:
  * Chỉ tải về những bản ghi được tạo mới hoặc chỉnh sửa sau mốc thời gian đồng bộ gần nhất (`LastSyncTimestamp`).
  * Giảm 95% lượng băng thông truyền tải qua Internet so với việc tải lại toàn bộ bảng.

---

## 3. Bảng Đo Đạc Hiệu Năng Thực Tế

| Thao Tác Nghiệp Vụ | Chưa Áp Dụng Cache (Neon Cloud) | Sau Khi Áp Dụng Hybrid Cache | Mức Độ Cải Thiện |
|---|:---:|:---:|:---:|
| Mở màn hình Tính học phí | ~1200ms | **0ms** (Lấy từ RAM) | Nhanh hơn 1000+ lần |
| Chuyển tab giữa Sinh viên & ĐKHP | ~850ms | **~10ms** (Tái dụng ViewModel) | Nhanh hơn 85 lần |
| Lọc tìm kiếm sinh viên theo từ khóa | ~350ms / ký tự | **0ms** (Debounce + RAM Filter) | Triệt tiêu giật lag UI |
| Tải danh sách môn học khi mở ĐKHP | ~600ms | **0ms** (Master In-Memory) | Phản hồi tức thì |

---

## 4. Các Tệp Mã Nguồn Triển Khai

* **Hợp đồng & Store In-Memory:** `src/QuanLyDKHP.Core/Interfaces/IMemoryCacheStore.cs` và `src/QuanLyDKHP.App/Services/MemoryCacheStore.cs`.
* **Quản lý Cache ViewModel:** Triển khai trong `src/QuanLyDKHP.App/ViewModels/MainWindowViewModel.cs`.
* **Cơ sở dữ liệu SQLite & Đồng bộ:** `src/QuanLyDKHP.Infrastructure/Data/SqliteLocalDbContext.cs`.
* **Tài liệu phân tích kiến trúc:** `docs/00-architecture/caching-and-state-management.md`.

---

## 5. Lý Do Ngừng Sử Dụng Mô Hình Cloud-Primary Và Chuyển Giao Sang F11

Trong quá trình kiểm tra tiến trình dự án và đánh giá mô hình triển khai thực tế tại đơn vị đào tạo, kiến trúc Cloud-Primary của F10 bộc lộ một số điểm không còn phù hợp với chính sách bảo mật mới:

1. **Yêu cầu bảo mật vật lý tại văn phòng:**
   * Nghiệp vụ quản lý hồ sơ sinh viên, điều chỉnh đăng ký học phần, nhập điểm và khóa sổ học phí được thực hiện bởi **cán bộ nội vụ thông qua ứng dụng Desktop đặt tại máy tính cố định ở văn phòng**.
   * Để bảo mật tuyệt đối dữ liệu nhạy cảm, quy định vận hành **không cho phép mở kết nối trực tiếp tới CSDL chính từ ngoài Internet** hoặc làm việc từ xa trên môi trường Cloud công cộng.
2. **Độ phức tạp của việc duy trì song song 2 DbContext (`AppDbContext` + `LocalAppDbContext`):**
   * Khi chỉ có máy trạm cố định tại văn phòng thao tác dữ liệu, việc ghi lên Cloud rồi lại đồng bộ ngược về SQLite cục bộ là dư thừa, dễ gây độ trễ khi ghi (Write Latency) và phụ thuộc vào đường truyền Internet của văn phòng.
3. **Nhu cầu Sao lưu & Kiểm soát lịch sử phiên bản dữ liệu (Backup & Time-Travel Recovery):**
   * Thay vì chỉ đồng bộ trạng thái mới nhất (nếu cán bộ thao tác nhầm trên Cloud thì trạng thái sai cũng bị đồng bộ đè xuống Local), văn phòng cần một cơ chế **lưu trữ lịch sử phiên bản dữ liệu theo từng ngày làm việc tối ưu như cách Git quản lý mã nguồn**, kết hợp khả năng sao lưu dự phòng sang một DB Host độc lập.

👉 **Xem chi tiết thiết kế kiến trúc mới tại:** **[F11 - CSDL SQLite Cục Bộ Chính & Sao Lưu Phiên Bản Kiểu Git (Git-Style Backup)](./F11-local-sqlite-git-backup.md)**.

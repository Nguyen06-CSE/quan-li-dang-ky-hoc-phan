---
id: nhat-ky-cong-viec-daily
title: Nhật Ký Công Việc Hằng Ngày (Daily Logs)
sidebar_position: 2
---

# Nhật Ký Công Việc Hằng Ngày (Daily Logs)

## 1. Nhật Ký Ngày 07/10/2026

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

## 2. Nhật Ký Ngày 04/10/2026

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

## 3. Nhật Ký Ngày 28/09/2026

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

## 4. Nhật Ký Ngày 20/09/2026 Đến 24/09/2026

* **Thực hiện:** NGÔ QUYỀN LINH, NGUYÊN, LIAM
* **Nội dung trọng tâm:** Hoàn thiện đặc tả phân hệ và cấu hình cơ sở dữ liệu

### Các kết quả đạt được
* Soạn thảo và hoàn thiện tài liệu đặc tả chức năng từ Spec 011 đến Spec 016 (Học phí, Báo cáo, Cấu hình, Người dùng, Import/Export).
* Viết tài liệu hướng dẫn cấu hình cơ sở dữ liệu PostgreSQL cục bộ qua Docker và tài khoản đám mây Neon.
* Sửa lỗi giao diện xem trước phiếu kết quả ĐKHP (Tab 4 Báo cáo), bổ sung thông tin trường chủ quản: *TRƯỜNG ĐẠI HỌC ĐÀ LẠT*.

---

## 5. Nhật Ký Ngày 18/09/2026

* **Thực hiện:** NGUYÊN
* **Nội dung trọng tâm:** Khởi tạo Solution Clean Architecture & Triển khai 10 Spec đầu tiên

### Các kết quả đạt được
* Khởi tạo Solution .NET 8 gồm 5 projects (`Core`, `Infrastructure`, `Services`, `App`, `Tests`).
* Cài đặt gói thư viện phụ thuộc, cố định phiên bản 8.x để tương thích .NET 8.
* Khởi tạo CSDL với Fluent API, cấu hình các trường Audit (`IAuditableEntity`) và Xóa mềm (`ISoftDeleteEntity`), sinh migration đầu tiên `InitialCreate`.
* Thiết lập ma trận phân quyền `PermissionMatrix` cho 4 vai trò.
* Triển khai hoàn tất quy trình kiểm tra 6 bước đăng ký học phần (`DangKyHocPhanService`) và viết 19 Unit Tests đầu tiên đạt 100% Passed.

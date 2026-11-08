---
id: F04-hoc-ky-lop-hoc-phan
title: F04 - Học Kỳ & Lớp Học Phần
sidebar_position: 4
---

# F04 - Quản Lý Học Kỳ & Lớp Học Phần

## 1. Mục Tiêu & Yêu Cầu Nghiệp Vụ

Tổ chức cấu trúc thời gian đào tạo và mở các lớp học phần cụ thể cho từng kỳ học. Đây là dữ liệu nền tảng phục vụ toàn bộ quy trình đăng ký môn học và xếp thời khóa biểu.

### Các tính năng chính
1. **Quản lý Học kỳ Đào tạo:**
   * Khai báo thông tin học kỳ: Mã học kỳ (VD: `HK1_2026_2027`), Tên học kỳ, Năm học, Ngày bắt đầu, Ngày kết thúc.
   * **Quy tắc Duy nhất một học kỳ mở:** Hệ thống bảo đảm chỉ có duy nhất một học kỳ mang cờ `DangMo = true`. Khi giáo vụ kích hoạt mở một kỳ mới, kỳ trước đó sẽ tự động đóng lại.
2. **Quản lý Lớp học phần:**
   * Mở lớp học phần gắn liền với một môn học và học kỳ đang mở.
   * Phân công Giảng viên giảng dạy từ danh sách cán bộ có quyền `GiangVien`.
   * Thiết lập lịch học: Thứ trong tuần (Thứ 2 đến Chủ nhật), Tiết bắt đầu (1-12), Số tiết (1-6), Phòng học.
   * Thiết lập sĩ số tối đa (`SiSoToiDa`).
3. **Theo dõi Sĩ số Real-time:**
   * Tính toán sĩ số thực tế đang theo học (`TrangThai == DangHoc`).
   * Hiển thị tỷ lệ lấp đầy trực quan bằng thanh trạng thái 3 mức màu:
     * **Màu xanh lục:** Lớp còn nhiều chỗ trống (Dưới 70% sĩ số).
     * **Màu cam:** Lớp sắp đầy (Từ 70% đến 95% sĩ số).
     * **Màu đỏ:** Lớp đã đạt mức tối đa 100% hoặc vượt giới hạn.
4. **Bảo vệ tính toàn vẹn khi xóa:**
   * Ngăn chặn tuyệt đối việc xóa lớp học phần nếu đã có sinh viên đăng ký đang theo học.

---

## 2. Quy Trình Cập Nhật Sĩ Số Tự Động

```mermaid
sequenceDiagram
    participant User as Giáo vụ
    participant LHP as LopHocPhanView
    participant Service as LopHocPhanService
    participant Repo as LopHocPhanRepository
    participant DB as PostgreSQL Database

    User->>LHP: Xem danh sách Lớp học phần theo Học kỳ
    LHP->>Service: LayDanhSachLopHocPhanAsync(maHocKy)
    Service->>Repo: LayTheoHocKyAsync(maHocKy)
    Repo->>DB: Query LHP join DangKyHocPhan (DemSiSo)
    DB-->>Repo: Trả về danh sách kèm sĩ số thực tế
    Repo-->>Service: IReadOnlyList<LopHocPhanDisplayDto>
    Service-->>LHP: Hiển thị lên DataGrid với màu cảnh báo sĩ số
```

---

## 3. Các Tệp Mã Nguồn Triển Khai

* **Giao diện:** `src/QuanLyDKHP.App/Views/HocKyLopHocPhanView.axaml` và `.axaml.cs`.
* **Hộp thoại chỉnh sửa:** `src/QuanLyDKHP.App/Views/LopHocPhanEditDialog.axaml`.
* **ViewModel:** `src/QuanLyDKHP.App/ViewModels/HocKyLopHocPhanViewModel.cs`.
* **Tầng nghiệp vụ & Repository:** `src/QuanLyDKHP.Services/LopHocPhanService.cs`, `HocKyService.cs`, `LopHocPhanRepository.cs`, `HocKyRepository.cs`.
* **SQL bổ sung lịch học:** `db/2026-10-01_them-lich-hoc-LopHocPhan.sql`.

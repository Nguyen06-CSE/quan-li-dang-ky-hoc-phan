---
id: co-so-du-lieu-erd
title: Thiết Kế Cơ Sở Dữ Liệu & Sơ Đồ ERD
sidebar_position: 3
---

# Thiết Kế Cơ Sở Dữ Liệu & Sơ Đồ ERD

## 1. Sơ Đồ Quan Hệ Thực Thể (ERD)

Cơ sở dữ liệu được chuẩn hóa ở dạng chuẩn 3 (3NF), thiết lập các ràng buộc toàn vẹn khóa ngoại (Foreign Keys), chỉ mục tìm kiếm (Indexes) và cơ chế xóa mềm (Soft Delete).

```mermaid
erDiagram
    SINH_VIEN ||--o{ DANG_KY_HOC_PHAN : "đăng ký"
    MON_HOC ||--o{ LOP_HOC_PHAN : "thuộc môn"
    HOC_KY ||--o{ LOP_HOC_PHAN : "mở trong kỳ"
    NGUOI_DUNG ||--o{ LOP_HOC_PHAN : "giảng viên dạy"
    LOP_HOC_PHAN ||--o{ DANG_KY_HOC_PHAN : "có đăng ký"

    SINH_VIEN {
        string MaSV PK "Mã sinh viên (Khóa chính)"
        string HoTen "Họ và tên"
        date NgaySinh "Ngày sinh"
        string GioiTinh "Giới tính (Nam/Nữ)"
        string Lop "Lớp sinh hoạt"
        string KhoaHoc "Khóa học (VD: K45)"
        string Email "Thư điện tử"
        string SoDienThoai "Số điện thoại liên hệ"
        boolean IsDeleted "Trạng thái xóa mềm"
    }

    MON_HOC {
        string MaMon PK "Mã môn học"
        string TenMon "Tên môn học"
        int SoTinChiLT "Số tín chỉ lý thuyết"
        int SoTinChiTH "Số tín chỉ thực hành"
        boolean IsDeleted "Trạng thái xóa mềm"
    }

    HOC_KY {
        string MaHocKy PK "Mã học kỳ (VD: HK1_2026_2027)"
        string TenHocKy "Tên học kỳ (VD: Học kỳ 1)"
        string NamHoc "Năm học (VD: 2026-2027)"
        boolean DangMo "Học kỳ hiện hành mở đăng ký"
        date NgayBatDau "Ngày bắt đầu"
        date NgayKetThuc "Ngày kết thúc"
    }

    LOP_HOC_PHAN {
        string MaLHP PK "Mã lớp học phần"
        string MaMon FK "Khóa ngoại tham chiếu MON_HOC"
        string MaHocKy FK "Khóa ngoại tham chiếu HOC_KY"
        string MaGV FK "Khóa ngoại tham chiếu NGUOI_DUNG"
        int Thu "Thứ trong tuần (2-8)"
        int TietBatDau "Tiết bắt đầu (1-12)"
        int SoTiet "Số tiết học"
        string Phong "Phòng học"
        int SiSoToiDa "Sĩ số sinh viên tối đa"
        boolean IsDeleted "Trạng thái xóa mềm"
    }

    DANG_KY_HOC_PHAN {
        int Id PK "Khóa chính tự tăng"
        string MaSV FK "Khóa ngoại tham chiếu SINH_VIEN"
        string MaLHP FK "Khóa ngoại tham chiếu LOP_HOC_PHAN"
        datetime NgayDangKy "Thời điểm đăng ký"
        string TrangThai "DangHoc / DaHuy"
        decimal SoTienPhaiDong "Học phí tính theo tín chỉ"
        boolean DaNopTien "Trạng thái đã đóng học phí"
    }

    NGUOI_DUNG {
        string MaNguoiDung PK "Mã định danh người dùng"
        string TenDangNhap UK "Tên đăng nhập duy nhất"
        string MatKhauHash "Mật khẩu mã hóa BCrypt"
        string HoTen "Họ và tên cán bộ"
        string Email "Email cán bộ"
        string Role "Admin / TroLyGiaoVu / GiaoVuBoMon / GiangVien"
        string TrangThai "HoatDong / DaKhoa"
    }

    CAU_HINH_HE_THONG {
        int Id PK "Khóa chính"
        int SoTinChiToiThieu "Giới hạn tín chỉ tối thiểu"
        int SoTinChiToiDa "Giới hạn tín chỉ tối đa"
        decimal DonGiaTinChiLT "Đơn giá tín chỉ lý thuyết (VNĐ)"
        decimal DonGiaTinChiTH "Đơn giá tín chỉ thực hành (VNĐ)"
        boolean ChoPhepSuaNgoaiHan "Cho phép hiệu chỉnh ngoài hạn"
    }
```

---

## 2. Chi Tiết Các Bảng Dữ Liệu

### 2.1. Bảng `SinhVien` (Quản lý Hồ sơ Sinh viên)
* **Khóa chính:** `MaSV` (varchar, tối đa 20 ký tự).
* **Trường dữ liệu:** `HoTen`, `NgaySinh`, `GioiTinh`, `Lop`, `KhoaHoc`, `Email`, `SoDienThoai`.
* **Ràng buộc:** `MaSV` không rỗng, tự động chuẩn hóa chuỗi viết hoa khi nhập.
* **Audit fields:** `NgayTao`, `NgayCapNhat`, `IsDeleted` (xóa mềm).

### 2.2. Bảng `MonHoc` (Danh mục Môn học)
* **Khóa chính:** `MaMon` (varchar, tối đa 20 ký tự).
* **Trường dữ liệu:** `TenMon`, `SoTinChiLT`, `SoTinChiTH`.
* **Ràng buộc:** `SoTinChiLT >= 0`, `SoTinChiTH >= 0`, `(SoTinChiLT + SoTinChiTH) > 0`.
* **Cột tính toán:** `TongTinChi` = `SoTinChiLT + SoTinChiTH`.

### 2.3. Bảng `HocKy` (Học kỳ Đào tạo)
* **Khóa chính:** `MaHocKy` (varchar, tối đa 20 ký tự).
* **Trường dữ liệu:** `TenHocKy`, `NamHoc`, `DangMo` (chỉ 1 học kỳ được mang giá trị `true` tại một thời điểm), `NgayBatDau`, `NgayKetThuc`.

### 2.4. Bảng `LopHocPhan` (Lớp Học Phần Mở Trong Kỳ)
* **Khóa chính:** `MaLHP` (varchar, tối đa 30 ký tự).
* **Khóa ngoại:** `MaMon` trỏ về `MonHoc`, `MaHocKy` trỏ về `HocKy`, `MaGV` trỏ về `NguoiDung`.
* **Trường lịch học:** `Thu` (2-8), `TietBatDau` (1-12), `SoTiet` (1-6), `Phong`.
* **Sĩ số:** `SiSoToiDa` (int > 0). Sĩ số hiện tại được tính toán động dựa trên số bản ghi `DangKyHocPhan` có trạng thái `DangHoc`.

### 2.5. Bảng `DangKyHocPhan` (Phiếu Đăng Ký Học Phần)
* **Khóa chính:** `Id` (int, tự tăng).
* **Khóa ngoại:** `MaSV` trỏ về `SinhVien`, `MaLHP` trỏ về `LopHocPhan`.
* **Trạng thái:** `TrangThai` nhận 1 trong 2 giá trị chuẩn:
  * `DangHoc`: Đăng ký hợp lệ và đang theo học.
  * `DaHuy`: Đã thực hiện hủy đăng ký (không xóa cứng khỏi CSDL nhằm bảo toàn lịch sử giao dịch).
* **Tài chính:** `SoTienPhaiDong` (decimal), `DaNopTien` (boolean).

### 2.6. Bảng `NguoiDung` (Tài khoản Người dùng Hệ thống)
* **Khóa chính:** `MaNguoiDung`. Khóa duy nhất: `TenDangNhap`.
* **Bảo mật:** `MatKhauHash` sử dụng thuật toán băm chậm `BCrypt.Net-Next` với Salt ngẫu nhiên, ngăn chặn tấn công Rainbow Table.
* **Vai trò:** `Role` nhận 1 trong 4 giá trị enum: `Admin`, `TroLyGiaoVu`, `GiaoVuBoMon`, `GiangVien`.

### 2.7. Bảng `CauHinhHeThong` (Tham số Toàn Cục)
* Lưu trữ các tham số nghiệp vụ cấu hình động: `SoTinChiToiThieu` (mặc định 10), `SoTinChiToiDa` (mặc định 24), `DonGiaTinChiLT` (mặc định 350.000 VNĐ), `DonGiaTinChiTH` (mặc định 450.000 VNĐ).

---

## 3. Cơ Chế Audit & Xóa Mềm (Soft Delete)

Để bảo đảm tính toàn vẹn và khả năng kiểm toán dữ liệu:
* Các thực thể kế thừa giao diện `IAuditableEntity` (`NgayTao`, `NgayCapNhat`) và `ISoftDeleteEntity` (`IsDeleted`).
* Tầng EF Core tự động bắt sự kiện `SaveChangesAsync` để gán `NgayCapNhat = DateTime.UtcNow`.
* Các truy vấn mặc định áp dụng Global Query Filter:
```csharp
modelBuilder.Entity<SinhVien>().HasQueryFilter(e => !e.IsDeleted);
modelBuilder.Entity<MonHoc>().HasQueryFilter(e => !e.IsDeleted);
modelBuilder.Entity<LopHocPhan>().HasQueryFilter(e => !e.IsDeleted);
```
Khi xóa bản ghi, hệ thống chỉ cập nhật cờ `IsDeleted = true`, bảo đảm không bao giờ xảy ra tình trạng mất dữ liệu liên đới hoặc hỏng khóa ngoại lịch sử.

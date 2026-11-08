---
id: F02-quan-ly-sinh-vien
title: F02 - Quản Lý Hồ Sơ Sinh Viên
sidebar_position: 2
---

# F02 - Quản Lý Hồ Sơ Sinh Viên

## 1. Mục Tiêu & Yêu Cầu Nghiệp Vụ

Đáp ứng yêu cầu số 1 trong đề bài môn học: Quản trị toàn diện danh sách hồ sơ sinh viên, đảm bảo khả năng tra cứu nhanh chóng, lọc đa chiều và thao tác thêm/sửa/xóa mượt mà ngay cả khi quy mô dữ liệu lên tới hàng chục nghìn bản ghi.

### Các tính năng chính
1. **Thêm mới sinh viên:** Nhập Mã SV, Họ tên, Ngày sinh, Giới tính, Lớp sinh hoạt, Khóa học, Email, Số điện thoại. Tự động kiểm tra trùng lặp khóa chính `MaSV`.
2. **Cập nhật thông tin:** Cho phép chỉnh sửa thông tin cá nhân và lớp sinh hoạt của sinh viên.
3. **Xóa mềm (Soft Delete):** Đánh dấu `IsDeleted = true` để bảo toàn lịch sử đăng ký học phần và các phiếu thu học phí liên quan.
4. **Bộ lọc đa tiêu chí:** Lọc đồng thời theo Khóa học (K44, K45, K46...), Lớp sinh hoạt và Từ khóa tìm kiếm (họ tên, mã sinh viên).
5. **Điều hướng nhanh sang Đăng ký học phần:** Cho phép người dùng Double-click hoặc bấm chuột phải vào dòng sinh viên để chuyển thẳng sang màn hình Đăng ký học phần với thông tin sinh viên đó đã được chọn sẵn.

---

## 2. Các Kỹ Thuật Tối Ưu Hiệu Năng Vượt Trội

### 2.1. Lọc Cục Bộ Trên Bộ Nhớ Đệm (Client-Side In-Memory Filtering)
Thay vì thực hiện một câu truy vấn `SELECT ... WHERE` gửi lên Neon Cloud Database mỗi khi người dùng gõ một ký tự vào ô tìm kiếm:
* Dữ liệu danh sách sinh viên được nạp vào bộ nhớ RAM (`_masterList`) 1 lần duy nhất khi mở màn hình.
* Toàn bộ thao tác lọc và tìm kiếm được thực thi trên RAM thông qua `Task.Run` chạy nền với độ trễ phản hồi **0ms**.

### 2.2. Trì Hoãn Thao Tác (Debounce Timer 300ms)
Sử dụng bộ đếm thời gian Debounce 300ms. Khi người dùng nhập liên tục vào ô tìm kiếm, thuật toán chỉ kích hoạt xử lý sau khi người dùng dừng gõ 300ms, loại bỏ hoàn toàn hiện tượng kẹt luồng UI.

### 2.3. Sắp Xếp Chuẩn Tên Tiếng Việt (`VietnameseNameComparer`)
Người Việt Nam có thói quen tra cứu danh sách sắp xếp theo thứ tự: **Tên gọi -> Tên lót -> Họ**. 
* Hệ thống xây dựng lớp `VietnameseNameComparer` triển khai giao diện `IComparer<SinhVienDto>`.
* Thuật toán phân tách chuỗi họ tên, so sánh phần tên trước bằng `CultureInfo("vi-VN")`, mang lại trải nghiệm tự nhiên và chính xác tuyệt đối cho cán bộ đào tạo.

### 2.4. Ảo Hóa Giao Diện (UI Virtualization)
DataGrid được đặt trong không gian co giãn linh hoạt (`Grid.Row="*"`). Avalonia UI chỉ dựng các phần tử giao diện hiển thị trong tầm mắt người dùng (khoảng 20-30 dòng), giải phóng hoàn toàn bộ nhớ cho hàng vạn bản ghi cuộn bên dưới.

---

## 3. Các Tệp Mã Nguồn Triển Khai

* **Giao diện:** `src/QuanLyDKHP.App/Views/SinhVienView.axaml` và `SinhVienView.axaml.cs`.
* **Hộp thoại chỉnh sửa:** `src/QuanLyDKHP.App/Views/SinhVienEditDialog.axaml`.
* **ViewModel:** `src/QuanLyDKHP.App/ViewModels/SinhVienViewModel.cs`.
* **Thuật toán sắp xếp tiếng Việt:** `src/QuanLyDKHP.App/Helpers/VietnameseNameComparer.cs`.
* **Tầng nghiệp vụ & Repository:** `src/QuanLyDKHP.Services/SinhVienService.cs` và `SinhVienRepository.cs`.
* **Kiểm thử tự động:** `tests/QuanLyDKHP.Tests/SinhVienMonHocServiceTests.cs`.

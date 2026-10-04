# 🧩 Tổng quan Chức năng Hệ thống QuanLyDKHP

Tài liệu này liệt kê **toàn bộ nhóm chức năng chính** của dự án **Quản lý Đăng ký Học phần (QuanLyDKHP)**, được phân chia theo từng nhóm nghiệp vụ.

> 📌 Đặc tả chi tiết từng chức năng nằm trong các file `F0x-*.md` cùng thư mục.

---

## 📑 Mục lục

1. [Quản lý Đăng ký Học phần](#1-quản-lý-đăng-ký-học-phần)
2. [Quản lý Học phí](#2-quản-lý-học-phí)
3. [Báo cáo & Thống kê](#3-báo-cáo--thống-kê)
4. [Quản lý Danh mục (CRUD & Phân trang)](#4-quản-lý-danh-mục-crud--phân-trang)
5. [Quản lý Người dùng & Phân quyền](#5-quản-lý-người-dùng--phân-quyền)
6. [Nhập dữ liệu tự động (Import Excel)](#6-nhập-dữ-liệu-tự-động-import-excel)
7. [Dashboard & Cấu hình Hệ thống](#7-dashboard--cấu-hình-hệ-thống)

---

## 1. Quản lý Đăng ký Học phần

- **Đăng ký học phần cho Sinh viên:** Cho phép tìm kiếm sinh viên, chọn học kỳ, đăng ký/hủy đăng ký lớp học phần cho sinh viên.
- **Ràng buộc & Cảnh báo nghiệp vụ:**
  - Kiểm tra số tín chỉ tối thiểu / tối đa (thanh tiến trình tín chỉ trực quan).
  - Kiểm tra sĩ số tối đa của lớp học phần.
  - Cảnh báo trùng môn / trùng lịch và cho phép xác nhận đăng ký đè nếu cần.

---

## 2. Quản lý Học phí

- **Tính học phí:** Tự động tính học phí dựa trên số tín chỉ Lý thuyết / Thực hành và đơn giá cấu hình.
- **Xem & Thống kê học phí:** Xem chi tiết học phí theo Sinh viên, Lớp sinh hoạt hoặc Khóa học; xem số tiền đã đóng và còn nợ.
- **Tính lại học phí:** Cho phép tính lại học phí toàn bộ hoặc theo danh sách khi có sự thay đổi về cấu hình giá tín chỉ.
- **Xuất phiếu học phí:** Xuất phiếu học phí chi tiết ra file PDF hoặc xuất danh sách tổng hợp ra file Excel.

---

## 3. Báo cáo & Thống kê

- **Danh sách SV theo môn:** Lấy danh sách sinh viên đăng ký theo từng môn học và học kỳ.
- **Danh sách thi:** Lập danh sách thi theo môn/lớp học phần (bao gồm các cột điểm, chữ ký, ghi chú).
- **Thống kê môn học:** Thống kê số lượng sinh viên đăng ký và số lớp học phần đang mở của từng môn.
- **In Phiếu đăng ký học phần:** Tra cứu và in kết quả đăng ký học phần của sinh viên.
- **Xuất báo cáo:** Hỗ trợ xuất dữ liệu ra file Excel và PDF.

---

## 4. Quản lý Danh mục (CRUD & Phân trang)

- **Quản lý Sinh viên:** Thêm, sửa, xóa, tìm kiếm sinh viên; lọc theo Lớp sinh hoạt, Khóa học; phân trang dữ liệu và xuất Excel.
- **Quản lý Môn học:** Thêm, sửa, xóa môn học; quản lý số tín chỉ lý thuyết/thực hành, bậc đào tạo; sắp xếp và xuất Excel.
- **Quản lý Học kỳ & Lớp học phần:**
  - Tạo/sửa học kỳ, đặt học kỳ hiện hành.
  - Tạo/sửa/xóa lớp học phần, phân công giảng viên, loại hình đào tạo, sĩ số tối đa, hình thức học online/offline.

---

## 5. Quản lý Người dùng & Phân quyền

- **Đăng nhập & Phiên làm việc:** Đăng nhập bằng tài khoản, ghi nhớ mật khẩu, kiểm tra tài khoản bị khóa.
- **Phân quyền dựa trên vai trò (RBAC):** Quản lý quyền hạn thông qua `PermissionMatrix` áp dụng cho các vai trò:
  - **Admin**
  - **Trợ lý giáo vụ**
  - **Giáo vụ bộ môn**
  - **Giảng viên**
- **Quản lý tài khoản:** Admin có thể tạo tài khoản mới, sửa thông tin, đổi mật khẩu, đổi trạng thái (Khóa/Mở khóa) người dùng.

> 🔗 Xem chi tiết ma trận phân quyền tại [`../00-architecture/PHAN-QUYEN.md`](../00-architecture/PHAN-QUYEN.md).

---

## 6. Nhập dữ liệu tự động (Import Excel)

- **Import dữ liệu tổng hợp:** Cho phép tải file Excel dữ liệu sinh viên, môn học, lớp học phần, đăng ký học phần vào hệ thống theo học kỳ chọn trước.
- **Tính toán tự động & Ghi log:** Theo dõi tiến trình import, tự động tính học phí và xuất log chi tiết (thành công, cảnh báo, lỗi) ra file text.

> 🔗 Xem chi tiết luồng import tại [`F04-import-excel.md`](F04-import-excel.md).

---

## 7. Dashboard & Cấu hình Hệ thống

- **Trang chủ (Dashboard):**
  - Hiển thị số liệu tổng quan: Tổng số SV, môn học, lớp học phần, lượt đăng ký.
  - Đối với **Giảng viên**: Hiển thị danh sách các lớp học phần được phân công giảng dạy.
- **Cấu hình hệ thống:** Cho phép thiết lập:
  - Số tín chỉ tối thiểu / tối đa.
  - Đơn giá tín chỉ lý thuyết.
  - Đơn giá tín chỉ thực hành.

---

## 🔗 Liên kết liên quan

- [Đặc tả Đăng nhập](F01-auth-login.md)
- [Đặc tả Quản lý Sinh viên](F02-quan-ly-sinh-vien.md)
- [Đặc tả Đăng ký học phần](F03-dkhp.md)
- [Đặc tả Import Excel](F04-import-excel.md)
- [Ma trận Phân quyền](../00-architecture/PHAN-QUYEN.md)
- [Sơ đồ ERD](../00-architecture/ERD.md)
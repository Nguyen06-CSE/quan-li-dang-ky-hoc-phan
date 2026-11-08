---
id: xuat-bao-cao-an-pham
title: Sản Phẩm Đầu Ra & Ấn Phẩm Báo Cáo
sidebar_position: 4
---

# Sản Phẩm Đầu Ra & Ấn Phẩm Báo Cáo

## 1. Danh Mục Các Ấn Phẩm Xuất Bản

Hệ thống hỗ trợ xuất bản các loại chứng từ và tài liệu đào tạo chính thức theo 2 định dạng tiêu chuẩn văn phòng: **PDF chuẩn trang in A4** (sử dụng thư viện `QuestPDF`) và **Bảng tính Excel** (sử dụng thư viện `ClosedXML`).

---

## 2. Chi Tiết Các Mẫu Ấn Phẩm

### 2.1. Phiếu Báo Học Phí Sinh Viên (Định dạng PDF - Khổ A4 Dọc)
* **Mục đích:** Cung cấp cho sinh viên và phụ huynh biên lai chi tiết về các khoản học phí phải nộp trong kỳ học.
* **Cấu trúc nội dung:**
  * Phần đầu (Header): Quốc hiệu, Tiêu ngữ, Tên cơ sở đào tạo (*TRƯỜNG ĐẠI HỌC ĐÀ LẠT - PHÒNG KẾ HOẠCH TÀI CHÍNH*).
  * Phần thông tin sinh viên: Họ và tên, Mã số sinh viên, Lớp sinh hoạt, Khóa học, Học kỳ áp dụng.
  * Bảng chi tiết học phí:
    * Cột 1: STT
    * Cột 2: Mã lớp học phần & Tên môn học
    * Cột 3: Số tín chỉ lý thuyết (LT) & Số tín chỉ thực hành (TH)
    * Cột 4: Đơn giá tín chỉ quy định
    * Cột 5: Thành tiền từng học phần
  * Phần tổng kết: Tổng số tín chỉ đăng ký, Tổng số tiền học phí bằng số và bằng chữ (VNĐ), Trạng thái nộp tiền.
  * Phần chân trang: Ngày xuất phiếu, Chữ ký của Người lập phiếu và Trưởng phòng Kế hoạch Tài chính.

### 2.2. Phiếu Kết Quả Đăng Ký Học Phần (Định dạng PDF - Khổ A4 Dọc)
* **Mục đích:** Xác nhận chính thức danh sách các môn học sinh viên đã đăng ký thành công trong kỳ, dùng làm căn cứ pháp lý để sinh viên vào lớp và dự thi.
* **Cấu trúc nội dung:**
  * Đơn vị chủ quản: *TRƯỜNG ĐẠI HỌC ĐÀ LẠT - KHOA CÔNG NGHỆ THÔNG TIN*.
  * Thông tin sinh viên và ngày giờ trích xuất hệ thống.
  * Bảng danh sách học phần: Tên môn, Số tín chỉ, Lịch học trong tuần (Thứ, Tiết), Giảng viên phụ trách và Phòng học.
  * Tổng số tín chỉ tích lũy trong kỳ; Cảnh báo nếu chưa đạt mức tín chỉ tối thiểu theo quy chế.

### 2.3. Danh Sách Thi Kết Thúc Học Phần (Định dạng PDF & Excel)
* **Mục đích:** Phục vụ công tác tổ chức thi của phòng Khảo thí và cán bộ coi thi.
* **Cấu trúc nội dung:**
  * Bảng danh sách thí sinh có đủ điều kiện dự thi (đã lọc các trường hợp bị cấm thi hoặc hủy học phần).
  * Cột chữ ký của sinh viên tại phòng thi.
  * Cột điểm đánh giá: Điểm chuyên cần / quá trình, Điểm thi kết thúc học phần và Điểm tổng kết.
  * Khung ký tên xác nhận của 2 Cán bộ coi thi và Cán bộ chấm thi.

### 2.4. Bảng Thống Kê Sinh Viên Theo Môn Học (Định dạng Excel)
* **Mục đích:** Phục vụ công tác quản lý của Giáo vụ bộ môn, lưu trữ văn bản đào tạo.
* **Đặc tính kỹ thuật:** Được định dạng bảng tính tự động với màu sắc nhận diện thương hiệu, tiêu đề in đậm, đường viền rõ nét và công thức tính tổng (`SUM`, `AVERAGE`) được nhúng trực tiếp vào file `.xlsx`.

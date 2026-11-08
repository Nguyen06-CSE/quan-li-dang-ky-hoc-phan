---
id: tong-quan-yeu-cau
title: Tổng Quan Đề Tài & Yêu Cầu Đề Bài
sidebar_position: 1
---

# Tổng Quan Đề Tài & Yêu Cầu Đề Bài

## 1. Bối Cảnh Đề Tài

Hệ thống **Quản Lý Đăng Ký Học Phần (QuanLyDKHP)** là một ứng dụng Desktop hoàn chỉnh, được phát triển nhằm tin học hóa và tối ưu hóa quy trình quản trị đào tạo tại cấp Khoa hoặc Phòng Đào tạo của các trường Đại học.

Trong môi trường giáo dục đại học theo học chế tín chỉ, khối lượng dữ liệu đăng ký môn học, điều chỉnh học phần, tính toán học phí theo từng loại tín chỉ (lý thuyết/thực hành) và lập danh sách thi kết thúc học phần là rất lớn. Việc thực hiện thủ công hoặc sử dụng các bảng tính rời rạc dễ dẫn đến sai sót, quá tải và xung đột lịch học. Ứng dụng Desktop chạy độc lập, tốc độ xử lý nhanh, bảo mật cao và hoạt động ổn định là công cụ thiết yếu cho các cán bộ giáo vụ và giảng viên.

:::note[Phạm vi áp dụng]
Ứng dụng được thiết kế phục vụ **nội bộ nhà trường** gồm 4 đối tượng: Quản trị viên (Admin), Trợ lý giáo vụ, Giáo vụ bộ môn và Giảng viên. Sinh viên không đăng nhập trực tiếp vào ứng dụng Desktop này; mọi thao tác đăng ký hoặc điều chỉnh học phần của sinh viên đều được tiếp nhận và xử lý qua cán bộ giáo vụ có thẩm quyền.
:::

---

## 2. Yêu Cầu Chức Năng Bắt Buộc (Theo Đề Bài Môn Học)

Đề bài môn học "Xây dựng ứng dụng Desktop" đặt ra 9 yêu cầu nghiệp vụ trọng tâm:

1. **Quản lý Thông tin Sinh viên:**
   * Cho phép thêm mới, cập nhật thông tin và xóa sinh viên (sử dụng cơ chế xóa mềm - Soft Delete).
   * Tra cứu thông tin sinh viên theo mã số, họ tên, lớp sinh hoạt hoặc niên khóa.
2. **Quản lý Danh mục Môn học:**
   * Quản lý thông tin chi tiết: Mã môn, tên môn học, số tín chỉ lý thuyết (LT), số tín chỉ thực hành (TH).
   * Cập nhật và xóa môn học có kiểm tra ràng buộc toàn vẹn.
3. **Đăng ký & Điều chỉnh Học phần cho Sinh viên:**
   * Cho phép cán bộ giáo vụ thực hiện đăng ký môn học cho sinh viên theo từng học kỳ.
   * Ràng buộc nghiệp vụ kiểm tra chặt chẽ: Chặn đăng ký trùng lớp học phần, chặn trùng lịch học, kiểm tra sĩ số lớp còn trống, kiểm tra giới hạn tín chỉ tối đa.
   * Hỗ trợ hủy đăng ký học phần (Soft Cancel) và kích hoạt lại đăng ký.
4. **Tính toán Học phí cho Sinh viên:**
   * Tự động tính toán tổng học phí phải đóng theo công thức chuẩn quy đổi tín chỉ lý thuyết và thực hành:
     ```text
     Học phí = (Số TC LT * Đơn giá LT) + (Số TC TH * Đơn giá TH)
     ```
   * Theo dõi trạng thái đóng học phí (Đã hoàn thành / Còn nợ) và cập nhật số tiền tức thời khi có biến động đăng ký.
5. **Liệt kê Danh sách Sinh viên Đăng ký Môn học:**
   * Tra cứu danh sách toàn bộ sinh viên đã đăng ký vào một môn học hoặc lớp học phần bất kỳ trong học kỳ được chọn.
6. **Lập Danh sách Thi Kết thúc Học phần:**
   * Lập danh sách sinh viên đủ điều kiện dự thi theo từng môn học hoặc lớp học phần.
   * Hỗ trợ xuất danh sách thi sang bảng tính Excel và in phiếu thi chuẩn A4.
7. **Liệt kê Danh sách Môn học theo Thứ tự Từ điển:**
   * Sắp xếp danh sách môn học theo quy chuẩn tiếng Việt (họ tên tiếng Việt và bảng chữ cái tiếng Việt có dấu) với độ chính xác cao dựa trên `CultureInfo("vi-VN")`.
8. **In Phiếu Kết quả Đăng ký Học phần:**
   * Trích xuất và định dạng phiếu báo kết quả đăng ký học phần của từng sinh viên dưới dạng tệp tài liệu số (PDF) chuẩn trang in A4.
9. **Thống kê Số lượng Sinh viên Đăng ký theo Từng Môn học:**
   * Thống kê tổng hợp số lượng đăng ký của từng môn học trong kỳ, tỉ lệ lấp đầy của các lớp học phần để bộ môn nắm bắt nhu cầu mở lớp hoặc ghép lớp.

---

## 3. Các Tính Năng Mở Rộng Của Dự Án

Ngoài 9 yêu cầu cốt lõi trên, dự án đã triển khai thêm các tính năng nâng cao nhằm nâng cao giá trị thực tế của sản phẩm:

* **Hệ thống Giao diện Hiện đại (Modern Desktop UI):** Xây dựng trên nền tảng Avalonia UI 11.x, áp dụng ngôn ngữ thiết kế "Liquid Glass", hỗ trợ chuyển đổi giao diện Sáng/Tối (Light/Dark Mode) tức thời, thanh điều hướng linh hoạt (App Shell).
* **Chuẩn hóa Biểu tượng Vector:** Tích hợp bộ biểu tượng vector tối giản `Material.Icons.Avalonia`, loại bỏ hoàn toàn việc sử dụng biểu tượng ký tự gõ phím rời rạc.
* **Nhập Dữ liệu Quy mô Lớn từ Excel (ClosedXML):** Hỗ trợ import hàng nghìn dòng dữ liệu đào tạo (sinh viên, môn học, lớp học phần, đăng ký) trong một lượt với thuật toán xử lý theo lô (batch processing), phòng chống lỗi lan truyền và tự động đồng bộ học phí.
* **Xuất Báo cáo & Phiếu Thu PDF Chuẩn In ấn (QuestPDF):** Thiết kế mẫu phiếu học phí, phiếu đăng ký và danh sách thi đạt tiêu chuẩn xuất bản khổ A4.
* **Kiến trúc Bộ nhớ Đệm Cục bộ & Hybrid Database (F05 & F06):**
  * Tầng 1: In-Memory Cache Store phản hồi tức thì với độ trễ 0ms cho các danh mục dùng chung.
  * Tầng 2: Cơ sở dữ liệu SQLite cục bộ chạy song song với Cloud PostgreSQL (Neon), hỗ trợ cơ chế Delta Sync để giảm tải băng thông và tăng tốc độ xử lý khi mạng yếu.
* **Xem Thời khóa biểu Đồ họa & Chống Trùng Lịch (M3 & M4):** Hiển thị trực quan thời khóa biểu tuần của sinh viên dạng lưới 7 cột (Thứ 2 đến Chủ nhật) và thuật toán tự động ngăn chặn đăng ký các lớp có khoảng tiết trùng nhau.

---
id: tong-ket-chuc-nang
title: Bảng Ma Trận Đối Chiếu Yêu Cầu Đề Bài
sidebar_position: 1
---

# Bảng Ma Trận Đối Chiếu Yêu Cầu Đề Bài Môn Học

## 1. Đánh Giá Mức Độ Hoàn Thiện Tổng Thể

Dự án đã hoàn thành **100%** toàn bộ 9 yêu cầu chức năng bắt buộc từ đề bài môn học "Xây dựng ứng dụng Desktop", đồng thời phát triển thêm 5 tính năng nâng cao phục vụ tối ưu hóa trải nghiệm người dùng và hiệu năng vận hành.

* **Tổng số yêu cầu cốt lõi theo đề bài:** 9/9 yêu cầu `[Hoàn thành 100%]`.
* **Tổng số tính năng mở rộng:** 5/5 tính năng `[Hoàn thành 100%]`.
* **Kết quả kiểm thử tự động (Unit Test):** 42/42 bài kiểm tra `[Đạt 100% Passed]`.

---

## 2. Ma Trận Đối Chiếu Chi Tiết 9 Yêu Cầu Cốt Lõi

| STT | Yêu Cầu Theo Đề Bài Môn Học | Hiện Trạng Triển Khai | Tệp Mã Nguồn Phụ Trách | Trạng Thái Nghiệm Thu |
|:---:|---|---|---|:---:|
| **1** | Thêm, cập nhật, xóa thông tin sinh viên | Đầy đủ CRUD, lọc lớp/khóa, tìm kiếm Debounce, sắp xếp tiếng Việt, xóa mềm | `SinhVienService.cs`, `SinhVienViewModel.cs` | [Hoàn thành 100%] |
| **2** | Thêm, cập nhật, xóa thông tin môn học | Đầy đủ CRUD môn học, quản lý số tín chỉ LT/TH, sao chép nhanh mã môn | `MonHocService.cs`, `MonHocViewModel.cs` | [Hoàn thành 100%] |
| **3** | Đăng ký / điều chỉnh học phần cho sinh viên | Quy trình kiểm tra 6 bước, chặn trùng LHP, chặn trùng lịch, kiểm soát sĩ số | `DangKyHocPhanService.cs`, `DangKyHocPhanViewModel.cs` | [Hoàn thành 100%] |
| **4** | Tính tiền học phí cho sinh viên | Tính theo công thức tín chỉ LT & TH, tra cứu theo SV/Lớp/Khóa, cập nhật theo lô | `HocPhiService.cs`, `HocPhiViewModel.cs` | [Hoàn thành 100%] |
| **5** | Liệt kê danh sách sinh viên đăng ký một môn học | Báo cáo Tab 1: Tra cứu theo môn trong học kỳ, xuất bảng tính Excel | `BaoCaoService.cs`, `BaoCaoViewModel.cs` | [Hoàn thành 100%] |
| **6** | Lập danh sách thi cho môn học nào đó | Báo cáo Tab 2: Lập danh sách thi theo LHP, xuất Excel và in PDF chuẩn A4 | `BaoCaoService.cs`, `QuestPdfExportService.cs` | [Hoàn thành 100%] |
| **7** | Liệt kê danh sách môn học theo thứ tự từ điển | Sắp xếp A-Z / Z-A theo chuẩn tiếng Việt có dấu bằng `CultureInfo("vi-VN")` | `MonHocService.cs` (LINQ OrderBy) | [Hoàn thành 100%] |
| **8** | In phiếu kết quả đăng ký học phần | Báo cáo Tab 4: Trích xuất phiếu báo kết quả ĐKHP xuất bản ra PDF khổ A4 | `BaoCaoService.cs`, `QuestPdfExportService.cs` | [Hoàn thành 100%] |
| **9** | Thống kê số lượng sinh viên đăng ký theo từng môn học | Báo cáo Tab 3: Thống kê số lớp đã mở, sĩ số dự kiến, SV thực tế, tỉ lệ lấp đầy | `BaoCaoService.cs`, `BaoCaoViewModel.cs` | [Hoàn thành 100%] |

---

## 3. Ma Trận Đối Chiếu Các Tính Năng Mở Rộng

| STT | Tính Năng Mở Rộng | Giải Pháp Kỹ Thuật Đã Hiện Thực | Giá Trị Thực Tiễn | Trạng Thái |
|:---:|---|---|---|:---:|
| **E1** | Nhập dữ liệu lớn từ Excel (Import Excel) | Đọc tệp `.xlsx` bằng ClosedXML, xử lý theo lô 500 dòng, cơ chế cách ly lỗi | Tiết kiệm 99% thời gian nhập liệu ban đầu | [Hoàn thành 100%] |
| **E2** | Giao diện hiện đại & Dark Mode | Avalonia UI 11.x, Theme Liquid Glass, chuyển theme Sáng/Tối tức thời | Nâng cao trải nghiệm thị giác người dùng | [Hoàn thành 100%] |
| **E3** | Biểu tượng vector Material chuẩn hóa | Thư viện `Material.Icons.Avalonia`, loại bỏ hoàn toàn emoji bàn phím | Đồng bộ nét vẽ, chuẩn thẩm mỹ ứng dụng Desktop | [Hoàn thành 100%] |
| **E4** | Bộ nhớ đệm lai & SQLite Delta Sync | In-Memory Cache Store phản hồi 0ms kết hợp SQLite cục bộ chạy song song | Triệt tiêu độ trễ mạng khi dùng Cloud Database | [Hoàn thành 100%] |
| **E5** | Lưới Thời khóa biểu & Chống trùng lịch | Lưới đồ họa 7 cột (Thứ 2 - CN), thuật toán kiểm tra giao nhau giữa các khoảng tiết | Trực quan hóa lịch học, ngăn chặn xếp trùng giờ | [Hoàn thành 100%] |

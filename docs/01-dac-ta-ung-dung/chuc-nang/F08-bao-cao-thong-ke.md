---
id: F08-bao-cao-thong-ke
title: F08 - Báo Cáo & Thống Kê Đào Tạo
sidebar_position: 8
---

# F08 - Báo Cáo & Thống Kê Đào Tạo

## 1. Mục Tiêu & Yêu Cầu Nghiệp Vụ

Đáp ứng trọn vẹn 4 yêu cầu nghiệp vụ trong đề bài môn học: Yêu cầu số 5 (Liệt kê SV đăng ký môn), Yêu cầu số 6 (Lập danh sách thi), Yêu cầu số 8 (In phiếu kết quả ĐKHP) và Yêu cầu số 9 (Thống kê số lượng SV theo môn).

Phân hệ được thiết kế dưới dạng giao diện đa thẻ (4 Tabs chuyên biệt), cho phép cán bộ đào tạo và giảng viên nhanh chóng trích xuất dữ liệu và xuất bản ấn phẩm.

---

## 2. Chi Tiết 4 Tab Báo Cáo

### Tab 1: Danh Sách Sinh Viên Đăng Ký Theo Môn Học (Yêu Cầu Số 5)
* **Chức năng:** Cho phép chọn Học kỳ và Môn học để xem toàn bộ danh sách sinh viên đã đăng ký học môn này (tổng hợp qua tất cả các lớp học phần).
* **Thông tin hiển thị:** STT, Mã sinh viên, Họ và tên, Lớp sinh hoạt, Mã lớp học phần tham gia, Trạng thái đóng học phí.
* **Đầu ra:** Xuất bảng dữ liệu sang tệp Microsoft Excel (`.xlsx`).

### Tab 2: Lập Danh Sách Thi Kết Thúc Học Phần (Yêu Cầu Số 6)
* **Chức năng:** Lập danh sách sinh viên đủ điều kiện dự thi kết thúc học phần cho một Lớp học phần cụ thể.
* **Thông tin danh sách thi:** Số thứ tự, Mã sinh viên, Họ tên, Lớp, Chữ ký xác nhận sinh viên dự thi, Cột ghi điểm (Điểm quá trình, Điểm thi kết thúc, Điểm tổng kết).
* **Đầu ra:** Xuất bảng tính Excel phục vụ nhập điểm của giảng viên và xuất tệp PDF phiếu thi chuẩn A4 có phần ký duyệt của Trưởng khoa / Trưởng bộ môn.

### Tab 3: Thống Kê Tổng Hợp Sinh Viên Theo Môn Học (Yêu Cầu Số 9)
* **Chức năng:** Báo cáo tổng thể tình hình đào tạo toàn học kỳ, thống kê số lượng sinh viên đăng ký của từng môn học.
* **Chỉ số phân tích:** Số lượng lớp học phần đã mở, Tổng sĩ số tối đa dự kiến, Số lượng sinh viên thực tế đã đăng ký, Tỉ lệ lấp đầy (%).
* **Ứng dụng thực tiễn:** Giúp Giáo vụ bộ môn và Ban chủ nhiệm khoa phát hiện sớm các lớp học phần quá tải để mở thêm lớp mới, hoặc các lớp quá ít sinh viên để tiến hành ghép lớp hoặc hủy lớp.

### Tab 4: In Phiếu Kết Quả Đăng Ký Học Phần (Yêu Cầu Số 8)
* **Chức năng:** Tra cứu sinh viên theo mã số hoặc họ tên, hiển thị bản xem trước (Preview) toàn bộ các học phần sinh viên đã đăng ký thành công trong kỳ.
* **Nội dung phiếu:** Tiêu ngữ, Thông tin sinh viên, Bảng danh sách các môn học, Số tín chỉ LT/TH, Lịch học, Phòng học và Tổng số tín chỉ tích lũy trong kỳ.
* **Đầu ra:** Xuất file PDF chuẩn in ấn A4 (QuestPDF). Tiêu đề cơ quan đào tạo được chuẩn hóa theo đúng đơn vị: *"TRƯỜNG ĐẠI HỌC ĐÀ LẠT - KHOA CÔNG NGHỆ THÔNG TIN"*.

---

## 3. Các Tệp Mã Nguồn Triển Khai

* **Giao diện:** `src/QuanLyDKHP.App/Views/BaoCaoView.axaml` và `.axaml.cs`.
* **ViewModel:** `src/QuanLyDKHP.App/ViewModels/BaoCaoViewModel.cs`.
* **Tầng nghiệp vụ & Repository:** `src/QuanLyDKHP.Services/BaoCaoService.cs` và `BaoCaoRepository.cs`.
* **Dịch vụ bộ nhớ cục bộ:** `src/QuanLyDKHP.Infrastructure/Services/LocalReadService.cs`.
* **Công cụ xuất ấn phẩm:** `src/QuanLyDKHP.Infrastructure/Export/ExcelExportService.cs` (ClosedXML) và `PdfExportService.cs` (QuestPDF).
* **Kiểm thử tự động:** `tests/QuanLyDKHP.Tests/BaoCaoServiceTests.cs`, `BaoCaoRepositoryTests.cs` và `ExportServiceTests.cs`.

---

## 4. Tối Ưu Hóa Toàn Vẹn & Đồng Bộ Dữ Liệu Báo Cáo (ADR-21)

Nhằm đảm bảo số liệu báo cáo, thống kê và tài liệu xuất bản luôn chính xác tuyệt đối và đồng nhất 100% giữa 4 Tabs:
1. **Khắc phục lỗi gom nhóm thực thể trên AsNoTracking:** Trong EF Core, `AsNoTracking()` không duy trì Identity Map, việc gom nhóm theo tham chiếu đối tượng `GroupBy(l => l.MonHoc)` tiềm ẩn nguy cơ trùng lặp dòng môn học hoặc gây `NullReferenceException`. Hệ thống đã chuyển đổi sang gom nhóm theo khóa tự nhiên `GroupBy(l => l.MaMon)` và ánh xạ thông tin môn học an toàn.
2. **Đồng bộ bộ lọc trạng thái và xóa mềm (Soft-delete):** Đồng nhất bộ lọc `!lhp.IsDeleted && !sv.IsDeleted && dk.TrangThai == "DangHoc"` trên toàn bộ 4 phân hệ (Danh sách theo môn, Danh sách thi, Thống kê học kỳ, Phiếu kết quả ĐKHP). Loại bỏ hoàn toàn sự chênh lệch số lượng sinh viên giữa các màn hình tra cứu.
3. **Nạp họ tên giảng viên chuẩn hóa (Instructor Enrichment):** Thay vì in mã giảng viên kỹ thuật thô (`GV01`, `gv01_pm`), hệ thống tự động đối soát `NguoiDung` để hiển thị họ tên đầy đủ (ví dụ: `TS. Nguyễn Văn A`, `ThS. Liam / Lâm`) trên giao diện, bảng tính Excel và phần tiêu đề/cột thông tin trên ấn phẩm PDF khổ A4.

---
id: kiem-thu-unit-test
title: Báo Cáo Kết Quả Kiểm Thử Tự Động (Unit Test)
sidebar_position: 2
---

# Báo Cáo Kết Quả Kiểm Thử Tự Động (Unit Test)

## 1. Tổng Quan Về Chiến Lược Kiểm Thử

Để đảm bảo tính đúng đắn của toàn bộ các quy trình nghiệp vụ đào tạo và ngăn chặn lỗi hồi quy (Regression Bugs), dự án áp dụng chiến lược kiểm thử tự động với mức độ bao phủ cao:

* **Framework kiểm thử:** `xUnit` kết hợp thư viện giả lập đối tượng phụ thuộc `Moq`.
* **Nguyên tắc kiểm thử cô lập:** Tầng nghiệp vụ (`QuanLyDKHP.Services`) chỉ phụ thuộc vào các Interface từ tầng Core. Toàn bộ các tương tác CSDL được giả lập (Mocked) thông qua các Repository Interface hoặc SQLite In-Memory Database (`Filename=:memory:`), giúp các bài kiểm thử thực thi với tốc độ cực nhanh (khoảng 1 giây cho toàn bộ bài kiểm tra) mà không cần kết nối Internet hay tạo cơ sở dữ liệu vật lý.
* **Tổng số bài kiểm thử:** **81 bài kiểm thử (Unit Tests)**.
* **Tỷ lệ vượt qua (Pass Rate):** **100% (81/81 bài kiểm tra đạt Passed)**.

---

## 2. Bảng Thống Kê Chi Tiết 81 Unit Tests Theo Phân Hệ

| Tệp Kiểm Thử (Test Suite) | Số Lượng Test | Trọng Tâm Kiểm Tra Nghiệp Vụ | Kết Quả |
|---|:---:|---|:---:|
| **`DangKyHocPhanServiceTests.cs`** | 19 | Kiểm tra tuần tự 6 bước: Chặn trùng LHP, cảnh báo trùng môn, xác nhận học lại, chặn quá sĩ số, chặn vượt tín chỉ tối đa, giao dịch nguyên tử `LuuDangKyAtomicAsync`, tái kích hoạt bản ghi cũ, hủy đăng ký và xử lý ngoại lệ. | [19/19 Passed] |
| **`SmartImportServiceTests.cs`** | 8 | **[Upgrade 1]** Phân tích dữ liệu Clipboard TSV, tự động nhận diện header và ghép họ đệm + tên, suy luận khóa học từ MSSV, kiểm chứng sức chứa & tỷ lệ lấp đầy lớp, phát hiện xung đột trùng lịch/trùng môn, nạp sinh viên nguyên tử kèm tự tạo hồ sơ và kích hoạt tính toán lại học phí. | [8/8 Passed] |
| **`HocPhiServiceTests.cs`** | 7 | Tính toán học phí kết hợp tín chỉ LT và TH, kiểm tra loại trừ học phần đã hủy, cập nhật trạng thái nợ/đã đóng, tính toán học phí theo lô và ghi nhận nộp tiền học phí. | [7/7 Passed] |
| **`SinhVienMonHocServiceTests.cs`** | 8 | Thêm mới sinh viên, sắp xếp danh sách môn học theo bảng chữ cái tiếng Việt có dấu (`CultureInfo("vi-VN")`), chặn xóa môn học đang có lớp mở, cơ chế tái kích hoạt xóa mềm Soft Delete. | [8/8 Passed] |
| **`BaoCaoServiceTests.cs`** | 6 | Lập danh sách sinh viên theo môn, danh sách thi kết thúc học phần, thống kê sĩ số theo kỳ, trích xuất phiếu kết quả ĐKHP và lọc phạm vi giảng dạy của giảng viên. | [6/6 Passed] |
| **`NguoiDungServiceTests.cs`** | 6 | Thêm mới cán bộ giáo vụ, đổi mật khẩu có kiểm tra mật khẩu cũ, phân quyền 4 vai trò, kiểm tra trùng MaGV và bảo toàn trạng thái khóa tài khoản. | [6/6 Passed] |
| **`AuthServiceTests.cs`** | 6 | Đăng nhập tài khoản hợp lệ, chặn mật khẩu sai, chặn tài khoản bị khóa (`DaKhoa`), kiểm tra thuật toán băm BCrypt và xác thực vai trò người dùng. | [6/6 Passed] |
| **`BaoCaoRepositoryTests.cs`** | 4 | Kiểm thử gom nhóm theo `MaMon` trên SQLite In-Memory, loại trừ LHP/môn học đã xóa mềm, lọc đăng ký đã hủy, nạp họ tên giảng viên lên danh sách thi và phiếu ĐKHP. | [4/4 Passed] |
| **`BackupServiceTests.cs`** | 4 | Định dạng dung lượng tệp tin bất biến văn hóa (`InvariantCulture`), kiểm tra short SHA commit Git, cơ chế sao lưu vi sai và phục hồi dữ liệu du hành thời gian. | [4/4 Passed] |
| **`ExportServiceTests.cs`** | 4 | Kiểm tra luồng sinh file Excel bằng ClosedXML (danh sách thi, danh sách sinh viên) và sinh phiếu thu PDF khổ A4 bằng QuestPDF. | [4/4 Passed] |
| **`HocKyServiceTests.cs`** | 3 | Quản lý vòng đời học kỳ, mở/đóng cổng đăng ký học phần và thiết lập học kỳ hiện hành. | [3/3 Passed] |
| **`CauHinhServiceTests.cs`** | 3 | Lấy tham số cấu hình mặc định (tín chỉ min/max, đơn giá LT/TH), cập nhật tham số và kiểm tra tính hợp lệ của đơn giá. | [3/3 Passed] |
| **`ImportExcelServiceTests.cs`** | 3 | Đọc dữ liệu file Excel 21 cột hợp lệ, bóc tách tiền tố học kỳ, phân tách dòng lỗi dữ liệu và kiểm tra cơ chế upsert đồng thời. | [3/3 Passed] |
| **TỔNG CỘNG** | **81** | **Bao phủ toàn diện logic nghiệp vụ, Smart Import Studio, tính toán học phí, sao lưu và xuất bản ấn phẩm** | **[81/81 ĐẠT (100%)]** |

---

## 3. Câu Lệnh Thực Thi Kiểm Thử

Để chạy toàn bộ hệ thống kiểm thử tự động, thực hiện lệnh sau từ thư mục gốc của dự án:

```bash
dotnet test tests/QuanLyDKHP.Tests/QuanLyDKHP.Tests.csproj
```

Kết quả hiển thị trên cửa sổ dòng lệnh:
```text
Test run for .../tests/QuanLyDKHP.Tests/bin/Debug/net8.0/QuanLyDKHP.Tests.dll (.NETCoreApp,Version=v8.0)
VSTest version 17.11.1 (arm64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    81, Skipped:     0, Total:    81, Duration: 1 s - QuanLyDKHP.Tests.dll (net8.0)
```

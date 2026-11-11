---
id: kiem-thu-unit-test
title: Báo Cáo Kết Quả Kiểm Thử Tự Động (Unit Test)
sidebar_position: 2
---

# Báo Cáo Kết Quả Kiểm Thử Tự Động (Unit Test)

## 1. Tổng Quan Về Chiến Lược Kiểm Thử

Để đảm bảo tính đúng đắn của toàn bộ các quy trình nghiệp vụ đào tạo và ngăn chặn lỗi hồi quy (Regression Bugs), dự án áp dụng chiến lược kiểm thử tự động với mức độ bao phủ cao:

* **Framework kiểm thử:** `xUnit` kết hợp thư viện giả lập đối tượng phụ thuộc `Moq`.
* **Nguyên tắc kiểm thử cô lập:** Tầng nghiệp vụ (`QuanLyDKHP.Services`) chỉ phụ thuộc vào các Interface từ tầng Core. Toàn bộ các tương tác CSDL được giả lập (Mocked) thông qua các Repository Interface, giúp các bài kiểm thử thực thi với tốc độ cực nhanh (vài giây cho toàn bộ bài kiểm tra) mà không cần kết nối Internet hay tạo cơ sở dữ liệu vật lý.
* **Tổng số bài kiểm thử:** **73 bài kiểm thử (Unit Tests)**.
* **Tỷ lệ vượt qua (Pass Rate):** **100% (73/73 bài kiểm tra đạt Passed)**.

---

## 2. Bảng Thống Kê Chi Tiết 73 Unit Tests Theo Phân Hệ

| Tệp Kiểm Thử (Test Suite) | Số Lượng Test | Trọng Tâm Kiểm Tra Nghiệp Vụ | Kết Quả |
|---|:---:|---|:---:|
| **`DangKyHocPhanServiceTests.cs`** | 19 | Kiểm tra tuần tự 6 bước: Chặn trùng LHP, cảnh báo trùng môn, xác nhận học lại, chặn quá sĩ số, chặn vượt tín chỉ tối đa, giao dịch nguyên tử `LuuDangKyAtomicAsync`, tái kích hoạt bản ghi cũ, hủy đăng ký và xử lý ngoại lệ. | [19/19 Passed] |
| **`HocPhiServiceTests.cs`** | 7 | Tính toán học phí kết hợp tín chỉ LT và TH, kiểm tra loại trừ học phần đã hủy, cập nhật trạng thái nợ/đã đóng, tính toán học phí theo lô và ghi nhận nộp tiền học phí. | [Đạt 100%] |
| **`BaoCaoServiceTests.cs`** | 6 | Lập danh sách sinh viên theo môn, danh sách thi kết thúc học phần, thống kê sĩ số theo kỳ, trích xuất phiếu kết quả ĐKHP và lọc phạm vi giảng dạy của giảng viên. | [Đạt 100%] |
| **`BaoCaoRepositoryTests.cs`** | 4 | Kiểm thử gom nhóm theo `MaMon` trên SQLite In-Memory, loại trừ LHP/môn học đã xóa mềm, lọc đăng ký đã hủy, nạp họ tên giảng viên lên danh sách thi và phiếu ĐKHP. | [Đạt 100%] |
| **`AuthServiceTests.cs`** | 6 | Đăng nhập tài khoản hợp lệ, chặn mật khẩu sai, chặn tài khoản bị khóa (`DaKhoa`), kiểm tra thuật toán băm BCrypt và xác thực vai trò người dùng. | [Đạt 100%] |
| **`SinhVienMonHocServiceTests.cs`** | 8 | Thêm mới sinh viên, sắp xếp danh sách môn học theo bảng chữ cái tiếng Việt có dấu (`CultureInfo("vi-VN")`), chặn xóa môn học đang có lớp mở, cơ chế tái kích hoạt xóa mềm Soft Delete. | [Đạt 100%] |
| **`NguoiDungServiceTests.cs`** | 6 | Thêm mới cán bộ giáo vụ, đổi mật khẩu có kiểm tra mật khẩu cũ, phân quyền 4 vai trò, kiểm tra trùng MaGV và bảo toàn trạng thái khóa tài khoản. | [Đạt 100%] |
| **`HocKyServiceTests.cs`** | 3 | Quản lý vòng đời học kỳ, mở/đóng cổng đăng ký học phần và thiết lập học kỳ hiện hành. | [Đạt 100%] |
| **`CauHinhServiceTests.cs`** | 3 | Lấy tham số cấu hình mặc định (tín chỉ min/max, đơn giá LT/TH), cập nhật tham số và kiểm tra tính hợp lệ của đơn giá. | [Đạt 100%] |
| **`ExportServiceTests.cs`** | 4 | Kiểm tra luồng sinh file Excel bằng ClosedXML (danh sách thi, danh sách sinh viên) và sinh phiếu thu PDF khổ A4 bằng QuestPDF. | [Đạt 100%] |
| **`ImportExcelServiceTests.cs`** | 3 | Đọc dữ liệu file Excel 21 cột hợp lệ, bóc tách tiền tố học kỳ, phân tách dòng lỗi dữ liệu và kiểm tra cơ chế upsert đồng thời. | [Đạt 100%] |
| **`BackupServiceTests.cs`** | 4 | Định dạng dung lượng tệp tin, kiểm tra short SHA commit Git, cơ chế sao lưu vi sai và phục hồi dữ liệu du hành thời gian. | [Đạt 100%] |
| **TỔNG CỘNG** | **73** | **Bao phủ toàn diện logic nghiệp vụ, tính toán học phí, sao lưu và xuất bản ấn phẩm** | **[73/73 ĐẠT]** |

---

## 3. Câu Lệnh Thực Thi Kiểm Thử

Để chạy toàn bộ hệ thống kiểm thử tự động, thực hiện lệnh sau từ thư mục gốc của dự án:

```bash
dotnet test tests/QuanLyDKHP.Tests/QuanLyDKHP.Tests.csproj
```

Kết quả hiển thị trên cửa sổ dòng lệnh:
```text
Passed!  - Failed:     0, Passed:    73, Skipped:     0, Total:    73, Duration: 1.2 s
```

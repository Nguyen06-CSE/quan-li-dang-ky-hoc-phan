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
* **Tổng số bài kiểm thử:** **42 bài kiểm thử (Unit Tests)**.
* **Tỷ lệ vượt qua (Pass Rate):** **100% (42/42 bài kiểm tra đạt Passed)**.

---

## 2. Bảng Thống Kê Chi Tiết 42 Unit Tests Theo Phân Hệ

| Tệp Kiểm Thử (Test Suite) | Số Lượng Test | Trọng Tâm Kiểm Tra Nghiệp Vụ | Kết Quả |
|---|:---:|---|:---:|
| **`DangKyHocPhanServiceTests.cs`** | 9 | Kiểm tra tuần tự 6 bước: Chặn trùng LHP, cảnh báo trùng môn, xác nhận học lại, chặn quá sĩ số, chặn vượt tín chỉ tối đa, tạo mới đăng ký, tái kích hoạt bản ghi cũ, hủy đăng ký và xử lý ngoại lệ bản ghi không tồn tại. | [42/42 Passed] |
| **`HocPhiServiceTests.cs`** | 4 | Tính toán học phí kết hợp tín chỉ LT và TH, kiểm tra loại trừ học phần đã hủy, cập nhật trạng thái nợ/đã đóng và tính toán học phí theo lô. | [Đạt 100%] |
| **`BaoCaoServiceTests.cs`** | 6 | Lập danh sách sinh viên theo môn, danh sách thi kết thúc học phần, thống kê sĩ số theo kỳ, trích xuất phiếu kết quả ĐKHP và lọc phạm vi giảng dạy của giảng viên. | [Đạt 100%] |
| **`AuthServiceTests.cs`** | 5 | Đăng nhập tài khoản hợp lệ, chặn mật khẩu sai, chặn tài khoản bị khóa (`DaKhoa`), kiểm tra thuật toán băm BCrypt và xác thực vai trò người dùng. | [Đạt 100%] |
| **`SinhVienMonHocServiceTests.cs`** | 4 | Thêm mới sinh viên, sắp xếp danh sách môn học theo bảng chữ cái tiếng Việt có dấu (`CultureInfo("vi-VN")`), chặn xóa môn học đang có lớp mở, cơ chế xóa mềm Soft Delete. | [Đạt 100%] |
| **`NguoiDungServiceTests.cs`** | 4 | Thêm mới cán bộ giáo vụ, đổi mật khẩu có kiểm tra mật khẩu cũ, phân quyền 4 vai trò và khóa tài khoản người dùng. | [Đạt 100%] |
| **`CauHinhServiceTests.cs`** | 3 | Lấy tham số cấu hình mặc định (tín chỉ min/max, đơn giá LT/TH), cập nhật tham số và kiểm tra tính hợp lệ của đơn giá. | [Đạt 100%] |
| **`ExportServiceTests.cs`** | 4 | Kiểm tra luồng sinh file Excel bằng ClosedXML (danh sách thi, danh sách sinh viên) và sinh phiếu thu PDF khổ A4 bằng QuestPDF. | [Đạt 100%] |
| **`ImportExcelServiceTests.cs`** | 3 | Đọc dữ liệu file Excel hợp lệ, xử lý phân tách dòng lỗi dữ liệu (Fault Isolation), kiểm tra cơ chế upsert đồng thời sinh viên và môn học. | [Đạt 100%] |
| **TỔNG CỘNG** | **42** | **Bao phủ toàn diện logic nghiệp vụ của cả 9 yêu cầu đề bài** | **[42/42 ĐẠT]** |

---

## 3. Câu Lệnh Thực Thi Kiểm Thử

Để chạy toàn bộ hệ thống kiểm thử tự động, thực hiện lệnh sau từ thư mục gốc của dự án:

```bash
dotnet test tests/QuanLyDKHP.Tests/QuanLyDKHP.Tests.csproj
```

Kết quả hiển thị trên cửa sổ dòng lệnh:
```text
Passed!  - Failed:     0, Passed:    42, Skipped:     0, Total:    42, Duration: 2.1 s
```

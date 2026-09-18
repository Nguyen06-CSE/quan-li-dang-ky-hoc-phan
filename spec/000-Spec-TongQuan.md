# 000 — SPEC TỔNG QUAN DỰ ÁN

## Bối cảnh
Xây dựng ứng dụng **desktop** (không phải web) "Quản lý Đăng ký Học phần" cho một khoa đại học, dùng cho 4 nhóm người dùng nội bộ: Admin, Trợ lý giáo vụ, Giáo vụ bộ môn, Giảng viên. Sinh viên không dùng app này trực tiếp.

Đây là **giai đoạn 1 (nền tảng)**: mục tiêu là đáp ứng đúng và đủ các yêu cầu chức năng bên dưới, kiến trúc code sạch để mở rộng ở giai đoạn sau, không cần thêm tính năng ngoài phạm vi.

## Yêu cầu chức năng bắt buộc (nguồn: đề bài môn học)
1. Thêm, cập nhật, xóa thông tin sinh viên.
2. Thêm, cập nhật, xóa thông tin môn học.
3. Đăng ký / điều chỉnh học phần cho sinh viên.
4. Tính tiền học phí cho sinh viên.
5. Liệt kê danh sách sinh viên đăng ký một môn học nào đó.
6. Lập danh sách thi cho môn học nào đó.
7. Liệt kê danh sách môn học theo thứ tự từ điển (theo tên môn).
8. In phiếu kết quả đăng ký học phần.
9. Thống kê số lượng sinh viên đăng ký theo từng môn học.

## Yêu cầu bổ sung của chủ dự án
- Ứng dụng phải là desktop app thật sự (không phải web đội lốt desktop) — dùng Avalonia UI, có menu strip kiểu ứng dụng truyền thống (File/Edit/View...).
- Có chức năng **import dữ liệu từ file Excel** (cấu trúc cột theo file mẫu — xem `015-Spec-Import-Excel.md`).
- Có đăng nhập, phân quyền theo 4 role.
- Có export báo cáo ra Excel/PDF.

## Ngăn xếp công nghệ (đã chốt)
- UI Framework: **Avalonia UI (.NET 8)**, mẫu kiến trúc **MVVM** (dùng `CommunityToolkit.Mvvm`).
- CSDL: **PostgreSQL**, giai đoạn test host trên **Neon**, sau này có thể chuyển local. Kết nối qua **Entity Framework Core** + `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Đọc/ghi Excel: **ClosedXML**.
- Xuất PDF: **QuestPDF**.
- Hash mật khẩu: **BCrypt.Net-Next**.

## Cách đọc bộ spec này
Đọc và triển khai theo đúng **thứ tự số** của file, vì các file sau phụ thuộc vào kết quả của file trước:

```
000 Tổng quan               (file này)
001 Kiến trúc & cấu trúc project
002 Cơ sở dữ liệu (entities, migrations)
003 Phân quyền (role, quyền theo màn hình)
004 Design system / giao diện chung (màu, style, layout khung)
005 Màn hình Đăng nhập
006 Màn hình Trang chủ (Dashboard)
007 Màn hình Sinh viên (CRUD)
008 Màn hình Môn học (CRUD)
009 Màn hình Học kỳ / Lớp học phần
010 Màn hình Đăng ký / Điều chỉnh học phần
011 Màn hình Tính học phí
012 Màn hình Báo cáo / Thống kê
013 Màn hình Cấu hình hệ thống
014 Màn hình Quản lý người dùng
015 Chức năng Import Excel
016 Chức năng Export báo cáo (Excel/PDF)
017 Lộ trình triển khai & tiêu chí hoàn thành (Definition of Done)
```

## Nguyên tắc bắt buộc khi code
- **Không** viết logic nghiệp vụ trong code-behind của View. Toàn bộ logic (tính học phí, validate số tín chỉ, kiểm tra trùng đăng ký...) đặt trong tầng `Services`.
- Mọi truy vấn dữ liệu đi qua tầng `Repository`, không gọi `DbContext` trực tiếp từ ViewModel.
- Mọi thao tác xóa dữ liệu có liên kết lịch sử (SV đã có đăng ký, môn đã có LHP...) dùng **soft delete** (cột `IsDeleted`), không xóa cứng.
- Đơn vị tiền tệ: VNĐ, kiểu `numeric(12,2)` trong DB, hiển thị có dấu phân cách hàng nghìn.
- Toàn bộ chuỗi hiển thị cho người dùng bằng **tiếng Việt có dấu**.
- Sau khi hoàn thành mỗi file spec (mỗi màn hình/module), build lại project để đảm bảo không lỗi trước khi chuyển sang file spec tiếp theo.

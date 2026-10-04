# PHÂN QUYỀN HỆ THỐNG

> ⚠️ **Tài liệu tham chiếu** — Chỉnh bảng dưới đây khi có xác nhận từ giảng viên, sau đó cập nhật `PermissionMatrix.cs` trong code cho khớp.

## 4 vai trò (Role)

| # | Enum Value      | Tên hiển thị      |
|---|----------------|--------------------|
| 0 | `Admin`        | Quản trị viên      |
| 1 | `TroLyGiaoVu`  | Trợ lý giáo vụ    |
| 2 | `GiaoVuBoMon`  | Giáo vụ bộ môn    |
| 3 | `GiangVien`    | Giảng viên         |

## Ma trận quyền theo chức năng

| Chức năng                        | Admin | Trợ lý giáo vụ | Giáo vụ bộ môn | Giảng viên |
|----------------------------------|:-----:|:---------------:|:--------------:|:----------:|
| Đăng nhập                        | ✅    | ✅              | ✅             | ✅         |
| Xem Dashboard                    | ✅    | ✅              | ✅             | ✅ *(số liệu giới hạn phạm vi phụ trách)* |
| CRUD Sinh viên                   | ✅    | ✅              | ✅ *(chưa rõ — tạm = TrợLýGiáoVụ)* | ❌ |
| CRUD Môn học                     | ✅    | ✅              | ✅             | ❌         |
| Đăng ký / Điều chỉnh học phần   | ✅    | ✅              | ✅             | ❌         |
| Tính học phí                     | ✅    | ✅              | ✅             | ❌         |
| Xem học phí                      | ✅    | ✅              | ✅             | ✅ *(chỉ xem)* |
| Xem DS SV đăng ký theo môn      | ✅    | ✅              | ✅             | ✅ *(chỉ môn/LHP mình dạy)* |
| Lập danh sách thi                | ✅    | ✅              | ✅             | ✅ *(chỉ môn/LHP mình dạy)* |
| Nhập điểm                        | ❌    | ❌              | ✅ *(tùy xác nhận)* | ✅ *(chỉ LHP mình dạy)* |
| In phiếu kết quả ĐKHP           | ✅    | ✅              | ✅             | ❌         |
| Thống kê SV theo môn             | ✅    | ✅              | ✅             | ✅ *(giới hạn phạm vi)* |
| Cấu hình hệ thống (số TC, đơn giá) | ✅ | ❌              | ❌             | ❌         |
| Quản lý người dùng              | ✅    | ❌              | ❌             | ❌         |
| Import Excel                    | ✅    | ✅              | ❌             | ❌         |

## Ghi chú — Các mục chưa rõ

1. **CRUD Sinh viên → Giáo vụ bộ môn**: Spec ghi "*giới hạn theo môn/lớp phụ trách — chưa rõ tiêu chí lọc*". Tạm thời cho phép = Trợ lý giáo vụ (không giới hạn). Cần xác nhận lại với giảng viên.

2. **Nhập điểm → Giáo vụ bộ môn**: Spec ghi "*(tùy xác nhận)*". Tạm thời cho phép. Cần xác nhận lại.

3. **Giảng viên**: Nhiều chức năng ghi "*chỉ môn/LHP mình dạy*" hoặc "*giới hạn phạm vi phụ trách*". Ở mức PermissionMatrix chỉ cho phép truy cập; việc lọc dữ liệu theo phạm vi sẽ thực hiện ở tầng Service (filter theo `MaGV`).

## Cách hoạt động trong code

- **Nguồn sự thật duy nhất**: `Core/Authorization/PermissionMatrix.cs`
- **Constants**: `Core/Authorization/ChucNang.cs`
- **Service theo dõi user**: `Core/Interfaces/ICurrentUserService.cs` → triển khai `Services/CurrentUserService.cs`
- **Menu lọc động**: `App/ViewModels/MainWindowViewModel.cs` — lọc menu dựa trên `PermissionMatrix.HasPermission()`
- **Service kiểm tra kép**: Mỗi method trong tầng Service gọi `PermissionMatrix.Authorize()` trước khi thực thi

Khi cần sửa quyền: chỉ sửa `PermissionMatrix.cs` + cập nhật file `PHAN-QUYEN.md` này.

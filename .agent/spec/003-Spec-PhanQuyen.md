# 003 — SPEC PHÂN QUYỀN

> ⚠️ Quyền chi tiết dưới đây là **đề xuất tạm thời**, chủ dự án sẽ xác nhận lại với giảng viên. Agent cần cài đặt theo cách **dễ sửa** (bảng ma trận quyền tập trung 1 chỗ), không rải rác điều kiện `if role == ...` khắp code.

## 4 vai trò (enum `UserRole` trong `Core/Enums`)
```csharp
public enum UserRole
{
    Admin,
    TroLyGiaoVu,
    GiaoVuBoMon,
    GiangVien
}
```

## Ma trận quyền theo màn hình/chức năng

| Chức năng | Admin | Trợ lý giáo vụ | Giáo vụ bộ môn | Giảng viên |
|---|:---:|:---:|:---:|:---:|
| Đăng nhập | ✅ | ✅ | ✅ | ✅ |
| Xem Dashboard | ✅ | ✅ | ✅ | ✅ (số liệu giới hạn phạm vi phụ trách) |
| CRUD Sinh viên | ✅ | ✅ | ✅ (giới hạn theo môn/lớp phụ trách — *chưa rõ tiêu chí lọc, để mặc định = không giới hạn cho tới khi xác nhận*) | ❌ |
| CRUD Môn học | ✅ | ✅ | ✅ | ❌ |
| Đăng ký/Điều chỉnh học phần | ✅ | ✅ | ✅ | ❌ |
| Tính học phí | ✅ | ✅ | ✅ | ❌ (chỉ xem) |
| Xem DS SV đăng ký theo môn | ✅ | ✅ | ✅ | ✅ (chỉ môn/LHP mình dạy) |
| Lập DS thi | ✅ | ✅ | ✅ | ✅ (chỉ môn/LHP mình dạy) |
| Nhập điểm | ❌ | ❌ | ✅ (tùy xác nhận) | ✅ (chỉ LHP mình dạy) |
| In phiếu kết quả ĐKHP | ✅ | ✅ | ✅ | ❌ |
| Thống kê SV theo môn | ✅ | ✅ | ✅ | ✅ (giới hạn phạm vi) |
| Cấu hình hệ thống (số TC, đơn giá) | ✅ | ❌ | ❌ | ❌ |
| Quản lý người dùng | ✅ | ❌ | ❌ | ❌ |
| Import Excel | ✅ | ✅ | ❌ | ❌ |

Ghi chú: ô đánh dấu "*chưa rõ*" — cài đặt tạm với giá trị an toàn nhất (cho phép như Trợ lý giáo vụ) và đánh dấu bằng comment `// TODO: xác nhận phạm vi quyền GiaoVuBoMon` trong code.

## Cách cài đặt kỹ thuật

1. Tạo lớp tĩnh `PermissionMatrix` trong `Core` — 1 `Dictionary<string, UserRole[]>` map tên chức năng (constant string) → danh sách role được phép. Đây là **nguồn sự thật duy nhất**.
2. Tạo `ICurrentUserService` (Infrastructure/App) lưu thông tin user đang đăng nhập (Id, Role) trong suốt phiên làm việc (Singleton).
3. Trong `MainWindowViewModel`, danh sách mục Sidebar/Menu strip được **lọc động** dựa trên `PermissionMatrix` + `ICurrentUserService.CurrentUser.Role` — role không có quyền thì mục menu không hiển thị (không phải hiển thị rồi disable).
4. Với các nút hành động trong từng màn hình (Thêm/Sửa/Xóa...), dùng property `bool CanXxx => PermissionMatrix.Check(...)` binding vào `IsVisible`/`IsEnabled` của control tương ứng.
5. Tuyệt đối không kiểm tra quyền chỉ ở UI — tầng `Services` cũng phải kiểm tra lại quyền trước khi thực thi (throw `UnauthorizedAccessException` nếu vi phạm) để tránh trường hợp gọi thẳng service mà bỏ qua UI.

## File cần tạo để chủ dự án chỉnh sau này
Tạo file `docs/PHAN-QUYEN.md` (không phải code) liệt kê lại đúng bảng ma trận ở trên dưới dạng dễ đọc, kèm ghi chú các mục "chưa rõ" — để chủ dự án chỉnh tay khi có xác nhận từ giảng viên, sau đó cập nhật `PermissionMatrix` trong code theo file này.

## Checklist cho agent
- [ ] Tạo `UserRole` enum.
- [ ] Tạo `PermissionMatrix` với đầy đủ các chức năng liệt kê ở bảng trên.
- [ ] Tạo `ICurrentUserService` + triển khai.
- [ ] Áp dụng lọc menu theo quyền ở `MainWindowViewModel`.
- [ ] Tạo file `docs/PHAN-QUYEN.md`.

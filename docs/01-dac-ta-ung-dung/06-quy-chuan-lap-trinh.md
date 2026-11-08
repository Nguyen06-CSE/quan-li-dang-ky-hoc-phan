---
id: quy-chuan-lap-trinh
title: Quy Chuẩn Lập Trình & Phát Triển Phần Mềm
sidebar_position: 6
---

# Quy Chuẩn Lập Trình & Phát Triển Phần Mềm

## 1. Tiêu Chuẩn Mã Nguồn C# 12 & .NET 8

Mã nguồn dự án áp dụng các tính năng hiện đại nhất của C# 12 và .NET 8 nhằm tối ưu hóa tính ngắn gọn, hiệu năng thực thi và độ an toàn kiểu:

* **File-Scoped Namespaces:** Sử dụng khai báo không gian tên phạm vi tệp (`namespace QuanLyDKHP.Core.Entities;`) để giảm độ thụt dòng thừa.
* **Biểu thức Khởi tạo Hiện đại (Collection Expressions):** Sử dụng cú pháp `[]` để khởi tạo danh sách, mảng thay cho `new List<T>()`.
* **Primary Constructors:** Khai báo constructor trực tiếp trên định nghĩa lớp trong trường hợp tiêm phụ thuộc đơn giản.
* **Bắt buộc Xử lý Bất đồng bộ (Async/Await Everywhere):** Toàn bộ các thao tác truy vấn cơ sở dữ liệu, đọc/ghi tệp Excel và sinh PDF đều phải là phương thức bất đồng bộ mang hậu tố `Async` và hỗ trợ `CancellationToken`.

```csharp
public async Task<IReadOnlyList<SinhVienDto>> LayDanhSachSinhVienAsync(
    string? tuKhoa, 
    CancellationToken cancellationToken = default)
{
    await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
    return await context.SinhViens
        .AsNoTracking()
        .Where(sv => !sv.IsDeleted)
        .Select(sv => new SinhVienDto { ... })
        .ToListAsync(cancellationToken);
}
```

---

## 2. Quy Chuẩn Đặt Tên (Naming Conventions)

| Thành phần | Quy tắc | Ví dụ |
|---|---|---|
| Lớp, Interface, Struct, Enum | PascalCase | `SinhVienService`, `IDangKyHocPhanRepository` |
| Phương thức, Thuộc tính | PascalCase | `TinhLaiHocPhiAsync`, `TongTinChi` |
| Biến thành viên riêng (Private Field) | camelCase có tiền tố gạch dưới `_` | `_contextFactory`, `_memoryCacheStore` |
| Tham số phương thức, Biến cục bộ | camelCase | `maSinhVien`, `hocKyHienHanh` |
| Tên Hằng số (Constants) | PascalCase | `ChucNang.CrudSinhVien`, `DefaultPageSize` |

---

## 3. Quy Chuẩn Lập Trình MVVM & Giao Diện Avalonia

* **Không viết mã xử lý nghiệp vụ trong Code-Behind (`.axaml.cs`):** Code-behind chỉ được dùng cho việc khởi tạo thành phần giao diện đồ họa (`InitializeComponent()`) hoặc xử lý các sự kiện phụ thuộc vòng đời cửa sổ (Window Lifecycle, File Picker Dialog).
* **Quản lý Luồng UI (Thread Dispatching):** Mọi thao tác cập nhật dữ liệu liên kết từ tiến trình nền phải được chuyển tiếp an toàn về UI Thread thông qua `Dispatcher.UIThread.Post(...)` hoặc async/await đúng ngữ cảnh.
* **Tránh rò rỉ bộ nhớ Sự kiện (Event Subscription Leak):** Tuyệt đối không đăng ký sự kiện trực tiếp giữa các ViewModel; bắt buộc sử dụng `WeakReferenceMessenger` để tự động dọn dẹp khi ViewModel không còn được dùng.

---

## 4. Quy Chuẩn Quản Lý Phiên Bản Git (Conventional Commits)

Toàn bộ lịch sử cam kết mã nguồn của nhóm tuân thủ nghiêm ngặt định dạng **Conventional Commits**:

```text
<loại_thay_đổi>(<phạm_vi>): <mô tả ngắn gọn bằng tiếng Việt hoặc tiếng Anh>
```

Các tiền tố được áp dụng:
* `feat`: Thêm mới một tính năng hoặc màn hình nghiệp vụ (VD: `feat(hocphi): tối ưu lại tốc độ`).
* `fix`: Sửa chữa một lỗi phần mềm (VD: `fix(dkhp): Sửa lỗi StackOverflowException do Binding Loop`).
* `perf`: Nâng cấp cải thiện hiệu năng (VD: `perf: Tối ưu hiệu suất tìm kiếm, lọc và hiển thị dữ liệu UI`).
* `docs`: Bổ sung hoặc tái cấu trúc tài liệu kỹ thuật (VD: `docs: tái cấu trúc cây thư mục tài liệu`).
* `refactor`: Tái cấu trúc mã nguồn nhưng không thay đổi hành vi bên ngoài (VD: `refactor: clean up dead files, restructure dtos`).

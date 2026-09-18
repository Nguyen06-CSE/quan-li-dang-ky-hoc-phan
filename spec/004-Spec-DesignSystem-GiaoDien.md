# 004 — SPEC DESIGN SYSTEM & KHUNG GIAO DIỆN CHUNG

Phong cách tham khảo: online.dlu.edu.vn / lms.dlu.edu.vn — nghiêm túc, tối giản hiện đại, mềm mại.

## 4.1 Bảng màu (định nghĩa trong `Styles/Colors.axaml` dạng `SolidColorBrush` resource)

```xml
<Color x:Key="ColorPrimary">#ACD26B</Color>            <!-- rgb(172,210,107) -->
<Color x:Key="ColorPrimaryDark">#8CB450</Color>
<Color x:Key="ColorPrimaryLight">#E6F2D6</Color>
<Color x:Key="ColorBackground">#FFFFFF</Color>
<Color x:Key="ColorSurfaceAlt">#F7F9F5</Color>
<Color x:Key="ColorTextPrimary">#212521</Color>
<Color x:Key="ColorTextSecondary">#6E766E</Color>
<Color x:Key="ColorBorder">#E0E6DA</Color>
<Color x:Key="ColorDanger">#D64541</Color>
<Color x:Key="ColorWarning">#E0A83C</Color>
<Color x:Key="ColorSuccess">#60A358</Color>
```
Tạo tương ứng các `SolidColorBrush` cùng tên (bỏ prefix `Color`, dùng `Brush`, VD `BrushPrimary`) để bind trực tiếp vào `Background`/`Foreground`.

## 4.2 Typography
- `FontFamily` mặc định toàn app: `Segoe UI, Inter, sans-serif` (khai báo ở `App.axaml` cấp Application).
- Style dùng chung đặt tên theo class: `.h1` (22px SemiBold), `.h2` (16px SemiBold), `.body` (14px Regular), `.caption` (12px, `BrushTextSecondary`).

## 4.3 Style control dùng chung (trong `Styles/Controls.axaml`)
- **Button chính** (class `.btn-primary`): nền `BrushPrimary`, chữ trắng, bo góc 6px, padding 10x16, hover → `BrushPrimaryDark`.
- **Button phụ** (class `.btn-secondary`): nền trong suốt, viền `BrushBorder`, chữ `BrushTextPrimary`, hover nền `BrushSurfaceAlt`.
- **Button nguy hiểm** (class `.btn-danger`): nền `BrushDanger`, chữ trắng.
- **TextBox/ComboBox**: viền `BrushBorder` 1px, bo góc 6px, padding 8x10, khi Focus → viền `BrushPrimary` 2px.
- **DataGrid**: header nền `BrushSurfaceAlt` chữ đậm `BrushTextPrimary`; hàng hover nền `BrushPrimaryLight`; hàng chọn nền `BrushPrimaryLight` đậm hơn + viền trái 3px `BrushPrimary`.
- **Card/Panel**: nền trắng, bo góc 10px, `BoxShadow="0 1 3 0 #14000000"`, padding 16-24.
- **Toast/Snackbar**: góc dưới-phải, bo góc 8px, nền theo loại (`Success`/`Danger`/`Warning`), tự ẩn sau 3 giây (dùng `DispatcherTimer`).

## 4.4 Khung ứng dụng — `MainWindow.axaml`

Cấu trúc layout dùng `Grid` với `RowDefinitions="56,36,*"` cho phần trên và `ColumnDefinitions="220,*"` cho phần dưới:

```
Row 0 (Header, cao 56, nền BrushPrimary, toàn chiều rộng):
  [Logo + "QUẢN LÝ ĐĂNG KÝ HỌC PHẦN"]  ---spacer---  [Học kỳ: <combobox>]   [Tên user | Role]  [Đăng xuất]

Row 1 (Menu strip, cao 36, nền BrushSurfaceAlt, viền dưới BrushBorder, toàn chiều rộng):
  Menu ngang kiểu truyền thống, dùng Avalonia's Menu control:
  Tệp | Danh mục | Đăng ký | Báo cáo | Trợ giúp
  - Tệp: Import Excel, Đăng xuất, Thoát
  - Danh mục: Sinh viên, Môn học, Học kỳ/Lớp học phần
  - Đăng ký: Đăng ký/Điều chỉnh học phần, Tính học phí
  - Báo cáo: DS SV theo môn, DS thi, Thống kê SV theo môn, In phiếu ĐKHP
  - Trợ giúp: Giới thiệu, Phiên bản

Row 2 (chia 2 cột):
  Col 0 (Sidebar, rộng 220, nền BrushSurfaceAlt):
    ListBox điều hướng dọc, các mục lọc theo quyền (xem 003-Spec-PhanQuyen.md):
    • Trang chủ
    • Sinh viên
    • Môn học
    • Học kỳ / Lớp học phần
    • Đăng ký học phần
    • Học phí
    • Báo cáo
    • Cấu hình (chỉ Admin)
    • Người dùng (chỉ Admin)
    Mục đang chọn: nền BrushPrimaryLight, chữ đậm, viền trái 3px BrushPrimary.

  Col 1 (Vùng nội dung, nền BrushBackground, padding 24):
    <ContentControl Content="{Binding CurrentViewModel}" />
    (DataTemplate map ViewModel → View đặt trong App.axaml resources)
```

> Lý do giữ **cả sidebar và menu strip**: menu strip đóng vai trò khẳng định "app desktop truyền thống" (đúng yêu cầu khắt khe của giảng viên), sidebar đóng vai trò điều hướng chính nhanh bằng chuột — hai thành phần không trùng lặp chức năng vì menu strip có thêm các lệnh thao tác (Import, Thoát...) mà sidebar không có.

## 4.5 Quy ước dialog (form Thêm/Sửa, xác nhận xóa)
- Mở dưới dạng `Window` con, `WindowStartupLocation=CenterOwner`, không có nút Maximize, có thể Resize theo chiều dọc nếu form dài.
- Luôn có 2 nút cuối form: `.btn-secondary` "Hủy" (trái) và `.btn-primary` "Lưu" (phải).
- Dialog xác nhận xóa: tiêu đề "Xác nhận xóa", nội dung mô tả rõ đối tượng sắp xóa, nút `.btn-danger` "Xóa" + `.btn-secondary` "Hủy".

## Checklist cho agent
- [ ] Tạo `Styles/Colors.axaml`, `Styles/Controls.axaml`, `Styles/Typography.axaml`, include vào `App.axaml`.
- [ ] Dựng `MainWindow.axaml` đúng layout Header/MenuStrip/Sidebar/Content nêu trên (chưa cần nội dung từng màn hình, chỉ khung + điều hướng chạy được, có thể tạm hiển thị `Trang chủ` mặc định).
- [ ] Cài đặt `IDialogService` (mở Window con từ ViewModel) dùng chung cho toàn bộ dialog ở các spec sau.
- [ ] Cài đặt Toast/Snackbar dùng chung (VD `INotificationService`).

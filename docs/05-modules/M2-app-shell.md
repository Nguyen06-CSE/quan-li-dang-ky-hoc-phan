# M2 — App Shell / Navigation

**Trạng thái: ✅ Hoàn thành**

## Đã làm

- Sidebar thu gọn/mở rộng (animated width), dải màu chỉ-mục ("indicator") trượt theo mục menu đang chọn.
- Top bar: breadcrumb, tìm kiếm nhanh màn hình (bỏ dấu tiếng Việt khi so khớp), chuông thông báo, nút đổi Dark mode,
  avatar + tên + vai trò, đăng xuất.
- Điều hướng theo kiểu **ViewModel-first**: đổi `MainWindowViewModel.CurrentViewModel` → `ContentControl` hiển thị View
  tương ứng; các ViewModel đã tạo được cache lại trong `_viewModelCache` để chuyển màn gần như tức thời.
- Hiệu ứng chuyển trang (`TransitioningContentControl` + `CrossFade`), fade overlay khi đổi theme sáng/tối.
- Toàn bộ giao diện (Login, MainWindow, và **tất cả** các màn còn lại — Sinh viên, Môn học, Học kỳ/LHP, Đăng ký học phần,
  Học phí, Báo cáo, Import Excel, Cấu hình, Người dùng) đã chuyển sang theme **"Liquid / iOS Material"**: nền kính mờ,
  góc bo lớn, bóng đổ mềm, nút có hiệu ứng nảy khi hover. Toàn bộ màu sắc/animation/hover được gom vào
  `Styles/Colors.axaml` (token theo `ThemeDictionaries` Light/Dark) và `Styles/Controls.axaml` (các `Classes` dùng chung:
  `glass-panel`, `glass-dialog`, `btn-primary`, `btn-secondary`, `badge-*`, `status`/`status.error`, v.v.) — không
  hard-code màu/animation trực tiếp trên từng control.
- Nút "Thao tác nhanh" ở Dashboard (Import Excel / Quản lý SV / Quản lý Môn học) đã nối đúng vào luồng điều hướng
  (trước đó bấm không có phản ứng gì — xem log sửa lỗi).

## File liên quan

- `src/QuanLyDKHP.App/MainWindow.axaml` (+ `.axaml.cs`)
- `src/QuanLyDKHP.App/ViewModels/MainWindowViewModel.cs`
- `src/QuanLyDKHP.App/Styles/Colors.axaml`, `Styles/Controls.axaml`
- Toàn bộ `src/QuanLyDKHP.App/Views/*.axaml`

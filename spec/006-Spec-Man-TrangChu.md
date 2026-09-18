# 006 — SPEC MÀN HÌNH TRANG CHỦ (DASHBOARD)

## Mục tiêu
Màn hình mặc định sau khi đăng nhập, cho cái nhìn tổng quan nhanh + lối tắt tới thao tác thường dùng.

## Giao diện (`DashboardView.axaml`)
1. Tiêu đề `.h1`: "Trang chủ".
2. Hàng 4 thẻ số liệu (dùng `UniformGrid Columns="4"`, mỗi thẻ là Card style `.card`, nền `BrushPrimaryLight`):
   - Tổng số Sinh viên
   - Tổng số Môn học
   - Tổng số Lớp học phần đang mở (học kỳ hiện hành)
   - Tổng số lượt đăng ký (học kỳ hiện hành)
   Mỗi thẻ: số lớn `.h1` màu `BrushTextPrimary` + label `.caption` bên dưới.
3. Khối "Học kỳ hiện hành": hiển thị `TenHocKy` + ComboBox cho phép đổi học kỳ đang xem số liệu (không đổi học kỳ hệ thống, chỉ đổi ngữ cảnh xem dashboard).
4. (Chỉ role Admin/TroLyGiaoVu/GiaoVuBoMon) Hàng nút lối tắt `.btn-secondary`: "Import Excel", "+ Thêm sinh viên", "+ Thêm môn học" — điều hướng sang màn hình tương ứng.
5. (Role GiangVien) Thay thế mục 4 bằng danh sách rút gọn "Các lớp học phần bạn đang dạy" (bảng nhỏ: Mã LHP, Môn, Sĩ số).

## ViewModel (`DashboardViewModel`)
- Load số liệu qua `BaoCaoService` khi `OnActivated`/constructor (query COUNT đơn giản theo học kỳ đang chọn).
- Property `HocKyDangXem`, danh sách `DsHocKy` load từ `HocKyService`/repository.
- Command điều hướng lối tắt: gọi `MainWindowViewModel` (qua sự kiện hoặc injected navigation service) để đổi `CurrentViewModel`.

## Checklist cho agent
- [ ] `DashboardView.axaml` + `DashboardViewModel`.
- [ ] Query 4 số liệu thẻ (COUNT SinhVien, COUNT MonHoc, COUNT LopHocPhan theo học kỳ, COUNT DangKyHocPhan theo học kỳ).
- [ ] Áp dụng hiển thị khác nhau theo Role (mục 5 vs mục 4) dùng `PermissionMatrix`/`ICurrentUserService`.

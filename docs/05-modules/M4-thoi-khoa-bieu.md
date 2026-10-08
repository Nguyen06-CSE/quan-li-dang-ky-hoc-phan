# M4 — Thời khóa biểu

**Trạng thái: ✅ Hoàn thành**

## Đã làm

- Dialog mới `ThoiKhoaBieuDialog`, mở từ màn **Đăng ký học phần** (State View — khi đã chọn 1 sinh viên) qua nút
  "Xem Thời khóa biểu" cạnh nút "Chỉnh sửa Đăng ký".
- Lưới 7 cột Thứ 2 → CN, dựng **hoàn toàn từ `DsDaDangKy` đã có sẵn trong bộ nhớ** của `DangKyHocPhanViewModel`
  (các LHP `TrangThai = DangHoc` của sinh viên đang chọn) — không query thêm DB.
- Mỗi ô lớp hiển thị Tên môn, Mã LHP, Phòng (nếu có) và khoảng tiết (`Tiết {TietBatDau}-{TietKetThuc}`), xếp dọc
  theo thứ tự tiết tăng dần trong từng cột Thứ.
- LHP nào **chưa xếp lịch** (`Thu`/`TietBatDau`/`SoTiet` còn NULL) **không vẽ lên lưới** — liệt kê riêng ở khung
  "Lớp chưa xếp lịch (không hiển thị trên lưới)" bên dưới lưới, để sinh viên/admin biết vẫn còn lớp thiếu dữ liệu lịch
  học chứ không bị "biến mất" âm thầm. Đúng theo nguyên tắc không bịa dữ liệu giả.

## Thiết kế

- Không thêm bảng/cột DB mới, không thêm hàm repository mới — tái sử dụng đúng field
  `Thu`/`TietBatDau`/`SoTiet`/`Phong` đã có trên `DangKyHocPhanDisplayDto.Entity.LopHocPhan`.
- `ThoiKhoaBieuDialogViewModel` nhận thẳng `List<DangKyHocPhanDisplayDto>` (đã lọc `IsDangHoc` từ ViewModel gọi) qua
  constructor — dựng cấu trúc 7 cột (`ThoiKhoaBieuCot`) + từng ô lớp (`ThoiKhoaBieuOClass`) ngay trong constructor,
  hoàn toàn client-side, không cần Service/Repository riêng.

## Giới hạn đã biết

- Lưới không giới hạn số tiết tối đa cố định theo `CauHinh` (hệ thống hiện chưa có cấu hình "số tiết/ngày") — hiển thị
  tự do theo đúng `TietBatDau`/`SoTiet` đã nhập cho từng lớp.
- Nếu 2 lớp trùng Thứ nhưng **không trùng tiết** (ví dụ lớp A tiết 1-3, lớp B tiết 4-6), cả 2 vẫn hiển thị trong cùng
  1 cột Thứ, xếp theo thứ tự tiết — không có ô lưới cố định theo từng tiết riêng lẻ (đơn giản hóa so với lưới
  tiết-cố-định truyền thống, nhưng đủ để xem lịch học tổng quan).

## File liên quan

- `src/QuanLyDKHP.App/ViewModels/ThoiKhoaBieuDialogViewModel.cs`
- `src/QuanLyDKHP.App/Views/ThoiKhoaBieuDialog.axaml`, `.axaml.cs`
- `src/QuanLyDKHP.App/ViewModels/DangKyHocPhanViewModel.cs` (`ShowThoiKhoaBieuFunc`, `XemThoiKhoaBieuCommand`)
- `src/QuanLyDKHP.App/Views/DangKyHocPhanView.axaml(.cs)`
- `src/QuanLyDKHP.App/Dtos/DangKyHocPhanDisplayDto.cs`

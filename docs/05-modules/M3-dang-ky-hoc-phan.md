# M3 — Đăng ký học phần

**Trạng thái: ⚠️ Một phần — chờ dữ liệu lịch học để làm "check trùng lịch"**

## Đã làm (dùng được ngay, dữ liệu thật)

- 5 bước kiểm tra khi đăng ký (`DangKyHocPhanService.DangKyAsync`):
  1. Chặn trùng lớp học phần (SV đã đăng ký đúng LHP này, trạng thái `DangHoc`).
  2. Cảnh báo mềm trùng môn (SV đã học môn này ở LHP khác — cho xác nhận học lại/cải thiện).
  3. Chặn vượt sĩ số tối đa.
  4. Chặn vượt tổng số tín chỉ tối đa cho phép (theo `CauHinh`).
  5. Ghi đăng ký (tạo mới hoặc tái kích hoạt bản ghi đã hủy) + gọi tính lại học phí.
- Ô chọn Lớp học phần hiển thị sĩ số dạng thanh tiến trình màu (xanh/cam/đỏ) theo tỉ lệ lấp đầy thật (`TyLeLapDay`),
  không phải dữ liệu giả.
- Debounce tìm sinh viên (300ms) khi gõ.

## Chưa làm — lý do

**"Kiểm tra trùng lịch trực tiếp"** (so giờ học giữa các lớp sinh viên đã đăng ký) cần cột lịch học
(Thứ/Tiết bắt đầu/Số tiết/Phòng) trên `LopHocPhan`, trước đây **không tồn tại** trong DB. Theo nguyên tắc không bịa dữ
liệu giả, các bước đã thực hiện:

1. Thêm 4 cột này vào `LopHocPhan` qua script riêng `db/2026-10-01_them-lich-hoc-LopHocPhan.sql` (đã chạy trên Neon).
2. Xác nhận file Excel import thật (`...ThongKeDKHP.xlsx`) **không có** cột lịch học — nên không thể tự động điền lúc
   import, phải nhập tay.
3. Thêm form nhập Thứ/Tiết bắt đầu/Số tiết/Phòng vào dialog Sửa Lớp học phần (`LopHocPhanEditDialog`) + cột "Lịch học"
   trong danh sách LHP để xem nhanh.

**Việc còn lại trước khi làm được "check trùng lịch":** cần người phụ trách dữ liệu nhập lịch học cho đủ số lớp cần
dùng để kiểm thử. Sau đó sẽ thêm bước kiểm tra (bước 2.5, trước khi ghi đăng ký): với mỗi LHP sinh viên đang học
(`TrangThai = DangHoc`) có `Thu` trùng nhau, kiểm tra khoảng `[TietBatDau, TietBatDau+SoTiet-1]` có giao nhau không —
lớp nào có `Thu`/`TietBatDau`/`SoTiet` NULL ("chưa xếp lịch") thì bỏ qua, không coi là trùng với gì cả.

## Giới hạn đã biết, chưa xử lý

- **Race condition sĩ số**: bước kiểm tra sĩ số và bước ghi đăng ký nằm ở 2 lần đọc/ghi khác nhau — về lý thuyết 2 người
  đăng ký cùng lúc khi lớp còn 1 chỗ có thể cùng vượt qua. Rủi ro thấp với 1 admin thao tác tuần tự; muốn chặn triệt để
  cần transaction mức DB (SERIALIZABLE) hoặc constraint/trigger PostgreSQL.
- **Không có kiểm tra môn tiên quyết**: `MonHoc` chưa có cột lưu môn tiên quyết.

## File liên quan

- `src/QuanLyDKHP.Services/DangKyHocPhanService.cs`
- `src/QuanLyDKHP.Infrastructure/Repositories/DangKyHocPhanRepository.cs`, `LopHocPhanRepository.cs`
- `src/QuanLyDKHP.App/Views/DangKyHocPhanView.axaml`, `LopHocPhanEditDialog.axaml`
- `db/2026-10-01_them-lich-hoc-LopHocPhan.sql`

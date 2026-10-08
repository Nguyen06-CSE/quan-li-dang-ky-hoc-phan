# M3 — Đăng ký học phần

**Trạng thái: ✅ Hoàn thành**

## Đã làm (dùng được ngay, dữ liệu thật)

- 6 bước kiểm tra khi đăng ký (`DangKyHocPhanService.DangKyAsync`):
  1. Chặn trùng lớp học phần (SV đã đăng ký đúng LHP này, trạng thái `DangHoc`).
  2. Cảnh báo mềm trùng môn (SV đã học môn này ở LHP khác — cho xác nhận học lại/cải thiện).
  3. Chặn vượt sĩ số tối đa.
  3.5. **Chặn trùng lịch học** (Thứ + khoảng tiết giao nhau với LHP đang học khác) — xem chi tiết bên dưới.
  4. Chặn vượt tổng số tín chỉ tối đa cho phép (theo `CauHinh`).
  5. Ghi đăng ký (tạo mới hoặc tái kích hoạt bản ghi đã hủy) + gọi tính lại học phí.
- Ô chọn Lớp học phần hiển thị sĩ số dạng thanh tiến trình màu (xanh/cam/đỏ) theo tỉ lệ lấp đầy thật (`TyLeLapDay`),
  không phải dữ liệu giả.
- Debounce tìm sinh viên (300ms) khi gõ.

## Kiểm tra trùng lịch học (bước 3.5)

Dùng lại đúng 4 cột lịch học của `LopHocPhan` (`Thu`, `TietBatDau`, `SoTiet`, `Phong` — thêm qua
`db/2026-10-01_them-lich-hoc-LopHocPhan.sql`, đã chạy trên Neon) và hàm có sẵn
`_dangKyRepo.LayTheoMaSVVaMaHocKyAsync` (đã `Include` `LopHocPhan` mang theo các cột này):

1. Chỉ chạy kiểm tra khi LHP **đang đăng ký** đã có đủ `Thu`/`TietBatDau`/`SoTiet` (khác NULL).
2. Lấy danh sách LHP sinh viên đang học (`TrangThai = DangHoc`) trong cùng học kỳ.
3. Với mỗi LHP đang học (bỏ qua chính LHP đang đăng ký): nếu LHP đó **chưa xếp lịch** (còn NULL) thì bỏ qua hoàn
   toàn — không coi là trùng với gì cả, đúng nguyên tắc không bịa dữ liệu giả.
4. Nếu cùng `Thu` và khoảng tiết `[TietBatDau, TietBatDau+SoTiet-1]` giao nhau với lớp đang xét → chặn cứng, trả lỗi
   nêu rõ tên lớp/môn bị trùng và khung giờ (Thứ mấy, tiết bao nhiêu).

Không cần thêm bảng/cột/hàm repository mới — dùng lại 100% dữ liệu và hạ tầng đã có từ trước.

## Giới hạn đã biết, chưa xử lý

- **Race condition sĩ số**: bước kiểm tra sĩ số và bước ghi đăng ký nằm ở 2 lần đọc/ghi khác nhau — về lý thuyết 2 người
  đăng ký cùng lúc khi lớp còn 1 chỗ có thể cùng vượt qua. Rủi ro thấp với 1 admin thao tác tuần tự; muốn chặn triệt để
  cần transaction mức DB (SERIALIZABLE) hoặc constraint/trigger PostgreSQL.
- **Không có kiểm tra môn tiên quyết**: `MonHoc` chưa có cột lưu môn tiên quyết.
- **Chỉ các LHP đã nhập lịch học mới được kiểm tra trùng lịch** — lớp "chưa xếp lịch" không tham gia kiểm tra (không
  bịa dữ liệu lịch học cho lớp chưa có).

## File liên quan

- `src/QuanLyDKHP.Services/DangKyHocPhanService.cs`
- `src/QuanLyDKHP.Infrastructure/Repositories/DangKyHocPhanRepository.cs`, `LopHocPhanRepository.cs`
- `src/QuanLyDKHP.App/Views/DangKyHocPhanView.axaml`, `LopHocPhanEditDialog.axaml`
- `db/2026-10-01_them-lich-hoc-LopHocPhan.sql`

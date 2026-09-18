# 015 — SPEC CHỨC NĂNG IMPORT EXCEL

## Cấu trúc file nguồn (đã xác nhận từ file mẫu thật `26_27_-_ThongKeDKHP.xlsx`, sheet "CT")

| Cột (thứ tự) | Tên cột trong file | Map vào |
|---|---|---|
| 1 | Mã SV | `SinhVien.MaSV` |
| 2 | Tên SV | `SinhVien.HoTen` |
| 3 | Lớp sinh hoạt | `SinhVien.LopSinhHoat` |
| 4 | Khóa Học | `SinhVien.KhoaHoc` |
| 5 | Mã môn | `MonHoc.MaMon` |
| 6 | Tên môn | `MonHoc.TenMon` |
| 7 | Mã LHP | `LopHocPhan.MaLHP` |
| 8 | Số TC | *(không map trực tiếp — xem xử lý đặc biệt bên dưới)* |
| 9 | Mã GV | `LopHocPhan.MaGV` (bỏ qua nếu rỗng) |
| 10 | Tên GV | *(chỉ dùng để hiển thị log, không có bảng riêng lưu tên GV ngoài NguoiDung)* |
| 11 | Bậc ĐT | `MonHoc.BacDaoTao` |
| 12 | Loại Hình ĐT | `LopHocPhan.LoaiHinhDT` |
| 13 | Hình Thức ĐK | `DangKyHocPhan.HinhThucDK` |
| 14 | Ngày ĐK | `DangKyHocPhan.NgayDK` |
| 15 | Người ĐK | `DangKyHocPhan.NguoiDK` |
| 16 | Điểm số | `DangKyHocPhan.DiemSo` (nullable) |
| 17 | Điểm chữ | `DangKyHocPhan.DiemChu` (nullable) |
| 18-21 | T/Trạng học phí, Đã đóng, Phải đóng, Giảng dạy Online | **KHÔNG import** — hệ thống tự tính lại học phí, xem ghi chú bên dưới. Riêng "Giảng dạy Online" nếu có giá trị non-empty ở dòng nào thì map vào `LopHocPhan.GiangDayOnline = true`. |

### ⚠️ Xử lý đặc biệt "Số TC" (cột 8)
File chỉ có 1 cột Số TC duy nhất (không tách LT/TH). Khi import môn học **mới** (chưa tồn tại trong `MonHoc`):
- Gán tạm `SoTinChiLT = SoTC` (toàn bộ vào LT), `SoTinChiTH = 0`.
- Ghi vào **log kết quả import** dòng cảnh báo: "Môn [MaMon] — [TenMon]: số TC LT/TH được gán mặc định, vui lòng vào màn hình Môn học để chỉnh lại cho đúng."
- Nếu môn đã tồn tại trong `MonHoc` → **không ghi đè** số TC đã có (giữ nguyên giá trị giáo vụ đã khai báo thủ công), chỉ dùng cột Số TC trong file để đối chiếu (nếu lệch → cảnh báo trong log, không tự sửa).

### ⚠️ Xử lý cột học phí (18-21)
Không đọc các cột này vào DB. Ghi chú rõ trong code (`// Các cột học phí trong file nguồn là dữ liệu tạm/chưa chính xác, hệ thống tự tính lại theo HocPhiService`).

## Luồng import (`ImportExcelService`)
1. Người dùng chọn file `.xlsx` qua `OpenFileDialog`.
2. Đọc từng dòng bằng `ClosedXML`, validate cơ bản: các cột bắt buộc (Mã SV, Tên SV, Mã môn, Tên môn, Mã LHP) không rỗng.
3. Với mỗi dòng hợp lệ, theo thứ tự **transaction 1 dòng = 1 đơn vị** (lỗi 1 dòng không làm hỏng toàn bộ import):
   - Upsert `SinhVien` (nếu MaSV chưa có → tạo mới; nếu có → không ghi đè HoTen/Lop nếu đã có giá trị khác — cảnh báo trong log nếu dữ liệu lệch, để tránh mất chỉnh sửa thủ công trước đó).
   - Upsert `MonHoc` theo quy tắc TC ở trên.
   - Xác định `HocKy`: dùng học kỳ `DangMo = true` hiện tại của hệ thống (file không có cột học kỳ tường minh — **giả định mọi dòng trong 1 lần import thuộc cùng 1 học kỳ đang chọn sẵn trên UI trước khi import**, xem giao diện bên dưới).
   - Upsert `LopHocPhan` (nếu MaLHP chưa có → tạo mới gắn với MaMon + HocKy đã xác định; nếu có → cập nhật MaGV nếu đang null).
   - Upsert `DangKyHocPhan` theo unique `(MaSV, MaLHP)` — nếu đã tồn tại thì bỏ qua (không tạo trùng), nếu chưa có thì tạo mới với `TrangThai = DangHoc`.
4. Sau khi xử lý xong toàn bộ file, gọi `HocPhiService.TinhLaiHocPhiAsync` cho tất cả SV vừa import trong học kỳ đó.
5. Trả về `KetQuaImportDto`: số dòng thành công, số dòng lỗi (kèm số dòng + lý do), số SV mới tạo, số môn mới tạo, danh sách cảnh báo (TC gán mặc định, dữ liệu lệch...).

## Giao diện Import (`ImportExcelDialog.axaml`)
1. Bước 1: chọn Học kỳ đích (bắt buộc, ComboBox — mặc định học kỳ đang mở).
2. Bước 2: chọn file (`Button` "Chọn file..." + hiển thị tên file đã chọn).
3. Bước 3: Button "Bắt đầu import" → hiện `ProgressBar` trong lúc xử lý (xử lý bất đồng bộ, không đứng UI).
4. Bước 4: hiển thị kết quả dạng bảng tóm tắt (Thành công / Lỗi / Cảnh báo) + `DataGrid` chi tiết log từng dòng có vấn đề, có thể "Xuất log ra file .txt".

## Checklist cho agent
- [ ] `ImportExcelDialog.axaml` + ViewModel theo 4 bước trên.
- [ ] `ImportExcelService` implement đúng logic upsert + xử lý đặc biệt TC như trên.
- [ ] Đảm bảo 1 dòng lỗi không làm dừng toàn bộ quá trình import (try/catch theo từng dòng, gom log).
- [ ] Test thử với chính file mẫu `26_27_-_ThongKeDKHP.xlsx` (542 SV, 36 môn, 110 LHP, 4843 dòng đăng ký) trước khi báo hoàn thành.

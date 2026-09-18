# 010 — SPEC MÀN HÌNH ĐĂNG KÝ / ĐIỀU CHỈNH HỌC PHẦN

Đây là màn hình nghiệp vụ phức tạp và quan trọng nhất — đọc kỹ trước khi code.

## Giao diện (`DangKyHocPhanView.axaml`)
1. Tiêu đề `.h1` "Đăng ký / Điều chỉnh học phần" + hiển thị Học kỳ hiện hành (readonly, lấy từ `HocKy.DangMo`).
2. **Khối chọn sinh viên**: TextBox autocomplete tìm theo Mã SV/Tên (gợi ý dropdown khi gõ ≥ 2 ký tự). Sau khi chọn:
   - Card thông tin SV: Mã SV, Họ tên, Lớp sinh hoạt.
   - Thanh tổng hợp tín chỉ: `ProgressBar` hiển thị `Tổng TC đã đăng ký / Số TC tối đa` (lấy cấu hình từ `CauHinhHeThong`), text bên cạnh "X / Y tín chỉ". Màu: bình thường `BrushPrimary`, khi ≥ 90% → `BrushWarning`, khi = 100% hoặc vượt (không nên xảy ra vì đã chặn) → `BrushDanger`. Nếu tổng TC < tối thiểu → hiện dòng cảnh báo nhỏ màu `BrushWarning` "Chưa đạt số tín chỉ tối thiểu (X/Y)" (không chặn, vì SV có thể đang trong quá trình đăng ký).
3. **Bảng "Học phần đã đăng ký"**: Mã LHP, Tên môn, Số TC, GV, Trạng thái, nút icon "Hủy đăng ký" (Danger, có confirm dialog) — chỉ hiện nếu `TrangThai = DangHoc`.
4. **Khối "Thêm đăng ký mới"**: ComboBox chọn Môn học (autocomplete) → sau khi chọn, ComboBox thứ 2 hiện các LHP thuộc môn đó **trong học kỳ hiện hành, còn chỗ trống** (ẩn LHP đã đầy sĩ số) → Button `.btn-primary` "+ Đăng ký".

## Luồng nghiệp vụ khi bấm "+ Đăng ký" (`DangKyHocPhanService.DangKyAsync`)
Thứ tự kiểm tra (dừng ngay khi gặp lỗi, hiển thị thông báo tương ứng tại UI, không popup gây gián đoạn trừ khi là lỗi chặn cứng):

1. **Kiểm tra trùng LHP**: SV đã đăng ký đúng `MaLHP` này với `TrangThai = DangHoc` chưa? → nếu có, **chặn cứng**: "Sinh viên đã đăng ký lớp học phần này."
2. **Kiểm tra trùng môn (cảnh báo mềm)**: SV đã có đăng ký `TrangThai = DangHoc` ở 1 LHP khác **cùng Mã môn**? → nếu có, hiện dialog cảnh báo (không phải lỗi): "Sinh viên đã đăng ký môn [Tên môn] ở lớp khác ([MaLHP cũ]). Đây có phải trường hợp học lại/học cải thiện?" với 2 nút "Vẫn đăng ký" / "Hủy" — không tự động chặn vì có thể là học lại hợp lệ (xem giả định #2 trong tài liệu thiết kế đã duyệt).
3. **Kiểm tra sĩ số LHP**: nếu `SiSoToiDa` đã đặt và `SoLuongDaDangKy >= SiSoToiDa` → chặn cứng: "Lớp học phần đã đủ sĩ số."
4. **Kiểm tra tổng số tín chỉ**: tính `TongTCHienTai + SoTCMonMoi`. Nếu > `SoTinChiToiDa` (từ `CauHinhHeThong`) → chặn cứng: "Vượt quá số tín chỉ tối đa cho phép (Y tín chỉ)."
5. Nếu qua hết → tạo bản ghi `DangKyHocPhan` mới: `NgayDK = DateTime.Now`, `NguoiDK = tên đăng nhập người thực hiện (từ ICurrentUserService)`, `TrangThai = DangHoc`, `HinhThucDK` mặc định `"KH"` (giá trị phổ biến nhất trong dữ liệu mẫu — có thể đổi sau ở cấu hình nếu cần).
6. Gọi `HocPhiService.TinhLaiHocPhiAsync(maSV, maHocKy)` để cập nhật `SoTienPhaiDong` cho toàn bộ đăng ký của SV trong học kỳ (xem `011-Spec-Man-HocPhi.md`).
7. Refresh lại bảng + thanh tổng hợp tín chỉ, toast "Đăng ký thành công."

## Luồng "Hủy đăng ký" / Điều chỉnh
- Xác nhận dialog → set `TrangThai = DaHuy` (**không xóa bản ghi**, giữ lịch sử) → gọi lại `TinhLaiHocPhiAsync` → refresh UI.
- "Điều chỉnh học phần" được hiện thực bằng tổ hợp: Hủy LHP cũ (bước trên) + Đăng ký LHP mới (mục trên) — không cần màn hình riêng, ghi rõ điều này trong code comment để không ai hiểu nhầm là thiếu tính năng.

## ViewModel (`DangKyHocPhanViewModel`)
Properties chính: `SinhVienDangChon`, `TongTinChiHienTai`, `TinChiToiDa`, `TinChiToiThieu`, `DsDaDangKy` (ObservableCollection), `MonDangChon`, `DsLHPTheoMon`, `LHPDangChon`.
Commands: `TimSinhVienCommand`, `ChonMonCommand` (load LHP theo môn), `DangKyCommand`, `HuyDangKyCommand`.

## Checklist cho agent
- [ ] `DangKyHocPhanView.axaml` + ViewModel đầy đủ luồng trên.
- [ ] `DangKyHocPhanService.DangKyAsync` implement đúng thứ tự 5 bước kiểm tra + gọi tính lại học phí.
- [ ] `DangKyHocPhanService.HuyDangKyAsync` soft-cancel (đổi trạng thái, không xóa).
- [ ] Unit test (trong `tests/QuanLyDKHP.Tests`) cho các case: vượt TC tối đa, trùng LHP, LHP đầy sĩ số — bắt buộc có test.

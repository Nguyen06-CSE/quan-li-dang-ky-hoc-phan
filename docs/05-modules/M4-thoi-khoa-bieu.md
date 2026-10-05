# M4 — Thời khóa biểu

**Trạng thái: ❌ Chưa làm — chờ dữ liệu lịch học**

## Lý do chặn

Toàn bộ Module này (lưới thời khóa biểu Thứ 2 → CN theo tiết) phụ thuộc vào 4 cột lịch học của `LopHocPhan`
(`Thu`, `TietBatDau`, `SoTiet`, `Phong`) — trước đây không tồn tại trong DB, và ngay cả sau khi thêm cột
(`db/2026-10-01_them-lich-hoc-LopHocPhan.sql`, đã chạy trên Neon), **toàn bộ lớp học phần hiện có đều có giá trị NULL**
("chưa xếp lịch") vì file Excel import thật không có cột này. Không có gì để vẽ lên lưới nếu dữ liệu toàn NULL —
theo nguyên tắc không bịa dữ liệu giả, Module này chờ đến khi:

1. Đủ số lớp học phần cần thiết đã được nhập lịch học (qua form thêm ở `LopHocPhanEditDialog`, xem `M3-dang-ky-hoc-phan.md`).
2. Xác nhận lại với người phụ trách xem có cần làm thêm màn hình nhập lịch học theo lô (ví dụ qua Excel riêng) hay
   nhập tay từng lớp là đủ.

## Kế hoạch khi có dữ liệu

- Lưới 7 ngày × N tiết (theo `CauHinh` số tiết/ngày), mỗi ô hiển thị Mã LHP/Tên môn/Phòng của lớp sinh viên đang học
  rơi vào đúng Thứ + khoảng tiết đó.
- Dùng lại đúng field `Thu`/`TietBatDau`/`SoTiet`/`Phong` đã có trên `LopHocPhanDisplayDto.Entity`, không cần thêm
  bảng/cột mới.

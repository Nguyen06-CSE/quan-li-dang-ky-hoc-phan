# 017 — LỘ TRÌNH TRIỂN KHAI & TIÊU CHÍ HOÀN THÀNH (DoD)

## Thứ tự triển khai đề xuất (theo sprint/giai đoạn nhỏ)

| Giai đoạn | Nội dung | File spec liên quan |
|---|---|---|
| 1 | Khởi tạo project, kiến trúc, CSDL, migration + seed | 001, 002 |
| 2 | Đăng nhập, phân quyền, khung giao diện chính (header/menu/sidebar) | 003, 004, 005 |
| 3 | CRUD Sinh viên, CRUD Môn học | 007, 008 |
| 4 | Học kỳ / Lớp học phần | 009 |
| 5 | Import Excel (test với file mẫu thật) | 015 |
| 6 | Đăng ký / Điều chỉnh học phần (module lõi quan trọng nhất) | 010 |
| 7 | Tính học phí | 011 |
| 8 | Báo cáo/thống kê + Export Excel/PDF | 012, 016 |
| 9 | Dashboard, Cấu hình hệ thống, Quản lý người dùng | 006, 013, 014 |
| 10 | Kiểm thử tổng thể, sửa lỗi, hoàn thiện UI polish | — |

## Tiêu chí hoàn thành (Definition of Done) cho mỗi module
Một module được coi là **xong** khi:
1. Build thành công, không warning nghiêm trọng.
2. Chức năng khớp đúng spec tương ứng (đối chiếu từng checklist cuối mỗi file spec).
3. Có xử lý lỗi cơ bản (không crash khi nhập sai dữ liệu, hiển thị thông báo rõ ràng).
4. Với các Service có logic nghiệp vụ quan trọng (Học phí, Đăng ký học phần) — có unit test tối thiểu cho các case chính.
5. Giao diện đúng design system ở `004-Spec-DesignSystem-GiaoDien.md` (màu sắc, spacing, style control).
6. Đã test thủ công với dữ liệu thật (import file mẫu) ít nhất 1 lần.

## Tiêu chí hoàn thành toàn dự án (Giai đoạn 1)
- [ ] Toàn bộ 9 yêu cầu chức năng trong đề bài (mục `000-Spec-TongQuan.md`) chạy được, có thể demo trực tiếp cho giảng viên.
- [ ] Ứng dụng chạy như 1 desktop app độc lập (không mở trình duyệt, không phụ thuộc localhost web server).
- [ ] Đăng nhập phân quyền 4 role hoạt động đúng ma trận đã duyệt.
- [ ] Import được file Excel mẫu thật không lỗi crash, ra kết quả log rõ ràng.
- [ ] Xuất được ít nhất: 1 phiếu ĐKHP PDF, 1 danh sách Excel, 1 phiếu học phí PDF.
- [ ] README hướng dẫn: cách cấu hình connection string, cách chạy migration, tài khoản Admin mặc định.

## Việc KHÔNG làm ở giai đoạn 1 (để tránh agent tự ý mở rộng ngoài phạm vi)
- Không làm sinh viên tự đăng ký qua app.
- Không làm chart/biểu đồ phức tạp cho thống kê (chỉ bảng số liệu).
- Không làm đồng bộ multi-user real-time (không cần SignalR/realtime update giữa nhiều máy đang mở app cùng lúc).
- Không cần responsive nhiều kích thước màn hình đặc biệt — tối ưu cho màn hình desktop thông thường (≥1280px chiều ngang).

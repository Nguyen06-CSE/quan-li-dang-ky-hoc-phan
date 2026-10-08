# 🧱 Module UI/UX (Spec nâng cấp giao diện + luồng nghiệp vụ)

Thư mục này ghi lại tiến độ của 4 Module trong bản nâng cấp UI/UX (theo spec "Liquid / iOS Material") — khác với `01-features/`
(đặc tả CHỨC NĂNG nghiệp vụ gốc), các file ở đây mô tả theo MODULE giao diện/trải nghiệm, kèm trạng thái triển khai thực tế
và các quyết định kỹ thuật đã chốt trong quá trình làm.

> 📌 Lỗi phát sinh trong lúc làm từng module được log chi tiết ở [`../03-troubleshooting/FIX.md`](../03-troubleshooting/FIX.md)
> (phần log dài, nhiều mục, do AI hỗ trợ ghi) — file ở đây chỉ tóm tắt KẾT QUẢ, không lặp lại chi tiết debug.

## Mục lục

| Module | Nội dung | Trạng thái |
|---|---|---|
| [M1 — Đăng nhập](M1-dang-nhap.md) | Màn hình Login, remember-me, hiệu ứng lỗi | ✅ Hoàn thành |
| [M2 — App Shell / Navigation](M2-app-shell.md) | Sidebar, top bar, điều hướng, theme Liquid glass | ✅ Hoàn thành |
| [M3 — Đăng ký học phần](M3-dang-ky-hoc-phan.md) | Luồng đăng ký, cảnh báo, check trùng lịch | ✅ Hoàn thành |
| [M4 — Thời khóa biểu](M4-thoi-khoa-bieu.md) | Lưới thời khóa biểu Thứ 2 → CN | ✅ Hoàn thành |

## Quy tắc chung đã áp dụng cho cả 4 module

- **Không bịa dữ liệu giả.** Nếu 1 tính năng cần dữ liệu chưa có trong DB (ví dụ lịch học lớp học phần), việc thêm cột/bảng
  luôn được tách ra thành 1 file SQL riêng trong `db/` để người phụ trách CSDL tự chạy trên Neon, không tự ý chạy hộ.
- **Giữ nguyên 100% cấu trúc chức năng khi đổi giao diện.** Khi áp theme "Liquid glass" (kính mờ) lên các màn hình, binding
  dữ liệu, `x:Name`, Command đều giữ nguyên — chỉ đổi "lớp áo" đồ họa, gom màu sắc/animation vào `Styles/Controls.axaml`
  và `Styles/Colors.axaml` dưới dạng `Classes`, không hard-code trực tiếp trên control.

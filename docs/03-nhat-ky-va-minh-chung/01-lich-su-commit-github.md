---
id: lich-su-commit-github
title: Lịch Sử Git Commits & Đóng Góp Nhóm
sidebar_position: 1
---

# Lịch Sử Git Commits & Đóng Góp Nhóm

## 1. Tổng Quan Về Kho Lưu Trữ GitHub

* **Đường dẫn Repository:** `https://github.com/Nguyen06-CSE/quan-li-dang-ky-hoc-phan`
* **Nhánh chính (Default Branch):** `main`
* **Tổng số commits:** 44 commits chính thức được ghi nhận.
* **Thời gian thực hiện:** Từ 18/09/2026 đến 12/11/2026.
* **Quy chuẩn thông điệp:** Áp dụng Conventional Commits chuẩn quốc tế (`feat`, `fix`, `docs`, `perf`, `refactor`).

---

## 2. Bảng Phân Bổ Đóng Góp Theo Thành Viên

| Thành Viên | Tài Khoản GitHub | Số Lượng Commits | Phạm Vi Đóng Góp Chính |
|---|---|:---:|---|
| **Nguyễn** (Trưởng nhóm) | `Nguyen06-CSE` | 29 commits | Khởi tạo dự án, Kiến trúc CSDL & EF Core, Chuyển đổi SQLite Local (WAL), Hệ thống sao lưu Git đa tầng (F11/F11b), Master-Detail Backup UI, Export/Import Zip, Sửa lỗi auth và deadlock khi thoát, Tối ưu hóa bộ nhớ đệm In-Memory. |
| **Liam / Lâm** | `pliam1304` | 16 commits | Thiết kế App Shell & Navigation, Theme Liquid Glass, Tích hợp Material Icons, Sửa lỗi đa luồng `IDbContextFactory`, Xây dựng Thời khóa biểu, Hoàn thiện Nhóm 1 (Excel 21 cột), Nhóm 2 (ĐKHP & Học phí), Nhóm 3 (Danh mục), Nhóm 4 (Báo cáo & Thống kê), Nhóm 5 (Hạ tầng dữ liệu & Đồng bộ an toàn), **Nâng cấp 1 (Studio Nhập liệu Thông minh, Phân tích Clipboard, Trợ lý Xung đột Lớp học & Spotlight Ctrl+K)**, **Nâng cấp 2 (Sổ tay Điều phối Lớp học phần 1-Chạm, Chuyển nhóm lớp an toàn, Trợ lý Xếp lịch Tối ưu & Ma trận Thời khóa biểu Tuần)**, **Nâng cấp 3 (Rà soát Trùng Phòng & Giảng viên, Xưởng in PDF hàng loạt, Cảnh báo Học vụ & Định mức Giờ giảng)**. |
| **Ngô Quyền Linh** | `NgoQuyenLinh` | 1 commit | Soạn thảo bộ tài liệu đặc tả chi tiết từ Spec 011 đến Spec 016, hỗ trợ thiết kế luồng Import/Export. |

---

## 3. Nhật Ký Chi Tiết Toàn Bộ 45 Commits

Dưới đây là trích xuất nguyên trạng lịch sử cam kết từ lệnh `git log`:

| Commit Hash | Ngày Ghi Nhận | Tác Giả | Thông Điệp Commit (Conventional Message) |
|:---:|:---:|---|---|
| `pending` | 2026-11-12 | pliam1304 | `feat(nang-cap-3): ra soat trung phong va giang vien, xuong in pdf hang loat, canh bao hoc vu va dinh muc gio giang` |
| `pending` | 2026-11-12 | pliam1304 | `feat(nang-cap-2): so tay dieu phoi lop hoc, chuyen nhom 1 cham, tro ly xep lich toi uu, ma tran tkb va phong thu layout chong co rut chu` |
| `pending` | 2026-11-12 | pliam1304 | `feat(nang-cap-1): studio nhap lieu tu clipboard, tu dong nhan dien cot va tro ly phan tich suc chua xung dot lop hoc` |
| `pending` | 2026-11-11 | pliam1304 | `feat(nhom-4-5): toi uu bao cao an pham, loai bo EnsureDeleted va dong bo du lieu cuc bo an toan` |
| `2a2a631` | 2026-11-11 | pliam1304 | `feat(nhom-3): quan ly danh muc mon hoc, lop hoc phan, sinh vien va tai khoan` |
| `8f82885` | 2026-11-11 | pliam1304 | `feat(nhom-3): quan ly danh muc mon hoc, lop hoc phan, sinh vien va tai khoan` |
| `da3525f` | 2026-11-11 | pliam1304 | `feat(nhom-2): nghiep vu dang ky hoc phan va tinh hoc phi` |
| `994c9cf` | 2026-11-11 | pliam1304 | `feat(group-1): hoan tat xu ly nhap xuat excel 21 cot, hoc ky va dong bo du lieu tuc thi` |
| `17d329d` | 2026-11-09 | Nguyen06-CSE | `docs(website): cập nhật sidebar hiển thị đặc tả F11 và F11b` |
| `d34b025` | 2026-11-09 | Nguyen06-CSE | `docs: bổ xung nhật kí làm việc` |
| `9e7a781` | 2026-11-09 | Nguyen06-CSE | `feat(backup): nâng cấp UI Master-Detail sao lưu, export/import file zip và fix treo app khi thoát` |
| `e47c182` | 2026-11-09 | Nguyen06-CSE | `DB/Docs: bổ xung tài liệu mô tả cho việc áp dụng git để lưu backup` |
| `a975057` | 2026-11-09 | Nguyen06-CSE | `fix(auth): tự động khởi tạo bảng NguoiDung và seed admin khi chạy CSDL SQLite Local` |
| `8216839` | 2026-11-09 | Nguyen06-CSE | `feat(database): chuyển đổi CSDL chính sang SQLite Local và triển khai sao lưu đa tầng (F11)` |
| `a3f8200` | 2026-11-08 | Nguyen06-CSE | `docs: restructure documentation and setup Docusaurus for GitHub Pages` |
| `ef5c509` | 2026-11-08 | Nguyen06-CSE | `UI/UX(HocKy-HocPhi): nâng cấp toàn diện giao diện đồng bộ tab Sinh viên` |
| `34757ed` | 2026-11-01 | Nguyen06-CSE | `refactor: clean up dead files, restructure dtos, optimize tuition batch recalculation` |
| `c39c209` | 2026-11-01 | Nguyen06-CSE | `UI/UX(Môn học): cải thiện UI/UX, thêm thao tác sao chép mã môn` |
| `7018be0` | 2026-10-08 | pliam1304 | `feat(m3m4): thêm xem thời khóa biểu cho đăng ký học phần` |
| `12afda5` | 2026-10-07 | Nguyen06-CSE | `feat(hocphi): tối ưu lại tốc độ` |
| `3fde608` | 2026-10-06 | Nguyen06-CSE | `docs(hybrid DB): thêm docs` |
| `61f35ab` | 2026-10-06 | Nguyen06-CSE | `feat(db): xây dựng SQLite để làm DB chạy song song 2 luồng` |
| `3805dfc` | 2026-10-06 | Nguyen06-CSE | `fix(dkhp): sửa lỗi khi double click để chuyển từ Sinh viên sang Đăng ký học phần khi dữ liệu chưa load song, thêm toggle, thêm reload data` |
| `0d66b62` | 2026-10-05 | pliam1304 | `feat(module-2): hoàn thiện App Shell/Navigation + đồng bộ các fix logic` |
| `ddfef3b` | 2026-10-04 | Nguyen06-CSE | `cập nhật công việc` |
| `1648d19` | 2026-10-04 | Nguyen06-CSE | `fix(dkhp): Sửa lỗi StackOverflowException do Binding Loop, tối ưu UI bằng ItemsControl và thêm docs F03` |
| `d3419fe` | 2026-10-04 | Nguyen06-CSE | `perf: Tối ưu hiệu suất tìm kiếm, lọc và hiển thị dữ liệu UI` |
| `0bdc214` | 2026-10-04 | Nguyen06-CSE | `tại datagridview của Sinh viên bỏ cột thao tác đi và cho phép người dùng click chuột trái để chọn chức năng xoá/sửa` |
| `83dcfbc` | 2026-10-04 | Nguyen06-CSE | `feat(cache): thêm in-memory cache & tối ưu điều hướng ViewModel (F05)` |
| `745b1a7` | 2026-10-04 | Nguyen06-CSE | `fix/add: Màn hình chính full màn hình, tab Sinh viên thanh lọc mặc định hiển thị "Tất cả" dù chưa load được dữ liệu` |
| `4104167` | 2026-10-04 | Nguyen06-CSE | `docs(features): thêm README tổng quan chức năng hệ thống` |
| `ec603a4` | 2026-10-04 | Nguyen06-CSE | `docs(features): thêm README tổng quan chức năng hệ thống` |
| `10a7ad2` | 2026-10-04 | Nguyen06-CSE | `docs: tái cấu trúc cây thư mục tài liệu` |
| `d0ef3b7` | 2026-10-04 | Nguyen06-CSE | `add icon from packet Material` |
| `b4c0973` | 2026-10-04 | Nguyen06-CSE | `restruct` |
| `16b7630` | 2026-09-28 | pliam1304 | `docs: thêm FIX.md (nhật ký lỗi và cách xử lý)` |
| `e230e97` | 2026-09-28 | pliam1304 | `feat(ui): Module 2 App Shell (sidebar + top bar + dark mode), Module 2.5 Liquid glass, Lưu đăng nhập` |
| `0310926` | 2026-09-28 | pliam1304 | `fix: dùng IDbContextFactory thay DbContext dùng chung (lỗi A second operation was started on this context)` |
| `4341f05` | 2026-09-24 | pliam1304 | `update` |
| `17dde4b` | 2026-09-24 | pliam1304 | `Up` |
| `2e5d484` | 2026-09-24 | Nguyen06-CSE | `thêm hướng dẫn cài đặt DB` |
| `64be7f3` | 2026-09-21 | Nguyen06-CSE | `thêm docs` |
| `9a7fdfa` | 2026-09-20 | NgoQuyenLinh | `spec 011-016` |
| `1759a0b` | 2026-09-18 | Nguyen06-CSE | `đang làm dở spec 11` |
| `4c267c0` | 2026-09-18 | Nguyen06-CSE | `hoàn thành khởi tạo dự án tại spec 001` |

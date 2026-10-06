# 📚 Tài liệu Dự án QuanLyDKHP

Chào mừng đến với trung tâm tài liệu kỹ thuật của dự án **Quản Lý Đăng Ký Học Phần**.

---

## 📂 Cấu trúc cây tài liệu (`docs/`)

```text
docs/
├── README.md                      # Index chính: Bản đồ tra cứu tài liệu & quy tắc nhóm
├── roadmap.md                     # Tổng quan lộ trình, các mốc quan trọng (Milestones)
│
├── 00-architecture/               # Kiến trúc hệ thống, Phân quyền, DB Schema
│   ├── caching-and-state-management.md # Kiến trúc Cache 2 tầng & ViewModel State
│   ├── coding-conventions.md      # Quy chuẩn code C# / MVVM / Git branch
│   ├── database-setup.md          # Hướng dẫn setup DB Local / Docker
│   ├── ERD.md                     # Thiết kế bảng & Mối quan hệ
│   └── PHAN-QUYEN.md              # Ma trận phân quyền (Permission Matrix)
│
├── 01-features/                   # Mô tả chi tiết từng chức năng (Spec / Requirements)
│   ├── F01-auth-login.md          # Đăng nhập, session, nhớ mật khẩu
│   ├── F02-quan-ly-sinh-vien.md   # CRUD sinh viên, bộ lọc Lớp/Khoá
│   ├── F03-dkhp.md                # Đăng ký học phần, thuật toán ràng buộc
│   ├── F04-import-excel.md        # Luồng Import Excel, xử lý file lớn
│   └── F06-local-database-cache-sync.md # Hybrid Cache SQLite & Delta Sync
│
├── 02-daily-logs/                 # Tiến trình công việc & Blockers theo NGÀY (Daily Log)
│   ├── 2026-09-28.md              # Báo cáo tiến độ ngày 28/09
│   └── 2026-10-04.md              # Báo cáo tiến độ mới nhất
│
├── 03-troubleshooting/            # Nhật ký lỗi, cách sửa & các vấn đề tồn đọng
│   ├── FIX.md                     # Tổng hợp các lỗi đã sửa & hướng giải quyết
│   ├── known-bugs.md              # Danh sách lỗi tồn đọng (Blockers / Backlog)
│   └── macos-issues.md            # Các lỗi đặc thù theo OS (Quarantine, Gatekeeper)
│
├── 04-scripts/                    # Tool phụ trợ, script sinh DB, sinh Context
│   ├── generate_db.py             # Script khởi tạo các file Entity Core
│   └── generate_dbcontext.py      # Script khởi tạo AppDbContext
│
└── 05-modules/                    # Tiến độ 4 Module UI/UX (Login, App Shell, ĐKHP, TKB)
    ├── M1-dang-nhap.md            # Module 1 — Đăng nhập
    ├── M2-app-shell.md            # Module 2 — App Shell / Navigation / Liquid glass
    ├── M3-dang-ky-hoc-phan.md     # Module 3 — Đăng ký học phần (check trùng lịch)
    └── M4-thoi-khoa-bieu.md       # Module 4 — Thời khóa biểu
```

---

## 🗺️ Bản đồ tra cứu nhanh

- **[Lộ trình phát triển](roadmap.md)**: Danh sách các Sprint & tính năng sắp triển khai.
- **[Kiến trúc & DB](00-architecture/)**:
  - [Quy chuẩn viết code](00-architecture/coding-conventions.md)
  - [Kiến trúc Cache 2 tầng & Trạng thái UI](00-architecture/caching-and-state-management.md)
  - [Hướng dẫn Cài đặt DB PostgreSQL](00-architecture/database-setup.md)
  - [Sơ đồ ERD](00-architecture/ERD.md)
  - [Ma trận Phân quyền Hệ thống](00-architecture/PHAN-QUYEN.md)
- **[Đặc tả Chức năng](01-features/)**: Chi tiết yêu cầu UI/UX và logic nghiệp vụ.
  - [Tổng quan chức năng](01-features/README.md)
  - [Đặc tả Bộ nhớ đệm Cục bộ & Delta Sync (F06)](01-features/F06-local-database-cache-sync.md)
- **[Tiến độ hằng ngày](02-daily-logs/)**: Cập nhật công việc, task hoàn thành theo ngày.
- **[Sửa lỗi & Tồn đọng](03-troubleshooting/)**:
  - [Nhật ký lỗi đã xử lý (FIX.md)](03-troubleshooting/FIX.md)
  - [Danh sách lỗi chưa sửa (known-bugs.md)](03-troubleshooting/known-bugs.md)
  - [Sửa lỗi đặc thù macOS](03-troubleshooting/macos-issues.md)
- **[Script phụ trợ](04-scripts/)**: Công cụ sinh code DB tự động.
- **[Module UI/UX](05-modules/)**: Tiến độ triển khai thực tế theo 4 Module (Login, App Shell, Đăng ký học phần,
  Thời khóa biểu) — khác với đặc tả nghiệp vụ gốc ở `01-features/`.
  - [Tổng quan 4 Module](05-modules/README.md)

---

## 🤝 Quy định làm việc nhóm

1. **Trước khi bắt đầu task**: Kiểm tra `03-troubleshooting/known-bugs.md` và `roadmap.md` để nắm các vấn đề cần ưu tiên.
2. **Cuối ngày (End of Day)**: Tạo 1 file log mới trong `02-daily-logs/YYYY-MM-DD.md` để ghi lại tiến độ và khó khăn.
3. **Khi gặp bug / sửa xong bug**:
   - Lỗi đã sửa: Cập nhật chi tiết nguyên nhân & cách xử lý vào `03-troubleshooting/FIX.md`.
   - Lỗi mới phát hiện: Thêm vào danh sách `03-troubleshooting/known-bugs.md`.
-- ============================================================
-- Seed data demo đầy đủ cho QLiDKHP
-- Chạy trên SQL Editor của Neon (PostgreSQL).
-- An toàn khi chạy lại (dùng ON CONFLICT DO NOTHING).
-- Mật khẩu mặc định của tài khoản mới: 123456
-- ============================================================

-- ===================== 1. NGUOI DUNG =====================
-- Admin/số đã tồn tại (pass: admin). Các tài khoản mới dùng chung hash của "123456".
INSERT INTO "NguoiDung" ("Id", "TenDangNhap", "MatKhauHash", "HoTen", "Role", "MaGV", "TrangThai", "IsDeleted", "NgayTao")
VALUES
    (2, 'troly.giaovu', '$2a$11$QG/lFfvjdVmhKxdVmRYzxeNm92mwxX9rj.xXj9kUl1kA80ak7t6Rm', 'Trần Thị Bích Trợ Lý', 'TroLyGiaoVu', NULL, 'HoatDong', false, NOW()),
    (3, 'giaovu.bomon', '$2a$11$QG/lFfvjdVmhKxdVmRYzxeNm92mwxX9rj.xXj9kUl1kA80ak7t6Rm', 'Lê Văn Giáo Vụ', 'GiaoVuBoMon', NULL, 'HoatDong', false, NOW()),
    (4, 'gv.nguyenvanan', '$2a$11$QG/lFfvjdVmhKxdVmRYzxeNm92mwxX9rj.xXj9kUl1kA80ak7t6Rm', 'Nguyễn Văn An', 'GiangVien', 'GV001', 'HoatDong', false, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- Cập nhật sequence để EF tự thêm user mới không bị trùng Id
SELECT setval(pg_get_serial_sequence('"NguoiDung"', 'Id'), (SELECT GREATEST(COALESCE(MAX("Id"), 1), 1) FROM "NguoiDung"));

-- ===================== 2. SINH VIEN =====================
INSERT INTO "SinhVien" ("MaSV", "HoTen", "LopSinhHoat", "KhoaHoc", "IsDeleted", "NgayTao")
VALUES
    ('SV001', 'Nguyễn Văn An',      'CNTT1', '2023', false, NOW()),
    ('SV002', 'Trần Thị Bình',      'CNTT1', '2023', false, NOW()),
    ('SV003', 'Lê Văn Cường',       'CNTT1', '2022', false, NOW()),
    ('SV004', 'Phạm Thị Dung',      'CNTT2', '2023', false, NOW()),
    ('SV005', 'Hoàng Văn Em',       'HTTT1', '2023', false, NOW()),
    ('SV006', 'Vũ Thị Phương',      'HTTT1', '2022', false, NOW()),
    ('SV007', 'Đặng Văn Giáp',      'ATTT1', '2023', false, NOW()),
    ('SV008', 'Ngô Thị Hạnh',       'ATTT1', '2024', false, NOW())
ON CONFLICT ("MaSV") DO NOTHING;

-- ===================== 3. HOC KY =====================
INSERT INTO "HocKy" ("MaHocKy", "TenHocKy", "NgayBatDau", "NgayKetThuc", "DangMo", "NgayTao")
VALUES
    ('HK20251', 'Học kỳ 1 - 2025/2026', '2025-08-25', '2025-12-20', true,  NOW()),
    ('HK20252', 'Học kỳ 2 - 2025/2026', '2026-01-05', '2026-05-20', false, NOW()),
    ('HK20261', 'Học kỳ Hè - 2026',     '2026-06-01', '2026-07-15', false, NOW())
ON CONFLICT ("MaHocKy") DO NOTHING;

-- ===================== 4. MON HOC =====================
INSERT INTO "MonHoc" ("MaMon", "TenMon", "SoTinChiLT", "SoTinChiTH", "BacDaoTao", "IsDeleted", "NgayTao")
VALUES
    ('CS101', 'Nhập môn lập trình',              2, 1, 'Đại học', false, NOW()),
    ('CS102', 'Lập trình hướng đối tượng',       2, 1, 'Đại học', false, NOW()),
    ('CS201', 'Cấu trúc dữ liệu và giải thuật',  3, 0, 'Đại học', false, NOW()),
    ('CS202', 'Cơ sở dữ liệu',                   2, 1, 'Đại học', false, NOW()),
    ('CS301', 'Mạng máy tính',                   3, 0, 'Đại học', false, NOW()),
    ('CS302', 'Hệ điều hành',                    3, 0, 'Đại học', false, NOW()),
    ('MH401', 'Toán rời rạc',                    3, 0, 'Đại học', false, NOW()),
    ('CS402', 'An toàn thông tin',               2, 1, 'Đại học', false, NOW())
ON CONFLICT ("MaMon") DO NOTHING;

-- ===================== 5. LOP HOC PHAN =====================
INSERT INTO "LopHocPhan" ("MaLHP", "MaMon", "MaHocKy", "MaGV", "LoaiHinhDT", "SiSoToiDa", "GiangDayOnline", "IsDeleted", "NgayTao")
VALUES
    ('LHP001', 'CS101', 'HK20251', 'GV001', 'LT', 40, false, false, NOW()),
    ('LHP002', 'CS101', 'HK20251', 'GV001', 'TH', 30, true,  false, NOW()),
    ('LHP003', 'CS102', 'HK20251', 'GV001', 'LT', 45, false, false, NOW()),
    ('LHP004', 'CS201', 'HK20251', 'GV002', 'LT', 50, false, false, NOW()),
    ('LHP005', 'CS202', 'HK20251', 'GV002', 'LT', 40, false, false, NOW()),
    ('LHP006', 'CS301', 'HK20252', 'GV001', 'LT', 50, true,  false, NOW()),
    ('LHP007', 'MH401', 'HK20251', 'GV003', 'LT', 45, false, false, NOW()),
    ('LHP008', 'CS402', 'HK20251', 'GV002', 'LT', 40, false, false, NOW())
ON CONFLICT ("MaLHP") DO NOTHING;

-- ===================== 6. DANG KY HOC PHAN =====================
-- Đơn giá: LT = 500.000đ/TC, TH = 700.000đ/TC
INSERT INTO "DangKyHocPhan" ("Id", "MaSV", "MaLHP", "HinhThucDK", "NgayDK", "NguoiDK", "DiemSo", "DiemChu", "TrangThai", "SoTienPhaiDong", "SoTienDaDong", "NgayTao")
VALUES
    ('11111111-1111-1111-1111-111111111101', 'SV001', 'LHP001', 'Online',   NOW(), 'admin', NULL, NULL, 'DangHoc', 1700000, 0, NOW()),
    ('11111111-1111-1111-1111-111111111102', 'SV002', 'LHP001', 'TrucTiep', NOW(), 'admin', NULL, NULL, 'DangHoc', 1700000, 0, NOW()),
    ('11111111-1111-1111-1111-111111111103', 'SV003', 'LHP003', 'TrucTiep', NOW(), 'admin', NULL, NULL, 'DangHoc', 1700000, 0, NOW()),
    ('11111111-1111-1111-1111-111111111104', 'SV004', 'LHP005', 'TrucTiep', NOW(), 'admin', NULL, NULL, 'DangHoc', 1700000, 0, NOW()),
    ('11111111-1111-1111-1111-111111111105', 'SV005', 'LHP006', 'Online',   NOW(), 'admin', NULL, NULL, 'DangHoc', 1500000, 0, NOW()),
    ('11111111-1111-1111-1111-111111111106', 'SV001', 'LHP004', 'TrucTiep', NOW(), 'admin', NULL, NULL, 'DangHoc', 1500000, 0, NOW()),
    ('11111111-1111-1111-1111-111111111107', 'SV006', 'LHP008', 'TrucTiep', NOW(), 'admin', NULL, NULL, 'DangHoc', 1700000, 0, NOW()),
    ('11111111-1111-1111-1111-111111111108', 'SV007', 'LHP007', 'TrucTiep', NOW(), 'admin', NULL, NULL, 'DangHoc', 1500000, 0, NOW()),
    ('11111111-1111-1111-1111-111111111109', 'SV008', 'LHP002', 'Online',   NOW(), 'admin', NULL, NULL, 'DangHoc', 1700000, 0, NOW()),
    ('11111111-1111-1111-1111-111111111110', 'SV003', 'LHP004', 'TrucTiep', NOW(), 'admin', NULL, NULL, 'DangHoc', 1500000, 0, NOW())
ON CONFLICT ("MaSV", "MaLHP") DO NOTHING;

-- ============================================================
-- Kiểm tra nhanh
-- ============================================================
SELECT 'NguoiDung' AS bang, COUNT(*) FROM "NguoiDung" UNION ALL
SELECT 'SinhVien',  COUNT(*) FROM "SinhVien" UNION ALL
SELECT 'HocKy',     COUNT(*) FROM "HocKy" UNION ALL
SELECT 'MonHoc',    COUNT(*) FROM "MonHoc" UNION ALL
SELECT 'LopHocPhan', COUNT(*) FROM "LopHocPhan" UNION ALL
SELECT 'DangKyHocPhan', COUNT(*) FROM "DangKyHocPhan";
-- ============================================================================
-- Thêm cột lịch học vào bảng LopHocPhan (Neon PostgreSQL)
-- Mục đích: có dữ liệu thật để làm "check trùng lịch" (Module 3) và
--           "Thời khóa biểu" (Module 4). Hiện bảng LopHocPhan KHÔNG có cột
--           lịch học nào, nên 2 tính năng đó chưa thể làm bằng dữ liệu thật.
--
-- Cách chạy: đăng nhập Neon Console -> SQL Editor -> dán toàn bộ file này -> Run.
-- An toàn để chạy nhiều lần (dùng IF NOT EXISTS / kiểm tra constraint trước khi thêm).
-- Các lớp học phần đã có sẵn trong CSDL sẽ có 4 cột mới này là NULL cho tới khi
-- bạn nhập tay hoặc import lại qua Excel (nếu file Excel của bạn có cột lịch học).
-- ============================================================================

BEGIN;

ALTER TABLE "LopHocPhan"
    ADD COLUMN IF NOT EXISTS "Thu"         integer      NULL,  -- Thứ trong tuần: 2=Thứ 2 ... 7=Thứ 7, 8=Chủ nhật
    ADD COLUMN IF NOT EXISTS "TietBatDau"  integer      NULL,  -- Tiết học bắt đầu (ví dụ 1, 7, 13...)
    ADD COLUMN IF NOT EXISTS "SoTiet"      integer      NULL,  -- Số tiết liên tục (ví dụ 3)
    ADD COLUMN IF NOT EXISTS "Phong"       varchar(20)  NULL;  -- Mã phòng học, ví dụ "A1.03"

-- Ràng buộc hợp lệ cho dữ liệu (chỉ thêm nếu chưa có, để chạy lại script không báo lỗi)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'CK_LopHocPhan_Thu'
    ) THEN
        ALTER TABLE "LopHocPhan"
            ADD CONSTRAINT "CK_LopHocPhan_Thu" CHECK ("Thu" IS NULL OR "Thu" BETWEEN 2 AND 8);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'CK_LopHocPhan_TietBatDau'
    ) THEN
        ALTER TABLE "LopHocPhan"
            ADD CONSTRAINT "CK_LopHocPhan_TietBatDau" CHECK ("TietBatDau" IS NULL OR "TietBatDau" BETWEEN 1 AND 16);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'CK_LopHocPhan_SoTiet'
    ) THEN
        ALTER TABLE "LopHocPhan"
            ADD CONSTRAINT "CK_LopHocPhan_SoTiet" CHECK ("SoTiet" IS NULL OR "SoTiet" BETWEEN 1 AND 10);
    END IF;
END $$;

COMMIT;

-- Kiểm tra lại sau khi chạy:
-- SELECT "MaLHP", "Thu", "TietBatDau", "SoTiet", "Phong" FROM "LopHocPhan" LIMIT 20;

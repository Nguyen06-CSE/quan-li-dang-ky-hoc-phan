# 002 — SPEC CƠ SỞ DỮ LIỆU

CSDL PostgreSQL, truy cập qua EF Core (Code First + Migrations). Toàn bộ bảng có 2 cột audit chung: `NgayTao timestamptz default now()`, `NgayCapNhat timestamptz nullable`. Bảng có vòng đời nghiệp vụ dài (SinhVien, MonHoc, LopHocPhan, NguoiDung) có thêm `IsDeleted boolean default false` (soft delete).

## 2.1 Bảng `NguoiDung`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| Id | serial | PK |
| TenDangNhap | varchar(50) | unique, not null |
| MatKhauHash | varchar(255) | not null (BCrypt hash) |
| HoTen | varchar(100) | not null |
| Role | varchar(30) | not null — giá trị: `Admin`, `TroLyGiaoVu`, `GiaoVuBoMon`, `GiangVien` |
| MaGV | varchar(20) | nullable — liên kết nghiệp vụ tới GV trong `LopHocPhan.MaGV` khi Role = GiangVien |
| TrangThai | varchar(20) | default `HoatDong` — hoặc `DaKhoa` |

## 2.2 Bảng `SinhVien`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| MaSV | varchar(20) | PK |
| HoTen | varchar(100) | not null |
| LopSinhHoat | varchar(30) | |
| KhoaHoc | varchar(20) | VD "Khóa 45" |

## 2.3 Bảng `MonHoc`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| MaMon | varchar(20) | PK |
| TenMon | varchar(150) | not null |
| SoTinChiLT | int | not null, default 0 |
| SoTinChiTH | int | not null, default 0 |
| BacDaoTao | varchar(10) | VD "DH" |
| CHECK | | `SoTinChiLT + SoTinChiTH > 0` |

## 2.4 Bảng `HocKy`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| MaHocKy | varchar(20) | PK, VD `2026-2027-HK1` |
| TenHocKy | varchar(50) | not null |
| NgayBatDau | date | |
| NgayKetThuc | date | |
| DangMo | boolean | default true — học kỳ hiện hành để mặc định lọc |

## 2.5 Bảng `LopHocPhan`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| MaLHP | varchar(30) | PK |
| MaMon | varchar(20) | FK → MonHoc, not null |
| MaHocKy | varchar(20) | FK → HocKy, not null |
| MaGV | varchar(20) | FK → NguoiDung.MaGV, nullable |
| LoaiHinhDT | varchar(10) | VD "CQ", "VLVH" |
| SiSoToiDa | int | nullable |
| GiangDayOnline | boolean | default false |

Index: `(MaMon, MaHocKy)` để truy vấn nhanh danh sách LHP theo môn trong 1 học kỳ.

## 2.6 Bảng `DangKyHocPhan`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| Id | uuid | PK, default `gen_random_uuid()` |
| MaSV | varchar(20) | FK → SinhVien, not null |
| MaLHP | varchar(30) | FK → LopHocPhan, not null |
| HinhThucDK | varchar(10) | VD KH/HL/NKH/CT/NCT/HV |
| NgayDK | timestamptz | not null |
| NguoiDK | varchar(50) | tên đăng nhập người thực hiện |
| DiemSo | numeric(4,2) | nullable |
| DiemChu | varchar(5) | nullable |
| TrangThai | varchar(20) | not null, default `DangHoc` — hoặc `DaHuy` |
| SoTienPhaiDong | numeric(12,2) | tính lại bởi hệ thống, không lấy từ import |
| SoTienDaDong | numeric(12,2) | default 0 |

Ràng buộc:
- **Unique** `(MaSV, MaLHP)` — chặn đăng ký trùng chính xác 1 LHP.
- Không đặt unique cứng `(MaSV, MaMon)` vì cho phép học lại (xem `010-Spec-Man-DangKyHocPhan.md` mục xử lý cảnh báo mềm).

## 2.7 Bảng `CauHinhHeThong`
| Cột | Kiểu | Ràng buộc |
|---|---|---|
| Key | varchar(50) | PK |
| Value | varchar(50) | not null |
| MoTa | varchar(200) | |

Dữ liệu seed mặc định (Migration seed data):
| Key | Value | Mô tả |
|---|---|---|
| `SoTinChiToiThieu` | `10` | Số TC tối thiểu/học kỳ |
| `SoTinChiToiDa` | `25` | Số TC tối đa/học kỳ |
| `DonGiaTinChiLT` | `500000` | Đơn giá 1 TC lý thuyết (VNĐ) |
| `DonGiaTinChiTH` | `700000` | Đơn giá 1 TC thực hành (VNĐ) |

> Giá trị trên là **giả định demo**, phải cho phép Admin/Trợ lý giáo vụ sửa trong màn hình `013-Spec-Man-CauHinh.md`.

## 2.8 Quan hệ tổng thể (EF Core Fluent API — điểm cần chú ý)
- `LopHocPhan.MaMon` → `MonHoc.MaMon`: `OnDelete(DeleteBehavior.Restrict)` — không cho xóa Môn học nếu đã có LHP.
- `DangKyHocPhan.MaLHP` → `LopHocPhan.MaLHP`: `Restrict`.
- `DangKyHocPhan.MaSV` → `SinhVien.MaSV`: `Restrict`.
- Toàn bộ khóa chính dạng `varchar` giữ nguyên định dạng như trong file Excel gốc (không tự sinh số) để khớp dữ liệu import.

## 2.9 Migration
- Dùng `dotnet ef migrations add InitialCreate` sau khi định nghĩa xong entity + `AppDbContext`.
- Seed dữ liệu `CauHinhHeThong` mặc định và **1 tài khoản Admin mặc định** (VD `admin`/mật khẩu mặc định phải đổi khi đăng nhập lần đầu — ghi rõ trong README) ngay trong migration hoặc `DbContext.OnModelCreating` `HasData`.

## Checklist cho agent
- [ ] Tạo toàn bộ Entity trong `Core/Entities` đúng theo bảng trên.
- [ ] Tạo `AppDbContext` trong `Infrastructure/Data`, cấu hình Fluent API cho toàn bộ ràng buộc/relationship/index nêu trên.
- [ ] Tạo migration đầu tiên, seed cấu hình mặc định + tài khoản Admin mặc định.
- [ ] Test kết nối tới Neon PostgreSQL (dùng connection string trong `appsettings.Local.json`), chạy `dotnet ef database update` thành công.

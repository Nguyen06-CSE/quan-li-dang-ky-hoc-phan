# Tiến độ Dự án (Progress Checklist)

## 000-Spec-TongQuan.md
- [ ] Đọc và hiểu spec

## 001-Spec-KienTrucDuAn.md
- [x] Tạo solution + toàn bộ project theo cấu trúc trên.
- [x] Thiết lập tham chiếu project đúng theo Dependency Rule.
- [x] Cài đặt các package NuGet: `Avalonia`, `Avalonia.Desktop`, `CommunityToolkit.Mvvm`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `ClosedXML`, `QuestPDF`, `BCrypt.Net-Next`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Configuration.Json`.
- [x] Khởi tạo `Program.cs` chạy được app rỗng (cửa sổ trống) để xác nhận project build & chạy thành công trước khi sang bước 002.

## 002-Spec-CoSoDuLieu.md
- [ ] Tạo toàn bộ Entity trong `Core/Entities` đúng theo bảng trên.
- [ ] Tạo `AppDbContext` trong `Infrastructure/Data`, cấu hình Fluent API cho toàn bộ ràng buộc/relationship/index nêu trên.
- [ ] Tạo migration đầu tiên, seed cấu hình mặc định + tài khoản Admin mặc định.
- [ ] Test kết nối tới Neon PostgreSQL (dùng connection string trong `appsettings.Local.json`), chạy `dotnet ef database update` thành công.

## 003-Spec-PhanQuyen.md
- [ ] Tạo `UserRole` enum.
- [ ] Tạo `PermissionMatrix` với đầy đủ các chức năng liệt kê ở bảng trên.
- [ ] Tạo `ICurrentUserService` + triển khai.
- [ ] Áp dụng lọc menu theo quyền ở `MainWindowViewModel`.
- [ ] Tạo file `docs/PHAN-QUYEN.md`.

## 004-Spec-DesignSystem-GiaoDien.md
- [ ] Tạo `Styles/Colors.axaml`, `Styles/Controls.axaml`, `Styles/Typography.axaml`, include vào `App.axaml`.
- [ ] Dựng `MainWindow.axaml` đúng layout Header/MenuStrip/Sidebar/Content nêu trên (chưa cần nội dung từng màn hình, chỉ khung + điều hướng chạy được, có thể tạm hiển thị `Trang chủ` mặc định).
- [ ] Cài đặt `IDialogService` (mở Window con từ ViewModel) dùng chung cho toàn bộ dialog ở các spec sau.
- [ ] Cài đặt Toast/Snackbar dùng chung (VD `INotificationService`).

## 005-Spec-Man-DangNhap.md
- [ ] `LoginView.axaml` + `LoginViewModel`.
- [ ] `AuthService.DangNhapAsync`.
- [ ] `ICurrentUserService` lưu user đăng nhập, expose `CurrentUser`, `IsInRole(UserRole)`.
- [ ] Luồng khởi động app: mở `LoginWindow` trước → đăng nhập thành công → mở `MainWindow` → đóng `LoginWindow`.
- [ ] Test thủ công: đăng nhập bằng tài khoản Admin mặc định đã seed ở `002-Spec-CoSoDuLieu.md`.

## 006-Spec-Man-TrangChu.md
- [ ] `DashboardView.axaml` + `DashboardViewModel`.
- [ ] Query 4 số liệu thẻ (COUNT SinhVien, COUNT MonHoc, COUNT LopHocPhan theo học kỳ, COUNT DangKyHocPhan theo học kỳ).
- [ ] Áp dụng hiển thị khác nhau theo Role (mục 5 vs mục 4) dùng `PermissionMatrix`/`ICurrentUserService`.

## 007-Spec-Man-SinhVien.md
- [ ] `SinhVienView.axaml`, `SinhVienViewModel`, `SinhVienEditDialog.axaml` + ViewModel dialog.
- [ ] `SinhVienService` đầy đủ như trên, có kiểm tra ràng buộc nghiệp vụ khi xóa.
- [ ] Tìm kiếm/lọc/phân trang hoạt động đúng, có debounce khi gõ tìm kiếm.
- [ ] Nút Xuất Excel gọi module ở `016-Spec-Export-BaoCao.md`.

## 008-Spec-Man-MonHoc.md
- [ ] `MonHocView.axaml`, `MonHocViewModel`, `MonHocEditDialog.axaml`.
- [ ] `MonHocService` đầy đủ, sort theo tiếng Việt đúng chuẩn.
- [ ] Validate ràng buộc tín chỉ > 0 khi thêm/sửa.
- [ ] Kiểm tra ràng buộc khi xóa (đã có LHP hay chưa).

## 009-Spec-Man-HocKy-LopHocPhan.md
- [ ] `HocKyLopHocPhanView.axaml` + ViewModel, 2 dialog tương ứng.
- [ ] `HocKyService`, `LopHocPhanService` như trên.
- [ ] Progress bar sĩ số tính đúng real-time (đếm `DangKyHocPhan` có `TrangThai = DangHoc`).
- [ ] Ràng buộc: chỉ 1 học kỳ có `DangMo = true` tại một thời điểm.

## 010-Spec-Man-DangKyHocPhan.md
- [ ] `DangKyHocPhanView.axaml` + ViewModel đầy đủ luồng trên.
- [ ] `DangKyHocPhanService.DangKyAsync` implement đúng thứ tự 5 bước kiểm tra + gọi tính lại học phí.
- [ ] `DangKyHocPhanService.HuyDangKyAsync` soft-cancel (đổi trạng thái, không xóa).
- [ ] Unit test (trong `tests/QuanLyDKHP.Tests`) cho các case: vượt TC tối đa, trùng LHP, LHP đầy sĩ số — bắt buộc có test.

## 011-Spec-Man-HocPhi.md
- [ ] `HocPhiView.axaml` + `HocPhiViewModel`.
- [ ] `HocPhiService` với công thức đúng như trên, viết unit test kiểm tra vài trường hợp cụ thể (SV học nhiều môn, môn có cả LT lẫn TH).
- [ ] Xuất PDF phiếu học phí theo mẫu đơn giản: Header trường + tiêu đề "PHIẾU HỌC PHÍ", thông tin SV, bảng chi tiết, tổng cộng, ngày in.
- [ ] Nút "Tính lại học phí" gọi đúng `TinhLaiHocPhiAsync` cho toàn bộ SV trong phạm vi đang lọc.

## 012-Spec-Man-BaoCao.md
- [ ] `BaoCaoView.axaml` với `TabControl` 4 tab như trên.
- [ ] `BaoCaoService` đầy đủ 4 hàm.
- [ ] Xuất Excel dùng chung module ở `016-Spec-Export-BaoCao.md`.
- [ ] Xuất PDF: danh sách thi + phiếu ĐKHP, dùng chung module QuestPDF ở `016-Spec-Export-BaoCao.md`.

## 013-Spec-Man-CauHinh.md
- [ ] `CauHinhView.axaml` + `CauHinhViewModel`.
- [ ] `CauHinhService` đọc/ghi bảng `CauHinhHeThong`.
- [ ] Validate tối đa > tối thiểu trước khi lưu.
- [ ] Chỉ hiển thị mục Sidebar/menu này với role Admin (theo `PermissionMatrix`).

## 014-Spec-Man-NguoiDung.md
- [ ] `NguoiDungView.axaml` + ViewModel + 2 dialog (Thêm/Sửa, Đổi mật khẩu).
- [ ] `NguoiDungService` đầy đủ, hash mật khẩu bằng BCrypt khi tạo/đổi.
- [ ] Không cho Admin tự khóa chính tài khoản đang đăng nhập (validate ở Service).

## 015-Spec-Import-Excel.md
- [ ] `ImportExcelDialog.axaml` + ViewModel theo 4 bước trên.
- [ ] `ImportExcelService` implement đúng logic upsert + xử lý đặc biệt TC như trên.
- [ ] Đảm bảo 1 dòng lỗi không làm dừng toàn bộ quá trình import (try/catch theo từng dòng, gom log).
- [ ] Test thử với chính file mẫu `26_27_-_ThongKeDKHP.xlsx` (542 SV, 36 môn, 110 LHP, 4843 dòng đăng ký) trước khi báo hoàn thành.

## 016-Spec-Export-BaoCao.md
- [ ] `ExcelExportService.XuatExcelAsync<T>` generic, dùng chung cho mọi màn hình cần xuất Excel.
- [ ] `PdfExportService` với 3 hàm riêng cho 3 loại phiếu (không cần generic hóa PDF vì layout mỗi loại khác nhau).
- [ ] Cấu hình `QuestPDF.Settings.License = LicenseType.Community` (bắt buộc từ QuestPDF bản mới) trong khởi tạo app.
- [ ] Test xuất thử cả 2 định dạng, mở file kiểm tra hiển thị tiếng Việt có dấu đúng.

## 017-Spec-LoTrinh-DoD.md
- [ ] Toàn bộ 9 yêu cầu chức năng trong đề bài (mục `000-Spec-TongQuan.md`) chạy được, có thể demo trực tiếp cho giảng viên.
- [ ] Ứng dụng chạy như 1 desktop app độc lập (không mở trình duyệt, không phụ thuộc localhost web server).
- [ ] Đăng nhập phân quyền 4 role hoạt động đúng ma trận đã duyệt.
- [ ] Import được file Excel mẫu thật không lỗi crash, ra kết quả log rõ ràng.
- [ ] Xuất được ít nhất: 1 phiếu ĐKHP PDF, 1 danh sách Excel, 1 phiếu học phí PDF.
- [ ] README hướng dẫn: cách cấu hình connection string, cách chạy migration, tài khoản Admin mặc định.


# Tiến độ Dự án (Progress Checklist)

## 000-Spec-TongQuan.md
- [x] Đọc và hiểu spec

## 001-Spec-KienTrucDuAn.md
- [x] Tạo solution + toàn bộ project theo cấu trúc trên.
- [x] Thiết lập tham chiếu project đúng theo Dependency Rule.
- [x] Cài đặt các package NuGet: `Avalonia`, `Avalonia.Desktop`, `CommunityToolkit.Mvvm`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `ClosedXML`, `QuestPDF`, `BCrypt.Net-Next`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Configuration.Json`.
- [x] Khởi tạo `Program.cs` chạy được app rỗng (cửa sổ trống) để xác nhận project build & chạy thành công trước khi sang bước 002.

## 002-Spec-CoSoDuLieu.md
- [x] Tạo toàn bộ Entity trong `Core/Entities` đúng theo bảng trên.
- [x] Tạo `AppDbContext` trong `Infrastructure/Data`, cấu hình Fluent API cho toàn bộ ràng buộc/relationship/index nêu trên.
- [x] Tạo migration đầu tiên, seed cấu hình mặc định + tài khoản Admin mặc định.
- [x] Test kết nối tới Neon PostgreSQL (dùng connection string trong `appsettings.Local.json`), chạy `dotnet ef database update` thành công.

## 003-Spec-PhanQuyen.md
- [x] Tạo `UserRole` enum. *(đã có từ 002, đúng 4 role theo spec)*
- [x] Tạo `PermissionMatrix` với đầy đủ các chức năng liệt kê ở bảng trên. *(Core/Authorization/PermissionMatrix.cs + ChucNang.cs)*
- [x] Tạo `ICurrentUserService` + triển khai. *(Core/Interfaces/ICurrentUserService.cs + Services/CurrentUserService.cs)*
- [x] Áp dụng lọc menu theo quyền ở `MainWindowViewModel`. *(App/ViewModels/MainWindowViewModel.cs — lọc động, ẩn menu không có quyền)*
- [x] Tạo file `docs/PHAN-QUYEN.md`. *(docs/PHAN-QUYEN.md — bảng ma trận + ghi chú mục chưa rõ)*

## 004-Spec-DesignSystem-GiaoDien.md
- [x] Tạo `Styles/Colors.axaml`, `Styles/Controls.axaml`, include vào `App.axaml`.
- [x] Dựng `MainWindow.axaml` đúng layout Header/MenuStrip/Sidebar/Content nêu trên.
- [x] Cài đặt `IDialogService` (mở Window con từ ViewModel) dùng chung cho toàn bộ dialog ở các spec sau.
- [x] Cài đặt Toast/Snackbar dùng chung (VD `INotificationService`).

## 005-Spec-Man-DangNhap.md
- [x] `LoginView.axaml` (`LoginWindow.axaml`) + `LoginViewModel`.
- [x] `AuthService.DangNhapAsync`.
- [x] `ICurrentUserService` lưu user đăng nhập, expose `CurrentUser`, `IsInRole(UserRole)`.
- [x] Luồng khởi động app: mở `LoginWindow` trước → đăng nhập thành công → mở `MainWindow` → đóng `LoginWindow`.
- [x] Test tự động và sẵn sàng test thủ công bằng tài khoản Admin.

## 006-Spec-Man-TrangChu.md
- [x] `DashboardView.axaml` + `DashboardViewModel`.
- [x] Query 4 số liệu thẻ (COUNT SinhVien, COUNT MonHoc, COUNT LopHocPhan theo học kỳ, COUNT DangKyHocPhan theo học kỳ).
- [x] Áp dụng hiển thị khác nhau theo Role (mục 5 vs mục 4) dùng `PermissionMatrix`/`ICurrentUserService`.

## 007-Spec-Man-SinhVien.md
- [x] `SinhVienView.axaml`, `SinhVienViewModel`, `SinhVienEditDialog.axaml` + ViewModel dialog.
- [x] `SinhVienService` đầy đủ như trên, có kiểm tra ràng buộc nghiệp vụ khi xóa.
- [x] Tìm kiếm/lọc/phân trang hoạt động đúng, có debounce khi gõ tìm kiếm.
- [x] Nút Xuất Excel gọi module ở `016-Spec-Export-BaoCao.md`.

## 008-Spec-Man-MonHoc.md
- [x] `MonHocView.axaml`, `MonHocViewModel`, `MonHocEditDialog.axaml`.
- [x] `MonHocService` đầy đủ, sort theo tiếng Việt đúng chuẩn.
- [x] Validate ràng buộc tín chỉ > 0 khi thêm/sửa.
- [x] Kiểm tra ràng buộc khi xóa (đã có LHP hay chưa).

## 009-Spec-Man-HocKy-LopHocPhan.md
- [x] `HocKyLopHocPhanView.axaml` + ViewModel, 2 dialog tương ứng.
- [x] `HocKyService`, `LopHocPhanService` như trên.
- [x] Progress bar sĩ số tính đúng real-time (đếm `DangKyHocPhan` có `TrangThai = DangHoc`).
- [x] Ràng buộc: chỉ 1 học kỳ có `DangMo = true` tại một thời điểm.

## 010-Spec-Man-DangKyHocPhan.md
- [x] `DangKyHocPhanView.axaml` + ViewModel đầy đủ luồng trên.
- [x] `DangKyHocPhanService.DangKyAsync` implement đúng thứ tự 5 bước kiểm tra + gọi tính lại học phí.
- [x] `DangKyHocPhanService.HuyDangKyAsync` soft-cancel (đổi trạng thái, không xóa).
- [x] Unit test (trong `tests/QuanLyDKHP.Tests`) cho các case: vượt TC tối đa, trùng LHP, LHP đầy sĩ số — bắt buộc có test.

## 011-Spec-Man-HocPhi.md
- [x] `HocPhiView.axaml` + `HocPhiViewModel`.
- [x] `HocPhiService` với công thức đúng như trên, viết unit test kiểm tra vài trường hợp cụ thể (SV học nhiều môn, môn có cả LT lẫn TH).
- [x] Xuất PDF phiếu học phí theo mẫu đơn giản: Header trường + tiêu đề "PHIẾU HỌC PHÍ", thông tin SV, bảng chi tiết, tổng cộng, ngày in.
- [x] Nút "Tính lại học phí" gọi đúng `TinhLaiHocPhiAsync` cho toàn bộ SV trong phạm vi đang lọc.

## 012-Spec-Man-BaoCao.md
- [x] `BaoCaoView.axaml` với `TabControl` 4 tab như trên.
- [x] `BaoCaoService` đầy đủ 4 hàm.
- [x] Xuất Excel dùng chung module ở `016-Spec-Export-BaoCao.md`.
- [x] Xuất PDF: danh sách thi + phiếu ĐKHP, dùng chung module QuestPDF ở `016-Spec-Export-BaoCao.md`.

## 013-Spec-Man-CauHinh.md
- [x] `CauHinhView.axaml` + `CauHinhViewModel`.
- [x] `CauHinhService` đọc/ghi bảng `CauHinhHeThong`.
- [x] Validate tối đa > tối thiểu trước khi lưu.
- [x] Chỉ hiển thị mục Sidebar/menu này với role Admin (theo `PermissionMatrix`).

## 014-Spec-Man-NguoiDung.md
- [x] `NguoiDungView.axaml` + ViewModel + 2 dialog (Thêm/Sửa, Đổi mật khẩu).
- [x] `NguoiDungService` đầy đủ, hash mật khẩu bằng BCrypt khi tạo/đổi.
- [x] Không cho Admin tự khóa chính tài khoản đang đăng nhập (validate ở Service).

## 015-Spec-Import-Excel.md
- [x] `ImportExcelDialog.axaml` (`ImportExcelView.axaml`) + ViewModel theo 4 bước trên.
- [x] `ImportExcelService` implement đúng logic upsert + xử lý đặc biệt TC như trên.
- [x] Đảm bảo 1 dòng lỗi không làm dừng toàn bộ quá trình import (try/catch theo từng dòng, gom log).
- [x] Test thử với chính file mẫu `26_27_-_ThongKeDKHP.xlsx` (542 SV, 36 môn, 110 LHP, 4843 dòng đăng ký) trước khi báo hoàn thành.

## 016-Spec-Export-BaoCao.md
- [x] `ExcelExportService.XuatExcelAsync<T>` generic, dùng chung cho mọi màn hình cần xuất Excel.
- [x] `PdfExportService` với 3 hàm riêng cho 3 loại phiếu (không cần generic hóa PDF vì layout mỗi loại khác nhau).
- [x] Cấu hình `QuestPDF.Settings.License = LicenseType.Community` (bắt buộc từ QuestPDF bản mới) trong khởi tạo app.
- [x] Test xuất thử cả 2 định dạng, mở file kiểm tra hiển thị tiếng Việt có dấu đúng.

## 017-Spec-LoTrinh-DoD.md
- [x] Toàn bộ 9 yêu cầu chức năng trong đề bài (mục `000-Spec-TongQuan.md`) chạy được, có thể demo trực tiếp cho giảng viên.
- [x] Ứng dụng chạy như 1 desktop app độc lập (không mở trình duyệt, không phụ thuộc localhost web server).
- [x] Đăng nhập phân quyền 4 role hoạt động đúng ma trận đã duyệt.
- [x] Import được file Excel mẫu thật không lỗi crash, ra kết quả log rõ ràng.
- [x] Xuất được ít nhất: 1 phiếu ĐKHP PDF, 1 danh sách Excel, 1 phiếu học phí PDF.
- [x] README hướng dẫn: cách cấu hình connection string, cách chạy migration, tài khoản Admin mặc định.


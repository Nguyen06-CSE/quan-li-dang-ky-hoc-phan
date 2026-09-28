# FIX.md — Nhật ký lỗi đã gặp & cách xử lý

Cập nhật: 28/09/2026. Trạng thái: ✅ đã sửa · ⚠️ đã sửa nhưng chưa build/chạy thử · ❌ chưa sửa

---

## 1. macOS báo "QuanLyDKHP.App.app bị hỏng và không thể mở được" ✅
- **Triệu chứng:** dialog "bị hỏng… nên di chuyển vào Thùng rác" khi `dotnet run`, dù `dotnet build` thành công.
- **Nguyên nhân:** không phải lỗi code. File tải về dưới dạng zip bị macOS gắn cờ quarantine (`com.apple.quarantine`); Gatekeeper chặn file thực thi/dylib chưa ký. Mỗi lần giải nén zip mới vào thư mục mới thì bị lại.
- **Cách xử lý:** bấm **Hủy** (không bấm "Chuyển vào Thùng rác"), rồi:
  ```bash
  cd <thư mục gốc project>
  xattr -cr .
  find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
  cd src/QuanLyDKHP.App && dotnet run
  ```
  Nếu vẫn bị: ký ad-hoc sau khi build
  ```bash
  find bin -name "*.dylib" -exec codesign --force --sign - {} \;
  codesign --force --sign - bin/Debug/net8.0/QuanLyDKHP.App
  dotnet run --no-build
  ```
- **Phòng tránh:** giải nén bằng Terminal (`unzip file.zip -d <thư mục>`) hoặc dùng git thay vì tải zip.

## 2. `dotnet run` báo "Couldn't find a project to run" / "The provided file path does not exist" ✅
- **Nguyên nhân:** chạy sai thư mục. File `.csproj` nằm ở `src/QuanLyDKHP.App`, không phải thư mục gốc; và khi đã đứng trong thư mục đó thì `--project src/QuanLyDKHP.App` thành đường dẫn sai.
- **Cách xử lý:**
  ```bash
  cd <gốc>/src/QuanLyDKHP.App && dotnet run
  # hoặc, đứng ở thư mục gốc:
  dotnet run --project src/QuanLyDKHP.App
  ```
- Không tìm thấy project → `find <gốc> -name "QuanLyDKHP.App.csproj"` để xem có bị lồng thư mục khi giải nén không.

## 3. EF Core: "A second operation was started on this context instance…" — mọi màn hình không đọc được dữ liệu ⚠️
- **Triệu chứng:** thanh lỗi "Lỗi khởi tạo dữ liệu: A second operation was started on this context instance before a previous operation completed", bảng dữ liệu trống. Xảy ra ở tất cả màn hình.
- **Nguyên nhân gốc:** `App.axaml.cs` đăng ký `AddDbContext` (Scoped) nhưng app desktop không bao giờ tạo scope con (`BuildServiceProvider()` rồi resolve thẳng từ root) → **cả app dùng chung 1 `AppDbContext`**. EF Core không cho 2 thao tác async chạy chồng nhau trên cùng 1 context.
- **Nguyên nhân kích hoạt ở màn Sinh viên:** `InitFiltersAndLoadAsync` gán `LopFilter`/`KhoaHocFilter` → mỗi setter gọi `_ = LoadDataAsync()` (không await), rồi code lại `await LoadDataAsync()` → 3 truy vấn song song trên 1 context.
- **Đã sửa:**
  1. `App.axaml.cs`: `AddDbContext` → `AddDbContextFactory`.
  2. 10 Repository (`BaoCao`, `CauHinh`, `DangKyHocPhan`, `Dashboard`, `HocKy`, `ImportExcel`, `LopHocPhan`, `MonHoc`, `NguoiDung`, `SinhVien`) nhận `IDbContextFactory<AppDbContext>`; mỗi phương thức tự tạo context riêng (`await using var _context = await _contextFactory.CreateDbContextAsync();`).
  3. `SinhVienViewModel`: thêm cờ `_dangKhoiTao` để chặn `OnLopFilterChanged`/`OnKhoaHocFilterChanged` gọi tải dữ liệu trong lúc khởi tạo.
  4. `LopHocPhanRepository.XoaAsync`: bản gốc gọi `GetByIdAsync` rồi lưu bằng context khác → khi tách context, thay đổi `IsDeleted` sẽ không được lưu. Đã sửa: tự truy vấn + lưu trên cùng 1 context.
- **Lưu ý:** cập nhật entity qua `DbSet.Update(entity)` (entity load từ context khác) là cách đúng với mô hình factory-per-call.
- **Cần kiểm tra:** mở lần lượt Sinh viên, Môn học, Học kỳ/LHP, Đăng ký HP, Học phí, Báo cáo, Dashboard.

## 4. Import Excel — chậm với file lớn + có thể mất toàn bộ dữ liệu khi lỗi ❌ (chưa sửa)
File: `Infrastructure/Repositories/ImportExcelRepository.cs`
- **Không try/catch quanh `SaveChangesAsync` cuối cùng** (~dòng 312): 1 dòng vi phạm ràng buộc DB → cả import fail, 0 dòng được lưu dù log báo thành công; ViewModel hiện nguyên message lỗi thô của Npgsql. Trái với spec 015 ("lỗi 1 dòng không làm hỏng toàn bộ import").
- **Dòng lỗi vẫn để lại entity trong change tracker:** `Add()` xong mới lỗi ở bước sau → entity dở dang vẫn được lưu.
- **N+1 khi tính lại học phí:** với mỗi sinh viên gọi `TinhLaiHocPhiAsync` → mỗi lần load lại + `CapNhatAsync` gọi `SaveChangesAsync` **cho từng dòng đăng ký**. File 10.000 dòng ≈ hàng nghìn round-trip tới Neon.
- **Change tracking:** cache tải toàn bộ bảng không `AsNoTracking`; `Add` nhiều lần với `AutoDetectChanges` bật làm chậm dần theo số dòng.
- **Code chết:** `Services/ImportExcelService.cs` inject chính `IImportExcelService`, không được DI dùng (DI trỏ thẳng `ImportExcelRepository`). Logic nghiệp vụ import đang nằm sai tầng.
- **Hướng sửa:** transaction + rollback rõ ràng; lưu theo lô (~500 dòng); tính học phí theo lô, 1 `SaveChanges`; tách `IImportExcelRepository`.

## 5. NullReferenceException khi bấm menu "Tệp → Import Excel" ⚠️
- **Nguồn:** `docs/bug.md`. Stack trace: `MainWindowViewModel.NavigateTo` (dòng 168).
- **Nguyên nhân:** `MenuItem` trên menu strip gắn `Command="{Binding NavigateToCommand}"` nhưng không có `CommandParameter` → `menuItem` = null.
- **Đã sửa:** (a) thêm `if (menuItem == null) return;`; (b) menu strip cũ bị thay bằng Top Bar ở Module 2 nên nguồn lỗi không còn.
- **Vẫn còn (bug.md):** các nút ở Trang chủ chưa có chức năng; màn "Học kỳ / LHP" chưa xây xong.

## 6. Bảo mật: mật khẩu database nằm plaintext trong source ❌
- `Infrastructure/Data/AppDbContextFactory.cs` chứa nguyên connection string Neon (host + user + password).
- **Việc cần làm:** đổi mật khẩu trên Neon; đọc từ biến môi trường / `appsettings.Local.json` (đã có trong `.gitignore`); kiểm tra lịch sử git nếu từng push.

## 7. Ghi chú Module 1 & Module 2 (giao diện)
- Module 1 (Đăng nhập): "Lưu đăng nhập" chỉ nhớ tên đăng nhập (không có token/session vì `AuthService` xác thực trực tiếp qua DB, không có API).
- Module 2 (App Shell): thay header xanh + menu strip bằng sidebar thu gọn được + top bar (breadcrumb, tìm nhanh màn hình, chuông, nút Sáng/Tối, avatar). Dark mode qua `ThemeDictionaries` trong `Styles/Colors.axaml`.
- **Giới hạn đã biết:** icon sidebar vẫn là emoji nên "nét → mảng" được mô phỏng bằng độ mờ (cần bộ icon như Projektanker/Material.Icons để đổi thật); chuông chưa có hệ thống thông báo; chế độ Sáng/Tối chưa lưu lại giữa các lần mở app; các view còn màu hex cứng (overlay `#40000000`, badge vai trò) chưa đổi theo theme.
- **Chưa build/chạy thử** phần Module 2 — cần `dotnet build` và xem lại giao diện thật.

## 8. Module 2.5 — Phong cách Liquid / iOS glass (28/09/2026) ⚠️ chưa build/chạy thử
- `Styles/Colors.axaml`: thêm token kính mờ (`BrushCanvas`, `BrushGlass*`, `BrushHero`, `BrushOnHero*`, `BrushOnPrimary`) cho cả Light & Dark.
- `Styles/Controls.axaml` (dùng chung mọi màn): nút chính bo 14 + nảy khi hover/nhấn (`TransformOperationsTransition`), ô nhập bo 12–14, thẻ `Border.glass-card`, `glass-bar`, `glass-side`, class `TextBox.invalid` (viền đỏ).
- `LoginWindow.axaml` & `MainWindow.axaml`: đồ họa chuyển hết vào `Window.Styles` bằng `Classes`; `x:Name` và `Binding` giữ nguyên (đã đối chiếu bằng script).
- **Thay đổi cấu trúc duy nhất:** thêm 1 `Border.glass-card` bọc form đăng nhập để có tấm kính.
- **Chưa làm được thật:** blur nền (acrylic) — hiện là kính "giả" bằng màu bán trong suốt + đổ bóng; muốn blur thật cần `TransparencyLevelHint="AcrylicBlur"` trên Window (cần thử trên macOS).
- **Rủi ro cần kiểm tra khi chạy:** hiệu ứng nảy nút (`scale(...)`), animation hiện dần của thẻ đăng nhập.

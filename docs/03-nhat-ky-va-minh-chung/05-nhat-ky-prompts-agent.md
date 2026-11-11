---
id: nhat-ky-prompts-agent
title: Minh Chứng Tương Tác & Prompts Với AI Agent
sidebar_position: 5
---

# Minh Chứng Tương Tác & Prompts Với AI Agent

## 1. Phương Pháp Luận: Lập Trình Đôi Với AI Agent (AI Pair Programming)

Theo yêu cầu nộp bài của môn học "Xây dựng ứng dụng Desktop", nhóm phát triển sử dụng công nghệ trợ lý lập trình thông minh (AI Agent) như một kỹ sư cộng tác đồng hành xuyên suốt quá trình thiết kế và phát triển.

Quy trình phối hợp với AI Agent được chuẩn hóa theo mô hình 4 bước nghiêm ngặt:
1. **Định hình Đặc tả (Specification Ingestion):** Soạn thảo tài liệu đặc tả chi tiết (`.agent/spec/000` đến `017`) xác định rõ ràng yêu cầu nghiệp vụ, tiêu chuẩn kiểm thử và tiêu chí nghiệm thu (Definition of Done - DoD).
2. **Kỹ thuật Câu lệnh (Prompt Engineering):** Đưa ra các câu lệnh có cấu trúc ngữ cảnh chặt chẽ, giới hạn phạm vi trong một tệp spec duy nhất và yêu cầu tuân thủ nguyên lý Clean Architecture.
3. **Sinh mã & Kiểm thử Tức thì (Code Generation & TDD Verification):** Agent thực thi viết mã và đồng thời tạo các bộ Unit Test để xác thực tự động.
4. **Ghi nhật ký & Đánh giá Nghiệm thu (Session Logging & Audit):** Toàn bộ kết quả thực hiện, các quyết định kiến trúc và bài test được lưu trữ vào `.agent/session-log/` và `.agent/decisions.md`.

---

## 2. Nhật Ký 10 Phiên Làm Việc Với AI Agent (Prompt Session Logs)

### Phiên 1: Khởi Tạo Kiến Trúc Clean Architecture & Solution .NET 8
* **Thời gian:** 18/09/2026 13:23
* **Tài liệu tham chiếu:** `.agent/spec/001-Spec-KienTrucDuAn.md`
* **Prompt chính đã thực hiện:**
  > *"Hãy khởi tạo solution QuanLyDangKyHocPhan.sln trên nền tảng .NET 8 gồm 5 project: App, Core, Infrastructure, Services và Tests theo đúng cấu trúc Clean Architecture. Cấu hình các project reference tuân thủ Dependency Rule và cài đặt các gói NuGet cần thiết phiên bản 8.x."*
* **Kết quả đạt được:** Tạo thành công 5 project, thiết lập luồng tham chiếu hợp lệ (`App -> Services -> Core <- Infrastructure`), biên dịch `dotnet build` đạt 0 lỗi 0 cảnh báo.
* **Tệp log:** `.agent/session-log/2026-09-18_13-23.md`.

---

### Phiên 2: Thiết Kế Thực Thể CSDL, Fluent API & Migration Đầu Tiên
* **Thời gian:** 18/09/2026 13:32
* **Tài liệu tham chiếu:** `.agent/spec/002-Spec-CoSoDuLieu.md`
* **Prompt chính đã thực hiện:**
  > *"Dựa trên đặc tả 002, hãy tạo toàn bộ các entity miền trong Core: SinhVien, MonHoc, HocKy, LopHocPhan, DangKyHocPhan, NguoiDung, CauHinhHeThong. Xây dựng giao diện IAuditableEntity và ISoftDeleteEntity. Cấu hình Fluent API trong AppDbContext để thiết lập quan hệ khóa ngoại, ràng buộc kiểm tra và tạo migration InitialCreate."*
* **Kết quả đạt được:** Hoàn thành schema CSDL 7 bảng, override `SaveChangesAsync` tự động gán ngày cập nhật, sinh thành công migration đầu tiên tới Neon PostgreSQL.
* **Tệp log:** `.agent/session-log/2026-09-18_13-32.md`.

---

### Phiên 3: Xây Dựng Hệ Thống Phân Quyền 4 Vai Trò
* **Thời gian:** 18/09/2026 14:00
* **Tài liệu tham chiếu:** `.agent/spec/003-Spec-PhanQuyen.md`
* **Prompt chính đã thực hiện:**
  > *"Hãy hiện thực hóa ma trận phân quyền theo spec 003: Định nghĩa ChucNang constants cho 15 chức năng nghiệp vụ, xây dựng PermissionMatrix kiểm tra quyền cho 4 role (Admin, TroLyGiaoVu, GiaoVuBoMon, GiangVien) và tạo ICurrentUserService để theo dõi phiên làm việc."*
* **Kết quả đạt được:** Tách riêng quyền `TinhHocPhi` và `XemHocPhi`, thiết lập cơ chế xác thực kép (Lớp UI và Lớp Service).
* **Tệp log:** `.agent/session-log/2026-09-18_14-00.md`.

---

### Phiên 4: Thiết Kế Design System & Màn Hình Đăng Nhập
* **Thời gian:** 18/09/2026 14:50
* **Tài liệu tham chiếu:** `.agent/spec/004-Spec-DesignSystem-GiaoDien.md` & `005-Spec-Man-DangNhap.md`
* **Prompt chính đã thực hiện:**
  > *"Xây dựng hệ thống tài nguyên giao diện Colors.axaml và Controls.axaml cho Avalonia UI. Sau đó hiện thực hóa màn hình LoginWindow dạng Split-Screen, hỗ trợ hiệu ứng rung khi nhập sai, mã hóa mật khẩu bằng BCrypt và lưu tên tài khoản vào LocalSessionStore."*
* **Kết quả đạt được:** Giao diện đăng nhập hiện đại, tích hợp an toàn BCrypt, viết bộ kiểm thử `AuthServiceTests.cs`.
* **Tệp log:** `.agent/session-log/2026-09-18_14-50.md`.

---

### Phiên 5: Xây Dựng Dashboard Trang Chủ & Thống Kê
* **Thời gian:** 18/09/2026 15:26
* **Tài liệu tham chiếu:** `.agent/spec/006-Spec-Man-TrangChu.md`
* **Prompt chính đã thực hiện:**
  > *"Thiết kế màn hình Dashboard hiển thị 4 thẻ chỉ số thống kê (Tổng số SV, Môn học, LHP mở, Lượt ĐKHP) và bảng danh sách lớp giảng dạy cho Giảng viên. Đảm bảo truy vấn qua IDashboardRepository tuân thủ Clean Architecture."*
* **Kết quả đạt được:** Hoàn thành `TrangChuView.axaml` và `DashboardRepository.cs`, xử lý dứt điểm lỗi Disposed DbContext khi mở màn hình chính.
* **Tệp log:** `.agent/session-log/2026-09-18_15-26.md`.

---

### Phiên 6: Triển Khai CRUD Sinh Viên & Môn Học (Sắp Xếp Tiếng Việt)
* **Thời gian:** 18/09/2026 16:43
* **Tài liệu tham chiếu:** `.agent/spec/007-Spec-Man-SinhVien.md` & `008-Spec-Man-MonHoc.md`
* **Prompt chính đã thực hiện:**
  > *"Xây dựng phân hệ Quản lý Sinh viên và Quản lý Môn học. Sử dụng SinhVienDto và MonHocDto để mang dữ liệu tính toán. Cấu hình sắp xếp danh mục môn học theo đúng thứ tự từ điển tiếng Việt bằng CultureInfo('vi-VN')."*
* **Kết quả đạt được:** Hoàn thành 2 màn hình quản trị, hỗ trợ phân trang và tìm kiếm mượt mà, bảo đảm thứ tự bảng chữ cái tiếng Việt chuẩn mực.
* **Tệp log:** `.agent/session-log/2026-09-18_16-43.md`.

---

### Phiên 7: Quản Lý Học Kỳ & Lớp Học Phần
* **Thời gian:** 18/09/2026 19:35
* **Tài liệu tham chiếu:** `.agent/spec/009-Spec-Man-HocKy-LopHocPhan.md`
* **Prompt chính đã thực hiện:**
  > *"Triển khai màn hình Học kỳ và Lớp học phần. Ràng buộc chỉ có duy nhất một học kỳ mở đăng ký. Tính toán sĩ số sinh viên thực tế đang học theo thời gian thực và hiển thị tỷ lệ lấp đầy."*
* **Kết quả đạt được:** Hoàn thành `HocKyLopHocPhanViewModel.cs`, bổ sung chức năng đổi nhanh học kỳ hiện hành và bảo vệ ràng buộc toàn vẹn khi xóa lớp.
* **Tệp log:** `.agent/session-log/2026-09-18_19-35-00.md`.

---

### Phiên 8: Quy Trình Đăng Ký Học Phần 6 Bước & 9 Unit Tests
* **Thời gian:** 18/09/2026 20:12
* **Tài liệu tham chiếu:** `.agent/spec/010-Spec-Man-DangKyHocPhan.md`
* **Prompt chính đã thực hiện:**
  > *"Hiện thực hóa DangKyHocPhanService với đầy đủ luồng kiểm tra tuần tự: Chặn trùng LHP, cảnh báo mềm trùng môn, chặn quá sĩ số, chặn vượt tín chỉ tối đa, tự động gọi tính lại học phí. Viết toàn bộ 9 bài unit test bao phủ toàn bộ các trường hợp biên."*
* **Kết quả đạt được:** Hoàn thành luồng đăng ký phức tạp nhất hệ thống, chạy kiểm thử tự động 19/19 unit test đạt 100% Passed.
* **Tệp log:** `.agent/session-log/2026-09-18_20-12-00.md`.

---

### Phiên 9: Quản Lý Tính Học Phí & Xuất Báo Cáo
* **Thời gian:** 19/09/2026 12:12
* **Tài liệu tham chiếu:** `.agent/spec/011-Spec-Man-HocPhi.md`
* **Prompt chính đã thực hiện:**
  > *"Xây dựng màn hình Tính học phí theo công thức quy đổi tín chỉ LT và TH. Hỗ trợ 3 góc nhìn tra cứu (theo SV, theo lớp sinh hoạt, theo khóa). Tích hợp xuất phiếu học phí PDF khổ in A4 bằng QuestPDF."*
* **Kết quả đạt được:** Hoàn thành phân hệ tài chính đào tạo, tính toán tự động số tiền phải nộp và viết 4 unit test kiểm thử logic tính tiền.
* **Tệp log:** `.agent/session-log/2026-09-19-1212.md`.

---

### Phiên 10: Xử Lý Lỗi Xem Trước Phiếu Kết Quả ĐKHP & Giao Diện Chuẩn Hóa
* **Thời gian:** 20/09/2026 10:15
* **Tài liệu tham chiếu:** `.agent/spec/012-Spec-Man-BaoCao.md`
* **Prompt chính đã thực hiện:**
  > *"Kiểm tra và sửa lỗi Tab 4 Báo cáo: Khi bấm 'Xem lại phiếu kết quả ĐKHP' không hiển thị dữ liệu nếu thiếu dữ liệu đầu vào. Bổ sung thông báo trạng thái rõ ràng và cập nhật tiêu đề cơ quan chủ quản thành TRƯỜNG ĐẠI HỌC ĐÀ LẠT."*
* **Kết quả đạt được:** Khắc phục lỗi im lặng (Silent Return), nạp dữ liệu xem trước phiếu đăng ký mượt mà, chuẩn hóa tiêu đề tài liệu xuất bản.
* **Tệp log:** `.agent/session-log/2026-09-20-1015.md`.

---

### Phiên 11: Chuyển Đổi Sang CSDL SQLite Local (WAL Mode) & Hệ Thống Sao Lưu Git-Style Đa Tầng (F11)
* **Thời gian:** 09/11/2026 09:30
* **Tài liệu tham chiếu:** `docs/01-dac-ta-ung-dung/chuc-nang/F11-local-sqlite-git-backup.md`
* **Prompt chính đã thực hiện:**
  > *"Dự án cần chuyển CSDL chính sang chạy local trên máy cố định tại văn phòng để bảo mật. Hãy đánh giá kỹ thuật và lập kế hoạch chuyển sang SQLite Local (WAL mode). Xây dựng cơ chế sao lưu tự động tối ưu như cách Git quản lý code bằng LibGit2Sharp (xuất dữ liệu dạng Canonical JSONL theo dòng, nén vi sai delta, tạo commit hằng ngày, chụp binary snapshot) kết hợp đồng bộ dự phòng lên DB Host. Viết tài liệu trước rồi mới thực hiện code."*
* **Kết quả đạt được:** 
  * Cấu hình SQLite WAL làm Primary DB cho toàn bộ Repositories.
  * Hợp nhất `AppDbContext` và `LocalAppDbContext`, tự động khởi tạo bảng `NguoiDung` và seed tài khoản admin.
  * Tích hợp `LibGit2Sharp` thực hiện sao lưu vi sai theo dòng 8 bảng và khôi phục du hành thời gian (`Time-Travel Restore` kèm `Pre-Restore Safety Commit`).
  * Xây dựng `RemoteBackupDbContext` đẩy vi sai một chiều lên máy chủ PostgreSQL.
  * Hoàn thành bộ tài liệu `F11-local-sqlite-git-backup.md`, `F11b-co-che-git-backup-chi-tiet.md`, `ADR-12`, `ADR-13` và 47/47 unit tests pass.

---

### Phiên 12: Nâng Cấp UI Master-Detail Lịch Sử Sao Lưu, Export/Import File Zip & Khắc Phục Lỗi Treo Khi Thoát
* **Thời gian:** 09/11/2026 10:25
* **Tài liệu tham chiếu:** `docs/01-dac-ta-ung-dung/chuc-nang/F11b-co-che-git-backup-chi-tiet.md`
* **Prompt chính đã thực hiện:**
  > *"Nâng cấp giao diện sao lưu: cho phép người dùng click vào hàng trong danh sách backup để xem biến động (+/- dòng và chi tiết bản ghi), xuất backup thành file .zip, thêm nút import file .zip để ghi đè CSDL kèm ghi chú yêu cầu định dạng. Tối đa hóa không gian hiển thị (tách tab riêng). Chẩn đoán và sửa lỗi ứng dụng bị treo khi thoát. Đo lường dữ liệu thực tế trên máy và bổ sung vào tài liệu."*
* **Kết quả đạt được:**
  * Tách 2 Tab trong `CauHinhView`, triển khai bố cục Master-Detail trực quan, phân tích diff chi tiết bản ghi bằng Tiếng Việt.
  * Đóng gói và xác thực toàn vẹn file backup `.dkhp-backup.zip` với chữ ký `QuanLyDKHP-Backup-v1` và mã hash SHA-256 từng bảng; tự động tạo điểm lưu `[Pre-Import]` trước khi ghi đè.
  * Chẩn đoán và giải quyết dứt điểm lỗi Async-over-Sync Deadlock tại `desktop.ShutdownRequested` bằng `Task.Run` trên ThreadPool (timeout 2s) và gỡ bỏ `Cache=Shared`.
  * Đo lường thực nghiệm: 6.661 bản ghi, CSDL SQLite 1,85 MB, kho Git 352 KB (nén ~6,5x), tổng thư mục ứng dụng 8,7 MB.
  * Bổ sung tài liệu `ADR-14`, `ADR-15`, cập nhật `ERR-08`, `ERR-09`, bộ kiểm thử đạt 53/53 tests passed (100%).

---

### Phiên 13: Xử Lý Nhập/Xuất Excel 21 Cột & Tự Động Phân Giải Học Kỳ Đích (Nhóm 1)
* **Thời gian:** 11/11/2026 14:15
* **Tài liệu tham chiếu:** `docs/01-dac-ta-ung-dung/chuc-nang/F09-import-excel-du-lieu-lon.md`
* **Prompt chính đã thực hiện:**
  > *"Trước khi bắt đầu nhóm 1, hãy fix lỗi đọc excel và hiển thị cũng như đẩy lên data: bỏ file excel vô, ấn nút đọc thì nó bắt đầu đọc và đưa data vô luôn. Trong cái import excel, khi bỏ file vô để file đọc thì nó vẫn bắt chọn học kỳ đích, tôi không muốn nó là điều bắt buộc khi đọc file. Tôi gửi định dạng mẫu data thực tế 21 cột (hàng triệu dòng). Sau khi nạp thành công phải tự động cập nhật lên database và làm mới giao diện ngay lập tức mà không cần tắt app chạy lại."*
* **Kết quả đạt được:**
  * Bỏ điều kiện bắt buộc chọn học kỳ, tự động phân tích tiền tố Mã LHP (ví dụ `26120CT4201D02` -> `HK261`) và tự khởi tạo học kỳ mới nếu chưa có.
  * Đọc streaming 21 cột của trường ĐH Đà Lạt, tối ưu nạp theo lô kết hợp bộ nhớ đệm tra cứu in-memory.
  * Đồng bộ tức thì lên cả Neon PostgreSQL và SQLite Local; kích hoạt phát tín hiệu cập nhật tự động toàn bộ các tab giao diện với độ trễ 0ms.
  * Bổ sung tài liệu `F09-import-excel-du-lieu-lon.md`, `ADR-16`, cập nhật `ERR-10` và viết bổ sung unit tests.

---

### Phiên 14: Hoàn Thiện Quy Trình ĐKHP, Chống Race Condition & Tính Học Phí (Nhóm 2)
* **Thời gian:** 11/11/2026 18:30
* **Tài liệu tham chiếu:** `docs/01-dac-ta-ung-dung/chuc-nang/F05-dang-ky-hoc-phan.md` & `F06-quan-ly-hoc-phi.md`
* **Prompt chính đã thực hiện:**
  > *"Tiếp tục với nhóm 2: Đảm bảo học kỳ phải đang mở mới được đăng ký/hủy học phần; xử lý đồng thời chống vượt sĩ số khi nhiều người đăng ký cùng lúc; thống nhất công thức tính học phí danh sách và chi tiết, fix lỗi học kỳ đóng bị hiển thị 0đ; thêm tính năng nộp tiền học phí. Đồng thời trau chuốt lại UI theo ảnh đính kèm: họ tên sinh viên bị cắt ngắn, header chi tiết SV bị đè chữ lên thanh tiến độ tín chỉ và mất cột tên môn học."*
* **Kết quả đạt được:**
  * Thêm kiểm tra Step 0 (`HocKy.DangMo == true`) chặn cứng thao tác trên kỳ đã đóng.
  * Hiện thực hóa `LuuDangKyAtomicAsync` với `SemaphoreSlim` và `BeginTransactionAsync`, kiểm tra sĩ số thời gian thực và rollback nguyên tử nếu lớp đã đầy.
  * Thống nhất 1 công thức học phí duy nhất, bổ sung fallback tính từ lịch sử ĐKHP khi snapshot chưa có trên kỳ đóng, tích hợp phương thức `GhiNhanNopTienAsync` và hộp thoại nộp tiền.
  * Trau chuốt UI: Tách header State 2 thành 2 tầng riêng biệt, mở rộng cột Họ tên với ToolTip, khôi phục cột Tên môn học với cuộn ngang tự động.
  * Bổ sung tài liệu `F05`, `F06`, `ADR-17`, `ADR-18`, ghi nhận `ERR-11` và hoàn thành unit tests.

---

### Phiên 15: Xử Lý Tái Tạo Mã Sau Soft-Delete, Thống Nhất Giảng Viên & Bảo Toàn Tài Khoản (Nhóm 3)
* **Thời gian:** 11/11/2026 22:15
* **Tài liệu tham chiếu:** `docs/01-dac-ta-ung-dung/chuc-nang/F02-quan-ly-sinh-vien.md`, `F03`, `F04`
* **Prompt chính đã thực hiện:**
  > *"Tiếp tục với nhóm 3: Sửa lỗi crash duplicate key khi tạo lại mã đã từng bị xóa mềm; thống nhất mã giảng viên MaGV và tên đăng nhập TenDangNhap trên toàn bộ ứng dụng; chặn mở khóa tài khoản tự ý khi sửa thông tin người dùng; chặn đổi mã môn học khi Lớp học phần đã có sinh viên đăng ký."*
* **Kết quả đạt được:**
  * Hiện thực hóa cơ chế Soft-Delete Reactivation trên 4 bảng (`SinhVien`, `MonHoc`, `LopHocPhan`, `NguoiDung`), tự động phục hồi bản ghi thay vì insert mới, triệt tiêu lỗi UNIQUE constraint crash.
  * Thống nhất nhận diện giảng viên: gán `MaGV = TenDangNhap` nếu để trống, kiểm tra duy nhất qua `GetByMaGVAsync`, tra cứu song song 2 khóa trong `gvDict` và `DashboardRepository`.
  * Bảo toàn trạng thái tài khoản `EditTrangThai` trong `NguoiDungViewModel`, ngăn chặn vô tình mở khóa tài khoản.
  * Chặn đổi `MaMon` và chặn hạ `SiSoToiDa` dưới số lượng sinh viên đang học trong `LopHocPhanService.CapNhatAsync`; vô hiệu hóa dropdown Môn học trên dialog kèm ToolTip cảnh báo.
  * Cập nhật tài liệu `F02`, `F03`, `F04`, `ADR-19`, `ADR-20`, ghi nhận `ERR-12`, bổ sung unit tests đầy đủ.

---

### Phiên 16: Tối Ưu Báo Cáo Thống Kê, Đồng Bộ Soft-Delete & Xuất Ấn Phẩm Chuẩn In (Nhóm 4)
* **Thời gian:** 11/11/2026 23:45
* **Tài liệu tham chiếu:** `docs/01-dac-ta-ung-dung/chuc-nang/F08-bao-cao-thong-ke.md`, `ADR-21`, `ERR-13`
* **Prompt chính đã thực hiện:**
  > *"Thực hiện Nhóm 4: Khắc phục lỗi GroupBy(l => l.MonHoc) trên AsNoTracking trong ThongKeSoSvTheoMonAsync, đồng bộ lọc DangHoc & soft-delete trên toàn bộ 4 tabs báo cáo, nạp họ tên giảng viên lên phiếu và danh sách thi, cập nhật tài liệu Docusaurus Phần III."*
* **Kết quả đạt được:**
  * Sửa lỗi `GroupBy(l => l.MonHoc)` sang `GroupBy(l => l.MaMon)`, loại bỏ triệt để nguy cơ phân mảnh nhóm trùng môn hoặc lỗi NRE do thiếu Identity Map trên `AsNoTracking()`.
  * Đồng bộ hóa điều kiện `!lhp.IsDeleted && !sv.IsDeleted && dk.TrangThai == "DangHoc"` trên toàn bộ các phương thức truy vấn báo cáo (`BaoCaoRepository` và `LocalReadService`), thống nhất số lượng sinh viên giữa màn hình danh sách và thống kê.
  * Nạp họ tên giảng viên (`NguoiDung.HoTen`) từ từ điển 2 khóa (`MaGV` & `TenDangNhap`) vào các DTO; hiển thị cột *Giảng viên* trên DataGrid Tab 1 & Tab 2, xuất ra tệp Excel và in lên phần tiêu đề/cột thông tin của phiếu thi PDF và phiếu ĐKHP PDF khổ A4.
  * Bổ sung bộ kiểm thử `tests/QuanLyDKHP.Tests/BaoCaoRepositoryTests.cs` (4/4 passed), nâng tổng số bài kiểm thử tự động của dự án lên 73/73 passed (100%).
  * Cập nhật tài liệu Docusaurus: `F08`, `ADR-21`, `ERR-13`, Daily Log và Commit History.

---

### Phiên 17: Hạ Tầng Dữ Liệu, Cache & Đồng Bộ Cục Bộ An Toàn (Nhóm 5)
* **Thời gian:** 11/11/2026 23:55
* **Tài liệu tham chiếu:** `docs/01-dac-ta-ung-dung/chuc-nang/F11-local-sqlite-git-backup.md`, `ADR-22`, `ERR-14`
* **Prompt chính đã thực hiện:**
  > *"Thực hiện Nhóm 5: Loại bỏ EnsureDeletedAsync() khỏi chu trình đồng bộ SyncService để bảo toàn file CSDL quanlydkhp.db; bổ sung NguoiDung vào MasterTables để bảo vệ tài khoản khi làm mới offline; sửa mốc timestamp Delta Sync tương thích Npgsql PostgreSQL."*
* **Kết quả đạt được:**
  * Triệt tiêu hoàn toàn `EnsureDeletedAsync()` khỏi `SyncService.RefreshLocalStoreAsync`. Thay vì xóa vật lý file CSDL gây khóa file SQLite WAL và mất dữ liệu, hệ thống dọn dẹp các bảng nghiệp vụ cũ bên trong transaction an toàn kèm cơ chế rollback.
  * Bổ sung `NguoiDung` vào `MasterTables` và quy trình đồng bộ vi sai `SyncDeltaAsync`, bảo toàn 100% tài khoản người dùng khi chuyển đổi giữa chế độ trực tuyến và ngoại tuyến.
  * Chuẩn hóa `GetLastSyncUtcAsync` trả về `new DateTime(2000, 1, 1, DateTimeKind.Utc)`, khắc phục triệt để lỗi định dạng thời gian và tràn số năm trên PostgreSQL Npgsql.
  * Cập nhật tài liệu Docusaurus: `F11`, `ADR-22`, `ERR-14`, Daily Log và Commit History.

---

## 3. Tổng Kết Giá Trị Ứng Dụng AI Vào Đồ Án

Việc ứng dụng AI Agent vào đồ án môn học mang lại hiệu quả vượt trội:
* **Tăng tốc độ phát triển:** Rút ngắn thời gian xây dựng khung mã nguồn từ hàng tuần xuống còn vài ngày.
* **Nâng cao chất lượng phần mềm:** 100% các dịch vụ nghiệp vụ đều có Unit Test tự động đi kèm (Test-Driven Development).
* **Tuân thủ quy chuẩn kỹ thuật:** Mã nguồn luôn giữ được tính nhất quán về kiến trúc Clean Architecture, quy chuẩn đặt tên và các mẫu thiết kế hướng đối tượng (GoF Design Patterns).

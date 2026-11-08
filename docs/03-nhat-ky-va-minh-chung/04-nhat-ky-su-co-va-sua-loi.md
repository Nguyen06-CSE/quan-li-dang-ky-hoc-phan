---
id: nhat-ky-su-co-va-sua-loi
title: Nhật Ký Xử Lý Sự Cố & Sửa Lỗi (Troubleshooting)
sidebar_position: 4
---

# Nhật Ký Xử Lý Sự Cố & Sửa Lỗi (Troubleshooting)

Trong quá trình phát triển ứng dụng Desktop phức tạp với cơ sở dữ liệu phân tán và giao diện đồ họa hiện đại, nhóm đã gặp và giải quyết dứt điểm nhiều sự cố kỹ thuật chuyên sâu. Tài liệu này ghi nhận chi tiết nguyên nhân gốc rễ và giải pháp khắc phục.

---

## 1. Bảng Tổng Hợp Trạng Thái Các Sự Cố Kỹ Thuật

| Mã Lỗi | Phân Hệ Phát Sinh | Hiện Tượng / Triệu Chứng | Mức Độ | Trạng Thái |
|:---:|---|---|:---:|:---:|
| **ERR-01** | Môi trường macOS | Thông báo ứng dụng bị hỏng (Damaged and can't be opened) | Nghiêm trọng | [Đã xử lý] |
| **ERR-02** | Khởi chạy CLI | `Couldn't find a project to run` khi gõ `dotnet run` | Trung bình | [Đã xử lý] |
| **ERR-03** | EF Core / Data | `A second operation was started on this context instance...` | Rất nghiêm trọng | [Đã xử lý] |
| **ERR-04** | Import Excel | Chậm với tập tin lớn, nguy cơ mất dữ liệu khi lỗi 1 dòng | Nghiêm trọng | [Đã xử lý] |
| **ERR-05** | Menu Navigation | `NullReferenceException` khi bấm menu Import | Trung bình | [Đã xử lý] |
| **ERR-06** | UI Data Binding | `StackOverflowException` do vòng lặp Binding Loop tại ĐKHP | Rất nghiêm trọng | [Đã xử lý] |
| **ERR-07** | Điều hướng nhanh | Double-click sinh viên khi danh sách chưa tải xong gây lỗi | Trung bình | [Đã xử lý] |

---

## 2. Chi Tiết Các Sự Cố Trọng Yếu & Phương Án Khắc Phục

### Sự Cố 1: macOS Báo "QuanLyDKHP.App.app Bị Hỏng Và Không Thể Mở Được"
* **Triệu chứng:** Hộp thoại cảnh báo hệ thống của macOS hiển thị yêu cầu di chuyển ứng dụng vào Thùng rác (Move to Trash) khi chạy lệnh `dotnet run`, dù quá trình `dotnet build` hoàn toàn thành công.
* **Nguyên nhân gốc rễ:** Không phải do lỗi mã nguồn C#. Tệp tải về dưới dạng tệp nén `.zip` bị cơ chế bảo mật Gatekeeper của macOS gắn thuộc tính mở rộng cách ly (`com.apple.quarantine`), chặn thực thi các thư viện động `.dylib` chưa được ký số thương mại (Notarization).
* **Phương án khắc phục:**
  1. Gỡ bỏ thuộc tính cách ly toàn bộ thư mục:
     ```bash
     xattr -cr .
     ```
  2. Dọn dẹp các tệp nhị phân cũ:
     ```bash
     find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
     ```
  3. Ký chữ ký ad-hoc cục bộ cho các tệp nhị phân trên máy Mac:
     ```bash
     find bin -name "*.dylib" -exec codesign --force --sign - {} \;
     codesign --force --sign - bin/Debug/net8.0/QuanLyDKHP.App
     dotnet run --no-build --project src/QuanLyDKHP.App
     ```

---

### Sự Cố 2: EF Core "A Second Operation Was Started On This Context Instance..."
* **Triệu chứng:** Khi mở ứng dụng hoặc chuyển đổi nhanh giữa các màn hình Sinh viên, Môn học, thanh trạng thái báo lỗi: *"A second operation was started on this context instance before a previous operation completed"*, bảng dữ liệu bị trắng hoàn toàn.
* **Nguyên nhân gốc rễ:** Trong `App.axaml.cs`, hệ thống ban đầu đăng ký `AddDbContext` (mô hình Scoped). Tuy nhiên, ứng dụng Desktop không có cơ chế request-scope như Web ASP.NET mà dùng chung một DbContext Singleton trong suốt phiên. Khi 2 tác vụ bất đồng bộ (`LoadDataAsync` và `InitFiltersAsync`) chạy song song, chúng cùng truy cập vào một DbContext duy nhất. Entity Framework Core nghiêm cấm chạy đồng thời 2 truy vấn trên cùng 1 context.
* **Phương án khắc phục:**
  1. Chuyển đổi đăng ký sang Factory Pattern: `services.AddDbContextFactory<AppDbContext>(...)`.
  2. Tái cấu trúc toàn bộ 10 Repositories: Tiếp nhận `IDbContextFactory<AppDbContext>`, mỗi hàm tự tạo và giải phóng context cục bộ:
     ```csharp
     await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
     return await context.SinhViens.AsNoTracking().ToListAsync(cancellationToken);
     ```
  3. Thêm cờ `_dangKhoiTao` trong ViewModel để chặn các sự kiện thay đổi bộ lọc kích hoạt tải dữ liệu khi giao diện đang trong quá trình nạp khởi đầu.

---

### Sự Cố 3: Import Excel Bị Nghẽn & Dễ Hỏng Toàn Bộ Dữ Liệu
* **Triệu chứng:** Quá trình nạp tệp Excel 1.000 dòng mất hơn 1 phút; nếu chỉ có 1 dòng dữ liệu bị lỗi (ví dụ: ngày sinh sai định dạng), toàn bộ 999 dòng còn lại đều bị hủy bỏ và không lưu vào CSDL.
* **Nguyên nhân gốc rễ:**
  * Thuật toán cũ gọi `SaveChanges` cho từng dòng sinh viên và tính lại học phí ngay lập tức (N+1 Query).
  * Bộ theo dõi thay đổi (`Change Tracker`) của EF Core bị quá tải khi tích lũy hàng nghìn thực thể với cờ `AutoDetectChanges = true`.
  * Thiếu khối bao bọc xử lý ngoại lệ theo từng bản ghi.
* **Phương án khắc phục:**
  1. Triển khai xử lý theo lô (Chunk size = 500 dòng). Mỗi lô chạy trong 1 Transaction riêng biệt.
  2. Cơ chế cách ly lỗi (Fault Isolation): Bao bọc kiểm tra dữ liệu từng dòng; dòng lỗi được ghi vào danh sách thông báo riêng, các dòng hợp lệ vẫn được commit vào CSDL bình thường.
  3. Tắt `AutoDetectChanges` trong quá trình lặp và chỉ bật lại khi commit lô, đồng thời thực hiện tính học phí theo lô một lần duy nhất ở bước kết thúc.

---

### Sự Cố 4: Vòng Lặp Liên Kết Giao Diện (Binding Loop `StackOverflowException`)
* **Triệu chứng:** Ứng dụng Desktop bị crash đột ngột với mã lỗi `StackOverflowException` khi người dùng chuyển từ tab Sinh viên sang tab Đăng ký học phần.
* **Nguyên nhân gốc rễ:** Trong `DangKyHocPhanView.axaml`, việc liên kết hai chiều (`Mode=TwoWay`) giữa thuộc tính `SelectedItem` và `SelectedIndex` trên `ItemsControl` kết hợp với sự kiện nạp lại danh sách môn học tạo thành một vòng lặp sự kiện vô tận: Thuộc tính này thay đổi kích hoạt thuộc tính kia cập nhật và ngược lại, làm tràn ngăn xếp bộ nhớ (Call Stack).
* **Phương án khắc phục:**
  * Ngắt liên kết hai chiều dư thừa, chỉ dùng `OneWayToSource` cho sự kiện chọn dòng.
  * Tối ưu hóa việc cập nhật dữ liệu thông qua `ObservableCollection.Reset()` hoặc tạo mới danh sách thay vì thay đổi từng phần tử đơn lẻ.

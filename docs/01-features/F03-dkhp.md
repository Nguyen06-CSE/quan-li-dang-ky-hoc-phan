# TÀI LIỆU KỸ THUẬT: MODULE ĐĂNG KÝ HỌC PHẦN (ADVANCED MASTER-DETAIL)

**Vị trí:** `docs/01-features/F03-dkhp.md`  
**Dự án:** Quản lý Đăng ký Học phần (QuanLyDKHP)  
**Công nghệ:** C# 12, .NET 8.0, Avalonia UI 11.x, CommunityToolkit.Mvvm

---

## 1. Tổng quan Nghiệp vụ

Module **Đăng ký học phần** áp dụng kiến trúc **Advanced Master-Detail** kết hợp với **Multi-State Pane** (Khung đa trạng thái). Thiết kế này chuyển đổi trải nghiệm người dùng từ thế "thụ động" (phải nhớ chính xác mã sinh viên) sang thế "chủ động khám phá" (lọc theo Khóa, Lớp, Môn học và so sánh chéo nhiều sinh viên).

## 2. Giao tiếp liên Module (Cross-Module Navigation)

Hệ thống cho phép người dùng nhảy trực tiếp từ Tab **Sinh viên** sang Tab **Đăng ký học phần** thông qua cơ chế nhắn tin lỏng lẻo (Loosely Coupled Messaging) của `IMessenger`.

* **Message Class:** `NavigateToRegistrationMessage(string MaSV)`
* **Người gửi (Publisher):** `SinhVienView` (Khi Double-click hoặc chọn ContextMenu "Đăng ký học phần" trên một sinh viên).
* **Người nhận 1 (Tab Switcher):** `MainWindowViewModel` nhận thông điệp, đổi `CurrentViewModel` sang `DangKyHocPhanViewModel`.
* **Người nhận 2 (Data Loader):** `DangKyHocPhanViewModel` bắt mã sinh viên, tự động tìm trong bộ nhớ đệm `_masterList`, chọn sinh viên đó ở cột trái và kích hoạt State 2 (Chế độ xem) ở cột phải.

---

## 3. Kiến trúc Giao diện (UI Architecture)

Màn hình được chia làm 2 cột bằng lưới Grid (`ColumnDefinitions="350,*"`), ngăn cách bởi `GridSplitter`.

### 3.1. Cột Trái (Master - Bộ lọc & Danh sách)

Đảm nhiệm vai trò tìm kiếm và điều hướng sinh viên, sử dụng kỹ thuật **In-Memory Caching** để có tốc độ phản hồi 0ms.

* **Bộ lọc xếp tầng (Cascading Filters):** ComboBox `Khóa học` ➔ `Lớp sinh hoạt` ➔ `Môn học`.
* **Lọc theo Trạng thái Môn:** RadioButton `Tất cả` / `Đã đăng ký` / `Chưa đăng ký` (Kích hoạt khi chọn cụ thể 1 môn học).
* **Thao tác chọn (Multi-select):** Thẻ `<ListBox>` thiết lập `SelectionMode="Multiple, Toggle"`, cho phép giáo vụ chọn nhiều sinh viên cùng lúc bằng cách giữ `Ctrl/Shift` (hoặc Click chéo).
* **Sắp xếp (Sorting):** Tích hợp `VietnameseNameComparer` để sắp xếp tên Tiếng Việt chuẩn xác (Tên ➔ Lót ➔ Họ), hoặc sắp xếp theo số tín chỉ để rà soát sinh viên đăng ký thiếu.

### 3.2. Cột Phải (Detail - Đa Trạng Thái / Multi-State)

Khu vực này thay đổi giao diện linh hoạt dựa trên số lượng sinh viên được chọn ở cột trái. Được điều hướng bằng biến `CurrentState` (Enum `DetailState`).

* **State 0 - Empty (Không chọn ai):** Hiển thị màn hình chờ hướng dẫn người dùng chọn sinh viên ở cột trái.
* **State 1 - Compare (Chọn >= 2 SV):**
  * Hiển thị danh sách các "Student Card" dọc từ trên xuống bằng `ItemsControl`.
  * Hiển thị cảnh báo màu sắc Tín chỉ (Xanh: Đạt, Đỏ: Thiếu).
  * Tích hợp tính năng "Chỉ hiển thị các môn học chung" (Sử dụng LINQ `Intersect` để lấy giao của các danh sách).
* **State 2 - Preview (Chọn duy nhất 1 SV):** Hiển thị thẻ thông tin cá nhân và `<DataGrid>` (chỉ đọc) chứa các môn đã đăng ký. Góc phải có nút "Chỉnh sửa đăng ký".
* **State 3 - Edit (Chế độ chỉnh sửa):** Khóa danh sách bên trái. Hiển thị form cho phép Đăng ký lớp học phần mới hoặc Hủy môn hiện tại. Có nút "Hoàn tất" để quay về State 2.

---

## 4. Troubleshooting & Tiêu chuẩn Kỹ thuật (RẤT QUAN TRỌNG)

Quá trình phát triển Module này đã ghi nhận và xử lý triệt để lỗi **StackOverflowException** do vòng lặp **TwoWay Binding Loop**. Khi bảo trì hoặc phát triển module mới, Developer bắt buộc tuân thủ 3 nguyên tắc sau:

### ⚠️ Quy tắc 1: Cập nhật Collection Nguyên tử (Atomic Collection Update)

**Tuyệt đối không** dùng vòng lặp gọi `.Add()`, `.Remove()` hoặc `.Clear()` liên tiếp trên một `ObservableCollection` đang được Bind lên DataGrid (nhất là trong State thay đổi liên tục).

* **Sai:** `foreach(var item in list) { UiCollection.Add(item); }` (Làm DataGrid liên tục đo đạc và chèn dòng mới, gây crash UI Thread).
* **Đúng:**
    ```csharp
    var tempList = new List<Model>();
    foreach(var item in list) { tempList.Add(item); }
    UiCollection = new ObservableCollection<Model>(tempList); // Gán 1 lần duy nhất
    ```

### ⚠️ Quy tắc 2: Tránh TwoWay Binding trên ListBox.SelectedItems

Avalonia không hỗ trợ mượt mà việc gán `SelectedItems="{Binding... Mode=TwoWay}"` cho thao tác Multi-select. Nó dễ kích hoạt vòng lặp `OnTargetPropertyChanged`.

* **Giải pháp:** Xử lý bằng Event ở Code-behind.
    ```csharp
    // Trong View.axaml.cs
    private void ListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selected = ((ListBox)sender).SelectedItems.Cast<SinhVienDto>().ToList();
        ((MyViewModel)DataContext).HandleSelectionChanged(selected); // Đẩy xuống ViewModel bằng method
    }
    ```

### ⚠️ Quy tắc 3: Cấm lồng DataGrid có chiều cao tự động vào ScrollViewer

Nếu bọc `<DataGrid Height="NaN">` bên trong một `<ItemsControl>` hoặc `<ScrollViewer>`, Avalonia sẽ mất khả năng tính toán Virtualization, dẫn đến lỗi Nested Scrolling hoặc đo đạc vô hạn (Measure Loop).

* **Giải pháp:** Với các danh sách lồng nhau (Như danh sách sinh viên -> mỗi sinh viên có danh sách môn học ở State 1), **KHÔNG** dùng `<DataGrid>`. Hãy tự dựng bảng bằng `<ItemsControl>` kết hợp `<Grid>` cho ItemTemplate. Trọng lượng nhẹ hơn và an toàn tuyệt đối.
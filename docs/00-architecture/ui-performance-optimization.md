# TÀI LIỆU KỸ THUẬT: TỐI ƯU HIỆU SUẤT TÌM KIẾM & HIỂN THỊ DỮ LIỆU (UI PERFORMANCE OPTIMIZATION)

**Vị trí:** `docs/00-architecture/ui-performance-optimization.md`  
**Dự án:** Quản lý Đăng ký Học phần (QuanLyDKHP)  
**Công nghệ:** C# 12, .NET 8.0, Avalonia UI 11.x, CommunityToolkit.Mvvm

---

## 1. Tổng quan kiến trúc

Nhằm giải quyết vấn đề độ trễ (latency) khi gọi Database từ xa (Neon DB) và hiện tượng giật lag giao diện khi xử lý dữ liệu lớn (hàng chục ngàn dòng), dự án áp dụng chuẩn kiến trúc: **In-Memory Caching kết hợp Client-Side Filtering & UI Virtualization**.

**Lợi ích đạt được:**

*   **Zero-latency:** Thời gian tìm kiếm và lọc dữ liệu diễn ra gần như tức thì (0ms) do thực hiện hoàn toàn trên RAM (Client-side).
*   **Mượt mà (60 FPS):** Áp dụng kỹ thuật Debounce và Background Task giúp UI không bị đóng băng khi người dùng gõ phím liên tục.
*   **Tiết kiệm băng thông:** Giảm thiểu tối đa số lượng request (Read) gửi lên Server/Database.
*   **Infinite Scrolling:** Loại bỏ phân trang rườm rà, mang lại trải nghiệm cuộn vô tận mượt mà nhờ cơ chế UI Virtualization mặc định của Avalonia.

---

## 2. Các nguyên tắc cốt lõi (Core Principles)

Kiến trúc UI cho các màn hình dạng danh sách dựa trên 4 trụ cột chính:

1.  **Single-fetch Master Data:** Tải toàn bộ dữ liệu 1 lần duy nhất từ DB khi khởi tạo View (Initial Load) và lưu vào một biến `List<T>` (Master List) trên bộ nhớ RAM.
2.  **Bulk UI Update:** Tuyệt đối không dùng hàm `.Add()` hoặc `.Remove()` từng phần tử trong vòng lặp trực tiếp lên danh sách đang Binding (`ObservableCollection`). Thay vào đó, gán mới toàn bộ collection 1 lần duy nhất để View chỉ phải tính toán Render 1 lần.
3.  **Debounce Timer:** Sử dụng Timer (300ms) để trì hoãn việc tính toán. Chỉ bắt đầu lọc dữ liệu khi người dùng đã ngừng thao tác gõ phím.
4.  **UI Virtualization:** Đặt `DataGrid`/`ListBox` trong không gian có thể mở rộng (`Grid.Row="*"`) để Avalonia tự động tái sử dụng các Control trên màn hình (chỉ render những dòng đang lọt vào tầm nhìn của màn hình), giúp cuộn 100.000 dòng vẫn không tốn thêm RAM.

---

## 3. Tiêu chuẩn triển khai Code (Implementation Guide)

Khi tạo mới một Module (ví dụ: `GiangVien`, `PhongHoc`), Developer bắt buộc phải tuân thủ cấu trúc ViewModel và View sau:

### 3.1. Tiêu chuẩn viết ViewModel (`.cs`)

**Khai báo các biến cần thiết:**

```csharp
// 1. Bộ nhớ đệm chứa toàn bộ dữ liệu gốc (Không bind trực tiếp lên UI)
private List<ModelDto> _masterList = new();

// 2. Cờ trạng thái hiển thị loading xoay vòng
[ObservableProperty]
private bool _isLoading;

// 3. Danh sách bind lên UI (Bắt buộc dùng cú pháp ObservableProperty)
[ObservableProperty]
private ObservableCollection<ModelDto> _danhSach = new();

// 4. Timer xử lý delay gõ phím để chống spam lọc dữ liệu
private readonly Timer _debounceTimer = new Timer(300) { AutoReset = false };
```

**Khởi tạo sự kiện (Trong Constructor):**

```csharp
_debounceTimer.Elapsed += (s, e) => ApplyFiltersInMemory();
```

**Kích hoạt Debounce khi Filter thay đổi:**

```csharp
// Trigger khi người dùng nhập Textbox hoặc chọn ComboBox
partial void OnTuKhoaChanged(string? value) => TriggerDebounce();
partial void OnTrangThaiFilterChanged(string? value) => TriggerDebounce();

private void TriggerDebounce()
{
    if (_dangKhoiTao) return; // Bỏ qua nếu đang ở luồng Init
    _debounceTimer.Stop();
    _debounceTimer.Start();   // Reset lại bộ đếm 300ms
}
```

**Hàm lọc In-Memory (Bắt buộc chạy trên Background Thread):**

```csharp
private void ApplyFiltersInMemory()
{
    // 1. Đưa logic tính toán xuống luồng nền (Task.Run)
    Task.Run(() =>
    {
        Dispatcher.UIThread.Invoke(() => IsLoading = true);

        // 2. Query trực tiếp trên MasterList
        var query = _masterList.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(TuKhoa))
        {
            var key = TuKhoa.Trim().ToLower();
            query = query.Where(x => x.Ten.ToLower().Contains(key) || x.Ma.ToLower().Contains(key));
        }
        
        var resultList = query.ToList();

        // 3. Cập nhật UI trên Main Thread
        Dispatcher.UIThread.Invoke(() =>
        {
            // Bulk update: Gán mới hoàn toàn để UI render 1 lần
            DanhSach = new ObservableCollection<ModelDto>(resultList);
            TotalItems = resultList.Count;
            IsLoading = false;
        });
    });
}
```

### 3.2. Tiêu chuẩn viết View (`.axaml`)

**Cấu trúc Layout chính:**

```xml
<!-- Đảm bảo dòng chứa DataGrid (dòng cuối cùng) là "*" để kích hoạt UI Virtualization -->
<Grid RowDefinitions="Auto,Auto,Auto,*">
    
    <!-- Row 0: Header & Message -->
    <!-- Row 1: Toolbar & Bộ lọc -->

    <!-- Row 2: Thanh Loading báo hiệu tiến trình (Chiều cao nhỏ, tinh tế) -->
    <ProgressBar Grid.Row="2" Height="2" HorizontalAlignment="Stretch" IsIndeterminate="True" IsVisible="{Binding IsLoading}" Margin="0,0,0,8"/>

    <!-- Row 3: Lưới dữ liệu -->
    <DataGrid ... Grid.Row="3" ItemsSource="{Binding DanhSach}">
        <!-- KHÔNG set cứng DataGrid.Height -->
    </DataGrid>
</Grid>
```

> **⚠️ LƯU Ý QUAN TRỌNG VỀ VIEW:**
> Tuyệt đối không đặt `DataGrid` bên trong một thẻ `<ScrollViewer>` hoặc `<StackPanel>`. Việc này sẽ cung cấp cho `DataGrid` không gian vô hạn, phá vỡ cơ chế Virtualization, khiến ứng dụng phải render toàn bộ hàng ngàn dòng cùng lúc và dẫn đến treo máy (crash/freeze).

---

## 4. Xử lý Đồng bộ dữ liệu sau CRUD (Data Syncing)

Vì dữ liệu hiển thị được lấy từ bộ đệm `_masterList` trên RAM, khi có các thao tác **Thêm (Create), Sửa (Update), Xóa (Delete)**, Developer phải đảm bảo tính toàn vẹn dữ liệu:

1. **Thực thi dưới DB:** Các hàm Thêm/Sửa/Xóa phải gọi API/Service lưu thẳng xuống Database gốc (Neon DB).
2. **Đồng bộ UI:** Sau khi Server trả về thành công, có 2 cách để update giao diện:
*   *Cách 1 (Dễ nhất):* Gọi lại hàm `LoadMasterDataAsync()` để kéo lại bộ dữ liệu mới nhất, sau đó gọi `ApplyFiltersInMemory()`.
*   *Cách 2 (Hiệu năng cao nhất):* Tìm và thay đổi (Thêm/Sửa/Xóa) đúng phần tử đó trực tiếp bên trong biến `_masterList`, sau đó gọi lại `ApplyFiltersInMemory()`. Không cần gọi lại DB.

---

*Tài liệu này được áp dụng làm quy chuẩn (convention) cho toàn bộ các màn hình hiển thị danh sách trong dự án.*
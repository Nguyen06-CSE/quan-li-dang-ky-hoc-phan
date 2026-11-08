---
id: design-system-ui-ux
title: Hệ Thống Thiết Kế UI/UX & Giao Diện Desktop
sidebar_position: 5
---

# Hệ Thống Thiết Kế UI/UX & Giao Diện Desktop

## 1. Triết Lý Thiết Kế: "Liquid Glass" & Hiện Đại

Khác với các ứng dụng Desktop truyền thống thường mang giao diện xám thô hoặc mô phỏng web trong cửa sổ Electron nặng nề, dự án được xây dựng trên nền tảng **Avalonia UI 11.x** thuần C#/.NET với ngôn ngữ thiết kế **Liquid Glass**:

* **Kính mờ (Frosted Acrylic / Mica Effect):** Sử dụng các lớp nền trong mờ, góc bo tròn mềm mại (CornerRadius 12-16px) và bóng đổ nhẹ tạo chiều sâu không gian làm việc.
* **Đồng bộ Chủ đề Sáng/Tối (Light/Dark Mode):** Cho phép người dùng chuyển đổi tức thì chỉ với một thao tác bấm nút tại thanh công cụ trên cùng. Toàn bộ bảng màu chuyển trạng thái mượt mà thông qua hiệu ứng Fade.
* **Điều hướng tập trung (App Shell):** Sidebar thông minh có thể thu gọn/mở rộng với dải màu trượt (Sliding Indicator) bám theo mục đang chọn, kết hợp Top Bar chứa điều hướng ngữ cảnh (Breadcrumb).

---

## 2. Chuẩn Hóa Biểu Tượng: Loại Bỏ Emoji Bộ Gõ

Trong giai đoạn đầu phát triển, ứng dụng thử nghiệm các biểu tượng emoji từ bàn phím ký tự. Tuy nhiên, emoji văn bản bộc lộ nhiều khuyết điểm nghiêm trọng trên ứng dụng Desktop:
1. Độ sắc nét không đồng nhất giữa các hệ điều hành (Windows, macOS, Linux).
2. Xảy ra hiện tượng đè chữ lên nhãn tiêu đề khi Avalonia đo đạc kích thước font chữ (Font Metrics).
3. Thiếu tính trang trọng, không đạt chuẩn thẩm mỹ học thuật và phần mềm quản trị chuyên nghiệp.

:::info[Giải pháp chuẩn hóa]
Dự án đã tích hợp thư viện vector icon tối giản **`Material.Icons.Avalonia`**:
* Toàn bộ mã nguồn sử dụng enum định danh chuẩn: `MaterialIconKind.HomeOutline`, `SchoolOutline`, `BookOpenPageVariantOutline`, `AccountGroupOutline`...
* Hiển thị qua thẻ vector chuẩn: `<materialIcons:MaterialIcon Kind="SchoolOutline" Width="20" Height="20" />`.
* Đảm bảo mọi biểu tượng đều tối giản, đồng bộ độ dày nét vẽ (stroke width), có khả năng đổi màu tự động theo theme và sắc nét ở mọi độ phân giải màn hình (Retina / 4K).
:::

---

## 3. Hệ Thống Token Màu Sắc (Color Tokens)

Toàn bộ màu sắc và cọ vẽ (Brushes) được tập trung tại `src/QuanLyDKHP.App/Styles/Colors.axaml` thông qua `ThemeDictionaries`:

| Tên Token Brush | Màu Theme Sáng | Màu Theme Tối | Ý Nghĩa Sử Dụng |
|---|---|---|---|
| `BrushPrimary` | `#86B049` | `#ACD26B` | Màu chủ đạo thương hiệu hệ thống (Xanh lá tri thức) |
| `BrushPrimaryHover` | `#6F9538` | `#BDDD82` | Hiệu ứng khi rê chuột vào nút hoặc liên kết chính |
| `BrushBackground` | `#F4F6F8` | `#14181B` | Màu nền bao quát toàn bộ cửa sổ ứng dụng |
| `BrushSurface` | `#FFFFFF` | `#1E2328` | Nền thẻ (Card), panel nội dung chính |
| `BrushSurfaceAlt` | `#EAEFF4` | `#282F36` | Nền thanh tiêu đề bảng, ô nhập liệu phụ |
| `BrushBorder` | `#D2D9E0` | `#333C45` | Đường viền ngăn cách các khối và DataGrid |
| `BrushTextPrimary` | `#1F2937` | `#F3F4F6` | Chữ văn bản chính, tiêu đề cấp 1 và cấp 2 |
| `BrushTextSecondary` | `#6B7280` | `#9CA3AF` | Chữ phụ, nhãn mô tả, thông tin ghi chú |
| `BrushDanger` | `#DC2626` | `#EF4444` | Trạng thái lỗi, nút xóa dữ liệu, cảnh báo nợ |
| `BrushWarning` | `#D97706` | `#F59E0B` | Cảnh báo mềm (học lại, sắp hết hạn, đầy chỗ) |
| `BrushSuccess` | `#16A34A` | `#22C55E` | Trạng thái hoàn thành, đã đóng học phí, lưu thành công |

---

## 4. Các Lớp Định Kiểu Giao Diện Dùng Chung (Style Classes)

Tại `src/QuanLyDKHP.App/Styles/Controls.axaml`, các lớp CSS-like trong Avalonia được thiết lập chuẩn hóa:

* `.glass-panel`: Thẻ nền kính bán trong suốt với viền sáng mờ, dùng bao bọc các phân vùng làm việc chính.
* `.btn-primary`: Nút thao tác chính yếu (Lưu, Đăng ký, Cập nhật), nền `BrushPrimary`, chữ trắng, bo góc 8px, hiệu ứng nhấc nổi khi hover.
* `.btn-secondary`: Nút phụ (Hủy, Làm mới, Quay lại), nền trong suốt có viền mỏng.
* `.btn-danger`: Nút thao tác rủi ro cao (Xóa sinh viên, Xóa môn học, Hủy đăng ký), màu cảnh báo đỏ.
* `.badge-success` / `.badge-danger`: Huy hiệu trạng thái nhỏ gọn hiển thị tình trạng học phí (Đã đóng / Còn nợ) hoặc trạng thái đăng ký.

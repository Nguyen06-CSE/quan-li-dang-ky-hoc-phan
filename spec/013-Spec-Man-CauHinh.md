# 013 — SPEC MÀN HÌNH CẤU HÌNH HỆ THỐNG (chỉ Admin)

## Giao diện (`CauHinhView.axaml`)
Form đơn giản dạng Card, các field:
- "Số tín chỉ tối thiểu / học kỳ" — NumericUpDown, min 1.
- "Số tín chỉ tối đa / học kỳ" — NumericUpDown, validate phải > tối thiểu.
- "Đơn giá 1 tín chỉ Lý thuyết (VNĐ)" — NumericUpDown, min 0, format hiển thị có dấu phân cách nghìn.
- "Đơn giá 1 tín chỉ Thực hành (VNĐ)" — tương tự.
- Button `.btn-primary` "Lưu cấu hình".
- Sau khi lưu: toast thành công + hỏi (dialog) "Bạn có muốn tính lại học phí cho toàn bộ sinh viên theo đơn giá mới không?" (Có/Không) — nếu Có, gọi `HocPhiService.TinhLaiHocPhiAsync` hàng loạt cho học kỳ hiện hành.

## Service (`CauHinhService`)
```csharp
Task<Dictionary<string,string>> LayTatCaAsync();
Task CapNhatAsync(Dictionary<string,string> cauHinhMoi);
Task<int> GetInt(string key);
Task<decimal> GetDecimal(string key);
```

## Checklist cho agent
- [ ] `CauHinhView.axaml` + `CauHinhViewModel`.
- [ ] `CauHinhService` đọc/ghi bảng `CauHinhHeThong`.
- [ ] Validate tối đa > tối thiểu trước khi lưu.
- [ ] Chỉ hiển thị mục Sidebar/menu này với role Admin (theo `PermissionMatrix`).

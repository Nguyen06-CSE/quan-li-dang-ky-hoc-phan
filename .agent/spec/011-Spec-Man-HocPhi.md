# 011 — SPEC MÀN HÌNH TÍNH HỌC PHÍ

## Công thức tính (đặt trong `HocPhiService`, có unit test riêng)
```
HocPhi(SV, HocKy) = Σ (SoTinChiLT_mon × DonGiaTinChiLT) + Σ (SoTinChiTH_mon × DonGiaTinChiTH)
                    với mọi môn thuộc các LopHocPhan mà SV có DangKyHocPhan.TrangThai = DangHoc
                    trong HocKy đó.
```
`DonGiaTinChiLT`, `DonGiaTinChiTH` lấy từ bảng `CauHinhHeThong` tại **thời điểm tính** (không lưu cứng đơn giá vào từng bản ghi đăng ký — luôn tính lại theo cấu hình hiện hành, trừ khi giai đoạn sau cần "khóa sổ" học kỳ đã qua, ghi TODO cho việc này).

## Giao diện (`HocPhiView.axaml`)
1. Tiêu đề `.h1` "Học phí".
2. Chọn phạm vi: radio/segmented control "Theo sinh viên" / "Theo lớp sinh hoạt" / "Theo khóa học" + ô chọn tương ứng.
3. Bảng kết quả — mỗi dòng SV: Mã SV, Họ tên, Tổng số TC, Tổng học phí, Đã đóng, Còn nợ (`PhaiDong - DaDong`), trạng thái badge màu (Đã đóng đủ = Success / Còn nợ = Warning).
4. Click 1 dòng SV → mở panel/dialog chi tiết: bảng từng môn (Tên môn, TC LT, TC TH, Thành tiền) + tổng cộng.
5. Button `.btn-secondary` "Xuất phiếu học phí (PDF)" ở cấp chi tiết từng SV, và "Xuất Excel danh sách" ở cấp bảng tổng.
6. Button `.btn-primary` "Tính lại học phí" (cho phạm vi đang chọn) — dùng khi vừa sửa cấu hình đơn giá hoặc sửa đăng ký thủ công ngoài luồng chuẩn.

## Service (`HocPhiService`)
```csharp
Task<HocPhiChiTietDto> TinhHocPhiSinhVienAsync(string maSV, string maHocKy);
Task TinhLaiHocPhiAsync(string maSV, string maHocKy); // cập nhật SoTienPhaiDong trong DangKyHocPhan
Task<List<HocPhiTongHopDto>> TinhHocPhiTheoDanhSachAsync(IEnumerable<string> dsMaSV, string maHocKy);
```
`HocPhiChiTietDto` gồm danh sách dòng môn + tổng cộng; `HocPhiTongHopDto` dùng cho bảng danh sách (không cần chi tiết từng môn).

## Checklist cho agent
- [ ] `HocPhiView.axaml` + `HocPhiViewModel`.
- [ ] `HocPhiService` với công thức đúng như trên, viết unit test kiểm tra vài trường hợp cụ thể (SV học nhiều môn, môn có cả LT lẫn TH).
- [ ] Xuất PDF phiếu học phí theo mẫu đơn giản: Header trường + tiêu đề "PHIẾU HỌC PHÍ", thông tin SV, bảng chi tiết, tổng cộng, ngày in.
- [ ] Nút "Tính lại học phí" gọi đúng `TinhLaiHocPhiAsync` cho toàn bộ SV trong phạm vi đang lọc.

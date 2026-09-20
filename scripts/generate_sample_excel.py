import openpyxl
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
import random
from datetime import datetime, timedelta

def create_sample_excel(filename="data_mau_test_import_50.xlsx"):
    wb = openpyxl.Workbook()
    ws = wb.active
    ws.title = "CT"

    # Header 21 cột theo chuẩn Spec 015
    headers = [
        "Mã SV", "Tên SV", "Lớp sinh hoạt", "Khóa Học",
        "Mã môn", "Tên môn", "Mã LHP", "Số TC",
        "Mã GV", "Tên GV", "Bậc ĐT", "Loại Hình ĐT",
        "Hình Thức ĐK", "Ngày ĐK", "Người ĐK",
        "Điểm số", "Điểm chữ",
        "T/Trạng học phí", "Đã đóng", "Phải đóng", "Giảng dạy Online"
    ]

    ws.append(headers)

    # Style Header
    header_fill = PatternFill(start_color="8CB450", end_color="8CB450", fill_type="solid")
    header_font = Font(name="Arial", size=10, bold=True, color="FFFFFF")
    thin_border = Border(
        left=Side(style='thin', color='E0E6DA'),
        right=Side(style='thin', color='E0E6DA'),
        top=Side(style='thin', color='E0E6DA'),
        bottom=Side(style='thin', color='E0E6DA')
    )

    for col_num in range(1, len(headers) + 1):
        cell = ws.cell(row=1, column=col_num)
        cell.fill = header_fill
        cell.font = header_font
        cell.alignment = Alignment(horizontal="center", vertical="center")
        cell.border = thin_border

    # Danh sách dữ liệu mẫu
    sinh_viens = [
        ("SV2025001", "Nguyễn Văn An", "21CNTT1", "K2021"),
        ("SV2025002", "Trần Thị Bích", "21CNTT1", "K2021"),
        ("SV2025003", "Lê Hoàng Cường", "21CNTT2", "K2021"),
        ("SV2025004", "Phạm Minh Đức", "21CNTT2", "K2021"),
        ("SV2025005", "Vũ Hải Đăng", "22CNTT1", "K2022"),
        ("SV2025006", "Hoàng Ngọc Hân", "22CNTT1", "K2022"),
        ("SV2025007", "Đỗ Quang Hùng", "22CNTT2", "K2022"),
        ("SV2025008", "Ngô Bảo Khánh", "22CNTT2", "K2022"),
        ("SV2025009", "Bùi Thanh Lâm", "23CNTT1", "K2023"),
        ("SV2025010", "Dương Thu Mai", "23CNTT1", "K2023"),
        ("SV2025011", "Trịnh Gia Nam", "23CNTT2", "K2023"),
        ("SV2025012", "Lý Tuyết Nhung", "23CNTT2", "K2023"),
    ]

    mon_hocs = [
        ("INT101", "Lập trình C/C++ cơ bản", 3, "LHP_INT101_01", "GV01", "TS. Nguyễn Văn Toàn"),
        ("INT102", "Cấu trúc dữ liệu và giải thuật", 4, "LHP_INT102_01", "GV01", "TS. Nguyễn Văn Toàn"),
        ("INT103", "Cơ sở dữ liệu", 3, "LHP_INT103_01", "GV02", "ThS. Trần Thị Lan"),
        ("INT104", "Lập trình hướng đối tượng", 4, "LHP_INT104_01", "GV02", "ThS. Trần Thị Lan"),
        ("INT105", "Mạng máy tính", 3, "LHP_INT105_01", "GV03", "TS. Lê Quang Vũ"),
        ("INT106", "Kiến trúc máy tính & HĐH", 3, "LHP_INT106_01", "GV03", "TS. Lê Quang Vũ"),
        ("INT107", "Công nghệ phần mềm", 3, "LHP_INT107_01", "GV04", "ThS. Phạm Thu Hà"),
        ("INT108", "Trí tuệ nhân tạo", 3, "LHP_INT108_01", "GV04", "ThS. Phạm Thu Hà"),
        ("INT109", "Phát triển ứng dụng Web", 4, "LHP_INT109_01", "GV01", "TS. Nguyễn Văn Toàn"),
        ("INT110", "An toàn thông tin", 3, "LHP_INT110_01", "GV03", "TS. Lê Quang Vũ"),
    ]

    base_date = datetime(2025, 9, 1, 8, 30)
    
    # Tạo 50 dòng kết hợp sinh viên và môn học
    current_count = 0
    row_idx = 2

    # Lặp để sinh đủ 50 dòng đăng ký
    for sv in sinh_viens:
        # Mỗi SV đăng ký 3-5 môn
        sample_mons = random.sample(mon_hocs, min(4, len(mon_hocs)))
        for mon in sample_mons:
            if current_count >= 50:
                break

            current_count += 1
            ngay_dk = (base_date + timedelta(days=random.randint(1, 10), minutes=random.randint(5, 500))).strftime("%d/%m/%Y %H:%M")
            diem_so = round(random.uniform(5.5, 9.5), 1) if random.random() > 0.4 else None
            diem_chu = "A" if diem_so and diem_so >= 8.5 else ("B" if diem_so and diem_so >= 7.0 else ("C" if diem_so else None))
            online = "X" if random.random() > 0.7 else ""

            row_data = [
                sv[0],          # Mã SV
                sv[1],          # Tên SV
                sv[2],          # Lớp sinh hoạt
                sv[3],          # Khóa học
                mon[0],         # Mã môn
                mon[1],         # Tên môn
                mon[3],         # Mã LHP
                mon[2],         # Số TC
                mon[4],         # Mã GV
                mon[5],         # Tên GV
                "Đại học",      # Bậc ĐT
                "Chính quy",    # Loại hình ĐT
                "Trực tuyến",   # Hình thức ĐK
                ngay_dk,        # Ngày ĐK
                "sv_online",    # Người ĐK
                diem_so if diem_so else "",  # Điểm số
                diem_chu if diem_chu else "", # Điểm chữ
                "Đã đóng",      # T/trạng học phí
                1500000,        # Đã đóng
                1500000,        # Phải đóng
                online          # Giảng dạy Online
            ]

            ws.append(row_data)

            # Style Data Row
            for col_num in range(1, len(headers) + 1):
                cell = ws.cell(row=row_idx, column=col_num)
                cell.font = Font(name="Arial", size=9)
                cell.border = thin_border
                if col_num in [1, 5, 7, 8, 9, 11, 12, 13, 16, 17, 21]:
                    cell.alignment = Alignment(horizontal="center", vertical="center")
                elif col_num in [18, 19, 20]:
                    cell.alignment = Alignment(horizontal="right", vertical="center")
                else:
                    cell.alignment = Alignment(horizontal="left", vertical="center")

            row_idx += 1

        if current_count >= 50:
            break

    # Auto-fit column widths
    for col in ws.columns:
        max_len = max(len(str(cell.value or '')) for cell in col)
        col_letter = openpyxl.utils.get_column_letter(col[0].column)
        ws.column_dimensions[col_letter].width = max(max_len + 4, 12)

    wb.save(filename)
    print(f"Da tao thanh cong file: {filename} voi {current_count} dong du lieu.")

if __name__ == "__main__":
    create_sample_excel("sample_data_import_50.xlsx")

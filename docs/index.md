---
id: index
title: Báo Cáo Dự Án Quản Lý Đăng Ký Học Phần
slug: /
sidebar_position: 1
---

# Báo Cáo Dự Án: Hệ Thống Quản Lý Đăng Ký Học Phần (QuanLyDKHP)

Tài liệu kỹ thuật, đặc tả phần mềm và hồ sơ minh chứng quá trình phát triển ứng dụng Desktop phục vụ công tác quản lý đào tạo, đăng ký học phần, tính học phí và lập báo cáo thống kê.

---

## 1. Thông Tin Đồ Án Môn Học

* **Học phần:** Xây dựng Ứng dụng Desktop
* **Môi trường & Công nghệ cốt lõi:** C# 12, .NET 8.0, Avalonia UI 11.x, Entity Framework Core 8.0, PostgreSQL & SQLite.
* **Mô hình kiến trúc:** Clean Architecture (Domain-Driven Design / MVVM Pattern).
* **Đơn vị thực hiện:** Nhóm sinh viên ngành Công nghệ Thông tin.
* **Mã nguồn dự án (GitHub):** [quan-li-dang-ky-hoc-phan](https://github.com/Nguyen06-CSE/quan-li-dang-ky-hoc-phan)
* **Website tài liệu:** Triển khai tự động thông qua GitHub Pages & Docusaurus.

---

## 2. Mục Tiêu & Yêu Cầu Nộp Bài

Báo cáo này được cấu trúc thành ba phần trọng tâm nhằm đáp ứng đầy đủ các tiêu chí đánh giá môn học:

### Phần I: Đặc Tả Ứng Dụng (Application Specifications)
Trình bày chi tiết bài toán nghiệp vụ, 9 chức năng cốt lõi theo đề bài giảng viên giao cùng các tính năng mở rộng (nhập dữ liệu Excel quy mô lớn, bộ nhớ đệm lai SQLite, xem thời khóa biểu đồ họa). Đồng thời mô tả toàn diện kiến trúc 4 tầng, thiết kế cơ sở dữ liệu quan hệ (ERD), ma trận phân quyền 4 vai trò và hệ thống giao diện (Design System).

### Phần II: Kết Quả Chung Đạt Được (Deliverables & Results)
Tổng hợp ma trận đối chiếu 100% tính năng so với yêu cầu ban đầu; báo cáo độ tin cậy thông qua hệ thống kiểm thử tự động (42/42 Unit Tests vượt qua kiểm tra); các giải pháp tối ưu hiệu năng giao diện phản hồi 0ms; và các ấn phẩm báo cáo mẫu (phiếu học phí A4 PDF, bảng tính Excel).

### Phần III: Minh Chứng Thực Hiện & Nhật Ký Làm Việc (Work Logs & Audit Trail)
Cung cấp toàn bộ bằng chứng xác thực về quá trình làm việc độc lập và làm việc nhóm:
1. **Lịch sử Git Commits:** Thống kê 29 commits với phân bổ thời gian thực hiện, thông điệp commit chuẩn hóa và đóng góp của từng thành viên.
2. **Nhật ký công việc hằng ngày (Daily Logs):** Tiến độ chi tiết từng ngày, các hạng mục hoàn thành và khó khăn đã vượt qua.
3. **Nhật ký quyết định kỹ thuật (ADR / Decisions Log):** Ghi nhận các phân tích đánh đổi kiến trúc và giải pháp công nghệ.
4. **Nhật ký xử lý sự cố (Troubleshooting & Fixes):** Phân tích nguyên nhân gốc rễ và giải pháp cho các lỗi phức tạp (bảo mật macOS Gatekeeper, xung đột luồng EF Core, chống lặp vòng lặp liên kết giao diện).
5. **Nhật ký Prompt với AI Agent:** Minh chứng các câu lệnh và phiên làm việc với AI Agent trong việc phân tích đặc tả, sinh mã và kiểm thử tự động.

---

## 3. Bản Đồ Tra Cứu Tài Liệu

| Mục | Nội dung chính | Trọng tâm đánh giá |
|---|---|---|
| **[Đặc tả Tổng quan Yêu cầu](01-dac-ta-ung-dung/01-tong-quan-yeu-cau.md)** | Bối cảnh, mục tiêu, 9 yêu cầu đề bài và tính năng mở rộng | Phạm vi và độ hoàn thiện đề tài |
| **[Kiến trúc Hệ thống](01-dac-ta-ung-dung/02-kien-truc-he-thong.md)** | Clean Architecture, MVVM, In-Memory Caching, SQLite Sync | Thiết kế phần mềm và tính mở rộng |
| **[Thiết kế Cơ sở dữ liệu & ERD](01-dac-ta-ung-dung/03-co-so-du-lieu-erd.md)** | Sơ đồ quan hệ thực thể, ràng buộc toàn vẹn, migration | Độ chuẩn hóa và toàn vẹn dữ liệu |
| **[Ma trận Phân quyền](01-dac-ta-ung-dung/04-ma-tran-phan-quyen.md)** | Quyền hạn 4 vai trò: Admin, Trợ lý giáo vụ, Giáo vụ BM, Giảng viên | Bảo mật và quản trị người dùng |
| **[Hệ thống Giao diện UI/UX](01-dac-ta-ung-dung/05-design-system-ui-ux.md)** | Avalonia UI, App Shell, Liquid Glass Theme, Material Icons | Trải nghiệm và thẩm mỹ Desktop |
| **[Đặc tả 11 Chức năng](01-dac-ta-ung-dung/chuc-nang/F01-xac-thuc-dang-nhap.md)** | Chi tiết luồng nghiệp vụ F01 đến F11 (bao gồm CSDL SQLite Local & Git Backup) | Độ sâu nghiệp vụ đào tạo đại học |
| **[Tổng kết & Đối chiếu Tính năng](02-ket-qua-dat-duoc/01-tong-ket-chuc-nang.md)** | Ma trận hoàn thành 9 yêu cầu cốt lõi + 4 yêu cầu mở rộng | Đạt chuẩn nghiệm thu đề tài |
| **[Báo cáo Kiểm thử Unit Test](02-ket-qua-dat-duoc/02-kiem-thu-unit-test.md)** | Kết quả 42/42 Unit Tests đạt 100% Passed | Chất lượng và độ tin cậy của mã nguồn |
| **[Tối ưu Hiệu năng](02-ket-qua-dat-duoc/03-toi-uu-hieu-nang.md)** | Debounce, UI Virtualization, In-Memory Caching 0ms | Tối ưu hóa ứng dụng Desktop |
| **[Hướng dẫn Cài đặt & Khởi chạy](02-ket-qua-dat-duoc/05-huong-dan-cai-dat-khoi-chay.md)** | Cài đặt môi trường .NET 8, cấu hình DB Neon/Local | Tính khả thi và tái lập sản phẩm |
| **[Lịch sử Git Commits](03-nhat-ky-va-minh-chung/01-lich-su-commit-github.md)** | Bảng tổng hợp toàn bộ commits từ kho lưu trữ GitHub | Minh chứng lịch sử phát triển |
| **[Nhật ký Công việc Daily](03-nhat-ky-va-minh-chung/02-nhat-ky-cong-viec-daily.md)** | Báo cáo tiến độ theo ngày của từng thành viên | Minh chứng thái độ và tiến trình làm việc |
| **[Nhật ký Quyết định Kỹ thuật](03-nhat-ky-va-minh-chung/03-nhat-ky-quyet-dinh-ky-thuat.md)** | Các quyết định kiến trúc (ADR) và đánh đổi kỹ thuật | Năng lực giải quyết vấn đề kỹ thuật |
| **[Nhật ký Sự cố & Sửa lỗi](03-nhat-ky-va-minh-chung/04-nhat-ky-su-co-va-sua-loi.md)** | Bảng tổng hợp các lỗi phát sinh và phương án khắc phục | Năng lực gỡ lỗi và kiểm soát rủi ro |
| **[Nhật ký Prompts với AI Agent](03-nhat-ky-va-minh-chung/05-nhat-ky-prompts-agent.md)** | Chi tiết 10 phiên làm việc với AI Agent (Prompt Engineering) | Ứng dụng AI vào kỹ thuật phần mềm |

# Nhật ký Quyết định (Decisions Log)

File này sẽ ghi lại các quyết định kỹ thuật, kiến trúc hoặc nghiệp vụ được đưa ra trong quá trình phát triển dự án.

## 2026-09-18 - 001-Spec-KienTrucDuAn.md
**Vấn đề:** Khi cài đặt các package NuGet như `Npgsql.EntityFrameworkCore.PostgreSQL` và `Microsoft.Extensions.*` mà không chỉ định version, hệ thống tự động tải bản mới nhất (v10.x) dẫn tới lỗi không tương thích (NU1202) do framework của dự án được chốt là .NET 8.
**Quyết định:** Chỉ định cứng version `8.x` cho các thư viện phụ thuộc .NET (như `Microsoft.Extensions.DependencyInjection`, `Npgsql.EntityFrameworkCore.PostgreSQL`...) khi thêm vào các project.
**Lý do:** Đảm bảo tính tương thích và build thành công với .NET 8 theo đúng yêu cầu công nghệ (Avalonia UI .NET 8).

using System;
using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuanLyDKHP.Infrastructure.Data;

/// <summary>
/// Factory dùng khi chạy lệnh `dotnet ef ...` (design-time). Không chứa mật khẩu trong source:
/// đọc từ biến môi trường ConnectionStrings__DefaultConnection, hoặc file appsettings.Local.json của project App.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        string? connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? DocTuFileLocal();

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Không tìm thấy chuỗi kết nối. Đặt biến môi trường ConnectionStrings__DefaultConnection " +
                "hoặc tạo src/QuanLyDKHP.App/appsettings.Local.json.");

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseNpgsql(connectionString);
        return new AppDbContext(builder.Options);
    }

    private static string? DocTuFileLocal()
    {
        string[] duongDan =
        {
            Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "QuanLyDKHP.App", "appsettings.Local.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "QuanLyDKHP.App", "appsettings.Local.json"),
        };

        foreach (var p in duongDan)
        {
            if (!File.Exists(p)) continue;
            using var doc = JsonDocument.Parse(File.ReadAllText(p));
            if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                cs.TryGetProperty("DefaultConnection", out var val))
                return val.GetString();
        }
        return null;
    }
}

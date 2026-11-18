using System.Threading.Tasks;

namespace QuanLyDKHP.Core.Interfaces;

/// <summary>
/// Lưu cấu hình chuỗi kết nối PostgreSQL dùng cho backup (tầng 2).
/// Secret không được đưa vào appsettings đã commit và không được ghi log.
/// </summary>
public interface IBackupConfigStore
{
    string ConfigFilePath { get; }

    /// <summary>Trả về connection string đã lưu, hoặc null nếu chưa cấu hình/không đọc được.</summary>
    string? GetConnectionString();

    Task SaveConnectionStringAsync(string connectionString);

    Task ClearAsync();
}

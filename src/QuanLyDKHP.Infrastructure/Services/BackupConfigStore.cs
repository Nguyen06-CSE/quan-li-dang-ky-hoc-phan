using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Services;

/// <summary>
/// Lưu connection string backup trong %LocalAppData%/QuanLyDKHP/backup-config.json.
/// Windows: mã hóa bằng DPAPI (CurrentUser). Unix: file quyền 0600.
/// File cũ chỉ có "ConnectionString" vẫn đọc được và sẽ được nâng cấp khi lưu lại.
/// </summary>
public class BackupConfigStore : IBackupConfigStore
{
    private const string DpapiProtection = "windows-dpapi-current-user";
    private const string UnixProtection = "unix-file-permissions";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _configFilePath;

    public BackupConfigStore() : this(null)
    {
    }

    public BackupConfigStore(string? configFilePath)
    {
        _configFilePath = configFilePath ?? Path.Combine(
            Path.GetDirectoryName(AppDbContext.GetPrimarySqlitePath()) ?? AppContext.BaseDirectory,
            "backup-config.json");
    }

    public string ConfigFilePath => _configFilePath;

    public string? GetConnectionString()
    {
        if (!File.Exists(_configFilePath)) return null;

        try
        {
            var config = JsonSerializer.Deserialize<BackupConfigFile>(File.ReadAllText(_configFilePath));
            if (config == null) return null;

            if (!string.IsNullOrWhiteSpace(config.ProtectedConnectionString) && OperatingSystem.IsWindows())
            {
                return Unprotect(config.ProtectedConnectionString);
            }

            // File cũ (plaintext) hoặc file tạo trên Unix.
            return string.IsNullOrWhiteSpace(config.ConnectionString) ? null : config.ConnectionString;
        }
        catch (Exception ex) when (ex is IOException or JsonException or CryptographicException or PlatformNotSupportedException)
        {
            // Không log nội dung file/secret, chỉ log loại lỗi.
            Debug.WriteLine($"[BackupConfig] Không đọc được cấu hình backup: {ex.GetType().Name}");
            return null;
        }
    }

    public Task SaveConnectionStringAsync(string connectionString)
    {
        var trimmed = connectionString?.Trim() ?? string.Empty;
        if (trimmed.Length == 0) return ClearAsync();

        ValidateConnectionString(trimmed);

        var file = OperatingSystem.IsWindows()
            ? new BackupConfigFile
            {
                Protection = DpapiProtection,
                ProtectedConnectionString = Protect(trimmed)
            }
            : new BackupConfigFile
            {
                Protection = UnixProtection,
                ConnectionString = trimmed
            };

        WriteConfig(file);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        if (File.Exists(_configFilePath))
        {
            File.Delete(_configFilePath);
        }

        return Task.CompletedTask;
    }

    private static void ValidateConnectionString(string connectionString)
    {
        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            throw new ArgumentException("Chuỗi kết nối PostgreSQL không hợp lệ. Dùng định dạng Host=...;Database=...;Username=...;Password=...;SSL Mode=Require.");
        }

        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            throw new ArgumentException("Chuỗi kết nối PostgreSQL phải có Host=...");
        }
    }

    private void WriteConfig(BackupConfigFile file)
    {
        var directory = Path.GetDirectoryName(_configFilePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(file, JsonOptions);
        var tempPath = $"{_configFilePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllText(tempPath, json);

            if (!OperatingSystem.IsWindows())
            {
                TrySetOwnerOnlyMode(tempPath);
            }

            File.Move(tempPath, _configFilePath, overwrite: true);

            if (!OperatingSystem.IsWindows())
            {
                TrySetOwnerOnlyMode(_configFilePath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
    }

    [UnsupportedOSPlatform("windows")]
    private static void TrySetOwnerOnlyMode(string path)
    {
        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch
        {
            // Một số filesystem không hỗ trợ; không làm hỏng thao tác lưu.
        }
    }

    [SupportedOSPlatform("windows")]
    private static string Protect(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    [SupportedOSPlatform("windows")]
    private static string Unprotect(string protectedBase64)
    {
        var protectedBytes = Convert.FromBase64String(protectedBase64);
        var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }

    private sealed class BackupConfigFile
    {
        public int Version { get; set; } = 1;
        public string Protection { get; set; } = string.Empty;
        public string? ConnectionString { get; set; }
        public string? ProtectedConnectionString { get; set; }
    }
}

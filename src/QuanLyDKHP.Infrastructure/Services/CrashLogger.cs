using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace QuanLyDKHP.Infrastructure.Services;

/// <summary>
/// Ghi log sự cố dạng append-only dưới AppData (app-YYYYMMDD.log).
/// Không bao giờ ném lỗi ra ngoài và luôn che các trường mật khẩu trước khi ghi.
/// </summary>
public static class CrashLogger
{
    private static readonly object WriteLock = new();
    private static readonly Regex SecretPattern = new(
        @"(?i)\b(password|pwd)\s*=\s*[^;\r\n]*",
        RegexOptions.Compiled);

    public static string LogDirectory { get; } = ResolveLogDirectory();

    public static string CurrentLogFilePath => Path.Combine(LogDirectory, $"app-{DateTime.Now:yyyyMMdd}.log");

    public static void Info(string message) => Write("INFO", message, null);

    public static void Error(string context, Exception? exception = null)
        => Write("ERROR", context, exception?.ToString());

    public static void Write(string level, string message, string? detail)
    {
        try
        {
            var sanitizedMessage = Sanitize(message);
            var sanitizedDetail = string.IsNullOrWhiteSpace(detail) ? null : Sanitize(detail);

            var entry = new StringBuilder()
                .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                .Append(" [").Append(level).Append(']')
                .Append(" [thread ").Append(Environment.CurrentManagedThreadId).Append("] ")
                .AppendLine(sanitizedMessage);

            if (sanitizedDetail != null)
            {
                entry.AppendLine(sanitizedDetail);
            }

            lock (WriteLock)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(CurrentLogFilePath, entry.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Logging không được phép làm sập ứng dụng.
        }
    }

    private static string Sanitize(string value) => SecretPattern.Replace(value, "$1=***");

    private static string ResolveLogDirectory()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root)) root = AppContext.BaseDirectory;
        return Path.Combine(root, "QuanLyDKHP", "logs");
    }
}

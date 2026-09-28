using System;
using System.IO;
using System.Text.Json;

namespace QuanLyDKHP.App.Services;

/// <summary>
/// Lưu trữ cục bộ (trên máy client) cho tính năng "Lưu đăng nhập".
/// Chỉ lưu TÊN ĐĂNG NHẬP để tự động điền lại ở lần mở app sau — KHÔNG lưu mật khẩu
/// hay bất kỳ token phiên nào, vì AuthService hiện tại chưa phát hành session token
/// (xác thực trực tiếp qua DB mỗi lần). Đây là bước đệm UX, không phải cơ chế "giữ đăng nhập"
/// bỏ qua bước nhập mật khẩu.
/// </summary>
public static class LocalSessionStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "QuanLyDKHP",
        "remember-me.json");

    private class RememberMeData
    {
        public string? TenDangNhap { get; set; }
        public bool GhiNho { get; set; }
    }

    public static (string? TenDangNhap, bool GhiNho) Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return (null, false);

            var json = File.ReadAllText(FilePath);
            var data = JsonSerializer.Deserialize<RememberMeData>(json);
            if (data == null || !data.GhiNho)
                return (null, false);

            return (data.TenDangNhap, true);
        }
        catch
        {
            // File hỏng/không đọc được -> coi như chưa lưu gì, không làm crash app
            return (null, false);
        }
    }

    public static void Save(string? tenDangNhap, bool ghiNho)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var data = new RememberMeData
            {
                TenDangNhap = ghiNho ? tenDangNhap : null,
                GhiNho = ghiNho
            };

            File.WriteAllText(FilePath, JsonSerializer.Serialize(data));
        }
        catch
        {
            // Không chặn luồng đăng nhập nếu ghi file thất bại (ví dụ: không có quyền ghi)
        }
    }
}

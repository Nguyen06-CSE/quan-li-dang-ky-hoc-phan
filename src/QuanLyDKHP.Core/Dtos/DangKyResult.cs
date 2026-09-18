namespace QuanLyDKHP.Core.Dtos;

public class DangKyResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsCanhBaoTrungMon { get; set; }
    public string? MaLHPCu { get; set; }
    public string? TenMon { get; set; }

    public static DangKyResult Ok(string message = "Đăng ký thành công.") =>
        new() { Success = true, Message = message };

    public static DangKyResult Fail(string message) =>
        new() { Success = false, Message = message };

    public static DangKyResult CanhBaoTrungMon(string maLHPCu, string tenMon, string message) =>
        new() { Success = false, IsCanhBaoTrungMon = true, MaLHPCu = maLHPCu, TenMon = tenMon, Message = message };
}

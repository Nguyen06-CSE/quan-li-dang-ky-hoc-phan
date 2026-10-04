namespace QuanLyDKHP.App.Messages;

/// <summary>
/// Message điều hướng liên module từ Tab Sinh Viên sang Tab Đăng Ký Học Phần.
/// </summary>
/// <param name="MaSV">Mã sinh viên cần mở xem/đăng ký chi tiết.</param>
public sealed record NavigateToRegistrationMessage(string MaSV);

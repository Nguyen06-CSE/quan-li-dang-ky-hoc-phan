namespace QuanLyDKHP.App.Messages;

/// <summary>
/// Message điều hướng liên module sang Tab Import Excel / Smart Studio.
/// Có thể kèm Mã học kỳ và Mã LHP đích để nạp thẳng danh sách vào lớp.
/// </summary>
public sealed record NavigateToImportExcelMessage(string? MaHocKy = null, string? MaLHP = null);

namespace QuanLyDKHP.App.Messages;

/// <summary>
/// Message điều hướng liên module sang Tab Học Phí và chọn sẵn sinh viên theo Mã SV.
/// </summary>
/// <param name="MaSV">Mã sinh viên cần tra cứu/thu học phí.</param>
public sealed record NavigateToHocPhiMessage(string MaSV);

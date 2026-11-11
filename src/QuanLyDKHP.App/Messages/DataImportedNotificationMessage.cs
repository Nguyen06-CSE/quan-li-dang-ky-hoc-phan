using QuanLyDKHP.Core.Dtos;

namespace QuanLyDKHP.App.Messages;

/// <summary>
/// Broadcast khi hoàn tất import dữ liệu Excel, yêu cầu tất cả ViewModel làm mới dữ liệu tự động trên giao diện (0ms delay, không cần khởi động lại app).
/// </summary>
public sealed record DataImportedNotificationMessage(KetQuaImportDto KetQua);

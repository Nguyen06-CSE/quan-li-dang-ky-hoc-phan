using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;

namespace QuanLyDKHP.App.ViewModels;

public enum CommandPaletteItemKind
{
    Navigation,
    Action,
    SmartQuery,
    Student,
    ClassSection,
    Course
}

/// <summary>
/// Đại diện cho một dòng kết quả / lệnh trong Universal Command Palette (Ctrl + K).
/// </summary>
public partial class CommandPaletteItemViewModel : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string CategoryLabel { get; init; } = string.Empty;
    public string BadgeText { get; init; } = string.Empty;
    public string BadgeStatus { get; init; } = "Primary"; // Primary, Success, Warning, Danger, Info
    public MaterialIconKind Icon { get; init; } = MaterialIconKind.Sparkles;
    public string ShortcutHint { get; init; } = "Enter ↵";
    public CommandPaletteItemKind ItemKind { get; init; }
    public string? EntityKey { get; init; }
    public string? SecondaryKey { get; init; }
    public string? DetailedDescription { get; init; }
    public Func<Task>? ExecuteAsync { get; init; }

    public bool IsBadgePrimary => BadgeStatus == "Primary";
    public bool IsBadgeSuccess => BadgeStatus == "Success";
    public bool IsBadgeWarning => BadgeStatus == "Warning";
    public bool IsBadgeDanger  => BadgeStatus == "Danger";
    public bool IsBadgeInfo    => BadgeStatus == "Info";
}

/// <summary>
/// Môn học tóm tắt hiển thị trong thẻ X-Ray Sinh viên của Command Palette.
/// </summary>
public sealed class SpotlightStudentCourseDto
{
    public string MaLHP { get; init; } = string.Empty;
    public string TenMon { get; init; } = string.Empty;
    public int SoTinChi { get; init; }
    public string LichHocText { get; init; } = string.Empty;
}

/// <summary>
/// Dữ liệu X-Ray Chi tiết Sinh viên hiển thị trực tiếp ở cột phải của Command Palette (0ms-2ms).
/// </summary>
public sealed class SpotlightStudentXRayDto
{
    public string MaSV { get; init; } = string.Empty;
    public string HoTen { get; init; } = string.Empty;
    public string LopSinhHoat { get; init; } = "--";
    public string KhoaHoc { get; init; } = "--";
    public string MaHocKy { get; init; } = string.Empty;
    public string TenHocKy { get; init; } = string.Empty;
    public int TongTinChi { get; init; }
    public int SoLopDangHoc { get; init; }
    public double TinChiProgress => Math.Min(100.0, TongTinChi * 100.0 / 25.0);
    public decimal TongHocPhi { get; init; }
    public decimal DaDong { get; init; }
    public decimal ConNo { get; init; }
    public bool IsConNo => ConNo > 0;
    public string TongHocPhiFormat => $"{TongHocPhi:N0} đ";
    public string DaDongFormat => $"{DaDong:N0} đ";
    public string ConNoFormat => $"{ConNo:N0} đ";
    public string TrangThaiHocPhiText => ConNo > 0 ? $"Còn nợ {ConNo:N0} đ" : "Đã hoàn tất học phí";
    public string AvatarInitials
    {
        get
        {
            var parts = (HoTen ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "SV";
            if (parts.Length == 1) return parts[0][..1].ToUpperInvariant();
            return (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
        }
    }
    public List<SpotlightStudentCourseDto> Courses { get; init; } = new();
    public bool HasCourses => Courses.Count > 0;
}

/// <summary>
/// Dữ liệu X-Ray Chi tiết Lớp học phần hiển thị trực tiếp ở cột phải của Command Palette.
/// </summary>
public sealed class SpotlightClassXRayDto
{
    public string MaLHP { get; init; } = string.Empty;
    public string MaMon { get; init; } = string.Empty;
    public string TenMon { get; init; } = string.Empty;
    public string MaHocKy { get; init; } = string.Empty;
    public int SoTinChi { get; init; }
    public string GiangVien { get; init; } = "Chưa phân công";
    public string LichHocText { get; init; } = "Chưa xếp lịch";
    public string PhongHoc { get; init; } = "--";
    public int SiSoHienTai { get; init; }
    public int SiSoToiDa { get; init; }
    public double PhanTramLapDay => SiSoToiDa > 0 ? Math.Min(100.0, Math.Round(SiSoHienTai * 100.0 / SiSoToiDa, 1)) : 0;
    public string SiSoText => $"{SiSoHienTai} / {(SiSoToiDa > 0 ? SiSoToiDa.ToString() : "∞")} SV ({PhanTramLapDay:0.#}%)";
    public bool IsQuaTai => SiSoToiDa > 0 && SiSoHienTai >= SiSoToiDa;
    public bool IsSapDay => SiSoToiDa > 0 && SiSoHienTai >= SiSoToiDa * 0.8 && SiSoHienTai < SiSoToiDa;
    public List<string> TopStudentsPreview { get; init; } = new();
    public bool HasStudents => TopStudentsPreview.Count > 0;
}

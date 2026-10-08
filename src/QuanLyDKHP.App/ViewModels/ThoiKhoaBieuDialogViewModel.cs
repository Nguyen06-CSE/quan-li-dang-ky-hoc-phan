using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.App.Dtos;

namespace QuanLyDKHP.App.ViewModels;

/// <summary>
/// Module 4 — Thời khóa biểu. Ô trong lưới hiển thị cho 1 (Thứ, Tiết) cụ thể.
/// </summary>
public class ThoiKhoaBieuOClass
{
    public string MaLHP { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public string? Phong { get; set; }
    public int TietBatDau { get; set; }
    public int SoTiet { get; set; }
    public int TietKetThuc => TietBatDau + SoTiet - 1;
    public string TietHienThi => $"Tiết {TietBatDau}-{TietKetThuc}";

    public string HienThi => string.IsNullOrWhiteSpace(Phong)
        ? $"{TenMon}\n({MaLHP})"
        : $"{TenMon}\n({MaLHP}) - {Phong}";
}

/// <summary>
/// 1 cột của lưới TKB = 1 Thứ, mỗi cột chứa danh sách lớp rơi vào từng tiết (không lồng được thì liệt kê dọc).
/// </summary>
public class ThoiKhoaBieuCot
{
    public string TenThu { get; set; } = string.Empty;
    public ObservableCollection<ThoiKhoaBieuOClass> CacLop { get; set; } = [];
}

/// <summary>
/// ViewModel cho dialog xem Thời khóa biểu (Module 4).
/// Dựng hoàn toàn từ danh sách DangKyHocPhanDisplayDto (TrangThai = DangHoc) được truyền vào — không
/// query thêm DB, không bịa dữ liệu: LHP nào "chưa xếp lịch" (Thu/TietBatDau/SoTiet NULL trên
/// LopHocPhan) bị loại khỏi lưới và liệt kê riêng ở danh sách "Chưa xếp lịch" bên dưới.
/// </summary>
public partial class ThoiKhoaBieuDialogViewModel : ObservableObject
{
    private static readonly string[] TenThuThuTu =
        { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ nhật" };

    public ObservableCollection<ThoiKhoaBieuCot> Cols { get; } = [];
    public ObservableCollection<string> DsChuaXepLich { get; } = [];

    [ObservableProperty]
    private bool _coLopChuaXepLich;

    public Action? CloseAction { get; set; }

    public ThoiKhoaBieuDialogViewModel(List<DangKyHocPhanDisplayDto> dsDaDangKy)
    {
        // Khởi tạo 7 cột Thứ 2 -> CN (Thu = 2..8 trong DB) theo đúng thứ tự hiển thị quen thuộc.
        for (int i = 0; i < TenThuThuTu.Length; i++)
        {
            Cols.Add(new ThoiKhoaBieuCot { TenThu = TenThuThuTu[i] });
        }

        foreach (var dk in dsDaDangKy)
        {
            var lhp = dk.Entity?.LopHocPhan;
            if (lhp?.Thu is not int thu || thu < 2 || thu > 8 ||
                lhp.TietBatDau is not int tietBatDau || lhp.SoTiet is not int soTiet)
            {
                DsChuaXepLich.Add($"{dk.MaLHP} - {dk.TenMon}");
                continue;
            }

            int colIndex = thu - 2; // Thu=2 (Thứ 2) -> cột 0 ... Thu=8 (CN) -> cột 6
            Cols[colIndex].CacLop.Add(new ThoiKhoaBieuOClass
            {
                MaLHP = dk.MaLHP,
                TenMon = dk.TenMon,
                Phong = lhp.Phong,
                TietBatDau = tietBatDau,
                SoTiet = soTiet
            });
        }

        foreach (var col in Cols)
        {
            var sorted = col.CacLop.OrderBy(c => c.TietBatDau).ToList();
            col.CacLop.Clear();
            foreach (var c in sorted) col.CacLop.Add(c);
        }

        CoLopChuaXepLich = DsChuaXepLich.Count > 0;
    }

    [RelayCommand]
    private void Close() => CloseAction?.Invoke();
}

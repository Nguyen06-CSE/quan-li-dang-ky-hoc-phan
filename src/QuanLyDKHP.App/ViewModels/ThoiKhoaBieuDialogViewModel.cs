using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.App.Dtos;

namespace QuanLyDKHP.App.ViewModels;

/// <summary>
/// Module 4 &amp; Nâng Cấp 2 — Ô hiển thị Lớp học phần trong Ma trận Thời khóa biểu Tuần.
/// </summary>
public class ThoiKhoaBieuOClass
{
    public string MaLHP { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public string? Phong { get; set; }
    public string TenGiangVien { get; set; } = "Chưa phân công";
    public int SoTinChi { get; set; }
    public bool GiangDayOnline { get; set; }
    public int TietBatDau { get; set; }
    public int SoTiet { get; set; }
    public int TietKetThuc => TietBatDau + SoTiet - 1;
    public string TietHienThi => $"Tiết {TietBatDau}–{TietKetThuc}";
    public bool IsCaSang => TietBatDau <= 6;
    public string CaHocBadge => IsCaSang ? "☀ Sáng" : "🌤 Chiều";
    public string PhongBadge => GiangDayOnline
        ? "🌐 Trực tuyến"
        : (string.IsNullOrWhiteSpace(Phong) ? "📍 Chưa xếp phòng" : $"📍 Phòng {Phong}");

    public string HienThi => string.IsNullOrWhiteSpace(Phong)
        ? $"{TenMon}\n({MaLHP})"
        : $"{TenMon}\n({MaLHP}) - {Phong}";
}

/// <summary>
/// 1 cột của lưới TKB = 1 Thứ trong tuần, hỗ trợ phân tầng Ca Sáng / Ca Chiều trực quan (Nâng Cấp 2).
/// </summary>
public class ThoiKhoaBieuCot
{
    public string TenThu { get; set; } = string.Empty;
    public ObservableCollection<ThoiKhoaBieuOClass> CacLop { get; set; } = [];
    public ObservableCollection<ThoiKhoaBieuOClass> CaSang { get; set; } = [];
    public ObservableCollection<ThoiKhoaBieuOClass> CaChieu { get; set; } = [];

    public bool CoLopTrongNgay => CacLop.Count > 0;
    public int SoCaTrongNgay => CacLop.Count;
}

/// <summary>
/// ViewModel cho dialog xem Thời khóa biểu Ma trận Tuần (Nâng Cấp 2: Ma Trận Thời Khóa Biểu Tuần Trực Quan).
/// </summary>
public partial class ThoiKhoaBieuDialogViewModel : ObservableObject
{
    private static readonly string[] TenThuThuTu =
        { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ nhật" };

    public ObservableCollection<ThoiKhoaBieuCot> Cols { get; } = [];
    public ObservableCollection<string> DsChuaXepLich { get; } = [];

    [ObservableProperty]
    private bool _coLopChuaXepLich;

    [ObservableProperty]
    private int _tongSoLop;

    [ObservableProperty]
    private int _tongTinChi;

    [ObservableProperty]
    private int _tongBuoiHocTuan;

    [ObservableProperty]
    private int _soLopOnline;

    [ObservableProperty]
    private string? _copyStatusToast;

    public Action? CloseAction { get; set; }

    public ThoiKhoaBieuDialogViewModel(List<DangKyHocPhanDisplayDto> dsDaDangKy)
    {
        for (int i = 0; i < TenThuThuTu.Length; i++)
        {
            Cols.Add(new ThoiKhoaBieuCot { TenThu = TenThuThuTu[i] });
        }

        var activeList = dsDaDangKy.Where(d => d.IsDangHoc || string.IsNullOrEmpty(d.TrangThai)).ToList();
        TongSoLop = activeList.Count;
        TongTinChi = activeList.Sum(d => d.TongTinChi);
        SoLopOnline = activeList.Count(d => d.Entity?.LopHocPhan?.GiangDayOnline == true);

        foreach (var dk in activeList)
        {
            var lhp = dk.Entity?.LopHocPhan;
            if (lhp?.Thu is not int thu || thu < 2 || thu > 8 ||
                lhp.TietBatDau is not int tietBatDau || lhp.SoTiet is not int soTiet)
            {
                DsChuaXepLich.Add($"{dk.MaLHP} — {dk.TenMon} ({dk.TongTinChi} TC)");
                continue;
            }

            int colIndex = thu - 2;
            var item = new ThoiKhoaBieuOClass
            {
                MaLHP = dk.MaLHP,
                TenMon = dk.TenMon,
                Phong = lhp.Phong,
                TenGiangVien = dk.TenGiangVien,
                SoTinChi = dk.TongTinChi,
                GiangDayOnline = lhp.GiangDayOnline,
                TietBatDau = tietBatDau,
                SoTiet = soTiet
            };

            Cols[colIndex].CacLop.Add(item);
        }

        int buoiCount = 0;
        foreach (var col in Cols)
        {
            var sorted = col.CacLop.OrderBy(c => c.TietBatDau).ToList();
            col.CacLop.Clear();
            col.CaSang.Clear();
            col.CaChieu.Clear();

            foreach (var c in sorted)
            {
                col.CacLop.Add(c);
                if (c.IsCaSang)
                    col.CaSang.Add(c);
                else
                    col.CaChieu.Add(c);
                buoiCount++;
            }
        }

        TongBuoiHocTuan = buoiCount;
        CoLopChuaXepLich = DsChuaXepLich.Count > 0;
    }

    [RelayCommand]
    private async Task SaoChepLichHocAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== THỜI KHÓA BIỂU HỌC KỲ ({TongSoLop} lớp • {TongTinChi} tín chỉ) ===");
        foreach (var col in Cols.Where(c => c.CacLop.Count > 0))
        {
            sb.AppendLine($"• {col.TenThu}:");
            foreach (var c in col.CacLop)
            {
                sb.AppendLine($"   - [{c.TietHienThi}] {c.TenMon} ({c.MaLHP}) | {c.PhongBadge} | GV: {c.TenGiangVien}");
            }
        }

        if (DsChuaXepLich.Count > 0)
        {
            sb.AppendLine("• Các lớp chưa xếp lịch cố định: " + string.Join(", ", DsChuaXepLich));
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow?.Clipboard != null)
        {
            await desktop.MainWindow.Clipboard.SetTextAsync(sb.ToString());
            CopyStatusToast = "✔ Đã sao chép Thời khóa biểu vào bộ nhớ tạm (dán ngay qua Zalo/Email)!";
        }
    }

    [RelayCommand]
    private void Close() => CloseAction?.Invoke();
}

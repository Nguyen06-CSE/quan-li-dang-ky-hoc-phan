using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.App.ViewModels;

public partial class LopHocPhanEditDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _maLHP = string.Empty;

    [ObservableProperty]
    private MonHoc? _selectedMonHoc;

    [ObservableProperty]
    private NguoiDungItem? _selectedGiangVien;

    [ObservableProperty]
    private string? _loaiHinhDT = "Chính quy";

    [ObservableProperty]
    private int? _siSoToiDa = 40;

    [ObservableProperty]
    private bool _giangDayOnline;

    // Lịch học — nhập tay vì file Excel import thật không có cột lịch học
    // (xem db/2026-10-01_them-lich-hoc-LopHocPhan.sql và FIX.md mục 20).
    [ObservableProperty]
    private ThuTrongTuanItem? _selectedThu;

    [ObservableProperty]
    private int? _tietBatDau;

    [ObservableProperty]
    private int? _soTiet;

    [ObservableProperty]
    private string? _phong;

    public ObservableCollection<ThuTrongTuanItem> DanhSachThu { get; } = new()
    {
        new ThuTrongTuanItem(null, "Chưa xếp lịch"),
        new ThuTrongTuanItem(2, "Thứ 2"),
        new ThuTrongTuanItem(3, "Thứ 3"),
        new ThuTrongTuanItem(4, "Thứ 4"),
        new ThuTrongTuanItem(5, "Thứ 5"),
        new ThuTrongTuanItem(6, "Thứ 6"),
        new ThuTrongTuanItem(7, "Thứ 7"),
        new ThuTrongTuanItem(8, "Chủ nhật"),
    };

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string? _errorMessage;

    public string MaHocKy { get; }

    public ObservableCollection<MonHoc> DanhSachMonHoc { get; } = new();
    public ObservableCollection<NguoiDungItem> DanhSachGiangVien { get; } = new();
    public ObservableCollection<string> DanhSachLoaiHinhDT { get; } = new() { "Chính quy", "Vừa học vừa làm", "Liên thông", "Từ xa" };

    public bool IsSuccess { get; private set; }

    public LopHocPhanEditDialogViewModel(
        string maHocKy,
        List<MonHoc> danhSachMonHoc,
        List<NguoiDung> danhSachGiangVien,
        LopHocPhan? existingLhp = null)
    {
        MaHocKy = maHocKy;

        foreach (var mon in danhSachMonHoc)
        {
            DanhSachMonHoc.Add(mon);
        }

        // Tùy chọn "Chưa phân công"
        DanhSachGiangVien.Add(new NguoiDungItem { MaNguoiDung = null, TenHienThi = "Chưa phân công" });
        foreach (var gv in danhSachGiangVien)
        {
            DanhSachGiangVien.Add(new NguoiDungItem { MaNguoiDung = gv.TenDangNhap, TenHienThi = $"{gv.HoTen} ({gv.TenDangNhap})" });
        }

        if (existingLhp != null)
        {
            IsEditMode = true;
            MaLHP = existingLhp.MaLHP;
            SelectedMonHoc = DanhSachMonHoc.FirstOrDefault(m => m.MaMon == existingLhp.MaMon);
            SelectedGiangVien = DanhSachGiangVien.FirstOrDefault(g => g.MaNguoiDung == existingLhp.MaGV) 
                                ?? DanhSachGiangVien.First();
            LoaiHinhDT = existingLhp.LoaiHinhDT ?? "Chính quy";
            SiSoToiDa = existingLhp.SiSoToiDa;
            GiangDayOnline = existingLhp.GiangDayOnline;
            SelectedThu = DanhSachThu.FirstOrDefault(t => t.GiaTri == existingLhp.Thu) ?? DanhSachThu.First();
            TietBatDau = existingLhp.TietBatDau;
            SoTiet = existingLhp.SoTiet;
            Phong = existingLhp.Phong;
        }
        else
        {
            IsEditMode = false;
            SelectedGiangVien = DanhSachGiangVien.First();
            SelectedThu = DanhSachThu.First();
        }
    }

    public Action? CloseAction { get; set; }

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = null;
        if (!IsEditMode && string.IsNullOrWhiteSpace(MaLHP))
        {
            ErrorMessage = "Mã lớp học phần không được để trống.";
            return;
        }

        if (SelectedMonHoc == null)
        {
            ErrorMessage = "Vui lòng chọn Môn học.";
            return;
        }

        if (SiSoToiDa.HasValue && SiSoToiDa.Value <= 0)
        {
            ErrorMessage = "Sĩ số tối đa (nếu nhập) phải lớn hơn 0.";
            return;
        }

        // Lịch học là tùy chọn (NULL = chưa xếp lịch), nhưng nếu nhập thì phải khớp ràng buộc
        // CHECK đã tạo trong db/2026-10-01_them-lich-hoc-LopHocPhan.sql.
        if (TietBatDau.HasValue && (TietBatDau.Value < 1 || TietBatDau.Value > 16))
        {
            ErrorMessage = "Tiết bắt đầu (nếu nhập) phải từ 1 đến 16.";
            return;
        }

        if (SoTiet.HasValue && (SoTiet.Value < 1 || SoTiet.Value > 10))
        {
            ErrorMessage = "Số tiết (nếu nhập) phải từ 1 đến 10.";
            return;
        }

        if (SelectedThu?.GiaTri != null && (!TietBatDau.HasValue || !SoTiet.HasValue))
        {
            ErrorMessage = "Đã chọn Thứ thì phải nhập đủ Tiết bắt đầu và Số tiết.";
            return;
        }

        IsSuccess = true;
        CloseAction?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        IsSuccess = false;
        CloseAction?.Invoke();
    }

    public LopHocPhan ToEntity()
    {
        return new LopHocPhan
        {
            MaLHP = MaLHP.Trim(),
            MaMon = SelectedMonHoc?.MaMon ?? string.Empty,
            MaHocKy = MaHocKy,
            MaGV = SelectedGiangVien?.MaNguoiDung,
            LoaiHinhDT = string.IsNullOrWhiteSpace(LoaiHinhDT) ? null : LoaiHinhDT.Trim(),
            SiSoToiDa = SiSoToiDa,
            GiangDayOnline = GiangDayOnline,
            Thu = SelectedThu?.GiaTri,
            TietBatDau = TietBatDau,
            SoTiet = SoTiet,
            Phong = string.IsNullOrWhiteSpace(Phong) ? null : Phong.Trim()
        };
    }
}

public class NguoiDungItem
{
    public string? MaNguoiDung { get; set; }
    public string TenHienThi { get; set; } = string.Empty;

    public override string ToString() => TenHienThi;
}

/// <summary>Mục chọn Thứ trong tuần cho ComboBox lịch học (GiaTri null = "Chưa xếp lịch").</summary>
public class ThuTrongTuanItem
{
    public int? GiaTri { get; }
    public string TenHienThi { get; }

    public ThuTrongTuanItem(int? giaTri, string tenHienThi)
    {
        GiaTri = giaTri;
        TenHienThi = tenHienThi;
    }

    public override string ToString() => TenHienThi;
}

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
        }
        else
        {
            IsEditMode = false;
            SelectedGiangVien = DanhSachGiangVien.First();
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
            GiangDayOnline = GiangDayOnline
        };
    }
}

public class NguoiDungItem
{
    public string? MaNguoiDung { get; set; }
    public string TenHienThi { get; set; } = string.Empty;

    public override string ToString() => TenHienThi;
}

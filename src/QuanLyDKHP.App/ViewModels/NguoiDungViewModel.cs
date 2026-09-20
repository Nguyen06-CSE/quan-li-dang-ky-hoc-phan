using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Enums;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class NguoiDungViewModel : ObservableObject
{
    private readonly INguoiDungService _nguoiDungService;

    public ObservableCollection<NguoiDung> DanhSachNguoiDung { get; } = new();

    public List<string> DanhSachRole { get; } = new()
    {
        UserRole.Admin.ToString(),
        UserRole.TroLyGiaoVu.ToString(),
        UserRole.GiaoVuBoMon.ToString(),
        UserRole.GiangVien.ToString()
    };

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    // --- State Dialog Thêm / Sửa ---
    [ObservableProperty]
    private bool _isThemSuaDialogOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private int _editId;

    [ObservableProperty]
    private string _tenDangNhap = string.Empty;

    [ObservableProperty]
    private string _hoTen = string.Empty;

    [ObservableProperty]
    private string _selectedRole = UserRole.Admin.ToString();

    [ObservableProperty]
    private string? _maGV;

    [ObservableProperty]
    private string _matKhauBanDau = string.Empty;

    [ObservableProperty]
    private bool _isRoleGiangVien;

    // --- State Dialog Đổi Mật Khẩu ---
    [ObservableProperty]
    private bool _isDoiMatKhauDialogOpen;

    [ObservableProperty]
    private int _doiMatKhauUserId;

    [ObservableProperty]
    private string _doiMatKhauUserName = string.Empty;

    [ObservableProperty]
    private string _matKhauCu = string.Empty;

    [ObservableProperty]
    private string _matKhauMoi = string.Empty;

    [ObservableProperty]
    private string _xacNhanMatKhau = string.Empty;

    public NguoiDungViewModel(INguoiDungService nguoiDungService)
    {
        _nguoiDungService = nguoiDungService;
        _ = LoadDanhSachAsync();
    }

    public NguoiDungViewModel()
    {
        _nguoiDungService = null!;
    }

    partial void OnSelectedRoleChanged(string value)
    {
        IsRoleGiangVien = value == UserRole.GiangVien.ToString();
    }

    [RelayCommand]
    public async Task LoadDanhSachAsync()
    {
        if (_nguoiDungService == null) return;
        try
        {
            IsLoading = true;
            var list = await _nguoiDungService.LayTatCaAsync();
            DanhSachNguoiDung.Clear();
            foreach (var item in list)
            {
                DanhSachNguoiDung.Add(item);
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void MoDialogThem()
    {
        IsEditMode = false;
        EditId = 0;
        TenDangNhap = string.Empty;
        HoTen = string.Empty;
        SelectedRole = UserRole.Admin.ToString();
        MaGV = string.Empty;
        MatKhauBanDau = string.Empty;
        IsThemSuaDialogOpen = true;
    }

    [RelayCommand]
    private void MoDialogSua(NguoiDung user)
    {
        if (user == null) return;
        IsEditMode = true;
        EditId = user.Id;
        TenDangNhap = user.TenDangNhap;
        HoTen = user.HoTen;
        SelectedRole = user.Role;
        MaGV = user.MaGV;
        MatKhauBanDau = string.Empty;
        IsThemSuaDialogOpen = true;
    }

    [RelayCommand]
    private void DongDialogThemSua()
    {
        IsThemSuaDialogOpen = false;
    }

    [RelayCommand]
    private async Task LuuThemSuaAsync()
    {
        if (_nguoiDungService == null) return;

        if (string.IsNullOrWhiteSpace(HoTen))
        {
            ShowMessage("Họ tên không được để trống.", true);
            return;
        }

        if (!IsEditMode)
        {
            if (string.IsNullOrWhiteSpace(TenDangNhap))
            {
                ShowMessage("Tên đăng nhập không được để trống.", true);
                return;
            }
            if (string.IsNullOrWhiteSpace(MatKhauBanDau))
            {
                ShowMessage("Mật khẩu ban đầu không được để trống.", true);
                return;
            }
        }

        try
        {
            IsLoading = true;
            if (IsEditMode)
            {
                var nd = new NguoiDung
                {
                    Id = EditId,
                    TenDangNhap = TenDangNhap,
                    HoTen = HoTen,
                    Role = SelectedRole,
                    MaGV = IsRoleGiangVien ? MaGV : null,
                    TrangThai = "HoatDong"
                };
                await _nguoiDungService.CapNhatAsync(nd);
                ShowMessage($"Đã cập nhật tài khoản '{TenDangNhap}' thành công.", false);
            }
            else
            {
                var nd = new NguoiDung
                {
                    TenDangNhap = TenDangNhap,
                    HoTen = HoTen,
                    Role = SelectedRole,
                    MaGV = IsRoleGiangVien ? MaGV : null,
                    TrangThai = "HoatDong"
                };
                await _nguoiDungService.ThemAsync(nd, MatKhauBanDau);
                ShowMessage($"Đã tạo mới tài khoản '{TenDangNhap}' thành công.", false);
            }

            IsThemSuaDialogOpen = false;
            await LoadDanhSachAsync();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void MoDialogDoiMatKhau(NguoiDung user)
    {
        if (user == null) return;
        DoiMatKhauUserId = user.Id;
        DoiMatKhauUserName = $"{user.TenDangNhap} ({user.HoTen})";
        MatKhauCu = string.Empty;
        MatKhauMoi = string.Empty;
        XacNhanMatKhau = string.Empty;
        IsDoiMatKhauDialogOpen = true;
    }

    [RelayCommand]
    private void DongDialogDoiMatKhau()
    {
        IsDoiMatKhauDialogOpen = false;
    }

    [RelayCommand]
    private async Task LuuDoiMatKhauAsync()
    {
        if (_nguoiDungService == null) return;

        if (string.IsNullOrWhiteSpace(MatKhauCu))
        {
            ShowMessage("Vui lòng nhập mật khẩu cũ.", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(MatKhauMoi))
        {
            ShowMessage("Mật khẩu mới không được để trống.", true);
            return;
        }

        if (MatKhauMoi != XacNhanMatKhau)
        {
            ShowMessage("Xác nhận mật khẩu không khớp.", true);
            return;
        }

        try
        {
            IsLoading = true;
            await _nguoiDungService.DoiMatKhauAsync(DoiMatKhauUserId, MatKhauCu, MatKhauMoi);
            ShowMessage($"Đã đổi mật khẩu cho tài khoản '{DoiMatKhauUserName}' thành công.", false);
            IsDoiMatKhauDialogOpen = false;
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleKhoaTaiKhoanAsync(NguoiDung user)
    {
        if (_nguoiDungService == null || user == null) return;

        try
        {
            IsLoading = true;
            string trangThaiMoi = user.TrangThai == "HoatDong" ? "DaKhoa" : "HoatDong";
            await _nguoiDungService.DoiTrangThaiAsync(user.Id, trangThaiMoi);

            string actionText = trangThaiMoi == "DaKhoa" ? "Khóa" : "Mở khóa";
            ShowMessage($"Đã {actionText} tài khoản '{user.TenDangNhap}' thành công.", false);

            await LoadDanhSachAsync();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowMessage(string msg, bool isError)
    {
        StatusMessage = msg;
        IsStatusError = isError;
    }
}

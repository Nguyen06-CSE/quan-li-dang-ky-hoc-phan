using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.App.ViewModels;

public partial class HocKyEditDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _maHocKy = string.Empty;

    [ObservableProperty]
    private string _tenHocKy = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _ngayBatDau = DateTimeOffset.Now;

    [ObservableProperty]
    private DateTimeOffset? _ngayKetThuc = DateTimeOffset.Now.AddMonths(4);

    [ObservableProperty]
    private bool _dangMo;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsSuccess { get; private set; }

    public HocKyEditDialogViewModel(HocKy? existingHocKy = null)
    {
        if (existingHocKy != null)
        {
            IsEditMode = true;
            MaHocKy = existingHocKy.MaHocKy;
            TenHocKy = existingHocKy.TenHocKy;
            NgayBatDau = existingHocKy.NgayBatDau.HasValue ? new DateTimeOffset(existingHocKy.NgayBatDau.Value) : null;
            NgayKetThuc = existingHocKy.NgayKetThuc.HasValue ? new DateTimeOffset(existingHocKy.NgayKetThuc.Value) : null;
            DangMo = existingHocKy.DangMo;
        }
        else
        {
            IsEditMode = false;
            DangMo = false;
        }
    }

    public Action? CloseAction { get; set; }

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = null;
        if (!IsEditMode && string.IsNullOrWhiteSpace(MaHocKy))
        {
            ErrorMessage = "Mã học kỳ không được để trống.";
            return;
        }

        if (string.IsNullOrWhiteSpace(TenHocKy))
        {
            ErrorMessage = "Tên học kỳ không được để trống.";
            return;
        }

        if (NgayBatDau.HasValue && NgayKetThuc.HasValue && NgayKetThuc.Value < NgayBatDau.Value)
        {
            ErrorMessage = "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.";
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

    public HocKy ToEntity()
    {
        return new HocKy
        {
            MaHocKy = MaHocKy.Trim(),
            TenHocKy = TenHocKy.Trim(),
            NgayBatDau = NgayBatDau?.DateTime,
            NgayKetThuc = NgayKetThuc?.DateTime,
            DangMo = DangMo
        };
    }
}

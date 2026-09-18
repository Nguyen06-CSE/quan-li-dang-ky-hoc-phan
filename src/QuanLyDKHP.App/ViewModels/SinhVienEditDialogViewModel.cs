using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.App.ViewModels;

public partial class SinhVienEditDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _maSV = string.Empty;

    [ObservableProperty]
    private string _hoTen = string.Empty;

    [ObservableProperty]
    private string? _lopSinhHoat;

    [ObservableProperty]
    private string? _khoaHoc;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsSuccess { get; private set; }

    public SinhVienEditDialogViewModel(SinhVien? existingSv = null)
    {
        if (existingSv != null)
        {
            IsEditMode = true;
            MaSV = existingSv.MaSV;
            HoTen = existingSv.HoTen;
            LopSinhHoat = existingSv.LopSinhHoat;
            KhoaHoc = existingSv.KhoaHoc;
        }
        else
        {
            IsEditMode = false;
        }
    }

    public System.Action? CloseAction { get; set; }

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = null;
        if (!IsEditMode && string.IsNullOrWhiteSpace(MaSV))
        {
            ErrorMessage = "Mã sinh viên không được để trống.";
            return;
        }

        if (string.IsNullOrWhiteSpace(HoTen))
        {
            ErrorMessage = "Họ tên không được để trống.";
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

    public SinhVien ToEntity()
    {
        return new SinhVien
        {
            MaSV = MaSV.Trim(),
            HoTen = HoTen.Trim(),
            LopSinhHoat = string.IsNullOrWhiteSpace(LopSinhHoat) ? null : LopSinhHoat.Trim(),
            KhoaHoc = string.IsNullOrWhiteSpace(KhoaHoc) ? null : KhoaHoc.Trim()
        };
    }
}

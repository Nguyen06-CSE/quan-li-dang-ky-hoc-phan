using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.App.ViewModels;

public partial class MonHocEditDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _maMon = string.Empty;

    [ObservableProperty]
    private string _tenMon = string.Empty;

    [ObservableProperty]
    private int _soTinChiLT;

    [ObservableProperty]
    private int _soTinChiTH;

    [ObservableProperty]
    private string? _bacDaoTao = "Đại học";

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsSuccess { get; private set; }

    public MonHocEditDialogViewModel(MonHoc? existingMon = null)
    {
        if (existingMon != null)
        {
            IsEditMode = true;
            MaMon = existingMon.MaMon;
            TenMon = existingMon.TenMon;
            SoTinChiLT = existingMon.SoTinChiLT;
            SoTinChiTH = existingMon.SoTinChiTH;
            BacDaoTao = existingMon.BacDaoTao ?? "Đại học";
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
        if (!IsEditMode && string.IsNullOrWhiteSpace(MaMon))
        {
            ErrorMessage = "Mã môn học không được để trống.";
            return;
        }

        if (string.IsNullOrWhiteSpace(TenMon))
        {
            ErrorMessage = "Tên môn học không được để trống.";
            return;
        }

        if (SoTinChiLT + SoTinChiTH <= 0)
        {
            ErrorMessage = "Tổng số tín chỉ (LT + TH) phải lớn hơn 0.";
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

    public MonHoc ToEntity()
    {
        return new MonHoc
        {
            MaMon = MaMon.Trim(),
            TenMon = TenMon.Trim(),
            SoTinChiLT = SoTinChiLT,
            SoTinChiTH = SoTinChiTH,
            BacDaoTao = string.IsNullOrWhiteSpace(BacDaoTao) ? null : BacDaoTao.Trim()
        };
    }
}

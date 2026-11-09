using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuanLyDKHP.Core.Dtos;

public class SinhVienDto : INotifyPropertyChanged
{
    public string MaSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? LopSinhHoat { get; set; }
    public string? KhoaHoc { get; set; }
    public int SoTinChiDangKy { get; set; }

    public bool DaDangKyTinChi => SoTinChiDangKy > 0;
    public bool ChuaDangKyTinChi => SoTinChiDangKy == 0;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


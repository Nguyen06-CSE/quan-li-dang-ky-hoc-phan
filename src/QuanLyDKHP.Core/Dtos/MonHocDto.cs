using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuanLyDKHP.Core.Dtos;

public class MonHocDto : INotifyPropertyChanged
{
    public string MaMon { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public int SoTinChiLT { get; set; }
    public int SoTinChiTH { get; set; }
    public int TongTinChi => SoTinChiLT + SoTinChiTH;
    public string? BacDaoTao { get; set; }
    public int SoLhpDangMo { get; set; }

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
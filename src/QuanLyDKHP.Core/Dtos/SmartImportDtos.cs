using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuanLyDKHP.Core.Dtos;

public class ColumnMappingDto
{
    public int MaSVCol { get; set; } = -1;
    public int HoTenCol { get; set; } = -1;
    public int HoDemCol { get; set; } = -1;
    public int TenCol { get; set; } = -1;
    public int LopSHCol { get; set; } = -1;
    public int KhoaHocCol { get; set; } = -1;
    public int MaMonCol { get; set; } = -1;
    public int TenMonCol { get; set; } = -1;
    public int MaLHPCol { get; set; } = -1;
    public int SoTCCol { get; set; } = -1;
    public int MaGVCol { get; set; } = -1;
    public int TenGVCol { get; set; } = -1;
    public bool HasHeader { get; set; } = true;
}

public class SinhVienImportPreviewDto : INotifyPropertyChanged
{
    private int _stt;
    private string _maSV = string.Empty;
    private string _hoTen = string.Empty;
    private string _lopSinhHoat = string.Empty;
    private string _khoaHoc = string.Empty;
    private string _maMon = string.Empty;
    private string _tenMon = string.Empty;
    private string _maLHP = string.Empty;
    private int _soTC = 3;
    private string _maGV = string.Empty;
    private string _tenGV = string.Empty;
    private string _bacDT = string.Empty;
    private string _loaiHinhDT = string.Empty;
    private string _hinhThucDK = string.Empty;
    private string _nguoiDK = string.Empty;
    private decimal? _diemSo;
    private string _diemChu = string.Empty;
    private decimal _soTienDaDong;
    private decimal? _soTienPhaiDong;
    private bool _giangDayOnline;
    private string _trangThaiXacThuc = "HopLe";
    private string _thongBaoLoi = string.Empty;
    private string? _canhBaoXungDot;
    private bool _isSelected = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Stt
    {
        get => _stt;
        set
        {
            if (SetField(ref _stt, value))
                OnPropertyChanged(nameof(DongSo));
        }
    }

    public int DongSo => Stt;

    public string MaSV
    {
        get => _maSV;
        set => SetField(ref _maSV, value ?? string.Empty);
    }

    public string HoTen
    {
        get => _hoTen;
        set => SetField(ref _hoTen, value ?? string.Empty);
    }

    public string LopSinhHoat
    {
        get => _lopSinhHoat;
        set => SetField(ref _lopSinhHoat, value ?? string.Empty);
    }

    public string KhoaHoc
    {
        get => _khoaHoc;
        set => SetField(ref _khoaHoc, value ?? string.Empty);
    }

    public string MaMon
    {
        get => _maMon;
        set => SetField(ref _maMon, value ?? string.Empty);
    }

    public string TenMon
    {
        get => _tenMon;
        set => SetField(ref _tenMon, value ?? string.Empty);
    }

    public string MaLHP
    {
        get => _maLHP;
        set => SetField(ref _maLHP, value ?? string.Empty);
    }

    public int SoTC
    {
        get => _soTC;
        set => SetField(ref _soTC, value);
    }

    public string MaGV
    {
        get => _maGV;
        set => SetField(ref _maGV, value ?? string.Empty);
    }

    public string TenGV
    {
        get => _tenGV;
        set => SetField(ref _tenGV, value ?? string.Empty);
    }

    public string BacDT
    {
        get => _bacDT;
        set => SetField(ref _bacDT, value ?? string.Empty);
    }

    public string LoaiHinhDT
    {
        get => _loaiHinhDT;
        set => SetField(ref _loaiHinhDT, value ?? string.Empty);
    }

    public string HinhThucDK
    {
        get => _hinhThucDK;
        set => SetField(ref _hinhThucDK, value ?? string.Empty);
    }

    public string NguoiDK
    {
        get => _nguoiDK;
        set => SetField(ref _nguoiDK, value ?? string.Empty);
    }

    public decimal? DiemSo
    {
        get => _diemSo;
        set => SetField(ref _diemSo, value);
    }

    public string DiemChu
    {
        get => _diemChu;
        set => SetField(ref _diemChu, value ?? string.Empty);
    }

    public decimal SoTienDaDong
    {
        get => _soTienDaDong;
        set => SetField(ref _soTienDaDong, value);
    }

    public decimal? SoTienPhaiDong
    {
        get => _soTienPhaiDong;
        set => SetField(ref _soTienPhaiDong, value);
    }

    public bool GiangDayOnline
    {
        get => _giangDayOnline;
        set => SetField(ref _giangDayOnline, value);
    }

    /// <summary>
    /// "HopLe", "DaCoTrongLop", "Loi", "DaTonTaiHeThong"
    /// </summary>
    public string TrangThaiXacThuc
    {
        get => _trangThaiXacThuc;
        set
        {
            if (SetField(ref _trangThaiXacThuc, value ?? "HopLe"))
            {
                OnPropertyChanged(nameof(IsHopLe));
                OnPropertyChanged(nameof(IsDaCo));
                OnPropertyChanged(nameof(IsLoi));
                OnPropertyChanged(nameof(DaTonTaiTrongLop));
            }
        }
    }

    public string ThongBaoLoi
    {
        get => _thongBaoLoi;
        set
        {
            if (SetField(ref _thongBaoLoi, value ?? string.Empty))
                OnPropertyChanged(nameof(TrangThaiLoi));
        }
    }

    public string? CanhBaoXungDot
    {
        get => _canhBaoXungDot;
        set
        {
            if (SetField(ref _canhBaoXungDot, value))
            {
                OnPropertyChanged(nameof(HasXungDotLich));
                OnPropertyChanged(nameof(HasTrungMon));
                OnPropertyChanged(nameof(TrangThaiLoi));
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public bool IsHopLe => TrangThaiXacThuc == "HopLe";
    public bool IsDaCo => TrangThaiXacThuc == "DaCoTrongLop";
    public bool IsLoi => TrangThaiXacThuc == "Loi";
    public bool DaTonTaiTrongLop => TrangThaiXacThuc == "DaCoTrongLop";
    public bool HasXungDotLich => !string.IsNullOrEmpty(CanhBaoXungDot) && CanhBaoXungDot.Contains("Trùng lịch");
    public bool HasTrungMon => !string.IsNullOrEmpty(CanhBaoXungDot) && (CanhBaoXungDot.Contains("cùng môn") || CanhBaoXungDot.Contains("Trùng môn"));
    public string TrangThaiLoi => !string.IsNullOrEmpty(ThongBaoLoi) ? ThongBaoLoi : CanhBaoXungDot ?? string.Empty;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class ThongKeCoCauDto
{
    public string Nhan { get; set; } = string.Empty;
    public string TenNhom => Nhan;
    public int SoLuong { get; set; }
    public double PhanTram { get; set; }
    public string HienThiPhanTram => $"{PhanTram:F1}%";
}

public class ClassInsightsDto
{
    public string MaLHP { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public int SiSoHienTai { get; set; }
    public int SiSoThemVao { get; set; }
    public int SiSoDuKien => SiSoHienTai + SiSoThemVao;
    public int SiSoSauKhiNap => SiSoDuKien;
    public int SiSoToiDa { get; set; }
    public string SiSoHienThi => $"{SiSoHienTai} / {(SiSoToiDa > 0 ? SiSoToiDa.ToString() : "∞")}";
    public double TiLeLapDayDuKien => SiSoToiDa > 0 ? System.Math.Min(100, (double)SiSoDuKien / SiSoToiDa * 100.0) : 0;
    public bool VuotSiSo => SiSoToiDa > 0 && SiSoDuKien > SiSoToiDa;
    public int SoLuongVuot => VuotSiSo ? (SiSoDuKien - SiSoToiDa) : 0;
    
    public int SoLuongHopLe { get; set; }
    public int SoLuongDaCo { get; set; }
    public int SoLuongLoi { get; set; }

    public bool CoXungDotLich => DanhSachCanhBao.Exists(c => c.Contains("Trùng lịch"));
    public bool CoTrungMon => DanhSachCanhBao.Exists(c => c.Contains("cùng môn") || c.Contains("Trùng môn"));

    public List<ThongKeCoCauDto> CoCauLop { get; set; } = new();
    public List<ThongKeCoCauDto> CoCauKhoa { get; set; } = new();
    public List<string> DanhSachCanhBao { get; set; } = new();
}

public class KetQuaSmartEnrollDto
{
    public string MaLHP { get; set; } = string.Empty;
    public int TongSoLuong { get; set; }
    public int ThanhCong { get; set; }
    public int BoQua { get; set; }
    public int Loi { get; set; }
    public int SoSinhVienMoiTao { get; set; }
    public int SoMonHocMoiTao { get; set; }
    public int SoLopHocPhanMoiTao { get; set; }
    public int SoCapNhat { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public List<string> ChiTietLoi { get; set; } = new();
}

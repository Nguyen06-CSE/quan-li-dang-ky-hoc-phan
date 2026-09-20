using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class CauHinhViewModel : ObservableObject
{
    private readonly ICauHinhService _cauHinhService;
    private readonly IHocPhiService _hocPhiService;
    private readonly IHocKyService _hocKyService;
    private readonly ISinhVienService _sinhVienService;

    [ObservableProperty]
    private int _soTinChiToiThieu = 10;

    [ObservableProperty]
    private int _soTinChiToiDa = 25;

    [ObservableProperty]
    private decimal _donGiaTinChiLT = 500000m;

    [ObservableProperty]
    private decimal _donGiaTinChiTH = 700000m;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    [ObservableProperty]
    private bool _hienThiHoiTinhLaiHocPhi;

    public CauHinhViewModel(
        ICauHinhService cauHinhService,
        IHocPhiService hocPhiService,
        IHocKyService hocKyService,
        ISinhVienService sinhVienService)
    {
        _cauHinhService = cauHinhService;
        _hocPhiService = hocPhiService;
        _hocKyService = hocKyService;
        _sinhVienService = sinhVienService;

        _ = LoadCauHinhAsync();
    }

    public CauHinhViewModel()
    {
        _cauHinhService = null!;
        _hocPhiService = null!;
        _hocKyService = null!;
        _sinhVienService = null!;
    }

    [RelayCommand]
    public async Task LoadCauHinhAsync()
    {
        if (_cauHinhService == null) return;
        try
        {
            IsLoading = true;
            SoTinChiToiThieu = await _cauHinhService.GetInt("SoTinChiToiThieu");
            SoTinChiToiDa = await _cauHinhService.GetInt("SoTinChiToiDa");
            DonGiaTinChiLT = await _cauHinhService.GetDecimal("DonGiaTinChiLT");
            DonGiaTinChiTH = await _cauHinhService.GetDecimal("DonGiaTinChiTH");
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi tải cấu hình: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LuuCauHinhAsync()
    {
        if (_cauHinhService == null) return;

        // 1. Validate
        if (SoTinChiToiThieu < 1)
        {
            ShowMessage("Số tín chỉ tối thiểu phải lớn hơn hoặc bằng 1.", true);
            return;
        }

        if (SoTinChiToiDa <= SoTinChiToiThieu)
        {
            ShowMessage("Số tín chỉ tối đa phải lớn hơn số tín chỉ tối thiểu.", true);
            return;
        }

        if (DonGiaTinChiLT < 0 || DonGiaTinChiTH < 0)
        {
            ShowMessage("Đơn giá tín chỉ không được âm.", true);
            return;
        }

        try
        {
            IsLoading = true;
            var dict = new Dictionary<string, string>
            {
                ["SoTinChiToiThieu"] = SoTinChiToiThieu.ToString(),
                ["SoTinChiToiDa"] = SoTinChiToiDa.ToString(),
                ["DonGiaTinChiLT"] = DonGiaTinChiLT.ToString("F0"),
                ["DonGiaTinChiTH"] = DonGiaTinChiTH.ToString("F0")
            };

            await _cauHinhService.CapNhatAsync(dict);
            ShowMessage("Lưu cấu hình hệ thống thành công!", false);

            // Mở prompt hỏi tính lại học phí
            HienThiHoiTinhLaiHocPhi = true;
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi lưu cấu hình: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DongYTinhLaiHocPhiAsync()
    {
        HienThiHoiTinhLaiHocPhi = false;
        if (_hocKyService == null || _sinhVienService == null || _hocPhiService == null) return;

        try
        {
            IsLoading = true;
            var tatCaHk = await _hocKyService.LayTatCaAsync();
            var hkMacDinh = tatCaHk.FirstOrDefault(h => h.DangMo) ?? tatCaHk.FirstOrDefault();

            if (hkMacDinh == null)
            {
                ShowMessage("Không tìm thấy học kỳ hiện hành để tính lại học phí.", true);
                return;
            }

            var tatCaSv = await _sinhVienService.LayDanhSachAsync(null, null, null);
            int count = 0;
            foreach (var sv in tatCaSv)
            {
                await _hocPhiService.TinhLaiHocPhiAsync(sv.MaSV, hkMacDinh.MaHocKy);
                count++;
            }

            ShowMessage($"Đã tính lại học phí thành công cho {count} sinh viên trong học kỳ {hkMacDinh.TenHocKy}.", false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi khi tính lại học phí: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void TuChoiTinhLaiHocPhi()
    {
        HienThiHoiTinhLaiHocPhi = false;
    }

    private void ShowMessage(string msg, bool isError)
    {
        StatusMessage = msg;
        IsStatusError = isError;
    }
}

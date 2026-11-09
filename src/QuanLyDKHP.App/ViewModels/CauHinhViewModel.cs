using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class CauHinhViewModel : ObservableObject
{
    private readonly ICauHinhService _cauHinhService;
    private readonly IHocPhiService _hocPhiService;
    private readonly IHocKyService _hocKyService;
    private readonly ISinhVienService _sinhVienService;
    private readonly IBackupService _backupService;
    private readonly IMemoryCacheStore _memoryCacheStore;

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

    // --- Thuộc tính quản lý Sao lưu & Phục hồi ---
    [ObservableProperty]
    private BackupDatabaseStatusDto? _backupStatus;

    [ObservableProperty]
    private ObservableCollection<BackupCommitInfoDto> _backupHistory = new();

    [ObservableProperty]
    private BackupCommitInfoDto? _selectedBackupCommit;

    [ObservableProperty]
    private string _ghiChuSaoLuu = string.Empty;

    [ObservableProperty]
    private bool _isBackingUp;

    [ObservableProperty]
    private bool _hienThiXacNhanKhoiPhuc;

    [ObservableProperty]
    private string _thongBaoKhoiPhuc = string.Empty;

    public CauHinhViewModel(
        ICauHinhService cauHinhService,
        IHocPhiService hocPhiService,
        IHocKyService hocKyService,
        ISinhVienService sinhVienService,
        IBackupService backupService,
        IMemoryCacheStore memoryCacheStore)
    {
        _cauHinhService = cauHinhService;
        _hocPhiService = hocPhiService;
        _hocKyService = hocKyService;
        _sinhVienService = sinhVienService;
        _backupService = backupService;
        _memoryCacheStore = memoryCacheStore;

        _ = LoadCauHinhAsync();
        _ = LoadBackupInfoAsync();
    }

    public CauHinhViewModel()
    {
        _cauHinhService = null!;
        _hocPhiService = null!;
        _hocKyService = null!;
        _sinhVienService = null!;
        _backupService = null!;
        _memoryCacheStore = null!;
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
    public async Task LoadBackupInfoAsync()
    {
        if (_backupService == null) return;
        try
        {
            BackupStatus = await _backupService.GetStatusAsync();
            var commits = await _backupService.GetCommitHistoryAsync(30);
            BackupHistory.Clear();
            foreach (var c in commits)
            {
                BackupHistory.Add(c);
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi tải lịch sử sao lưu: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task TaoBanSaoLuuGitAsync()
    {
        if (_backupService == null) return;
        try
        {
            IsBackingUp = true;
            var result = await _backupService.CreateLocalGitBackupAsync("Manual", GhiChuSaoLuu, skipIfNoChanges: false);
            GhiChuSaoLuu = string.Empty;
            await LoadBackupInfoAsync();
            ShowMessage($"Tạo bản sao lưu Git thành công! (Phiên bản: {result?.ShortSha})", false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi tạo bản sao lưu: {ex.Message}", true);
        }
        finally
        {
            IsBackingUp = false;
        }
    }

    [RelayCommand]
    public async Task DongBoLenDbHostAsync()
    {
        if (_backupService == null) return;
        try
        {
            IsBackingUp = true;
            var (count, msg) = await _backupService.PushDeltaToRemoteHostAsync();
            await LoadBackupInfoAsync();
            ShowMessage(msg, false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi đồng bộ lên DB Host: {ex.Message}", true);
        }
        finally
        {
            IsBackingUp = false;
        }
    }

    [RelayCommand]
    public void MoXacNhanKhoiPhuc(BackupCommitInfoDto commit)
    {
        SelectedBackupCommit = commit;
        ThongBaoKhoiPhuc = $"Bạn có chắc chắn muốn khôi phục toàn bộ dữ liệu CSDL về phiên bản ngày {commit.CommittedAtLocal:dd/MM/yyyy HH:mm} (Mã: {commit.ShortSha}) không?\n\nHệ thống sẽ tự động tạo một điểm lưu an toàn trước khi khôi phục.";
        HienThiXacNhanKhoiPhuc = true;
    }

    [RelayCommand]
    public void HuyKhoiPhuc()
    {
        HienThiXacNhanKhoiPhuc = false;
        SelectedBackupCommit = null;
    }

    [RelayCommand]
    public async Task XacNhanKhoiPhucAsync()
    {
        if (_backupService == null || SelectedBackupCommit == null) return;
        HienThiXacNhanKhoiPhuc = false;

        try
        {
            IsBackingUp = true;
            await _backupService.RestoreFromGitCommitAsync(SelectedBackupCommit.CommitSha);
            await _memoryCacheStore.InitializeAsync(forceReload: true);
            await LoadCauHinhAsync();
            await LoadBackupInfoAsync();
            ShowMessage($"Đã khôi phục thành công CSDL về phiên bản {SelectedBackupCommit.ShortSha}!", false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi khi khôi phục dữ liệu: {ex.Message}", true);
        }
        finally
        {
            IsBackingUp = false;
            SelectedBackupCommit = null;
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
            await _hocPhiService.TinhLaiHocPhiHangLoatAsync(tatCaSv.Select(s => s.MaSV), hkMacDinh.MaHocKy);

            ShowMessage($"Đã tính lại học phí thành công cho {tatCaSv.Count} sinh viên trong học kỳ {hkMacDinh.TenHocKy}.", false);
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

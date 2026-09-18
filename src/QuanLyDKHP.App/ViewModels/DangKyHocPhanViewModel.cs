using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.App.Dtos;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class DangKyHocPhanViewModel : ObservableObject
{
    private readonly IDangKyHocPhanService _dangKyService;
    private readonly IHocKyService _hocKyService;
    private readonly ISinhVienService _sinhVienService;
    private readonly IMonHocService _monHocService;
    private readonly ILopHocPhanService _lopHocPhanService;
    private readonly ICauHinhService _cauHinhService;
    private readonly INguoiDungRepository _nguoiDungRepository;
    private readonly Timer _searchDebounceTimer;

    [ObservableProperty]
    private HocKy? _hocKyHienHanh;

    [ObservableProperty]
    private string? _tuKhoaSinhVien;

    [ObservableProperty]
    private bool _isGoiYOpen;

    [ObservableProperty]
    private SinhVienDto? _sinhVienDangChon;

    [ObservableProperty]
    private int _tongTinChiHienTai;

    [ObservableProperty]
    private int _tinChiToiDa = 25;

    [ObservableProperty]
    private int _tinChiToiThieu = 10;

    [ObservableProperty]
    private double _progressBarValue;

    [ObservableProperty]
    private string _progressBarColor = "#ACD26B"; // BrushPrimary

    [ObservableProperty]
    private bool _isChuaDatToiThieu;

    [ObservableProperty]
    private string _canhBaoToiThieuText = string.Empty;

    [ObservableProperty]
    private MonHoc? _monDangChon;

    [ObservableProperty]
    private LopHocPhanDisplayDto? _lHPDangChon;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    public ObservableCollection<SinhVienDto> GoiYSinhVien { get; } = new();
    public ObservableCollection<DangKyHocPhanDisplayDto> DsDaDangKy { get; } = new();
    public ObservableCollection<MonHoc> DsMonHoc { get; } = new();
    public ObservableCollection<LopHocPhanDisplayDto> DsLHPTheoMon { get; } = new();

    public Func<string, Task<bool>>? ShowConfirmFunc { get; set; }
    public Func<string, Task<bool>>? ShowWarningConfirmFunc { get; set; }

    public DangKyHocPhanViewModel(
        IDangKyHocPhanService dangKyService,
        IHocKyService hocKyService,
        ISinhVienService sinhVienService,
        IMonHocService monHocService,
        ILopHocPhanService lopHocPhanService,
        ICauHinhService cauHinhService,
        INguoiDungRepository nguoiDungRepository)
    {
        _dangKyService = dangKyService;
        _hocKyService = hocKyService;
        _sinhVienService = sinhVienService;
        _monHocService = monHocService;
        _lopHocPhanService = lopHocPhanService;
        _cauHinhService = cauHinhService;
        _nguoiDungRepository = nguoiDungRepository;

        _searchDebounceTimer = new Timer(300) { AutoReset = false };
        _searchDebounceTimer.Elapsed += (s, e) => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(TimKiemSinhVienGoiYAsync);

        _ = InitDataAsync();
    }

    public DangKyHocPhanViewModel()
    {
        _dangKyService = null!;
        _hocKyService = null!;
        _sinhVienService = null!;
        _monHocService = null!;
        _lopHocPhanService = null!;
        _cauHinhService = null!;
        _nguoiDungRepository = null!;
        _searchDebounceTimer = new Timer(300);
    }

    private async Task InitDataAsync()
    {
        if (_hocKyService == null) return;
        try
        {
            await LoadHocKyHienHanhAsync();
            await LoadCauHinhTinChiAsync();
            await LoadDanhSachMonHocAsync();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    private async Task LoadHocKyHienHanhAsync()
    {
        var hks = await _hocKyService.LayTatCaAsync();
        HocKyHienHanh = hks.FirstOrDefault(hk => hk.DangMo) ?? hks.FirstOrDefault();
    }

    private async Task LoadCauHinhTinChiAsync()
    {
        TinChiToiDa = await _cauHinhService.GetInt("SoTinChiToiDa");
        if (TinChiToiDa <= 0) TinChiToiDa = 25;

        TinChiToiThieu = await _cauHinhService.GetInt("SoTinChiToiThieu");
        if (TinChiToiThieu <= 0) TinChiToiThieu = 10;
    }

    private async Task LoadDanhSachMonHocAsync()
    {
        var mons = await _monHocService.LayDanhSachAsync(null, "TenMon");
        DsMonHoc.Clear();
        foreach (var m in mons)
        {
            DsMonHoc.Add(m);
        }
    }

    partial void OnTuKhoaSinhVienChanged(string? value)
    {
        _searchDebounceTimer.Stop();
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < 2)
        {
            GoiYSinhVien.Clear();
            IsGoiYOpen = false;
            return;
        }
        _searchDebounceTimer.Start();
    }

    private async Task TimKiemSinhVienGoiYAsync()
    {
        if (string.IsNullOrWhiteSpace(TuKhoaSinhVien) || TuKhoaSinhVien.Trim().Length < 2)
        {
            GoiYSinhVien.Clear();
            IsGoiYOpen = false;
            return;
        }

        try
        {
            var result = await _sinhVienService.TimKiemAsync(TuKhoaSinhVien.Trim(), null, null, 1, 15);
            GoiYSinhVien.Clear();
            foreach (var sv in result.Items)
            {
                GoiYSinhVien.Add(sv);
            }
            IsGoiYOpen = GoiYSinhVien.Count > 0;
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    [RelayCommand]
    private async Task ChonSinhVienAsync(SinhVienDto sv)
    {
        if (sv == null) return;
        SinhVienDangChon = sv;
        IsGoiYOpen = false;
        TuKhoaSinhVien = $"{sv.MaSV} - {sv.HoTen}";

        await LoadDangKyCuaSinhVienAsync();
    }

    public async Task LoadDangKyCuaSinhVienAsync()
    {
        if (SinhVienDangChon == null || HocKyHienHanh == null)
        {
            DsDaDangKy.Clear();
            TongTinChiHienTai = 0;
            CapNhatThanhTinChi();
            return;
        }

        try
        {
            var list = await _dangKyService.LayDanhSachDangKyAsync(SinhVienDangChon.MaSV, HocKyHienHanh.MaHocKy);
            var giangViens = await _nguoiDungRepository.LayGiangVienAsync();
            var gvDict = giangViens.ToDictionary(g => g.TenDangNhap, g => g.HoTen);

            DsDaDangKy.Clear();
            int tongTC = 0;

            foreach (var dk in list)
            {
                string gvName = "Chưa phân công";
                if (dk.LopHocPhan != null && !string.IsNullOrEmpty(dk.LopHocPhan.MaGV) && gvDict.TryGetValue(dk.LopHocPhan.MaGV, out var name))
                {
                    gvName = $"{name} ({dk.LopHocPhan.MaGV})";
                }

                int tcLT = dk.LopHocPhan?.MonHoc?.SoTinChiLT ?? 0;
                int tcTH = dk.LopHocPhan?.MonHoc?.SoTinChiTH ?? 0;

                DsDaDangKy.Add(new DangKyHocPhanDisplayDto
                {
                    Id = dk.Id,
                    MaSV = dk.MaSV,
                    MaLHP = dk.MaLHP,
                    MaMon = dk.LopHocPhan?.MaMon ?? string.Empty,
                    TenMon = dk.LopHocPhan?.MonHoc?.TenMon ?? dk.MaLHP,
                    SoTinChiLT = tcLT,
                    SoTinChiTH = tcTH,
                    TenGiangVien = gvName,
                    HinhThucDK = dk.HinhThucDK,
                    NgayDK = dk.NgayDK,
                    TrangThai = dk.TrangThai,
                    SoTienPhaiDong = dk.SoTienPhaiDong,
                    SoTienDaDong = dk.SoTienDaDong,
                    Entity = dk
                });

                if (dk.TrangThai == "DangHoc")
                {
                    tongTC += (tcLT + tcTH);
                }
            }

            TongTinChiHienTai = tongTC;
            CapNhatThanhTinChi();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    private void CapNhatThanhTinChi()
    {
        ProgressBarValue = TinChiToiDa > 0 ? Math.Min(100.0, (double)TongTinChiHienTai / TinChiToiDa * 100.0) : 0;

        // Màu: bình thường BrushPrimary (#ACD26B), khi >= 90% -> BrushWarning (#E0A83C), khi = 100% hoặc vượt -> BrushDanger (#D64541)
        if (TinChiToiDa > 0 && TongTinChiHienTai >= TinChiToiDa)
        {
            ProgressBarColor = "#D64541"; // BrushDanger
        }
        else if (TinChiToiDa > 0 && (double)TongTinChiHienTai / TinChiToiDa >= 0.9)
        {
            ProgressBarColor = "#E0A83C"; // BrushWarning
        }
        else
        {
            ProgressBarColor = "#ACD26B"; // BrushPrimary
        }

        // Nếu tổng TC < tối thiểu -> hiện dòng cảnh báo nhỏ màu BrushWarning "Chưa đạt số tín chỉ tối thiểu (X/Y)"
        if (SinhVienDangChon != null && TongTinChiHienTai < TinChiToiThieu)
        {
            IsChuaDatToiThieu = true;
            CanhBaoToiThieuText = $"Chưa đạt số tín chỉ tối thiểu ({TongTinChiHienTai}/{TinChiToiThieu})";
        }
        else
        {
            IsChuaDatToiThieu = false;
            CanhBaoToiThieuText = string.Empty;
        }
    }

    partial void OnMonDangChonChanged(MonHoc? value)
    {
        _ = LoadLHPTheoMonAsync(value);
    }

    private async Task LoadLHPTheoMonAsync(MonHoc? mon)
    {
        DsLHPTheoMon.Clear();
        LHPDangChon = null;

        if (mon == null || HocKyHienHanh == null) return;

        try
        {
            var lhps = await _lopHocPhanService.LayTheoHocKyAsync(HocKyHienHanh.MaHocKy, null, mon.MaMon);
            var giangViens = await _nguoiDungRepository.LayGiangVienAsync();
            var gvDict = giangViens.ToDictionary(g => g.TenDangNhap, g => g.HoTen);

            foreach (var lhp in lhps)
            {
                int siSoRealtime = await _lopHocPhanService.DemSiSoDangKyAsync(lhp.MaLHP);
                // Ẩn các LHP đã đầy sĩ số
                if (lhp.SiSoToiDa.HasValue && lhp.SiSoToiDa.Value > 0 && siSoRealtime >= lhp.SiSoToiDa.Value)
                {
                    continue;
                }

                string gvName = !string.IsNullOrEmpty(lhp.MaGV) && gvDict.TryGetValue(lhp.MaGV, out var name)
                    ? $"{name} ({lhp.MaGV})"
                    : "Chưa phân công";

                DsLHPTheoMon.Add(new LopHocPhanDisplayDto
                {
                    MaLHP = lhp.MaLHP,
                    MaMon = lhp.MaMon,
                    TenMon = lhp.MonHoc?.TenMon ?? mon.TenMon,
                    MaGV = lhp.MaGV,
                    TenGiangVien = gvName,
                    LoaiHinhDT = lhp.LoaiHinhDT,
                    SiSoToiDa = lhp.SiSoToiDa,
                    SiSoDangKy = siSoRealtime,
                    GiangDayOnline = lhp.GiangDayOnline,
                    Entity = lhp
                });
            }

            if (DsLHPTheoMon.Count > 0)
            {
                LHPDangChon = DsLHPTheoMon.First();
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    [RelayCommand]
    private async Task DangKyAsync()
    {
        if (SinhVienDangChon == null)
        {
            ShowMessage("Vui lòng chọn sinh viên cần đăng ký học phần.", true);
            return;
        }

        if (LHPDangChon == null)
        {
            ShowMessage("Vui lòng chọn lớp học phần cần đăng ký.", true);
            return;
        }

        try
        {
            var result = await _dangKyService.DangKyAsync(SinhVienDangChon.MaSV, LHPDangChon.MaLHP, boQuaCanhBaoTrungMon: false);

            if (result.IsCanhBaoTrungMon)
            {
                if (ShowWarningConfirmFunc != null)
                {
                    bool confirm = await ShowWarningConfirmFunc(result.Message);
                    if (confirm)
                    {
                        var confirmResult = await _dangKyService.DangKyAsync(SinhVienDangChon.MaSV, LHPDangChon.MaLHP, boQuaCanhBaoTrungMon: true);
                        if (!confirmResult.Success)
                        {
                            ShowMessage(confirmResult.Message, true);
                            return;
                        }
                    }
                    else
                    {
                        return; // Người dùng hủy đăng ký
                    }
                }
                else
                {
                    ShowMessage(result.Message, true);
                    return;
                }
            }
            else if (!result.Success)
            {
                ShowMessage(result.Message, true);
                return;
            }

            ShowMessage("Đăng ký thành công.", false);
            await LoadDangKyCuaSinhVienAsync();
            await LoadLHPTheoMonAsync(MonDangChon);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    [RelayCommand]
    private async Task HuyDangKyAsync(DangKyHocPhanDisplayDto dto)
    {
        if (dto == null || SinhVienDangChon == null) return;

        if (ShowConfirmFunc != null)
        {
            bool confirm = await ShowConfirmFunc($"Bạn có chắc chắn muốn hủy đăng ký lớp học phần {dto.MaLHP} - môn {dto.TenMon} của sinh viên {SinhVienDangChon.HoTen}?");
            if (!confirm) return;
        }

        try
        {
            await _dangKyService.HuyDangKyAsync(SinhVienDangChon.MaSV, dto.MaLHP);
            ShowMessage($"Đã hủy đăng ký lớp học phần {dto.MaLHP} thành công.", false);
            await LoadDangKyCuaSinhVienAsync();
            await LoadLHPTheoMonAsync(MonDangChon);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    private void ShowMessage(string msg, bool isError)
    {
        StatusMessage = msg;
        IsStatusError = isError;
    }
}

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Enums;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IBaoCaoService _baoCaoService;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty]
    private int _tongSinhVien;

    [ObservableProperty]
    private int _tongMonHoc;

    [ObservableProperty]
    private int _tongLopHocPhan;

    [ObservableProperty]
    private int _tongLuotDangKy;

    [ObservableProperty]
    private HocKy? _hocKyDangXem;

    [ObservableProperty]
    private bool _isGiangVien;

    [ObservableProperty]
    private bool _isQuanLy;

    public ObservableCollection<HocKy> DsHocKy { get; } = new();
    public ObservableCollection<GiangVienLhpDto> DsLhpGiangVien { get; } = new();

    /// <summary>
    /// Event yêu cầu điều hướng lối tắt tới màn hình khác (truyền tên chức năng / tag).
    /// </summary>
    public event EventHandler<string>? NavigationRequested;

    public DashboardViewModel(IBaoCaoService baoCaoService, ICurrentUserService currentUserService)
    {
        _baoCaoService = baoCaoService;
        _currentUserService = currentUserService;

        // Check role
        var currentUser = _currentUserService.CurrentUser;
        IsGiangVien = currentUser != null && currentUser.Role == UserRole.GiangVien;
        IsQuanLy = !IsGiangVien;

        _ = LoadDataAsync();
    }

    public DashboardViewModel()
    {
        _baoCaoService = null!;
        _currentUserService = null!;
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var dsHk = await _baoCaoService.GetDanhSachHocKyAsync();
            DsHocKy.Clear();
            foreach (var hk in dsHk)
            {
                DsHocKy.Add(hk);
            }

            HocKyDangXem = await _baoCaoService.GetHocKyMacDinhAsync();
            await UpdateStatsAsync();
        }
        catch
        {
            // Tránh crash nếu CSDL trống
        }
    }

    partial void OnHocKyDangXemChanged(HocKy? value)
    {
        _ = UpdateStatsAsync();
    }

    private async Task UpdateStatsAsync()
    {
        if (_baoCaoService == null) return;

        string? maHk = HocKyDangXem?.MaHocKy;
        var stats = await _baoCaoService.GetDashboardStatsAsync(maHk);

        TongSinhVien = stats.TongSinhVien;
        TongMonHoc = stats.TongMonHoc;
        TongLopHocPhan = stats.TongLopHocPhan;
        TongLuotDangKy = stats.TongLuotDangKy;

        if (IsGiangVien && _currentUserService?.CurrentUser?.MaGV != null)
        {
            var lhpList = await _baoCaoService.GetLhpGiangVienAsync(_currentUserService.CurrentUser.MaGV, maHk);
            DsLhpGiangVien.Clear();
            foreach (var item in lhpList)
            {
                DsLhpGiangVien.Add(item);
            }
        }
    }

    [RelayCommand]
    private void NavigateShortcut(string destination)
    {
        NavigationRequested?.Invoke(this, destination);
    }
}

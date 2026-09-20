using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

public class NguoiDungService : INguoiDungService
{
    private readonly INguoiDungRepository _nguoiDungRepository;
    private readonly ICurrentUserService _currentUserService;

    public NguoiDungService(INguoiDungRepository nguoiDungRepository, ICurrentUserService currentUserService)
    {
        _nguoiDungRepository = nguoiDungRepository;
        _currentUserService = currentUserService;
    }

    public Task<List<NguoiDung>> LayTatCaAsync()
    {
        return _nguoiDungRepository.LayTatCaAsync();
    }

    public async Task ThemAsync(NguoiDung nd, string matKhauBanDau)
    {
        if (string.IsNullOrWhiteSpace(nd.TenDangNhap))
            throw new ArgumentException("Tên đăng nhập không được để trống.");

        if (string.IsNullOrWhiteSpace(matKhauBanDau))
            throw new ArgumentException("Mật khẩu ban đầu không được để trống.");

        if (string.IsNullOrWhiteSpace(nd.HoTen))
            throw new ArgumentException("Họ tên không được để trống.");

        var existing = await _nguoiDungRepository.GetByTenDangNhapAsync(nd.TenDangNhap.Trim());
        if (existing != null)
            throw new InvalidOperationException($"Tên đăng nhập '{nd.TenDangNhap}' đã tồn tại trong hệ thống.");

        nd.TenDangNhap = nd.TenDangNhap.Trim();
        nd.HoTen = nd.HoTen.Trim();
        nd.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(matKhauBanDau);
        nd.TrangThai = string.IsNullOrWhiteSpace(nd.TrangThai) ? "HoatDong" : nd.TrangThai;

        await _nguoiDungRepository.ThemAsync(nd);
    }

    public async Task CapNhatAsync(NguoiDung nd)
    {
        if (string.IsNullOrWhiteSpace(nd.HoTen))
            throw new ArgumentException("Họ tên không được để trống.");

        var user = await _nguoiDungRepository.GetByIdAsync(nd.Id);
        if (user == null)
            throw new KeyNotFoundException($"Không tìm thấy người dùng có ID = {nd.Id}.");

        user.HoTen = nd.HoTen.Trim();
        user.Role = nd.Role;
        user.MaGV = nd.Role == "GiangVien" ? nd.MaGV : null;
        user.TrangThai = nd.TrangThai;

        await _nguoiDungRepository.CapNhatAsync(user);
    }

    public async Task DoiMatKhauAsync(int id, string matKhauCu, string matKhauMoi)
    {
        if (string.IsNullOrWhiteSpace(matKhauCu))
            throw new ArgumentException("Mật khẩu cũ không được để trống.");

        if (string.IsNullOrWhiteSpace(matKhauMoi))
            throw new ArgumentException("Mật khẩu mới không được để trống.");

        var user = await _nguoiDungRepository.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"Không tìm thấy người dùng có ID = {id}.");

        // Kiểm tra mật khẩu cũ
        bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(matKhauCu, user.MatKhauHash);
        if (!isOldPasswordValid)
        {
            throw new InvalidOperationException("Mật khẩu cũ không chính xác.");
        }

        user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(matKhauMoi);
        await _nguoiDungRepository.CapNhatAsync(user);
    }

    public async Task DoiTrangThaiAsync(int id, string trangThaiMoi)
    {
        var user = await _nguoiDungRepository.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"Không tìm thấy người dùng có ID = {id}.");

        // Ràng buộc bảo vệ: Không cho Admin tự khóa chính tài khoản đang đăng nhập
        var currentUser = _currentUserService.CurrentUser;
        if (currentUser != null && currentUser.Id == id && trangThaiMoi == "DaKhoa")
        {
            throw new InvalidOperationException("Không thể tự khóa tài khoản Admin đang đăng nhập.");
        }

        user.TrangThai = trangThaiMoi;
        await _nguoiDungRepository.CapNhatAsync(user);
    }
}

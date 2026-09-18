using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Services;
using Xunit;

namespace QuanLyDKHP.Tests;

public class SinhVienServiceTests
{
    private class MockSinhVienRepository : ISinhVienRepository
    {
        public bool HasDangKyHocPhan = false;
        public bool SvExists = false;
        public SinhVien? SavedSv = null;

        public Task<PagedResult<SinhVienDto>> TimKiemAsync(string? tuKhoa, string? lop, string? khoaHoc, int page, int pageSize)
        {
            return Task.FromResult(new PagedResult<SinhVienDto>());
        }

        public Task<List<string>> GetDanhSachLopSinhHoatAsync() => Task.FromResult(new List<string>());
        public Task<List<string>> GetDanhSachKhoaHocAsync() => Task.FromResult(new List<string>());
        public Task<SinhVien?> GetByIdAsync(string maSV) => Task.FromResult(SavedSv);
        public Task ThemAsync(SinhVien sv) { SavedSv = sv; return Task.CompletedTask; }
        public Task CapNhatAsync(SinhVien sv) { SavedSv = sv; return Task.CompletedTask; }
        public Task XoaMoiAsync(string maSV) { return Task.CompletedTask; }
        public Task<bool> TonTaiAsync(string maSV) => Task.FromResult(SvExists);
        public Task<bool> CoDangKyHocPhanDangHocAsync(string maSV) => Task.FromResult(HasDangKyHocPhan);
    }

    [Fact]
    public async Task XoaSinhVien_CoDangKyHocPhan_ThrowsException()
    {
        var mockRepo = new MockSinhVienRepository { HasDangKyHocPhan = true };
        var service = new SinhVienService(mockRepo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.XoaAsync("SV001"));
        Assert.Contains("Không thể xóa: sinh viên đang có đăng ký học phần", ex.Message);
    }
}

public class MonHocServiceTests
{
    private class MockMonHocRepository : IMonHocRepository
    {
        public bool HasLhp = false;
        public MonHoc? SavedMon = null;

        public Task<List<MonHoc>> LayDanhSachAsync(string? tuKhoa) => Task.FromResult(new List<MonHoc>());
        public Task<MonHoc?> GetByIdAsync(string maMon) => Task.FromResult(SavedMon);
        public Task ThemAsync(MonHoc mon) { SavedMon = mon; return Task.CompletedTask; }
        public Task CapNhatAsync(MonHoc mon) { SavedMon = mon; return Task.CompletedTask; }
        public Task XoaMoiAsync(string maMon) { return Task.CompletedTask; }
        public Task<bool> TonTaiAsync(string maMon) => Task.FromResult(false);
        public Task<bool> CoLopHocPhanAsync(string maMon) => Task.FromResult(HasLhp);
        public Task<int> DemSoLhpDangMoAsync(string maMon, string? maHocKy) => Task.FromResult(0);
    }

    [Fact]
    public async Task ThemMonHoc_TinChiBangZero_ThrowsException()
    {
        var mockRepo = new MockMonHocRepository();
        var service = new MonHocService(mockRepo);

        var mon = new MonHoc { MaMon = "MH001", TenMon = "Môn test", SoTinChiLT = 0, SoTinChiTH = 0 };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ThemAsync(mon));
        Assert.Contains("Tổng số tín chỉ (LT + TH) phải lớn hơn 0", ex.Message);
    }

    [Fact]
    public async Task XoaMonHoc_CoLopHocPhan_ThrowsException()
    {
        var mockRepo = new MockMonHocRepository { HasLhp = true };
        var service = new MonHocService(mockRepo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.XoaAsync("MH001"));
        Assert.Contains("Không thể xóa: môn học đã có lớp học phần", ex.Message);
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

public class MonHocService : IMonHocService
{
    private readonly IMonHocRepository _monHocRepository;
    private static readonly CultureInfo ViCulture = new CultureInfo("vi-VN");

    public MonHocService(IMonHocRepository monHocRepository)
    {
        _monHocRepository = monHocRepository;
    }

    public async Task<List<MonHocDto>> LayDanhSachDtoAsync(string? tuKhoa, string sortBy = "TenMon")
    {
        var rawList = await _monHocRepository.LayDanhSachAsync(tuKhoa);
        
        var dtoList = new List<MonHocDto>();
        foreach (var mon in rawList)
        {
            int countLhp = await _monHocRepository.DemSoLhpDangMoAsync(mon.MaMon, null);
            dtoList.Add(new MonHocDto
            {
                MaMon = mon.MaMon,
                TenMon = mon.TenMon,
                SoTinChiLT = mon.SoTinChiLT,
                SoTinChiTH = mon.SoTinChiTH,
                BacDaoTao = mon.BacDaoTao,
                SoLhpDangMo = countLhp
            });
        }

        // Sap xep tieng Viet
        if (sortBy == "TenMonZToA")
        {
            dtoList = dtoList.OrderByDescending(m => m.TenMon, StringComparer.Create(ViCulture, false)).ToList();
        }
        else if (sortBy == "MaMon")
        {
            dtoList = dtoList.OrderBy(m => m.MaMon).ToList();
        }
        else // TenMon A->Z mac dinh
        {
            dtoList = dtoList.OrderBy(m => m.TenMon, StringComparer.Create(ViCulture, false)).ToList();
        }

        return dtoList;
    }

    public async Task<List<MonHoc>> LayDanhSachAsync(string? tuKhoa, string sortBy = "TenMon")
    {
        var rawList = await _monHocRepository.LayDanhSachAsync(tuKhoa);

        if (sortBy == "TenMonZToA")
        {
            return rawList.OrderByDescending(m => m.TenMon, StringComparer.Create(ViCulture, false)).ToList();
        }
        if (sortBy == "MaMon")
        {
            return rawList.OrderBy(m => m.MaMon).ToList();
        }
        return rawList.OrderBy(m => m.TenMon, StringComparer.Create(ViCulture, false)).ToList();
    }

    public async Task ThemAsync(MonHoc mon)
    {
        if (string.IsNullOrWhiteSpace(mon.MaMon))
            throw new InvalidOperationException("Mã môn học không được để trống.");

        if (string.IsNullOrWhiteSpace(mon.TenMon))
            throw new InvalidOperationException("Tên môn học không được để trống.");

        if (mon.SoTinChiLT + mon.SoTinChiTH <= 0)
            throw new InvalidOperationException("Tổng số tín chỉ (LT + TH) phải lớn hơn 0.");

        if (await _monHocRepository.TonTaiAsync(mon.MaMon))
            throw new InvalidOperationException($"Mã môn học '{mon.MaMon}' đã tồn tại trong hệ thống.");

        await _monHocRepository.ThemAsync(mon);
    }

    public async Task CapNhatAsync(MonHoc mon)
    {
        if (string.IsNullOrWhiteSpace(mon.TenMon))
            throw new InvalidOperationException("Tên môn học không được để trống.");

        if (mon.SoTinChiLT + mon.SoTinChiTH <= 0)
            throw new InvalidOperationException("Tổng số tín chỉ (LT + TH) phải lớn hơn 0.");

        var existing = await _monHocRepository.GetByIdAsync(mon.MaMon);
        if (existing == null)
            throw new InvalidOperationException($"Không tìm thấy môn học có mã '{mon.MaMon}'.");

        existing.TenMon = mon.TenMon;
        existing.SoTinChiLT = mon.SoTinChiLT;
        existing.SoTinChiTH = mon.SoTinChiTH;
        existing.BacDaoTao = mon.BacDaoTao;

        await _monHocRepository.CapNhatAsync(existing);
    }

    public async Task XoaAsync(string maMon)
    {
        if (await _monHocRepository.CoLopHocPhanAsync(maMon))
        {
            throw new InvalidOperationException("Không thể xóa: môn học đã có lớp học phần.");
        }

        await _monHocRepository.XoaMoiAsync(maMon);
    }

    public Task<bool> TonTaiAsync(string maMon)
    {
        return _monHocRepository.TonTaiAsync(maMon);
    }
}

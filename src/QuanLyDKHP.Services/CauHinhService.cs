using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

public class CauHinhService : ICauHinhService
{
    private readonly ICauHinhRepository _cauHinhRepository;

    public CauHinhService(ICauHinhRepository cauHinhRepository)
    {
        _cauHinhRepository = cauHinhRepository;
    }

    public Task<Dictionary<string, string>> LayTatCaAsync()
    {
        return _cauHinhRepository.LayTatCaAsync();
    }

    public Task CapNhatAsync(Dictionary<string, string> cauHinhMoi)
    {
        return _cauHinhRepository.CapNhatAsync(cauHinhMoi);
    }

    public Task<int> GetInt(string key)
    {
        int defaultVal = key switch
        {
            "SoTinChiToiThieu" => 10,
            "SoTinChiToiDa" => 25,
            _ => 0
        };
        return _cauHinhRepository.LayGiaTriIntAsync(key, defaultVal);
    }

    public Task<decimal> GetDecimal(string key)
    {
        decimal defaultVal = key switch
        {
            "DonGiaTinChiLT" => 500000m,
            "DonGiaTinChiTH" => 700000m,
            _ => 0m
        };
        return _cauHinhRepository.LayGiaTriDecimalAsync(key, defaultVal);
    }
}

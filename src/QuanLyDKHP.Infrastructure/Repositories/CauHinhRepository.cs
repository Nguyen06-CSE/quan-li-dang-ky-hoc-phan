using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Repositories;

public class CauHinhRepository : ICauHinhRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CauHinhRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Dictionary<string, string>> LayTatCaAsync()
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.CauHinhHeThongs
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Key, c => c.Value);
    }

    public async Task<string?> LayGiaTriAsync(string key)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        var config = await _context.CauHinhHeThongs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == key);
        return config?.Value;
    }

    public async Task<int> LayGiaTriIntAsync(string key, int defaultValue)
    {
        var val = await LayGiaTriAsync(key);
        if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }
        return defaultValue;
    }

    public async Task<decimal> LayGiaTriDecimalAsync(string key, decimal defaultValue)
    {
        var val = await LayGiaTriAsync(key);
        if (decimal.TryParse(val, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }
        return defaultValue;
    }

    public async Task CapNhatAsync(Dictionary<string, string> cauHinhMoi)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        foreach (var kvp in cauHinhMoi)
        {
            var item = await _context.CauHinhHeThongs.FirstOrDefaultAsync(c => c.Key == kvp.Key);
            if (item != null)
            {
                item.Value = kvp.Value;
                item.NgayCapNhat = DateTime.UtcNow;
            }
            else
            {
                _context.CauHinhHeThongs.Add(new CauHinhHeThong
                {
                    Key = kvp.Key,
                    Value = kvp.Value,
                    NgayTao = DateTime.UtcNow
                });
            }
        }
        await _context.SaveChangesAsync();
    }
}

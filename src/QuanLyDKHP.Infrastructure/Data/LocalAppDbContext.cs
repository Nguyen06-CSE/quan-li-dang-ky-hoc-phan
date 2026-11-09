using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace QuanLyDKHP.Infrastructure.Data;

public class LocalAppDbContext : AppDbContext
{
    public LocalAppDbContext(DbContextOptions<LocalAppDbContext> options) : base(options)
    {
    }

    public static string GetDatabasePath() => GetPrimarySqlitePath();

    public async Task CheckAndRecreateIfMissingTablesAsync()
    {
        await EnsureTablesAndSeedAsync(this);
    }
}

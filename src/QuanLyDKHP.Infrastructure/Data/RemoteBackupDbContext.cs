using Microsoft.EntityFrameworkCore;

namespace QuanLyDKHP.Infrastructure.Data;

public class RemoteBackupDbContext : AppDbContext
{
    public RemoteBackupDbContext(DbContextOptions<RemoteBackupDbContext> options) : base(options)
    {
    }
}

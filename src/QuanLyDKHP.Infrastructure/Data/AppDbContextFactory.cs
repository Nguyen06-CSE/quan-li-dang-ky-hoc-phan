using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuanLyDKHP.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        // Design-time connection string (Neon PostgreSQL)
        builder.UseNpgsql("Host=ep-hidden-math-awrpifwn-pooler.c-12.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_VPln5gFhfEw4;SslMode=Require");

        return new AppDbContext(builder.Options);
    }
}

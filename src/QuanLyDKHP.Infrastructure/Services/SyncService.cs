using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Services;

/// <summary>
/// Chỉ còn thao tác ghi SQLite local. Không seed/pull/đồng bộ từ DB host.
/// </summary>
public class SyncService : ISyncService
{
    private readonly IDbContextFactory<LocalAppDbContext> _localFactory;

    public SyncService(IDbContextFactory<LocalAppDbContext> localFactory)
    {
        _localFactory = localFactory;
    }

    public async Task UpsertLocalEntityAsync<TEntity>(TEntity entity) where TEntity : class
    {
        await using var local = await _localFactory.CreateDbContextAsync();
        var keyValues = local.Model.FindEntityType(typeof(TEntity))?
            .FindPrimaryKey()?.Properties
            .Select(p => p.PropertyInfo?.GetValue(entity))
            .ToArray();

        if (keyValues != null && keyValues.All(k => k != null))
        {
            var existing = await local.Set<TEntity>().FindAsync(keyValues);
            if (existing != null)
            {
                local.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                await local.Set<TEntity>().AddAsync(entity);
            }
        }
        else
        {
            await local.Set<TEntity>().AddAsync(entity);
        }

        await local.SaveChangesAsync();
    }

    public async Task RemoveLocalEntityAsync<TEntity>(params object[] keyValues) where TEntity : class
    {
        await using var local = await _localFactory.CreateDbContextAsync();
        var existing = await local.Set<TEntity>().FindAsync(keyValues);
        if (existing != null)
        {
            // Soft delete if supports ISoftDeleteEntity
            if (existing is ISoftDeleteEntity softDelete)
            {
                softDelete.IsDeleted = true;
            }
            else
            {
                local.Set<TEntity>().Remove(existing);
            }
            await local.SaveChangesAsync();
        }
    }
}

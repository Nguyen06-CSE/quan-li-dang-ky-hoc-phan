using System;
using System.Threading.Tasks;

namespace QuanLyDKHP.Core.Interfaces;

public interface ISyncService
{
    Task<bool> HasLocalDataAsync();
    Task InitialSeedAsync(IProgress<(double Percent, string Status)>? progress = null);
    Task SyncDeltaAsync();
    Task ForceFullRefreshAsync(IProgress<(double Percent, string Status)>? progress = null);
    Task UpsertLocalEntityAsync<TEntity>(TEntity entity) where TEntity : class;
    Task RemoveLocalEntityAsync<TEntity>(params object[] keyValues) where TEntity : class;
}

using System;
using System.Threading.Tasks;

namespace QuanLyDKHP.Core.Interfaces;

public interface ISyncService
{
    Task UpsertLocalEntityAsync<TEntity>(TEntity entity) where TEntity : class;
    Task RemoveLocalEntityAsync<TEntity>(params object[] keyValues) where TEntity : class;
}

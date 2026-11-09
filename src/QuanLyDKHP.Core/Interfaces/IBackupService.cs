using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;

namespace QuanLyDKHP.Core.Interfaces;

public interface IBackupService
{
    Task<BackupDatabaseStatusDto> GetStatusAsync();
    Task<IReadOnlyList<BackupCommitInfoDto>> GetCommitHistoryAsync(int maxCount = 50);
    Task<BackupCommitInfoDto?> CreateLocalGitBackupAsync(string backupType = "Manual", string? customNote = null, bool skipIfNoChanges = false);
    Task EnsureDailyBackupAsync();
    Task RestoreFromGitCommitAsync(string commitSha, IProgress<(double Percent, string Status)>? progress = null);
    Task<(int PushedRecords, string Message)> PushDeltaToRemoteHostAsync(IProgress<(double Percent, string Status)>? progress = null);
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;

namespace QuanLyDKHP.Core.Interfaces;

public interface IBackupService
{
    Task<BackupDatabaseStatusDto> GetStatusAsync();
    Task<IReadOnlyList<BackupCommitInfoDto>> GetCommitHistoryAsync(int maxCount = 50);
    Task<BackupCommitDetailDto> GetCommitDetailAsync(string commitSha);
    Task<BackupCommitInfoDto?> CreateLocalGitBackupAsync(string backupType = "Manual", string? customNote = null, bool skipIfNoChanges = false);
    Task EnsureDailyBackupAsync();
    Task RestoreFromGitCommitAsync(string commitSha, IProgress<(double Percent, string Status)>? progress = null);
    Task<byte[]> ExportCommitToArchiveBytesAsync(string commitSha);
    Task<BackupArchiveManifestDto> InspectAndValidateBackupArchiveAsync(Stream archiveStream);
    Task ImportFromBackupArchiveAsync(Stream archiveStream, IProgress<(double Percent, string Status)>? progress = null);
    Task<(int PushedRecords, string Message)> PushDeltaToRemoteHostAsync(IProgress<(double Percent, string Status)>? progress = null);
}

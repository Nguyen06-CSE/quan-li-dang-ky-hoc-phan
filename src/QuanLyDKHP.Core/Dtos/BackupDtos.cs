using System;

namespace QuanLyDKHP.Core.Dtos;

public class BackupCommitInfoDto
{
    public string CommitSha { get; set; } = string.Empty;
    public string ShortSha => CommitSha.Length >= 7 ? CommitSha[..7] : CommitSha;
    public DateTime CommittedAtLocal { get; set; }
    public string BackupType { get; set; } = "Manual"; // "Auto-Daily", "Manual", "Pre-Restore", "Post-Import"
    public string Message { get; set; } = string.Empty;
    public string ChangedTablesSummary { get; set; } = string.Empty;
}

public class BackupDatabaseStatusDto
{
    public string SqliteFilePath { get; set; } = string.Empty;
    public long DatabaseSizeBytes { get; set; }
    public string FormattedSize => DatabaseSizeBytes switch
    {
        < 1024 => $"{DatabaseSizeBytes} B",
        < 1024 * 1024 => $"{DatabaseSizeBytes / 1024.0:F1} KB",
        _ => $"{DatabaseSizeBytes / (1024.0 * 1024.0):F2} MB"
    };
    public bool WalModeEnabled { get; set; }
    public int TotalCommits { get; set; }
    public DateTime? LastLocalBackupAt { get; set; }
    public DateTime? LastRemoteSyncAt { get; set; }
    public bool IsRemoteHostConfigured { get; set; }
}

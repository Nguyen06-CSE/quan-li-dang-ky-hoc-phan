using System;
using System.Collections.Generic;

namespace QuanLyDKHP.Core.Dtos;

public class BackupCommitInfoDto
{
    public string CommitSha { get; set; } = string.Empty;
    public string ShortSha => CommitSha.Length >= 7 ? CommitSha[..7] : CommitSha;
    public DateTime CommittedAtLocal { get; set; }
    public string BackupType { get; set; } = "Manual"; // "Auto-Daily", "Manual", "Pre-Restore", "Pre-Import", "Imported", "Post-Import"
    public string Message { get; set; } = string.Empty;
    public string ChangedTablesSummary { get; set; } = string.Empty;
}

public class BackupTableDiffDto
{
    public string TableName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int LinesAdded { get; set; }
    public int LinesDeleted { get; set; }
    public int TotalRecordsInSnapshot { get; set; }
    public bool HasChanges => LinesAdded > 0 || LinesDeleted > 0;
    public string ChangeBadge => HasChanges ? $"+{LinesAdded} / -{LinesDeleted}" : "Không đổi";
}

public class BackupCommitDetailDto
{
    public string CommitSha { get; set; } = string.Empty;
    public string ShortSha => CommitSha.Length >= 7 ? CommitSha[..7] : CommitSha;
    public DateTime CommittedAtLocal { get; set; }
    public string BackupType { get; set; } = string.Empty;
    public string FullMessage { get; set; } = string.Empty;
    public List<BackupTableDiffDto> TableStats { get; set; } = new();
    public List<string> ReadableChanges { get; set; } = new();
}

public class BackupArchiveManifestDto
{
    public const string ExpectedFormatSignature = "QuanLyDKHP-Backup-v1";
    public string FormatSignature { get; set; } = ExpectedFormatSignature;
    public string SourceCommitSha { get; set; } = string.Empty;
    public DateTime CreatedAtLocal { get; set; }
    public string BackupType { get; set; } = "Manual";
    public string Note { get; set; } = string.Empty;
    public Dictionary<string, int> TableRecordCounts { get; set; } = new();
    public Dictionary<string, string> TableSha256Checksums { get; set; } = new();
}

public class BackupDatabaseStatusDto
{
    public string SqliteFilePath { get; set; } = string.Empty;
    public long DatabaseSizeBytes { get; set; }
    public string FormattedSize => DatabaseSizeBytes switch
    {
        < 1024 => $"{DatabaseSizeBytes} B",
        < 1024 * 1024 => $"{(DatabaseSizeBytes / 1024.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} KB",
        _ => $"{(DatabaseSizeBytes / (1024.0 * 1024.0)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} MB"
    };
    public bool WalModeEnabled { get; set; }
    public int TotalCommits { get; set; }
    public DateTime? LastLocalBackupAt { get; set; }
    public DateTime? LastRemoteSyncAt { get; set; }
    public bool IsRemoteHostConfigured { get; set; }
}

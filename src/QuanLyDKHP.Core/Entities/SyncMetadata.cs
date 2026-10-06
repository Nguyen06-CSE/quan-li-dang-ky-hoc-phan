using System;

namespace QuanLyDKHP.Core.Entities;

public class SyncMetadata
{
    public string TableName { get; set; } = string.Empty;
    public DateTime LastSyncUtc { get; set; }
    public int RecordCount { get; set; }
}

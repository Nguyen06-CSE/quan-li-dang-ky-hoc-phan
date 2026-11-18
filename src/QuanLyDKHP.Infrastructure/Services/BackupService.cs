using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;
using System.Diagnostics;
using Npgsql;

namespace QuanLyDKHP.Infrastructure.Services;

public class BackupService : IBackupService
{
    private static readonly (string TableName, string DisplayName)[] ManagedTables =
    {
        ("NguoiDung", "Tài khoản người dùng"),
        ("CauHinhHeThong", "Cấu hình hệ thống"),
        ("HocKy", "Học kỳ"),
        ("MonHoc", "Môn học"),
        ("SinhVien", "Sinh viên"),
        ("LopHocPhan", "Lớp học phần"),
        ("DangKyHocPhan", "Đăng ký học phần"),
        ("HocPhiHocKy", "Học phí học kỳ")
    };

    private readonly IDbContextFactory<AppDbContext> _localFactory;
    private readonly IBackupConfigStore? _configStore;
    private readonly string _gitDir;
    private readonly string _tablesDir;
    private readonly string _snapshotPath;
    private readonly string _snapshotsDir;

    public BackupService(
        IDbContextFactory<AppDbContext> localFactory,
        IBackupConfigStore? configStore = null,
        string? customBackupDir = null)
    {
        _localFactory = localFactory;
        _configStore = configStore;

        var baseBackupDir = customBackupDir ?? Path.Combine(
            Path.GetDirectoryName(AppDbContext.GetPrimarySqlitePath()) ?? AppContext.BaseDirectory,
            "Backups");
        _gitDir = Path.Combine(baseBackupDir, "GitRepo");
        _tablesDir = Path.Combine(_gitDir, "tables");
        _snapshotPath = Path.Combine(baseBackupDir, "latest_snapshot.db");
        _snapshotsDir = Path.Combine(baseBackupDir, "Snapshots");

        Directory.CreateDirectory(_tablesDir);
    }

    public async Task<BackupDatabaseStatusDto> GetStatusAsync()
    {
        var localPath = AppDbContext.GetPrimarySqlitePath();
        long size = File.Exists(localPath) ? new FileInfo(localPath).Length : 0;

        int totalCommits = 0;
        DateTime? lastBackup = null;

        if (Repository.IsValid(_gitDir))
        {
            using var repo = new Repository(_gitDir);
            var lastCommit = repo.Commits.FirstOrDefault();
            if (lastCommit != null)
            {
                totalCommits = repo.Commits.Count();
                lastBackup = lastCommit.Author.When.LocalDateTime;
            }
        }

        DateTime? lastRemoteSync = null;
        try
        {
            await using var local = await _localFactory.CreateDbContextAsync();
            if (await local.Database.CanConnectAsync())
            {
                var meta = await local.SyncMetadatas
                    .Where(s => s.TableName == "RemotePush")
                    .FirstOrDefaultAsync();
                if (meta != null && meta.LastSyncUtc > DateTime.MinValue)
                {
                    lastRemoteSync = meta.LastSyncUtc.ToLocalTime();
                }
            }
        }
        catch
        {
            // Ignore if metadata table not initialized yet
        }

        return new BackupDatabaseStatusDto
        {
            SqliteFilePath = localPath,
            DatabaseSizeBytes = size,
            WalModeEnabled = true,
            TotalCommits = totalCommits,
            LastLocalBackupAt = lastBackup,
            LastRemoteSyncAt = lastRemoteSync,
            IsRemoteHostConfigured = !string.IsNullOrWhiteSpace(_configStore?.GetConnectionString())
        };
    }

    public string BackupConfigFilePath => _configStore?.ConfigFilePath ?? string.Empty;

    public string? GetBackupConnectionString() => _configStore?.GetConnectionString();

    public Task SaveBackupConnectionStringAsync(string connectionString)
    {
        if (_configStore == null)
            throw new InvalidOperationException("Không có nơi lưu cấu hình backup trên máy này.");

        return _configStore.SaveConnectionStringAsync(connectionString);
    }

    public Task ClearBackupConnectionStringAsync()
    {
        return _configStore?.ClearAsync() ?? Task.CompletedTask;
    }

    public async Task<(bool Success, string Message)> TestRemoteConnectionAsync(
    string? connectionString = null)
{
    try
    {
        var remote = CreateRemoteDbContext(connectionString);
        if (remote == null)
            return (false, "Chưa có chuỗi kết nối PostgreSQL.");

        await using (remote)
        {
            var connected = await remote.Database.CanConnectAsync();

            Console.Error.WriteLine(
                $"[Backup DB] CanConnectAsync={connected}; " +
                $"Provider={remote.Database.ProviderName}");

            return connected
                ? (true, "Kết nối PostgreSQL thành công.")
                : (false, "Không thể kết nối tới PostgreSQL. Xem log [Backup DB] để biết chi tiết.");
        }
    }
    catch (Exception ex)
    {
        var sqlState = ex is PostgresException pg ? pg.SqlState : "n/a";
        var innerType = ex.InnerException?.GetType().Name ?? "none";

        // Không ghi ex.ToString() hoặc connection string để tránh lộ thông tin kết nối.
        Console.Error.WriteLine(
            $"[Backup DB] Kết nối thất bại; " +
            $"Exception={ex.GetType().Name}; SqlState={sqlState}; " +
            $"InnerException={innerType}; Message={ex.Message}");

        return (false,
            $"Không thể kết nối tới PostgreSQL ({ex.GetType().Name}, SQLSTATE {sqlState}). Xem Output log.");
    }
}

    public async Task<RemoteBackupPreviewDto> GetRemoteBackupPreviewAsync()
    {
        await using var remote = CreateRemoteDbContext(null)
            ?? throw new InvalidOperationException("Chưa cấu hình chuỗi kết nối PostgreSQL backup.");

        if (!await remote.Database.CanConnectAsync())
            throw new InvalidOperationException("Không thể kết nối tới PostgreSQL. Vui lòng kiểm tra mạng và thông tin kết nối.");

        var preview = new RemoteBackupPreviewDto
        {
            Host = remote.Database.GetDbConnection().DataSource ?? string.Empty,
            Database = remote.Database.GetDbConnection().Database ?? string.Empty,
            CapturedAtLocal = DateTime.Now
        };

        preview.TableRecordCounts[nameof(NguoiDung)] = await remote.NguoiDungs.CountAsync();
        preview.TableRecordCounts[nameof(CauHinhHeThong)] = await remote.CauHinhHeThongs.CountAsync();
        preview.TableRecordCounts[nameof(HocKy)] = await remote.HocKys.CountAsync();
        preview.TableRecordCounts[nameof(MonHoc)] = await remote.MonHocs.CountAsync();
        preview.TableRecordCounts[nameof(SinhVien)] = await remote.SinhViens.CountAsync();
        preview.TableRecordCounts[nameof(LopHocPhan)] = await remote.LopHocPhans.CountAsync();
        preview.TableRecordCounts[nameof(DangKyHocPhan)] = await remote.DangKyHocPhans.CountAsync();
        preview.TableRecordCounts[nameof(HocPhiHocKy)] = await remote.HocPhiHocKys.CountAsync();

        return preview;
    }

    public Task<IReadOnlyList<BackupCommitInfoDto>> GetCommitHistoryAsync(int maxCount = 50)
    {
        var result = new List<BackupCommitInfoDto>();
        if (!Repository.IsValid(_gitDir)) return Task.FromResult<IReadOnlyList<BackupCommitInfoDto>>(result);

        using var repo = new Repository(_gitDir);
        foreach (var c in repo.Commits.Take(maxCount))
        {
            var msg = c.MessageShort ?? string.Empty;
            string bType = ParseBackupType(msg);
            var lines = (c.Message ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var diffSummary = lines.Length > 1 ? lines[^1] : msg;

            result.Add(new BackupCommitInfoDto
            {
                CommitSha = c.Sha,
                CommittedAtLocal = c.Author.When.LocalDateTime,
                BackupType = bType,
                Message = c.Message?.Trim() ?? string.Empty,
                ChangedTablesSummary = diffSummary
            });
        }

        return Task.FromResult<IReadOnlyList<BackupCommitInfoDto>>(result);
    }

    public Task<BackupCommitDetailDto> GetCommitDetailAsync(string commitSha)
    {
        if (!Repository.IsValid(_gitDir))
            throw new InvalidOperationException("Kho lưu trữ sao lưu Git chưa được khởi tạo.");

        using var repo = new Repository(_gitDir);
        var commit = repo.Lookup<Commit>(commitSha)
            ?? throw new ArgumentException($"Không tìm thấy bản sao lưu với mã SHA: {commitSha}");

        var parentTree = commit.Parents.FirstOrDefault()?.Tree;
        var patch = repo.Diff.Compare<Patch>(parentTree, commit.Tree);
        var tablesTree = commit["tables"]?.Target as Tree;

        var detail = new BackupCommitDetailDto
        {
            CommitSha = commit.Sha,
            CommittedAtLocal = commit.Author.When.LocalDateTime,
            BackupType = ParseBackupType(commit.MessageShort ?? string.Empty),
            FullMessage = commit.Message?.Trim() ?? string.Empty
        };

        foreach (var (tableName, displayName) in ManagedTables)
        {
            var fileName = $"{tableName}.jsonl";
            var patchEntry = patch[$"tables/{fileName}"];
            int totalRecords = tablesTree != null ? CountBlobLines(tablesTree[fileName]?.Target as Blob) : 0;

            detail.TableStats.Add(new BackupTableDiffDto
            {
                TableName = tableName,
                DisplayName = displayName,
                LinesAdded = patchEntry?.LinesAdded ?? 0,
                LinesDeleted = patchEntry?.LinesDeleted ?? 0,
                TotalRecordsInSnapshot = totalRecords
            });

            if (patchEntry != null)
            {
                ExtractReadableChanges(tableName, displayName, patchEntry.Patch, detail.ReadableChanges);
            }
        }

        return Task.FromResult(detail);
    }

    public Task<byte[]> ExportCommitToArchiveBytesAsync(string commitSha)
    {
        if (!Repository.IsValid(_gitDir))
            throw new InvalidOperationException("Kho lưu trữ sao lưu Git chưa được khởi tạo.");

        using var repo = new Repository(_gitDir);
        var commit = repo.Lookup<Commit>(commitSha)
            ?? throw new ArgumentException($"Không tìm thấy bản sao lưu với mã SHA: {commitSha}");

        var tablesTree = commit["tables"]?.Target as Tree
            ?? throw new InvalidOperationException("Bản sao lưu không chứa thư mục tables.");

        var manifest = new BackupArchiveManifestDto
        {
            FormatSignature = BackupArchiveManifestDto.ExpectedFormatSignature,
            SourceCommitSha = commit.Sha,
            CreatedAtLocal = commit.Author.When.LocalDateTime,
            BackupType = ParseBackupType(commit.MessageShort ?? string.Empty),
            Note = commit.Message?.Trim() ?? string.Empty
        };

        var filePayloads = new Dictionary<string, byte[]>();
        foreach (var (tableName, _) in ManagedTables)
        {
            var fileName = $"{tableName}.jsonl";
            var blob = tablesTree[fileName]?.Target as Blob;
            byte[] bytes;
            int lineCount = 0;

            if (blob != null)
            {
                using var ms = new MemoryStream();
                using (var contentStream = blob.GetContentStream())
                {
                    contentStream.CopyTo(ms);
                }
                bytes = ms.ToArray();
                lineCount = CountBytesLines(bytes);
            }
            else
            {
                bytes = Array.Empty<byte>();
            }

            filePayloads[fileName] = bytes;
            manifest.TableRecordCounts[tableName] = lineCount;
            manifest.TableSha256Checksums[fileName] = Convert.ToHexString(SHA256.HashData(bytes));
        }

        using var archiveStream = new MemoryStream();
        using (var zip = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var manifestEntry = zip.CreateEntry("manifest.json", CompressionLevel.Optimal);
            using (var entryStream = manifestEntry.Open())
            {
                var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, new JsonSerializerOptions { WriteIndented = true });
                entryStream.Write(manifestBytes, 0, manifestBytes.Length);
            }

            foreach (var (fileName, bytes) in filePayloads)
            {
                var tableEntry = zip.CreateEntry($"tables/{fileName}", CompressionLevel.Optimal);
                using var entryStream = tableEntry.Open();
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }

        return Task.FromResult(archiveStream.ToArray());
    }

    public Task<BackupArchiveManifestDto> InspectAndValidateBackupArchiveAsync(Stream archiveStream)
    {
        var (manifest, _) = ValidateAndReadArchive(archiveStream);
        return Task.FromResult(manifest);
    }

    public async Task ImportFromBackupArchiveAsync(Stream archiveStream, IProgress<(double Percent, string Status)>? progress = null)
    {
        progress?.Report((10, "Đang kiểm tra định dạng và chữ ký toàn vẹn SHA-256 của file backup..."));
        var (manifest, tableBytes) = ValidateAndReadArchive(archiveStream);

        progress?.Report((25, "Tạo điểm lưu an toàn [Pre-Import] trước khi ghi đè..."));
        var shortSource = manifest.SourceCommitSha.Length >= 7 ? manifest.SourceCommitSha[..7] : "external";
        await CreateLocalGitBackupAsync("Pre-Import", $"Lưu tự động trước khi Import file backup ({shortSource})", skipIfNoChanges: true);

        progress?.Report((45, "Đang giải mã dữ liệu các bảng từ gói backup..."));
        var nguoiDungs = DeserializeLines<NguoiDung>(tableBytes["NguoiDung.jsonl"]);
        var cauHinhs = DeserializeLines<CauHinhHeThong>(tableBytes["CauHinhHeThong.jsonl"]);
        var hocKys = DeserializeLines<HocKy>(tableBytes["HocKy.jsonl"]);
        var monHocs = DeserializeLines<MonHoc>(tableBytes["MonHoc.jsonl"]);
        var sinhViens = DeserializeLines<SinhVien>(tableBytes["SinhVien.jsonl"]);
        var lopHocPhans = DeserializeLines<LopHocPhan>(tableBytes["LopHocPhan.jsonl"]);
        var dangKyHocPhans = DeserializeLines<DangKyHocPhan>(tableBytes["DangKyHocPhan.jsonl"]);
        var hocPhiHocKys = DeserializeLines<HocPhiHocKy>(tableBytes["HocPhiHocKy.jsonl"]);

        progress?.Report((65, "Đang ghi đè dữ liệu vào CSDL SQLite Local..."));
        await OverwriteLocalDatabaseAsync(nguoiDungs, cauHinhs, hocKys, monHocs, sinhViens, lopHocPhans, dangKyHocPhans, hocPhiHocKys);

        progress?.Report((85, "Đang lưu mốc lịch sử [Imported] vào kho Git Local..."));
        await CreateLocalGitBackupAsync("Imported", $"Khôi phục từ file backup (Mã gốc: {shortSource})", skipIfNoChanges: false);

        progress?.Report((100, "Nhập file backup và ghi đè CSDL thành công!"));
    }

    public async Task<BackupCommitInfoDto?> CreateLocalGitBackupAsync(
        string backupType = "Manual",
        string? customNote = null,
        bool skipIfNoChanges = false)
    {
        EnsureGitRepoInitialized();

        // 1. Xuất Canonical JSONL từ SQLite Local
        await ExportCanonicalJsonlAsync();

        // 2. Chụp Binary Snapshot nóng
        await CreateBinarySnapshotAsync();

        using var repo = new Repository(_gitDir);
        Commands.Stage(repo, "*");

        var status = repo.RetrieveStatus(new StatusOptions { IncludeUntracked = true });
        if (!status.IsDirty)
        {
            if (skipIfNoChanges) return null;
        }

        // Tạo commit message giàu ngữ cảnh
        var summary = BuildDiffSummary(repo);
        var timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        var notePart = string.IsNullOrWhiteSpace(customNote) ? "" : $" - {customNote.Trim()}";
        var commitMessage = $"[{backupType} {timeStr}]{notePart}\n\n{summary}";

        var sig = new Signature("QuanLyDKHP AutoBackup", "backup@local", DateTimeOffset.Now);
        var commit = repo.Commit(commitMessage, sig, sig);

        return new BackupCommitInfoDto
        {
            CommitSha = commit.Sha,
            CommittedAtLocal = commit.Author.When.LocalDateTime,
            BackupType = backupType,
            Message = commitMessage,
            ChangedTablesSummary = summary
        };
    }

    public async Task EnsureDailyBackupAsync()
    {
        if (Repository.IsValid(_gitDir))
        {
            using var repo = new Repository(_gitDir);
            var today = DateTime.Today;
            var todayCommit = repo.Commits
                .FirstOrDefault(c => c.Author.When.LocalDateTime.Date == today && c.MessageShort.StartsWith("[Auto-Daily"));
            if (todayCommit != null)
            {
                return; // Đã có bản sao lưu ngày hôm nay
            }
        }

        await CreateLocalGitBackupAsync("Auto-Daily", "Tự động sao lưu đầu ngày", skipIfNoChanges: true);
    }

    public async Task<RestoreResultDto> RestoreFromGitCommitAsync(string commitSha, IProgress<(double Percent, string Status)>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(commitSha))
            throw new ArgumentException("Mã SHA của bản sao lưu không hợp lệ.", nameof(commitSha));

        if (!Repository.IsValid(_gitDir))
            throw new InvalidOperationException("Kho lưu trữ sao lưu Git chưa được khởi tạo.");

        var shortSha = commitSha.Length >= 7 ? commitSha[..7] : commitSha;
        CrashLogger.Info($"Restore[{shortSha}]: bắt đầu");

        progress?.Report((5, "Kiểm tra phiên bản sao lưu..."));
        using var repo = new Repository(_gitDir);
        var commit = repo.Lookup<Commit>(commitSha)
            ?? throw new ArgumentException($"Không tìm thấy bản sao lưu với mã SHA: {commitSha}");

        var tablesTree = commit["tables"]?.Target as Tree
            ?? throw new InvalidOperationException("Bản sao lưu không chứa thư mục tables.");

        // Chỉ đọc và kiểm tra dữ liệu target trước khi chạm vào SQLite local.
        ValidateRestoreTree(tablesTree);

        progress?.Report((15, "Đang đọc và kiểm tra dữ liệu từ kho Git..."));
        var nguoiDungs = ReadEntitiesFromTree<NguoiDung>(tablesTree, "NguoiDung.jsonl");
        var cauHinhs = ReadEntitiesFromTree<CauHinhHeThong>(tablesTree, "CauHinhHeThong.jsonl");
        var hocKys = ReadEntitiesFromTree<HocKy>(tablesTree, "HocKy.jsonl");
        var monHocs = ReadEntitiesFromTree<MonHoc>(tablesTree, "MonHoc.jsonl");
        var sinhViens = ReadEntitiesFromTree<SinhVien>(tablesTree, "SinhVien.jsonl");
        var lopHocPhans = ReadEntitiesFromTree<LopHocPhan>(tablesTree, "LopHocPhan.jsonl");
        var dangKyHocPhans = ReadEntitiesFromTree<DangKyHocPhan>(tablesTree, "DangKyHocPhan.jsonl");
        var hocPhiHocKys = ReadEntitiesFromTree<HocPhiHocKy>(tablesTree, "HocPhiHocKy.jsonl");

        // Bản restore rỗng tài khoản sẽ khiến không thể đăng nhập sau khi ghi đè.
        if (nguoiDungs.Count == 0)
        {
            throw new InvalidOperationException(
                "Bản sao lưu không có tài khoản người dùng nào. Đã hủy khôi phục để tránh mất khả năng đăng nhập; CSDL local không bị thay đổi.");
        }

        CrashLogger.Info(
            $"Restore[{shortSha}]: dữ liệu hợp lệ (NguoiDung={nguoiDungs.Count}, HocKy={hocKys.Count}, " +
            $"MonHoc={monHocs.Count}, SinhVien={sinhViens.Count}, LopHocPhan={lopHocPhans.Count}, " +
            $"DangKy={dangKyHocPhans.Count}, HocPhi={hocPhiHocKys.Count})");

        // Snapshot file là bảo đảm cuối cùng, kể cả khi working tree sạch (Git không có gì để commit).
        progress?.Report((25, "Tạo snapshot SQLite an toàn trước khi ghi đè..."));
        var snapshotPath = await CreateVersionedSnapshotAsync(shortSha);
        CrashLogger.Info($"Restore[{shortSha}]: snapshot={snapshotPath}");

        // Pre-Restore Git là lớp bảo hiểm thứ hai; lỗi Git không được chặn restore khi đã có snapshot.
        progress?.Report((35, "Tạo điểm lưu Git [Pre-Restore]..."));
        try
        {
            await CreateLocalGitBackupAsync(
                "Pre-Restore",
                $"Lưu tự động trước khi khôi phục về {shortSha}",
                skipIfNoChanges: true);
            CrashLogger.Info($"Restore[{shortSha}]: Pre-Restore Git hoàn tất");
        }
        catch (Exception ex)
        {
            CrashLogger.Error($"Restore[{shortSha}]: Git Pre-Restore lỗi, tiếp tục với snapshot {snapshotPath}", ex);
        }

        progress?.Report((55, "Đang ghi đè dữ liệu vào CSDL SQLite local..."));
        await OverwriteLocalDatabaseAsync(nguoiDungs, cauHinhs, hocKys, monHocs, sinhViens, lopHocPhans, dangKyHocPhans, hocPhiHocKys);
        CrashLogger.Info($"Restore[{shortSha}]: ghi đè SQLite hoàn tất");

        progress?.Report((75, "Kiểm tra số dòng và toàn vẹn dữ liệu sau khôi phục..."));
        await ValidateRestoredDatabaseAsync(
            new (string Table, int Expected)[]
            {
                ("NguoiDung", nguoiDungs.Count),
                ("CauHinhHeThong", cauHinhs.Count),
                ("HocKy", hocKys.Count),
                ("MonHoc", monHocs.Count),
                ("SinhVien", sinhViens.Count),
                ("LopHocPhan", lopHocPhans.Count),
                ("DangKyHocPhan", dangKyHocPhans.Count),
                ("HocPhiHocKy", hocPhiHocKys.Count)
            },
            snapshotPath);
        CrashLogger.Info($"Restore[{shortSha}]: kiểm tra sau khôi phục đạt");

        progress?.Report((90, "Tạo commit [Post-Restore] từ CSDL vừa khôi phục..."));
        var message = $"Phục hồi thành công về phiên bản {shortSha}.";
        string? warning = null;
        try
        {
            var postCommit = await CreateLocalGitBackupAsync(
                "Post-Restore",
                $"Khôi phục về {shortSha}",
                skipIfNoChanges: true);

            if (postCommit != null)
            {
                message += $" Đã lưu commit [{postCommit.ShortSha}].";
                CrashLogger.Info($"Restore[{shortSha}]: Post-Restore commit {postCommit.ShortSha}");
            }
            else
            {
                message += " Dữ liệu trùng với bản hiện tại nên không tạo commit mới.";
                CrashLogger.Info($"Restore[{shortSha}]: dữ liệu trùng, bỏ qua Post-Restore commit");
            }
        }
        catch (Exception ex)
        {
            CrashLogger.Error($"Restore[{shortSha}]: Post-Restore Git lỗi", ex);
            warning =
                $"Đã khôi phục xong CSDL về {shortSha}, nhưng không tạo được commit Git [Post-Restore]: {ex.Message}. " +
                $"Snapshot an toàn vẫn được giữ tại: {snapshotPath}";
        }

        progress?.Report((100, message));
        return new RestoreResultDto
        {
            Message = message,
            Warning = warning,
            SnapshotPath = snapshotPath
        };
    }

    public async Task<(int PushedRecords, string Message)> PushToRemoteHostAsync(
        IProgress<(double Percent, string Status)>? progress = null)
    {
        await using var remote = CreateRemoteDbContext(null);
        if (remote == null)
        {
            return (0, "Chưa cấu hình chuỗi kết nối PostgreSQL backup. Hãy lưu cấu hình ở tab 'DB Backup (PostgreSQL)'.");
        }

        progress?.Report((5, "Kết nối tới PostgreSQL backup..."));
        if (!await remote.Database.CanConnectAsync())
        {
            return (0, "Không thể kết nối tới PostgreSQL backup. Vui lòng kiểm tra mạng và thông tin kết nối.");
        }

        await using var local = await _localFactory.CreateDbContextAsync();
        var syncStartedAt = DateTime.UtcNow;

        progress?.Report((20, "Đẩy tài khoản, cấu hình, học kỳ, môn học..."));
        int pushed = 0;
        pushed += await PushTableAsync(local.NguoiDungs, remote.NguoiDungs, e => e.Id);
        pushed += await PushTableAsync(local.CauHinhHeThongs, remote.CauHinhHeThongs, e => e.Key);
        pushed += await PushTableAsync(local.HocKys, remote.HocKys, e => e.MaHocKy);
        pushed += await PushTableAsync(local.MonHocs, remote.MonHocs, e => e.MaMon);

        progress?.Report((45, "Đẩy sinh viên..."));
        pushed += await PushTableAsync(local.SinhViens, remote.SinhViens, e => e.MaSV);

        progress?.Report((60, "Đẩy lớp học phần..."));
        pushed += await PushTableAsync(local.LopHocPhans, remote.LopHocPhans, e => e.MaLHP);

        progress?.Report((75, "Đẩy đăng ký học phần..."));
        pushed += await PushTableAsync(local.DangKyHocPhans, remote.DangKyHocPhans, e => e.Id);

        progress?.Report((85, "Đẩy học phí học kỳ..."));
        pushed += await PushTableAsync(local.HocPhiHocKys, remote.HocPhiHocKys, e => e.Id);

        progress?.Report((92, "Lưu dữ liệu lên PostgreSQL backup..."));
        await remote.SaveChangesAsync();

        var meta = await local.SyncMetadatas.FindAsync("RemotePush");
        if (meta == null)
        {
            meta = new SyncMetadata { TableName = "RemotePush" };
            await local.SyncMetadatas.AddAsync(meta);
        }
        meta.LastSyncUtc = syncStartedAt;
        meta.RecordCount = pushed;
        await local.SaveChangesAsync();

        progress?.Report((100, $"Đồng bộ hoàn tất: đã đẩy {pushed} bản ghi lên PostgreSQL backup."));
        return (pushed, $"Đồng bộ thành công {pushed} bản ghi lên PostgreSQL backup.");
    }

    public async Task RestoreFromRemoteHostAsync(IProgress<(double Percent, string Status)>? progress = null)
    {
        await using var remote = CreateRemoteDbContext(null)
            ?? throw new InvalidOperationException("Chưa cấu hình chuỗi kết nối PostgreSQL backup.");

        progress?.Report((10, "Kết nối tới PostgreSQL backup..."));
        if (!await remote.Database.CanConnectAsync())
            throw new InvalidOperationException("Không thể kết nối tới PostgreSQL backup. Dữ liệu cục bộ không bị thay đổi.");

        // Đọc và xác thực đầy đủ trước khi chạm vào SQLite local.
        progress?.Report((25, "Đọc toàn bộ dữ liệu backup từ PostgreSQL..."));
        var nguoiDungs = await remote.NguoiDungs.AsNoTracking().ToListAsync();
        var cauHinhs = await remote.CauHinhHeThongs.AsNoTracking().ToListAsync();
        var hocKys = await remote.HocKys.AsNoTracking().ToListAsync();
        var monHocs = await remote.MonHocs.AsNoTracking().ToListAsync();
        var sinhViens = await remote.SinhViens.AsNoTracking().ToListAsync();
        var lopHocPhans = await remote.LopHocPhans.AsNoTracking().ToListAsync();
        var dangKyHocPhans = await remote.DangKyHocPhans.AsNoTracking().ToListAsync();
        var hocPhiHocKys = await remote.HocPhiHocKys.AsNoTracking().ToListAsync();

        // Chốt an toàn: bản backup rỗng tài khoản sẽ khiến không thể đăng nhập sau khi ghi đè.
        if (nguoiDungs.Count == 0)
        {
            throw new InvalidOperationException(
                "Bản backup PostgreSQL không có tài khoản người dùng nào. Đã hủy khôi phục để tránh mất khả năng đăng nhập; dữ liệu local không bị thay đổi.");
        }

        progress?.Report((40, "Tạo điểm lưu Git [Pre-Restore] cho dữ liệu local hiện có..."));
        await CreateLocalGitBackupAsync(
            "Pre-Restore",
            "Lưu tự động trước khi khôi phục từ PostgreSQL backup",
            skipIfNoChanges: true);

        progress?.Report((60, "Ghi đè dữ liệu vào SQLite local..."));
        await OverwriteLocalDatabaseAsync(nguoiDungs, cauHinhs, hocKys, monHocs, sinhViens, lopHocPhans, dangKyHocPhans, hocPhiHocKys);

        progress?.Report((90, "Lưu mốc lịch sử [Imported] vào kho Git local..."));
        await CreateLocalGitBackupAsync(
            "Imported",
            "Khôi phục từ PostgreSQL backup",
            skipIfNoChanges: false);

        progress?.Report((100, "Khôi phục từ PostgreSQL backup thành công!"));
    }

    // ------------------- Hỗ trợ nội bộ -------------------

    private RemoteBackupDbContext? CreateRemoteDbContext(string? overrideConnectionString)
    {
        var connectionString = string.IsNullOrWhiteSpace(overrideConnectionString)
            ? _configStore?.GetConnectionString()
            : overrideConnectionString.Trim();

        if (string.IsNullOrWhiteSpace(connectionString)) return null;

        var options = new DbContextOptionsBuilder<RemoteBackupDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new RemoteBackupDbContext(options);
    }

    /// <summary>
    /// Upsert một bảng: đọc khóa hiện có của bảng remote trong MỘT truy vấn,
    /// sau đó thêm/cập nhật trong bộ nhớ để tránh N+1 round-trip khi đẩy toàn bộ dữ liệu.
    /// </summary>
    private static async Task<int> PushTableAsync<TEntity, TKey>(
        IQueryable<TEntity> source,
        DbSet<TEntity> remoteSet,
        Func<TEntity, TKey> keySelector) where TEntity : class where TKey : notnull
    {
        var items = await source.AsNoTracking().ToListAsync();
        if (items.Count == 0) return 0;

        var existingByKey = (await remoteSet.ToListAsync()).ToDictionary(keySelector);
        foreach (var item in items)
        {
            var key = keySelector(item);
            if (existingByKey.TryGetValue(key, out var existing))
            {
                remoteSet.Entry(existing).CurrentValues.SetValues(item);
            }
            else
            {
                await remoteSet.AddAsync(item);
                existingByKey[key] = item;
            }
        }

        return items.Count;
    }

    private void EnsureGitRepoInitialized()
    {
        if (!Repository.IsValid(_gitDir))
        {
            Repository.Init(_gitDir);
            var gitignore = Path.Combine(_gitDir, ".gitignore");
            if (!File.Exists(gitignore))
            {
                File.WriteAllText(gitignore, "*.db\n*.db-wal\n*.db-shm\n*.tmp\n");
            }
        }
    }

    private async Task ExportCanonicalJsonlAsync()
    {
        await using var local = await _localFactory.CreateDbContextAsync();
        var opt = new JsonSerializerOptions { WriteIndented = false };

        await WriteTableAsync(Path.Combine(_tablesDir, "NguoiDung.jsonl"),
            await local.NguoiDungs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), opt);

        await WriteTableAsync(Path.Combine(_tablesDir, "CauHinhHeThong.jsonl"),
            await local.CauHinhHeThongs.AsNoTracking().OrderBy(x => x.Key).ToListAsync(), opt);

        await WriteTableAsync(Path.Combine(_tablesDir, "HocKy.jsonl"),
            await local.HocKys.AsNoTracking().OrderBy(x => x.MaHocKy).ToListAsync(), opt);

        await WriteTableAsync(Path.Combine(_tablesDir, "MonHoc.jsonl"),
            await local.MonHocs.AsNoTracking().OrderBy(x => x.MaMon).ToListAsync(), opt);

        await WriteTableAsync(Path.Combine(_tablesDir, "SinhVien.jsonl"),
            await local.SinhViens.AsNoTracking().OrderBy(x => x.MaSV).ToListAsync(), opt);

        await WriteTableAsync(Path.Combine(_tablesDir, "LopHocPhan.jsonl"),
            await local.LopHocPhans.AsNoTracking().OrderBy(x => x.MaLHP).ToListAsync(), opt);

        await WriteTableAsync(Path.Combine(_tablesDir, "DangKyHocPhan.jsonl"),
            await local.DangKyHocPhans.AsNoTracking().OrderBy(x => x.MaSV).ThenBy(x => x.MaLHP).ThenBy(x => x.Id).ToListAsync(), opt);

        await WriteTableAsync(Path.Combine(_tablesDir, "HocPhiHocKy.jsonl"),
            await local.HocPhiHocKys.AsNoTracking().OrderBy(x => x.MaSV).ThenBy(x => x.MaHocKy).ThenBy(x => x.Id).ToListAsync(), opt);
    }

    private static async Task WriteTableAsync<T>(string filePath, IEnumerable<T> items, JsonSerializerOptions options)
    {
        await using var stream = File.Create(filePath);
        await using var writer = new StreamWriter(stream);
        foreach (var item in items)
        {
            var line = JsonSerializer.Serialize(item, options);
            await writer.WriteLineAsync(line);
        }
    }

    private async Task CreateBinarySnapshotAsync()
    {
        var localPath = AppDbContext.GetPrimarySqlitePath();
        if (!File.Exists(localPath)) return;

        try
        {
            // Sử dụng SQLite Online Backup API để snapshot không bị lock
            await using var src = new SqliteConnection($"Data Source={localPath}");
            await using var dst = new SqliteConnection($"Data Source={_snapshotPath}");
            await src.OpenAsync();
            await dst.OpenAsync();
            src.BackupDatabase(dst);
        }
        catch
        {
            // ponytail: fallback copy file nếu in-memory hoặc connection bận
            try { File.Copy(localPath, _snapshotPath, overwrite: true); } catch { }
        }
    }

    /// <summary>
    /// Snapshot có phiên bản (không ghi đè) trước khi restore, dùng SQLite Online Backup API.
    /// Đây là lớp bảo đảm cuối cùng: nếu tạo snapshot thất bại thì hủy restore.
    /// </summary>
    private async Task<string> CreateVersionedSnapshotAsync(string commitSha)
    {
        await using var local = await _localFactory.CreateDbContextAsync();

        var localConnectionString = local.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(localConnectionString))
            throw new InvalidOperationException("Không xác định được chuỗi kết nối SQLite local để tạo snapshot an toàn.");

        var localPath = new SqliteConnectionStringBuilder(localConnectionString).DataSource;
        if (string.IsNullOrWhiteSpace(localPath) || localPath == ":memory:" || !File.Exists(localPath))
            throw new FileNotFoundException("Không tìm thấy CSDL SQLite local để tạo snapshot an toàn.", localPath);

        Directory.CreateDirectory(_snapshotsDir);
        var snapshotPath = Path.Combine(
            _snapshotsDir,
            $"pre-restore-{commitSha}-{DateTime.Now:yyyyMMdd-HHmmssfff}.db");

        try
        {
            var sourceConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = localPath,
                Pooling = false
            }.ToString();
            var snapshotConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = snapshotPath,
                Pooling = false
            }.ToString();

            using var source = new SqliteConnection(sourceConnectionString);
            using var destination = new SqliteConnection(snapshotConnectionString);
            source.Open();
            destination.Open();
            source.BackupDatabase(destination);
        }
        catch (Exception ex)
        {
            CrashLogger.Error($"Không thể tạo snapshot SQLite trước khôi phục: {snapshotPath}", ex);
            try { if (File.Exists(snapshotPath)) File.Delete(snapshotPath); } catch { }

            throw new InvalidOperationException(
                $"Không thể tạo snapshot SQLite an toàn trước khi khôi phục nên thao tác đã bị hủy; CSDL local không bị thay đổi. Chi tiết: {ex.Message}",
                ex);
        }

        PruneSnapshots(5);
        return snapshotPath;
    }

    private void PruneSnapshots(int keepCount)
    {
        try
        {
            if (!Directory.Exists(_snapshotsDir)) return;

            var staleSnapshots = Directory.GetFiles(_snapshotsDir, "pre-restore-*.db")
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Skip(keepCount);

            foreach (var file in staleSnapshots)
            {
                try { file.Delete(); }
                catch (Exception ex) { CrashLogger.Error($"Không xóa được snapshot cũ: {file.FullName}", ex); }
            }
        }
        catch (Exception ex)
        {
            CrashLogger.Error("Không dọn được snapshot cũ", ex);
        }
    }

    private static void ValidateRestoreTree(Tree tablesTree)
    {
        foreach (var (tableName, displayName) in ManagedTables)
        {
            // Commit cũ có thể chưa có bảng học phí; danh sách rỗng vẫn hợp lệ.
            if (tableName == "HocPhiHocKy") continue;

            if (tablesTree[$"{tableName}.jsonl"]?.Target is not Blob)
            {
                throw new InvalidOperationException(
                    $"Bản sao lưu thiếu dữ liệu bảng bắt buộc: {displayName} ({tableName}.jsonl). Đã hủy khôi phục, CSDL local không bị thay đổi.");
            }
        }
    }

    private async Task ValidateRestoredDatabaseAsync(
        (string Table, int Expected)[] expectedCounts,
        string snapshotPath)
    {
        await using var local = await _localFactory.CreateDbContextAsync();

        var actualCounts = new Dictionary<string, int>
        {
            ["NguoiDung"] = await local.NguoiDungs.CountAsync(),
            ["CauHinhHeThong"] = await local.CauHinhHeThongs.CountAsync(),
            ["HocKy"] = await local.HocKys.CountAsync(),
            ["MonHoc"] = await local.MonHocs.CountAsync(),
            ["SinhVien"] = await local.SinhViens.CountAsync(),
            ["LopHocPhan"] = await local.LopHocPhans.CountAsync(),
            ["DangKyHocPhan"] = await local.DangKyHocPhans.CountAsync(),
            ["HocPhiHocKy"] = await local.HocPhiHocKys.CountAsync()
        };

        foreach (var (table, expected) in expectedCounts)
        {
            var actual = actualCounts[table];
            if (actual != expected)
            {
                throw new InvalidOperationException(
                    $"CSDL đã được ghi đè nhưng số dòng bảng {table} không khớp (mong đợi {expected}, thực tế {actual}). " +
                    $"Snapshot an toàn để phục hồi: {snapshotPath}");
            }
        }

        var connection = local.Database.GetDbConnection();
        await local.Database.OpenConnectionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            var integrityResult = Convert.ToString(await command.ExecuteScalarAsync());
            if (!string.Equals(integrityResult, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"CSDL đã được ghi đè nhưng kiểm tra toàn vẹn integrity_check thất bại ('{integrityResult}'). " +
                    $"Snapshot an toàn để phục hồi: {snapshotPath}");
            }

            command.CommandText = "PRAGMA foreign_key_check;";
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var table = reader.IsDBNull(0) ? "?" : reader.GetString(0);
                throw new InvalidOperationException(
                    $"CSDL đã được ghi đè nhưng kiểm tra khóa ngoại phát hiện bản ghi mồ côi ở bảng {table}. " +
                    $"Snapshot an toàn để phục hồi: {snapshotPath}");
            }
        }
        finally
        {
            await local.Database.CloseConnectionAsync();
        }
    }

    private string BuildDiffSummary(Repository repo)
    {
        var patch = repo.Diff.Compare<Patch>(repo.Head.Tip?.Tree, DiffTargets.Index);
        var changes = new List<string>();
        foreach (var entry in patch)
        {
            if (entry.LinesAdded == 0 && entry.LinesDeleted == 0) continue;
            var name = Path.GetFileNameWithoutExtension(entry.Path);
            var parts = new List<string>();
            if (entry.LinesAdded > 0) parts.Add($"+{entry.LinesAdded}");
            if (entry.LinesDeleted > 0) parts.Add($"-{entry.LinesDeleted}");
            changes.Add($"{name} ({string.Join(", ", parts)})");
        }
        return changes.Count > 0 ? string.Join(", ", changes) : "Không có thay đổi dữ liệu";
    }

    private static List<T> ReadEntitiesFromTree<T>(Tree tablesTree, string fileName)
    {
        var entry = tablesTree[fileName];
        if (entry == null || entry.Target is not Blob blob) return new List<T>();

        var result = new List<T>();
        using var stream = blob.GetContentStream();
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var obj = JsonSerializer.Deserialize<T>(line);
            if (obj != null) result.Add(obj);
        }
        return result;
    }

    private async Task OverwriteLocalDatabaseAsync(
        List<NguoiDung> nguoiDungs,
        List<CauHinhHeThong> cauHinhs,
        List<HocKy> hocKys,
        List<MonHoc> monHocs,
        List<SinhVien> sinhViens,
        List<LopHocPhan> lopHocPhans,
        List<DangKyHocPhan> dangKyHocPhans,
        List<HocPhiHocKy> hocPhiHocKys)
    {
        await using var local = await _localFactory.CreateDbContextAsync();
        local.ChangeTracker.AutoDetectChangesEnabled = false;

        CrashLogger.Info(
            $"Overwrite DB: bắt đầu (NguoiDung={nguoiDungs.Count}, CauHinh={cauHinhs.Count}, HocKy={hocKys.Count}, " +
            $"MonHoc={monHocs.Count}, SinhVien={sinhViens.Count}, LopHocPhan={lopHocPhans.Count}, " +
            $"DangKy={dangKyHocPhans.Count}, HocPhi={hocPhiHocKys.Count})");

        await using var tx = await local.Database.BeginTransactionAsync();
        try
        {
            local.DangKyHocPhans.RemoveRange(local.DangKyHocPhans);
            local.HocPhiHocKys.RemoveRange(local.HocPhiHocKys);
            local.LopHocPhans.RemoveRange(local.LopHocPhans);
            local.MonHocs.RemoveRange(local.MonHocs);
            local.SinhViens.RemoveRange(local.SinhViens);
            local.HocKys.RemoveRange(local.HocKys);
            local.CauHinhHeThongs.RemoveRange(local.CauHinhHeThongs);
            local.NguoiDungs.RemoveRange(local.NguoiDungs);
            await local.SaveChangesAsync();

            // Xóa các entity đã tracked khỏi change tracker trước khi thêm dữ liệu mới,
            // tránh xung đột khóa/identity khi restore đè lên chính các Id cũ.
            local.ChangeTracker.Clear();

            if (nguoiDungs.Count > 0) await local.NguoiDungs.AddRangeAsync(nguoiDungs);
            if (cauHinhs.Count > 0) await local.CauHinhHeThongs.AddRangeAsync(cauHinhs);
            if (hocKys.Count > 0) await local.HocKys.AddRangeAsync(hocKys);
            if (monHocs.Count > 0) await local.MonHocs.AddRangeAsync(monHocs);
            if (sinhViens.Count > 0) await local.SinhViens.AddRangeAsync(sinhViens);
            if (lopHocPhans.Count > 0) await local.LopHocPhans.AddRangeAsync(lopHocPhans);
            if (dangKyHocPhans.Count > 0) await local.DangKyHocPhans.AddRangeAsync(dangKyHocPhans);
            if (hocPhiHocKys.Count > 0) await local.HocPhiHocKys.AddRangeAsync(hocPhiHocKys);

            await local.SaveChangesAsync();
            await tx.CommitAsync();
            CrashLogger.Info("Overwrite DB: commit transaction thành công");
        }
        catch (Exception ex)
        {
            CrashLogger.Error("Overwrite DB: lỗi, đang rollback transaction", ex);
            try { await tx.RollbackAsync(); }
            catch (Exception rollbackEx) { CrashLogger.Error("Overwrite DB: rollback thất bại", rollbackEx); }
            throw;
        }
    }

    private static string ParseBackupType(string msg)
    {
        if (msg.StartsWith("[Auto-Daily")) return "Auto-Daily";
        if (msg.StartsWith("[Pre-Restore")) return "Pre-Restore";
        if (msg.StartsWith("[Pre-Import")) return "Pre-Import";
        if (msg.StartsWith("[Imported")) return "Imported";
        if (msg.StartsWith("[Post-Import")) return "Post-Import";
        if (msg.StartsWith("[Post-Restore")) return "Post-Restore";
        return "Manual";
    }

    private static int CountBlobLines(Blob? blob)
    {
        if (blob == null) return 0;
        int count = 0;
        using var stream = blob.GetContentStream();
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line)) count++;
        }
        return count;
    }

    private static int CountBytesLines(byte[] bytes)
    {
        if (bytes.Length == 0) return 0;
        int count = 0;
        using var ms = new MemoryStream(bytes);
        using var reader = new StreamReader(ms);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line)) count++;
        }
        return count;
    }

    private static List<T> DeserializeLines<T>(byte[] bytes)
    {
        var result = new List<T>();
        if (bytes.Length == 0) return result;
        using var ms = new MemoryStream(bytes);
        using var reader = new StreamReader(ms);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var obj = JsonSerializer.Deserialize<T>(line);
            if (obj != null) result.Add(obj);
        }
        return result;
    }

    private static (BackupArchiveManifestDto Manifest, Dictionary<string, byte[]> TableBytes) ValidateAndReadArchive(Stream archiveStream)
    {
        if (archiveStream.CanSeek) archiveStream.Position = 0;

        ZipArchive zip;
        try
        {
            zip = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException("Tệp không phải là định dạng nén ZIP hợp lệ của chương trình.", ex);
        }

        using (zip)
        {
            var manifestEntry = zip.GetEntry("manifest.json")
                ?? throw new InvalidDataException("Tệp backup không hợp lệ: Thiếu tệp định danh 'manifest.json'. Vui lòng chỉ sử dụng tệp được xuất từ chương trình.");

            BackupArchiveManifestDto? manifest;
            using (var s = manifestEntry.Open())
            {
                manifest = JsonSerializer.Deserialize<BackupArchiveManifestDto>(s);
            }

            if (manifest == null || manifest.FormatSignature != BackupArchiveManifestDto.ExpectedFormatSignature)
            {
                throw new InvalidDataException($"Chữ ký định dạng tệp không khớp (Yêu cầu '{BackupArchiveManifestDto.ExpectedFormatSignature}'). Tệp không phải là bản sao lưu chuẩn của chương trình.");
            }

            var tableBytes = new Dictionary<string, byte[]>();
            foreach (var (tableName, _) in ManagedTables)
            {
                var fileName = $"{tableName}.jsonl";
                var entry = zip.GetEntry($"tables/{fileName}")
                    ?? throw new InvalidDataException($"Tệp backup bị thiếu dữ liệu bảng bắt buộc: tables/{fileName}.");

                using var ms = new MemoryStream();
                using (var es = entry.Open())
                {
                    es.CopyTo(ms);
                }
                var bytes = ms.ToArray();

                if (manifest.TableSha256Checksums.TryGetValue(fileName, out var expectedHash))
                {
                    var actualHash = Convert.ToHexString(SHA256.HashData(bytes));
                    if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException($"Dữ liệu bảng '{fileName}' đã bị chỉnh sửa hoặc hư hỏng (Mã kiểm tra SHA-256 không khớp).");
                    }
                }

                tableBytes[fileName] = bytes;
            }

            return (manifest, tableBytes);
        }
    }

    private static void ExtractReadableChanges(string tableName, string displayName, string rawPatch, List<string> targetList)
    {
        using var reader = new StringReader(rawPatch);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (targetList.Count >= 120) break; // ponytail: cap readable diff lines at 120 to keep UI snappy
            if ((line.StartsWith('+') && !line.StartsWith("+++")) ||
                (line.StartsWith('-') && !line.StartsWith("---")))
            {
                char op = line[0];
                var json = line[1..].Trim();
                if (!json.StartsWith('{')) continue;

                var desc = FormatJsonRecordSummary(tableName, displayName, op, json);
                if (!string.IsNullOrEmpty(desc))
                {
                    targetList.Add(desc);
                }
            }
        }
    }

    private static string FormatJsonRecordSummary(string tableName, string displayName, char op, string json)
    {
        string prefix = op == '+' ? "[+]" : "[-]";
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string GetStr(string prop) => root.TryGetProperty(prop, out var v) ? v.ToString() : "";

            return tableName switch
            {
                "SinhVien" => $"{prefix} {displayName}: {GetStr("MaSV")} - {GetStr("HoTen")} (Lớp: {GetStr("LopSinhHoat")}, Khóa: {GetStr("KhoaHoc")})",
                "MonHoc" => $"{prefix} {displayName}: {GetStr("MaMon")} - {GetStr("TenMon")} ({GetStr("SoTinChiLT")} LT / {GetStr("SoTinChiTH")} TH)",
                "LopHocPhan" => $"{prefix} {displayName}: {GetStr("MaLHP")} (Môn: {GetStr("MaMon")}, HK: {GetStr("MaHocKy")}, Sĩ số: {GetStr("SiSoHienTai")}/{GetStr("SiSoToiDa")})",
                "DangKyHocPhan" => $"{prefix} {displayName}: SV {GetStr("MaSV")} -> Lớp {GetStr("MaLHP")}",
                "HocPhiHocKy" => $"{prefix} {displayName}: SV {GetStr("MaSV")} (HK: {GetStr("MaHocKy")}) - Tổng phải đóng: {GetStr("TongHocPhiPhaiDong")} VNĐ",
                "CauHinhHeThong" => $"{prefix} {displayName}: {GetStr("Key")} = {GetStr("Value")}",
                "HocKy" => $"{prefix} {displayName}: {GetStr("MaHocKy")} - {GetStr("TenHocKy")} ({GetStr("NamHoc")})",
                "NguoiDung" => $"{prefix} {displayName}: {GetStr("TenDangNhap")} ({GetStr("HoTen")})",
                _ => $"{prefix} {displayName}: {json}"
            };
        }
        catch
        {
            return $"{prefix} {displayName}: {json}";
        }
    }
}

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
    private readonly IDbContextFactory<RemoteBackupDbContext>? _remoteFactory;
    private readonly string _gitDir;
    private readonly string _tablesDir;
    private readonly string _snapshotPath;

    public BackupService(
        IDbContextFactory<AppDbContext> localFactory,
        IDbContextFactory<RemoteBackupDbContext>? remoteFactory = null,
        string? customBackupDir = null)
    {
        _localFactory = localFactory;
        _remoteFactory = remoteFactory;

        var baseBackupDir = customBackupDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuanLyDKHP", "Backups");
        _gitDir = Path.Combine(baseBackupDir, "GitRepo");
        _tablesDir = Path.Combine(_gitDir, "tables");
        _snapshotPath = Path.Combine(baseBackupDir, "latest_snapshot.db");

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
                var meta = await local.SyncMetadatas.OrderByDescending(s => s.LastSyncUtc).FirstOrDefaultAsync();
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
            IsRemoteHostConfigured = _remoteFactory != null
        };
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

    public async Task RestoreFromGitCommitAsync(string commitSha, IProgress<(double Percent, string Status)>? progress = null)
    {
        if (!Repository.IsValid(_gitDir))
            throw new InvalidOperationException("Kho lưu trữ sao lưu Git chưa được khởi tạo.");

        progress?.Report((10, "Kiểm tra phiên bản sao lưu..."));
        using var repo = new Repository(_gitDir);
        var commit = repo.Lookup<Commit>(commitSha)
            ?? throw new ArgumentException($"Không tìm thấy bản sao lưu với mã SHA: {commitSha}");

        // Tạo bản lưu an toàn trước khi khôi phục
        progress?.Report((20, "Tạo điểm lưu an toàn trước khi phục hồi..."));
        await CreateLocalGitBackupAsync("Pre-Restore", $"Lưu tự động trước khi khôi phục về {commitSha[..7]}", skipIfNoChanges: true);

        progress?.Report((40, "Đang trích xuất dữ liệu từ kho Git..."));
        var tablesTree = commit["tables"]?.Target as Tree
            ?? throw new InvalidOperationException("Bản sao lưu không chứa thư mục tables.");

        var nguoiDungs = ReadEntitiesFromTree<NguoiDung>(tablesTree, "NguoiDung.jsonl");
        var cauHinhs = ReadEntitiesFromTree<CauHinhHeThong>(tablesTree, "CauHinhHeThong.jsonl");
        var hocKys = ReadEntitiesFromTree<HocKy>(tablesTree, "HocKy.jsonl");
        var monHocs = ReadEntitiesFromTree<MonHoc>(tablesTree, "MonHoc.jsonl");
        var sinhViens = ReadEntitiesFromTree<SinhVien>(tablesTree, "SinhVien.jsonl");
        var lopHocPhans = ReadEntitiesFromTree<LopHocPhan>(tablesTree, "LopHocPhan.jsonl");
        var dangKyHocPhans = ReadEntitiesFromTree<DangKyHocPhan>(tablesTree, "DangKyHocPhan.jsonl");
        var hocPhiHocKys = ReadEntitiesFromTree<HocPhiHocKy>(tablesTree, "HocPhiHocKy.jsonl");

        progress?.Report((60, "Đang nạp lại dữ liệu vào CSDL SQLite Local..."));
        await OverwriteLocalDatabaseAsync(nguoiDungs, cauHinhs, hocKys, monHocs, sinhViens, lopHocPhans, dangKyHocPhans, hocPhiHocKys);
        progress?.Report((100, $"Phục hồi thành công về phiên bản {commitSha[..7]}!"));
    }

    public async Task<(int PushedRecords, string Message)> PushDeltaToRemoteHostAsync(
        IProgress<(double Percent, string Status)>? progress = null)
    {
        if (_remoteFactory == null)
        {
            return (0, "Chưa cấu hình máy chủ DB Host từ xa (RemoteBackupConnection).");
        }

        progress?.Report((10, "Kết nối tới máy chủ DB Host..."));
        await using var remote = await _remoteFactory.CreateDbContextAsync();
        if (!await remote.Database.CanConnectAsync())
        {
            return (0, "Không thể kết nối tới máy chủ DB Host. Vui lòng kiểm tra mạng nội bộ.");
        }

        await using var local = await _localFactory.CreateDbContextAsync();
        var syncStartedAt = DateTime.UtcNow;

        var lastSyncUtc = await local.SyncMetadatas
            .Where(m => m.TableName == "RemotePush")
            .Select(m => m.LastSyncUtc)
            .FirstOrDefaultAsync();

        progress?.Report((30, "Lọc các bản ghi thay đổi từ CSDL Local..."));

        int pushed = 0;

        // Sinh viên mới hoặc sửa
        var newSv = await local.SinhViens.AsNoTracking()
            .Where(s => s.NgayTao > lastSyncUtc || (s.NgayCapNhat != null && s.NgayCapNhat > lastSyncUtc))
            .ToListAsync();
        foreach (var s in newSv)
        {
            var ex = await remote.SinhViens.FindAsync(s.MaSV);
            if (ex == null) await remote.SinhViens.AddAsync(s);
            else remote.Entry(ex).CurrentValues.SetValues(s);
            pushed++;
        }

        // Môn học
        var newMh = await local.MonHocs.AsNoTracking()
            .Where(m => m.NgayTao > lastSyncUtc || (m.NgayCapNhat != null && m.NgayCapNhat > lastSyncUtc))
            .ToListAsync();
        foreach (var m in newMh)
        {
            var ex = await remote.MonHocs.FindAsync(m.MaMon);
            if (ex == null) await remote.MonHocs.AddAsync(m);
            else remote.Entry(ex).CurrentValues.SetValues(m);
            pushed++;
        }

        // Lớp học phần
        var newLhp = await local.LopHocPhans.AsNoTracking()
            .Where(l => l.NgayTao > lastSyncUtc || (l.NgayCapNhat != null && l.NgayCapNhat > lastSyncUtc))
            .ToListAsync();
        foreach (var l in newLhp)
        {
            var ex = await remote.LopHocPhans.FindAsync(l.MaLHP);
            if (ex == null) await remote.LopHocPhans.AddAsync(l);
            else remote.Entry(ex).CurrentValues.SetValues(l);
            pushed++;
        }

        // Đăng ký học phần
        var newDk = await local.DangKyHocPhans.AsNoTracking()
            .Where(d => d.NgayTao > lastSyncUtc || (d.NgayCapNhat != null && d.NgayCapNhat > lastSyncUtc))
            .ToListAsync();
        foreach (var d in newDk)
        {
            var ex = await remote.DangKyHocPhans.FindAsync(d.Id);
            if (ex == null) await remote.DangKyHocPhans.AddAsync(d);
            else remote.Entry(ex).CurrentValues.SetValues(d);
            pushed++;
        }

        progress?.Report((75, "Lưu dữ liệu lên DB Host..."));
        await remote.SaveChangesAsync();

        // Cập nhật mốc LastSyncUtc
        var meta = await local.SyncMetadatas.FindAsync("RemotePush");
        if (meta == null)
        {
            meta = new SyncMetadata { TableName = "RemotePush" };
            await local.SyncMetadatas.AddAsync(meta);
        }
        meta.LastSyncUtc = syncStartedAt;
        meta.RecordCount += pushed;
        await local.SaveChangesAsync();

        progress?.Report((100, $"Đồng bộ hoàn tất: đã đẩy {pushed} bản ghi lên DB Host."));
        return (pushed, $"Đồng bộ thành công {pushed} bản ghi lên máy chủ DB Host.");
    }

    // ------------------- Hỗ trợ nội bộ -------------------

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
        }
        catch
        {
            await tx.RollbackAsync();
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

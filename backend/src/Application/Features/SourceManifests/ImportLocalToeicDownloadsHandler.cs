using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceManifests;

public sealed record ImportLocalToeicDownloadsCommand(string DownloadsRootPath);

public sealed record ImportLocalToeicDownloadsResult(
    int ScannedFileCount,
    int ImportedPdfCount,
    int RejectedFileCount
);

public sealed class ImportLocalToeicDownloadsHandler(IKnowledgeRepository repository)
{
    private const int LocalSheetRowOffset = 100_000;

    public ImportLocalToeicDownloadsResult Handle(ImportLocalToeicDownloadsCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.DownloadsRootPath))
        {
            throw new ArgumentException("Downloads root path is required.", nameof(command));
        }

        var root = Path.GetFullPath(command.DownloadsRootPath);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Local TOEIC downloads root was not found: {root}");
        }

        var scanned = 0;
        var imported = 0;
        var rejected = 0;
        var files = Directory
            .EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        for (var index = 0; index < files.Length; index++)
        {
            scanned++;
            var path = files[index];
            if (!IsValidPdf(path))
            {
                rejected++;
                continue;
            }

            var relativePath = NormalizeRelativePath(Path.GetRelativePath(root, path));
            var pathHash = ShortHash(relativePath);
            var sourceId = $"local-download-{pathHash}";
            var fileName = Path.GetFileName(path);
            var title = Path.GetFileNameWithoutExtension(path).Trim();
            var fileInfo = new FileInfo(path);
            var checksum = Sha256File(path);
            var materialClass = ClassifyMaterial(title, relativePath);
            var hasAnswerKey = ContainsAny($"{title} {relativePath}", "answer key", "đáp án", "dap an", "scriptsak", "script");

            var source = new SourceManifestEntry(
                SourceId: sourceId,
                SheetRowNumber: LocalSheetRowOffset + index + 1,
                Title: title,
                Url: $"file://downloads/{relativePath}",
                Provider: SourceProvider.Unknown,
                SourceType: SourceType.Other,
                PrimaryMaterialClass: materialClass,
                AccessStatus: SourceAccessStatus.Accessible,
                Evidence: new SourceEvidenceFlags(
                    HasPdf: true,
                    HasAudio: false,
                    HasImage: false,
                    HasTranscript: IsTranscript(title, relativePath),
                    HasAnswerKey: hasAnswerKey
                ),
                AuditNotes: $"Imported from downloads local corpus. RelativePath={relativePath}; Sha256={checksum}"
            );
            var container = new SourceContainer(
                ContainerId: $"local-container-{pathHash}",
                SourceId: sourceId,
                Provider: SourceProvider.Unknown,
                ExternalId: relativePath,
                Title: title,
                AccessStatus: SourceAccessStatus.Accessible,
                DiscoveredAtUtc: new DateTimeOffset(2026, 7, 3, 0, 0, 0, TimeSpan.Zero)
            );
            var asset = new SourceAsset(
                AssetId: $"local-asset-{pathHash}",
                ContainerId: container.ContainerId,
                SourceId: sourceId,
                FileName: fileName,
                MimeType: "application/pdf",
                Extension: ".pdf",
                SizeBytes: fileInfo.Length,
                DetectedRole: SourceAssetRole.Pdf,
                ProviderUrl: source.Url,
                ObjectKey: $"local-downloads/{relativePath}",
                Checksum: checksum
            );

            repository.UpsertSourceManifestEntry(source);
            repository.UpsertSourceContainer(container);
            repository.UpsertSourceAsset(asset);
            imported++;
        }

        return new ImportLocalToeicDownloadsResult(scanned, imported, rejected);
    }

    private static bool IsValidPdf(string path)
    {
        Span<byte> header = stackalloc byte[5];
        using var stream = File.OpenRead(path);
        return stream.Read(header) == header.Length && Encoding.ASCII.GetString(header) == "%PDF-";
    }

    private static string NormalizeRelativePath(string relativePath) =>
        relativePath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');

    private static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string ShortHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes[..8]).ToLowerInvariant();
    }

    private static MaterialClass ClassifyMaterial(string title, string relativePath)
    {
        var value = $"{title} {relativePath}".ToLowerInvariant();
        if (ContainsAny(value, "speaking", "writing", " sw", "toeic sw", "đề writing", "de writing"))
        {
            return MaterialClass.SpeakingWriting;
        }

        if (ContainsAny(value, "grammar", "ngữ pháp", "ngu phap", "collocation", "dictionary", "email english"))
        {
            return MaterialClass.GrammarReference;
        }

        if (ContainsAny(value, "vựng", "tu vung", "vocab", "1500", "300 từ", "300 tu", "600 essential"))
        {
            return MaterialClass.Vocabulary;
        }

        if (ContainsAny(value, "lộ trình", "lo trinh", "kế hoạch", "ke hoach", "30 ngày", "30 ngay", "10 ngày", "10 ngay"))
        {
            return MaterialClass.Roadmap;
        }

        if (ContainsAny(value, "mẹo", "meo", "thủ thuật", "thu thuat", "skill", "strategy", "tactics", "cẩm nang", "cam nang", "hướng dẫn", "huong dan"))
        {
            return MaterialClass.SkillBook;
        }

        if (ContainsAny(value, "toeic", "test", "đề", "de ", "economy", "sparta", "abc", "starter", "target", "analyst", "preparation", "format"))
        {
            return MaterialClass.TestBook;
        }

        return MaterialClass.Unknown;
    }

    private static bool IsTranscript(string title, string relativePath) =>
        ContainsAny($"{title} {relativePath}", "transcript", "lời thoại", "loi thoai", "script");

    private static bool ContainsAny(string value, params string[] needles)
    {
        var normalized = value.ToLowerInvariant();
        foreach (var needle in needles)
        {
            if (normalized.Contains(needle, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

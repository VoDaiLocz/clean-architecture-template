using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceManifests;

public sealed record ImportLocalToeicDownloadsCommand(string DownloadsRootPath);

public sealed record ImportLocalToeicDownloadsResult(
    int ScannedFileCount,
    int ImportedPdfCount,
    int ImportedAudioCount,
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
        var importedPdfs = 0;
        var importedAudio = 0;
        var rejected = 0;
        var files = Directory
            .EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        for (var index = 0; index < files.Length; index++)
        {
            scanned++;
            var path = files[index];
            var fileInfo = new FileInfo(path);
            var extension = fileInfo.Extension.ToLowerInvariant();
            var isValidPdf = IsValidPdf(path);
            var isSupportedAudio = IsSupportedAudio(extension);

            if (!isValidPdf && !isSupportedAudio)
            {
                rejected++;
                var rejectedRelativePath = NormalizeRelativePath(Path.GetRelativePath(root, path));
                var rejectedPathHash = ShortHash(rejectedRelativePath);

                var reason = RejectedReason.UnsupportedMime;
                if (extension == ".pdf")
                {
                    reason = RejectedReason.InvalidPdfHeader;
                }
                else if (fileInfo.Length < 10000 && !extension.Contains("mp4") && !extension.Contains("zip"))
                {
                    reason = RejectedReason.DriveHtmlPlaceholder;
                }

                repository.UpsertRejectedLocalSourceFile(new RejectedLocalSourceFile(
                    RejectionId: $"local-rejected-{rejectedPathHash}",
                    FilePath: rejectedRelativePath,
                    Extension: extension,
                    SizeBytes: fileInfo.Length,
                    Reason: reason,
                    AuditNotes: "Failed IsValidPdf check.",
                    RejectedAtUtc: DateTimeOffset.UtcNow
                ));

                continue;
            }

            var relativePath = NormalizeRelativePath(Path.GetRelativePath(root, path));
            var pathHash = ShortHash(relativePath);
            var sourceId = $"local-download-{pathHash}";
            var fileName = Path.GetFileName(path);
            var title = Path.GetFileNameWithoutExtension(path).Trim();
            var checksum = Sha256File(path);
            var materialClass = ClassifyMaterial(title, relativePath);
            var hasAnswerKey = ContainsAny($"{title} {relativePath}", "answer key", "đáp án", "dap an", "scriptsak", "script");
            var detectedRole = isSupportedAudio ? SourceAssetRole.Audio : SourceAssetRole.Pdf;
            var mimeType = isSupportedAudio ? MimeTypeForAudio(extension) : "application/pdf";
            var hasPdf = !isSupportedAudio;
            var hasAudio = isSupportedAudio;

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
                    HasPdf: hasPdf,
                    HasAudio: hasAudio,
                    HasImage: false,
                    HasTranscript: IsTranscript(title, relativePath),
                    HasAnswerKey: hasPdf && hasAnswerKey
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
                MimeType: mimeType,
                Extension: extension,
                SizeBytes: fileInfo.Length,
                DetectedRole: detectedRole,
                ProviderUrl: source.Url,
                ObjectKey: $"local-downloads/{relativePath}",
                Checksum: checksum
            );

            repository.UpsertSourceManifestEntry(source);
            repository.UpsertSourceContainer(container);
            repository.UpsertSourceAsset(asset);
            if (isSupportedAudio)
            {
                importedAudio++;
            }
            else
            {
                importedPdfs++;
            }
        }

        return new ImportLocalToeicDownloadsResult(scanned, importedPdfs, importedAudio, rejected);
    }

    private static bool IsValidPdf(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        Span<byte> header = stackalloc byte[5];
        using var stream = File.OpenRead(path);
        return stream.Read(header) == header.Length && Encoding.ASCII.GetString(header) == "%PDF-";
    }

    private static bool IsSupportedAudio(string extension) =>
        extension is ".mp3" or ".wav" or ".m4a" or ".ogg" or ".aac" or ".flac" or ".wma";

    private static string MimeTypeForAudio(string extension) =>
        extension switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".ogg" => "audio/ogg",
            ".aac" => "audio/aac",
            ".flac" => "audio/flac",
            ".wma" => "audio/x-ms-wma",
            _ => "application/octet-stream",
        };

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

using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceManifests;

public sealed record ImportToeicSourceManifestResult(
    int ImportedCount,
    int BlockedCount,
    int SourcesWithPdf,
    int SourcesWithAudio,
    int SourcesWithTranscript,
    int SourcesWithAnswerKey
);

public sealed class ImportToeicSourceManifestHandler(IKnowledgeRepository repository)
{
    public ImportToeicSourceManifestResult Handle()
    {
        foreach (var row in AuditedRows)
        {
            repository.UpsertSourceManifestEntry(SourceManifestClassifier.Classify(
                row.SheetRowNumber,
                row.Title,
                UrlFor(row.SourceType, row.SheetRowNumber),
                row.Inaccessible,
                row.HasPdf,
                row.HasAudio,
                row.HasTranscript,
                row.HasAnswerKey,
                row.HasImage
            ));
        }

        var summary = repository.GetSourceManifestSummary();
        return new ImportToeicSourceManifestResult(
            summary.TotalSources,
            summary.BlockedSources,
            summary.SourcesWithPdf,
            summary.SourcesWithAudio,
            summary.SourcesWithTranscript,
            summary.SourcesWithAnswerKey
        );
    }

    private static string UrlFor(SourceType sourceType, int rowNumber) =>
        sourceType switch
        {
            SourceType.DriveFile => $"https://drive.google.com/file/d/audit-source-{rowNumber}/view",
            SourceType.DriveFolder => $"https://drive.google.com/drive/folders/audit-source-{rowNumber}",
            SourceType.GoogleSheet => $"https://docs.google.com/spreadsheets/d/audit-source-{rowNumber}/edit",
            SourceType.GoogleDoc => $"https://docs.google.com/document/d/audit-source-{rowNumber}/edit",
            SourceType.SharePoint => $"https://toeic.sharepoint.com/sites/materials/audit-source-{rowNumber}",
            SourceType.Shortlink => $"https://tinyurl.com/toeic-audit-{rowNumber}",
            SourceType.ExternalWeb => $"https://toeic-source.example/materials/{rowNumber}",
            _ => $"https://toeic-source.example/unknown/{rowNumber}",
        };

    private static readonly IReadOnlyList<AuditedSourceRow> AuditedRows =
    [
        new(1, "Từ vựng Part 2 - TOEIC Practice Club", SourceType.DriveFile, false, true, true, true, true, false),
        new(2, "1500 từ vựng TOEIC thường gặp - Lửa TOEIC", SourceType.DriveFile, false, true, false, true, false, false),
        new(3, "30 ngày tự ôn", SourceType.GoogleSheet, false, false, false, true, false, false),
        new(4, "300 từ vựng TOEIC cho người mất gốc", SourceType.DriveFile, false, true, false, true, false, false),
        new(5, "Tài liệu tự luyện tập 450-800 nghe đọc - Zenlish", SourceType.DriveFile, false, true, true, true, false, true),
        new(6, "Tài liệu tự học SW - TOEIC 2 skill", SourceType.DriveFolder, false, true, false, false, false, false),
        new(7, "SPARTA TOEIC ( quyển hồng - 10TEST )", SourceType.DriveFolder, false, true, true, false, true, true),
        new(8, "SPARTA TOEIC (quyển cam - 5 TEST )", SourceType.DriveFolder, false, true, false, false, true, true),
        new(9, "TOMOTO TOEIC ( 10 TEST - FORMAT mới )", SourceType.DriveFolder, false, false, true, false, false, false),
        new(10, "YBM TOEIC 1,2,3", SourceType.DriveFolder, false, false, false, false, false, false),
        new(11, "TOEIC PREPARATION 2019 ( 2 quyển)", SourceType.DriveFolder, false, true, true, false, true, false),
        new(12, "Bộ 4 đề Full Test target 700+", SourceType.DriveFolder, false, true, false, false, false, false),
        new(13, "ECONOMY TOEIC 1-5", SourceType.DriveFolder, false, true, true, false, false, false),
        new(14, "Bộ xanh cam TOEIC", SourceType.DriveFolder, false, true, true, false, true, false),
        new(15, "ABC TOEIC", SourceType.DriveFolder, false, true, true, false, true, false),
        new(16, "TACTICS FOR TOEIC (LR Test)", SourceType.DriveFolder, false, true, true, false, false, true),
        new(17, "Taking the TOEIC - Skill and Strategies 1", SourceType.DriveFolder, false, true, true, false, false, false),
        new(18, "Taking the TOEIC - Skill and Strategies 2", SourceType.DriveFolder, false, true, true, false, false, false),
        new(19, "40 tuyệt chiêu luyện thi cấp tốc", SourceType.DriveFolder, false, false, false, false, false, false),
        new(20, "Hệ thống mẹo trong bài thi", SourceType.DriveFolder, false, true, false, false, false, false),
        new(21, "Ebook 10 nguyên tắc tự học TOEIC", SourceType.DriveFolder, false, true, false, false, false, false),
        new(22, "Tự học TOEIC để tiết kiệm tiền", SourceType.DriveFile, false, true, false, true, false, false),
        new(23, "Very Easy TOEIC cho người bắt đầu", SourceType.DriveFolder, false, true, true, false, false, false),
        new(24, "STARTER TOEIC", SourceType.DriveFolder, false, true, false, false, false, false),
        new(25, "BIG STEP TOEIC", SourceType.DriveFolder, false, false, false, false, false, false),
        new(26, "DEVELOPING SKILL TOEIC", SourceType.DriveFolder, false, true, true, false, false, false),
        new(27, "ANALYST TOEIC", SourceType.DriveFolder, false, true, false, false, false, false),
        new(28, "TARGET TOEIC", SourceType.DriveFolder, false, true, true, false, false, false),
        new(29, "LONGMAN TOEIC NEW REAL", SourceType.DriveFolder, false, false, true, false, false, false),
        new(30, "600 ESSENTIAL WORD FOR TOEIC", SourceType.DriveFolder, false, false, true, false, false, false),
        new(31, "Học theo Part 5,6,7, ( có giải thích)", SourceType.DriveFolder, false, false, false, false, false, false),
        new(32, "Bí kíp đạt 450 cho người mất gốc", SourceType.DriveFolder, false, false, false, false, false, false),
        new(33, "Tự luyện 550+ trong 10day - BENZEN", SourceType.DriveFolder, false, true, true, false, false, false),
        new(34, "3 đề TOEIC và giải thích cực sát - BENZEN", SourceType.DriveFolder, false, true, false, false, false, false),
        new(35, "Web học 600 từ vựng có hình ảnh", SourceType.ExternalWeb, false, false, false, false, false, false),
        new(36, "Khóa tiếng anh giao tiếp file ZIP", SourceType.DriveFolder, true, false, false, false, false, false),
        new(37, "TOEIC thần tốc cho mất gốc file ZIP", SourceType.DriveFolder, true, false, false, false, false, false),
        new(38, "Dễ dàng đạt Listening 750+ - Unica", SourceType.ExternalWeb, false, false, true, false, false, false),
        new(39, "Bí quyết chinh phúc 500+ - Unica", SourceType.ExternalWeb, false, false, false, false, false, false),
        new(40, "Tuyệt kĩ giải đề Listening", SourceType.ExternalWeb, false, false, false, false, false, false),
        new(41, "Bài luyện tập theo thang điểm", SourceType.DriveFolder, false, true, true, false, false, false),
        new(42, "Hướng dẫn giải bài nghe có 3 giọng đọc", SourceType.ExternalWeb, false, false, false, false, false, false),
        new(43, "Kĩ năng làm bài nghe có hình ảnh đề Format mới", SourceType.ExternalWeb, false, false, false, false, false, false),
        new(44, "Cẩm nang giải TOEIC Part 7", SourceType.DriveFile, false, true, false, true, false, false),
        new(45, "Các dạng câu hỏi Part 2", SourceType.ExternalWeb, false, false, false, false, false, false),
        new(46, "1000 câu giải đề Format mới", SourceType.DriveFile, false, true, false, true, false, true),
        new(47, "Giải thích chi tiết Part 5 New ECONOMY", SourceType.ExternalWeb, false, false, false, false, false, false),
        new(48, "Vượt qua dạng điền câu vào chỗ trống Part 6 dễ dàng", SourceType.ExternalWeb, false, false, false, false, false, false),
        new(49, "Giải chi tiết đề thi TOEIC theo mẫu của IIG", SourceType.ExternalWeb, false, false, false, false, false, false),
        new(50, "Giải thích chi tiết HACKER TOEIC Style", SourceType.ExternalWeb, false, false, false, false, false, false),
        new(51, "Kế hoạch ôn thi TOEIC 30 ngày - Lửa TOEIC", SourceType.ExternalWeb, false, false, false, false, false, false),
        new(52, "Thủ thuật làm bài thi TOEIC", SourceType.ExternalWeb, false, false, false, false, false, false),
        new(53, "8 tuần đạt target 750+", SourceType.SharePoint, false, true, false, false, false, false),
        new(54, "Lộ trình TOEIC 700+ từ A-Z", SourceType.DriveFile, false, true, true, true, false, false),
        new(55, "Lộ trình TOEIC 550+ trong 60 ngày", SourceType.SharePoint, true, false, false, false, false, false),
        new(56, "2 tháng đạt TOEIC 450", SourceType.SharePoint, true, false, false, false, false, false),
        new(57, "Tài liệu TOEIC Speaking + Writing", SourceType.SharePoint, true, false, false, false, false, false),
        new(58, "Liên từ phụ thuộc", SourceType.DriveFile, true, false, false, false, false, false),
        new(59, "Từ điển Oxford Collocations", SourceType.DriveFile, true, false, false, false, false, false),
        new(60, "IIIustrated English Dictionary", SourceType.DriveFile, true, false, false, false, false, false),
        new(61, "Keywords and Phrases", SourceType.DriveFolder, true, false, false, false, false, false),
        new(62, "Advanced Grammar in Use", SourceType.DriveFile, true, false, false, false, false, false),
        new(63, "Grammar Course Basic", SourceType.DriveFile, true, false, false, false, false, false),
        new(64, "Collins SW", SourceType.DriveFolder, true, false, false, false, false, false),
        new(65, "Tomato SW", SourceType.DriveFolder, true, false, false, false, false, false),
        new(66, "Tài liệu tự học SW", SourceType.DriveFolder, false, true, false, false, false, false),
        new(67, "Phương pháp nâng cấp Speaking", SourceType.Shortlink, false, false, false, false, false, false),
        new(68, "SW test IIG", SourceType.DriveFile, false, true, false, true, false, false),
        new(69, "Đề Writing", SourceType.DriveFolder, false, true, false, false, false, false),
        new(70, "Lộ trình cơ bản", SourceType.GoogleDoc, false, false, false, false, false, false),
        new(71, "Tiêu chí chấm", SourceType.Shortlink, false, false, false, false, false, false),
        new(72, "Kênh yt giải đề SW", SourceType.Shortlink, false, false, false, true, false, false),
        new(73, "100 đề luyện Speaking", SourceType.Shortlink, false, false, false, false, false, false),
    ];

    private sealed record AuditedSourceRow(
        int SheetRowNumber,
        string Title,
        SourceType SourceType,
        bool Inaccessible,
        bool HasPdf,
        bool HasAudio,
        bool HasImage,
        bool HasTranscript,
        bool HasAnswerKey
    );
}

namespace Domain.Aggregates.Corpus;

public static class SourceManifestClassifier
{
    public static SourceManifestEntry Classify(
        int sheetRowNumber,
        string title,
        string url,
        bool inaccessible,
        bool hasPdf,
        bool hasAudio,
        bool hasTranscript,
        bool hasAnswerKey,
        bool hasImage
    )
    {
        var normalizedTitle = title.Trim();
        return new SourceManifestEntry(
            SourceId: $"sheet-row-{sheetRowNumber}",
            SheetRowNumber: sheetRowNumber,
            Title: normalizedTitle,
            Url: url.Trim(),
            Provider: DetectProvider(url),
            SourceType: DetectSourceType(url),
            PrimaryMaterialClass: DetectMaterialClass(normalizedTitle, url),
            AccessStatus: inaccessible ? SourceAccessStatus.AccessBlocked : SourceAccessStatus.Accessible,
            Evidence: new SourceEvidenceFlags(hasPdf, hasAudio, hasImage, hasTranscript, hasAnswerKey),
            AuditNotes: inaccessible ? "Access blocked during source audit." : "Accessible during source audit."
        );
    }

    private static SourceProvider DetectProvider(string url)
    {
        var value = url.ToLowerInvariant();
        if (value.Contains("drive.google.com", StringComparison.Ordinal))
        {
            return SourceProvider.GoogleDrive;
        }

        if (value.Contains("docs.google.com", StringComparison.Ordinal))
        {
            return SourceProvider.GoogleDocs;
        }

        if (value.Contains("sharepoint.com", StringComparison.Ordinal))
        {
            return SourceProvider.SharePoint;
        }

        if (value.Contains("bit.ly", StringComparison.Ordinal) || value.Contains("tinyurl.com", StringComparison.Ordinal))
        {
            return SourceProvider.Shortlink;
        }

        if (value.StartsWith("http://", StringComparison.Ordinal) || value.StartsWith("https://", StringComparison.Ordinal))
        {
            return SourceProvider.ExternalWeb;
        }

        return SourceProvider.Unknown;
    }

    private static SourceType DetectSourceType(string url)
    {
        var value = url.ToLowerInvariant();
        if (value.Contains("drive.google.com/drive/folders", StringComparison.Ordinal))
        {
            return SourceType.DriveFolder;
        }

        if (value.Contains("drive.google.com/file/d", StringComparison.Ordinal))
        {
            return SourceType.DriveFile;
        }

        if (value.Contains("docs.google.com/spreadsheets", StringComparison.Ordinal))
        {
            return SourceType.GoogleSheet;
        }

        if (value.Contains("docs.google.com/document", StringComparison.Ordinal))
        {
            return SourceType.GoogleDoc;
        }

        if (value.Contains("sharepoint.com", StringComparison.Ordinal))
        {
            return SourceType.SharePoint;
        }

        if (value.Contains("bit.ly", StringComparison.Ordinal) || value.Contains("tinyurl.com", StringComparison.Ordinal))
        {
            return SourceType.Shortlink;
        }

        if (value.StartsWith("http://", StringComparison.Ordinal) || value.StartsWith("https://", StringComparison.Ordinal))
        {
            return SourceType.ExternalWeb;
        }

        return SourceType.Other;
    }

    private static MaterialClass DetectMaterialClass(string title, string url)
    {
        var value = $"{title} {url}".ToLowerInvariant();

        if (ContainsAny(value, "speaking", "writing", " sw", "toeic sw", "đề writing", "tieu chi cham", "tiêu chí chấm"))
        {
            return MaterialClass.SpeakingWriting;
        }

        if (ContainsAny(value, "grammar", "ngữ pháp", "ngu phap", "collocation", "dictionary", "liên từ", "lien tu"))
        {
            return MaterialClass.GrammarReference;
        }

        if (ContainsAny(value, "vựng", "tu vung", "vocab", "600 essential", "1500", "300 từ"))
        {
            return MaterialClass.Vocabulary;
        }

        if (ContainsAny(value, "lộ trình", "lo trinh", "30 ngày", "8 tuần", "60 ngày", "kế hoạch", "ke hoach"))
        {
            return MaterialClass.Roadmap;
        }

        if (ContainsAny(value, "mẹo", "meo", "chiến thuật", "chien thuat", "strategy", "skill", "tactics", "cẩm nang", "cam nang", "tuyệt chiêu", "tuyet chieu"))
        {
            return MaterialClass.SkillBook;
        }

        if (ContainsAny(value, "toeic", "test", "đề", "de ", "economy", "sparta", "tomato", "ybm", "abc", "starter", "target", "longman", "preparation", "format mới", "format moi"))
        {
            return MaterialClass.TestBook;
        }

        if (DetectSourceType(url) is SourceType.ExternalWeb or SourceType.Shortlink)
        {
            return MaterialClass.ExternalReference;
        }

        return MaterialClass.Unknown;
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (value.Contains(needle, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

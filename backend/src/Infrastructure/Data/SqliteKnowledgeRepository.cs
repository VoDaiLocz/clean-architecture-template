using System.Text.Json;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;
using Domain.Aggregates.LearningItems;
using Microsoft.Data.Sqlite;

namespace Infrastructure.Data;

public sealed class SqliteKnowledgeRepository : IKnowledgeRepository, IDisposable
{
    private readonly SqliteConnection connection;

    private SqliteKnowledgeRepository(SqliteConnection connection)
    {
        this.connection = connection;
    }

    public static SqliteKnowledgeRepository InMemory()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return new SqliteKnowledgeRepository(connection);
    }

    public static SqliteKnowledgeRepository FromConnectionString(string connectionString)
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return new SqliteKnowledgeRepository(connection);
    }

    public void Initialize()
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS raw_sources (
                source_id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                url TEXT NOT NULL,
                status TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS learning_items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                item_type TEXT NOT NULL,
                skill TEXT NOT NULL,
                part INTEGER,
                payload_json TEXT NOT NULL,
                source_id TEXT NOT NULL,
                file_id TEXT NOT NULL,
                page INTEGER,
                block_id TEXT,
                confidence REAL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS validation_issues (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                issue_code TEXT NOT NULL,
                message TEXT NOT NULL,
                item_type TEXT NOT NULL,
                source_id TEXT
            );

            CREATE TABLE IF NOT EXISTS corpus_manifests (
                corpus_id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                sheet_tabs INTEGER NOT NULL,
                sheet_rows INTEGER NOT NULL,
                pdf_books INTEGER NOT NULL,
                pdf_pages INTEGER NOT NULL,
                audio_files INTEGER NOT NULL,
                target_learning_items INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS normalization_stages (
                stage_key TEXT PRIMARY KEY,
                display_name TEXT NOT NULL,
                total_count INTEGER NOT NULL,
                completed_count INTEGER NOT NULL,
                rejected_count INTEGER NOT NULL
            );
            """;
        command.ExecuteNonQuery();
        EnsureDefaultCorpusManifest();
        EnsureDefaultNormalizationStages();
    }

    public void InsertRawSource(string sourceId, string title, string url, string status)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO raw_sources (source_id, title, url, status)
            VALUES ($source_id, $title, $url, $status)
            ON CONFLICT(source_id) DO UPDATE SET
                title = excluded.title,
                url = excluded.url,
                status = excluded.status
            """;
        command.Parameters.AddWithValue("$source_id", sourceId);
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$url", url);
        command.Parameters.AddWithValue("$status", status);
        command.ExecuteNonQuery();
    }

    public void UpsertCorpusManifest(CorpusManifest manifest)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO corpus_manifests (
                corpus_id, title, sheet_tabs, sheet_rows, pdf_books, pdf_pages, audio_files, target_learning_items
            )
            VALUES (
                $corpus_id, $title, $sheet_tabs, $sheet_rows, $pdf_books, $pdf_pages, $audio_files, $target_learning_items
            )
            ON CONFLICT(corpus_id) DO UPDATE SET
                title = excluded.title,
                sheet_tabs = excluded.sheet_tabs,
                sheet_rows = excluded.sheet_rows,
                pdf_books = excluded.pdf_books,
                pdf_pages = excluded.pdf_pages,
                audio_files = excluded.audio_files,
                target_learning_items = excluded.target_learning_items
            """;
        command.Parameters.AddWithValue("$corpus_id", manifest.CorpusId);
        command.Parameters.AddWithValue("$title", manifest.Title);
        command.Parameters.AddWithValue("$sheet_tabs", manifest.SheetTabs);
        command.Parameters.AddWithValue("$sheet_rows", manifest.SheetRows);
        command.Parameters.AddWithValue("$pdf_books", manifest.PdfBooks);
        command.Parameters.AddWithValue("$pdf_pages", manifest.PdfPages);
        command.Parameters.AddWithValue("$audio_files", manifest.AudioFiles);
        command.Parameters.AddWithValue("$target_learning_items", manifest.TargetLearningItems);
        command.ExecuteNonQuery();
    }

    public void UpsertNormalizationStage(NormalizationStageSnapshot stage)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO normalization_stages (
                stage_key, display_name, total_count, completed_count, rejected_count
            )
            VALUES (
                $stage_key, $display_name, $total_count, $completed_count, $rejected_count
            )
            ON CONFLICT(stage_key) DO UPDATE SET
                display_name = excluded.display_name,
                total_count = excluded.total_count,
                completed_count = excluded.completed_count,
                rejected_count = excluded.rejected_count
            """;
        command.Parameters.AddWithValue("$stage_key", stage.StageKey);
        command.Parameters.AddWithValue("$display_name", stage.DisplayName);
        command.Parameters.AddWithValue("$total_count", stage.TotalCount);
        command.Parameters.AddWithValue("$completed_count", stage.CompletedCount);
        command.Parameters.AddWithValue("$rejected_count", stage.RejectedCount);
        command.ExecuteNonQuery();
    }

    public CorpusManifest GetCorpusManifest()
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT corpus_id, title, sheet_tabs, sheet_rows, pdf_books, pdf_pages, audio_files, target_learning_items
            FROM corpus_manifests
            ORDER BY corpus_id
            LIMIT 1
            """;

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return DefaultCorpusManifest();
        }

        return new CorpusManifest(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetInt32(6),
            reader.GetInt32(7)
        );
    }

    public IReadOnlyList<NormalizationStageSnapshot> GetNormalizationStages()
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT stage_key, display_name, total_count, completed_count, rejected_count
            FROM normalization_stages
            ORDER BY
                CASE stage_key
                    WHEN 'inventory' THEN 1
                    WHEN 'extraction' THEN 2
                    WHEN 'normalization' THEN 3
                    WHEN 'validation' THEN 4
                    WHEN 'publish' THEN 5
                    ELSE 99
                END,
                stage_key
            """;

        var stages = new List<NormalizationStageSnapshot>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            stages.Add(new NormalizationStageSnapshot(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetInt32(4)
            ));
        }

        return stages;
    }

    public ValidationResult Publish(DraftLearningItem item)
    {
        var result = ValidationGate.Validate(item);
        if (!result.CanPublish)
        {
            RecordIssues(item, result);
            return result;
        }

        if (item.SourceRef is null)
        {
            throw new InvalidOperationException("Valid item unexpectedly has no source reference.");
        }

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO learning_items (
                item_type, skill, part, payload_json, source_id, file_id, page, block_id, confidence
            )
            VALUES (
                $item_type, $skill, $part, $payload_json, $source_id, $file_id, $page, $block_id, $confidence
            )
            """;
        command.Parameters.AddWithValue("$item_type", item.ItemType.ToString());
        command.Parameters.AddWithValue("$skill", item.Skill.ToString());
        command.Parameters.AddWithValue("$part", (object?)item.Part ?? DBNull.Value);
        command.Parameters.AddWithValue("$payload_json", JsonSerializer.Serialize(ItemPayload.From(item)));
        command.Parameters.AddWithValue("$source_id", item.SourceRef.SourceId);
        command.Parameters.AddWithValue("$file_id", item.SourceRef.FileId);
        command.Parameters.AddWithValue("$page", (object?)item.SourceRef.Page ?? DBNull.Value);
        command.Parameters.AddWithValue("$block_id", (object?)item.SourceRef.BlockId ?? DBNull.Value);
        command.Parameters.AddWithValue("$confidence", item.Confidence);
        command.ExecuteNonQuery();

        return result;
    }

    public int Count(string tableName)
    {
        if (tableName is not ("raw_sources" or "learning_items" or "validation_issues"))
        {
            throw new ArgumentException($"Unsupported table: {tableName}", nameof(tableName));
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    private void RecordIssues(DraftLearningItem item, ValidationResult result)
    {
        foreach (var issue in result.Issues)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO validation_issues (issue_code, message, item_type, source_id)
                VALUES ($issue_code, $message, $item_type, $source_id)
                """;
            command.Parameters.AddWithValue("$issue_code", issue.Code);
            command.Parameters.AddWithValue("$message", issue.Message);
            command.Parameters.AddWithValue("$item_type", item.ItemType.ToString());
            command.Parameters.AddWithValue("$source_id", (object?)item.SourceRef?.SourceId ?? DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    private void EnsureDefaultCorpusManifest()
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM corpus_manifests";
        var existingCount = Convert.ToInt32(command.ExecuteScalar());
        if (existingCount > 0)
        {
            return;
        }

        UpsertCorpusManifest(DefaultCorpusManifest());
    }

    private void EnsureDefaultNormalizationStages()
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM normalization_stages";
        var existingCount = Convert.ToInt32(command.ExecuteScalar());
        if (existingCount > 0)
        {
            return;
        }

        foreach (var stage in DefaultNormalizationStages())
        {
            UpsertNormalizationStage(stage);
        }
    }

    private static CorpusManifest DefaultCorpusManifest() =>
        new(
            "toeic-master",
            "Google Sheet + PDF book library awaiting authenticated scan",
            SheetTabs: 3,
            SheetRows: 18000,
            PdfBooks: 64,
            PdfPages: 12800,
            AudioFiles: 0,
            TargetLearningItems: 54000
        );

    private static IReadOnlyList<NormalizationStageSnapshot> DefaultNormalizationStages() =>
    [
        new("inventory", "Inventory scan", 18867, 1, 0),
        new("extraction", "Text extraction", 12800, 0, 0),
        new("normalization", "Item normalization", 54000, 0, 0),
        new("validation", "Validation gate", 54000, 0, 0),
        new("publish", "Publish queue", 54000, 0, 0),
    ];

    private sealed record ItemPayload(
        string Prompt,
        IReadOnlyDictionary<string, string> Options,
        string CorrectAnswer,
        string Explanation,
        string? GroupRef,
        string Word,
        string Meaning
    )
    {
        public static ItemPayload From(DraftLearningItem item) =>
            new(
                item.Prompt,
                item.Options,
                item.CorrectAnswer,
                item.Explanation,
                item.GroupRef,
                item.Word,
                item.Meaning
            );
    }
}

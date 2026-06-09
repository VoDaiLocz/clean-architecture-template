using System.Text.Json;
using Application.Common.Interfaces.Repositories;
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
            """;
        command.ExecuteNonQuery();
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

namespace DatabaseMigrations;

public static class PostgresMigrationCatalog
{
    public const string Provider = "postgresql";

    public static readonly IReadOnlyList<PostgresMigration> All =
    [
        new(
            "001_platform_schema_history",
            """
            CREATE TABLE IF NOT EXISTS platform_schema_history (
                migration_id varchar(160) PRIMARY KEY,
                applied_at_utc timestamptz NOT NULL DEFAULT now(),
                checksum varchar(128) NOT NULL
            );
            """
        ),
    ];
}

public sealed record PostgresMigration(string Id, string SqlStatements);

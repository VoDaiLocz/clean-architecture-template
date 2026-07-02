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
        new(
            "002_source_assets",
            """
            CREATE TABLE IF NOT EXISTS source_containers (
                container_id varchar(160) PRIMARY KEY,
                source_id varchar(160) NOT NULL REFERENCES source_manifest_entries(source_id),
                provider varchar(80) NOT NULL,
                external_id varchar(260) NOT NULL,
                title text NOT NULL,
                access_status varchar(80) NOT NULL,
                discovered_at_utc timestamptz NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_source_containers_source_id
                ON source_containers(source_id);

            CREATE TABLE IF NOT EXISTS source_assets (
                asset_id varchar(160) PRIMARY KEY,
                container_id varchar(160) NOT NULL REFERENCES source_containers(container_id),
                source_id varchar(160) NOT NULL REFERENCES source_manifest_entries(source_id),
                file_name text NOT NULL,
                mime_type varchar(160) NOT NULL,
                extension varchar(32) NOT NULL,
                size_bytes bigint NOT NULL,
                detected_role varchar(80) NOT NULL,
                provider_url text NOT NULL,
                object_key text NOT NULL,
                checksum varchar(160) NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_source_assets_container_id
                ON source_assets(container_id);

            CREATE INDEX IF NOT EXISTS idx_source_assets_source_role
                ON source_assets(source_id, detected_role);
            """
        ),
    ];
}

public sealed record PostgresMigration(string Id, string SqlStatements);

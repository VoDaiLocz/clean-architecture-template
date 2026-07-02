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

            CREATE TABLE IF NOT EXISTS source_manifest_entries (
                source_id TEXT PRIMARY KEY,
                sheet_row_number INTEGER NOT NULL,
                title TEXT NOT NULL,
                url TEXT NOT NULL,
                provider TEXT NOT NULL,
                source_type TEXT NOT NULL,
                material_class TEXT NOT NULL,
                access_status TEXT NOT NULL,
                has_pdf INTEGER NOT NULL,
                has_audio INTEGER NOT NULL,
                has_image INTEGER NOT NULL,
                has_transcript INTEGER NOT NULL,
                has_answer_key INTEGER NOT NULL,
                audit_notes TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS source_containers (
                container_id TEXT PRIMARY KEY,
                source_id TEXT NOT NULL,
                provider TEXT NOT NULL,
                external_id TEXT NOT NULL,
                title TEXT NOT NULL,
                access_status TEXT NOT NULL,
                discovered_at_utc TEXT NOT NULL,
                FOREIGN KEY (source_id) REFERENCES source_manifest_entries(source_id)
            );

            CREATE INDEX IF NOT EXISTS idx_source_containers_source_id
                ON source_containers(source_id);

            CREATE TABLE IF NOT EXISTS source_assets (
                asset_id TEXT PRIMARY KEY,
                container_id TEXT NOT NULL,
                source_id TEXT NOT NULL,
                file_name TEXT NOT NULL,
                mime_type TEXT NOT NULL,
                extension TEXT NOT NULL,
                size_bytes INTEGER NOT NULL,
                detected_role TEXT NOT NULL,
                provider_url TEXT NOT NULL,
                object_key TEXT NOT NULL,
                checksum TEXT NOT NULL,
                FOREIGN KEY (container_id) REFERENCES source_containers(container_id),
                FOREIGN KEY (source_id) REFERENCES source_manifest_entries(source_id)
            );

            CREATE INDEX IF NOT EXISTS idx_source_assets_container_id
                ON source_assets(container_id);

            CREATE INDEX IF NOT EXISTS idx_source_assets_source_role
                ON source_assets(source_id, detected_role);

            CREATE TABLE IF NOT EXISTS extracted_pages (
                page_id TEXT PRIMARY KEY,
                asset_id TEXT NOT NULL,
                page_number INTEGER NOT NULL,
                width INTEGER NOT NULL,
                height INTEGER NOT NULL,
                extracted_at_utc TEXT NOT NULL,
                FOREIGN KEY (asset_id) REFERENCES source_assets(asset_id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS idx_extracted_pages_asset_page
                ON extracted_pages(asset_id, page_number);

            CREATE TABLE IF NOT EXISTS extracted_text_blocks (
                block_id TEXT PRIMARY KEY,
                asset_id TEXT NOT NULL,
                page_id TEXT NOT NULL,
                page_number INTEGER NOT NULL,
                block_type TEXT NOT NULL,
                text TEXT NOT NULL,
                confidence REAL NOT NULL,
                coordinates_json TEXT NOT NULL,
                FOREIGN KEY (asset_id) REFERENCES source_assets(asset_id),
                FOREIGN KEY (page_id) REFERENCES extracted_pages(page_id)
            );

            CREATE INDEX IF NOT EXISTS idx_extracted_text_blocks_asset_page
                ON extracted_text_blocks(asset_id, page_number);

            CREATE TABLE IF NOT EXISTS draft_content_items (
                draft_id TEXT PRIMARY KEY,
                asset_id TEXT NOT NULL,
                material_class TEXT NOT NULL,
                toeic_part INTEGER,
                item_type TEXT NOT NULL,
                payload_json TEXT NOT NULL,
                source_trace_json TEXT NOT NULL,
                parser_confidence REAL NOT NULL,
                status TEXT NOT NULL,
                FOREIGN KEY (asset_id) REFERENCES source_assets(asset_id)
            );

            CREATE INDEX IF NOT EXISTS idx_draft_content_items_asset_status
                ON draft_content_items(asset_id, status);

            CREATE TABLE IF NOT EXISTS published_lessons (
                lesson_id TEXT PRIMARY KEY,
                unit_id TEXT NOT NULL,
                toeic_part INTEGER NOT NULL,
                title TEXT NOT NULL,
                objective TEXT NOT NULL,
                skill_tags TEXT NOT NULL,
                source_trace_json TEXT NOT NULL,
                status TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_published_lessons_unit_status
                ON published_lessons(unit_id, status);

            CREATE TABLE IF NOT EXISTS guided_examples (
                example_id TEXT PRIMARY KEY,
                lesson_id TEXT NOT NULL,
                prompt TEXT NOT NULL,
                explanation TEXT NOT NULL,
                display_order INTEGER NOT NULL,
                FOREIGN KEY (lesson_id) REFERENCES published_lessons(lesson_id)
            );

            CREATE INDEX IF NOT EXISTS idx_guided_examples_lesson_order
                ON guided_examples(lesson_id, display_order);

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

    public void UpsertSourceManifestEntry(SourceManifestEntry entry)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO source_manifest_entries (
                source_id,
                sheet_row_number,
                title,
                url,
                provider,
                source_type,
                material_class,
                access_status,
                has_pdf,
                has_audio,
                has_image,
                has_transcript,
                has_answer_key,
                audit_notes
            )
            VALUES (
                $source_id,
                $sheet_row_number,
                $title,
                $url,
                $provider,
                $source_type,
                $material_class,
                $access_status,
                $has_pdf,
                $has_audio,
                $has_image,
                $has_transcript,
                $has_answer_key,
                $audit_notes
            )
            ON CONFLICT(source_id) DO UPDATE SET
                sheet_row_number = excluded.sheet_row_number,
                title = excluded.title,
                url = excluded.url,
                provider = excluded.provider,
                source_type = excluded.source_type,
                material_class = excluded.material_class,
                access_status = excluded.access_status,
                has_pdf = excluded.has_pdf,
                has_audio = excluded.has_audio,
                has_image = excluded.has_image,
                has_transcript = excluded.has_transcript,
                has_answer_key = excluded.has_answer_key,
                audit_notes = excluded.audit_notes
            """;
        command.Parameters.AddWithValue("$source_id", entry.SourceId);
        command.Parameters.AddWithValue("$sheet_row_number", entry.SheetRowNumber);
        command.Parameters.AddWithValue("$title", entry.Title);
        command.Parameters.AddWithValue("$url", entry.Url);
        command.Parameters.AddWithValue("$provider", entry.Provider.ToString());
        command.Parameters.AddWithValue("$source_type", entry.SourceType.ToString());
        command.Parameters.AddWithValue("$material_class", entry.PrimaryMaterialClass.ToString());
        command.Parameters.AddWithValue("$access_status", entry.AccessStatus.ToString());
        command.Parameters.AddWithValue("$has_pdf", entry.Evidence.HasPdf ? 1 : 0);
        command.Parameters.AddWithValue("$has_audio", entry.Evidence.HasAudio ? 1 : 0);
        command.Parameters.AddWithValue("$has_image", entry.Evidence.HasImage ? 1 : 0);
        command.Parameters.AddWithValue("$has_transcript", entry.Evidence.HasTranscript ? 1 : 0);
        command.Parameters.AddWithValue("$has_answer_key", entry.Evidence.HasAnswerKey ? 1 : 0);
        command.Parameters.AddWithValue("$audit_notes", entry.AuditNotes);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<SourceManifestEntry> GetSourceManifestEntries()
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                source_id,
                sheet_row_number,
                title,
                url,
                provider,
                source_type,
                material_class,
                access_status,
                has_pdf,
                has_audio,
                has_image,
                has_transcript,
                has_answer_key,
                audit_notes
            FROM source_manifest_entries
            ORDER BY sheet_row_number
            """;

        var entries = new List<SourceManifestEntry>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(ReadSourceManifestEntry(reader));
        }

        return entries;
    }

    public SourceManifestSummary GetSourceManifestSummary()
    {
        var entries = GetSourceManifestEntries();
        return new SourceManifestSummary(
            TotalSources: entries.Count,
            AccessibleSources: entries.Count(entry => entry.AccessStatus == SourceAccessStatus.Accessible),
            BlockedSources: entries.Count(entry => entry.AccessStatus == SourceAccessStatus.AccessBlocked),
            DriveFiles: entries.Count(entry => entry.SourceType == SourceType.DriveFile),
            DriveFolders: entries.Count(entry => entry.SourceType == SourceType.DriveFolder),
            GoogleSheets: entries.Count(entry => entry.SourceType == SourceType.GoogleSheet),
            GoogleDocs: entries.Count(entry => entry.SourceType == SourceType.GoogleDoc),
            SharePointSources: entries.Count(entry => entry.SourceType == SourceType.SharePoint),
            Shortlinks: entries.Count(entry => entry.SourceType == SourceType.Shortlink),
            ExternalWebSources: entries.Count(entry => entry.SourceType == SourceType.ExternalWeb),
            TestBooks: entries.Count(entry => entry.PrimaryMaterialClass == MaterialClass.TestBook),
            SkillBooks: entries.Count(entry => entry.PrimaryMaterialClass == MaterialClass.SkillBook),
            VocabularySources: entries.Count(entry => entry.PrimaryMaterialClass == MaterialClass.Vocabulary),
            RoadmapSources: entries.Count(entry => entry.PrimaryMaterialClass == MaterialClass.Roadmap),
            SpeakingWritingSources: entries.Count(entry => entry.PrimaryMaterialClass == MaterialClass.SpeakingWriting),
            GrammarReferenceSources: entries.Count(entry => entry.PrimaryMaterialClass == MaterialClass.GrammarReference),
            SourcesWithPdf: entries.Count(entry => entry.Evidence.HasPdf),
            SourcesWithAudio: entries.Count(entry => entry.Evidence.HasAudio),
            SourcesWithImage: entries.Count(entry => entry.Evidence.HasImage),
            SourcesWithTranscript: entries.Count(entry => entry.Evidence.HasTranscript),
            SourcesWithAnswerKey: entries.Count(entry => entry.Evidence.HasAnswerKey)
        );
    }

    public void UpsertSourceContainer(SourceContainer container)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO source_containers (
                container_id,
                source_id,
                provider,
                external_id,
                title,
                access_status,
                discovered_at_utc
            )
            VALUES (
                $container_id,
                $source_id,
                $provider,
                $external_id,
                $title,
                $access_status,
                $discovered_at_utc
            )
            ON CONFLICT(container_id) DO UPDATE SET
                source_id = excluded.source_id,
                provider = excluded.provider,
                external_id = excluded.external_id,
                title = excluded.title,
                access_status = excluded.access_status,
                discovered_at_utc = excluded.discovered_at_utc
            """;
        command.Parameters.AddWithValue("$container_id", container.ContainerId);
        command.Parameters.AddWithValue("$source_id", container.SourceId);
        command.Parameters.AddWithValue("$provider", container.Provider.ToString());
        command.Parameters.AddWithValue("$external_id", container.ExternalId);
        command.Parameters.AddWithValue("$title", container.Title);
        command.Parameters.AddWithValue("$access_status", container.AccessStatus.ToString());
        command.Parameters.AddWithValue("$discovered_at_utc", container.DiscoveredAtUtc.ToString("O"));
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<SourceContainer> GetSourceContainers(string sourceId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                container_id,
                source_id,
                provider,
                external_id,
                title,
                access_status,
                discovered_at_utc
            FROM source_containers
            WHERE source_id = $source_id
            ORDER BY container_id
            """;
        command.Parameters.AddWithValue("$source_id", sourceId);

        var containers = new List<SourceContainer>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            containers.Add(ReadSourceContainer(reader));
        }

        return containers;
    }

    public void UpsertSourceAsset(SourceAsset asset)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO source_assets (
                asset_id,
                container_id,
                source_id,
                file_name,
                mime_type,
                extension,
                size_bytes,
                detected_role,
                provider_url,
                object_key,
                checksum
            )
            VALUES (
                $asset_id,
                $container_id,
                $source_id,
                $file_name,
                $mime_type,
                $extension,
                $size_bytes,
                $detected_role,
                $provider_url,
                $object_key,
                $checksum
            )
            ON CONFLICT(asset_id) DO UPDATE SET
                container_id = excluded.container_id,
                source_id = excluded.source_id,
                file_name = excluded.file_name,
                mime_type = excluded.mime_type,
                extension = excluded.extension,
                size_bytes = excluded.size_bytes,
                detected_role = excluded.detected_role,
                provider_url = excluded.provider_url,
                object_key = excluded.object_key,
                checksum = excluded.checksum
            """;
        command.Parameters.AddWithValue("$asset_id", asset.AssetId);
        command.Parameters.AddWithValue("$container_id", asset.ContainerId);
        command.Parameters.AddWithValue("$source_id", asset.SourceId);
        command.Parameters.AddWithValue("$file_name", asset.FileName);
        command.Parameters.AddWithValue("$mime_type", asset.MimeType);
        command.Parameters.AddWithValue("$extension", asset.Extension);
        command.Parameters.AddWithValue("$size_bytes", asset.SizeBytes);
        command.Parameters.AddWithValue("$detected_role", asset.DetectedRole.ToString());
        command.Parameters.AddWithValue("$provider_url", asset.ProviderUrl);
        command.Parameters.AddWithValue("$object_key", asset.ObjectKey);
        command.Parameters.AddWithValue("$checksum", asset.Checksum);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<SourceAsset> GetSourceAssets(string containerId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                asset_id,
                container_id,
                source_id,
                file_name,
                mime_type,
                extension,
                size_bytes,
                detected_role,
                provider_url,
                object_key,
                checksum
            FROM source_assets
            WHERE container_id = $container_id
            ORDER BY asset_id
            """;
        command.Parameters.AddWithValue("$container_id", containerId);

        var assets = new List<SourceAsset>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            assets.Add(ReadSourceAsset(reader));
        }

        return assets;
    }

    public void UpsertExtractedPage(ExtractedPage page)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO extracted_pages (
                page_id,
                asset_id,
                page_number,
                width,
                height,
                extracted_at_utc
            )
            VALUES (
                $page_id,
                $asset_id,
                $page_number,
                $width,
                $height,
                $extracted_at_utc
            )
            ON CONFLICT(page_id) DO UPDATE SET
                asset_id = excluded.asset_id,
                page_number = excluded.page_number,
                width = excluded.width,
                height = excluded.height,
                extracted_at_utc = excluded.extracted_at_utc
            """;
        command.Parameters.AddWithValue("$page_id", page.PageId);
        command.Parameters.AddWithValue("$asset_id", page.AssetId);
        command.Parameters.AddWithValue("$page_number", page.PageNumber);
        command.Parameters.AddWithValue("$width", page.Width);
        command.Parameters.AddWithValue("$height", page.Height);
        command.Parameters.AddWithValue("$extracted_at_utc", page.ExtractedAtUtc.ToString("O"));
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<ExtractedPage> GetExtractedPages(string assetId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT page_id, asset_id, page_number, width, height, extracted_at_utc
            FROM extracted_pages
            WHERE asset_id = $asset_id
            ORDER BY page_number
            """;
        command.Parameters.AddWithValue("$asset_id", assetId);

        var pages = new List<ExtractedPage>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            pages.Add(ReadExtractedPage(reader));
        }

        return pages;
    }

    public void UpsertExtractedTextBlock(ExtractedTextBlock block)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO extracted_text_blocks (
                block_id,
                asset_id,
                page_id,
                page_number,
                block_type,
                text,
                confidence,
                coordinates_json
            )
            VALUES (
                $block_id,
                $asset_id,
                $page_id,
                $page_number,
                $block_type,
                $text,
                $confidence,
                $coordinates_json
            )
            ON CONFLICT(block_id) DO UPDATE SET
                asset_id = excluded.asset_id,
                page_id = excluded.page_id,
                page_number = excluded.page_number,
                block_type = excluded.block_type,
                text = excluded.text,
                confidence = excluded.confidence,
                coordinates_json = excluded.coordinates_json
            """;
        command.Parameters.AddWithValue("$block_id", block.BlockId);
        command.Parameters.AddWithValue("$asset_id", block.AssetId);
        command.Parameters.AddWithValue("$page_id", block.PageId);
        command.Parameters.AddWithValue("$page_number", block.PageNumber);
        command.Parameters.AddWithValue("$block_type", block.BlockType.ToString());
        command.Parameters.AddWithValue("$text", block.Text);
        command.Parameters.AddWithValue("$confidence", block.Confidence);
        command.Parameters.AddWithValue("$coordinates_json", block.CoordinatesJson);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<ExtractedTextBlock> GetExtractedTextBlocks(string assetId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                block_id,
                asset_id,
                page_id,
                page_number,
                block_type,
                text,
                confidence,
                coordinates_json
            FROM extracted_text_blocks
            WHERE asset_id = $asset_id
            ORDER BY page_number, block_id
            """;
        command.Parameters.AddWithValue("$asset_id", assetId);

        var blocks = new List<ExtractedTextBlock>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            blocks.Add(ReadExtractedTextBlock(reader));
        }

        return blocks;
    }

    public void UpsertDraftContentItem(DraftContentItem draft)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO draft_content_items (
                draft_id,
                asset_id,
                material_class,
                toeic_part,
                item_type,
                payload_json,
                source_trace_json,
                parser_confidence,
                status
            )
            VALUES (
                $draft_id,
                $asset_id,
                $material_class,
                $toeic_part,
                $item_type,
                $payload_json,
                $source_trace_json,
                $parser_confidence,
                $status
            )
            ON CONFLICT(draft_id) DO UPDATE SET
                asset_id = excluded.asset_id,
                material_class = excluded.material_class,
                toeic_part = excluded.toeic_part,
                item_type = excluded.item_type,
                payload_json = excluded.payload_json,
                source_trace_json = excluded.source_trace_json,
                parser_confidence = excluded.parser_confidence,
                status = excluded.status
            """;
        command.Parameters.AddWithValue("$draft_id", draft.DraftId);
        command.Parameters.AddWithValue("$asset_id", draft.AssetId);
        command.Parameters.AddWithValue("$material_class", draft.MaterialClass.ToString());
        command.Parameters.AddWithValue("$toeic_part", (object?)draft.ToeicPart ?? DBNull.Value);
        command.Parameters.AddWithValue("$item_type", draft.ItemType);
        command.Parameters.AddWithValue("$payload_json", draft.PayloadJson);
        command.Parameters.AddWithValue("$source_trace_json", draft.SourceTraceJson);
        command.Parameters.AddWithValue("$parser_confidence", draft.ParserConfidence);
        command.Parameters.AddWithValue("$status", draft.Status.ToString());
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<DraftContentItem> GetDraftContentItems(string assetId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                draft_id,
                asset_id,
                material_class,
                toeic_part,
                item_type,
                payload_json,
                source_trace_json,
                parser_confidence,
                status
            FROM draft_content_items
            WHERE asset_id = $asset_id
            ORDER BY draft_id
            """;
        command.Parameters.AddWithValue("$asset_id", assetId);

        var drafts = new List<DraftContentItem>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            drafts.Add(ReadDraftContentItem(reader));
        }

        return drafts;
    }

    public void UpsertPublishedLesson(PublishedLesson lesson)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO published_lessons (
                lesson_id,
                unit_id,
                toeic_part,
                title,
                objective,
                skill_tags,
                source_trace_json,
                status
            )
            VALUES (
                $lesson_id,
                $unit_id,
                $toeic_part,
                $title,
                $objective,
                $skill_tags,
                $source_trace_json,
                $status
            )
            ON CONFLICT(lesson_id) DO UPDATE SET
                unit_id = excluded.unit_id,
                toeic_part = excluded.toeic_part,
                title = excluded.title,
                objective = excluded.objective,
                skill_tags = excluded.skill_tags,
                source_trace_json = excluded.source_trace_json,
                status = excluded.status
            """;
        command.Parameters.AddWithValue("$lesson_id", lesson.LessonId);
        command.Parameters.AddWithValue("$unit_id", lesson.UnitId);
        command.Parameters.AddWithValue("$toeic_part", lesson.ToeicPart);
        command.Parameters.AddWithValue("$title", lesson.Title);
        command.Parameters.AddWithValue("$objective", lesson.Objective);
        command.Parameters.AddWithValue("$skill_tags", lesson.SkillTags);
        command.Parameters.AddWithValue("$source_trace_json", lesson.SourceTraceJson);
        command.Parameters.AddWithValue("$status", lesson.Status.ToString());
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<PublishedLesson> GetPublishedLessons(string unitId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT lesson_id, unit_id, toeic_part, title, objective, skill_tags, source_trace_json, status
            FROM published_lessons
            WHERE unit_id = $unit_id
            ORDER BY lesson_id
            """;
        command.Parameters.AddWithValue("$unit_id", unitId);

        var lessons = new List<PublishedLesson>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lessons.Add(ReadPublishedLesson(reader));
        }

        return lessons;
    }

    public void UpsertGuidedExample(GuidedExample example)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO guided_examples (
                example_id,
                lesson_id,
                prompt,
                explanation,
                display_order
            )
            VALUES (
                $example_id,
                $lesson_id,
                $prompt,
                $explanation,
                $display_order
            )
            ON CONFLICT(example_id) DO UPDATE SET
                lesson_id = excluded.lesson_id,
                prompt = excluded.prompt,
                explanation = excluded.explanation,
                display_order = excluded.display_order
            """;
        command.Parameters.AddWithValue("$example_id", example.ExampleId);
        command.Parameters.AddWithValue("$lesson_id", example.LessonId);
        command.Parameters.AddWithValue("$prompt", example.Prompt);
        command.Parameters.AddWithValue("$explanation", example.Explanation);
        command.Parameters.AddWithValue("$display_order", example.DisplayOrder);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<GuidedExample> GetGuidedExamples(string lessonId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT example_id, lesson_id, prompt, explanation, display_order
            FROM guided_examples
            WHERE lesson_id = $lesson_id
            ORDER BY display_order, example_id
            """;
        command.Parameters.AddWithValue("$lesson_id", lessonId);

        var examples = new List<GuidedExample>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            examples.Add(ReadGuidedExample(reader));
        }

        return examples;
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
        if (tableName is not (
            "raw_sources"
            or "source_manifest_entries"
            or "source_containers"
            or "source_assets"
            or "extracted_pages"
            or "extracted_text_blocks"
            or "draft_content_items"
            or "published_lessons"
            or "guided_examples"
            or "learning_items"
            or "validation_issues"))
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

    private static SourceManifestEntry ReadSourceManifestEntry(SqliteDataReader reader)
    {
        return new SourceManifestEntry(
            reader.GetString(0),
            reader.GetInt32(1),
            reader.GetString(2),
            reader.GetString(3),
            Enum.Parse<SourceProvider>(reader.GetString(4)),
            Enum.Parse<SourceType>(reader.GetString(5)),
            Enum.Parse<MaterialClass>(reader.GetString(6)),
            Enum.Parse<SourceAccessStatus>(reader.GetString(7)),
            new SourceEvidenceFlags(
                reader.GetInt32(8) == 1,
                reader.GetInt32(9) == 1,
                reader.GetInt32(10) == 1,
                reader.GetInt32(11) == 1,
                reader.GetInt32(12) == 1
            ),
            reader.GetString(13)
        );
    }

    private static SourceContainer ReadSourceContainer(SqliteDataReader reader)
    {
        return new SourceContainer(
            reader.GetString(0),
            reader.GetString(1),
            Enum.Parse<SourceProvider>(reader.GetString(2)),
            reader.GetString(3),
            reader.GetString(4),
            Enum.Parse<SourceAccessStatus>(reader.GetString(5)),
            DateTimeOffset.Parse(reader.GetString(6))
        );
    }

    private static SourceAsset ReadSourceAsset(SqliteDataReader reader)
    {
        return new SourceAsset(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetInt64(6),
            Enum.Parse<SourceAssetRole>(reader.GetString(7)),
            reader.GetString(8),
            reader.GetString(9),
            reader.GetString(10)
        );
    }

    private static ExtractedPage ReadExtractedPage(SqliteDataReader reader)
    {
        return new ExtractedPage(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            DateTimeOffset.Parse(reader.GetString(5))
        );
    }

    private static ExtractedTextBlock ReadExtractedTextBlock(SqliteDataReader reader)
    {
        return new ExtractedTextBlock(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            Enum.Parse<ExtractedBlockType>(reader.GetString(4)),
            reader.GetString(5),
            reader.GetDecimal(6),
            reader.GetString(7)
        );
    }

    private static DraftContentItem ReadDraftContentItem(SqliteDataReader reader)
    {
        return new DraftContentItem(
            reader.GetString(0),
            reader.GetString(1),
            Enum.Parse<MaterialClass>(reader.GetString(2)),
            reader.IsDBNull(3) ? null : reader.GetInt32(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetDecimal(7),
            Enum.Parse<DraftContentStatus>(reader.GetString(8))
        );
    }

    private static PublishedLesson ReadPublishedLesson(SqliteDataReader reader)
    {
        return new PublishedLesson(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            Enum.Parse<PublishedContentStatus>(reader.GetString(7))
        );
    }

    private static GuidedExample ReadGuidedExample(SqliteDataReader reader)
    {
        return new GuidedExample(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetInt32(4)
        );
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

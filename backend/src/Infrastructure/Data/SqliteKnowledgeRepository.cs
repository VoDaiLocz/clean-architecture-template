using System.Text.Json;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;
using Domain.Aggregates.LearnerProgress;
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

            CREATE TABLE IF NOT EXISTS source_discovery_issues (
                issue_id TEXT PRIMARY KEY,
                source_id TEXT NOT NULL,
                issue_code TEXT NOT NULL,
                message TEXT NOT NULL,
                status TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                FOREIGN KEY (source_id) REFERENCES source_manifest_entries(source_id)
            );

            CREATE INDEX IF NOT EXISTS idx_source_discovery_issues_source_status
                ON source_discovery_issues(source_id, status);

            CREATE TABLE IF NOT EXISTS source_resolution_records (
                resolution_id TEXT PRIMARY KEY,
                source_id TEXT NOT NULL,
                original_url TEXT NOT NULL,
                resolved_url TEXT NOT NULL,
                http_status_code INTEGER NOT NULL,
                redirect_count INTEGER NOT NULL,
                status TEXT NOT NULL,
                resolved_at_utc TEXT NOT NULL,
                FOREIGN KEY (source_id) REFERENCES source_manifest_entries(source_id)
            );

            CREATE INDEX IF NOT EXISTS idx_source_resolution_records_source_status
                ON source_resolution_records(source_id, status);

            CREATE TABLE IF NOT EXISTS source_audio_metadata (
                audio_metadata_id TEXT PRIMARY KEY,
                asset_id TEXT NOT NULL,
                duration_seconds INTEGER NOT NULL,
                format TEXT NOT NULL,
                sample_rate_hz INTEGER NOT NULL,
                bitrate_kbps INTEGER NOT NULL,
                extracted_at_utc TEXT NOT NULL,
                FOREIGN KEY (asset_id) REFERENCES source_assets(asset_id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS idx_source_audio_metadata_asset
                ON source_audio_metadata(asset_id);

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

            CREATE TABLE IF NOT EXISTS published_questions (
                question_id TEXT PRIMARY KEY,
                lesson_id TEXT NOT NULL,
                toeic_part INTEGER NOT NULL,
                question_type TEXT NOT NULL,
                prompt TEXT NOT NULL,
                options_json TEXT NOT NULL,
                correct_answer TEXT NOT NULL,
                explanation TEXT NOT NULL,
                media_asset_id TEXT,
                passage_id TEXT,
                group_id TEXT,
                evidence_json TEXT NOT NULL,
                skill_tags TEXT NOT NULL,
                source_trace_json TEXT NOT NULL,
                status TEXT NOT NULL,
                FOREIGN KEY (lesson_id) REFERENCES published_lessons(lesson_id),
                FOREIGN KEY (media_asset_id) REFERENCES source_assets(asset_id)
            );

            CREATE INDEX IF NOT EXISTS idx_published_questions_part_status
                ON published_questions(toeic_part, status);

            CREATE INDEX IF NOT EXISTS idx_published_questions_lesson
                ON published_questions(lesson_id);

            CREATE TABLE IF NOT EXISTS published_tests (
                test_id TEXT PRIMARY KEY,
                test_mode TEXT NOT NULL,
                title TEXT NOT NULL,
                target_question_count INTEGER NOT NULL,
                duration_minutes INTEGER NOT NULL,
                source_trace_json TEXT NOT NULL,
                status TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_published_tests_mode_status
                ON published_tests(test_mode, status);

            CREATE TABLE IF NOT EXISTS published_test_sections (
                section_id TEXT PRIMARY KEY,
                test_id TEXT NOT NULL,
                section_type TEXT NOT NULL,
                display_order INTEGER NOT NULL,
                target_question_count INTEGER NOT NULL,
                duration_minutes INTEGER NOT NULL,
                FOREIGN KEY (test_id) REFERENCES published_tests(test_id)
            );

            CREATE INDEX IF NOT EXISTS idx_published_test_sections_test_order
                ON published_test_sections(test_id, display_order);

            CREATE TABLE IF NOT EXISTS published_test_items (
                test_item_id TEXT PRIMARY KEY,
                section_id TEXT NOT NULL,
                question_id TEXT NOT NULL,
                toeic_part INTEGER NOT NULL,
                display_order INTEGER NOT NULL,
                score_weight REAL NOT NULL,
                FOREIGN KEY (section_id) REFERENCES published_test_sections(section_id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS idx_published_test_items_section_order
                ON published_test_items(section_id, display_order);

            CREATE TABLE IF NOT EXISTS learner_profiles (
                learner_id TEXT PRIMARY KEY,
                display_name TEXT NOT NULL,
                email TEXT NOT NULL,
                target_score INTEGER NOT NULL,
                current_estimated_score INTEGER NOT NULL,
                daily_study_minutes INTEGER NOT NULL,
                time_zone_id TEXT NOT NULL,
                status TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_learner_profiles_status
                ON learner_profiles(status);

            CREATE TABLE IF NOT EXISTS learner_assignments (
                assignment_id TEXT PRIMARY KEY,
                learner_id TEXT NOT NULL,
                assignment_type TEXT NOT NULL,
                content_ref_id TEXT NOT NULL,
                status TEXT NOT NULL,
                assigned_at_utc TEXT NOT NULL,
                due_at_utc TEXT,
                FOREIGN KEY (learner_id) REFERENCES learner_profiles(learner_id)
            );

            CREATE INDEX IF NOT EXISTS idx_learner_assignments_learner_status
                ON learner_assignments(learner_id, status);

            CREATE TABLE IF NOT EXISTS activity_sessions (
                session_id TEXT PRIMARY KEY,
                assignment_id TEXT NOT NULL,
                learner_id TEXT NOT NULL,
                activity_type TEXT NOT NULL,
                status TEXT NOT NULL,
                started_at_utc TEXT NOT NULL,
                completed_at_utc TEXT,
                FOREIGN KEY (assignment_id) REFERENCES learner_assignments(assignment_id),
                FOREIGN KEY (learner_id) REFERENCES learner_profiles(learner_id)
            );

            CREATE INDEX IF NOT EXISTS idx_activity_sessions_assignment
                ON activity_sessions(assignment_id);

            CREATE TABLE IF NOT EXISTS learner_attempts (
                attempt_id TEXT PRIMARY KEY,
                session_id TEXT NOT NULL,
                learner_id TEXT NOT NULL,
                status TEXT NOT NULL,
                correct_count INTEGER NOT NULL,
                total_count INTEGER NOT NULL,
                score_percent INTEGER NOT NULL,
                submitted_at_utc TEXT NOT NULL,
                FOREIGN KEY (session_id) REFERENCES activity_sessions(session_id),
                FOREIGN KEY (learner_id) REFERENCES learner_profiles(learner_id)
            );

            CREATE INDEX IF NOT EXISTS idx_learner_attempts_session
                ON learner_attempts(session_id);

            CREATE TABLE IF NOT EXISTS attempt_answers (
                answer_id TEXT PRIMARY KEY,
                attempt_id TEXT NOT NULL,
                question_id TEXT NOT NULL,
                learner_answer TEXT NOT NULL,
                correct_answer TEXT NOT NULL,
                is_correct INTEGER NOT NULL,
                answered_at_utc TEXT NOT NULL,
                FOREIGN KEY (attempt_id) REFERENCES learner_attempts(attempt_id)
            );

            CREATE INDEX IF NOT EXISTS idx_attempt_answers_attempt
                ON attempt_answers(attempt_id);

            CREATE TABLE IF NOT EXISTS review_items (
                review_item_id TEXT PRIMARY KEY,
                learner_id TEXT NOT NULL,
                source_attempt_id TEXT NOT NULL,
                question_id TEXT NOT NULL,
                unit_id TEXT NOT NULL,
                error_tag TEXT NOT NULL,
                learner_answer TEXT NOT NULL,
                correct_answer TEXT NOT NULL,
                status TEXT NOT NULL,
                is_blocking INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                resolved_at_utc TEXT,
                FOREIGN KEY (learner_id) REFERENCES learner_profiles(learner_id)
            );

            CREATE INDEX IF NOT EXISTS idx_review_items_learner_status
                ON review_items(learner_id, status, is_blocking);

            CREATE TABLE IF NOT EXISTS repair_attempts (
                repair_attempt_id TEXT PRIMARY KEY,
                review_item_id TEXT NOT NULL,
                learner_id TEXT NOT NULL,
                answer TEXT NOT NULL,
                is_correct INTEGER NOT NULL,
                attempted_at_utc TEXT NOT NULL,
                FOREIGN KEY (review_item_id) REFERENCES review_items(review_item_id),
                FOREIGN KEY (learner_id) REFERENCES learner_profiles(learner_id)
            );

            CREATE INDEX IF NOT EXISTS idx_repair_attempts_review
                ON repair_attempts(review_item_id, attempted_at_utc);

            CREATE TABLE IF NOT EXISTS mastery_records (
                mastery_record_id TEXT PRIMARY KEY,
                learner_id TEXT NOT NULL,
                unit_id TEXT NOT NULL,
                mastery_percent INTEGER NOT NULL,
                is_unlocked INTEGER NOT NULL,
                blocking_review_count INTEGER NOT NULL,
                updated_at_utc TEXT NOT NULL,
                FOREIGN KEY (learner_id) REFERENCES learner_profiles(learner_id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS idx_mastery_records_learner_unit
                ON mastery_records(learner_id, unit_id);

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

    public SourceAsset? GetSourceAsset(string assetId)
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
            WHERE asset_id = $asset_id
            """;
        command.Parameters.AddWithValue("$asset_id", assetId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadSourceAsset(reader) : null;
    }

    public void UpsertSourceDiscoveryIssue(SourceDiscoveryIssue issue)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO source_discovery_issues (
                issue_id, source_id, issue_code, message, status, created_at_utc
            )
            VALUES (
                $issue_id, $source_id, $issue_code, $message, $status, $created_at_utc
            )
            ON CONFLICT(issue_id) DO UPDATE SET
                source_id = excluded.source_id,
                issue_code = excluded.issue_code,
                message = excluded.message,
                status = excluded.status,
                created_at_utc = excluded.created_at_utc
            """;
        command.Parameters.AddWithValue("$issue_id", issue.IssueId);
        command.Parameters.AddWithValue("$source_id", issue.SourceId);
        command.Parameters.AddWithValue("$issue_code", issue.IssueCode);
        command.Parameters.AddWithValue("$message", issue.Message);
        command.Parameters.AddWithValue("$status", issue.Status.ToString());
        command.Parameters.AddWithValue("$created_at_utc", issue.CreatedAtUtc.ToString("O"));
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<SourceDiscoveryIssue> GetSourceDiscoveryIssues(string sourceId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT issue_id, source_id, issue_code, message, status, created_at_utc
            FROM source_discovery_issues
            WHERE source_id = $source_id
            ORDER BY created_at_utc, issue_id
            """;
        command.Parameters.AddWithValue("$source_id", sourceId);

        var issues = new List<SourceDiscoveryIssue>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            issues.Add(ReadSourceDiscoveryIssue(reader));
        }

        return issues;
    }

    public void UpsertSourceResolutionRecord(SourceResolutionRecord record)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO source_resolution_records (
                resolution_id, source_id, original_url, resolved_url, http_status_code, redirect_count, status, resolved_at_utc
            )
            VALUES (
                $resolution_id, $source_id, $original_url, $resolved_url, $http_status_code, $redirect_count, $status, $resolved_at_utc
            )
            ON CONFLICT(resolution_id) DO UPDATE SET
                source_id = excluded.source_id,
                original_url = excluded.original_url,
                resolved_url = excluded.resolved_url,
                http_status_code = excluded.http_status_code,
                redirect_count = excluded.redirect_count,
                status = excluded.status,
                resolved_at_utc = excluded.resolved_at_utc
            """;
        command.Parameters.AddWithValue("$resolution_id", record.ResolutionId);
        command.Parameters.AddWithValue("$source_id", record.SourceId);
        command.Parameters.AddWithValue("$original_url", record.OriginalUrl);
        command.Parameters.AddWithValue("$resolved_url", record.ResolvedUrl);
        command.Parameters.AddWithValue("$http_status_code", record.HttpStatusCode);
        command.Parameters.AddWithValue("$redirect_count", record.RedirectCount);
        command.Parameters.AddWithValue("$status", record.Status.ToString());
        command.Parameters.AddWithValue("$resolved_at_utc", record.ResolvedAtUtc.ToString("O"));
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<SourceResolutionRecord> GetSourceResolutionRecords()
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT resolution_id, source_id, original_url, resolved_url, http_status_code, redirect_count, status, resolved_at_utc
            FROM source_resolution_records
            ORDER BY source_id
            """;

        var records = new List<SourceResolutionRecord>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            records.Add(ReadSourceResolutionRecord(reader));
        }

        return records;
    }

    public void UpsertSourceAudioMetadata(SourceAudioMetadata metadata)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO source_audio_metadata (
                audio_metadata_id, asset_id, duration_seconds, format, sample_rate_hz, bitrate_kbps, extracted_at_utc
            )
            VALUES (
                $audio_metadata_id, $asset_id, $duration_seconds, $format, $sample_rate_hz, $bitrate_kbps, $extracted_at_utc
            )
            ON CONFLICT(audio_metadata_id) DO UPDATE SET
                asset_id = excluded.asset_id,
                duration_seconds = excluded.duration_seconds,
                format = excluded.format,
                sample_rate_hz = excluded.sample_rate_hz,
                bitrate_kbps = excluded.bitrate_kbps,
                extracted_at_utc = excluded.extracted_at_utc
            """;
        command.Parameters.AddWithValue("$audio_metadata_id", metadata.AudioMetadataId);
        command.Parameters.AddWithValue("$asset_id", metadata.AssetId);
        command.Parameters.AddWithValue("$duration_seconds", metadata.DurationSeconds);
        command.Parameters.AddWithValue("$format", metadata.Format);
        command.Parameters.AddWithValue("$sample_rate_hz", metadata.SampleRateHz);
        command.Parameters.AddWithValue("$bitrate_kbps", metadata.BitrateKbps);
        command.Parameters.AddWithValue("$extracted_at_utc", metadata.ExtractedAtUtc.ToString("O"));
        command.ExecuteNonQuery();
    }

    public SourceAudioMetadata? GetSourceAudioMetadata(string assetId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT audio_metadata_id, asset_id, duration_seconds, format, sample_rate_hz, bitrate_kbps, extracted_at_utc
            FROM source_audio_metadata
            WHERE asset_id = $asset_id
            """;
        command.Parameters.AddWithValue("$asset_id", assetId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadSourceAudioMetadata(reader) : null;
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

    public void UpsertPublishedQuestion(PublishedQuestion question)
    {
        PublishedQuestionRules.EnsureValid(question);

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO published_questions (
                question_id,
                lesson_id,
                toeic_part,
                question_type,
                prompt,
                options_json,
                correct_answer,
                explanation,
                media_asset_id,
                passage_id,
                group_id,
                evidence_json,
                skill_tags,
                source_trace_json,
                status
            )
            VALUES (
                $question_id,
                $lesson_id,
                $toeic_part,
                $question_type,
                $prompt,
                $options_json,
                $correct_answer,
                $explanation,
                $media_asset_id,
                $passage_id,
                $group_id,
                $evidence_json,
                $skill_tags,
                $source_trace_json,
                $status
            )
            ON CONFLICT(question_id) DO UPDATE SET
                lesson_id = excluded.lesson_id,
                toeic_part = excluded.toeic_part,
                question_type = excluded.question_type,
                prompt = excluded.prompt,
                options_json = excluded.options_json,
                correct_answer = excluded.correct_answer,
                explanation = excluded.explanation,
                media_asset_id = excluded.media_asset_id,
                passage_id = excluded.passage_id,
                group_id = excluded.group_id,
                evidence_json = excluded.evidence_json,
                skill_tags = excluded.skill_tags,
                source_trace_json = excluded.source_trace_json,
                status = excluded.status
            """;
        command.Parameters.AddWithValue("$question_id", question.QuestionId);
        command.Parameters.AddWithValue("$lesson_id", question.LessonId);
        command.Parameters.AddWithValue("$toeic_part", question.ToeicPart);
        command.Parameters.AddWithValue("$question_type", question.QuestionType.ToString());
        command.Parameters.AddWithValue("$prompt", question.Prompt);
        command.Parameters.AddWithValue("$options_json", question.OptionsJson);
        command.Parameters.AddWithValue("$correct_answer", question.CorrectAnswer);
        command.Parameters.AddWithValue("$explanation", question.Explanation);
        command.Parameters.AddWithValue("$media_asset_id", (object?)question.MediaAssetId ?? DBNull.Value);
        command.Parameters.AddWithValue("$passage_id", (object?)question.PassageId ?? DBNull.Value);
        command.Parameters.AddWithValue("$group_id", (object?)question.GroupId ?? DBNull.Value);
        command.Parameters.AddWithValue("$evidence_json", question.EvidenceJson);
        command.Parameters.AddWithValue("$skill_tags", question.SkillTags);
        command.Parameters.AddWithValue("$source_trace_json", question.SourceTraceJson);
        command.Parameters.AddWithValue("$status", question.Status.ToString());
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<PublishedQuestion> GetPublishedQuestions(int toeicPart)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                question_id,
                lesson_id,
                toeic_part,
                question_type,
                prompt,
                options_json,
                correct_answer,
                explanation,
                media_asset_id,
                passage_id,
                group_id,
                evidence_json,
                skill_tags,
                source_trace_json,
                status
            FROM published_questions
            WHERE toeic_part = $toeic_part
            ORDER BY question_id
            """;
        command.Parameters.AddWithValue("$toeic_part", toeicPart);

        var questions = new List<PublishedQuestion>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            questions.Add(ReadPublishedQuestion(reader));
        }

        return questions;
    }

    public void UpsertPublishedTest(PublishedTest test)
    {
        PublishedTestRules.EnsureValid(test);

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO published_tests (
                test_id, test_mode, title, target_question_count, duration_minutes, source_trace_json, status
            )
            VALUES (
                $test_id, $test_mode, $title, $target_question_count, $duration_minutes, $source_trace_json, $status
            )
            ON CONFLICT(test_id) DO UPDATE SET
                test_mode = excluded.test_mode,
                title = excluded.title,
                target_question_count = excluded.target_question_count,
                duration_minutes = excluded.duration_minutes,
                source_trace_json = excluded.source_trace_json,
                status = excluded.status
            """;
        command.Parameters.AddWithValue("$test_id", test.TestId);
        command.Parameters.AddWithValue("$test_mode", test.TestMode.ToString());
        command.Parameters.AddWithValue("$title", test.Title);
        command.Parameters.AddWithValue("$target_question_count", test.TargetQuestionCount);
        command.Parameters.AddWithValue("$duration_minutes", test.DurationMinutes);
        command.Parameters.AddWithValue("$source_trace_json", test.SourceTraceJson);
        command.Parameters.AddWithValue("$status", test.Status.ToString());
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<PublishedTest> GetPublishedTests(PublishedTestMode testMode)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT test_id, test_mode, title, target_question_count, duration_minutes, source_trace_json, status
            FROM published_tests
            WHERE test_mode = $test_mode
            ORDER BY test_id
            """;
        command.Parameters.AddWithValue("$test_mode", testMode.ToString());

        var tests = new List<PublishedTest>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tests.Add(ReadPublishedTest(reader));
        }

        return tests;
    }

    public void UpsertPublishedTestSection(PublishedTestSection section)
    {
        PublishedTestRules.EnsureValid(section);

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO published_test_sections (
                section_id, test_id, section_type, display_order, target_question_count, duration_minutes
            )
            VALUES (
                $section_id, $test_id, $section_type, $display_order, $target_question_count, $duration_minutes
            )
            ON CONFLICT(section_id) DO UPDATE SET
                test_id = excluded.test_id,
                section_type = excluded.section_type,
                display_order = excluded.display_order,
                target_question_count = excluded.target_question_count,
                duration_minutes = excluded.duration_minutes
            """;
        command.Parameters.AddWithValue("$section_id", section.SectionId);
        command.Parameters.AddWithValue("$test_id", section.TestId);
        command.Parameters.AddWithValue("$section_type", section.SectionType.ToString());
        command.Parameters.AddWithValue("$display_order", section.DisplayOrder);
        command.Parameters.AddWithValue("$target_question_count", section.TargetQuestionCount);
        command.Parameters.AddWithValue("$duration_minutes", section.DurationMinutes);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<PublishedTestSection> GetPublishedTestSections(string testId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT section_id, test_id, section_type, display_order, target_question_count, duration_minutes
            FROM published_test_sections
            WHERE test_id = $test_id
            ORDER BY display_order, section_id
            """;
        command.Parameters.AddWithValue("$test_id", testId);

        var sections = new List<PublishedTestSection>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            sections.Add(ReadPublishedTestSection(reader));
        }

        return sections;
    }

    public void UpsertPublishedTestItem(PublishedTestItem item)
    {
        PublishedTestRules.EnsureValid(item);

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO published_test_items (
                test_item_id, section_id, question_id, toeic_part, display_order, score_weight
            )
            VALUES (
                $test_item_id, $section_id, $question_id, $toeic_part, $display_order, $score_weight
            )
            ON CONFLICT(test_item_id) DO UPDATE SET
                section_id = excluded.section_id,
                question_id = excluded.question_id,
                toeic_part = excluded.toeic_part,
                display_order = excluded.display_order,
                score_weight = excluded.score_weight
            """;
        command.Parameters.AddWithValue("$test_item_id", item.TestItemId);
        command.Parameters.AddWithValue("$section_id", item.SectionId);
        command.Parameters.AddWithValue("$question_id", item.QuestionId);
        command.Parameters.AddWithValue("$toeic_part", item.ToeicPart);
        command.Parameters.AddWithValue("$display_order", item.DisplayOrder);
        command.Parameters.AddWithValue("$score_weight", item.ScoreWeight);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<PublishedTestItem> GetPublishedTestItems(string sectionId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT test_item_id, section_id, question_id, toeic_part, display_order, score_weight
            FROM published_test_items
            WHERE section_id = $section_id
            ORDER BY display_order, test_item_id
            """;
        command.Parameters.AddWithValue("$section_id", sectionId);

        var items = new List<PublishedTestItem>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(ReadPublishedTestItem(reader));
        }

        return items;
    }

    public void UpsertLearnerProfile(LearnerProfile profile)
    {
        ValidateLearnerProfile(profile);

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO learner_profiles (
                learner_id,
                display_name,
                email,
                target_score,
                current_estimated_score,
                daily_study_minutes,
                time_zone_id,
                status,
                created_at_utc,
                updated_at_utc
            )
            VALUES (
                $learner_id,
                $display_name,
                $email,
                $target_score,
                $current_estimated_score,
                $daily_study_minutes,
                $time_zone_id,
                $status,
                $created_at_utc,
                $updated_at_utc
            )
            ON CONFLICT(learner_id) DO UPDATE SET
                display_name = excluded.display_name,
                email = excluded.email,
                target_score = excluded.target_score,
                current_estimated_score = excluded.current_estimated_score,
                daily_study_minutes = excluded.daily_study_minutes,
                time_zone_id = excluded.time_zone_id,
                status = excluded.status,
                updated_at_utc = excluded.updated_at_utc
            """;
        command.Parameters.AddWithValue("$learner_id", profile.LearnerId);
        command.Parameters.AddWithValue("$display_name", profile.DisplayName);
        command.Parameters.AddWithValue("$email", profile.Email);
        command.Parameters.AddWithValue("$target_score", profile.TargetScore);
        command.Parameters.AddWithValue("$current_estimated_score", profile.CurrentEstimatedScore);
        command.Parameters.AddWithValue("$daily_study_minutes", profile.DailyStudyMinutes);
        command.Parameters.AddWithValue("$time_zone_id", profile.TimeZoneId);
        command.Parameters.AddWithValue("$status", profile.Status.ToString());
        command.Parameters.AddWithValue("$created_at_utc", profile.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$updated_at_utc", profile.UpdatedAtUtc.ToString("O"));
        command.ExecuteNonQuery();
    }

    public LearnerProfile? GetLearnerProfile(string learnerId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                learner_id,
                display_name,
                email,
                target_score,
                current_estimated_score,
                daily_study_minutes,
                time_zone_id,
                status,
                created_at_utc,
                updated_at_utc
            FROM learner_profiles
            WHERE learner_id = $learner_id
            """;
        command.Parameters.AddWithValue("$learner_id", learnerId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadLearnerProfile(reader) : null;
    }

    public void UpsertLearnerAssignment(LearnerAssignment assignment)
    {
        LearnerWorkRules.EnsureValid(assignment);
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO learner_assignments (
                assignment_id, learner_id, assignment_type, content_ref_id, status, assigned_at_utc, due_at_utc
            )
            VALUES (
                $assignment_id, $learner_id, $assignment_type, $content_ref_id, $status, $assigned_at_utc, $due_at_utc
            )
            ON CONFLICT(assignment_id) DO UPDATE SET
                learner_id = excluded.learner_id,
                assignment_type = excluded.assignment_type,
                content_ref_id = excluded.content_ref_id,
                status = excluded.status,
                assigned_at_utc = excluded.assigned_at_utc,
                due_at_utc = excluded.due_at_utc
            """;
        command.Parameters.AddWithValue("$assignment_id", assignment.AssignmentId);
        command.Parameters.AddWithValue("$learner_id", assignment.LearnerId);
        command.Parameters.AddWithValue("$assignment_type", assignment.AssignmentType.ToString());
        command.Parameters.AddWithValue("$content_ref_id", assignment.ContentRefId);
        command.Parameters.AddWithValue("$status", assignment.Status.ToString());
        command.Parameters.AddWithValue("$assigned_at_utc", assignment.AssignedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$due_at_utc", assignment.DueAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<LearnerAssignment> GetLearnerAssignments(string learnerId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT assignment_id, learner_id, assignment_type, content_ref_id, status, assigned_at_utc, due_at_utc
            FROM learner_assignments
            WHERE learner_id = $learner_id
            ORDER BY assigned_at_utc, assignment_id
            """;
        command.Parameters.AddWithValue("$learner_id", learnerId);
        var assignments = new List<LearnerAssignment>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) assignments.Add(ReadLearnerAssignment(reader));
        return assignments;
    }

    public void UpsertActivitySession(ActivitySession session)
    {
        LearnerWorkRules.EnsureValid(session);
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO activity_sessions (
                session_id, assignment_id, learner_id, activity_type, status, started_at_utc, completed_at_utc
            )
            VALUES (
                $session_id, $assignment_id, $learner_id, $activity_type, $status, $started_at_utc, $completed_at_utc
            )
            ON CONFLICT(session_id) DO UPDATE SET
                assignment_id = excluded.assignment_id,
                learner_id = excluded.learner_id,
                activity_type = excluded.activity_type,
                status = excluded.status,
                started_at_utc = excluded.started_at_utc,
                completed_at_utc = excluded.completed_at_utc
            """;
        command.Parameters.AddWithValue("$session_id", session.SessionId);
        command.Parameters.AddWithValue("$assignment_id", session.AssignmentId);
        command.Parameters.AddWithValue("$learner_id", session.LearnerId);
        command.Parameters.AddWithValue("$activity_type", session.ActivityType.ToString());
        command.Parameters.AddWithValue("$status", session.Status.ToString());
        command.Parameters.AddWithValue("$started_at_utc", session.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$completed_at_utc", session.CompletedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<ActivitySession> GetActivitySessions(string assignmentId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT session_id, assignment_id, learner_id, activity_type, status, started_at_utc, completed_at_utc
            FROM activity_sessions
            WHERE assignment_id = $assignment_id
            ORDER BY started_at_utc, session_id
            """;
        command.Parameters.AddWithValue("$assignment_id", assignmentId);
        var sessions = new List<ActivitySession>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) sessions.Add(ReadActivitySession(reader));
        return sessions;
    }

    public void UpsertLearnerAttempt(LearnerAttempt attempt)
    {
        LearnerWorkRules.EnsureValid(attempt);
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO learner_attempts (
                attempt_id, session_id, learner_id, status, correct_count, total_count, score_percent, submitted_at_utc
            )
            VALUES (
                $attempt_id, $session_id, $learner_id, $status, $correct_count, $total_count, $score_percent, $submitted_at_utc
            )
            ON CONFLICT(attempt_id) DO UPDATE SET
                session_id = excluded.session_id,
                learner_id = excluded.learner_id,
                status = excluded.status,
                correct_count = excluded.correct_count,
                total_count = excluded.total_count,
                score_percent = excluded.score_percent,
                submitted_at_utc = excluded.submitted_at_utc
            """;
        command.Parameters.AddWithValue("$attempt_id", attempt.AttemptId);
        command.Parameters.AddWithValue("$session_id", attempt.SessionId);
        command.Parameters.AddWithValue("$learner_id", attempt.LearnerId);
        command.Parameters.AddWithValue("$status", attempt.Status.ToString());
        command.Parameters.AddWithValue("$correct_count", attempt.CorrectCount);
        command.Parameters.AddWithValue("$total_count", attempt.TotalCount);
        command.Parameters.AddWithValue("$score_percent", attempt.ScorePercent);
        command.Parameters.AddWithValue("$submitted_at_utc", attempt.SubmittedAtUtc.ToString("O"));
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<LearnerAttempt> GetLearnerAttempts(string sessionId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT attempt_id, session_id, learner_id, status, correct_count, total_count, score_percent, submitted_at_utc
            FROM learner_attempts
            WHERE session_id = $session_id
            ORDER BY submitted_at_utc, attempt_id
            """;
        command.Parameters.AddWithValue("$session_id", sessionId);
        var attempts = new List<LearnerAttempt>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) attempts.Add(ReadLearnerAttempt(reader));
        return attempts;
    }

    public void UpsertAttemptAnswer(AttemptAnswer answer)
    {
        LearnerWorkRules.EnsureValid(answer);
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO attempt_answers (
                answer_id, attempt_id, question_id, learner_answer, correct_answer, is_correct, answered_at_utc
            )
            VALUES (
                $answer_id, $attempt_id, $question_id, $learner_answer, $correct_answer, $is_correct, $answered_at_utc
            )
            ON CONFLICT(answer_id) DO UPDATE SET
                attempt_id = excluded.attempt_id,
                question_id = excluded.question_id,
                learner_answer = excluded.learner_answer,
                correct_answer = excluded.correct_answer,
                is_correct = excluded.is_correct,
                answered_at_utc = excluded.answered_at_utc
            """;
        command.Parameters.AddWithValue("$answer_id", answer.AnswerId);
        command.Parameters.AddWithValue("$attempt_id", answer.AttemptId);
        command.Parameters.AddWithValue("$question_id", answer.QuestionId);
        command.Parameters.AddWithValue("$learner_answer", answer.LearnerAnswer);
        command.Parameters.AddWithValue("$correct_answer", answer.CorrectAnswer);
        command.Parameters.AddWithValue("$is_correct", answer.IsCorrect ? 1 : 0);
        command.Parameters.AddWithValue("$answered_at_utc", answer.AnsweredAtUtc.ToString("O"));
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<AttemptAnswer> GetAttemptAnswers(string attemptId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT answer_id, attempt_id, question_id, learner_answer, correct_answer, is_correct, answered_at_utc
            FROM attempt_answers
            WHERE attempt_id = $attempt_id
            ORDER BY answered_at_utc, answer_id
            """;
        command.Parameters.AddWithValue("$attempt_id", attemptId);
        var answers = new List<AttemptAnswer>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) answers.Add(ReadAttemptAnswer(reader));
        return answers;
    }

    public void UpsertReviewItem(ReviewItem item)
    {
        ReviewMasteryRules.EnsureValid(item);
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO review_items (
                review_item_id, learner_id, source_attempt_id, question_id, unit_id, error_tag,
                learner_answer, correct_answer, status, is_blocking, created_at_utc, resolved_at_utc
            )
            VALUES (
                $review_item_id, $learner_id, $source_attempt_id, $question_id, $unit_id, $error_tag,
                $learner_answer, $correct_answer, $status, $is_blocking, $created_at_utc, $resolved_at_utc
            )
            ON CONFLICT(review_item_id) DO UPDATE SET
                learner_id = excluded.learner_id,
                source_attempt_id = excluded.source_attempt_id,
                question_id = excluded.question_id,
                unit_id = excluded.unit_id,
                error_tag = excluded.error_tag,
                learner_answer = excluded.learner_answer,
                correct_answer = excluded.correct_answer,
                status = excluded.status,
                is_blocking = excluded.is_blocking,
                created_at_utc = excluded.created_at_utc,
                resolved_at_utc = excluded.resolved_at_utc
            """;
        command.Parameters.AddWithValue("$review_item_id", item.ReviewItemId);
        command.Parameters.AddWithValue("$learner_id", item.LearnerId);
        command.Parameters.AddWithValue("$source_attempt_id", item.SourceAttemptId);
        command.Parameters.AddWithValue("$question_id", item.QuestionId);
        command.Parameters.AddWithValue("$unit_id", item.UnitId);
        command.Parameters.AddWithValue("$error_tag", item.ErrorTag);
        command.Parameters.AddWithValue("$learner_answer", item.LearnerAnswer);
        command.Parameters.AddWithValue("$correct_answer", item.CorrectAnswer);
        command.Parameters.AddWithValue("$status", item.Status.ToString());
        command.Parameters.AddWithValue("$is_blocking", item.IsBlocking ? 1 : 0);
        command.Parameters.AddWithValue("$created_at_utc", item.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$resolved_at_utc", item.ResolvedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<ReviewItem> GetReviewItems(string learnerId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT review_item_id, learner_id, source_attempt_id, question_id, unit_id, error_tag,
                learner_answer, correct_answer, status, is_blocking, created_at_utc, resolved_at_utc
            FROM review_items
            WHERE learner_id = $learner_id
            ORDER BY created_at_utc, review_item_id
            """;
        command.Parameters.AddWithValue("$learner_id", learnerId);
        var items = new List<ReviewItem>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) items.Add(ReadReviewItem(reader));
        return items;
    }

    public void UpsertRepairAttempt(RepairAttempt attempt)
    {
        ReviewMasteryRules.EnsureValid(attempt);
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO repair_attempts (
                repair_attempt_id, review_item_id, learner_id, answer, is_correct, attempted_at_utc
            )
            VALUES (
                $repair_attempt_id, $review_item_id, $learner_id, $answer, $is_correct, $attempted_at_utc
            )
            ON CONFLICT(repair_attempt_id) DO UPDATE SET
                review_item_id = excluded.review_item_id,
                learner_id = excluded.learner_id,
                answer = excluded.answer,
                is_correct = excluded.is_correct,
                attempted_at_utc = excluded.attempted_at_utc
            """;
        command.Parameters.AddWithValue("$repair_attempt_id", attempt.RepairAttemptId);
        command.Parameters.AddWithValue("$review_item_id", attempt.ReviewItemId);
        command.Parameters.AddWithValue("$learner_id", attempt.LearnerId);
        command.Parameters.AddWithValue("$answer", attempt.Answer);
        command.Parameters.AddWithValue("$is_correct", attempt.IsCorrect ? 1 : 0);
        command.Parameters.AddWithValue("$attempted_at_utc", attempt.AttemptedAtUtc.ToString("O"));
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<RepairAttempt> GetRepairAttempts(string reviewItemId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT repair_attempt_id, review_item_id, learner_id, answer, is_correct, attempted_at_utc
            FROM repair_attempts
            WHERE review_item_id = $review_item_id
            ORDER BY attempted_at_utc, repair_attempt_id
            """;
        command.Parameters.AddWithValue("$review_item_id", reviewItemId);
        var attempts = new List<RepairAttempt>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) attempts.Add(ReadRepairAttempt(reader));
        return attempts;
    }

    public void UpsertMasteryRecord(MasteryRecord record)
    {
        ReviewMasteryRules.EnsureValid(record);
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO mastery_records (
                mastery_record_id, learner_id, unit_id, mastery_percent, is_unlocked, blocking_review_count, updated_at_utc
            )
            VALUES (
                $mastery_record_id, $learner_id, $unit_id, $mastery_percent, $is_unlocked, $blocking_review_count, $updated_at_utc
            )
            ON CONFLICT(mastery_record_id) DO UPDATE SET
                learner_id = excluded.learner_id,
                unit_id = excluded.unit_id,
                mastery_percent = excluded.mastery_percent,
                is_unlocked = excluded.is_unlocked,
                blocking_review_count = excluded.blocking_review_count,
                updated_at_utc = excluded.updated_at_utc
            """;
        command.Parameters.AddWithValue("$mastery_record_id", record.MasteryRecordId);
        command.Parameters.AddWithValue("$learner_id", record.LearnerId);
        command.Parameters.AddWithValue("$unit_id", record.UnitId);
        command.Parameters.AddWithValue("$mastery_percent", record.MasteryPercent);
        command.Parameters.AddWithValue("$is_unlocked", record.IsUnlocked ? 1 : 0);
        command.Parameters.AddWithValue("$blocking_review_count", record.BlockingReviewCount);
        command.Parameters.AddWithValue("$updated_at_utc", record.UpdatedAtUtc.ToString("O"));
        command.ExecuteNonQuery();
    }

    public MasteryRecord? GetMasteryRecord(string learnerId, string unitId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT mastery_record_id, learner_id, unit_id, mastery_percent, is_unlocked, blocking_review_count, updated_at_utc
            FROM mastery_records
            WHERE learner_id = $learner_id AND unit_id = $unit_id
            """;
        command.Parameters.AddWithValue("$learner_id", learnerId);
        command.Parameters.AddWithValue("$unit_id", unitId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadMasteryRecord(reader) : null;
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
            or "source_discovery_issues"
            or "source_resolution_records"
            or "source_audio_metadata"
            or "extracted_pages"
            or "extracted_text_blocks"
            or "draft_content_items"
            or "published_lessons"
            or "guided_examples"
            or "published_questions"
            or "published_tests"
            or "published_test_sections"
            or "published_test_items"
            or "learner_profiles"
            or "learner_assignments"
            or "activity_sessions"
            or "learner_attempts"
            or "attempt_answers"
            or "review_items"
            or "repair_attempts"
            or "mastery_records"
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

    private static SourceDiscoveryIssue ReadSourceDiscoveryIssue(SqliteDataReader reader)
    {
        return new SourceDiscoveryIssue(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            Enum.Parse<SourceDiscoveryIssueStatus>(reader.GetString(4)),
            DateTimeOffset.Parse(reader.GetString(5))
        );
    }

    private static SourceResolutionRecord ReadSourceResolutionRecord(SqliteDataReader reader)
    {
        return new SourceResolutionRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            Enum.Parse<SourceResolutionStatus>(reader.GetString(6)),
            DateTimeOffset.Parse(reader.GetString(7))
        );
    }

    private static SourceAudioMetadata ReadSourceAudioMetadata(SqliteDataReader reader)
    {
        return new SourceAudioMetadata(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetString(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            DateTimeOffset.Parse(reader.GetString(6))
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

    private static PublishedQuestion ReadPublishedQuestion(SqliteDataReader reader)
    {
        return new PublishedQuestion(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt32(2),
            Enum.Parse<PublishedQuestionType>(reader.GetString(3)),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.GetString(11),
            reader.GetString(12),
            reader.GetString(13),
            Enum.Parse<PublishedContentStatus>(reader.GetString(14))
        );
    }

    private static PublishedTest ReadPublishedTest(SqliteDataReader reader)
    {
        return new PublishedTest(
            reader.GetString(0),
            Enum.Parse<PublishedTestMode>(reader.GetString(1)),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetString(5),
            Enum.Parse<PublishedContentStatus>(reader.GetString(6))
        );
    }

    private static PublishedTestSection ReadPublishedTestSection(SqliteDataReader reader)
    {
        return new PublishedTestSection(
            reader.GetString(0),
            reader.GetString(1),
            Enum.Parse<ToeicTestSectionType>(reader.GetString(2)),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetInt32(5)
        );
    }

    private static PublishedTestItem ReadPublishedTestItem(SqliteDataReader reader)
    {
        return new PublishedTestItem(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetDecimal(5)
        );
    }

    private static LearnerProfile ReadLearnerProfile(SqliteDataReader reader)
    {
        return new LearnerProfile(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetString(6),
            Enum.Parse<LearnerProfileStatus>(reader.GetString(7)),
            DateTimeOffset.Parse(reader.GetString(8)),
            DateTimeOffset.Parse(reader.GetString(9))
        );
    }

    private static void ValidateLearnerProfile(LearnerProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.LearnerId)
            || string.IsNullOrWhiteSpace(profile.DisplayName)
            || string.IsNullOrWhiteSpace(profile.Email)
            || string.IsNullOrWhiteSpace(profile.TimeZoneId))
        {
            throw new InvalidOperationException("Learner profile identity, display name, email, and timezone are required.");
        }

        if (profile.TargetScore is < 10 or > 990 || profile.CurrentEstimatedScore is < 0 or > 990)
        {
            throw new InvalidOperationException("Learner TOEIC scores must be within TOEIC score bounds.");
        }

        if (profile.DailyStudyMinutes <= 0)
        {
            throw new InvalidOperationException("Learner daily study minutes must be positive.");
        }
    }

    private static LearnerAssignment ReadLearnerAssignment(SqliteDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            Enum.Parse<LearnerAssignmentType>(reader.GetString(2)),
            reader.GetString(3),
            Enum.Parse<LearnerAssignmentStatus>(reader.GetString(4)),
            DateTimeOffset.Parse(reader.GetString(5)),
            reader.IsDBNull(6) ? null : DateTimeOffset.Parse(reader.GetString(6))
        );

    private static ActivitySession ReadActivitySession(SqliteDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            Enum.Parse<LearnerAssignmentType>(reader.GetString(3)),
            Enum.Parse<ActivitySessionStatus>(reader.GetString(4)),
            DateTimeOffset.Parse(reader.GetString(5)),
            reader.IsDBNull(6) ? null : DateTimeOffset.Parse(reader.GetString(6))
        );

    private static LearnerAttempt ReadLearnerAttempt(SqliteDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            Enum.Parse<LearnerAttemptStatus>(reader.GetString(3)),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetInt32(6),
            DateTimeOffset.Parse(reader.GetString(7))
        );

    private static AttemptAnswer ReadAttemptAnswer(SqliteDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetInt32(5) == 1,
            DateTimeOffset.Parse(reader.GetString(6))
        );

    private static ReviewItem ReadReviewItem(SqliteDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            Enum.Parse<ReviewItemStatus>(reader.GetString(8)),
            reader.GetInt32(9) == 1,
            DateTimeOffset.Parse(reader.GetString(10)),
            reader.IsDBNull(11) ? null : DateTimeOffset.Parse(reader.GetString(11))
        );

    private static RepairAttempt ReadRepairAttempt(SqliteDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetInt32(4) == 1,
            DateTimeOffset.Parse(reader.GetString(5))
        );

    private static MasteryRecord ReadMasteryRecord(SqliteDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetInt32(4) == 1,
            reader.GetInt32(5),
            DateTimeOffset.Parse(reader.GetString(6))
        );

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

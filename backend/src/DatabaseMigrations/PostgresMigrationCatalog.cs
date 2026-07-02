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
        new(
            "003_extracted_content",
            """
            CREATE TABLE IF NOT EXISTS extracted_pages (
                page_id varchar(160) PRIMARY KEY,
                asset_id varchar(160) NOT NULL REFERENCES source_assets(asset_id),
                page_number integer NOT NULL,
                width integer NOT NULL,
                height integer NOT NULL,
                extracted_at_utc timestamptz NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS idx_extracted_pages_asset_page
                ON extracted_pages(asset_id, page_number);

            CREATE TABLE IF NOT EXISTS extracted_text_blocks (
                block_id varchar(160) PRIMARY KEY,
                asset_id varchar(160) NOT NULL REFERENCES source_assets(asset_id),
                page_id varchar(160) NOT NULL REFERENCES extracted_pages(page_id),
                page_number integer NOT NULL,
                block_type varchar(80) NOT NULL,
                text text NOT NULL,
                confidence numeric(5,4) NOT NULL,
                coordinates_json jsonb NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_extracted_text_blocks_asset_page
                ON extracted_text_blocks(asset_id, page_number);
            """
        ),
        new(
            "004_draft_content",
            """
            CREATE TABLE IF NOT EXISTS draft_content_items (
                draft_id varchar(160) PRIMARY KEY,
                asset_id varchar(160) NOT NULL REFERENCES source_assets(asset_id),
                material_class varchar(80) NOT NULL,
                toeic_part integer,
                item_type varchar(80) NOT NULL,
                payload_json jsonb NOT NULL,
                source_trace_json jsonb NOT NULL,
                parser_confidence numeric(5,4) NOT NULL,
                status varchar(80) NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_draft_content_items_asset_status
                ON draft_content_items(asset_id, status);
            """
        ),
        new(
            "005_published_lessons",
            """
            CREATE TABLE IF NOT EXISTS published_lessons (
                lesson_id varchar(160) PRIMARY KEY,
                unit_id varchar(160) NOT NULL,
                toeic_part integer NOT NULL,
                title text NOT NULL,
                objective text NOT NULL,
                skill_tags text NOT NULL,
                source_trace_json jsonb NOT NULL,
                status varchar(80) NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_published_lessons_unit_status
                ON published_lessons(unit_id, status);

            CREATE TABLE IF NOT EXISTS guided_examples (
                example_id varchar(160) PRIMARY KEY,
                lesson_id varchar(160) NOT NULL REFERENCES published_lessons(lesson_id),
                prompt text NOT NULL,
                explanation text NOT NULL,
                display_order integer NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_guided_examples_lesson_order
                ON guided_examples(lesson_id, display_order);
            """
        ),
        new(
            "006_published_questions",
            """
            CREATE TABLE IF NOT EXISTS published_questions (
                question_id varchar(160) PRIMARY KEY,
                lesson_id varchar(160) NOT NULL REFERENCES published_lessons(lesson_id),
                toeic_part integer NOT NULL,
                question_type varchar(80) NOT NULL,
                prompt text NOT NULL,
                options_json jsonb NOT NULL,
                correct_answer varchar(16) NOT NULL,
                explanation text NOT NULL,
                media_asset_id varchar(160) REFERENCES source_assets(asset_id),
                passage_id varchar(160),
                group_id varchar(160),
                evidence_json jsonb NOT NULL,
                skill_tags text NOT NULL,
                source_trace_json jsonb NOT NULL,
                status varchar(80) NOT NULL,
                CONSTRAINT ck_published_questions_toeic_part CHECK (toeic_part BETWEEN 1 AND 7),
                CONSTRAINT ck_published_questions_part1_media CHECK (toeic_part <> 1 OR media_asset_id IS NOT NULL),
                CONSTRAINT ck_published_questions_part34_group CHECK (toeic_part NOT IN (3, 4) OR group_id IS NOT NULL),
                CONSTRAINT ck_published_questions_part67_passage CHECK (toeic_part NOT IN (6, 7) OR passage_id IS NOT NULL)
            );

            CREATE INDEX IF NOT EXISTS idx_published_questions_part_status
                ON published_questions(toeic_part, status);

            CREATE INDEX IF NOT EXISTS idx_published_questions_lesson
                ON published_questions(lesson_id);
            """
        ),
        new(
            "007_published_tests",
            """
            CREATE TABLE IF NOT EXISTS published_tests (
                test_id varchar(160) PRIMARY KEY,
                test_mode varchar(80) NOT NULL,
                title text NOT NULL,
                target_question_count integer NOT NULL,
                duration_minutes integer NOT NULL,
                source_trace_json jsonb NOT NULL,
                status varchar(80) NOT NULL,
                CONSTRAINT ck_published_tests_positive_counts CHECK (target_question_count > 0 AND duration_minutes > 0),
                CONSTRAINT ck_published_tests_full_count CHECK (test_mode <> 'Full' OR target_question_count = 200)
            );

            CREATE INDEX IF NOT EXISTS idx_published_tests_mode_status
                ON published_tests(test_mode, status);

            CREATE TABLE IF NOT EXISTS published_test_sections (
                section_id varchar(160) PRIMARY KEY,
                test_id varchar(160) NOT NULL REFERENCES published_tests(test_id),
                section_type varchar(80) NOT NULL,
                display_order integer NOT NULL,
                target_question_count integer NOT NULL,
                duration_minutes integer NOT NULL,
                CONSTRAINT ck_published_test_sections_positive_counts
                    CHECK (display_order > 0 AND target_question_count > 0 AND duration_minutes > 0)
            );

            CREATE INDEX IF NOT EXISTS idx_published_test_sections_test_order
                ON published_test_sections(test_id, display_order);

            CREATE TABLE IF NOT EXISTS published_test_items (
                test_item_id varchar(160) PRIMARY KEY,
                section_id varchar(160) NOT NULL REFERENCES published_test_sections(section_id),
                question_id varchar(160) NOT NULL,
                toeic_part integer NOT NULL,
                display_order integer NOT NULL,
                score_weight numeric(8,4) NOT NULL,
                CONSTRAINT ck_published_test_items_toeic_part CHECK (toeic_part BETWEEN 1 AND 7),
                CONSTRAINT ck_published_test_items_positive_order_weight CHECK (display_order > 0 AND score_weight > 0)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS idx_published_test_items_section_order
                ON published_test_items(section_id, display_order);
            """
        ),
        new(
            "008_learner_profiles",
            """
            CREATE TABLE IF NOT EXISTS learner_profiles (
                learner_id varchar(160) PRIMARY KEY,
                display_name text NOT NULL,
                email text NOT NULL,
                target_score integer NOT NULL,
                current_estimated_score integer NOT NULL,
                daily_study_minutes integer NOT NULL,
                time_zone_id varchar(120) NOT NULL,
                status varchar(80) NOT NULL,
                created_at_utc timestamptz NOT NULL,
                updated_at_utc timestamptz NOT NULL,
                CONSTRAINT ck_learner_profiles_target_score CHECK (target_score BETWEEN 10 AND 990),
                CONSTRAINT ck_learner_profiles_estimated_score CHECK (current_estimated_score BETWEEN 0 AND 990),
                CONSTRAINT ck_learner_profiles_daily_minutes CHECK (daily_study_minutes > 0)
            );

            CREATE INDEX IF NOT EXISTS idx_learner_profiles_status
                ON learner_profiles(status);
            """
        ),
        new(
            "009_learner_assignments_attempts",
            """
            CREATE TABLE IF NOT EXISTS learner_assignments (
                assignment_id varchar(160) PRIMARY KEY,
                learner_id varchar(160) NOT NULL REFERENCES learner_profiles(learner_id),
                assignment_type varchar(80) NOT NULL,
                content_ref_id varchar(160) NOT NULL,
                status varchar(80) NOT NULL,
                assigned_at_utc timestamptz NOT NULL,
                due_at_utc timestamptz
            );

            CREATE INDEX IF NOT EXISTS idx_learner_assignments_learner_status
                ON learner_assignments(learner_id, status);

            CREATE TABLE IF NOT EXISTS activity_sessions (
                session_id varchar(160) PRIMARY KEY,
                assignment_id varchar(160) NOT NULL REFERENCES learner_assignments(assignment_id),
                learner_id varchar(160) NOT NULL REFERENCES learner_profiles(learner_id),
                activity_type varchar(80) NOT NULL,
                status varchar(80) NOT NULL,
                started_at_utc timestamptz NOT NULL,
                completed_at_utc timestamptz
            );

            CREATE INDEX IF NOT EXISTS idx_activity_sessions_assignment
                ON activity_sessions(assignment_id);

            CREATE TABLE IF NOT EXISTS learner_attempts (
                attempt_id varchar(160) PRIMARY KEY,
                session_id varchar(160) NOT NULL REFERENCES activity_sessions(session_id),
                learner_id varchar(160) NOT NULL REFERENCES learner_profiles(learner_id),
                status varchar(80) NOT NULL,
                correct_count integer NOT NULL,
                total_count integer NOT NULL,
                score_percent integer NOT NULL,
                submitted_at_utc timestamptz NOT NULL,
                CONSTRAINT ck_learner_attempts_counts CHECK (
                    total_count > 0 AND correct_count >= 0 AND correct_count <= total_count
                ),
                CONSTRAINT ck_learner_attempts_score_percent CHECK (score_percent BETWEEN 0 AND 100)
            );

            CREATE INDEX IF NOT EXISTS idx_learner_attempts_session
                ON learner_attempts(session_id);

            CREATE TABLE IF NOT EXISTS attempt_answers (
                answer_id varchar(160) PRIMARY KEY,
                attempt_id varchar(160) NOT NULL REFERENCES learner_attempts(attempt_id),
                question_id varchar(160) NOT NULL,
                learner_answer text NOT NULL,
                correct_answer text NOT NULL,
                is_correct boolean NOT NULL,
                answered_at_utc timestamptz NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_attempt_answers_attempt
                ON attempt_answers(attempt_id);
            """
        ),
        new(
            "010_review_mastery_records",
            """
            CREATE TABLE IF NOT EXISTS review_items (
                review_item_id varchar(160) PRIMARY KEY,
                learner_id varchar(160) NOT NULL REFERENCES learner_profiles(learner_id),
                source_attempt_id varchar(160) NOT NULL,
                question_id varchar(160) NOT NULL,
                unit_id varchar(160) NOT NULL,
                error_tag varchar(120) NOT NULL,
                learner_answer text NOT NULL,
                correct_answer text NOT NULL,
                status varchar(80) NOT NULL,
                is_blocking boolean NOT NULL,
                created_at_utc timestamptz NOT NULL,
                resolved_at_utc timestamptz
            );

            CREATE INDEX IF NOT EXISTS idx_review_items_learner_status
                ON review_items(learner_id, status, is_blocking);

            CREATE TABLE IF NOT EXISTS repair_attempts (
                repair_attempt_id varchar(160) PRIMARY KEY,
                review_item_id varchar(160) NOT NULL REFERENCES review_items(review_item_id),
                learner_id varchar(160) NOT NULL REFERENCES learner_profiles(learner_id),
                answer text NOT NULL,
                is_correct boolean NOT NULL,
                attempted_at_utc timestamptz NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_repair_attempts_review
                ON repair_attempts(review_item_id, attempted_at_utc);

            CREATE TABLE IF NOT EXISTS mastery_records (
                mastery_record_id varchar(160) PRIMARY KEY,
                learner_id varchar(160) NOT NULL REFERENCES learner_profiles(learner_id),
                unit_id varchar(160) NOT NULL,
                mastery_percent integer NOT NULL,
                is_unlocked boolean NOT NULL,
                blocking_review_count integer NOT NULL,
                updated_at_utc timestamptz NOT NULL,
                CONSTRAINT ck_mastery_records_percent CHECK (mastery_percent BETWEEN 0 AND 100),
                CONSTRAINT ck_mastery_records_blocking_count CHECK (blocking_review_count >= 0)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS idx_mastery_records_learner_unit
                ON mastery_records(learner_id, unit_id);
            """
        ),
        new(
            "011_toeic_data_integrity",
            """
            CREATE INDEX IF NOT EXISTS idx_review_items_blocking_unlock
                ON review_items(learner_id, unit_id, is_blocking, status);

            CREATE INDEX IF NOT EXISTS idx_attempt_answers_question
                ON attempt_answers(question_id, is_correct);

            CREATE INDEX IF NOT EXISTS idx_published_questions_media
                ON published_questions(toeic_part, media_asset_id, passage_id, group_id);

            CREATE INDEX IF NOT EXISTS idx_learner_attempts_learner_submitted
                ON learner_attempts(learner_id, submitted_at_utc);

            CREATE INDEX IF NOT EXISTS idx_mastery_records_unlock_lookup
                ON mastery_records(learner_id, unit_id, is_unlocked, blocking_review_count);
            """
        ),
        new(
            "012_source_discovery_issues",
            """
            CREATE TABLE IF NOT EXISTS source_discovery_issues (
                issue_id varchar(160) PRIMARY KEY,
                source_id varchar(160) NOT NULL REFERENCES source_manifest_entries(source_id),
                issue_code varchar(120) NOT NULL,
                message text NOT NULL,
                status varchar(80) NOT NULL,
                created_at_utc timestamptz NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_source_discovery_issues_source_status
                ON source_discovery_issues(source_id, status);
            """
        ),
        new(
            "013_source_resolution_records",
            """
            CREATE TABLE IF NOT EXISTS source_resolution_records (
                resolution_id varchar(160) PRIMARY KEY,
                source_id varchar(160) NOT NULL REFERENCES source_manifest_entries(source_id),
                original_url text NOT NULL,
                resolved_url text NOT NULL,
                http_status_code integer NOT NULL,
                redirect_count integer NOT NULL,
                status varchar(80) NOT NULL,
                resolved_at_utc timestamptz NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_source_resolution_records_source_status
                ON source_resolution_records(source_id, status);
            """
        ),
    ];
}

public sealed record PostgresMigration(string Id, string SqlStatements);

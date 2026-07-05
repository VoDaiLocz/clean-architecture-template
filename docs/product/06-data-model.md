# Data Model Specification

## Content Factory Tables

### source_manifest_entries

Purpose: source row inventory imported from Google Sheet and other manifests.

Required fields:

- source_id
- sheet_row_number
- title
- original_url
- resolved_url
- provider
- source_type
- material_class
- access_status
- evidence_flags
- last_checked_at

### source_containers

Purpose: folders, shared locations, or resolved external containers.

Required fields:

- container_id
- source_id
- provider
- external_id
- title
- access_status
- discovered_at

### source_assets

Purpose: concrete PDF/audio/image/video/doc/web assets.

Required fields:

- asset_id
- container_id
- source_id
- file_name
- mime_type
- extension
- size_bytes
- detected_role
- provider_url
- storage_url
- checksum

### extracted_text_blocks

Purpose: raw machine-readable content from PDFs/web/docs.

Required fields:

- block_id
- asset_id
- page_number
- block_type
- text
- confidence
- coordinates_json

### draft_content_items

Purpose: parser output awaiting validation/review.

Required fields:

- draft_id
- material_class
- toeic_part
- item_type
- payload_json
- source_trace_json
- parser_confidence
- status

### validation_issues

Purpose: explain why draft content cannot publish.

Required fields:

- issue_id
- draft_id
- issue_code
- severity
- message
- required_action
- status

## Published Content Tables

Must support:

- lessons
- guided examples
- questions
- question groups
- passages
- evidence spans
- tests
- media assets
- transcripts
- answer keys

Published content must include source trace for admin audit, but source trace must not appear in learner UI.

## Learner Tables

Must support:

- learner_profiles
- placement_sessions
- placement_results
- learning_paths
- learning_units
- learner_assignments
- activity_sessions
- attempts
- attempt_answers
- review_items
- repair_attempts
- mastery_records
- test_results

## Data Integrity Rules

- Published listening question requires audio asset.
- Published Part 1 question requires image asset.
- Published Part 7 question requires passage and evidence span.
- Attempt must reference learner and assignment/test session.
- Review item must reference attempt or content item.
- Mastery record must reference learner and learning unit.

## Local Runtime Database Rule

Local development must use `backend/src/Api/toeic-normalization.db` as the active SQLite database until a dedicated corpus promotion job exists. This database currently contains the real source inventory and extracted block evidence. `toeic_knowledge.db` is not the content source of truth because it has learner demo state but no source assets, extracted blocks, drafts, published lessons, published questions, or published tests.

The local API configuration must therefore point `ConnectionStrings:ToeicDb` to:

```text
Data Source=toeic-normalization.db
```

If a future task introduces a promotion process into another runtime DB, that task must prove the promoted DB has non-zero source inventory and published content counts before changing this rule.

# Extracted Content Data Model

## Purpose

P2.2 stores machine-readable extraction output from source assets before parser normalization.

This layer preserves evidence from PDFs, documents, web pages, and future media extraction without exposing raw extraction output to learners.

## Domain Model

Domain records:

- `ExtractedPage`
- `ExtractedTextBlock`
- `ExtractedBlockType`

Block types:

- `Heading`
- `Paragraph`
- `Question`
- `AnswerOption`
- `Table`
- `Caption`
- `Unknown`

## Repository Contract

Repository methods:

- `UpsertExtractedPage`
- `GetExtractedPages`
- `UpsertExtractedTextBlock`
- `GetExtractedTextBlocks`

Upserts are idempotent by primary key.

## Tables

SQLite local/test tables:

- `extracted_pages`
- `extracted_text_blocks`

PostgreSQL migration:

- `003_extracted_content`

Indexes:

- `idx_extracted_pages_asset_page`
- `idx_extracted_text_blocks_asset_page`

## Data Rules

1. An extracted page belongs to a source asset.
2. An extracted text block belongs to a source asset and extracted page.
3. Block confidence must be persisted for validation and review.
4. Coordinates are stored as JSON for parser evidence.
5. Extraction output is draft/evidence data, not learner-facing curriculum.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ExtractedPage|ExtractedTextBlock|003_extracted_content|extracted_text_blocks" backend/src backend/tests docs/product
```

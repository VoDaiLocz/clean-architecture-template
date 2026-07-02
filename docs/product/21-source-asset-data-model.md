# Source Asset Data Model

## Purpose

P2.1 models TOEIC source containers and concrete source assets so the system can track real PDFs, audio, images, transcripts, answer keys, documents, and web assets before extraction.

This is the first production data foundation step for the content factory.

## Domain Model

Domain records:

- `SourceContainer`
- `SourceAsset`
- `SourceAssetRole`

Asset roles:

- `Pdf`
- `Audio`
- `Image`
- `Transcript`
- `AnswerKey`
- `Document`
- `WebPage`
- `Unknown`

## Repository Contract

Repository methods:

- `UpsertSourceContainer`
- `GetSourceContainers`
- `UpsertSourceAsset`
- `GetSourceAssets`

Upserts are idempotent by primary key.

## Tables

SQLite local/test tables:

- `source_containers`
- `source_assets`

PostgreSQL migration:

- `002_source_assets`

Indexes:

- `idx_source_containers_source_id`
- `idx_source_assets_container_id`
- `idx_source_assets_source_role`

## Data Rules

1. A source container belongs to a source manifest entry.
2. A source asset belongs to a source container and source manifest entry.
3. Source assets store metadata and object key, not raw bytes.
4. Asset role is explicit and queryable.
5. Upsert by id must update existing rows instead of duplicating them.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "SourceContainer|SourceAsset|002_source_assets|source_assets" backend/src backend/tests docs/product
```

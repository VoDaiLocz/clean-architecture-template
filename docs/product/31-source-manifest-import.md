# TOEIC Source Manifest Import

## Purpose

P3.1 imports the audited TOEIC source manifest into normalized database inventory.

This is not learner content publication. It is the first content factory step that turns the user's source list into durable, queryable source rows.

## Import Contract

Use case:

- `ImportToeicSourceManifestHandler`

Result contract:

- `ImportedCount`
- `AccessibleCount`
- `BlockedCount`
- `SourcesWithPdf`
- `SourcesWithAudio`
- `SourcesWithImage`
- `SourcesWithTranscript`
- `SourcesWithAnswerKey`

## Audit Counts

Expected counts:

- imported rows: 73
- accessible rows: 60
- blocked rows: 13
- sources with PDF evidence: 33
- sources with audio evidence: 20
- sources with image evidence: 11
- sources with transcript evidence: 6
- sources with answer-key evidence: 5

## Data Rules

1. Each source id is stable: `sheet-row-{rowNumber}`.
2. Each source is classified by provider, source type, material class, access status, and evidence flags.
3. Blocked sources remain durable inventory rows.
4. Repeated imports update existing rows and must not duplicate rows.
5. Learner APIs still cannot expose raw source manifest rows as learning content.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ImportToeicSourceManifestResult|AccessibleCount|SourcesWithImage|P3.1" backend/src backend/tests docs/product
```

# Draft Content Data Model

## Purpose

P2.3 stores parser output as draft content that must pass validation/review before learners can see it.

Draft content is content factory data. It is not learner curriculum.

## Domain Model

Domain records:

- `DraftContentItem`
- `DraftContentStatus`

Statuses:

- `PendingValidation`
- `ValidationFailed`
- `ReadyForReview`
- `Approved`
- `Rejected`
- `Published`

## Repository Contract

Repository methods:

- `UpsertDraftContentItem`
- `GetDraftContentItems`

Upserts are idempotent by draft id.

## Tables

SQLite local/test table:

- `draft_content_items`

PostgreSQL migration:

- `004_draft_content`

Index:

- `idx_draft_content_items_asset_status`

## Data Rules

1. Draft content belongs to a source asset.
2. Draft content stores parser payload and source trace JSON.
3. Parser confidence must be persisted.
4. Draft status must be explicit.
5. Learner API contracts must not expose draft content.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "DraftContentItem|DraftContentStatus|004_draft_content|draft_content_items" backend/src backend/tests docs/product
```

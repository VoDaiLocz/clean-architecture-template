# Review And Publish Workflow

## Purpose

P3.12 applies human review decisions to validated draft content.

Approved drafts can become learner-ready published questions. Rejected drafts remain hidden and must not create learner content.

## Application Contract

Handler:

- `ReviewAndPublishToeicContentHandler`

Command:

- `ReviewAndPublishToeicContentCommand`

Decision:

- `ReviewDecision`

Result:

- `PublishedCount`
- `RejectedCount`

## Rules

1. Only `ReadyForReview` drafts can receive review decisions.
2. Approved question drafts create `PublishedQuestion` rows.
3. Approved drafts transition to `Published`.
4. Rejected drafts transition to `Rejected`.
5. Rejected drafts must not create published content.
6. Published questions preserve source trace and evidence.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ReviewAndPublishToeicContent|ReviewDecision|review publish" backend/src backend/tests docs/product
```

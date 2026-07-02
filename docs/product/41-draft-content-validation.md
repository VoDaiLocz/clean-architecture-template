# Draft Content Validation

## Purpose

P3.11 validates TOEIC parser draft content before review and publishing.

Invalid parser output must become actionable validation issues, not learner-visible content.

## Application Contract

Handler:

- `ValidateToeicDraftContentHandler`

Result:

- `ValidDraftCount`
- `InvalidDraftCount`

## Validation Rules

1. Parser confidence must be at least the validation threshold.
2. Source trace is required.
3. Draft questions require a prompt.
4. Part 3 and Part 4 drafts require group relationship.
5. Part 6 and Part 7 drafts require passage context.

## Status Rules

Valid draft:

- `ReadyForReview`

Invalid draft:

- `ValidationFailed`
- issue row in `validation_issues`

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ValidateToeicDraftContent|ValidationFailed|draft content validation" backend/src backend/tests docs/product
```

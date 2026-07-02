# Answer Key Parsing

## Purpose

P3.7 parses answer key source assets into draft answer mapping records.

Answer mappings are draft content. They are not learner-visible until later validation and publish workflows approve them.

## Application Contract

Handler:

- `ParseToeicAnswerKeysHandler`

Parser:

- `IAnswerKeyParser`

Result:

- `CreatedDraftMappingCount`

## Draft Contract

Draft item type:

- `AnswerKeyMapping`

Payload fields:

- `testId`
- `questionNumber`
- `correctAnswer`

## Data Rules

1. Only `AnswerKey` source assets can be parsed.
2. Each answer mapping becomes one `DraftContentItem`.
3. Parser confidence must persist.
4. Source trace must persist.
5. Draft mappings are not learner-visible published content.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ParseToeicAnswerKeys|AnswerKeyMapping|answer key parsing" backend/src backend/tests docs/product
```

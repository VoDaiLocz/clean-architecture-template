# Reading Draft Parsing

## Purpose

P3.9 parses extracted PDF text blocks into Part 5/7 reading draft questions.

Reading drafts are structured parser output awaiting validation. They are not learner-visible published questions.

## Application Contract

Handler:

- `ParseToeicReadingDraftsHandler`

Parser:

- `IReadingDraftParser`

Result:

- `CreatedReadingDraftCount`

## Draft Contract

Draft item type:

- `ReadingQuestion`

Required fields:

- TOEIC part
- question type
- prompt
- skill tags
- parser payload
- source block trace
- parser confidence

## Data Rules

1. Reading draft parsing requires a `Pdf` source asset.
2. Extracted text blocks are passed to the parser.
3. Part 5 and Part 7 drafts persist with `ToeicPart`.
4. Skill tags must persist in payload.
5. Source trace must include extracted block evidence.
6. Reading drafts remain hidden from learner APIs until validation and publishing.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ParseToeicReadingDrafts|ReadingQuestion|reading draft" backend/src backend/tests docs/product
```

# Listening Group Parsing

## Purpose

P3.10 parses audio/transcript evidence into Part 1-4 listening draft questions.

Part 3 and Part 4 draft questions must preserve their group relationship because TOEIC conversations/talks share audio and context across multiple questions.

## Application Contract

Handler:

- `ParseToeicListeningGroupsHandler`

Parser:

- `IListeningDraftParser`

Result:

- `CreatedListeningDraftCount`
- `CreatedGroupCount`

## Draft Contract

Draft item type:

- `ListeningQuestion`

Required fields:

- TOEIC part
- group id for Part 3/4
- question number
- prompt
- skill tags
- parser payload
- audio source trace
- parser confidence

## Data Rules

1. Listening group parsing requires an `Audio` source asset.
2. Part 3 and Part 4 drafts require group id.
3. Multiple questions may share the same group id.
4. Audio asset id must persist in source trace.
5. Listening drafts remain hidden from learner APIs until validation and publishing.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ParseToeicListeningGroups|ListeningQuestion|listening draft" backend/src backend/tests docs/product
```

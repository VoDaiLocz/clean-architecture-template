# Transcript Parsing

## Purpose

P3.8 parses transcript assets into draft transcript segments linked to audio assets and listening test groups.

Transcript segments are evidence for listening review and validation. They are not learner-visible published content yet.

## Application Contract

Handler:

- `ParseToeicTranscriptsHandler`

Parser:

- `ITranscriptParser`

Result:

- `CreatedTranscriptSegmentCount`

## Draft Contract

Draft item type:

- `TranscriptSegment`

Payload fields:

- `testGroupId`
- `linkedAudioAssetId`
- `speakerLabel`
- `text`
- `startSecond`
- `endSecond`

## Data Rules

1. Only `Transcript` source assets can be parsed.
2. Each transcript segment becomes one `DraftContentItem`.
3. Audio asset linkage must persist.
4. Test group linkage must persist.
5. Parser confidence must persist.
6. Transcript drafts are not learner-visible published content.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ParseToeicTranscripts|TranscriptSegment|transcript parsing" backend/src backend/tests docs/product
```

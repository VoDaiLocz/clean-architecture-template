# Audio Metadata Extraction

## Purpose

P3.6 extracts technical metadata from TOEIC listening audio assets.

This task adds the application workflow and probe contract. It does not parse transcripts or produce learner-facing listening items.

## Application Contract

Handler:

- `ExtractToeicAudioMetadataHandler`

Probe:

- `IAudioMetadataProbe`

Result:

- `ExtractedAudioMetadataCount`

## Data Model

Domain record:

- `SourceAudioMetadata`

Table:

- `source_audio_metadata`

Migration:

- `014_source_audio_metadata`

## Data Rules

1. Only `Audio` source assets can be probed by this handler.
2. Duration seconds must be positive.
3. Sample rate must be positive.
4. Bitrate must be positive.
5. Audio metadata is evidence for listening validation and transcript alignment.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ExtractToeicAudioMetadata|SourceAudioMetadata|014_source_audio_metadata" backend/src backend/tests docs/product
```

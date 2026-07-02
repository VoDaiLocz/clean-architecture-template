# Source Asset Registration

## Purpose

P3.4 registers PDF, audio, and image asset placeholders from audited source evidence.

This task creates durable DB asset rows for later extraction. It does not download files or upload object bytes.

## Application Contract

Handler:

- `RegisterToeicSourceAssetsHandler`

Result:

- `RegisteredContainerCount`
- `RegisteredAssetCount`
- `SkippedBlockedSourceCount`

## Data Rules

1. Accessible sources with PDF/audio/image evidence create a registration container.
2. PDF evidence creates a `Pdf` source asset.
3. Audio evidence creates an `Audio` source asset.
4. Image evidence creates an `Image` source asset.
5. Missing evidence must not create an asset row.
6. Blocked sources are skipped.
7. Registered asset rows store metadata and object keys only.
8. Checksum remains `pending-registration` until byte-level object storage work.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "RegisterToeicSourceAssets|RegisteredAssetCount|source asset registration" backend/src backend/tests docs/product
```

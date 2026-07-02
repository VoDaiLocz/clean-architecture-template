# Drive Source Discovery

## Purpose

P3.2 expands accessible Google Drive source folders into source containers and source assets.

Blocked Drive folders are preserved as discovery issues so content operators can resolve access later without losing audit context.

## Application Contract

Handler:

- `DiscoverDriveSourceAssetsHandler`

Gateway:

- `IDriveDiscoveryGateway`

Result:

- `DiscoveredContainerCount`
- `DiscoveredAssetCount`
- `BlockedIssueCount`

## Domain Model

Domain records:

- `SourceDiscoveryIssue`
- `SourceDiscoveryIssueStatus`

## Tables

SQLite local/test table:

- `source_discovery_issues`

PostgreSQL migration:

- `012_source_discovery_issues`

Indexes:

- `idx_source_discovery_issues_source_status`

## Data Rules

1. P3.2 discovers Google Drive folders only.
2. Accessible Drive folders create source containers and source assets.
3. Blocked Drive folders create source discovery issues.
4. Blocked Drive folders are not sent to the Drive gateway.
5. Discovered assets store metadata and object keys, not raw bytes.
6. Asset role detection is based on filename, extension, and MIME type.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "DiscoverDriveSourceAssets|SourceDiscoveryIssue|012_source_discovery_issues" backend/src backend/tests docs/product
```

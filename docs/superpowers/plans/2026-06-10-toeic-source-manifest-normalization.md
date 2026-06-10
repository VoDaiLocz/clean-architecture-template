# TOEIC Source Manifest Normalization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Store the audited 73 TOEIC source rows as validated database inventory with source type, provider, material class, access status, evidence flags, and dashboard summary.

**Architecture:** Keep Clean Architecture boundaries: Domain defines normalization value objects and classifiers, Application exposes manifest import/query use cases, Infrastructure persists SQLite tables, and Api exposes thin endpoints. This slice normalizes source inventory only; it does not parse PDFs into final TOEIC questions yet.

**Tech Stack:** .NET 9, C#, Minimal API, SQLite via `Microsoft.Data.Sqlite`, existing console-style unit test harness.

---

## File Structure

- Create `backend/src/Domain/Aggregates/Corpus/SourceManifestModels.cs`
  - Owns source manifest record types, enums, and aggregate summary.
- Create `backend/src/Domain/Aggregates/Corpus/SourceManifestClassifier.cs`
  - Classifies URL provider/source type/material class/access status/evidence flags from audited rows.
- Create `backend/src/Application/Features/SourceManifests/ImportToeicSourceManifestHandler.cs`
  - Imports the 73-row audited manifest into repository tables using domain classifier.
- Create `backend/src/Application/Features/SourceManifests/GetSourceManifestSummaryHandler.cs`
  - Reads normalized manifest summary for API/dashboard.
- Modify `backend/src/Application/Common/Interfaces/Repositories/IKnowledgeRepository.cs`
  - Add source manifest persistence/query methods.
- Modify `backend/src/Infrastructure/Data/SqliteKnowledgeRepository.cs`
  - Add `source_manifest_entries` schema and SQL methods.
- Modify `backend/src/Application/Features/Dashboard/Queries/GetDashboardHandler.cs`
  - Include manifest summary in dashboard response.
- Modify `backend/src/Api/Program.cs`
  - Add `POST /api/source-manifest/toeic-audit` and `GET /api/source-manifest/summary`.
- Modify `backend/tests/Application.UnitTest/Program.cs`
  - Add TDD coverage for classification, import, persistence, and summary.

## Task 1: Domain Model and Classifier

**Files:**
- Create: `backend/src/Domain/Aggregates/Corpus/SourceManifestModels.cs`
- Create: `backend/src/Domain/Aggregates/Corpus/SourceManifestClassifier.cs`
- Test: `backend/tests/Application.UnitTest/Program.cs`

- [ ] **Step 1: Write failing classifier tests**

Add tests that assert:

```csharp
var driveFolder = SourceManifestClassifier.Classify(
    7,
    "SPARTA TOEIC ( quyển hồng - 10TEST )",
    "https://drive.google.com/drive/folders/1oHUHYyEQ0T5H-rl_fXHMjljV4lGKCRB-",
    inaccessible: false,
    hasPdf: true,
    hasAudio: true,
    hasTranscript: true,
    hasAnswerKey: true,
    hasImage: false
);
Assert.Equal(SourceProvider.GoogleDrive, driveFolder.Provider, "Expected Google Drive provider.");
Assert.Equal(SourceType.DriveFolder, driveFolder.SourceType, "Expected Drive folder type.");
Assert.Equal(MaterialClass.TestBook, driveFolder.PrimaryMaterialClass, "SPARTA is a test book.");
Assert.Equal(SourceAccessStatus.Accessible, driveFolder.AccessStatus, "Expected accessible source.");
Assert.True(driveFolder.Evidence.HasAudio, "Expected audio evidence.");
Assert.True(driveFolder.Evidence.HasTranscript, "Expected transcript evidence.");
Assert.True(driveFolder.Evidence.HasAnswerKey, "Expected answer key evidence.");

var blockedGrammar = SourceManifestClassifier.Classify(
    62,
    "Advanced Grammar in Use",
    "https://drive.google.com/file/d/example/view",
    inaccessible: true,
    hasPdf: false,
    hasAudio: false,
    hasTranscript: false,
    hasAnswerKey: false,
    hasImage: false
);
Assert.Equal(SourceAccessStatus.AccessBlocked, blockedGrammar.AccessStatus, "Blocked source must be explicit.");
Assert.Equal(MaterialClass.GrammarReference, blockedGrammar.PrimaryMaterialClass, "Grammar book should not become test bank.");
```

- [ ] **Step 2: Run tests to verify failure**

Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`

Expected: compile failure because `SourceManifestClassifier` does not exist.

- [ ] **Step 3: Implement minimal domain model/classifier**

Create enums and records:

```csharp
public enum SourceProvider { GoogleDrive, GoogleDocs, SharePoint, Shortlink, ExternalWeb, Unknown }
public enum SourceType { DriveFile, DriveFolder, GoogleSheet, GoogleDoc, SharePoint, Shortlink, ExternalWeb, Other }
public enum SourceAccessStatus { Accessible, AccessBlocked }
public enum MaterialClass { TestBook, SkillBook, Vocabulary, Roadmap, SpeakingWriting, GrammarReference, ExternalReference, Unknown }
public sealed record SourceEvidenceFlags(bool HasPdf, bool HasAudio, bool HasImage, bool HasTranscript, bool HasAnswerKey);
public sealed record SourceManifestEntry(...);
```

Implement keyword-based classification for the audited source titles and URL patterns.

- [ ] **Step 4: Run tests to verify pass**

Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`

Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add backend/src/Domain/Aggregates/Corpus/SourceManifestModels.cs backend/src/Domain/Aggregates/Corpus/SourceManifestClassifier.cs backend/tests/Application.UnitTest/Program.cs
git commit -m "feat: classify TOEIC source manifest entries"
```

## Task 2: Repository Schema and Persistence

**Files:**
- Modify: `backend/src/Application/Common/Interfaces/Repositories/IKnowledgeRepository.cs`
- Modify: `backend/src/Infrastructure/Data/SqliteKnowledgeRepository.cs`
- Test: `backend/tests/Application.UnitTest/Program.cs`

- [ ] **Step 1: Write failing repository tests**

Add a test that initializes SQLite, upserts a classified source manifest entry, and verifies:

```csharp
Assert.Equal(1, repository.Count("source_manifest_entries"), "Expected one normalized source row.");
var summary = repository.GetSourceManifestSummary();
Assert.Equal(1, summary.TotalSources, "Expected one total source.");
Assert.Equal(1, summary.AccessibleSources, "Expected one accessible source.");
Assert.Equal(1, summary.SourcesWithAudio, "Expected audio count.");
Assert.Equal(1, summary.SourcesWithAnswerKey, "Expected answer-key count.");
```

- [ ] **Step 2: Run tests to verify failure**

Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`

Expected: compile failure because repository methods do not exist.

- [ ] **Step 3: Implement schema and methods**

Add table `source_manifest_entries`:

```sql
CREATE TABLE IF NOT EXISTS source_manifest_entries (
    source_id TEXT PRIMARY KEY,
    sheet_row_number INTEGER NOT NULL,
    title TEXT NOT NULL,
    url TEXT NOT NULL,
    provider TEXT NOT NULL,
    source_type TEXT NOT NULL,
    material_class TEXT NOT NULL,
    access_status TEXT NOT NULL,
    has_pdf INTEGER NOT NULL,
    has_audio INTEGER NOT NULL,
    has_image INTEGER NOT NULL,
    has_transcript INTEGER NOT NULL,
    has_answer_key INTEGER NOT NULL,
    audit_notes TEXT NOT NULL
);
```

Add repository methods:

```csharp
void UpsertSourceManifestEntry(SourceManifestEntry entry);
IReadOnlyList<SourceManifestEntry> GetSourceManifestEntries();
SourceManifestSummary GetSourceManifestSummary();
```

Allow `Count("source_manifest_entries")`.

- [ ] **Step 4: Run tests to verify pass**

Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`

Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add backend/src/Application/Common/Interfaces/Repositories/IKnowledgeRepository.cs backend/src/Infrastructure/Data/SqliteKnowledgeRepository.cs backend/tests/Application.UnitTest/Program.cs
git commit -m "feat: persist normalized TOEIC source manifest"
```

## Task 3: Import Audited 73-Row Manifest

**Files:**
- Create: `backend/src/Application/Features/SourceManifests/ImportToeicSourceManifestHandler.cs`
- Create: `backend/src/Application/Features/SourceManifests/GetSourceManifestSummaryHandler.cs`
- Test: `backend/tests/Application.UnitTest/Program.cs`

- [ ] **Step 1: Write failing import test**

Add a test that calls `ImportToeicSourceManifestHandler.Handle()` and verifies:

```csharp
Assert.Equal(73, result.ImportedCount, "Expected all audited source rows imported.");
Assert.Equal(13, result.BlockedCount, "Expected blocked source count from audit.");
Assert.Equal(33, result.SourcesWithPdf, "Expected PDF evidence count from audit.");
Assert.Equal(20, result.SourcesWithAudio, "Expected audio evidence count from audit.");
Assert.Equal(6, result.SourcesWithTranscript, "Expected transcript evidence count from audit.");
Assert.Equal(5, result.SourcesWithAnswerKey, "Expected answer-key evidence count from audit.");
Assert.Equal(73, repository.Count("source_manifest_entries"), "Expected DB rows.");
```

- [ ] **Step 2: Run tests to verify failure**

Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`

Expected: compile failure because import handler does not exist.

- [ ] **Step 3: Implement static audited manifest fixture**

Implement the handler with an embedded immutable list of the 73 audited rows from the live audit. Store only source title, URL category evidence, access flag, and evidence flags needed for DB normalization. Do not store raw PDFs or learner content in this task.

- [ ] **Step 4: Run tests to verify pass**

Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`

Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add backend/src/Application/Features/SourceManifests backend/tests/Application.UnitTest/Program.cs
git commit -m "feat: import audited TOEIC source manifest"
```

## Task 4: API and Dashboard Summary

**Files:**
- Modify: `backend/src/Application/Features/Dashboard/Queries/GetDashboardHandler.cs`
- Modify: `backend/src/Api/Program.cs`
- Test: `backend/tests/Application.UnitTest/Program.cs`

- [ ] **Step 1: Write failing dashboard summary test**

Add a test that imports the manifest and verifies dashboard response includes:

```csharp
Assert.Equal(73, response.SourceManifest.TotalSources, "Dashboard should show normalized source inventory.");
Assert.Equal(13, response.SourceManifest.BlockedSources, "Dashboard should show blocked source count.");
Assert.Equal(36, response.SourceManifest.DriveFolders, "Dashboard should show Drive folder count.");
```

- [ ] **Step 2: Run tests to verify failure**

Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`

Expected: compile failure because dashboard response lacks `SourceManifest`.

- [ ] **Step 3: Implement query/API**

Update dashboard response and add endpoints:

```csharp
api.MapPost("/source-manifest/toeic-audit", ...);
api.MapGet("/source-manifest/summary", ...);
```

The POST imports the audited source manifest into SQLite. The GET returns `SourceManifestSummary`.

- [ ] **Step 4: Run tests and build**

Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`

Expected: all tests pass.

Run: `dotnet build backend/ToeicSystem.sln`

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add backend/src/Application/Features/Dashboard/Queries/GetDashboardHandler.cs backend/src/Api/Program.cs backend/tests/Application.UnitTest/Program.cs
git commit -m "feat: expose TOEIC source manifest summary"
```

## Self-Review Notes

- This plan implements only DB source normalization, matching the user's current instruction.
- It intentionally does not parse PDFs/audio into question items yet.
- It keeps blocked sources explicit instead of pretending the whole corpus is usable.
- It keeps learner-facing content separate from source/admin tables.

# Phase D3: Answer Key & Transcript Parsing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract scoring keys and listening transcripts as structured draft data linked to source assets.

**Architecture:** We will introduce a `SourceAssetLink` entity to link assets many-to-many. We will then implement real `ToeicAnswerKeyParser` and `ToeicTranscriptParser` services that read `ExtractedTextBlock` from the repository and use heuristic grouping + regex to yield DraftContent. A final `LinkSourceAssetsHandler` will wire them together.

**Tech Stack:** C# 13, .NET 9, SQLite.

---

### Task 1: Add SourceAssetLink Entity & Repository Support

**Files:**
- Modify: `backend/src/Domain/Aggregates/Corpus/SourceManifestModels.cs`
- Modify: `backend/src/Application/Common/Interfaces/Repositories/IKnowledgeRepository.cs`
- Modify: `backend/src/Infrastructure/Data/SqliteKnowledgeRepository.cs`
- Modify: `backend/tests/Application.UnitTest/Program.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// In backend/tests/Application.UnitTest/Program.cs (add to ApplicationTests)
public static void RepositoryPersistsSourceAssetLinks()
{
    using var repository = SqliteKnowledgeRepository.InMemory();
    repository.Initialize();
    
    var link = new SourceAssetLink("asset-key-1", "asset-book-1", SourceAssetRelationType.ProvidesAnswerKeyFor);
    repository.UpsertSourceAssetLink(link);
    
    var links = repository.GetSourceAssetLinks("asset-book-1");
    Assert.Equal(1, links.Count, "Should persist link");
    Assert.Equal("asset-key-1", links[0].SourceAssetId, "Should match source id");
}
```
Add `("repository persists source asset links", ApplicationTests.RepositoryPersistsSourceAssetLinks),` to the test array.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`
Expected: FAIL (Compile error or missing method)

- [ ] **Step 3: Write minimal implementation**

Add to `SourceManifestModels.cs`:
```csharp
public enum SourceAssetRelationType { ProvidesAnswerKeyFor, ProvidesTranscriptFor }
public sealed record SourceAssetLink(string SourceAssetId, string TargetAssetId, SourceAssetRelationType RelationType);
```

Add to `IKnowledgeRepository.cs`:
```csharp
void UpsertSourceAssetLink(SourceAssetLink link);
IReadOnlyList<SourceAssetLink> GetSourceAssetLinks(string targetAssetId);
```

Add to `SqliteKnowledgeRepository.cs`:
Update `Initialize()`:
```csharp
command.CommandText += @"
CREATE TABLE IF NOT EXISTS source_asset_links (
    source_asset_id TEXT NOT NULL,
    target_asset_id TEXT NOT NULL,
    relation_type INTEGER NOT NULL,
    PRIMARY KEY(source_asset_id, target_asset_id, relation_type)
);";
```
Implement the interface methods using ADO.NET SQLite commands matching the table structure.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "feat(data): add SourceAssetLink entity and repository methods"
```

### Task 2: Implement Real ToeicAnswerKeyParser

**Files:**
- Create: `backend/src/Infrastructure/Extraction/ToeicAnswerKeyParser.cs`
- Modify: `backend/tests/Application.UnitTest/Program.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// In Application.UnitTest/Program.cs
public static void ToeicAnswerKeyParserExtractsKeys()
{
    using var repository = SqliteKnowledgeRepository.InMemory();
    repository.Initialize();
    
    var asset = new SourceAsset("asset-key", "src-1", "cont-1", "Key.pdf", "path/Key.pdf", 1000, "hash", "url", SourceAssetRole.AnswerKey);
    repository.UpsertSourceAsset(asset);
    
    // Simulate some blocks
    repository.UpsertExtractedTextBlock(new ExtractedTextBlock("b1", "asset-key", 1, 0, ExtractedBlockType.Paragraph, "1. A  2. B", 1m, "{}"));
    repository.UpsertExtractedTextBlock(new ExtractedTextBlock("b2", "asset-key", 1, 1, ExtractedBlockType.Paragraph, "3.C", 1m, "{}"));
    
    var parser = new Infrastructure.Extraction.ToeicAnswerKeyParser(repository);
    var results = parser.Parse(asset);
    
    Assert.True(results.Count == 3, $"Should extract 3 answers, got {results.Count}");
    Assert.Equal("A", results[0].CorrectAnswer);
    Assert.Equal("B", results[1].CorrectAnswer);
    Assert.Equal("C", results[2].CorrectAnswer);
    Assert.True(results[0].Confidence < 0.9m, "Should flag low confidence because total isn't 100 or 200");
}
```
Add to `tests` array in `Program.cs`. Include `using Infrastructure.Extraction;` if needed.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`

- [ ] **Step 3: Write minimal implementation**

Create `ToeicAnswerKeyParser.cs` inside `backend/src/Infrastructure/Extraction`:
```csharp
using System.Text.RegularExpressions;
using Application.Common.Interfaces.Repositories;
using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;

namespace Infrastructure.Extraction;

public class ToeicAnswerKeyParser(IKnowledgeRepository repository) : IAnswerKeyParser
{
    private static readonly Regex AnswerRegex = new Regex(@"(\d{1,3})\s*[.:-]?\s*([A-D])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IReadOnlyList<AnswerKeyMappingResult> Parse(SourceAsset asset)
    {
        var blocks = repository.GetExtractedTextBlocks(asset.AssetId);
        var results = new List<AnswerKeyMappingResult>();
        
        foreach (var block in blocks)
        {
            var matches = AnswerRegex.Matches(block.Text);
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out int qNum))
                {
                    results.Add(new AnswerKeyMappingResult("unknown-test", qNum, match.Groups[2].Value.ToUpperInvariant(), 0.5m));
                }
            }
        }
        
        // Mastery check: if total is exactly 100 or 200, boost confidence
        decimal finalConfidence = (results.Count == 100 || results.Count == 200) ? 0.9m : 0.5m;
        
        return results.Select(r => r with { Confidence = finalConfidence }).ToList();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "feat(data): implement regex ToeicAnswerKeyParser"
```

### Task 3: Implement Real ToeicTranscriptParser

**Files:**
- Create: `backend/src/Infrastructure/Extraction/ToeicTranscriptParser.cs`
- Modify: `backend/tests/Application.UnitTest/Program.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// In Application.UnitTest/Program.cs
public static void ToeicTranscriptParserExtractsDialogs()
{
    using var repository = SqliteKnowledgeRepository.InMemory();
    repository.Initialize();
    
    var asset = new SourceAsset("asset-trans", "src-1", "cont-1", "Trans.pdf", "path/Trans.pdf", 1000, "hash", "url", SourceAssetRole.Transcript);
    repository.UpsertSourceAsset(asset);
    
    repository.UpsertExtractedTextBlock(new ExtractedTextBlock("b1", "asset-trans", 1, 0, ExtractedBlockType.Paragraph, "M: Hello there.\nW: Hi.", 1m, "{}"));
    
    var parser = new Infrastructure.Extraction.ToeicTranscriptParser(repository);
    var results = parser.Parse(asset);
    
    Assert.True(results.Count == 2, $"Should extract 2 segments, got {results.Count}");
    Assert.Equal("M", results[0].SpeakerLabel);
    Assert.Equal("W", results[1].SpeakerLabel);
}
```
Add to `tests` array.

- [ ] **Step 2: Run test to verify it fails**

- [ ] **Step 3: Write minimal implementation**

Create `ToeicTranscriptParser.cs`:
```csharp
using System.Text.RegularExpressions;
using Application.Common.Interfaces.Repositories;
using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;

namespace Infrastructure.Extraction;

public class ToeicTranscriptParser(IKnowledgeRepository repository) : ITranscriptParser
{
    private static readonly Regex SpeakerRegex = new Regex(@"^(M|W|Man|Woman)\s*:\s*(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IReadOnlyList<TranscriptSegmentResult> Parse(SourceAsset asset)
    {
        var blocks = repository.GetExtractedTextBlocks(asset.AssetId);
        var results = new List<TranscriptSegmentResult>();
        
        foreach (var block in blocks)
        {
            var lines = block.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var match = SpeakerRegex.Match(line.Trim());
                if (match.Success)
                {
                    results.Add(new TranscriptSegmentResult(
                        TestGroupId: "unknown-group",
                        LinkedAudioAssetId: "",
                        SpeakerLabel: match.Groups[1].Value.ToUpperInvariant(),
                        Text: match.Groups[2].Value.Trim(),
                        StartSecond: 0,
                        EndSecond: 0,
                        Confidence: 0.8m
                    ));
                }
            }
        }
        
        return results;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "feat(data): implement regex ToeicTranscriptParser"
```

### Task 4: LinkSourceAssetsHandler

**Files:**
- Create: `backend/src/Application/Features/SourceClassification/LinkSourceAssetsHandler.cs`
- Modify: `backend/tests/Application.UnitTest/Program.cs`

- [ ] **Step 1: Write the failing test**

```csharp
public static void LinkSourceAssetsHandlerPairsAssets()
{
    using var repository = SqliteKnowledgeRepository.InMemory();
    repository.Initialize();
    
    // TestBook
    var book = new SourceAsset("book-1", "src-1", "cont-1", "ETS_2022.pdf", "path/ETS_2022.pdf", 1000, "h1", "u1", SourceAssetRole.TestBook);
    // Key in same container
    var key = new SourceAsset("key-1", "src-1", "cont-1", "ETS_2022_Key.pdf", "path/ETS_2022_Key.pdf", 1000, "h2", "u2", SourceAssetRole.AnswerKey);
    // Transcript in same container
    var trans = new SourceAsset("trans-1", "src-1", "cont-1", "ETS_2022_Transcript.pdf", "path/ETS_2022_Transcript.pdf", 1000, "h3", "u3", SourceAssetRole.Transcript);
    
    repository.UpsertSourceAsset(book);
    repository.UpsertSourceAsset(key);
    repository.UpsertSourceAsset(trans);
    
    var handler = new Application.Features.SourceClassification.LinkSourceAssetsHandler(repository);
    handler.Handle();
    
    var links = repository.GetSourceAssetLinks(book.AssetId);
    Assert.Equal(2, links.Count, "Should link both key and transcript");
}
```
Add to `tests` array.

- [ ] **Step 2: Run test to verify it fails**

- [ ] **Step 3: Write minimal implementation**

Create `LinkSourceAssetsHandler.cs`:
```csharp
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceClassification;

public class LinkSourceAssetsHandler(IKnowledgeRepository repository)
{
    public void Handle()
    {
        var allAssets = repository.GetSourceManifestEntries() // Just a hacky way to scan everything if GetSourceAssets doesn't exist
            // Wait, IKnowledgeRepository doesn't have GetAllSourceAssets.
            // Let's assume you need to query them. 
            // Better: Add IReadOnlyList<SourceAsset> GetAllSourceAssets() to repository, then filter.
            ;
    }
}
```
*Note: You may need to add `GetAllSourceAssets()` to `IKnowledgeRepository.cs` and `SqliteKnowledgeRepository.cs` first.*
Then implement `Handle()`:
```csharp
        var allAssets = repository.GetAllSourceAssets();
        var books = allAssets.Where(a => a.DetectedRole == SourceAssetRole.TestBook).ToList();
        var others = allAssets.Where(a => a.DetectedRole == SourceAssetRole.AnswerKey || a.DetectedRole == SourceAssetRole.Transcript).ToList();

        foreach (var other in others)
        {
            // Heuristic 1: Same container
            var match = books.FirstOrDefault(b => b.ContainerId == other.ContainerId);
            if (match != null)
            {
                var relType = other.DetectedRole == SourceAssetRole.AnswerKey 
                    ? SourceAssetRelationType.ProvidesAnswerKeyFor 
                    : SourceAssetRelationType.ProvidesTranscriptFor;
                repository.UpsertSourceAssetLink(new SourceAssetLink(other.AssetId, match.AssetId, relType));
            }
        }
```

- [ ] **Step 4: Run test to verify it passes**

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "feat(data): auto-link answer keys and transcripts to test books"
```

# Phase D4: Draft Question Extraction Design Specification

## 1. Goal
Convert extracted text blocks into `DraftContentItem` entities for TOEIC Parts 1-7. The drafts will not be visible to learners directly; they must undergo a review/validation process. Every draft must include a precise source trace (asset id, page, block id, confidence).

## 2. Architecture & Data Model

We adopt an Orchestrator + Strategy Pattern approach to handle the diverse structure of the 7 TOEIC parts. We use a unified JSON payload in the database to maintain a single normalized workflow state machine for all drafts.

### 2.1 Domain Entities

**DraftContentItem**
Represents a pending learning item extracted from the corpus.
- `Id` (string): Unique draft identifier.
- `SourceAssetId` (string): The asset from which this draft was extracted.
- `SourceTraceContext` (string): Metadata tracing back to exact pages/blocks (e.g., page 5, block 12).
- `ToeicPart` (int): 1 through 7.
- `PayloadJson` (string): The part-specific structured data (Text, Options, ImageUrl, AudioUrl, etc).
- `Confidence` (decimal): Parser's confidence score (0.0 to 1.0).
- `ValidationStatus` (enum): `PendingReview`, `NeedsSourceFix`, `Validated`, `Rejected`.

**DraftValidationIssue**
Represents structural or data issues encountered during parsing (e.g., "Missing image context for Part 1", "Found 3 options instead of 4 for Part 5").
- `Id` (string)
- `DraftContentItemId` (string?): Nullable if the issue is at the document level.
- `SourceAssetId` (string)
- `IssueMessage` (string)

### 2.2 Extraction Interfaces

```csharp
public interface IToeicPartParser 
{
    int TargetPart { get; }
    
    // Parses a single asset's blocks and associated answer keys into draft items
    IReadOnlyList<DraftContentItem> Parse(
        SourceAsset testBookAsset, 
        IReadOnlyList<ExtractedTextBlock> blocks, 
        IReadOnlyList<AnswerKeyMappingResult> keys);
}
```

### 2.3 Application Flow (Orchestrator)
**`ParseDraftQuestionsHandler`**
1. Fetches all un-extracted `TestBook` assets.
2. For each asset, fetches its `ExtractedTextBlock`s and its linked `AnswerKeyMappingResult`s (from Phase D3).
3. Iterates over an injected `IEnumerable<IToeicPartParser>`.
4. Saves the resulting `DraftContentItem`s and `DraftValidationIssue`s to the repository.

## 3. Implementation Scope for Phase D4
To establish the pipeline, Phase D4 will implement:
1. The Core Domain Models (`DraftContentItem`, `DraftValidationIssue`).
2. The Repository Interfaces and SQLite implementation (using `TEXT` for JSON storage).
3. The Orchestrator `ParseDraftQuestionsHandler`.
4. **Part 5 Parser (`ToeicPart5Parser`)**: A concrete implementation for Part 5 (Incomplete Sentences) to serve as the reference architecture.

*Note: Parsers for Parts 1, 2, 3, 4, 6, and 7 will be implemented in subsequent iterations following this established architectural pattern.*

## 4. Error Handling
- If a parser cannot confidently prove the structure (e.g., unable to find exactly 4 answer options for a Part 5 question), it will output a `DraftValidationIssue` rather than inventing data.
- The `PayloadJson` is treated as schemaless by the database but is mapped to specific DTOs (e.g., `Part5Payload`) in the application layer using `System.Text.Json`.

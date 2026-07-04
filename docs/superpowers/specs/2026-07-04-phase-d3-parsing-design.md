# Phase D3: Answer Key & Transcript Parsing Design

## Purpose
This document specifies the design for Phase D3 of the TOEIC data production pipeline. The goal is to accurately parse raw PDF text blocks into structured Draft Content (Answer Keys and Transcripts), and to link these supplementary files to their primary TestBook files.

## Architecture & Data Flow
1. **SourceAssetLink Entity**: 
   A new entity to model many-to-many relationships between `SourceAssets`.
   - `SourceAssetId` (e.g., the Answer Key PDF)
   - `TargetAssetId` (e.g., the TestBook PDF)
   - `RelationType` (Enum: `ProvidesAnswerKeyFor`, `ProvidesTranscriptFor`)

2. **Validation Issues**: 
   If an Answer Key or Transcript is imported but the linker handler cannot find an appropriate TestBook, a `ValidationIssue` will be emitted (Unresolved Link).

## Component: Answer Key Parser (`ToeicAnswerKeyParser`)
- **Input**: A list of `ExtractedTextBlock` objects for a given Answer Key `SourceAsset`.
- **Strategy**: Hybrid Regex + Block Grouping.
  - Scan lines for common TOEIC answer formats using regex (e.g., `(\d{1,3})\s*[.:-]?\s*([A-D])`).
  - **Confidence Gate**: A standard TOEIC test section has exactly 100 questions. If the parser finds contiguous blocks of 100 questions (LC/RC), or 200 questions, the extraction is considered highly reliable (`Confidence = 0.9`, `NeedsReview = false`). 
  - If anomalous counts are detected (e.g., 98 questions found due to complex table layouts), the system will still save the parsed draft mapping but flag it (`Confidence = 0.5`, `NeedsReview = true`) for admin manual review.

## Component: Transcript Parser (`ToeicTranscriptParser`)
- **Input**: A list of `ExtractedTextBlock` objects for a given Transcript `SourceAsset`.
- **Strategy**: Speaker Marker Detection.
  - Search for speaker markers (e.g., `M:`, `W:`, `Man:`, `Woman:`).
  - Attempt to slice segments by audio track identifiers or Part headings (Part 3, Part 4) where available.

## Component: Linker Handler (`LinkSourceAssetsHandler`)
- **Purpose**: Automatically pair Answer Keys and Transcripts to their TestBooks.
- **Heuristics**:
  1. **Container Colocation**: Assets residing in the same `ContainerId` (i.e., same Google Drive folder).
  2. **Lexical Similarity**: High string similarity prefix/suffix matching on the filename/title (e.g., "Hacker TOEIC 3" vs "Hacker TOEIC 3 Answer Key").
- **Execution**: Runs periodically or triggered after local imports/extractions to retroactively link newly imported supplementary files.

## Review & Error Handling
- All parsed items are stored as `DraftContentItem` entities. They are NOT published directly to the learner.
- Admins will review drafts with low confidence.

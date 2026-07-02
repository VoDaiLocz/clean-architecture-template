# PDF Block Extraction

## Purpose

P3.5 extracts PDF pages and text blocks into durable extracted content tables.

This task adds the application workflow and extractor contract. Real PDF parsing adapters can be added later behind the same contract.

## Application Contract

Handler:

- `ExtractToeicPdfBlocksHandler`

Extractor:

- `IPdfTextBlockExtractor`

Result:

- `ExtractedPageCount`
- `ExtractedBlockCount`

## Data Rules

1. Only `Pdf` source assets can be extracted by this handler.
2. Extracted pages are persisted by asset and page number.
3. Extracted text blocks preserve block type, text, confidence, and coordinates JSON.
4. Extracted content is evidence for parser/review workflows, not learner-visible content.
5. Repeated extraction uses stable page and block ids.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ExtractToeicPdfBlocks|PdfExtractedPageResult|pdf block extraction" backend/src backend/tests docs/product
```

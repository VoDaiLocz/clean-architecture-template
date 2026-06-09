# TOEIC Clean Architecture Normalization Plan

**Goal:** Build a production-style TOEIC Listening/Reading normalization system where source PDFs and Drive files become validated learning data, not links opened by users.

**Architecture:** The backend follows the clean architecture template shape: `Domain`, `Application`, `Infrastructure`, `Api`, and `tests/Application.UnitTest`. Domain owns validation rules, Application owns use cases and repository contracts, Infrastructure owns SQLite persistence, and Api exposes a thin Minimal API surface.

**Frontend:** TypeScript/Vite dashboard for operational workflows: register raw sources, publish draft learning items, and inspect validation counts.

## Commit Discipline

- One commit per layer or workflow slice.
- No broad `git add .` for production commits.
- Each backend feature commit is verified with `dotnet build` or the application test harness.
- Frontend commit is verified with `npm run build`.

## Current Vertical Slice

1. Raw source rows can be registered in `raw_sources`.
2. Draft learning items go through the Domain validation gate.
3. Valid items are inserted into `learning_items`.
4. Invalid or low-confidence items are stored in `validation_issues`.
5. Dashboard API returns counts for raw sources, learning items, and validation issues.

## Next Slices

1. Add source audit import for the 73 Google Sheet rows.
2. Add Drive folder manifest ingestion.
3. Add PDF text/OCR extraction into raw page/block tables.
4. Add parser profiles per material type: vocabulary, grammar, test book, strategy, roadmap.
5. Add review queue UI for invalid/low-confidence normalized items.

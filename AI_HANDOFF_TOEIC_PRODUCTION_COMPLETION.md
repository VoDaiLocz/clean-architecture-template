# AI Handoff: TOEIC Production Completion

## Purpose

This file is the single handoff document for the next AI/dev agent to continue the TOEIC production system from the current repository state.

The product goal is not a PDF/file browser. The product must convert the user's TOEIC material library into normalized, validated, reviewable, publishable, learner-ready TOEIC learning data in the database, then expose it through a clean Angular learner/admin UX.

## Current Truth

- Repository: `/home/vodailoc/toeci`
- Branch: `main`
- Main stack target:
  - Backend: C# / ASP.NET Core / Clean Architecture style
  - Frontend: Angular / TypeScript
  - DB target: PostgreSQL for production, SQLite currently used for local development
- User expects:
  - Vietnamese communication
  - TDD for implementation
  - Small real commits
  - Push each commit to remote
  - No fake claims about imported data
  - DB-first normalized content, not opening raw PDFs to learners

## Important Current Git State

There are uncommitted changes made during this handoff work:

- Modified: `backend/tests/Application.UnitTest/Program.cs`
- Modified: `backend/src/Application/Common/ApiContracts/ApiContractCatalog.cs`
- Added: `backend/src/Application/Features/SourceManifests/ImportLocalToeicDownloadsHandler.cs`

There are also existing untracked working files/directories that may predate this task or are raw material:

- `downloads/`
- `raw-data/`
- `animation-assets/`
- `.playwright-cli/`
- several downloader helper scripts such as `download_toeic.py`, `download_folders.py`, etc.

Do not delete these unless the user explicitly asks. Treat `downloads/` as the current raw TOEIC corpus.

## Verification Already Run

Baseline before local import work:

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
```

Result before adding local import: `43 tests passed`.

After adding local import handler/test:

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
```

Result: `44 tests passed`.

## Current Downloads Corpus Inventory

Source directory to process:

```text
/home/vodailoc/toeci/downloads
```

Recursive inventory observed:

- Total files by extension:
  - `86` files ending in `.pdf`
  - `49` files ending in `.mp4`
  - `15` files with no extension
  - `2` files ending in `.zip`
  - `2` files ending in `.rar`
- MIME type scan:
  - `75` files are `application/pdf`
  - `77` files are `text/html`
  - `2` files are `application/vnd.rar`
- PDF-name validation:
  - `74` files have `.pdf` name and real PDF MIME/header
  - `12` files have `.pdf` name but are actually HTML placeholders/errors
- Duplicate checksum scan:
  - `13` duplicate hash groups
  - `26` duplicate files

Critical implication:

- File extension is not trustworthy.
- Some `.pdf`, `.mp4`, `.zip`, and extensionless files are actually 1.7KB/2.6KB HTML pages from failed Google Drive downloads.
- Importers must validate magic bytes and MIME/header, not just names.

Useful commands:

```bash
find downloads -type f -print0 | xargs -0 file --mime-type | awk -F': *' '{count[$2]++} END {for (m in count) print count[m], m}' | sort -nr
find downloads -type f -iname '*.pdf' -print0 | xargs -0 file --mime-type | awk -F': *' '$2=="application/pdf"{ok++} $2!="application/pdf"{bad++} END{print "valid_pdf", ok+0; print "invalid_pdf_named_files", bad+0}'
find downloads -type f -print0 | xargs -0 sha256sum | sort | awk '{count[$1]++; example[$1]=$0} END{dups=0; files=0; for(h in count){if(count[h]>1){dups++; files+=count[h]}} print "duplicate_hash_groups", dups; print "duplicate_files", files}'
```

## Data Import Work Completed So Far

### Added Local Downloads Import Handler

File:

```text
backend/src/Application/Features/SourceManifests/ImportLocalToeicDownloadsHandler.cs
```

Public types added:

- `ImportLocalToeicDownloadsCommand(string DownloadsRootPath)`
- `ImportLocalToeicDownloadsResult(int ScannedFileCount, int ImportedPdfCount, int RejectedFileCount)`
- `ImportLocalToeicDownloadsHandler`

Behavior:

- Recursively scans a local downloads root.
- Validates real PDF by reading first 5 bytes and requiring `%PDF-`.
- Rejects fake PDFs and non-PDF files.
- Creates:
  - `source_manifest_entries`
  - `source_containers`
  - `source_assets`
- Uses stable IDs based on SHA-256 of relative path:
  - `local-download-{hash}`
  - `local-container-{hash}`
  - `local-asset-{hash}`
- Stores content checksum SHA-256 in `source_assets.checksum`.
- Stores relative path and checksum in `audit_notes`.
- Classifies broad material class from title/path:
  - `SpeakingWriting`
  - `GrammarReference`
  - `Vocabulary`
  - `Roadmap`
  - `SkillBook`
  - `TestBook`
  - `Unknown`
- Flags answer-key evidence from names containing:
  - `answer key`
  - `đáp án`
  - `dap an`
  - `scriptsak`
  - `script`
- Flags transcript evidence from names containing:
  - `transcript`
  - `lời thoại`
  - `loi thoai`
  - `script`

### Added Unit Test

File:

```text
backend/tests/Application.UnitTest/Program.cs
```

Test added:

```text
imports local TOEIC downloads and rejects fake PDFs
```

The test creates a temp corpus with:

- one valid PDF byte stream
- one fake `.pdf` containing HTML
- one non-PDF text file

Pass criteria:

- scans all files recursively
- imports only valid PDF
- rejects fake PDF and non-PDF
- second import does not duplicate DB rows
- source has PDF evidence
- answer key PDF is flagged
- audit note identifies local downloads corpus

### API Contract Partially Added

File:

```text
backend/src/Application/Common/ApiContracts/ApiContractCatalog.cs
```

Contract added:

```text
POST /api/source-manifest/local-downloads -> ImportLocalToeicDownloadsResult
```

Test assertion added in `ApiContractCatalogDefinesStableTypedRoutes`.

Not completed yet:

- The actual ASP.NET route has not been added to `backend/src/Api/Program.cs`.
- No real import has been run against `backend/src/Api/toeic-normalization.db` yet.
- No commit has been made for this local import slice yet.

## Immediate Next Task For Next Agent

### Task 1: Finish API Route For Local Downloads Import

Files:

- `backend/src/Api/Program.cs`
- `backend/tests/Application.UnitTest/Program.cs` if extra API contract checks are needed

Add route:

```text
POST /api/source-manifest/local-downloads
```

Suggested request body:

```json
{
  "downloadsRootPath": "/home/vodailoc/toeci/downloads"
}
```

Implementation sketch:

```csharp
api.MapPost(
    "/source-manifest/local-downloads",
    Ok<ImportLocalToeicDownloadsResult> (
        ImportLocalToeicDownloadsCommand request,
        IKnowledgeRepository repository
    ) =>
    {
        var handler = new ImportLocalToeicDownloadsHandler(repository);
        return TypedResults.Ok(handler.Handle(request));
    }
);
```

Pass criteria:

- `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj` passes.
- `dotnet build backend/ToeicSystem.sln` passes.
- API contract route and real route match.

Commit:

```text
feat(data): import local TOEIC downloads inventory
```

Push immediately after commit.

## Run Real Local Import After API Route

Start API from repo root:

```bash
dotnet run --project backend/src/Api/Api.csproj
```

In another terminal:

```bash
curl -s -X POST http://localhost:5000/api/source-manifest/local-downloads \
  -H 'Content-Type: application/json' \
  -d '{"downloadsRootPath":"/home/vodailoc/toeci/downloads"}'
```

If API uses another port, inspect the `dotnet run` output and adjust URL.

Expected current result shape:

```json
{
  "scannedFileCount": 154,
  "importedPdfCount": 74,
  "rejectedFileCount": 80
}
```

The exact scanned/rejected number may change if files are added/removed, but imported valid PDFs should be around `74` based on current inventory.

After import, verify DB counts through API:

```bash
curl -s http://localhost:5000/api/source-manifest/summary
curl -s http://localhost:5000/api/admin/content-coverage
```

Important:

- Do not claim "all documents are in DB" until this real API import has run and DB counts prove it.
- This imports PDF metadata/assets, not extracted questions/lessons yet.

## Phase Plan To Complete Production Data And Product

### Phase D0: Stabilize Raw Corpus Inventory

Goal:

Build a truthful, repeatable local corpus inventory so the system knows what is valid, invalid, duplicate, and missing.

Tasks:

1. Finish local downloads API route.
   - Pass: route exists, contract test passes, API can import `/home/vodailoc/toeci/downloads`.
2. Run real import into SQLite local DB.
   - Pass: `source_manifest_entries`, `source_containers`, `source_assets` increase by valid PDF count.
3. Add `RejectedLocalSourceFile` model/table or equivalent issue records.
   - Purpose: fake HTML `.pdf`, fake `.mp4`, fake `.zip`, extensionless Drive HTML placeholders must be visible to admin, not silently ignored.
   - Pass: invalid files are persisted with reason such as `invalid_pdf_header`, `unsupported_mime`, `drive_html_placeholder`.
4. Add duplicate asset report by checksum.
   - Purpose: duplicate books currently exist in both named folders and `downloads/folders/Thư mục`.
   - Pass: API/admin query shows duplicate checksum groups and paths.
5. Add corpus inventory markdown/report generator.
   - Output: `raw-data/reports/local-downloads-inventory.md` or DB-backed admin report.
   - Pass: report lists valid PDFs, rejected placeholders, duplicate groups, and material-class counts.

Commit sequence:

- `feat(data): import local TOEIC downloads inventory`
- `feat(data): record rejected local corpus files`
- `feat(data): report duplicate TOEIC source assets`

### Phase D1: PDF Metadata And Page Extraction

Goal:

Turn valid PDFs into machine-readable extracted pages/text blocks.

Tasks:

1. Implement real PDF extractor adapter using `pdftotext`, `pdfinfo`, or a C# library.
   - Current existing extraction handler uses an extractor contract and test doubles.
   - Do not parse learner questions directly from PDF before preserving extracted page/block evidence.
2. Store PDF page count and extracted text blocks into existing tables:
   - `extracted_pages`
   - `extracted_text_blocks`
3. Add extraction run status.
   - Need statuses: `pending`, `processing`, `completed`, `failed`, `needs_ocr`, `unsupported`.
4. Detect scanned/image PDFs.
   - Pass: PDFs with low/no extractable text are not treated as empty content; they are marked `needs_ocr`.
5. Run extraction for all imported local PDF assets.
   - Pass: content coverage shows extracted page/block counts.

Validation:

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
```

Do not publish learner content in this phase.

Commit sequence:

- `feat(data): extract local TOEIC PDF metadata`
- `feat(data): persist local TOEIC PDF text blocks`
- `feat(data): flag scanned TOEIC PDFs for OCR`

### Phase D2: Corpus Classification And Source Roles

Goal:

Classify each material into production-useful roles so the platform knows which books feed lessons, question banks, answer keys, transcripts, roadmap logic, and admin-only references.

Required roles:

- `TestBook`
- `SkillBook`
- `Vocabulary`
- `Roadmap`
- `SpeakingWriting`
- `GrammarReference`
- `AnswerKey`
- `Transcript`
- `PracticeSet`
- `TeacherGuide`
- `UnknownNeedsReview`

Tasks:

1. Extend domain model if current `MaterialClass` is too coarse.
2. Create deterministic classifier from path/title/text sample.
3. Add confidence and review status for classification.
4. Surface low-confidence classification to admin queue.
5. Validate against known corpus examples:
   - `Sparta Toeic` answer key/transcript/book
   - `TACTICS FOR TOEIC` book/practice tests/answer key
   - `ABC TOEIC` listening/reading
   - `Tài liệu tự học SW`
   - roadmap PDFs such as `KẾ HOẠCH 30 NGÀY ÔN TOEIC.pdf`

Pass:

- Every valid PDF has a class and a confidence.
- Low confidence is visible as a validation/admin issue.
- No unknown material is silently used for learner content.

Commit:

- `feat(data): classify TOEIC local corpus roles`

### Phase D3: Answer Key And Transcript Parsing

Goal:

Extract scoring keys and listening transcripts as structured draft data linked to source assets.

Tasks:

1. Parse answer key PDFs into draft mappings.
   - Existing handler concept: `ParseToeicAnswerKeysHandler`.
   - Need real parser implementation, not only test double.
2. Parse transcript PDFs into transcript segments.
   - Existing handler concept: `ParseToeicTranscriptsHandler`.
3. Link answer keys/transcripts to matching source book/test by folder, title similarity, checksum group, and page references.
4. Add unresolved-link issues.
   - Example: answer key exists but parent test book unknown.

Pass:

- Answer mappings have source trace.
- Transcript segments have source trace.
- Unmatched answer keys/transcripts are stored but not published.

Commit sequence:

- `feat(data): parse local TOEIC answer keys`
- `feat(data): parse local TOEIC transcripts`
- `feat(data): link TOEIC keys and transcripts to source books`

### Phase D4: Draft Question Extraction

Goal:

Convert extracted text blocks into `draft_content_items` for TOEIC Parts 1-7.

Rules:

- Drafts are not visible to learners.
- Every draft must include source trace:
  - asset id
  - page
  - block id
  - parser profile
  - confidence
- Do not invent missing answers/explanations.
- If parser cannot prove structure, create validation issue or review task.

Tasks by TOEIC part:

1. Part 1:
   - Requires image/photo context plus audio.
   - If image/audio missing, draft is incomplete.
2. Part 2:
   - Requires audio prompt and answer choices or response mapping.
3. Part 3:
   - Requires conversation group, 3 questions, shared audio/transcript.
4. Part 4:
   - Requires talk group, 3 questions, shared audio/transcript.
5. Part 5:
   - Sentence completion question, 4 choices, correct answer, explanation if available.
6. Part 6:
   - Passage/text completion, multiple blanks, answer mapping.
7. Part 7:
   - Passage(s), questions, choices, evidence if available.

Pass:

- Parser creates draft rows only when minimum required structure is present.
- Invalid/incomplete drafts create `validation_issues`.
- Coverage by part is measurable.

Commit sequence:

- `feat(data): parse TOEIC reading drafts from local PDFs`
- `feat(data): parse TOEIC listening drafts from local corpus`
- `feat(data): preserve TOEIC draft source traces`

### Phase D5: Validation And Human Review

Goal:

Block bad extracted content before it becomes learner-facing.

Tasks:

1. Strengthen part-specific validation rules.
2. Add payload schema validation for each item type.
3. Add answer-option contract validation.
4. Add review queue endpoints.
5. Add publish decision audit:
   - approved
   - rejected
   - needs source fix
   - duplicate

Pass:

- No draft can publish without passing validation.
- Review decision is persisted.
- Published question/lesson has source trace back to raw material.

Commit sequence:

- `feat(data): validate TOEIC drafts by part`
- `feat(data): review local TOEIC draft content`
- `feat(data): publish validated TOEIC questions`

### Phase D6: Learning Product Logic

Goal:

Make the system teach TOEIC, not just show questions.

Business rules:

- Learner follows a structured path.
- Completing one unit unlocks the next.
- Mastery thresholds unlock harder material.
- Practice and tests feed weakness repair.

Tasks:

1. Define canonical learning path from beginner to 800+.
2. Map corpus content to path units:
   - foundation vocabulary/grammar
   - part-specific skill lessons
   - drills
   - mini tests
   - part tests
   - section tests
   - full TOEIC LR tests
3. Implement mastery gates:
   - lesson completion
   - drill accuracy
   - review mistakes
   - timed test performance
4. Implement Today Plan generation from learner state.
5. Implement repair plan from wrong answers and weak tags.

Pass:

- A new learner can onboard, take placement, receive Today Plan, study, practice, unlock next unit.
- User cannot skip locked units unless admin/test mode.
- Progress is persisted, not frontend fake state.

Commit sequence:

- `feat(learning): generate TOEIC learner path`
- `feat(learning): assign TOEIC today plan`
- `feat(learning): enforce TOEIC mastery unlocks`
- `feat(learning): generate TOEIC mistake repair plans`

### Phase D7: Backend API Completion

Goal:

Expose stable APIs for learner and admin workflows.

Learner APIs needed:

- onboarding
- placement start/submit/result
- home/today plan
- lesson detail
- drill session
- mini test session
- part test session
- listening/reading/full test session
- attempt submit
- review queue
- progress overview

Admin APIs needed:

- source inventory
- rejected files
- duplicate assets
- extraction runs
- draft review queue
- validation issue workflow
- publish queue
- content coverage dashboard

Pass:

- All routes are in `ApiContractCatalog`.
- Each route has application test coverage.
- No learner route exposes raw file paths as the primary learning experience.

Commit sequence:

- `feat(api): expose TOEIC learner study routes`
- `feat(api): expose TOEIC test session routes`
- `feat(api): expose TOEIC content operations routes`

### Phase D8: Angular Frontend Completion

Goal:

Build production learner/admin UX. The user rejected simple dashboard-style UI and wants a polished white + ocean-blue design inspired by Refero styles.

Rules:

- Use Angular.
- White + ocean blue as primary direction.
- Clean product UX, not AI-looking generic cards.
- First screen should be actual learner app, not a marketing landing page.
- Learner cares about:
  - 7 TOEIC parts
  - what is locked/unlocked
  - what to study today
  - how many lessons/drills/tests exist
  - progress and weak areas
  - practice test flow
- Admin cares about:
  - data coverage
  - broken source files
  - extraction status
  - review/publish workflow

Learner screens:

1. App shell
2. Onboarding + placement
3. Today Plan
4. 7 Part overview
5. Part detail route
6. Lesson view
7. Drill view
8. Mini test view
9. Practice test view
10. Mistake review
11. Progress overview

Admin screens:

1. Source inventory
2. Rejected files
3. Duplicate assets
4. Extraction operations
5. Draft review queue
6. Validation issue board
7. Publish queue
8. Coverage dashboard

Pass:

- Playwright E2E tests simulate real learner use.
- Screens consume backend APIs.
- No fake hardcoded learner content remains in production routes.
- Visual QA screenshots pass desktop and mobile.

Commit sequence:

- `feat(frontend): build TOEIC learner app shell`
- `feat(frontend): build TOEIC today plan`
- `feat(frontend): build TOEIC part overview`
- `feat(frontend): build TOEIC lesson and drill UX`
- `feat(frontend): build TOEIC practice test UX`
- `feat(frontend): build TOEIC admin content operations`

### Phase D9: Production Hardening

Goal:

Prepare for real market deployment.

Tasks:

1. Move durable DB to PostgreSQL.
2. Store raw assets outside DB through object storage abstraction.
3. Add background jobs for extraction/parsing.
4. Add auth and authorization:
   - learner
   - admin/content operator
5. Add observability:
   - health checks
   - structured logs
   - extraction failure metrics
   - content coverage metrics
6. Add backup/migration strategy.
7. Add release pipeline.

Pass:

- Production config refuses to run without real DB connection.
- Admin routes require admin authorization.
- Background extraction can retry/fail safely.
- Release checklist is green.

Commit sequence:

- `feat(infra): configure PostgreSQL persistence`
- `feat(infra): run TOEIC extraction jobs in background`
- `feat(security): add TOEIC authentication and authorization`
- `feat(ops): add TOEIC production observability`
- `feat(release): add TOEIC deployment readiness gate`

## Definition Of Done For The Whole Project

The system is not production-ready until all of these are true:

- Raw corpus inventory is complete and truthful.
- Invalid/fake files are tracked, not ignored.
- Valid PDFs are imported as DB source assets.
- Text/pages are extracted and linked to source trace.
- Answer keys/transcripts are parsed and linked.
- Draft questions exist across TOEIC Parts 1-7.
- Draft validation blocks bad/incomplete content.
- Human review/publish workflow exists.
- Published learner content is separated from parser drafts.
- Learner path, unlocks, practice, tests, and review work from DB state.
- Angular frontend uses backend APIs, not fake static content.
- Admin UI can see coverage, failed files, duplicates, extraction, validation, review, publish.
- Tests cover backend use cases and frontend user flows.
- Commits are small, named by task, and pushed.

## Do Not Do

- Do not claim all data is imported before DB counts prove it.
- Do not use PDF links as the learner-facing content model.
- Do not parse questions without source trace.
- Do not publish parser output directly to learners.
- Do not silently skip fake HTML files.
- Do not delete raw corpus files unless user explicitly asks.
- Do not mix huge unrelated changes into one commit.

## Suggested First Commands For Next Agent

```bash
git status --short --branch
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
find downloads -type f -print0 | xargs -0 file --mime-type | awk -F': *' '{count[$2]++} END {for (m in count) print count[m], m}' | sort -nr
```

Then finish `POST /api/source-manifest/local-downloads`, run the real import, verify DB coverage, commit, and push.

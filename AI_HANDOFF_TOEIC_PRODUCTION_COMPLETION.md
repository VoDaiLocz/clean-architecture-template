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

## Current Review For Next Agent: P4.10 And Project Direction

This section is newer than the earlier handoff notes. Follow this section first.

The previous AI/dev agent appears to have completed and pushed work through:

- `feat(p4.9): create learner review queue`

The working tree currently contains uncommitted P4.10-style changes:

- `backend/src/Application/Features/Learner/Mastery/MasteryCalculationService.cs`
- `backend/src/Application/Features/Learner/Mastery/GetLearnerMasteryHandler.cs`
- changes in:
  - `backend/src/Api/Program.cs`
  - `backend/src/Application/Common/Interfaces/Repositories/IKnowledgeRepository.cs`
  - `backend/src/Application/Features/Learner/Review/ResolveReviewItemHandler.cs`
  - `backend/src/Application/Features/Learner/Work/GetLearnerTodayPlanHandler.cs`
  - `backend/src/Application/Features/Learner/Work/ManageActivitySessionHandler.cs`
  - `backend/src/Application/Features/Learner/Work/SubmitAttemptHandler.cs`
  - `backend/src/Domain/Aggregates/LearnerProgress/LearningPathModels.cs`
  - `backend/src/Infrastructure/Data/SqliteKnowledgeRepository.cs`
  - `backend/tests/Application.UnitTest/Program.cs`

Fresh verification already run on this state:

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
```

Observed result:

- `53 tests passed`
- build succeeded with `0 Warning(s), 0 Error(s)`

Important: passing tests do not mean the business logic is correct. The current P4.10 implementation has production-blocking logic issues.

### Current P4.10 Assessment

The direction is correct at architecture level:

- mastery is backend-owned
- unlock blockers are persisted
- review resolution triggers recalculation
- Today Plan consults mastery state
- a learner mastery API route was started

But the implementation is not ready to commit as `feat(p4.10): enforce mastery unlocks`.

### Production-Blocking Issues To Fix Before Commit

#### Issue 1: Failed Mini-Test Can Still Mark Assignment Completed

Current risk:

- `SubmitAttemptHandler` marks the parent assignment `Completed` after any submitted/scored attempt.
- `MasteryCalculationService` only checks assignment status to decide whether `MiniTest` is complete.
- Therefore a learner can fail a mini-test and still pass the mastery gate if the assignment was marked completed.

Required behavior:

- Drill and mini-test completion must depend on score threshold, not only assignment status.
- Mini-test must pass threshold before `MINI_TEST_NOT_PASSED` is removed.
- Failed mini-test may complete the session/attempt lifecycle, but must not satisfy the mastery gate.

Implementation guidance:

- Do not use only `LearnerAssignmentStatus.Completed` as proof of mastery.
- Use the latest scored attempt for that assignment/session.
- Recommended threshold:
  - Drill: `>= 80%`
  - MiniTest: `>= 80%`
  - If spec later defines a different threshold, centralize it in a mastery policy constant/service.
- Add tests where mini-test score is below threshold and next unit remains locked.

Pass criteria:

- A mini-test attempt with 70% creates/keeps `MINI_TEST_NOT_PASSED`.
- Next unit does not unlock.
- Today Plan does not move learner forward.

#### Issue 2: Completing Current Unit Does Not Unlock Next Unit Status

Current risk:

- `MasteryCalculationService` sets the current unit status to `Completed`.
- It recalculates next units, but does not set the immediate next unit status to `Unlocked`.
- `GetLearnerTodayPlanHandler` only selects units with status `Unlocked`.
- The learner can become stuck after completing the first unit.

Required behavior:

- When unit N is fully completed:
  - unit N becomes `Completed`
  - the next unit N+1 becomes `Unlocked` if all prerequisite blockers are cleared
  - Today Plan can assign work from unit N+1

Pass criteria:

- Complete unit 1 gates.
- Call Today Plan.
- Response returns unit 2 assignment, not `ContentUnavailable`.

#### Issue 3: Today Plan Is Still Too Naive

Current risk:

- Today Plan always creates a `Lesson` assignment for the current unlocked unit.
- It does not choose the next correct activity based on progress inside the unit.

Required behavior:

Today Plan should choose the next backend-owned action in this order:

1. Blocking review item, if any exists for current path/unit.
2. Existing active assignment, if any exists.
3. Lesson, if no completed lesson gate exists.
4. Drill, if lesson is complete but drill gate is incomplete.
5. MiniTest, if drill gate is complete but mini-test gate is incomplete.
6. Unlock next unit only after mini-test pass and blocking reviews are resolved.

Pass criteria:

- New unit returns Lesson.
- Completed lesson returns Drill.
- Completed drill returns MiniTest.
- Failed MiniTest returns review/mini-test repair path, not next unit.
- Resolved review plus passed MiniTest unlocks next unit.

#### Issue 4: Mastery API Route Is Not In ApiContractCatalog

Current risk:

- Real route exists in `backend/src/Api/Program.cs`:
  - `GET /api/learner/units/{unitId}/mastery`
- It is missing from `backend/src/Application/Common/ApiContracts/ApiContractCatalog.cs`.

Required behavior:

- Add API contract:

```text
GET /api/learner/units/{unitId}/mastery -> LearnerMasteryResponse
```

Pass criteria:

- `ApiContractCatalogDefinesStableTypedRoutes` asserts the route.
- API contract catalog has no duplicate routes.

#### Issue 5: PostgreSQL Migration Missing For `unlock_blockers`

Current risk:

- SQLite creates `unlock_blockers`.
- `PostgresMigrationCatalog` does not create `unlock_blockers`.
- Production target requires PostgreSQL, so this is not production-complete.

Required behavior:

- Add PostgreSQL migration or extend the correct review/mastery migration to include:
  - `unlock_blockers`
  - FK to `learner_profiles`
  - index on `(learner_id, unit_id)`

Suggested migration:

```sql
CREATE TABLE IF NOT EXISTS unlock_blockers (
    blocker_id varchar(160) PRIMARY KEY,
    learner_id varchar(160) NOT NULL REFERENCES learner_profiles(learner_id),
    unit_id varchar(160) NOT NULL,
    reason varchar(160) NOT NULL,
    created_at_utc timestamptz NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_unlock_blockers_learner_unit
    ON unlock_blockers(learner_id, unit_id);
```

Pass criteria:

- Migration test checks `unlock_blockers`.
- `rg -n "unlock_blockers" backend/src/DatabaseMigrations backend/tests` finds both migration and test.

#### Issue 6: Mastery Record Missing Should Not Always Be 404

Current behavior:

- `GetLearnerMasteryHandler` throws `MASTERY_RECORD_NOT_FOUND` if no record exists.

Production direction:

- If unit belongs to learner path but record is missing, handler should either:
  - recalculate then return state, or
  - return deterministic locked state with reason `MASTERY_NOT_CALCULATED`.

Preferred behavior:

- Recalculate on read if unit is in path and no record exists.
- Then return current computed state.

Pass criteria:

- New learner/unit without existing mastery record can call mastery endpoint and receives deterministic response, not accidental missing state.

### Required TDD Plan For Completing P4.10

Do not rewrite everything. Use vertical TDD slices.

#### P4.10.1: Add Failing Test For Failed Mini-Test Gate

Add one test:

- Seed learner, path, unit 1 unlocked, unit 2 locked.
- Complete lesson and drill for unit 1.
- Submit mini-test with 70%.
- Assert:
  - unit 1 not completed
  - unit 2 remains locked
  - mastery blockers include `MINI_TEST_NOT_PASSED`

Expected before fix:

- Test should fail or reveal false completion.

After implementation:

- Test passes.

Commit only after all P4.10 fixes are done unless user asks for micro commits. If committing micro-task:

```text
fix(p4.10): keep failed mini tests from satisfying mastery
```

#### P4.10.2: Add Failing Test For Unlocking Next Unit

Add one test:

- Complete unit 1 gates with passing mini-test.
- Resolve all blocking reviews.
- Recalculate mastery.
- Assert:
  - unit 1 status is `Completed`
  - unit 2 status is `Unlocked`
  - Today Plan returns unit 2 assignment

Commit:

```text
fix(p4.10): unlock next unit after mastery gates pass
```

#### P4.10.3: Add Today Plan Activity Sequencing Tests

Add tests:

- New unlocked unit -> Lesson
- Lesson complete -> Drill
- Drill passed -> MiniTest
- Blocking review exists -> review/count blockers outrank new unit work

Implementation:

- Extract Today Plan decision logic if needed.
- Keep it in Application layer, not frontend.
- Do not hardcode frontend content.

Commit:

```text
feat(p4.10): sequence today plan by mastery gates
```

#### P4.10.4: Complete Mastery API Contract And Migration

Add:

- API contract catalog entry
- contract assertion test
- PostgreSQL migration for `unlock_blockers`
- migration test

Commit:

```text
feat(p4.10): expose mastery contract and blocker migration
```

#### P4.10.5: Verification And Push

Run:

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "Mastery|unlock_blockers|CanUnlockUnit|/api/learner/units" backend/src backend/tests docs/product
```

Then:

```bash
git status --short --branch
git add backend/src backend/tests docs/product
git commit -m "feat(p4.10): enforce mastery unlocks"
git push origin main
```

If you split commits, every commit must pass tests and be pushed.

## Project-Wide Business Guardrails For The Next Agent

These are non-negotiable for the whole TOEIC system.

### Guardrail 1: Learner UX Must Be A TOEIC Learning Product

The learner does not care about folders, parser jobs, raw PDFs, or source tables.

Learner-facing product must answer:

- What should I study today?
- Which TOEIC part am I improving?
- Why is this unit locked?
- What must I do to unlock it?
- What mistakes must I repair?
- How does this move me toward 800+?

If a feature does not support those questions, it is admin/internal, not learner UX.

### Guardrail 2: Backend Owns Business Decisions

Frontend must not own:

- placement scoring
- Today Plan selection
- assignment ordering
- mastery percentage
- unlock decisions
- answer correctness
- review blocker resolution
- content publish state

Frontend displays backend decisions only.

### Guardrail 3: Data Pipeline Must Preserve Traceability

Every learner-facing lesson/question/test item must trace back to:

- source asset
- page/block or media segment
- parser profile
- validation result
- review/publish decision

No untraced generated content should be published.

### Guardrail 4: Raw Files Are Not The Product

Do not implement learner flows that open PDFs as the primary learning experience.

Correct flow:

```text
raw file -> source asset -> extracted block -> draft item -> validation -> review -> published lesson/question/test -> learner activity
```

### Guardrail 5: Test Names Must Describe User/Business Behavior

Bad:

```text
MasteryServiceWorks
```

Good:

```text
failed mini test keeps next unit locked
today plan assigns drill after lesson completion
resolved blocking review allows next unit unlock
```

### Guardrail 6: Do Not Advance Phase With Broken Business Logic

Do not move to P5/P6/P7 until P4.10 is genuinely correct.

Minimum P4 complete proof:

- onboarding works
- placement creates path
- Today Plan chooses correct next work
- activity lifecycle persists
- attempt scoring persists
- wrong answers create review
- blocking review prevents unlock
- failed mini-test prevents unlock
- passed mini-test plus resolved review unlocks next unit
- mastery endpoint returns locked reasons

### Guardrail 7: Commit Discipline

Each commit must be small and real:

- one business capability or one fix
- tests pass before commit
- push immediately
- no bundled unrelated frontend/backend/data changes
- no commit that only moves code around without product value unless it is a required refactor for the current task

## After P4.10: Next Correct Direction

Only after P4.10 passes, continue in this order:

1. P5.1: Define common TOEIC item contracts.
   - Ensure all Parts 1-7 have required media/context fields.
2. P5.2-P5.8: Implement TOEIC part engines.
   - Part 1: image + audio.
   - Part 2: audio response.
   - Part 3: conversation group + 3 questions.
   - Part 4: talk group + 3 questions.
   - Part 5: sentence completion.
   - Part 6: passage completion.
   - Part 7: reading passage(s) + questions.
3. P6: Practice/test runtime.
   - Mini tests must feed P4.10 mastery.
   - Part/section/full tests must create weakness and repair data.
4. P7: Angular learner UX.
   - Build real user flow over backend APIs.
   - Start from Today Plan and 7 Part overview, not a marketing page.
5. P8: Admin operations UX.
   - Source inventory, rejected files, duplicate assets, extraction, validation, review, publish.
6. P9: Production hardening.
   - PostgreSQL, auth, background jobs, observability, backup, deployment.

Do not skip the data pipeline. The frontend must not fake content to compensate for missing published lessons/questions.

## Detailed Phase Plan After P4.10

This is the execution plan for the next AI/dev agent after P4.10 is fixed, verified, committed, and pushed. Do not start these phases until P4.10 has real passing tests for mastery gates, Today Plan sequencing, review blockers, and next-unit unlock.

### Phase P5: TOEIC Part Engines

Goal:

Make the backend understand the real structure and validation rules of all 7 TOEIC parts. This phase is not UI work.

Business outcome:

The system can represent, validate, and serve learner-ready questions for every TOEIC part without losing required media/context.

#### P5.1: Define Common TOEIC Item Contracts

Purpose:

Create one canonical item contract model so drills/tests can handle all TOEIC parts consistently.

Required work:

1. Define common fields:
   - `questionId`
   - `toeicPart`
   - `questionType`
   - `prompt`
   - `options`
   - `correctAnswer`
   - `explanation`
   - `skillTags`
   - `sourceTrace`
2. Define part-specific extension fields:
   - Part 1: `imageAssetId`, `audioAssetId`
   - Part 2: `audioAssetId`
   - Part 3: `groupId`, `audioAssetId`, `transcriptId`, 3 grouped questions
   - Part 4: `groupId`, `audioAssetId`, `transcriptId`, 3 grouped questions
   - Part 5: sentence stem and choices
   - Part 6: passage id, blank id/order
   - Part 7: passage id, passage group id for double/triple passages
3. Add validation tests proving missing required fields are rejected.

Pass criteria:

- No part can be published without its required media/context.
- Tests cover all 7 part contracts.
- Existing published question storage still works.

Verification:

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ToeicItem|Part1|Part2|Part3|Part4|Part5|Part6|Part7" backend/src backend/tests
```

Commit:

```text
feat(p5.1): define TOEIC item contracts
```

#### P5.2-P5.8: Implement Each TOEIC Part Engine

Do this one part per commit. Do not bundle multiple parts.

Each part engine must include:

- domain validation
- application use case or service for serving/practicing items
- tests for valid and invalid item shape
- source trace preserved
- no frontend logic

Part-specific requirements:

| Task | Required behavior | Pass criteria | Commit |
|---|---|---|---|
| P5.2 Part 1 | Photograph description requires image + audio + answer options | missing image/audio rejected | `feat(p5.2): implement TOEIC Part 1 engine` |
| P5.3 Part 2 | Question-response requires audio and response choices | missing audio rejected | `feat(p5.3): implement TOEIC Part 2 engine` |
| P5.4 Part 3 | Conversation group owns audio/transcript and exactly grouped questions | group relation preserved | `feat(p5.4): implement TOEIC Part 3 engine` |
| P5.5 Part 4 | Talk group owns audio/transcript and exactly grouped questions | group relation preserved | `feat(p5.5): implement TOEIC Part 4 engine` |
| P5.6 Part 5 | Sentence completion validates grammar/vocab question with options/explanation | invalid option/correct answer rejected | `feat(p5.6): implement TOEIC Part 5 engine` |
| P5.7 Part 6 | Text completion validates passage + blanks | passage and blank mapping required | `feat(p5.7): implement TOEIC Part 6 engine` |
| P5.8 Part 7 | Reading comprehension validates passage(s), questions, evidence | passage required; double/triple passage supported | `feat(p5.8): implement TOEIC Part 7 engine` |

Required tests per part:

- valid item can be served/practiced
- missing required media/context is rejected
- answer options are valid
- source trace exists
- learner attempt can reference item contract

Exit criteria for P5:

- All 7 TOEIC parts are represented.
- Backend validation blocks incomplete part-specific items.
- API/application layer can return part items without frontend hardcoding.

### Phase P6: TOEIC Practice And Test Runtime

Goal:

Build real practice/test workflows that use published questions and feed scoring, review, weakness tags, and mastery.

Business outcome:

Learner can practice by unit, by part, by section, and full TOEIC LR test. Results affect Today Plan and review/repair.

#### P6.1: Unit Mini-Test Runtime

Purpose:

Verify mastery for a unit before unlocking the next unit.

Required work:

1. Start mini-test session tied to `learnerId` and `unitId`.
2. Select questions from published question pool for that unit.
3. Submit answers through backend.
4. Score result.
5. Feed result into P4.10 mastery gate.
6. Wrong answers create review items.

Pass criteria:

- Mini-test cannot start for locked unit.
- Failed mini-test does not unlock next unit.
- Passed mini-test unlocks only when no blocking reviews remain.

Commit:

```text
feat(p6.1): run TOEIC mini tests
```

#### P6.2: Part Test Runtime

Purpose:

Measure one TOEIC part independently.

Required work:

- Start part test by `toeicPart`.
- Enforce part-specific item contracts from P5.
- Score by part and skill tags.
- Feed weak tags into review/repair plan.

Pass criteria:

- Part 1-7 tests can be represented.
- Missing content returns clear `INSUFFICIENT_CONTENT`.
- Result includes part score and weak tags.

Commit:

```text
feat(p6.2): run TOEIC part tests
```

#### P6.3-P6.5: Listening, Reading, Full TOEIC Tests

Tasks:

| Task | Scope | Pass criteria | Commit |
|---|---|---|---|
| P6.3 | Listening section: Parts 1-4 | section has correct part order and timing metadata | `feat(p6.3): run TOEIC listening tests` |
| P6.4 | Reading section: Parts 5-7 | section has correct part order and timing metadata | `feat(p6.4): run TOEIC reading tests` |
| P6.5 | Full TOEIC LR: 200 questions | full test enforces 200-question structure | `feat(p6.5): run full TOEIC LR tests` |

Do not fake official score conversion if scale table is not implemented. Return raw score and explicit `scoreConversionStatus`.

#### P6.6-P6.8: Test Session State, Score Breakdown, Repair Plan

Required work:

1. Persist test session lifecycle:
   - `Started`
   - `InProgress`
   - `Expired`
   - `Submitted`
   - `Scored`
2. Prevent invalid transitions:
   - cannot submit expired test
   - cannot answer after submit
   - cannot score twice
3. Create score breakdown:
   - by part
   - by skill tag
   - by timing bucket if timing exists
4. Generate repair plan:
   - wrong answers
   - weak skill tags
   - recommended review items
   - suggested units

Commits:

```text
feat(p6.6): manage TOEIC test sessions
feat(p6.7): calculate TOEIC score breakdown
feat(p6.8): generate TOEIC test repair plans
```

Exit criteria for P6:

- Practice and tests are backend-owned.
- Results feed review/mastery.
- No test workflow relies on frontend fake scoring.

### Phase P7: Angular Learner UX

Goal:

Build the learner-facing app experience over real backend APIs.

Business outcome:

The product feels like a real TOEIC learning system, not an admin dashboard or PDF browser.

Design direction:

- Angular + TypeScript.
- White + ocean blue primary visual direction.
- Clean, commercial, polished.
- Refero-style visual quality.
- No generic AI dashboard layout.
- No raw source tables on learner screens.

#### P7.1: Remove Frontend Demo Learner Content

Purpose:

Stop frontend from pretending with static content.

Required work:

- Identify all hardcoded learner content.
- Replace with API-driven state.
- If API lacks content, show honest empty/locked/loading state.

Pass criteria:

- `rg` finds no production fake TOEIC learner content.
- Existing demo route is clearly legacy or removed after replacement.

Commit:

```text
feat(p7.1): remove frontend demo learner content
```

#### P7.2: Learner App Shell

Required screens/regions:

- left/top navigation
- Today
- Parts
- Practice Tests
- Review
- Progress
- profile/status area

Pass criteria:

- Responsive desktop/mobile shell.
- Navigation does not overlap.
- Playwright screenshot check passes.

Commit:

```text
feat(p7.2): build TOEIC learner app shell
```

#### P7.3: Onboarding And Placement UX

Required flow:

1. Create learner profile.
2. Start placement.
3. Submit placement.
4. Show diagnosis.
5. Generate path.
6. Land on Today Plan.

Pass criteria:

- E2E can create learner and reach Today Plan.
- No frontend-owned placement scoring.

Commit:

```text
feat(p7.3): build onboarding and placement UX
```

#### P7.4: Today Plan UX

Required UI:

- primary next action
- why this action
- current unit/part
- blockers
- review count
- next unlock requirement

Pass criteria:

- If backend says locked, UI shows locked reason.
- If backend assigns Drill/MiniTest, UI does not relabel it as Lesson.

Commit:

```text
feat(p7.4): build TOEIC today plan UX
```

#### P7.5: Lesson And Example UX

Required UI:

- objective
- focused teaching section
- guided examples
- source-derived explanation when available
- next action button

Pass criteria:

- User studies before drill.
- Lesson completion calls backend session lifecycle.

Commit:

```text
feat(p7.5): build TOEIC lesson UX
```

#### P7.6: Drill And Mini-Test UX

Required UI:

- question player by TOEIC part contract
- answer selection
- submit
- result
- review prompt for mistakes

Pass criteria:

- Part-specific media/context renders correctly.
- Failed mini-test does not visually unlock next unit.

Commit:

```text
feat(p7.6): build TOEIC drill and mini-test UX
```

#### P7.7: Mistake Repair UX

Required UI:

- review queue
- error tag
- learner answer vs correct answer
- explanation
- repair answer
- resolve status

Pass criteria:

- Resolving review updates backend.
- Mastery recalculates after resolution.

Commit:

```text
feat(p7.7): build TOEIC mistake repair UX
```

#### P7.8: 7-Part Overview

Required UI:

- one row/card per TOEIC part
- part name
- skill type: Listening/Reading
- progress
- current unit
- lock state
- next action

Pass criteria:

- All 7 parts visible.
- Lock/progress comes from backend.
- No placeholder "coming soon" cards for production flow.

Commit:

```text
feat(p7.8): build TOEIC part overview
```

#### P7.9-P7.10: Practice Test UX And Progress UX

Required work:

- test navigation
- timer display
- submit flow
- score breakdown
- weak tags
- repair plan
- progress trend

Commits:

```text
feat(p7.9): build TOEIC practice test UX
feat(p7.10): build learner progress UX
```

Exit criteria for P7:

- Playwright E2E covers a real learner flow:
  - onboarding
  - Today Plan
  - lesson
  - drill
  - mini-test fail
  - review
  - mini-test pass
  - next unit unlock
- Desktop and mobile screenshots are reviewed.
- UI does not expose raw PDFs as learner content.

### Phase P8: Admin Content Operations UX

Goal:

Give content/admin operators visibility and control over the data pipeline.

Business outcome:

The team can see what source materials exist, what failed, what was parsed, what needs review, and what is publishable.

Required admin screens:

| Task | Screen | Purpose | Commit |
|---|---|---|---|
| P8.1 | Source inventory | Show imported sources/assets and coverage | `feat(p8.1): build admin source inventory` |
| P8.2 | Asset discovery | Show rejected files, duplicate assets, missing media | `feat(p8.2): build admin asset discovery` |
| P8.3 | Extraction dashboard | Show PDF/audio/transcript extraction runs | `feat(p8.3): build extraction operations dashboard` |
| P8.4 | Draft review queue | Approve/reject/relabel draft content | `feat(p8.4): build draft review queue` |
| P8.5 | Validation issue workflow | Manage validation failures | `feat(p8.5): build validation issue workflow` |
| P8.6 | Publish queue | Move reviewed content to learner-visible state | `feat(p8.6): build content publish queue` |
| P8.7 | Coverage dashboard | Coverage by part, lifecycle, source, issue | `feat(p8.7): build content coverage dashboard` |

Pass criteria:

- Admin can identify fake HTML downloads.
- Admin can identify duplicate books/assets.
- Admin can see draft counts by TOEIC part.
- Admin can publish only validated/reviewed content.

### Phase P9: Production Hardening

Goal:

Make the system deployable and maintainable for real users.

#### P9.1: Authentication

Required work:

- learner login
- admin login
- session/token strategy
- no anonymous admin access

Commit:

```text
feat(p9.1): add TOEIC authentication
```

#### P9.2: Authorization

Required rules:

- Learner can access only own profile/work.
- Admin can access source/review/publish.
- Legacy demo route cannot expose production data.

Commit:

```text
feat(p9.2): enforce learner admin authorization
```

#### P9.3: PostgreSQL Production Persistence

Required work:

- production connection string
- migrations
- schema history
- local SQLite remains dev/test only

Commit:

```text
feat(p9.3): configure PostgreSQL production persistence
```

#### P9.4: Standard Error Contract

Required work:

- stable error code
- message
- trace id
- validation details

Commit:

```text
feat(p9.4): standardize TOEIC API errors
```

#### P9.5-P9.8: Jobs, Observability, Backup, Release

Required work:

- background extraction/parsing jobs
- retries/failure status
- health checks
- structured logs
- backup/restore docs
- release checklist
- deployment config

Commits:

```text
feat(p9.5): run TOEIC content jobs in background
feat(p9.6): add TOEIC production observability
feat(p9.7): add backup and migration strategy
feat(p9.8): add release deployment pipeline
```

Exit criteria for P9:

- Production refuses to start without required config.
- Admin routes are protected.
- Data jobs are retryable and observable.
- Release checklist is explicit and testable.

## Phase Execution Rule

For every phase task:

1. Read the phase spec file in `docs/product`.
2. Write or update tests first.
3. Implement the smallest vertical slice.
4. Run required verification.
5. Commit with the exact task-style message.
6. Push immediately.
7. Update this handoff file only when the project direction changes or a new blocker is discovered.

Never mark a phase complete because tests compile only. A phase is complete only when the business behavior works through the public application/API boundary.

## Production Value Gate: Avoid Code That Is Theoretically Correct But Useless

This project must become a real commercial TOEIC learning product. Do not produce code that looks architecturally correct but does not improve the learner/admin workflow. A clean interface, handler, table, or service has no value unless it moves real TOEIC data and real users through the product.

### Definition Of Valuable Production Code

Code has production value only when all of these are true:

1. It solves a real product workflow.
   - Example: learner can move from lesson to drill to mini-test to unlock.
   - Not enough: adding a `Service` class that is never used by an API/user flow.
2. It uses or prepares real data.
   - Example: imported PDF asset -> extracted text -> draft -> validation -> publish.
   - Not enough: hardcoded examples that make tests pass but cannot run on the corpus.
3. It is reachable through a public application/API boundary.
   - Example: endpoint or handler used by Today Plan, attempt submit, admin review.
   - Not enough: private helper with no integration path.
4. It has a failing-business-case test before implementation.
   - Example: failed mini-test must not unlock next unit.
   - Not enough: test only checks object construction.
5. It leaves the system in a measurably better state.
   - Example: coverage count increases, blockers visible, learner can complete a step.
   - Not enough: more files/classes but same user capability.

### Anti-Patterns That Are Not Acceptable

Avoid these patterns even if tests pass:

1. **Architecture shell**
   - Creating handlers, services, DTOs, interfaces, and routes that return fake/static data.
   - Why bad: looks clean but does not operate the product.

2. **CRUD without business outcome**
   - Adding table/repository methods without connecting them to TOEIC flow.
   - Why bad: storage exists, but no learner/admin problem is solved.

3. **Test-only implementation**
   - Writing logic that only satisfies a narrow artificial unit test.
   - Why bad: passes CI but breaks real user flow.

4. **Frontend illusion**
   - Building beautiful screens with fake progress, fake unlocks, fake question counts, or hardcoded part content.
   - Why bad: user sees a product shell that is not backed by learning logic.

5. **Raw file escape hatch**
   - Letting learner click PDF/source link instead of studying normalized lesson/question/test content.
   - Why bad: product becomes a file browser, not a TOEIC learning system.

6. **Ignoring failed data**
   - Silently skipping fake HTML downloads, bad PDFs, missing audio, missing answers.
   - Why bad: production team cannot know why content coverage is low.

7. **Compile-success completion**
   - Claiming done because `dotnet build` passes.
   - Why bad: build success says syntax is valid, not that business works.

8. **Phase skipping**
   - Building UI before backend workflow/data is real.
   - Why bad: creates UX debt and fake states that later must be deleted.

### Production Quality Questions Before Any Commit

Before every commit, answer these in the commit description or handoff notes if the answer is not obvious from code:

1. What user/admin workflow is now possible that was not possible before?
2. What real data moves through the system because of this change?
3. What business rule is enforced?
4. What public API/application path proves it?
5. What failure case is tested?
6. What observable count/status/result proves it works?

If the answer to question 1 is "none", do not commit. The change is probably code shell.

### Minimum Production Acceptance By Area

#### Data Pipeline

Production value requires:

- Valid source files are persisted as source assets.
- Invalid/fake files are persisted as rejected files/issues.
- Duplicate files are visible.
- Extraction status is visible.
- Draft content is source-traced.
- Validation issues are explicit.
- Published content is separated from draft/parser output.

Not enough:

- "We have a parser class."
- "We can list PDFs."
- "We have a DB table."

Concrete proof:

```bash
curl -s http://localhost:<port>/api/admin/content-coverage
curl -s http://localhost:<port>/api/admin/rejected-files
curl -s http://localhost:<port>/api/admin/duplicate-assets
```

The response must show real counts from `/home/vodailoc/toeci/downloads`.

#### Learner Journey

Production value requires:

- Onboarding creates persisted learner profile.
- Placement creates diagnosis.
- Learning path is generated from diagnosis.
- Today Plan chooses the next correct action.
- Activity sessions persist lifecycle.
- Attempts are scored backend-side.
- Wrong answers create review items.
- Review blockers prevent unlock.
- Passed mastery gates unlock next unit.
- Learner can continue without fake frontend state.

Not enough:

- "There is a TodayPlanHandler."
- "There is a MasteryService."
- "The UI has 7 cards."

Concrete proof:

An integration/user-flow test or E2E must prove:

```text
onboard -> placement -> today lesson -> complete lesson -> drill -> fail mini-test -> review blocker -> resolve review -> pass mini-test -> next unit unlock
```

#### TOEIC Part Engines

Production value requires:

- Each TOEIC part has correct required fields.
- Listening parts require audio/media.
- Reading parts require passage/context.
- Grouped questions preserve group relation.
- Invalid part-specific content is rejected before publish.

Not enough:

- "Part number exists."
- "Part card renders."
- "Question has prompt/options."

Concrete proof:

- Tests reject missing media for Parts 1-4.
- Tests reject missing passage for Parts 6-7.
- Tests preserve group id for Parts 3-4.

#### Practice/Test Runtime

Production value requires:

- Tests use published questions.
- Test sessions persist state.
- Timing/session state is enforced.
- Submit/score cannot happen twice incorrectly.
- Results feed review and repair plan.
- Full TOEIC LR structure can represent 200 questions.

Not enough:

- "There is a test screen."
- "There is a score number."
- "The frontend calculates percentage."

Concrete proof:

- Backend rejects invalid session transitions.
- Failed mini-test affects mastery.
- Full test validates target question count.

#### Frontend UX

Production value requires:

- Angular screens consume real backend APIs.
- UI displays backend lock reasons and next actions.
- UI can complete a real learner workflow.
- Mobile and desktop screenshots show usable layout.
- Loading/error/empty states are honest.

Not enough:

- "Looks beautiful."
- "Has animations."
- "Shows sample TOEIC content."

Concrete proof:

Playwright must exercise real API-backed flow, not mocked state, unless the test is explicitly a component-only visual test.

#### Admin UX

Production value requires:

- Admin can see source inventory.
- Admin can see rejected/fake files.
- Admin can see duplicate assets.
- Admin can see extraction/parsing status.
- Admin can review/approve/reject draft content.
- Admin can inspect coverage by TOEIC part and lifecycle.

Not enough:

- "Dashboard has cards."
- "It shows total count only."

Concrete proof:

- Admin can identify why a TOEIC part has low publishable content.
- Admin can take the next operational action.

### Production Quality Bar

Every production feature must satisfy:

- Correctness: business rule is right for TOEIC domain.
- Traceability: important state has source/audit trail.
- Idempotency: repeated import/recalc/submit where applicable is safe or explicitly rejected.
- Recoverability: failures are recorded and visible.
- Observability: coverage/status can be queried.
- Security boundary: learner/admin concerns are not mixed.
- UX utility: user can complete a real task without understanding internals.

### How To Review Your Own Work

After implementing any task, run this review:

1. Start from the real user/admin goal, not from the files changed.
2. Execute the public path:
   - API request
   - application handler
   - E2E flow
3. Inspect persisted state.
4. Trigger one failure case.
5. Confirm the UI/API reports a useful state.
6. Only then claim complete.

If you cannot execute a public path, the feature is not complete.

### Examples Of Good Vs Bad Completion Claims

Bad:

```text
Done: added mastery service and tests pass.
```

Good:

```text
Done: failed mini-test now keeps next unit locked. Verified by test `failed mini test keeps next unit locked`; Today Plan returns `MINI_TEST_NOT_PASSED`; build and unit tests pass.
```

Bad:

```text
Done: built Part 1 UI.
```

Good:

```text
Done: Part 1 practice screen loads backend question with image/audio, submits answer through API, shows result, and Playwright verifies desktop/mobile layout.
```

### If You Discover A Shortcut

Before taking any shortcut, write down:

- what production behavior will be missing
- how the user/admin will be affected
- what follow-up task will close the gap

If the shortcut creates fake learner value, do not take it.

## DB Reality: Current Database Is Not A Complete TOEIC Content Database

This section is mandatory context for any next AI/dev agent. Do not misrepresent the current data state.

### Current Verified DB State

Local DB inspected:

```text
backend/src/Api/toeic-normalization.db
```

Direct SQLite counts observed:

```text
source_manifest_entries      75
source_containers            75
source_assets                75
source_audio_metadata        0
extracted_pages              0
extracted_text_blocks        0
draft_content_items          0
validation_issues            0
published_lessons            0
guided_examples              0
published_questions          0
published_tests              0
published_test_sections      0
published_test_items         0
learner_profiles             0
placement_sessions           0
learner_assignments          0
activity_sessions            0
learner_attempts             0
attempt_answers              0
review_items                 0
repair_attempts              0
mastery_records              0
```

Meaning:

- The DB currently contains local source inventory and PDF asset metadata.
- The DB does not yet contain extracted PDF text.
- The DB does not yet contain normalized learner-ready TOEIC lessons/questions/tests.
- The DB does not yet contain real learner journey state.

### Current Raw Corpus State

Current raw corpus:

```text
/home/vodailoc/toeci/downloads
```

Observed MIME inventory:

```text
75 application/pdf
77 text/html
2 application/vnd.rar
```

Important:

- Many files that look like `.pdf`, `.mp4`, `.zip`, or extensionless Drive files are actually small HTML placeholder/error pages.
- The import has registered valid PDF assets, but the rejected HTML placeholders are not confirmed in the current DB snapshot.
- Code contains support for rejected local files, but the current DB inspected did not have `rejected_local_source_files`, meaning DB was not initialized/migrated with that newer schema or import was run before that feature.

### Correct Claims Allowed

Allowed:

```text
The project has a foundation for importing local TOEIC source assets.
The current local DB has source inventory and PDF asset metadata.
The data pipeline foundation exists but content normalization is not complete.
```

Not allowed:

```text
The raw documents have been fully normalized into DB.
The database has all TOEIC learning content.
The system is ready to teach from the downloaded corpus.
All PDF content has been extracted.
All questions/lessons/tests are generated.
```

Short version:

- P4 does not complete raw data DB normalization.
- DB is not complete.
- Current state is source inventory, not content database.
- Saying "documents are normalized into DB" is false.
- Saying "the foundation exists to continue normalization" is true.

## DB Completion Plan: Raw Corpus To Real Content Database

This is the required process to turn `/downloads` into a production TOEIC content database.

Do not skip steps. Do not jump from raw PDF assets directly to frontend learner UI.

### DB Phase 1: Reinitialize And Verify Source Inventory Schema

Goal:

Ensure the local DB schema matches current code and can record both valid and rejected raw files.

Required tasks:

1. Decide whether to migrate existing SQLite or recreate local dev DB.
   - If recreating, back up current DB first.
   - Do not delete user raw files.
2. Ensure `rejected_local_source_files` table exists.
3. Ensure duplicate asset reporting works.
4. Run local downloads import again.
5. Verify counts.

Required verification commands:

```bash
python3 - <<'PY'
import sqlite3
con=sqlite3.connect('backend/src/Api/toeic-normalization.db')
cur=con.cursor()
for table in [
  'source_manifest_entries',
  'source_containers',
  'source_assets',
  'rejected_local_source_files',
  'extracted_pages',
  'extracted_text_blocks',
  'draft_content_items',
  'published_lessons',
  'published_questions',
  'published_tests'
]:
    try:
        cur.execute(f'select count(*) from {table}')
        print(table, cur.fetchone()[0])
    except Exception as exc:
        print(table, 'MISSING_OR_ERROR', exc)
PY
```

Pass criteria:

- `source_assets` has real PDF count.
- `rejected_local_source_files` exists and has rejected HTML/unsupported count.
- duplicate asset endpoint/report shows duplicate checksum groups.

Commit:

```text
fix(data): align local DB schema with source inventory pipeline
```

### DB Phase 2: Extract PDF Pages And Text Blocks

Goal:

Convert each valid PDF asset into extractable DB evidence.

Required tasks:

1. Query all `source_assets` where `detected_role = 'Pdf'`.
2. Extract PDF metadata:
   - page count
   - width/height if available
   - extraction status
3. Extract text per page/block using the existing PDF extraction implementation.
4. Persist:
   - `extracted_pages`
   - `extracted_text_blocks`
5. Mark failures:
   - scanned/image-only PDF
   - encrypted/corrupt PDF
   - unsupported PDF
6. Do not create draft questions yet.

Pass criteria:

- `extracted_pages > 0`
- `extracted_text_blocks > 0`
- failed PDFs are visible as issues/status, not silently skipped.
- source trace from block to asset is intact.

Verification:

```bash
python3 - <<'PY'
import sqlite3
con=sqlite3.connect('backend/src/Api/toeic-normalization.db')
cur=con.cursor()
for table in ['source_assets','extracted_pages','extracted_text_blocks','source_discovery_issues']:
    cur.execute(f'select count(*) from {table}')
    print(table, cur.fetchone()[0])
PY
```

Commit:

```text
feat(data): extract TOEIC PDF pages and text blocks
```

### DB Phase 3: Classify Assets And Content Roles

Goal:

Classify materials so parsers know what each source is for.

Required classifications:

- test book
- skill book
- vocabulary
- roadmap
- speaking/writing
- grammar/reference
- answer key
- transcript
- practice test
- unknown needs review

Required tasks:

1. Use title/path/text sample to classify.
2. Persist classification and confidence.
3. Create validation/review issue for low confidence.
4. Do not use unknown materials for learner publishing.

Pass criteria:

- Every valid source asset has a role/class.
- Low confidence sources are visible.
- Answer key/transcript candidates are identifiable.

Commit:

```text
feat(data): classify TOEIC corpus assets
```

### DB Phase 4: Parse Answer Keys And Transcripts

Goal:

Create structured draft data needed for scoring and listening support.

Required tasks:

1. Parse answer-key PDFs/text blocks.
2. Parse transcript PDFs/text blocks.
3. Link answer keys/transcripts to source books/tests where possible.
4. Store unmatched items as review issues.
5. Preserve source trace.

Tables affected:

- `draft_content_items`
- `validation_issues`
- possibly `source_discovery_issues`

Pass criteria:

- Answer mappings exist as drafts.
- Transcript segments exist as drafts.
- Unmatched keys/transcripts are visible.

Commit:

```text
feat(data): parse TOEIC answer keys and transcripts
```

### DB Phase 5: Parse Draft Questions For Parts 1-7

Goal:

Create draft TOEIC questions from extracted blocks.

Required tasks:

1. Parse Part 5 sentence completion first because it is text-only and easiest to validate.
2. Parse Part 6 passage completion.
3. Parse Part 7 reading comprehension.
4. Parse Parts 1-4 only when audio/image/transcript linkage is available.
5. Every draft must include:
   - TOEIC part
   - item type
   - payload JSON
   - source trace JSON
   - parser confidence
   - status
6. Do not publish drafts automatically.

Pass criteria:

- `draft_content_items > 0`
- draft counts by TOEIC part are visible.
- low-confidence/invalid drafts create validation issues.

Verification:

```bash
python3 - <<'PY'
import sqlite3
con=sqlite3.connect('backend/src/Api/toeic-normalization.db')
cur=con.cursor()
cur.execute('select coalesce(toeic_part,-1), status, count(*) from draft_content_items group by coalesce(toeic_part,-1), status order by 1,2')
for row in cur.fetchall():
    print(row)
cur.execute('select issue_code, count(*) from validation_issues group by issue_code order by count(*) desc')
for row in cur.fetchall():
    print(row)
PY
```

Commit:

```text
feat(data): parse TOEIC draft questions from extracted corpus
```

### DB Phase 6: Validate Draft Content

Goal:

Block bad parser output before human review/publish.

Required validations:

- answer options exist and are unique
- correct answer exists in options
- TOEIC part matches item contract
- required media/context exists by part
- source trace exists
- parser confidence above threshold or marked for review

Pass criteria:

- invalid drafts do not publish
- validation issues have actionable codes
- coverage dashboard shows validation breakdown

Commit:

```text
feat(data): validate parsed TOEIC draft content
```

### DB Phase 7: Human Review And Publish

Goal:

Move only reviewed valid content into learner-facing tables.

Required tasks:

1. Review valid draft items.
2. Approve/reject/relabel.
3. Publish approved content to:
   - `published_lessons`
   - `guided_examples`
   - `published_questions`
   - `published_tests`
4. Preserve source trace.
5. Never publish invalid drafts.

Pass criteria:

- `published_lessons > 0`
- `published_questions > 0`
- published questions are distributed across TOEIC parts where source supports it
- learner APIs can serve published content

Commit:

```text
feat(data): publish reviewed TOEIC learning content
```

### DB Phase 8: Build Practice/Test Content Sets

Goal:

Create learner-ready practice/test structures.

Required tasks:

1. Create unit drills from published questions.
2. Create mini-tests per unit.
3. Create part tests.
4. Create listening/reading section tests.
5. Create full tests only when 200-question requirement can be satisfied.

Tables affected:

- `published_tests`
- `published_test_sections`
- `published_test_items`

Pass criteria:

- mini-test exists for units with enough content.
- part tests exist only when enough part content exists.
- full test does not exist unless 200 valid questions are available.

Commit:

```text
feat(data): compose TOEIC practice and test sets
```

## DB Completion Definition Of Done

The DB can be called a real TOEIC content database only when:

- raw valid assets are imported
- rejected files are tracked
- duplicate assets are reported
- PDF text/pages are extracted
- answer keys/transcripts are parsed
- draft questions exist with source trace
- validation issues are recorded
- reviewed content is published
- learner-facing lesson/question/test tables are populated
- content coverage can prove counts by TOEIC part and lifecycle

Until then, say:

```text
The database has source inventory and pipeline foundation, but the TOEIC learning content normalization is not complete.
```

Do not say:

```text
The documents have been standardized into the DB.
```

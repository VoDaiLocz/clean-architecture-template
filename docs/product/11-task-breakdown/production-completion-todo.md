# TOEIC Production Completion Todo

## Purpose

This is the execution checklist for finishing the TOEIC production system from the current repository state. It is not a feature wishlist. A task is complete only when real data, backend behavior, frontend behavior, tests, commit, and push evidence satisfy the pass criteria.

Current verified database state on 2026-07-05:

| Database | Current Truth |
| --- | --- |
| `backend/src/Api/toeic_knowledge.db` | Runtime DB has `learner_profiles=1`, `placement_sessions=1`, but `source_assets=0`, `extracted_text_blocks=0`, `draft_content_items=0`, `published_lessons=0`, `published_questions=0`, `published_tests=0`. |
| `backend/src/Api/toeic-normalization.db` | Normalization DB has `source_manifest_entries=75`, `source_assets=75`, `source_containers=75`, `extracted_pages=6522`, `extracted_text_blocks=23034`, `learning_items=3`, but `draft_content_items=0`, `published_lessons=0`, `published_questions=0`, `published_tests=0`. |
| `backend/src/Infrastructure/Data/toeci.db` | Empty DB with `0` tables. |

Production implication:

- The project currently has extracted corpus evidence, not learner-ready content.
- Any claim that all raw TOEIC material has been normalized into the learner DB is false.
- The first production priority is converting extracted blocks into validated draft content, reviewed published lessons/questions/tests, and then making learner flows consume that data.

## Global Execution Rules

- Follow TDD for every code task: failing test first, implementation second, verification third.
- Do not publish low-confidence or fabricated content.
- Learner UI must never open raw PDF/Drive/source files as the learning experience.
- Learner-visible content must come from `published_lessons`, `published_questions`, or `published_tests`.
- Every learner-visible item must keep source traceability to asset/page/block/media.
- Frontend displays backend decisions; it must not own placement, scoring, mastery, unlock, review, or publish logic.
- Every completed task must be committed and pushed immediately.
- Do not mark a phase complete if verification commands were not run.

## Completion Order

Work must proceed in this order:

1. Stabilize DB source of truth and local runtime DB selection.
2. Convert normalization DB extracted blocks into real draft content.
3. Validate and review/publish content into learner-ready tables.
4. Make placement, Today Plan, Learn, Practice, Review, Tests, and Progress consume only published/backend state.
5. Rebuild Angular UX around Vietnamese learner workflows.
6. Add admin content operations so the corpus can be scaled beyond the first slice.
7. Harden production configuration, auth, monitoring, backup, and release gates.

Do not skip from extracted blocks directly to frontend cards.

---

## Phase A: Repository And DB Truth Reset

### A1. Freeze Current State And Remove Generated Noise From Future Commits

**Purpose:** Stop hidden dirty files and generated Angular cache from contaminating production commits.

**Scope:**

- Inspect `git status --short`.
- Decide which current dirty files belong to the active fix.
- Do not delete user corpus files such as `downloads/`, `raw-data/`, or source assets.
- Add ignore rules for generated local artifacts if missing:
  - `frontend/.angular/`
  - Playwright screenshots/test results
  - local DB backups
  - temporary logs

**Pass Criteria:**

- `git status --short` shows only intentional source/doc changes before each commit.
- Generated cache files are not staged.

**Verification:**

```bash
git status --short
git diff --name-only --cached
```

**Commit:**

```text
chore: stabilize repository hygiene for TOEIC production work
```

### A2. Choose One Runtime DB Strategy For Local Development

**Purpose:** Prevent the app from running against empty `toeic_knowledge.db` while normalized corpus evidence sits in `toeic-normalization.db`.

**Scope:**

- Inspect `backend/src/Api/appsettings.Development.json`.
- Decide one local truth:
  - either switch development runtime to `toeic-normalization.db`, or
  - run a migration/copy job that promotes normalized corpus data into `toeic_knowledge.db`.
- Document the decision in `docs/product/06-data-model.md` or this file.
- Add a startup or health check warning when runtime DB has zero published content.

**Pass Criteria:**

- API and data pipeline commands point to the same DB during local development.
- `GET /api/learner/placement/{sessionId}` cannot silently look healthy while content tables are empty; it must return a clear Vietnamese unavailable state or admin-visible diagnostic.

**Verification:**

```bash
python3 - <<'PY'
import sqlite3
from pathlib import Path
for p in [Path('backend/src/Api/toeic_knowledge.db'), Path('backend/src/Api/toeic-normalization.db')]:
    con = sqlite3.connect(f'file:{p.resolve()}?mode=ro', uri=True)
    print(p, con.execute("select count(*) from sqlite_master where type='table'").fetchone()[0])
    for t in ['source_assets','extracted_text_blocks','draft_content_items','published_lessons','published_questions','published_tests']:
        exists = con.execute("select count(*) from sqlite_master where type='table' and name=?", (t,)).fetchone()[0]
        if exists:
            print(t, con.execute(f"select count(*) from {t}").fetchone()[0])
    con.close()
PY
dotnet build backend/ToeicSystem.sln
```

**Commit:**

```text
chore: align local TOEIC runtime database source
```

---

## Phase B: Real Corpus To Draft Content

### B1. Build A Deterministic Corpus Coverage Audit

**Purpose:** Know exactly what can be normalized from the downloaded corpus before writing parsers.

**Scope:**

- Add an application/query handler or utility that reports:
  - source assets by role: PDF, audio, image, html-placeholder, duplicate
  - extracted text blocks by asset
  - likely TOEIC part coverage by evidence
  - missing media risks for Parts 1-4
  - candidate answer-key assets
  - candidate transcript assets
- Output must be DB-backed, not filesystem-only.

**Pass Criteria:**

- Report proves the current corpus has `source_assets=75` and `extracted_text_blocks=23034` in the chosen DB.
- Report identifies at least one realistic first publish slice, preferably Part 5 or Part 7 reading because it can be validated from text before audio alignment is solved.
- Report does not call extracted text "published content".

**Required Tests:**

- Unit test for counting assets and extracted blocks.
- Unit test for classifying a fake HTML placeholder as rejected/unusable.
- Unit test for duplicate checksum grouping if repository exposes checksums.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
```

**Commit:**

```text
feat(data): audit real TOEIC corpus coverage
```

### B2. Implement Part 5 Draft Parser From Extracted Text Blocks

**Purpose:** Create the first real learner-ready candidate questions from actual extracted corpus text.

**Scope:**

- Use extracted text blocks from the chosen DB.
- Parse only high-confidence Part 5 incomplete sentence questions when the parser can detect:
  - question stem
  - exactly four options A-D
  - answer key evidence from the same source or linked answer-key block
  - source trace: asset id, page, block ids
- Store results in `draft_content_items` with `PendingValidation`.
- Do not invent explanations. If no explanation exists, mark the draft as missing explanation and keep it out of published learner content until reviewed.

**Pass Criteria:**

- `draft_content_items > 0` for real Part 5 candidates.
- Every draft has source trace.
- Every draft has no answer leak through learner APIs.
- Low-confidence or incomplete items become validation issues, not published questions.

**Required Tests:**

- Parser fixture test with realistic OCR/text-block input.
- Negative test: no answer key means draft is not publishable.
- Repository test: draft persists with source trace and parser confidence.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
python3 - <<'PY'
import sqlite3
from pathlib import Path
p = Path('backend/src/Api/toeic-normalization.db')
con = sqlite3.connect(p)
for t in ['draft_content_items','published_questions']:
    print(t, con.execute(f'select count(*) from {t}').fetchone()[0])
con.close()
PY
```

**Commit:**

```text
feat(data): parse real Part 5 draft questions
```

### B3. Implement Reading Passage Draft Parser For Parts 6 And 7

**Purpose:** Normalize reading passage material into structured drafts instead of treating PDFs as files.

**Scope:**

- Detect passage boundaries and linked questions from extracted blocks.
- Store passage context with question drafts.
- Require evidence spans/page/block ids.
- Mark ambiguous passage/question splits as `ValidationFailed` or `NeedsReview`.

**Pass Criteria:**

- Part 6/7 drafts include passage context.
- No Part 6/7 item can reach publish state without `passageId` or equivalent persisted passage payload.
- Admin review can see source trace for every passage/question pair.

**Required Tests:**

- Fixture test for passage plus two questions.
- Negative test for orphan question without passage.
- Validation test for missing evidence span.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
```

**Commit:**

```text
feat(data): parse real TOEIC reading passage drafts
```

### B4. Implement Audio/Image Asset Pairing For Listening Parts

**Purpose:** Prepare Parts 1-4 for real publication without faking audio or images.

**Scope:**

- Classify audio files and image-bearing PDF/page assets.
- Link likely audio tracks to source containers and extracted transcript blocks where available.
- Do not publish Part 1 without image and audio.
- Do not publish Parts 2-4 without audio.
- Do not publish Parts 3-4 without group structure.

**Pass Criteria:**

- Admin coverage report shows which Parts 1-4 assets are publishable, missing transcript, missing audio, or missing image.
- Listening drafts are created only when required media evidence exists.

**Required Tests:**

- Audio metadata classification test.
- Part 1 missing image/audio rejection test.
- Part 3/4 group question count validation test.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
```

**Commit:**

```text
feat(data): map listening media for TOEIC draft content
```

---

## Phase C: Validation, Review, Publish, And Seeded Learner Content

### C1. Make Draft Validation Strict And Explainable

**Purpose:** Ensure content quality is enforceable, not based on hopeful parsing.

**Scope:**

- Validation must produce issue codes:
  - `MISSING_SOURCE_TRACE`
  - `LOW_CONFIDENCE`
  - `MISSING_OPTIONS`
  - `MISSING_CORRECT_ANSWER`
  - `MISSING_EXPLANATION`
  - `MISSING_AUDIO`
  - `MISSING_IMAGE`
  - `MISSING_PASSAGE`
  - `INVALID_GROUP_SIZE`
- Store validation issues with draft id and source trace.

**Pass Criteria:**

- Invalid drafts are not publishable.
- Validation result is visible to admin APIs.
- Learner APIs cannot see invalid drafts.

**Required Tests:**

- One positive validation test per TOEIC part.
- One negative validation test per required part-specific field.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
```

**Commit:**

```text
feat(content): enforce explainable TOEIC draft validation
```

### C2. Publish A Small Real Vertical Slice

**Purpose:** Prove the end-to-end content path with real corpus-derived data before scaling all files.

**Scope:**

- Select the first validated slice:
  - minimum: one Part 5 lesson and at least five Part 5 questions, or
  - better: one unit with lesson, guided example, drill questions, and mini-test questions.
- Create `published_lessons` before publishing linked `published_questions`.
- Ensure published records include source trace and skill tags.
- Ensure play payload excludes `correctAnswer`, `explanation`, and admin source trace before submit.

**Pass Criteria:**

- `published_lessons > 0`.
- `published_questions > 0`.
- `GET /api/learner/toeic-parts` shows non-zero content for the published part.
- `GET /api/learner/placement/{sessionId}` returns real questions when a placement session exists.

**Required Tests:**

- Publish handler test creates lesson and questions.
- API contract test proves play payload has no correct answer.
- API smoke test returns non-empty placement/part overview content.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
python3 - <<'PY'
import sqlite3
from pathlib import Path
p = Path('backend/src/Api/toeic-normalization.db')
con = sqlite3.connect(p)
for t in ['published_lessons','published_questions','published_tests']:
    print(t, con.execute(f'select count(*) from {t}').fetchone()[0])
con.close()
PY
```

**Commit:**

```text
feat(content): publish first real TOEIC learning slice
```

### C3. Scale Publish Coverage By TOEIC Part

**Purpose:** Move from proof slice to useful product coverage.

**Scope:**

- Define minimum useful market coverage per part:
  - Part 1: image+audio item set
  - Part 2: audio response set
  - Part 3: conversation groups
  - Part 4: short talk groups
  - Part 5: grammar/vocabulary question bank
  - Part 6: passage completion sets
  - Part 7: reading passage sets
- Add coverage dashboard metrics:
  - extracted
  - parsed
  - validation failed
  - ready for review
  - published
  - blocked by missing media/answer/passages

**Pass Criteria:**

- Coverage report makes it impossible to confuse source inventory with published content.
- Published content count by TOEIC part is visible and testable.

**Required Tests:**

- Coverage query tests by part/status.
- API/admin response tests.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
```

**Commit:**

```text
feat(content): report TOEIC publish coverage by part
```

---

## Phase D: Learner Journey Business Logic

### D1. Re-verify P4.10 Mastery Unlocks Against Real Published Content

**Purpose:** Ensure learner progression works with real content, not only synthetic tests.

**Scope:**

- Re-test:
  - placement creates path
  - Today Plan assigns lesson first
  - lesson completion unlocks drill
  - drill pass unlocks mini test
  - failed mini test keeps next unit locked
  - wrong answer creates review blocker
  - resolved review plus passed mini test unlocks next unit
- Use published content records from Phase C.

**Pass Criteria:**

- Learner cannot skip teach -> drill -> mini-test -> review gates.
- Backend returns lock reasons in Vietnamese-ready response fields.

**Required Tests:**

- Application tests using repository-backed published content.
- API smoke tests for Today Plan and mastery endpoints.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
```

**Commit:**

```text
test(learner): verify mastery flow with published TOEIC content
```

### D2. Complete Placement With Real Published Question Selection

**Purpose:** Placement must diagnose from real item banks, not empty sessions or fake questions.

**Scope:**

- Build placement blueprint:
  - balanced across available TOEIC parts
  - no correct answer in play payload
  - skip support
  - result breakdown by part and skill tag
- If content is insufficient, API must return `ContentUnavailable` with clear reason, not an empty test that looks valid.

**Pass Criteria:**

- Placement session returns non-empty real questions when enough published content exists.
- Empty/insufficient content returns explicit unavailable state.
- Submit produces persisted diagnostic result and weakness summary.

**Required Tests:**

- Enough-content test.
- Insufficient-content test.
- No-answer-leak serialization test.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
```

**Commit:**

```text
feat(learner): run placement from published TOEIC questions
```

### D3. Complete Today Plan, Learn, Practice, Review, Tests, Progress APIs

**Purpose:** Frontend must have complete learner APIs before UX can be called production.

**Scope:**

- Today API returns next action, blocker count, progress, and reason.
- Learn API returns available/locked units and lesson content.
- Practice API returns drill/mini-test sessions.
- Review API returns unresolved mistakes and repair workflow.
- Tests API returns mini/part/section/full test options from published tests.
- Progress API returns score trend, part strengths/weaknesses, mastery status.

**Pass Criteria:**

- Every learner route has a backend API source.
- No learner route depends on hardcoded sample questions/progress.
- Empty states are explicit and useful.

**Required Tests:**

- API contract catalog tests.
- One repository-backed application test per API group.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "DemoLearnerSession|mock|sample|hardcoded" frontend/src backend/src
```

**Commit:**

```text
feat(learner): complete production learner API surface
```

---

## Phase E: Angular Learner UX

### E1. Remove Fake Learner Content From Angular

**Purpose:** Stop UI from looking populated when backend has no real published data.

**Scope:**

- Replace hardcoded learner cards/questions/progress with API data.
- Vietnamese UI copy for learner screens.
- Empty content state must explain the real state:
  - no placement
  - no published content
  - locked by mastery
  - blocked by review
  - loading/API error

**Pass Criteria:**

- Searching frontend source finds no fake production questions.
- User can see why data is missing.

**Required Tests:**

- Angular unit tests for empty and loaded states.
- Playwright test for no-content state.

**Verification:**

```bash
npm run build --prefix frontend
npm run test:unit --prefix frontend
npx --prefix frontend playwright test
```

**Commit:**

```text
feat(ui): remove fake TOEIC learner content
```

### E2. Rebuild Learner Navigation Around Real TOEIC Workflows

**Purpose:** Make the app feel like a TOEIC learning product instead of one home page with random sections.

**Scope:**

- Routes:
  - `/today`
  - `/learn`
  - `/learn/:lessonId`
  - `/practice`
  - `/practice/:activityId`
  - `/review`
  - `/tests`
  - `/progress`
  - `/onboarding`
  - `/placement/:sessionId`
- Each route has one main job and one primary action.
- 7-part overview shows roadmap, locks, available work, content count, and next action.

**Pass Criteria:**

- Clicking each TOEIC part leads to a meaningful part-specific workflow or clear locked/unavailable state.
- UI does not expose admin/source terms to learners.

**Required Tests:**

- Playwright route smoke for all learner routes.
- Playwright part overview click flow.

**Verification:**

```bash
npm run build --prefix frontend
npx --prefix frontend playwright test tests/e2e/learner-routes.spec.ts
npx --prefix frontend playwright test tests/e2e/part-overview.spec.ts
```

**Commit:**

```text
feat(ui): build production TOEIC learner navigation
```

### E3. Apply Ocean Classroom Design System With Visual QA

**Purpose:** Raise UI quality to a marketable white/sea-blue education product.

**Scope:**

- Implement reusable tokens/components:
  - app shell
  - buttons
  - status badges
  - progress bars
  - locked chips
  - answer choices
  - audio controls
  - passage reader
  - table/filter controls for admin
- Use motion only for route entrance, answer selection, progress reveal, unlock, and review resolved states.

**Pass Criteria:**

- UI uses design tokens from `docs/product/08a-angular-design-system.md`.
- No generic hero page, random SVG decoration, purple gradients, or one-off card clutter.
- Desktop and mobile screenshots show no overlap or hidden headings.

**Required Tests:**

- Playwright screenshots for desktop and mobile:
  - Today
  - Placement
  - Lesson
  - Drill/Mini-test
  - Part Overview
  - Review

**Verification:**

```bash
npm run build --prefix frontend
npx --prefix frontend playwright test
```

**Commit:**

```text
feat(ui): apply Ocean Classroom learner design system
```

---

## Phase F: Admin Content Operations

### F1. Build Admin Source Inventory And Corpus Coverage

**Purpose:** Operators need to see what exists, what failed, and what can become learner content.

**Scope:**

- Admin inventory for:
  - source assets
  - rejected files
  - duplicate groups
  - extracted block counts
  - media classification
  - parser readiness
- Coverage dashboard by part and status.

**Pass Criteria:**

- Admin can tell exactly why DB has no published content or why a part is under-covered.
- Admin UI never pretends raw assets are learner lessons.

**Required Tests:**

- Admin API tests.
- Angular admin route smoke.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
npm run build --prefix frontend
```

**Commit:**

```text
feat(admin): show TOEIC source inventory and coverage
```

### F2. Build Draft Review, Validation Issue, And Publish Queues

**Purpose:** Scale content quality through controlled human/operator decisions.

**Scope:**

- Queue for drafts ready for review.
- Queue for validation issues.
- Approve/reject/relabel actions.
- Publish queue with pre-publish validation.
- Audit actor, timestamp, reason, resulting state.

**Pass Criteria:**

- Invalid content cannot be published from UI.
- Every publish decision is auditable.
- Admin can trace from published item back to source asset/page/block.

**Required Tests:**

- Authorization negative tests.
- Publish/reject audit tests.
- Admin E2E approve/reject flow.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
npm run build --prefix frontend
npx --prefix frontend playwright test
```

**Commit:**

```text
feat(admin): operate TOEIC draft review and publish queues
```

---

## Phase G: Practice Tests And Assessment

### G1. Generate Mini, Part, Section, And Full Test Definitions From Published Items

**Purpose:** Learners need real practice tests, not only isolated quizzes.

**Scope:**

- Create `published_tests` for:
  - mini tests
  - part tests
  - Listening section tests
  - Reading section tests
  - full TOEIC LR tests when enough content exists
- Enforce item count rules and part composition.
- If content is insufficient, show unavailable state with missing count.

**Pass Criteria:**

- Tests are generated only from published valid questions.
- Full TOEIC LR mode cannot be offered until enough Listening and Reading items exist.

**Required Tests:**

- Test blueprint validation tests.
- Insufficient-content tests.
- Score breakdown tests.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
```

**Commit:**

```text
feat(tests): generate TOEIC practice tests from published content
```

### G2. Complete Exam Runtime UX

**Purpose:** Practice tests must behave like serious test sessions.

**Scope:**

- Timer.
- Question navigator.
- Answer persistence.
- Resume after refresh.
- Submit confirmation.
- Result and repair plan.

**Pass Criteria:**

- Browser refresh does not lose answers.
- Submitted/expired session rejects new answers.
- Result creates repair assignments.

**Required Tests:**

- Backend session state tests.
- Playwright exam start-answer-submit-result flow.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
npm run build --prefix frontend
npx --prefix frontend playwright test tests/e2e/learner-flow.spec.ts
```

**Commit:**

```text
feat(tests): complete TOEIC exam runtime experience
```

---

## Phase H: Production Hardening

### H1. Authentication And Authorization

**Purpose:** Separate learner/admin access before market release.

**Scope:**

- Login/register/session restore.
- Learner ownership checks.
- Admin role protection.
- No learner access to admin/source/draft APIs.

**Pass Criteria:**

- Unauthorized requests receive stable safe error.
- Admin APIs require admin role.

**Verification:**

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
npm run build --prefix frontend
```

**Commit:**

```text
feat(auth): enforce TOEIC learner and admin access
```

### H2. PostgreSQL, Storage, Jobs, And Production Config

**Purpose:** Move beyond local SQLite-only development.

**Scope:**

- PostgreSQL migration parity with SQLite schema.
- Object storage for audio/image/PDF derivatives.
- Background jobs for extraction/parsing.
- Environment validation.

**Pass Criteria:**

- App fails fast if required production config is missing.
- Migration tests cover all production tables.
- Extraction jobs can retry and record failure reasons.

**Verification:**

```bash
dotnet build backend/ToeicSystem.sln
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
```

**Commit:**

```text
feat(platform): harden TOEIC production infrastructure
```

### H3. Observability, Backup, Security, And Release Gate

**Purpose:** Make the product operable after launch.

**Scope:**

- Correlation id.
- Structured logs.
- Health checks.
- Error taxonomy.
- Backup and restore rehearsal.
- Security headers/rate limit/CORS.
- Release checklist with evidence.

**Pass Criteria:**

- A release cannot be called ready without passing product, data, test, security, backup, and deployment checks.

**Verification:**

```bash
dotnet build backend/ToeicSystem.sln
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
npm run build --prefix frontend
npx --prefix frontend playwright test
```

**Commit:**

```text
chore(release): add TOEIC production readiness gates
```

---

## Current Next Task

Start with **A1**, then **A2**, then **B1**.

Do not start frontend redesign or learner route polishing again until **C2** proves at least one real published learning slice exists. Without published content, frontend work can only produce a cleaner empty shell, not a real TOEIC learning product.

## Completion Evidence Required Before Saying "Done"

The project is not complete until all of these are true:

- Runtime DB has non-zero `published_lessons`, `published_questions`, and `published_tests`.
- Published content covers all 7 TOEIC parts or the release gate explicitly limits beta scope.
- Placement returns real question payloads without answer leakage.
- Today Plan assigns real backend-owned work.
- Learner can complete lesson -> drill -> mini test -> review -> unlock flow.
- Wrong answers create review work.
- Full/section/part practice tests are available only when content coverage is sufficient.
- Angular UI is Vietnamese, clean, responsive, and driven by backend APIs.
- Admin can inspect source inventory, validation issues, review drafts, and publish content.
- Backend tests pass.
- Frontend build and unit tests pass.
- Playwright user-flow tests pass.
- Production config, auth, authorization, logging, backup, and release checklist are in place.
- Final commit is pushed to remote.

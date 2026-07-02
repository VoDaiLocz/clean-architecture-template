# P2 - Production Data Foundation

## Phase Goal

Create durable data structures for content, learners, assignments, attempts, reviews, and mastery.

| Task | Purpose | Key Pass Criteria | Commit |
| --- | --- | --- | --- |
| P2.1 Source/container/asset schema | Represent real source inventory and assets | Source, container, asset tables with indexes and statuses | `feat(p2.1): model TOEIC source assets` |
| P2.2 Extracted content schema | Store extracted PDF/web/audio metadata | Extracted pages and blocks persist with confidence | `feat(p2.2): model extracted TOEIC content` |
| P2.3 Draft content schema | Store parser output safely | Drafts cannot appear in learner APIs | `feat(p2.3): model TOEIC draft content` |
| P2.4 Published lesson schema | Store lessons and guided examples | Lessons linked to units and skill tags | `feat(p2.4): model published TOEIC lessons` |
| P2.5 Published question schema | Store part-specific questions | Required fields enforced by part | `feat(p2.5): model published TOEIC questions` |
| P2.6 Test schema | Store mini/part/skill/full tests | Test sections and item ordering persist | `feat(p2.6): model TOEIC test structures` |
| P2.7 Learner profile schema | Persist learner profile | Learner survives restart; goal fields stored | `feat(p2.7): model learner profiles` |
| P2.8 Assignment/attempt schema | Persist work lifecycle | Assignment and attempt relations enforced | `feat(p2.8): model learner assignments and attempts` |
| P2.9 Review/mastery schema | Persist review and unlock state | Wrong answer creates review; mastery queryable | `feat(p2.9): model review and mastery records` |
| P2.10 Integrity and indexes | Protect data quality and performance | FK/index tests pass; invalid rows rejected | `feat(p2.10): enforce TOEIC data integrity` |

## Required Tests For P2

- migration test
- repository integration test
- invalid insert rejection
- idempotent write test where applicable
- domain rule test for required relationships

## P2.1 - Model TOEIC Source Assets

**Context:** Content Factory  
**Purpose:** Represent real source inventory containers and concrete media/document assets before extraction.  
**User/Business Value:** Enables the platform to track PDFs, audio, images, transcripts, and answer keys from the user's TOEIC corpus without exposing raw Drive/PDF navigation to learners.  
**Dependencies:** P1.3, P1.4.  
**Detailed Scope:** Add source container and source asset domain records; add asset role enum; add repository upsert/query methods; add SQLite local/test tables and indexes; add PostgreSQL migration `002_source_assets`; add tests and docs.  
**Out Of Scope:** Drive discovery adapter, object upload, extraction, parser jobs, learner APIs, admin UI.  
**Data Contract:** `source_containers` belongs to `source_manifest_entries`; `source_assets` belongs to both source container and source manifest entry; asset metadata includes role, mime type, extension, size, provider URL, object key, and checksum.  
**API Contract:** none for P2.1.  
**UI Contract:** none for P2.1.  
**Business Rules:** Source assets store metadata/object key only, not raw bytes; upserts are idempotent; asset role is explicit.  
**Edge Cases:** repeated discovery updates existing rows; assets are queryable by container; migration must create indexes for source/container/role lookup.  
**Required Tests:** Repository integration test covers idempotent container/asset upsert and readback; migration test covers `002_source_assets`.  
**Acceptance Criteria:** Domain records exist; repository methods exist; SQLite schema exists; PostgreSQL migration exists; tests and build pass; data model doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "SourceContainer|SourceAsset|002_source_assets|source_assets" backend/src backend/tests docs/product`.  
**Definition Of Done:** Source asset model is committed and pushed.  
**Commit:** `feat(p2.1): model TOEIC source assets`  
**Push:** `git push origin main`

## P2.2 - Model Extracted TOEIC Content

**Context:** Content Factory  
**Purpose:** Persist extracted pages and text blocks from source assets before parser normalization.  
**User/Business Value:** Gives the content factory reliable evidence for parsing, validation, and human review instead of re-reading PDFs or web pages repeatedly.  
**Dependencies:** P2.1.  
**Detailed Scope:** Add extracted page and extracted text block domain records; add block type enum; add repository upsert/query methods; add SQLite local/test tables and indexes; add PostgreSQL migration `003_extracted_content`; add tests and docs.  
**Out Of Scope:** PDF parser implementation, audio transcript extraction, OCR, draft item parsing, learner APIs, admin UI.  
**Data Contract:** `extracted_pages` belongs to `source_assets`; `extracted_text_blocks` belongs to both source asset and extracted page; blocks include type, text, confidence, and coordinates JSON.  
**API Contract:** none for P2.2.  
**UI Contract:** none for P2.2.  
**Business Rules:** Extraction output is evidence/draft data, not learner-facing curriculum; confidence and coordinates must be preserved.  
**Edge Cases:** repeated extraction updates existing page/block rows; blocks are queryable by asset and ordered by page/block; migration must create indexes for asset/page lookup.  
**Required Tests:** Repository integration test covers idempotent page/block upsert and readback; migration test covers `003_extracted_content`.  
**Acceptance Criteria:** Domain records exist; repository methods exist; SQLite schema exists; PostgreSQL migration exists; tests and build pass; extracted content doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "ExtractedPage|ExtractedTextBlock|003_extracted_content|extracted_text_blocks" backend/src backend/tests docs/product`.  
**Definition Of Done:** Extracted content model is committed and pushed.  
**Commit:** `feat(p2.2): model extracted TOEIC content`  
**Push:** `git push origin main`

## P2.3 - Model TOEIC Draft Content

**Context:** Content Factory  
**Purpose:** Persist parser output safely before validation, review, and publishing.  
**User/Business Value:** Prevents low-confidence or invalid parser output from leaking to learners while keeping source trace for content operations.  
**Dependencies:** P2.1, P2.2.  
**Detailed Scope:** Add draft content domain record and status enum; add repository upsert/query methods; add SQLite local/test table and index; add PostgreSQL migration `004_draft_content`; add tests proving idempotent persistence and learner API safety; add docs.  
**Out Of Scope:** validation workflow, publish workflow, admin review UI, parser implementation, learner APIs.  
**Data Contract:** `draft_content_items` belongs to `source_assets`; stores material class, optional TOEIC part, item type, payload JSON, source trace JSON, parser confidence, and status.  
**API Contract:** learner API contracts must not expose draft content.  
**UI Contract:** none for P2.3.  
**Business Rules:** Draft content is not learner-visible curriculum; parser confidence and source trace must persist; draft status is explicit.  
**Edge Cases:** repeated parser runs update existing draft row; drafts are queryable by asset; migration must index asset/status workflow lookup.  
**Required Tests:** Repository integration test covers idempotent draft upsert/readback; migration test covers `004_draft_content`; API contract test verifies learner contracts do not expose drafts.  
**Acceptance Criteria:** Domain records exist; repository methods exist; SQLite schema exists; PostgreSQL migration exists; tests and build pass; draft content doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "DraftContentItem|DraftContentStatus|004_draft_content|draft_content_items" backend/src backend/tests docs/product`.  
**Definition Of Done:** Draft content model is committed and pushed.  
**Commit:** `feat(p2.3): model TOEIC draft content`  
**Push:** `git push origin main`

## P2.4 - Model Published TOEIC Lessons

**Context:** Learning Content  
**Purpose:** Persist learner-ready lessons and guided examples after validation/review.  
**User/Business Value:** Enables the platform to teach before testing, using DB-backed curriculum instead of frontend hardcoded content or draft parser output.  
**Dependencies:** P2.3.  
**Detailed Scope:** Add published lesson and guided example domain records; add published content status enum; add repository upsert/query methods; add SQLite local/test tables and indexes; add PostgreSQL migration `005_published_lessons`; add tests and docs.  
**Out Of Scope:** question schema, tests schema, learner path assignment, frontend lesson UX, publish workflow implementation.  
**Data Contract:** `published_lessons` stores unit id, TOEIC part, title, objective, skill tags, source trace JSON, and status; `guided_examples` belongs to a lesson and has display order.  
**API Contract:** none for P2.4.  
**UI Contract:** future learner UI must read published lesson content, not draft content.  
**Business Rules:** Published lesson content is separate from draft content; guided examples are ordered; upserts are idempotent.  
**Edge Cases:** repeated publish updates existing lesson/example rows; examples are queryable in display order; migration must index unit/status and lesson/order lookup.  
**Required Tests:** Repository integration test covers idempotent lesson/example upsert and readback; migration test covers `005_published_lessons`.  
**Acceptance Criteria:** Domain records exist; repository methods exist; SQLite schema exists; PostgreSQL migration exists; tests and build pass; published lesson doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "PublishedLesson|GuidedExample|005_published_lessons|published_lessons" backend/src backend/tests docs/product`.  
**Definition Of Done:** Published lesson model is committed and pushed.  
**Commit:** `feat(p2.4): model published TOEIC lessons`  
**Push:** `git push origin main`

## P2.5 - Model Published TOEIC Questions

**Context:** Learning Content  
**Purpose:** Persist learner-ready TOEIC questions with part-specific required fields after validation/review.  
**User/Business Value:** Enables drills, mini tests, and future test modes to use validated DB-backed questions instead of hardcoded frontend data or raw extracted PDF content.  
**Dependencies:** P2.4.  
**Detailed Scope:** Add published question domain record; add question type enum; add part-specific validation rules; add repository upsert/query methods; add SQLite local/test table and indexes; add PostgreSQL migration `006_published_questions`; add tests and docs.  
**Out Of Scope:** test section/item ordering, attempt scoring, review scheduling, frontend practice UI, parser-to-publish workflow, passage/group tables.  
**Data Contract:** `published_questions` belongs to `published_lessons`; stores TOEIC part, question type, prompt, options JSON, correct answer, explanation, optional media asset id, optional passage id, optional group id, evidence JSON, skill tags, source trace JSON, and status.  
**API Contract:** none for P2.5. Future learner APIs must read from `published_questions`, not draft parser output.  
**UI Contract:** none for P2.5. Future learner screens must render media/passage/group context based on these persisted fields.  
**Business Rules:** TOEIC part must be 1-7; every question needs prompt/options/correct answer/explanation/evidence/source trace; Part 1 requires media; Part 3 and Part 4 require group relationship; Part 6 and Part 7 require passage context; upserts are idempotent.  
**Edge Cases:** repeated publish updates existing question row; invalid part-specific rows are rejected before persistence; questions are queryable by TOEIC part/status lookup index.  
**Required Tests:** Repository integration test covers idempotent question upsert/readback; domain rule test rejects invalid Part 1 and Part 7 rows; migration test covers `006_published_questions`.  
**Acceptance Criteria:** Domain records exist; part validation rules exist; repository methods exist; SQLite schema exists; PostgreSQL migration exists; tests and build pass; published question doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "PublishedQuestion|006_published_questions|published_questions" backend/src backend/tests docs/product`.  
**Definition Of Done:** Published question model is committed and pushed.  
**Commit:** `feat(p2.5): model published TOEIC questions`  
**Push:** `git push origin main`

## P2.6 - Model TOEIC Test Structures

**Context:** Learning Content  
**Purpose:** Persist TOEIC mini, part, skill, and full practice test structures with sections and ordered question items.  
**User/Business Value:** Enables production practice modes to be assembled from published DB content instead of frontend hardcoded quizzes, while preserving TOEIC section counts and item order.  
**Dependencies:** P2.5.  
**Detailed Scope:** Add published test, test section, and test item domain records; add test mode and section type enums; add repository upsert/query methods; add SQLite local/test tables and indexes; add PostgreSQL migration `007_published_tests`; add tests and docs.  
**Out Of Scope:** attempt lifecycle, scoring conversion table, timer runtime behavior, learner assignment, review creation, frontend test player.  
**Data Contract:** `published_tests` stores mode, title, target question count, duration, source trace, and status; `published_test_sections` belongs to a test and stores section type/order/count/duration; `published_test_items` belongs to a section and stores question id, TOEIC part, display order, and score weight.  
**API Contract:** none for P2.6. Future learner test APIs must expose ordered sections/items from these tables.  
**UI Contract:** none for P2.6. Future test UI must render by section order and item order.  
**Business Rules:** Full TOEIC tests must represent 200 questions; counts/durations/order/score weight must be positive; test items must reference TOEIC part 1-7; upserts are idempotent; ordered items are stable by section/display order.  
**Edge Cases:** repeated publish updates existing test row; sections inserted out of order still query in display order; items inserted out of order still query in display order; invalid full-test question count is rejected before persistence.  
**Required Tests:** Repository integration test covers idempotent test upsert/readback, Listening/Reading sections, ordered items, and invalid full-test count rejection; migration test covers `007_published_tests`.  
**Acceptance Criteria:** Domain records exist; repository methods exist; SQLite schema exists; PostgreSQL migration exists; tests and build pass; published test doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "PublishedTest|007_published_tests|published_test" backend/src backend/tests docs/product`.  
**Definition Of Done:** TOEIC test structure model is committed and pushed.  
**Commit:** `feat(p2.6): model TOEIC test structures`  
**Push:** `git push origin main`

## P2.7 - Model Learner Profiles

**Context:** Learner Journey  
**Purpose:** Persist learner identity, TOEIC score goals, current estimated score, study minutes, timezone, and lifecycle status.  
**User/Business Value:** Gives the platform durable learner state across sessions and restarts so onboarding, Today Plan, assignment, and mastery logic can be personalized.  
**Dependencies:** P1.3.  
**Detailed Scope:** Add learner profile domain record and status enum; add repository upsert/read methods; add SQLite local/test table and index; add PostgreSQL migration `008_learner_profiles`; add restart persistence test and docs.  
**Out Of Scope:** authentication, password/session management, placement test results, learning path assignment, subscription, frontend onboarding UI.  
**Data Contract:** `learner_profiles` stores learner id, display name, email, target TOEIC score, current estimated score, daily study minutes, timezone, status, created timestamp, and updated timestamp.  
**API Contract:** none for P2.7. Future learner APIs must derive personalization from `learner_profiles`, not `DemoLearnerSession`.  
**UI Contract:** none for P2.7. Future onboarding/profile UI must write to this model.  
**Business Rules:** Learner id/display name/email/timezone are required; target score must be within TOEIC score bounds; current estimated score must be within TOEIC score bounds; daily study minutes must be positive; upserts are idempotent; profile must survive repository restart.  
**Edge Cases:** repeated onboarding updates an existing profile; persisted file-backed repository can be reopened and still read the learner; invalid score/minutes are rejected before persistence.  
**Required Tests:** Repository restart test covers profile insert, update, idempotent count, close/reopen, and readback; migration test covers `008_learner_profiles`.  
**Acceptance Criteria:** Domain record exists; repository methods exist; SQLite schema exists; PostgreSQL migration exists; tests and build pass; learner profile doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "LearnerProfile|008_learner_profiles|learner_profiles" backend/src backend/tests docs/product`.  
**Definition Of Done:** Learner profile model is committed and pushed.  
**Commit:** `feat(p2.7): model learner profiles`  
**Push:** `git push origin main`

## P2.8 - Model Learner Assignments And Attempts

**Context:** Learner Journey / Attempt And Review  
**Purpose:** Persist the learner work lifecycle from assigned work to activity session, submitted attempt, and per-question answers.  
**User/Business Value:** Makes Today Plan and practice work auditable and replayable, enabling scoring, review creation, mastery, and analytics in later tasks.  
**Dependencies:** P2.7.  
**Detailed Scope:** Add learner assignment, activity session, learner attempt, and attempt answer domain records; add lifecycle enums and validation rules; add repository upsert/query methods; add SQLite local/test tables and indexes; add PostgreSQL migration `009_learner_assignments_attempts`; add tests and docs.  
**Out Of Scope:** review item creation, mastery update, TOEIC scaled score conversion, timer enforcement, frontend activity player, assignment recommendation engine.  
**Data Contract:** `learner_assignments` belongs to learner profile; `activity_sessions` belongs to assignment and learner; `learner_attempts` belongs to session and learner; `attempt_answers` belongs to attempt and stores question id, learner answer, correct answer, correctness, and timestamp.  
**API Contract:** none for P2.8. Future attempt APIs must write to these lifecycle tables.  
**UI Contract:** none for P2.8. Future learner UI must submit attempts through backend lifecycle, not local-only state.  
**Business Rules:** Assignment/session/attempt/answer ids are required; attempts require positive total count, correct count between zero and total count, and score percent 0-100; upserts are idempotent; attempts reference learner and session; answers reference attempt.  
**Edge Cases:** repeated assignment updates status; session can move from in-progress to completed; invalid attempt counts are rejected before persistence; ordered reads are deterministic by lifecycle timestamp/id.  
**Required Tests:** Repository integration test covers assignment, session, attempt, answer persistence and lifecycle update; invalid attempt count rejection; migration test covers `009_learner_assignments_attempts`.  
**Acceptance Criteria:** Domain records exist; repository methods exist; SQLite schema exists; PostgreSQL migration exists; tests and build pass; lifecycle doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "LearnerAssignment|ActivitySession|LearnerAttempt|AttemptAnswer|009_learner_assignments_attempts" backend/src backend/tests docs/product`.  
**Definition Of Done:** Learner assignment and attempt model is committed and pushed.  
**Commit:** `feat(p2.8): model learner assignments and attempts`  
**Push:** `git push origin main`

# TOEIC Production Task Register

## Purpose

This register is the execution contract for every production task. Each row defines what the task is, why it exists, what contracts it touches, how it passes, what tests are required, and the exact commit/push rule.

Every implementation plan must expand one row into step-by-step TDD work before coding. A row is intentionally strict: if a developer cannot satisfy the pass criteria and tests, the task is not complete.

## Global Task Rule

For every task:

- Write failing test first.
- Implement production code only after the failing test exists.
- Run the verification commands listed in the row.
- Commit with the exact commit message.
- Push immediately to remote.

Default push:

```bash
git push origin main
```

## How To Read A Task

Each task row is complete only when read together with:

1. the row itself,
2. the global task contract below,
3. the phase execution contract for the task phase,
4. the standalone task specification linked from the phase index and `../00-master-spec.md`.

The standalone task specification is the source of truth for detailed scope, out-of-scope boundaries, data/API/UI contracts, business rules, edge cases, required tests, acceptance criteria, verification, commit, and push. The register is a high-level control table only. If a register row conflicts with a standalone task spec, stop and update the register or the spec before coding; do not let developers choose one interpretation silently.

## Global Task Contract

These fields apply to every task unless the phase contract explicitly narrows them.

**Context Owner:** The bounded context named by the phase. Ownership cannot be bypassed by writing directly into another context's tables, services, routes, or UI state.

**Dependencies:** Earlier tasks in the same phase plus all prior phases that provide required platform capability. A later task may start earlier only if its tests use stable contracts and do not fake completed business behavior.

**Detailed Scope:** Implement only the production behavior required by the row. Include persistence, validation, API/worker contracts, UI integration, and tests only when the row or phase contract requires them.

**Out Of Scope:** Payment, TOEIC Speaking/Writing, mobile apps, tutor marketplace, social features, learner-facing raw PDFs, raw Drive links, frontend-owned scoring, frontend-owned unlock logic, and hardcoded production learning content.

**Data Contract:** Data must be persisted in normalized tables or read models owned by the context. Every learner-visible object must be traceable to a published content record or learner state record. Source assets and draft extraction data are never directly exposed as learner curriculum.

**API Contract:** API responses must be typed, stable, version-aware where needed, and free of persistence internals. Learner APIs must not leak admin/source terminology. Admin APIs must expose enough evidence to operate content quality gates.

**UI Contract:** Learner UI must show the next meaningful action, current progress, lock reason, result, or review requirement. Empty placeholder cards, fake static questions, source-file navigation, and admin vocabulary in learner screens fail the task.

**Business Rules:** Backend owns placement, assignment, scoring, mastery, unlock, review, publishing, and validation. A task cannot move these decisions into frontend code.

**Edge Cases:** Every task must cover missing required data, duplicate command submission, unauthorized access when applicable, invalid state transition, failed dependency, and idempotent retry where the operation can be repeated.

**Required Tests:** Start with a failing test. Minimum layer is determined by the phase contract. Any learner journey task that changes visible behavior needs at least one Playwright or API-level user-flow check.

**Acceptance Criteria:** The task passes only when its row pass criteria, phase contract, required tests, build, and spec traceability are all satisfied.

**Verification Commands:** Run the row checks plus the relevant base commands from `../09-quality-strategy.md`. If a command cannot run, document the exact reason in the commit or PR notes before pushing.

**Definition Of Done:** Tests pass, build passes, no unrelated files are staged, docs remain consistent, exact commit message is used, and the commit is pushed to remote.

**Push Requirement:** Every task must run `git push origin main` immediately after commit unless the team explicitly works on a named feature branch; then push that branch immediately.

## Phase Execution Contracts

### P0 - Product And Documentation Reset

**Context Owner:** Product architecture.
**Data Contract:** Documentation only. No database schema or seed content changes.
**API Contract:** Documentation only. Any API mentioned must be marked as required future contract, not implemented behavior.
**UI Contract:** Documentation only. UI references must define learner/admin intent without adding screens.
**Business Rules:** Establish non-negotiable rules for DB-first content, no raw PDF learner flow, and commit-push discipline.
**Edge Cases:** Conflicting old specs, vague language, missing task fields, and unpushed local-only work.
**Required Tests:** Documentation structure scan, placeholder scan, old-spec removal check, git status check before staging.
**Definition Of Done:** Official `docs/product` spec exists, superseded TOEIC specs are removed, no code is touched, and the docs commit is pushed.

### P1 - Architecture And Infrastructure

**Context Owner:** Platform architecture.
**Data Contract:** Define storage boundaries, migrations, configuration, health, and job metadata without modeling learner curriculum yet.
**API Contract:** Health and typed-contract conventions must be stable enough for frontend and deployment automation.
**UI Contract:** No production learner UI unless needed for a health/admin smoke screen.
**Business Rules:** Clean Architecture dependency direction is mandatory; infrastructure may implement ports but cannot own domain decisions.
**Edge Cases:** Missing environment variable, unavailable DB/storage/worker, retry exhaustion, secret accidentally committed, contract drift.
**Required Tests:** Dependency/build checks, configuration tests, storage test double tests, job retry tests, health smoke tests, CI verification.
**Definition Of Done:** Platform capability is testable locally, documented for staging/production, and cannot be bypassed by feature teams.

### P2 - Production Data Foundation

**Context Owner:** Learning content, learner state, attempts, mastery.
**Data Contract:** Add normalized entities, constraints, indexes, and repositories for sources, assets, extracted blocks, drafts, published lessons/questions/tests, learner state, attempts, reviews, and mastery.
**API Contract:** API exposure is optional in this phase; repository/application contracts are required. If an API is added, it must not expose drafts to learners.
**UI Contract:** No learner UI requirement. Admin/debug visibility is allowed only for validation.
**Business Rules:** Draft content is never learner-visible; published content must satisfy part validation; learner state must survive restart.
**Edge Cases:** Duplicate imports, missing FK targets, invalid part-specific required fields, orphan attempts, stale mastery rows, query performance regressions.
**Required Tests:** Migration tests, repository integration tests, domain invariant tests, negative tests for invalid rows.
**Definition Of Done:** The database can represent the complete TOEIC LR product model before extraction and UX scale.

### P3 - Content Factory Pipeline

**Context Owner:** Content factory.
**Data Contract:** Import source manifest, discover assets, store extraction output, draft items, validation issues, review decisions, and publish records with source traceability.
**API Contract:** Admin/operator APIs may expose source, job, draft, issue, and publish state. Learner APIs can consume only published content.
**UI Contract:** Admin workflow only; learner screens remain unaffected until content is published.
**Business Rules:** Extraction is evidence-generating, not automatically trusted. Human review or explicit validation policy is required before publish.
**Edge Cases:** Blocked Google/Drive access, shortlink redirect failure, corrupted PDF/audio, low-confidence parsing, missing transcript, duplicate asset, partial job failure.
**Required Tests:** Parser fixture tests, mocked external adapter tests, idempotent import tests, validation-policy tests, publish/reject application tests.
**Definition Of Done:** The system can convert real audited TOEIC materials into reviewed, validated, learner-safe published content.

### P4 - Learner Journey Core

**Context Owner:** Learner journey.
**Data Contract:** Persist learner profile, placement sessions/results, learning path, assignments, activity sessions, attempts, review items, and mastery records.
**API Contract:** Learner APIs must return next action, lock reasons, results, review requirements, and progress from backend state.
**UI Contract:** UI may consume the APIs but cannot decide placement, Today Plan, scoring, unlock, or review creation.
**Business Rules:** Teach before drill, drill before mini test, wrong answer creates review, unresolved blockers prevent gated unlock.
**Edge Cases:** Repeat onboarding, duplicate active placement, resume interrupted activity, submit same attempt twice, stale assignment, review blocker conflict.
**Required Tests:** Domain tests for scoring/unlock, application tests for assignment/review, API tests for lifecycle, at least one user-flow check for visible journey behavior when UI changes.
**Definition Of Done:** A learner can be diagnosed, assigned work, complete activities, repair mistakes, and progress through backend-owned state.

### P5 - TOEIC Part Engines

**Context Owner:** TOEIC part engine.
**Data Contract:** Each part has explicit required media/content fields, grouped relationships where needed, answer/explanation/evidence, skill tags, and validation status.
**API Contract:** Part item payloads must be consistent enough for shared clients while preserving part-specific fields.
**UI Contract:** UI must render each TOEIC part in the correct learning/practice mode, including media controls for listening and passage context for reading.
**Business Rules:** Part 1 requires image and audio; Parts 2-4 require audio; Parts 3-4 require grouped questions; Parts 6-7 require passage context; answer explanations are required for learning modes.
**Edge Cases:** Missing media, mismatched group/question count, answer key mismatch, unsupported question type, missing evidence, transcript unavailable.
**Required Tests:** Part validation tests, scorer tests, API serialization tests, and UI/E2E tests for any part-specific screen.
**Definition Of Done:** All 7 TOEIC parts can be represented, validated, served, attempted, scored, and reviewed without fake content.

### P6 - Practice Test System

**Context Owner:** Attempt and assessment.
**Data Contract:** Persist test definitions, sections, ordered items, sessions, timer state, submissions, score breakdowns, and repair assignments.
**API Contract:** Test APIs must support start/resume/answer/navigate/submit/result and reject invalid session states.
**UI Contract:** Exam UI must support timer, navigation, unanswered indicators, section boundaries, submit confirmation, and result review.
**Business Rules:** Full TOEIC LR test has 200 questions; exam mode disables hints; expired/submitted sessions cannot accept new answers; results generate repair work.
**Edge Cases:** Browser refresh, expired timer, double submit, missing item, interrupted network, section switch, unanswered questions, score recalculation attempt.
**Required Tests:** Domain tests for timing/state, application tests for scoring/repair, API tests for session lifecycle, E2E for exam start-submit-result.
**Definition Of Done:** Learners can take mini, part, section, and full tests with reliable scoring and actionable follow-up.

### P7 - Learner UX Production

**Context Owner:** Learner experience.
**Data Contract:** UI reads only learner-facing API/read-model data. No hardcoded production questions, vocabulary, progress, lock state, or score.
**API Contract:** Every screen must declare the API it consumes and the loading/error/empty states it supports.
**UI Contract:** Angular is the production frontend framework. Navigation is centered on Today, Learn, Practice, Review, Tests, and Progress. The 7 parts are entry points into structured work, not empty cards.
**Business Rules:** UI displays backend decisions; it does not invent unlocks, mastery, fake progress, or recommendations.
**Edge Cases:** No published content, locked unit, pending review blocker, failed API, slow media load, mobile viewport, keyboard-only use.
**Required Tests:** Frontend build, Playwright smoke/user-flow tests, accessibility checks for core workflows, visual sanity for desktop and mobile when layout changes.
**Definition Of Done:** A real learner can understand what to do next, why something is locked, how to study, how to practice, and how to review mistakes.

### P8 - Admin Content Operations

**Context Owner:** Content operations.
**Data Contract:** Admin screens operate on sources, assets, jobs, drafts, validation issues, review decisions, publish state, and coverage metrics.
**API Contract:** Admin APIs must be role-protected, auditable, filterable, and able to show source evidence for every draft/published item.
**UI Contract:** Admin UI prioritizes queues, issue resolution, retry, approve/reject, relabel, publish, and coverage visibility.
**Business Rules:** Admin actions must not silently publish invalid content; every decision records actor, time, reason, and resulting state.
**Edge Cases:** Concurrent review, retrying failed jobs, rejecting already published content, missing source evidence, bulk action partial failure.
**Required Tests:** Admin API tests, admin E2E tests, authorization negative tests, audit trail assertions.
**Definition Of Done:** Operators can move content from source inventory to learner-safe publication with evidence and control.

### P9 - Production Hardening

**Context Owner:** Platform operations and security.
**Data Contract:** Add operational records only where needed for auth, audit, observability, backup, migration, and release readiness.
**API Contract:** Auth, authorization, error, health, observability, and release endpoints/contracts must be stable and documented.
**UI Contract:** User-facing errors must be understandable; admin-only operational screens must be role-protected.
**Business Rules:** No learner can access admin APIs; no raw exception leaks; production release requires passing gates, backup/restore confidence, and CI/CD evidence.
**Edge Cases:** Expired session, unauthorized role, dependency outage, migration failure, secret exposure, rate spikes, rollback, restore rehearsal failure.
**Required Tests:** Auth tests, authorization tests, error contract tests, security scans, performance smoke, backup/restore rehearsal, CI/CD green run.
**Definition Of Done:** The product can be deployed, operated, monitored, secured, backed up, and released with explicit go/no-go evidence.

## P0 - Product And Documentation Reset

| ID | Purpose | User/Business Value | Contracts And Scope | Pass Criteria | Required Tests/Checks | Commit |
| --- | --- | --- | --- | --- | --- | --- |
| P0.1 | Reset official product docs | Team has one source of truth | Docs only; delete conflicting TOEIC specs; create `docs/product` | `docs/product` exists; old TOEIC specs removed; no code touched | `find docs/product -type f`; `rg TBD docs/product` | `docs(p0.1): reset TOEIC product specification` |
| P0.2 | Deprecate demo learner direction | Prevent demo flow from becoming product | Mark `DemoLearnerSession` and FE fake content as non-production | New production work cannot depend on demo session | `rg DemoLearnerSession backend/src`; architecture note exists | `chore(p0.2): mark demo learner flow as non-production` |
| P0.3 | Define bounded context ownership | Allows team parallel work without coupling | Content Factory, Learning Content, Learner Journey, Attempt/Review, Analytics | Every future task maps to one owner | Docs check for context ownership table | `docs(p0.3): define TOEIC bounded context ownership` |
| P0.4 | Define task execution standard | Ensures professional delivery | Task template, DoD, test policy, commit/push rule | Every phase task follows template | `rg "Commit:" docs/product/11-task-breakdown` | `docs(p0.4): define TOEIC task execution standard` |
| P0.5 | Define release gate policy | Prevent premature market release | Alpha, private beta, public beta, market gates | Each gate has required features and quality checks | Release plan review | `docs(p0.5): define TOEIC release gate policy` |

## P1 - Architecture And Infrastructure

| ID | Purpose | User/Business Value | Contracts And Scope | Pass Criteria | Required Tests/Checks | Commit |
| --- | --- | --- | --- | --- | --- | --- |
| P1.1 | Define backend module boundaries | Prevents tangled codebase | Domain/Application/Infrastructure/Api context namespaces | Domain has no forbidden dependency; context folders exist | build plus dependency review | `docs(p1.1): define backend module boundaries` |
| P1.2 | Add production configuration strategy | Enables staging/production safely | config keys for DB, storage, worker, auth, logging | no secrets committed; env-specific config documented | build; secret scan by `rg "password|secret"` | `chore(p1.2): add production configuration strategy` |
| P1.3 | Add PostgreSQL migration foundation | Enables real production DB | migration project/tooling and local test strategy | migration can create empty production schema | migration test; `dotnet build` | `feat(p1.3): add PostgreSQL migration foundation` |
| P1.4 | Add object storage abstraction | Stores audio/PDF/image outside DB | `IObjectStorage` contract; local test double | upload/download/delete/list covered | unit tests for storage abstraction | `feat(p1.4): add object storage abstraction` |
| P1.5 | Add background job foundation | Extraction can run reliably | job entity, queue abstraction, retry policy | failed jobs retry and record failure reason | application tests for retry/failure | `feat(p1.5): add background job foundation` |
| P1.6 | Add typed API contract convention | Prevents FE/API drift | typed client or OpenAPI generation strategy | frontend consumes generated or typed contracts | API contract test; frontend build | `feat(p1.6): add typed API contract convention` |
| P1.7 | Add CI baseline | Stops broken commits entering remote | CI runs restore/build/tests | CI passes on push | workflow run green | `ci(p1.7): add backend quality gate` |
| P1.8 | Add health checks | Enables deployment readiness | API health endpoints for DB, worker, storage | unhealthy dependency returns unhealthy | API smoke test | `feat(p1.8): add platform health checks` |

## P2 - Production Data Foundation

| ID | Purpose | User/Business Value | Contracts And Scope | Pass Criteria | Required Tests/Checks | Commit |
| --- | --- | --- | --- | --- | --- | --- |
| P2.1 | Model source assets | Content team can track source library | source/container/asset tables and repository | blocked, accessible, discovered states persist | repository integration tests | `feat(p2.1): model TOEIC source assets` |
| P2.2 | Model extracted content | Parser can consume PDF/web output | extracted pages, blocks, confidence, coordinates | extraction output persists idempotently | migration + repository tests | `feat(p2.2): model extracted TOEIC content` |
| P2.3 | Model draft content | Bad content stays away from learners | draft item, parser run, validation status | draft not visible through learner APIs | API negative test | `feat(p2.3): model TOEIC draft content` |
| P2.4 | Model lessons | Learners can study structured lessons | lesson, guided example, skill tags | lesson linked to unit and objective | repository + domain tests | `feat(p2.4): model published TOEIC lessons` |
| P2.5 | Model questions | Supports 7 TOEIC part item types | published question with part-specific fields | invalid missing required fields rejected | part validation tests | `feat(p2.5): model published TOEIC questions` |
| P2.6 | Model tests | Enables mini/part/skill/full tests | test, section, ordered test items | TOEIC question counts representable | repository tests | `feat(p2.6): model TOEIC test structures` |
| P2.7 | Model learner profiles | User state survives sessions | learner profile fields and goal settings | profile persists across restart | repository tests | `feat(p2.7): model learner profiles` |
| P2.8 | Model assignments/attempts | Work lifecycle is auditable | assignment, activity session, attempt, answer | attempt references learner and content | FK tests; application tests | `feat(p2.8): model learner assignments and attempts` |
| P2.9 | Model review/mastery | Mistakes and unlock state persist | review item, repair attempt, mastery record | wrong answer can create review and blocker | domain + repository tests | `feat(p2.9): model review and mastery records` |
| P2.10 | Add integrity and indexes | Production data remains reliable | FK, unique constraints, indexes | invalid rows fail; key queries indexed | migration tests; query plan review | `feat(p2.10): enforce TOEIC data integrity` |

## P3 - Content Factory Pipeline

| ID | Purpose | User/Business Value | Contracts And Scope | Pass Criteria | Required Tests/Checks | Commit |
| --- | --- | --- | --- | --- | --- | --- |
| P3.1 | Import 73 audited sources | Starts from real material inventory | source manifest import use case | 73 total, 13 blocked, evidence counts match audit | application tests | `feat(p3.1): import TOEIC source manifest` |
| P3.2 | Discover Drive assets | Finds real files inside folders | Drive discovery adapter and source assets | children persisted; blocked access issue recorded | mocked Drive tests | `feat(p3.2): discover Drive source assets` |
| P3.3 | Resolve external sources | Makes shortlinks/web stable | resolver stores original and final URL | redirect chain captured; failure handled | resolver tests | `feat(p3.3): resolve TOEIC external sources` |
| P3.4 | Register media assets | Prepares PDF/audio/image processing | asset role detection | PDF/audio/image roles stored | classification tests | `feat(p3.4): register TOEIC source assets` |
| P3.5 | Extract PDF blocks | Makes books machine-readable | PDF extraction job writes pages/blocks | fixture PDF creates blocks with confidence | parser fixture tests | `feat(p3.5): extract TOEIC PDF blocks` |
| P3.6 | Extract audio metadata | Enables listening validation | duration, format, track metadata | invalid audio rejected; valid metadata stored | media tests | `feat(p3.6): extract TOEIC audio metadata` |
| P3.7 | Parse answer keys | Allows scoring | answer key parser and draft mappings | answer mappings created with confidence | fixture tests | `feat(p3.7): parse TOEIC answer keys` |
| P3.8 | Parse transcripts | Enables listening review | transcript parser and linking | transcript linked to asset/test group | fixture tests | `feat(p3.8): parse TOEIC transcripts` |
| P3.9 | Parse reading drafts | Creates Part 5/7 draft content | parser profiles for reading | drafts have source trace and tags | parser tests | `feat(p3.9): parse TOEIC reading drafts` |
| P3.10 | Parse listening groups | Creates Part 1-4 draft content | grouped item parser | Part 3/4 group relationships created | parser tests | `feat(p3.10): parse TOEIC listening groups` |
| P3.11 | Validate drafts | Blocks unsafe content | validation policies per part | invalid drafts create issues; do not publish | domain tests | `feat(p3.11): validate TOEIC draft content` |
| P3.12 | Review and publish | Human gate creates learner content | review decision and publish use case | approved valid draft publishes; rejected hidden | application + API tests | `feat(p3.12): publish reviewed TOEIC content` |

## P4 - Learner Journey Core

| ID | Purpose | User/Business Value | Contracts And Scope | Pass Criteria | Required Tests/Checks | Commit |
| --- | --- | --- | --- | --- | --- | --- |
| P4.1 | Onboarding | Personalizes learning | create/update profile API | profile persisted; next action returned | API + application tests | `feat(p4.1): implement learner onboarding` |
| P4.2 | Profile persistence | Removes memory-only learner | persisted learner state | restart does not lose profile | repository test | `feat(p4.2): persist learner profile state` |
| P4.3 | Start placement | Begins diagnosis | placement session API | active duplicate handled | API tests | `feat(p4.3): start TOEIC placement session` |
| P4.4 | Score placement | Creates diagnosis | scoring by part and skill tags | result has part breakdown | domain tests | `feat(p4.4): score TOEIC placement` |
| P4.5 | Generate path | Turns diagnosis into plan | learning path generator | starting unit reflects weaknesses | domain/application tests | `feat(p4.5): generate learner path from placement` |
| P4.6 | Today Plan | Shows next best activity | assignment engine | review blocker outranks new lesson | application tests | `feat(p4.6): assign learner today plan` |
| P4.7 | Activity lifecycle | Tracks progress | activity session states | start/resume/complete persist | application tests | `feat(p4.7): manage learner activity sessions` |
| P4.8 | Submit attempts | Processes learner work | attempt API and scorer | attempt persists; result returned | API tests | `feat(p4.8): process learner attempts` |
| P4.9 | Review queue | Forces mistake repair | review item creation | wrong answer creates blocker | domain/application tests | `feat(p4.9): create learner review queue` |
| P4.10 | Mastery unlock | Enforces progression | mastery policy and lock reasons | incomplete unit blocks next unit | domain tests | `feat(p4.10): enforce mastery unlocks` |

## P5 - TOEIC Part Engines

| ID | Purpose | User/Business Value | Contracts And Scope | Pass Criteria | Required Tests/Checks | Commit |
| --- | --- | --- | --- | --- | --- | --- |
| P5.1 | Common item contract | Consistent item handling | shared metadata plus part-specific extensions | no part loses required fields | domain tests | `feat(p5.1): define TOEIC item contracts` |
| P5.2 | Part 1 engine | Practice photographs correctly | image+audio item flow | missing image/audio rejected | validation + API tests | `feat(p5.2): implement TOEIC Part 1 engine` |
| P5.3 | Part 2 engine | Practice short responses | audio response flow | audio required; question type tagged | validation tests | `feat(p5.3): implement TOEIC Part 2 engine` |
| P5.4 | Part 3 engine | Practice conversations | grouped audio/questions | group required | domain tests | `feat(p5.4): implement TOEIC Part 3 engine` |
| P5.5 | Part 4 engine | Practice talks | grouped talk/questions | talk group and answers required | domain tests | `feat(p5.5): implement TOEIC Part 4 engine` |
| P5.6 | Part 5 engine | Practice grammar/vocab | sentence item flow | explanation required | validation tests | `feat(p5.6): implement TOEIC Part 5 engine` |
| P5.7 | Part 6 engine | Practice text completion | passage+blank flow | passage required | validation tests | `feat(p5.7): implement TOEIC Part 6 engine` |
| P5.8 | Part 7 engine | Practice reading comprehension | passage+evidence flow | evidence required | validation tests | `feat(p5.8): implement TOEIC Part 7 engine` |
| P5.9 | Weakness tagging | Powers recommendations | stable error tag taxonomy | attempts produce tags | domain tests | `feat(p5.9): tag TOEIC learner weaknesses` |

## P6 - Practice Test System

| ID | Purpose | User/Business Value | Contracts And Scope | Pass Criteria | Required Tests/Checks | Commit |
| --- | --- | --- | --- | --- | --- | --- |
| P6.1 | Mini test runtime | Confirms unit mastery | mini test session | result feeds mastery | application tests | `feat(p6.1): run TOEIC mini tests` |
| P6.2 | Part test runtime | Measures one part | part test composition | official counts enforced | domain tests | `feat(p6.2): run TOEIC part tests` |
| P6.3 | Listening section test | Builds listening endurance | Parts 1-4 combined | timing and breakdown supported | application tests | `feat(p6.3): run TOEIC listening tests` |
| P6.4 | Reading section test | Builds reading endurance | Parts 5-7 combined | timing and breakdown supported | application tests | `feat(p6.4): run TOEIC reading tests` |
| P6.5 | Full TOEIC test | Simulates real exam | 200-question test | no hints; section timing | E2E/API tests | `feat(p6.5): run full TOEIC LR tests` |
| P6.6 | Timer/session state | Prevents invalid exams | active/expired/submitted states | expired test cannot submit | domain tests | `feat(p6.6): manage TOEIC test sessions` |
| P6.7 | Score breakdown | Makes result actionable | score by part/tag/time | result includes weaknesses | application tests | `feat(p6.7): calculate TOEIC score breakdown` |
| P6.8 | Repair plan | Converts test into study | repair assignments from result | Today Plan updates after test | application tests | `feat(p6.8): generate TOEIC test repair plans` |

## P7 - Learner UX Production

| ID | Purpose | User/Business Value | Contracts And Scope | Pass Criteria | Required Tests/Checks | Commit |
| --- | --- | --- | --- | --- | --- | --- |
| P7.1 | Remove fake FE content | Stops demo UX | remove static questions/vocab/fallbacks | production flow requires API | frontend build + E2E | `refactor(p7.1): remove frontend demo learner content` |
| P7.2 | App shell | Professional navigation | learner layout and routes | Today/Learn/Practice/Review/Tests/Progress | Playwright smoke | `feat(p7.2): build learner app shell` |
| P7.3 | Onboarding/placement UX | Starts learner correctly | onboarding and placement screens | user can complete placement start flow | E2E | `feat(p7.3): build onboarding and placement UX` |
| P7.4 | Today screen | Main daily screen | next activity, blockers, progress | no admin/source terminology | E2E | `feat(p7.4): build learner today screen` |
| P7.5 | Lesson/example UX | Teach before drill | lesson and guided example screens | API-driven content renders | E2E | `feat(p7.5): build lesson and example UX` |
| P7.6 | Drill/mini test UX | Practice correctly | attempt forms and results | submit uses API | E2E | `feat(p7.6): build drill and mini test UX` |
| P7.7 | Mistake repair UX | Helps learner fix errors | review evidence/explanation/repair | wrong-answer review usable | E2E | `feat(p7.7): build mistake repair UX` |
| P7.8 | 7 Part overview | Real TOEIC navigation | progress/locks/actions by part | no placeholder part card | E2E | `feat(p7.8): build TOEIC part overview` |
| P7.9 | Practice test UX | Exam-like practice | timer, navigation, submit | test session stable | E2E | `feat(p7.9): build TOEIC practice test UX` |
| P7.10 | Progress/result UX | Shows improvement | mastery and test breakdown | real progress data only | E2E | `feat(p7.10): build learner progress UX` |

## P8 - Admin Content Operations

| ID | Purpose | User/Business Value | Contracts And Scope | Pass Criteria | Required Tests/Checks | Commit |
| --- | --- | --- | --- | --- | --- | --- |
| P8.1 | Source inventory UI | Operators see corpus state | admin source list | 73 sources and blockers visible | admin E2E | `feat(p8.1): build admin source inventory` |
| P8.2 | Asset discovery UI | Operators inspect discovered files | container/asset screens | assets visible by source | admin E2E | `feat(p8.2): build admin asset discovery` |
| P8.3 | Extraction jobs UI | Operators manage processing | job status/retry/failure | failed job retry available | admin E2E | `feat(p8.3): build extraction operations dashboard` |
| P8.4 | Draft review queue | Human quality gate | draft list/detail/actions | approve/reject/relabel works | admin E2E | `feat(p8.4): build draft review queue` |
| P8.5 | Validation issue workflow | Resolve content defects | issue status and required action | issue lifecycle works | admin E2E | `feat(p8.5): build validation issue workflow` |
| P8.6 | Publish queue | Control learner visibility | publish workflow | only approved content visible | integration tests | `feat(p8.6): build content publish queue` |
| P8.7 | Coverage dashboard | Know content gaps | coverage by part/media/status | missing media visible | admin E2E | `feat(p8.7): build content coverage dashboard` |

## P9 - Production Hardening

| ID | Purpose | User/Business Value | Contracts And Scope | Pass Criteria | Required Tests/Checks | Commit |
| --- | --- | --- | --- | --- | --- | --- |
| P9.1 | Authentication | Secure identity | login/session/token strategy | learner/admin login works | auth tests | `feat(p9.1): add authentication` |
| P9.2 | Authorization | Protect roles | learner/admin policies | learner cannot call admin API | API tests | `feat(p9.2): enforce learner admin authorization` |
| P9.3 | Observability | Operate production | logs/metrics/traces | correlation IDs and key metrics | smoke checks | `feat(p9.3): add production observability` |
| P9.4 | Error handling | Stable failure UX | error codes and messages | no raw exception leaks | API tests | `feat(p9.4): standardize error handling` |
| P9.5 | Performance baseline | Avoid slow product | load smoke and budgets | p95 targets measured | perf smoke | `perf(p9.5): establish performance baseline` |
| P9.6 | Security baseline | Reduce production risk | OWASP/secrets/input validation | secret scan and auth checks pass | security checks | `sec(p9.6): add security baseline` |
| P9.7 | Backup/migration | Protect data | backup restore and migration plan | restore rehearsal documented | migration check | `chore(p9.7): add backup and migration strategy` |
| P9.8 | CI/CD release | Ship professionally | build/test/deploy pipeline | pipeline green | CI run | `ci(p9.8): add release pipeline` |
| P9.9 | Deployment config | Run production | DB/storage/secrets/health config | staging deploy works | deploy smoke | `chore(p9.9): add production deployment config` |
| P9.10 | Release readiness | Decide go/no-go | release checklist | all market gates checked | checklist review | `docs(p9.10): add release readiness checklist` |

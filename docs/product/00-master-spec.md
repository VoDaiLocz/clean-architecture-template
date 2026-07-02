# TOEIC Market-Ready Platform Master Specification

## Document Purpose

This folder is the official product and engineering specification for the TOEIC Listening and Reading platform. It replaces all previous TOEIC specs under `docs/superpowers/specs`.

The goal is to give product, design, backend, frontend, QA, and content operators one shared source of truth. No implementation task is valid unless it can be traced to this specification set.

## Product Definition

We are building a commercial TOEIC Listening and Reading learning platform. The platform diagnoses learner weaknesses, assigns a structured daily plan, teaches before testing, forces mistake repair, unlocks content by mastery, and prepares learners for TOEIC-like practice tests.

This product is not:

- a PDF viewer
- a raw Google Drive link directory
- a simple quiz website
- a static 7-part card UI
- a demo dashboard with fake data
- a frontend-owned learning flow

## Initial Market Scope

In scope for the first market-ready release:

- TOEIC Listening and Reading only
- onboarding and learner profile
- placement diagnosis
- Today Plan assignment
- learning unit flow
- guided examples
- focus drills
- mini tests
- mistake repair
- mastery unlock
- 7 TOEIC part engines
- practice test modes
- content factory for source-to-published-content
- admin review workflow
- learner production UX
- CI/CD, observability, auth, deployment readiness

Deferred:

- TOEIC Speaking and Writing learner product
- payment/subscription
- native mobile apps
- tutor marketplace
- social/community features

## Documentation Map

- [01-product-requirements.md](./01-product-requirements.md): product goals, users, success metrics, scope.
- [02-domain-model.md](./02-domain-model.md): bounded contexts, entities, domain rules.
- [03-user-journeys.md](./03-user-journeys.md): learner/admin journeys and state transitions.
- [04-bounded-context-ownership.md](./04-bounded-context-ownership.md): context ownership, phase owners, cross-context contracts.
- [05-technical-architecture.md](./05-technical-architecture.md): production architecture, tech standards, boundaries.
- [06-data-model.md](./06-data-model.md): database entities and required relationships.
- [07-api-contracts.md](./07-api-contracts.md): API groups and contract rules.
- [08-ux-specs.md](./08-ux-specs.md): Angular frontend standard, design system, and screen-level learner/admin UX requirements.
- [09-quality-strategy.md](./09-quality-strategy.md): testing, validation, observability, acceptance gates.
- [10-release-plan.md](./10-release-plan.md): alpha, beta, market release gates.
- [11-task-breakdown](./11-task-breakdown): phase-by-phase task execution specs.
- [12-development-workflow.md](./12-development-workflow.md): TDD, Clean Architecture, SOLID, verification, commit, and push standard.
- [13-backend-module-boundaries.md](./13-backend-module-boundaries.md): backend context catalog, dependency direction, namespace target, and enforcement.
- [14-production-configuration.md](./14-production-configuration.md): environment rules, required configuration keys, secret handling, and production DB validation.
- [15-postgresql-migration-foundation.md](./15-postgresql-migration-foundation.md): production PostgreSQL migration project, schema history migration, and enforcement.
- [16-object-storage-abstraction.md](./16-object-storage-abstraction.md): object storage port, local test double, storage rules, and verification.
- [17-background-job-foundation.md](./17-background-job-foundation.md): background job queue port, retry policy, local implementation, and job state rules.
- [18-typed-api-contract-convention.md](./18-typed-api-contract-convention.md): route/audience/response contract catalog and typed API drift prevention rules.
- [19-ci-baseline.md](./19-ci-baseline.md): GitHub Actions backend quality workflow and baseline checks.
- [20-platform-health-checks.md](./20-platform-health-checks.md): platform health endpoint, dependency readiness checks, and operations contract.
- [21-source-asset-data-model.md](./21-source-asset-data-model.md): source container and source asset domain, repository, migration, and indexing model.
- [22-extracted-content-data-model.md](./22-extracted-content-data-model.md): extracted page/block schema, confidence, coordinates, and migration model.
- [23-draft-content-data-model.md](./23-draft-content-data-model.md): parser draft content schema, source trace, validation status, and learner API safety rule.
- [24-published-lesson-data-model.md](./24-published-lesson-data-model.md): learner-ready lesson and guided example schema, status, ordering, and migration model.
- [25-published-question-data-model.md](./25-published-question-data-model.md): learner-ready TOEIC question schema, part-specific required fields, validation rules, and migration model.
- [26-published-test-data-model.md](./26-published-test-data-model.md): TOEIC mini, part, skill, and full test schema with sections and ordered items.
- [27-learner-profile-data-model.md](./27-learner-profile-data-model.md): learner identity, TOEIC goals, study settings, profile status, and persistence rules.
- [28-learner-work-lifecycle-data-model.md](./28-learner-work-lifecycle-data-model.md): learner assignments, activity sessions, attempts, and attempt answers.
- [29-review-mastery-data-model.md](./29-review-mastery-data-model.md): review items, repair attempts, mastery records, and unlock blocker state.
- [30-data-integrity-indexes.md](./30-data-integrity-indexes.md): production FK rejection expectations and query indexes for TOEIC learner flows.
- [31-source-manifest-import.md](./31-source-manifest-import.md): audited TOEIC source manifest import use case, summary counts, and idempotency rules.
- [32-drive-source-discovery.md](./32-drive-source-discovery.md): Drive folder discovery gateway, source containers/assets, and blocked-source issues.
- [33-source-resolution.md](./33-source-resolution.md): shortlink/external source resolution records and resolver contract.
- [34-source-asset-registration.md](./34-source-asset-registration.md): registration of PDF/audio/image source assets from audited evidence flags.
- [35-pdf-block-extraction.md](./35-pdf-block-extraction.md): PDF text block extraction handler, extractor contract, page/block persistence, and confidence rules.
- [36-audio-metadata-extraction.md](./36-audio-metadata-extraction.md): audio metadata probe contract, duration/format persistence, and validation rules.
- [37-answer-key-parsing.md](./37-answer-key-parsing.md): answer-key parser contract and draft answer mapping records.
- [38-transcript-parsing.md](./38-transcript-parsing.md): transcript parser contract and draft transcript segments linked to audio/test groups.
- [39-reading-draft-parsing.md](./39-reading-draft-parsing.md): Part 5/7 reading draft parser contract, skill tags, and source trace rules.
- [40-listening-group-parsing.md](./40-listening-group-parsing.md): Part 1-4 listening draft parser contract and Part 3/4 group relationship rules.
- [41-draft-content-validation.md](./41-draft-content-validation.md): TOEIC draft validation policies, validation issue recording, and status transitions.
- [42-review-publish-workflow.md](./42-review-publish-workflow.md): human review decisions, approved draft publishing, and rejected draft hiding.
- [43-learner-onboarding.md](./43-learner-onboarding.md): learner profile onboarding command, API contract, idempotent update, and next placement action.
- [44-persisted-learner-home.md](./44-persisted-learner-home.md): repository-backed learner home state, onboarding CTA, placement CTA, and no memory-only home dependency.
- [45-placement-session-start.md](./45-placement-session-start.md): placement session model, start/resume behavior, typed API contract, and duplicate active session handling.
- [46-score-toeic-placement.md](./46-score-toeic-placement.md): placement scoring, diagnostic score bands, idempotent submit, and weakness breakdown contract.
- [47-generate-learner-path-from-placement.md](./47-generate-learner-path-from-placement.md): backend-owned learning path generation from placement weaknesses and unit catalog rules.
- [48-assign-learner-today-plan.md](./48-assign-learner-today-plan.md): Today Plan priority engine for review blockers, resume work, and next unit activity.
- [49-manage-learner-activity-sessions.md](./49-manage-learner-activity-sessions.md): durable activity session lifecycle and state-transition rules.
- [50-process-learner-attempts.md](./50-process-learner-attempts.md): backend scoring of learner attempts, answer persistence, and result contract.
- [51-create-learner-review-queue.md](./51-create-learner-review-queue.md): wrong-answer review item creation, blocking rules, and repair resolution contract.
- [52-enforce-mastery-unlocks.md](./52-enforce-mastery-unlocks.md): mastery gates, unlock blockers, and locked-reason API contract.
- [53-define-toeic-item-contracts.md](./53-define-toeic-item-contracts.md): learner-safe play/result/review item contracts that prevent answer-key leaks.
- [54-implement-toeic-part-1-engine.md](./54-implement-toeic-part-1-engine.md): Part 1 photograph engine with required image/audio validation.
- [55-implement-toeic-part-2-engine.md](./55-implement-toeic-part-2-engine.md): Part 2 audio question-response engine with hidden spoken prompt rules.
- [56-implement-toeic-part-3-engine.md](./56-implement-toeic-part-3-engine.md): Part 3 conversation group engine and child-question relationship rules.
- [57-implement-toeic-part-4-engine.md](./57-implement-toeic-part-4-engine.md): Part 4 short-talk group engine and talk-audio validation rules.
- [58-implement-toeic-part-5-engine.md](./58-implement-toeic-part-5-engine.md): Part 5 incomplete-sentence engine with grammar/vocabulary tagging.
- [59-implement-toeic-part-6-engine.md](./59-implement-toeic-part-6-engine.md): Part 6 text-completion engine with passage and blank-anchor rules.
- [60-implement-toeic-part-7-engine.md](./60-implement-toeic-part-7-engine.md): Part 7 reading-comprehension engine with passage sets and evidence spans.
- [61-tag-toeic-learner-weaknesses.md](./61-tag-toeic-learner-weaknesses.md): learner weakness event aggregation and severity summary contract.
- [62-run-toeic-mini-tests.md](./62-run-toeic-mini-tests.md): unit-scoped mini-test sessions that feed mastery gates.
- [63-run-toeic-part-tests.md](./63-run-toeic-part-tests.md): part-specific practice-test runtime and blueprint enforcement.
- [64-run-toeic-listening-tests.md](./64-run-toeic-listening-tests.md): Listening section practice runtime for Parts 1-4.
- [65-run-toeic-reading-tests.md](./65-run-toeic-reading-tests.md): Reading section practice runtime for Parts 5-7.
- [66-run-full-toeic-lr-tests.md](./66-run-full-toeic-lr-tests.md): full 200-question TOEIC LR exam-mode practice runtime.
- [67-manage-toeic-test-sessions.md](./67-manage-toeic-test-sessions.md): practice-test timer, resume, expiration, and final-submit state machine.
- [68-calculate-toeic-score-breakdown.md](./68-calculate-toeic-score-breakdown.md): score-band, part, tag, and time breakdown result contract.
- [69-generate-toeic-test-repair-plans.md](./69-generate-toeic-test-repair-plans.md): test-result repair assignment generation and Today Plan integration.
- [70-remove-frontend-demo-learner-content.md](./70-remove-frontend-demo-learner-content.md): frontend fake-content removal and production API dependency gate.
- [71-build-learner-app-shell.md](./71-build-learner-app-shell.md): production learner navigation shell and guarded route structure.
- [72-build-onboarding-and-placement-ux.md](./72-build-onboarding-and-placement-ux.md): onboarding and placement UX driven by backend next actions.
- [73-build-learner-today-screen.md](./73-build-learner-today-screen.md): daily learner workflow UI for assignments, blockers, and progress.
- [74-build-lesson-and-example-ux.md](./74-build-lesson-and-example-ux.md): lesson and guided-example UI backed by published APIs.
- [75-build-drill-and-mini-test-ux.md](./75-build-drill-and-mini-test-ux.md): drill and mini-test submission UI with no pre-submit answer leaks.
- [76-build-mistake-repair-ux.md](./76-build-mistake-repair-ux.md): wrong-answer repair workspace with evidence, explanation, and media replay.
- [77-build-toeic-part-overview.md](./77-build-toeic-part-overview.md): 7-part overview showing backend-owned progress, locks, and actions.
- [78-build-toeic-practice-test-ux.md](./78-build-toeic-practice-test-ux.md): exam-like test UI for timer, navigation, answer state, and submit.
- [79-build-learner-progress-ux.md](./79-build-learner-progress-ux.md): progress dashboard for score trends, weaknesses, and repair completion.
- [80-build-admin-source-inventory.md](./80-build-admin-source-inventory.md): admin source inventory for 73 audited rows, blocked state, and source traceability.
- [81-build-admin-asset-discovery.md](./81-build-admin-asset-discovery.md): admin asset discovery UI for containers, media classification, and checksums.
- [82-build-extraction-operations-dashboard.md](./82-build-extraction-operations-dashboard.md): extraction job dashboard for status, failure reason, retry, and run history.
- [83-build-draft-review-queue.md](./83-build-draft-review-queue.md): human review queue for draft content approve/reject/relabel decisions.
- [84-build-validation-issue-workflow.md](./84-build-validation-issue-workflow.md): validation issue workflow for missing media, answer keys, and passage mismatches.
- [85-build-content-publish-queue.md](./85-build-content-publish-queue.md): controlled publish queue with pre-publish validation and audit trail.
- [86-build-content-coverage-dashboard.md](./86-build-content-coverage-dashboard.md): coverage dashboard by TOEIC part, media requirement, extraction, and publish readiness.
- [87-add-authentication.md](./87-add-authentication.md): authentication, password hashing, token issuance, refresh rotation, and auth tests.
- [88-enforce-learner-admin-authorization.md](./88-enforce-learner-admin-authorization.md): learner/admin authorization policies, ownership checks, and denial audit rules.
- [89-add-production-observability.md](./89-add-production-observability.md): request logging, correlation ids, health checks, metrics, and redaction baseline.
- [90-standardize-error-handling.md](./90-standardize-error-handling.md): global error taxonomy, safe response format, and correlation id propagation.
- [91-establish-performance-baseline.md](./91-establish-performance-baseline.md): representative API/UI performance budgets, benchmark command, and baseline report.
- [92-add-security-baseline.md](./92-add-security-baseline.md): secure headers, CORS, rate limits, validation, secret scan, and dependency audit baseline.
- [93-add-backup-and-migration-strategy.md](./93-add-backup-and-migration-strategy.md): backup, restore rehearsal, migration rollback, and data-protection operating rules.
- [94-add-release-pipeline.md](./94-add-release-pipeline.md): CI/CD release pipeline with quality gates and deployment evidence.
- [95-add-production-deployment-config.md](./95-add-production-deployment-config.md): staging/production configuration for DB, storage, jobs, secrets, and health checks.
- [96-add-release-readiness-checklist.md](./96-add-release-readiness-checklist.md): go/no-go checklist tying product, quality, security, operations, and release evidence.

## Non-Negotiable Product Rules

1. Learners never use raw PDFs, Drive links, SharePoint links, source spreadsheets, or admin manifests as the primary learning experience.
2. Frontend never owns mastery, unlock, placement, scoring, review, or assignment business logic.
3. Learning content shown to learners must come from published content tables or read models.
4. Every wrong answer creates or updates review work.
5. Blocking review items must be resolved before the next gated unit unlocks.
6. Listening items cannot publish without audio.
7. Part 1 items cannot publish without image and audio.
8. Part 3 and Part 4 items cannot publish without group relationship.
9. Part 6 and Part 7 items cannot publish without passage context.
10. Production code cannot depend on `DemoLearnerSession`.
11. Each task must be committed and pushed to remote immediately after it passes.

## Engineering Workflow Rule

Every implementation task must follow this sequence:

1. Read the task spec.
2. Write the failing test first.
3. Implement the smallest production-grade change that satisfies the test.
4. Run all required verification commands listed by the task.
5. Commit with the exact commit message in the task.
6. Push immediately:

```bash
git push origin main
```

If working on a feature branch, push that branch:

```bash
git push origin <branch-name>
```

No local-only commit counts as complete.

## Task Spec Standard

Every task in `11-task-breakdown` must define:

- Task ID
- Task name
- Phase
- Context owner
- Purpose
- User or business value
- Dependencies
- Detailed scope
- Out of scope
- Data contract
- API contract
- UI contract
- Business rules
- Edge cases
- Required tests
- Acceptance criteria
- Verification commands
- Definition of done
- Commit message
- Push requirement

If any of these are missing, the task is not ready for implementation.

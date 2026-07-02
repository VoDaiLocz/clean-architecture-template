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
- [08-ux-specs.md](./08-ux-specs.md): screen-level learner/admin UX requirements.
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
- [46-score-toeic-placement.md](./46-score-toeic-placement.md): detailed product specification for score toeic placement.
- [47-generate-learner-path-from-placement.md](./47-generate-learner-path-from-placement.md): detailed product specification for generate learner path from placement.
- [48-assign-learner-today-plan.md](./48-assign-learner-today-plan.md): detailed product specification for assign learner today plan.
- [49-manage-learner-activity-sessions.md](./49-manage-learner-activity-sessions.md): detailed product specification for manage learner activity sessions.
- [50-process-learner-attempts.md](./50-process-learner-attempts.md): detailed product specification for process learner attempts.
- [51-create-learner-review-queue.md](./51-create-learner-review-queue.md): detailed product specification for create learner review queue.
- [52-enforce-mastery-unlocks.md](./52-enforce-mastery-unlocks.md): detailed product specification for enforce mastery unlocks.
- [53-define-toeic-item-contracts.md](./53-define-toeic-item-contracts.md): detailed product specification for define toeic item contracts.
- [54-implement-toeic-part-1-engine.md](./54-implement-toeic-part-1-engine.md): detailed product specification for implement toeic part 1 engine.
- [55-implement-toeic-part-2-engine.md](./55-implement-toeic-part-2-engine.md): detailed product specification for implement toeic part 2 engine.
- [56-implement-toeic-part-3-engine.md](./56-implement-toeic-part-3-engine.md): detailed product specification for implement toeic part 3 engine.
- [57-implement-toeic-part-4-engine.md](./57-implement-toeic-part-4-engine.md): detailed product specification for implement toeic part 4 engine.
- [58-implement-toeic-part-5-engine.md](./58-implement-toeic-part-5-engine.md): detailed product specification for implement toeic part 5 engine.
- [59-implement-toeic-part-6-engine.md](./59-implement-toeic-part-6-engine.md): detailed product specification for implement toeic part 6 engine.
- [60-implement-toeic-part-7-engine.md](./60-implement-toeic-part-7-engine.md): detailed product specification for implement toeic part 7 engine.
- [61-tag-toeic-learner-weaknesses.md](./61-tag-toeic-learner-weaknesses.md): detailed product specification for tag toeic learner weaknesses.
- [62-run-toeic-mini-tests.md](./62-run-toeic-mini-tests.md): detailed product specification for run toeic mini tests.
- [63-run-toeic-part-tests.md](./63-run-toeic-part-tests.md): detailed product specification for run toeic part tests.
- [64-run-toeic-listening-tests.md](./64-run-toeic-listening-tests.md): detailed product specification for run toeic listening tests.
- [65-run-toeic-reading-tests.md](./65-run-toeic-reading-tests.md): detailed product specification for run toeic reading tests.
- [66-run-full-toeic-lr-tests.md](./66-run-full-toeic-lr-tests.md): detailed product specification for run full toeic lr tests.
- [67-manage-toeic-test-sessions.md](./67-manage-toeic-test-sessions.md): detailed product specification for manage toeic test sessions.
- [68-calculate-toeic-score-breakdown.md](./68-calculate-toeic-score-breakdown.md): detailed product specification for calculate toeic score breakdown.
- [69-generate-toeic-test-repair-plans.md](./69-generate-toeic-test-repair-plans.md): detailed product specification for generate toeic test repair plans.
- [70-remove-frontend-demo-learner-content.md](./70-remove-frontend-demo-learner-content.md): detailed product specification for remove frontend demo learner content.
- [71-build-learner-app-shell.md](./71-build-learner-app-shell.md): detailed product specification for build learner app shell.
- [72-build-onboarding-and-placement-ux.md](./72-build-onboarding-and-placement-ux.md): detailed product specification for build onboarding and placement ux.
- [73-build-learner-today-screen.md](./73-build-learner-today-screen.md): detailed product specification for build learner today screen.
- [74-build-lesson-and-example-ux.md](./74-build-lesson-and-example-ux.md): detailed product specification for build lesson and example ux.
- [75-build-drill-and-mini-test-ux.md](./75-build-drill-and-mini-test-ux.md): detailed product specification for build drill and mini test ux.
- [76-build-mistake-repair-ux.md](./76-build-mistake-repair-ux.md): detailed product specification for build mistake repair ux.
- [77-build-toeic-part-overview.md](./77-build-toeic-part-overview.md): detailed product specification for build toeic part overview.
- [78-build-toeic-practice-test-ux.md](./78-build-toeic-practice-test-ux.md): detailed product specification for build toeic practice test ux.
- [79-build-learner-progress-ux.md](./79-build-learner-progress-ux.md): detailed product specification for build learner progress ux.
- [80-build-admin-source-inventory.md](./80-build-admin-source-inventory.md): detailed product specification for build admin source inventory.
- [81-build-admin-asset-discovery.md](./81-build-admin-asset-discovery.md): detailed product specification for build admin asset discovery.
- [82-build-extraction-operations-dashboard.md](./82-build-extraction-operations-dashboard.md): detailed product specification for build extraction operations dashboard.

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

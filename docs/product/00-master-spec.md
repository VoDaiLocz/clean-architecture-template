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

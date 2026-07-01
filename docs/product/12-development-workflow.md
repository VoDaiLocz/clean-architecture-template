# Development Workflow Standard

## Purpose

This document defines how every TOEIC platform task is implemented, verified, committed, and pushed.

The goal is not speed by batching. The goal is production-grade traceability: every commit must represent one real task, one clear behavior or documentation improvement, and one verified state.

## Required Task Flow

Every task must follow this sequence:

1. Read the task row in `11-task-breakdown/task-register.md`.
2. Read the phase file in `11-task-breakdown`.
3. Read related product, domain, API, data, UX, quality, and ownership docs.
4. Identify the primary bounded context owner.
5. Write or update the failing test/check first.
6. Implement the smallest production-quality change that satisfies the task.
7. Refactor only after tests are green.
8. Run required verification commands.
9. Inspect `git status --short` and stage only task-owned files.
10. Commit with the exact task commit message.
11. Push immediately to remote.

## TDD Rule

Code tasks must use red-green-refactor:

| Step | Required Evidence | Failure Condition |
| --- | --- | --- |
| Red | failing unit, integration, API, E2E, or static check exists | implementation starts without a failing check |
| Green | task behavior passes with minimal production code | fake behavior, hardcoded production content, or skipped test |
| Refactor | code is simplified without changing behavior | refactor changes behavior or weakens tests |

Documentation-only tasks must still have objective checks, such as required heading scans, placeholder scans, link checks, or task-field scans.

## Clean Architecture Rule

Backend code must preserve dependency direction:

```text
Api -> Application -> Domain
Infrastructure -> Application abstractions
Domain -> no Application, Infrastructure, or Api dependency
```

Rules:

1. Domain contains business rules and has no framework dependency.
2. Application orchestrates use cases and depends on abstractions.
3. Infrastructure implements persistence, storage, external services, and workers.
4. Api exposes route contracts and does not own business decisions.
5. Frontend consumes API decisions and does not recreate scoring, unlock, assignment, publish, or validation logic.

## SOLID And Clean Code Rule

Every code task must satisfy:

| Principle | Required Behavior |
| --- | --- |
| Single Responsibility | A class/function has one reason to change. |
| Open/Closed | New part-specific behavior should extend a stable contract rather than editing unrelated logic. |
| Liskov Substitution | Implementations of interfaces must preserve caller expectations. |
| Interface Segregation | Consumers depend only on methods they use. |
| Dependency Inversion | Application code depends on abstractions, not infrastructure details. |

Clean-code guardrails:

- Use domain names from `02-domain-model.md` and `04-bounded-context-ownership.md`.
- Keep changes inside the task owner boundary.
- Prefer small use cases and explicit contracts over generic service objects.
- Do not add helpers, factories, or abstractions until they remove real duplication or protect a boundary.
- Do not leave comments that explain obvious code; improve names instead.

## Verification Matrix

| Task Type | Minimum Verification |
| --- | --- |
| Documentation | heading/field scan, placeholder scan, git diff review |
| Domain rule | domain unit test plus solution build |
| Application use case | application test plus solution build |
| Repository/schema | migration or repository integration test plus solution build |
| API contract | API test or smoke check plus solution build |
| Frontend workflow | frontend build plus Playwright/user-flow check when route behavior changes |
| Cross-cutting platform | solution build plus task-specific health/config/security check |

Task-specific verification commands can add checks but cannot remove the minimum checks.

## Commit And Push Rule

Before commit:

```bash
git status --short
git diff --cached --name-status
```

Rules:

1. Stage only files owned by the current task.
2. Never stage unrelated dirty files.
3. Use the exact commit message from the task register.
4. Push immediately after commit.

Default push:

```bash
git push origin main
```

If a feature branch is explicitly used:

```bash
git push origin <branch-name>
```

No local-only commit is complete.

## Task Pass Checklist

A task passes only when all answers are yes:

1. Does the task map to one primary bounded context owner?
2. Are scope and out-of-scope respected?
3. Are data/API/UI contracts respected?
4. Is business logic in the correct backend layer?
5. Are learner APIs free of source/admin internals?
6. Is frontend free of production fake learning logic?
7. Did the test/check fail before implementation when code changed?
8. Do required tests and builds pass?
9. Is the staged diff limited to this task?
10. Is the exact commit message used?
11. Was the commit pushed to remote?

If any answer is no, the task is not done.

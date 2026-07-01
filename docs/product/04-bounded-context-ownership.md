# Bounded Context Ownership

## Purpose

This document defines who owns each business decision, data model, API surface, and UI workflow in the TOEIC Listening and Reading platform.

Every task must map to exactly one primary context owner. A task can collaborate with other contexts only through explicit contracts. If a task needs two owners to change core behavior, split it before implementation.

## Ownership Matrix

| Context | Primary Purpose | Owns Decisions | Owns Data | Owns APIs | Owns UI Surface | Must Not Own |
| --- | --- | --- | --- | --- | --- | --- |
| Content Factory | Convert raw materials into reviewed content candidates | source discovery, extraction, parsing, validation issues, review workflow | sources, containers, assets, extracted blocks, parser runs, draft content, validation issues, review decisions | admin source, asset, job, draft, review, publish-prep APIs | admin inventory, extraction jobs, draft review, validation issue screens | learner path, scoring, mastery, learner-facing curriculum |
| Learning Content | Store learner-ready lessons, questions, groups, tests, and media | published content structure, part-specific content validity, content versioning | published lessons, guided examples, questions, groups, passages, evidence, answer keys, transcripts, audio, images, tests | learner content read APIs, admin published-content APIs | lesson/question/test preview surfaces | raw extraction records, learner progress, attempt scoring |
| Learner Journey | Decide what the learner should do next | onboarding, placement interpretation, learning path, assignment priority, unlock reason selection | learner profile, placement result, learning path, learning units, assignments, activity sessions | learner onboarding, placement, today plan, activity lifecycle APIs | learner Today, Learn, Part roadmap, lock-state screens | parsing, publishing, scoring correctness, admin review |
| Attempt And Review | Process work, score answers, repair mistakes, evaluate mastery | scoring, attempt lifecycle, review creation, repair completion, mastery policy | attempts, attempt answers, review items, repair attempts, mastery records, test results | attempt submit, review, mastery, test result APIs | drill result, review, mastery/result screens | content publication, source operations, navigation recommendation outside mastery signals |
| Analytics And Operations | Provide visibility and operate production safely | metrics definition, read model projection, audit visibility, release readiness | read models, metrics, audit logs, health snapshots, release gate evidence | dashboard, health, metrics, audit, release-readiness APIs | admin dashboards, operations screens | source of truth for learner decisions or content decisions |
| Platform Infrastructure | Provide technical capabilities shared by all contexts | persistence, storage, background jobs, auth, authorization, configuration, observability | migrations, job records, auth records, operational config state | health, auth, typed-contract, platform support APIs | login, error, operational shell where needed | TOEIC business decisions |

## Phase Owner Map

| Phase | Primary Owner | Secondary Collaborators | Ownership Rule |
| --- | --- | --- | --- |
| P0 Product And Documentation Reset | Product architecture | all contexts | Defines rules only; no production behavior changes unless the task explicitly marks legacy code. |
| P1 Architecture And Infrastructure | Platform Infrastructure | all contexts | Builds shared platform capabilities without encoding TOEIC learning decisions. |
| P2 Production Data Foundation | Platform Infrastructure | Content Factory, Learning Content, Learner Journey, Attempt And Review | Creates schemas/repositories according to context ownership; no learner-visible fake content. |
| P3 Content Factory Pipeline | Content Factory | Learning Content, Platform Infrastructure | Produces draft and publishable content with traceability; cannot bypass validation gates. |
| P4 Learner Journey Core | Learner Journey | Learning Content, Attempt And Review | Owns next-action and unlock flow; consumes published content only. |
| P5 TOEIC Part Engines | Learning Content | Attempt And Review, Learner Journey | Owns part-specific content contracts; scoring remains in Attempt And Review. |
| P6 Practice Test System | Attempt And Review | Learning Content, Learner Journey | Owns test sessions, scoring, results, and repair plans. |
| P7 Learner UX Production | Learner Journey | Learning Content, Attempt And Review | Displays backend decisions; never recreates business logic in frontend state. |
| P8 Admin Content Operations | Content Factory | Learning Content, Analytics And Operations | Operates source-to-publish workflow; learner visibility only changes through publish contracts. |
| P9 Production Hardening | Platform Infrastructure | Analytics And Operations | Adds auth, observability, deployment, backup, and release gates without changing learning rules. |

## Task Ownership Rules

1. Every task must name one primary context owner in its implementation plan.
2. A task can read another context only through an application contract, API contract, repository interface, domain event, or read model.
3. A context cannot directly mutate another context's source-of-truth tables.
4. Learner APIs can read only published content and learner state.
5. Admin APIs can expose source/draft/validation data only to authorized admin workflows.
6. Analytics can aggregate data but cannot become the authority for assignment, unlock, publish, score, or mastery decisions.
7. Frontend screens display decisions returned by APIs. They do not own assignment, score, mastery, publish, or validation decisions.
8. Shared read models must be rebuilt from owning-context facts and must not accept direct manual edits.

## Cross-Context Contract Types

| Contract Type | Allowed Use | Not Allowed |
| --- | --- | --- |
| Application command/query | A use case asks the owning context to perform behavior | calling internal services from another context |
| Repository interface | Application layer persists owned aggregate/read model data | one context writing another context's tables |
| Domain event | Owning context announces a completed fact | using events as hidden synchronous commands |
| Read model | Analytics or UI reads projected state | using read model as source of truth |
| API contract | Frontend/admin/system integration uses stable payloads | exposing database entities directly |

## Backend Namespace Target

Future backend work must move toward these ownership namespaces:

```text
Domain/ContentFactory
Domain/LearningContent
Domain/LearnerJourney
Domain/AttemptReview
Domain/AnalyticsOperations
Application/ContentFactory
Application/LearningContent
Application/LearnerJourney
Application/AttemptReview
Application/AnalyticsOperations
Infrastructure/<technology-specific implementation>
Api/<route groups by audience: learner, admin, ops>
```

Existing aggregate folders can remain until P1/P2 migration tasks create the production module boundaries. New production code should use the target ownership vocabulary unless a task explicitly refactors existing code.

## Review Checklist

Before any task is implemented, answer:

1. What is the primary context owner?
2. Which owned data changes?
3. Which API or application contract changes?
4. Which UI audience is affected: learner, admin, ops, or none?
5. Which business decision is being introduced or changed?
6. Does the task read another context through an allowed contract?
7. Does the task avoid raw source data in learner APIs?
8. Does the task avoid frontend-owned learning business logic?
9. Are tests written at the owning context boundary?
10. Is the commit message the exact task commit message?

The task is not ready if any answer is missing.

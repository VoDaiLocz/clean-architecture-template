# P1 - Architecture And Infrastructure

## Phase Goal

Prepare the production technical foundation before feature work scales.

## Task Summary

| Task | Purpose | Key Pass Criteria | Commit |
| --- | --- | --- | --- |
| P1.1 Define backend module boundaries | Ensure Clean Architecture boundaries match bounded contexts | Domain has no infra dependency; contexts mapped to namespaces | `docs(p1.1): define backend module boundaries` |
| P1.2 Add production configuration strategy | Separate local, staging, production config | No secrets committed; production DB configurable | `chore(p1.2): add production configuration strategy` |
| P1.3 Add PostgreSQL migration foundation | Move toward production DB | Migration project exists; SQLite remains dev/test only | `feat(p1.3): add PostgreSQL migration foundation` |
| P1.4 Add object storage abstraction | Store source/media assets outside DB | Interface and test double exist | `feat(p1.4): add object storage abstraction` |
| P1.5 Add background job foundation | Process extraction asynchronously | Job model, status, retry policy exist | `feat(p1.5): add background job foundation` |
| P1.6 Add typed API contract convention | Prevent FE/API drift | Shared contract generation or typed client strategy documented and enforced | `feat(p1.6): add typed API contract convention` |
| P1.7 Add CI baseline | Run build/tests on push | CI passes backend build and tests | `ci(p1.7): add backend quality gate` |
| P1.8 Add environment health checks | Verify runtime dependencies | Health endpoints cover DB and worker readiness | `feat(p1.8): add platform health checks` |

## Required Detail For Each P1 Task

Each P1 task must include:

- dependency direction check
- required config keys
- test double or local dev strategy
- failure mode
- verification command
- commit and push


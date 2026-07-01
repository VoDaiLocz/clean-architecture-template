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

## P1.1 - Define Backend Module Boundaries

**Context:** Platform architecture  
**Purpose:** Establish enforceable backend module boundaries before production feature work scales.  
**User/Business Value:** Prevents tangled implementation where source extraction, learner journey, scoring, and operations mutate each other directly.  
**Dependencies:** P0.3, P0.4.  
**Detailed Scope:** Add backend context catalogs for Domain/Application; document dependency direction, target namespace layout, and enforcement checks.  
**Out Of Scope:** Moving all existing classes into final folders; adding analyzers; rewriting current learner/demo endpoints.  
**Data Contract:** none.  
**API Contract:** none.  
**UI Contract:** none.  
**Business Rules:** Domain cannot reference Application, Infrastructure, or Api; Application context catalog must match Domain context catalog.  
**Edge Cases:** Existing folders can remain until migration tasks; new production code should use target context vocabulary.  
**Required Tests:** Unit test verifies context catalogs match and Domain has no forbidden outer-layer references.  
**Acceptance Criteria:** `DomainContextCatalog` and `ApplicationContextCatalog` exist; backend module boundary doc exists; tests and solution build pass.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "DomainContextCatalog|ApplicationContextCatalog|Backend Module Boundaries" backend/src docs/product`.  
**Definition Of Done:** Boundary catalogs and docs are committed and pushed.  
**Commit:** `docs(p1.1): define backend module boundaries`  
**Push:** `git push origin main`

## P1.2 - Add Production Configuration Strategy

**Context:** Platform architecture  
**Purpose:** Separate local, staging, and production configuration so production cannot start with unsafe implicit defaults.  
**User/Business Value:** Reduces deployment risk and prevents hidden local/demo settings from becoming market infrastructure.  
**Dependencies:** P1.1.  
**Detailed Scope:** Add configuration options for database startup behavior; document required keys for DB, storage, worker, auth, logging, and observability; prevent production DB fallback.  
**Out Of Scope:** Implementing object storage, worker runtime, auth, or observability providers.  
**Data Contract:** none.  
**API Contract:** none.  
**UI Contract:** none.  
**Business Rules:** Production requires explicit `ConnectionStrings:ToeicDb`; local development may use SQLite fallback.  
**Edge Cases:** Empty or whitespace production connection string must fail; development without connection string must still run locally.  
**Required Tests:** Unit test validates production requires explicit DB configuration; solution build passes.  
**Acceptance Criteria:** `ToeicPlatformOptions` exists; API passes environment name into Infrastructure DI; configuration strategy doc defines required keys and secret policy.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "ToeicPlatformOptions|ConnectionStrings:ToeicDb|Production Configuration Strategy" backend/src docs/product`.  
**Definition Of Done:** Configuration validation and docs are committed and pushed.  
**Commit:** `chore(p1.2): add production configuration strategy`  
**Push:** `git push origin main`

## P1.3 - Add PostgreSQL Migration Foundation

**Context:** Platform architecture  
**Purpose:** Establish the production database migration foundation before production schemas expand.  
**User/Business Value:** Moves the product away from local-only SQLite assumptions and creates a controlled path to production PostgreSQL.  
**Dependencies:** P1.2.  
**Detailed Scope:** Add `DatabaseMigrations` project; add PostgreSQL migration catalog; add initial schema history migration; include project in solution; add tests proving provider and first migration.  
**Out Of Scope:** Live PostgreSQL connection, migration runner CLI, full production schema, rollback engine.  
**Data Contract:** `platform_schema_history` records applied migration id, UTC apply time, and checksum.  
**API Contract:** none.  
**UI Contract:** none.  
**Business Rules:** SQLite remains local/dev/test only; production migrations must be PostgreSQL-specific and ordered.  
**Edge Cases:** Migration catalog cannot be empty; migration SQL must not use SQLite syntax; first migration must create schema history before later schema work.  
**Required Tests:** Unit test verifies PostgreSQL provider, first migration id, schema history SQL, and no SQLite marker text.  
**Acceptance Criteria:** Migration project exists in solution; test project references it; tests and build pass; migration foundation doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "PostgresMigrationCatalog|platform_schema_history|DatabaseMigrations" backend/src backend/tests docs/product`.  
**Definition Of Done:** Migration foundation is committed and pushed.  
**Commit:** `feat(p1.3): add PostgreSQL migration foundation`  
**Push:** `git push origin main`

## P1.4 - Add Object Storage Abstraction

**Context:** Platform architecture  
**Purpose:** Store source/media assets outside the relational database through a clean application port.  
**User/Business Value:** Enables PDF/audio/image processing at production scale without turning the database into a blob store.  
**Dependencies:** P1.2.  
**Detailed Scope:** Add `IObjectStorage` application interface; add object key/request/response records; add in-memory infrastructure test double; register storage in DI; document storage rules.  
**Out Of Scope:** Cloud provider implementation, signed URLs, CDN, virus scanning, media transcoding, multipart upload.  
**Data Contract:** Future DB records store object keys and metadata, not raw bytes.  
**API Contract:** none for P1.4.  
**UI Contract:** none for P1.4.  
**Business Rules:** Object key and content type are required; storage implementation copies bytes on read/write to avoid mutation.  
**Edge Cases:** missing object returns null; deleted object is no longer listable/readable; empty key/content type rejected.  
**Required Tests:** Unit test covers put, get, list, delete, content type round-trip, payload round-trip, and missing-after-delete behavior.  
**Acceptance Criteria:** `IObjectStorage` exists in Application; `InMemoryObjectStorage` exists in Infrastructure; DI registers the abstraction; tests and build pass; storage doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "IObjectStorage|InMemoryObjectStorage|Object Storage Abstraction" backend/src backend/tests docs/product`.  
**Definition Of Done:** Object storage abstraction is committed and pushed.  
**Commit:** `feat(p1.4): add object storage abstraction`  
**Push:** `git push origin main`

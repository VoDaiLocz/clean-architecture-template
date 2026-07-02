# P9 - Production Hardening And Release

## Phase Goal

Make the TOEIC product secure, observable, deployable, operable, recoverable, and release-ready. P9 is not a place for broad rewrites; each task must harden an already-working product capability with explicit production controls.

## Phase Acceptance Standard

- No public release without authentication, authorization, stable errors, monitoring, backup/restore evidence, CI/CD gates, and release checklist approval.
- No task may commit secrets, production private URLs, or fake operational evidence.
- Every task must include negative tests, not only happy paths.
- Production behavior must fail closed when security or configuration is missing.

| Task | Purpose | Key Pass Criteria | Commit |
| --- | --- | --- | --- |
| P9.1 Authentication | Identify users | Learner/admin login works | `feat(p9.1): add authentication` |
| P9.2 Authorization | Protect roles | Admin APIs inaccessible to learners | `feat(p9.2): enforce learner admin authorization` |
| P9.3 Observability | Operate production | Logs, metrics, traces available | `feat(p9.3): add production observability` |
| P9.4 Error handling | Stable failures | Error codes and user-safe messages | `feat(p9.4): standardize error handling` |
| P9.5 Performance baseline | Prevent slow UX | API p95 targets measured | `perf(p9.5): establish performance baseline` |
| P9.6 Security baseline | Reduce risk | OWASP checks and secret handling | `sec(p9.6): add security baseline` |
| P9.7 Backup/migration strategy | Protect data | Restore and migration checks documented/tested | `chore(p9.7): add backup and migration strategy` |
| P9.8 CI/CD release pipeline | Ship safely | Build/test/deploy gates run | `ci(p9.8): add release pipeline` |
| P9.9 Deployment config | Run production | PostgreSQL, object storage, secrets configured | `chore(p9.9): add production deployment config` |
| P9.10 Release readiness checklist | Decide go/no-go | Checklist completed before market release | `docs(p9.10): add release readiness checklist` |

## P9.1 - Add Authentication

**Context:** Platform Security
**Purpose:** Establish verified identity for learners and internal users.
**User/Business Value:** Real users can securely access their own TOEIC learning state and operators can be identified for content operations.
**Dependencies:** P4.1, P7.3, production configuration registry.
**Detailed Scope:** Add auth user model, login credentials, password hashing, access token issuance, refresh token rotation, logout, `/api/auth/me`, login/register endpoints for first production slice, auth config validation, and tests.
**Out Of Scope:** password reset email, email verification flow, social login, MFA, SSO, user management UI, paid subscription identity. These require separate tasks.
**Data Contract:** Persist `auth_users` and `auth_refresh_tokens`; tokens are stored hashed; auth user maps to learner profile by stable user id/learner id relationship.
**API Contract:** Add `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`, `GET /api/auth/me`; responses use stable auth error codes.
**UI Contract:** Existing learner UI can consume login state, but full auth UX screens may be implemented in P7/P9 integration task if not present.
**Business Rules:** Passwords are hashed with approved password hasher; refresh tokens rotate; logout revokes refresh token; disabled users cannot login; auth failure messages do not reveal whether email exists.
**Edge Cases:** Duplicate email, invalid password, locked/disabled user, expired access token, reused refresh token, missing auth config, concurrent refresh.
**Required Tests:** Password hash/verify tests; login/register/refresh/logout application tests; token validation tests; missing config tests; no-secret scan.
**Acceptance Criteria:** Users can register/login/refresh/logout; `/api/auth/me` works with valid token; invalid/expired/reused token fails; build/tests pass.
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "AuthenticateUser|auth_users|RegisterUser|LoginUser|RefreshToken" backend/src backend/tests docs/product`.
**Definition Of Done:** Authentication first production slice is committed and pushed.
**Commit:** `feat(p9.1): add authentication`
**Push:** `git push origin main`

## P9.2 - Enforce Learner Admin Authorization

**Context:** Platform Security
**Purpose:** Enforce role and ownership access rules across learner and admin APIs.
**User/Business Value:** Learners cannot access admin content operations or another learner's data; operators/admins have least-privilege access.
**Dependencies:** P9.1, P8 admin routes, P4 learner routes.
**Detailed Scope:** Add authenticated actor context, role policies, ownership checks in application use cases, route-level auth requirements, admin/operator policy split, authorization error contract, audit record for denied admin/operator actions, and tests.
**Out Of Scope:** MFA, fine-grained custom permissions UI, external IAM/SSO, row-level database security.
**Data Contract:** Add auth/audit records only as needed; learner-owned queries must scope by authenticated user id/learner id.
**API Contract:** Protected routes return `401` for missing/invalid token and `403` for valid token without permission; stable error body is shared with P9.4.
**UI Contract:** Learner UI handles auth expiration; admin UI hides unavailable actions but backend remains source of truth.
**Business Rules:** Default deny; learner APIs require ownership; admin APIs require admin role; operator can operate content but cannot publish/unpublish unless explicitly allowed; learner APIs never expose draft/source/admin data.
**Edge Cases:** Learner attempts another learner id, learner calls admin route, operator calls publish route, missing role claim, stale/deactivated user, public health route.
**Required Tests:** Policy unit tests; API/application negative tests for cross-learner/admin access; audit assertion for denied privileged actions; route catalog coverage test.
**Acceptance Criteria:** Every non-health production endpoint has explicit auth policy; forbidden paths fail closed; tests/build pass.
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "Authorize|AuthenticatedActor|LearnerAuthorization|AdminAuthorization" backend/src backend/tests docs/product`.
**Definition Of Done:** Authorization enforcement is committed and pushed.
**Commit:** `feat(p9.2): enforce learner admin authorization`
**Push:** `git push origin main`

## P9.3 - Add Production Observability

**Context:** Platform Operations
**Purpose:** Add minimum production observability for diagnosing API, content pipeline, learner flow, and dependency failures.
**User/Business Value:** Operators can detect problems before learners are blocked and can debug failures using correlation evidence.
**Dependencies:** P1.8, P9.1, P9.2.
**Detailed Scope:** Add structured request logs, correlation id middleware, health readiness/liveness split if not present, key metrics counters/histograms, safe log redaction, and admin/operator audit log correlation.
**Out Of Scope:** Full Grafana dashboards, PagerDuty contracts, SIEM vendor integration, distributed tracing across external services. Those are deployment/ops follow-up tasks.
**Data Contract:** Audit/operational records store actor id, action, resource, outcome, correlation id, timestamp, and safe detail only.
**API Contract:** `GET /api/health/live`, `GET /api/health/ready`, and a metrics read path or in-process metrics collector smoke; responses do not leak secrets.
**UI Contract:** Admin status screens may consume health later; no learner UI dependency required.
**Business Rules:** Logs must include correlation id, method, route template, status, duration, authenticated user id when available; request bodies are not logged globally; secrets/tokens/passwords are never logged.
**Edge Cases:** Missing correlation id, downstream dependency unhealthy, auth failure, exception path, background job failure, log redaction of sensitive fields.
**Required Tests:** Correlation id tests; health success/failure tests; redaction tests; metrics smoke test; log schema assertion for request/error paths.
**Acceptance Criteria:** Health/log/correlation baseline works, sensitive data is redacted, unhealthy dependencies produce readiness failure, tests/build pass.
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "CorrelationId|ProductionObservability|Readiness|StructuredLog" backend/src backend/tests docs/product`.
**Definition Of Done:** Production observability baseline is committed and pushed.
**Commit:** `feat(p9.3): add production observability`
**Push:** `git push origin main`

## P9.4 - Standardize Error Handling

**Context:** Platform API
**Purpose:** Provide stable, user-safe error responses for all APIs.
**User/Business Value:** Frontend and operators can handle failures predictably without raw exceptions or inconsistent messages.
**Dependencies:** P9.1, P9.2, P9.3.
**Detailed Scope:** Add global exception middleware, error code taxonomy, validation error format, auth/forbidden error format, correlation id in errors, production stack trace hiding, and tests.
**Out Of Scope:** Full localization, toast design, admin incident workflow.
**Data Contract:** Error logs/audit events include correlation id and internal diagnostic detail; API response contains safe public fields only.
**API Contract:** All error responses follow `{ error: { code, message, correlationId, timestamp, details? } }`; status codes align with error taxonomy.
**UI Contract:** UI displays message and correlation id where useful; no UI parses raw exception strings.
**Business Rules:** Production never returns stack traces; validation errors map to stable field errors; domain rule failures map to known codes.
**Edge Cases:** Unhandled exception, validation failure, auth failure, forbidden resource, not found, conflict/idempotency conflict, dependency unavailable.
**Required Tests:** Middleware tests for each error class; production stack trace negative test; API contract tests; frontend error-state smoke where routes exist.
**Acceptance Criteria:** Error format is stable across API families, raw exceptions are hidden, tests/build pass.
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "GlobalException|ErrorCode|correlationId" backend/src backend/tests docs/product`.
**Definition Of Done:** Error handling is committed and pushed.
**Commit:** `feat(p9.4): standardize error handling`
**Push:** `git push origin main`

## P9.5 - Establish Performance Baseline

**Context:** Platform Operations
**Purpose:** Define and measure baseline latency for core learner/admin workflows.
**User/Business Value:** The product remains usable under realistic load and regressions are caught before release.
**Dependencies:** P7 core UX, P8 admin ops, P9.3.
**Detailed Scope:** Add performance budgets, benchmark/smoke test harness, representative API scenarios, local documented results, and regression gate proposal.
**Out Of Scope:** Large-scale paid load testing, infrastructure autoscaling implementation, database query rewrites unless a blocking regression is found.
**Data Contract:** Performance report records scenario, data volume, environment, p50/p95/p99, error rate, timestamp, commit sha.
**API Contract:** Core APIs must be measurable with stable request payloads/fixtures.
**UI Contract:** Playwright trace may measure major screen load smoke when frontend routes are part of the scenario; backend API baseline is primary.
**Business Rules:** Baselines must use seeded realistic data, not empty DB only; failures above budget create follow-up tasks.
**Edge Cases:** Cold start, empty DB, large source inventory, many review items, media-heavy part payloads, slow dependency.
**Required Tests:** Benchmark/smoke command; API p95 assertions where deterministic; report artifact check; no regression budget bypass.
**Acceptance Criteria:** Baseline report exists, core API budgets are declared, measurement command runs, tests/build pass.
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "PerformanceBaseline|p95|performance budget" backend/src backend/tests docs/product`.
**Definition Of Done:** Performance baseline is committed and pushed.
**Commit:** `perf(p9.5): establish performance baseline`
**Push:** `git push origin main`

## P9.6 - Add Security Baseline

**Context:** Platform Security
**Purpose:** Add baseline OWASP controls, secure headers, CORS restrictions, input validation policy, and secret scanning checks.
**User/Business Value:** The public product has basic protection against common web risks.
**Dependencies:** P9.1, P9.2, P9.4, production configuration registry.
**Detailed Scope:** Add secure headers, production CORS policy, request size limits, rate limit policy, input validation conventions, dependency/secret scan commands, and security checklist.
**Out Of Scope:** Full penetration test, WAF setup, SAST vendor integration, compliance certification.
**Data Contract:** Security events may write audit/observability records; no learner content schema changes expected.
**API Contract:** Security middleware must preserve standard error contract and not break health endpoints.
**UI Contract:** Frontend must work only from configured allowed origins in production.
**Business Rules:** Wildcard CORS forbidden outside Development; HSTS enabled in production; auth/admin endpoints rate-limited; secrets are not logged or committed.
**Edge Cases:** Preflight request, local development origin, oversized payload, malicious input string, missing security header, dependency vulnerability finding.
**Required Tests:** Header tests; CORS config tests; rate-limit test; secret scan; dependency audit command if available.
**Acceptance Criteria:** Baseline security middleware/config exists, production unsafe config fails, tests/build pass.
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "SecurityBaseline|Cors|HSTS|RateLimit" backend/src backend/tests docs/product`.
**Definition Of Done:** Security baseline is committed and pushed.
**Commit:** `sec(p9.6): add security baseline`
**Push:** `git push origin main`

## P9.7 - Add Backup And Migration Strategy

**Context:** Platform Operations / Data
**Purpose:** Ensure production data can be migrated, backed up, and restored with evidence.
**User/Business Value:** Learner progress and content operations are protected from data loss and broken migrations.
**Dependencies:** P1.3, P2 data foundation, P9.9.
**Detailed Scope:** Add migration runbook, backup schedule contract, restore rehearsal script/checklist, migration rollback/forward-fix policy, and data retention notes.
**Out Of Scope:** Managed cloud backup provisioning if deployment target is not chosen, cross-region DR automation.
**Data Contract:** All production tables are covered by backup/restore; migration history is tracked; restore rehearsal verifies key learner/content counts.
**API Contract:** No API endpoint is required for this task; an admin/ops status endpoint must be specified as a separate task if added later.
**UI Contract:** None unless admin ops status page exists.
**Business Rules:** No production deploy without migration plan; destructive migrations require explicit backup and rollback/forward-fix plan; restore rehearsal must be documented.
**Edge Cases:** Failed migration, partial migration, incompatible schema, restore to staging, missing object storage backup, corrupted backup.
**Required Tests:** Migration catalog test; restore rehearsal checklist/command check; docs checklist scan; backup config validation.
**Acceptance Criteria:** Backup/migration runbook exists, restore rehearsal path is testable, migration safety rules are documented, tests/build pass.
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "BackupMigration|restore rehearsal|migration rollback" backend/src backend/tests docs/product`.
**Definition Of Done:** Backup and migration strategy is committed and pushed.
**Commit:** `chore(p9.7): add backup and migration strategy`
**Push:** `git push origin main`

## P9.8 - Add Release Pipeline

**Context:** Platform Delivery
**Purpose:** Add CI/CD gates that prevent broken code from reaching production.
**User/Business Value:** The team can ship repeatedly with evidence that tests/build/security checks passed.
**Dependencies:** P9.4-P9.7.
**Detailed Scope:** Add or update pipeline for restore, build, backend tests, frontend build/tests, Playwright where required, secret scan, artifact creation, and explicit deployment gate stubs.
**Out Of Scope:** Vendor-specific production deploy if target is not selected, blue/green rollout automation, paid CI optimization.
**Data Contract:** Pipeline does not change app data; release artifacts are traceable to commit sha.
**API Contract:** Health endpoint may be used by deploy smoke.
**UI Contract:** Frontend artifact must build from clean install.
**Business Rules:** Pipeline failure blocks release; scheduled/e2e jobs can be separate but release gate must be explicit; workflow must run on pushed branch.
**Edge Cases:** Missing secrets in CI, flaky Playwright, cache corruption, test DB unavailable, frontend/backend version mismatch.
**Required Tests:** CI config lint if available; local commands matching CI; workflow presence check; push/run evidence after commit.
**Acceptance Criteria:** CI pipeline runs required gates and documents how release is blocked on failure.
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `npm --prefix frontend run build`; `rg -n "ReleasePipeline|dotnet build|playwright|secret" .github docs/product`.
**Definition Of Done:** Release pipeline is committed and pushed.
**Commit:** `ci(p9.8): add release pipeline`
**Push:** `git push origin main`

## P9.9 - Add Production Deployment Config

**Context:** Platform Deployment
**Purpose:** Provide deployable production/staging configuration bindings without committing secrets.
**User/Business Value:** The product can run outside local development with explicit database, storage, auth, worker, and observability settings.
**Dependencies:** P9.1, P9.3, P9.6, P9.7, P9.8.
**Detailed Scope:** Add staging/production config templates, environment variable mapping, deployment manifest stubs, secret binding docs, startup validation, and smoke test commands.
**Out Of Scope:** Purchasing cloud resources, committing real credentials, production domain DNS setup, full autoscaling strategy.
**Data Contract:** Uses PostgreSQL and object storage configured by environment; no SQLite fallback outside Development.
**API Contract:** Health/readiness endpoint must validate configured dependencies.
**UI Contract:** Frontend API base URL comes from environment and cannot point to localhost in production.
**Business Rules:** Missing required production config fails startup; wildcard CORS forbidden; demo auth/session disabled; worker enabled; object storage required for media assets.
**Edge Cases:** Missing DB string, wrong storage credentials, invalid auth issuer, disabled worker, frontend wrong API URL, stale migration.
**Required Tests:** Config binding tests; production missing-key tests; deployment manifest scan for secrets; smoke command docs.
**Acceptance Criteria:** Staging/production config templates are explicit, safe, validated, and documented; tests/build pass.
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "DeploymentConfig|USE_SECRET_MANAGER_VALUE|ConnectionStrings:ToeicDb" backend/src docs/product deployment .github`.
**Definition Of Done:** Production deployment config is committed and pushed.
**Commit:** `chore(p9.9): add production deployment config`
**Push:** `git push origin main`

## P9.10 - Add Release Readiness Checklist

**Context:** Product Release Governance
**Purpose:** Define the final go/no-go checklist before market release.
**User/Business Value:** The team avoids releasing an incomplete, unsafe, or unmeasured TOEIC product.
**Dependencies:** P0-P9.9.
**Detailed Scope:** Add release checklist covering product scope, TOEIC content readiness, learner journey, admin ops, auth/security, performance, observability, backup/restore, CI/CD, deployment smoke, known risks, and sign-off roles.
**Out Of Scope:** Implementing missing checklist items; those become blocking follow-up tasks.
**Data Contract:** Checklist references verified counts/metrics from DB/read models where possible.
**API Contract:** Release smoke uses health, auth, learner, admin, and test endpoints.
**UI Contract:** Release smoke includes learner and admin happy paths on desktop/mobile.
**Business Rules:** Any critical blocker prevents public release; private beta can proceed only with explicitly accepted risks and rollback plan.
**Edge Cases:** Partial content coverage, flaky tests, unresolved security finding, failed restore rehearsal, high latency, admin ops missing.
**Required Tests:** Checklist document scan; release smoke command list; evidence links/commands; no empty-row scan.
**Acceptance Criteria:** Checklist is complete, unambiguous, evidence-based, and maps each blocker to owner/status.
**Verification Commands:** `rg -n "Release Readiness|Go/No-Go|rollback|restore rehearsal|security baseline" docs/product`; `rg -n "TB[D]|TO[D]O" docs/product/96-add-release-readiness-checklist.md`.
**Definition Of Done:** Release readiness checklist is committed and pushed.
**Commit:** `docs(p9.10): add release readiness checklist`
**Push:** `git push origin main`

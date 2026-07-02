# Add Production Observability

## Task

P9.3 - Add Production Observability

## Purpose

Add the minimum production observability baseline required to diagnose API, learner journey, admin content operations, background jobs, and dependency failures.

## Detailed Scope

- Add correlation id middleware.
- Add structured request/response logs.
- Add safe log redaction rules.
- Split health into liveness and readiness if not already split.
- Add dependency health checks for database, storage, and job queue where implemented.
- Add the minimum metrics counters/histograms listed in this spec.
- Add audit log correlation for privileged operations.
- Add tests for correlation, health, redaction, and metrics smoke.

## Out Of Scope

- Full Grafana dashboard implementation.
- PagerDuty/Slack vendor integration.
- Long-term log retention infrastructure.
- Distributed tracing across external services.
- Production incident management process.

These are operational follow-up tasks after the code baseline exists.

## Data Contract

Operational records include correlation id, actor id when authenticated, route template, outcome, duration, and safe diagnostic detail. Logs and metrics must never contain tokens, passwords, answer keys before authorization, or raw source credentials.

## Log Contract

Every request log must include:

- timestamp
- level
- correlation id
- HTTP method
- route template or normalized path
- status code
- duration ms
- authenticated user id when available
- error code when request fails

Do not globally log request bodies. Only log endpoint-specific allowlisted fields.

Never log:

- password
- access token
- refresh token
- cookie
- signing key
- raw authorization header
- storage credential

## Health Contract

Endpoints:

- `GET /api/health/live`
- `GET /api/health/ready`

Rules:

- Live returns success when the process can respond.
- Ready returns failure when required dependency is unavailable.
- Ready response includes dependency names and safe status only.
- Health response must not expose connection strings, bucket secrets, tokens, or private hostnames.

## Metrics Contract

Minimum metrics:

- request count by route/status
- request duration by route/status
- background job count/status when jobs exist
- content publish count by TOEIC part when publish exists
- auth login success/failure when auth exists

Metric names must be stable and documented in code/tests.

## API Contract

Expose liveness/readiness health endpoints and metrics collection through the production-approved path. Health responses are safe for infrastructure and do not leak secrets or internal exception stacks.

## UI Contract

No learner UI dependency is required. Admin/operator status screens may consume health or operational summaries later, but they must use role-protected APIs.

## Business Rules

1. Every response has or propagates a correlation id.
2. Exceptions are logged with correlation id.
3. Readiness fails closed when production dependencies are unhealthy.
4. Sensitive data redaction is tested.
5. Observability cannot change business behavior.

## Edge Cases

- Request missing correlation header.
- Request supplies correlation header.
- Exception before endpoint handler.
- Auth failure.
- Database unavailable.
- Storage unavailable.
- Background job failure.
- Sensitive value appears in exception/message.

## Required Tests

- Correlation id is generated when missing.
- Correlation id is propagated when supplied.
- Error response includes correlation id.
- Readiness fails when dependency health fails.
- Live endpoint succeeds without dependency checks.
- Sensitive fields are redacted from structured logs.
- Metrics endpoint or metric collector exposes request count/duration smoke.

## Acceptance Criteria

- Logs are structured and correlation-aware.
- Health live/ready behavior is explicit.
- Sensitive data is not logged.
- Core metrics are emitted or collected.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "CorrelationId|ProductionObservability|Readiness|StructuredLog" backend/src backend/tests docs/product
```

## Commit

`feat(p9.3): add production observability`

## Push

```bash
git push origin main
```

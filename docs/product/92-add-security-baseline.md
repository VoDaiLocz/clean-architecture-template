# Add Security Baseline

## Task

P9.6 - Add Security Baseline

## Purpose

Add baseline OWASP controls required before public release.

## Detailed Scope

- Add secure HTTP headers.
- Add production CORS restrictions.
- Add rate limit policy for auth/admin/learner routes.
- Add request size limits.
- Add input validation conventions.
- Add secret scan check.
- Add dependency vulnerability check if available in project tooling.

## Out Of Scope

- Full penetration test.
- WAF provisioning.
- Compliance certification.
- Paid SAST/DAST integration.

## Data Contract

Security events may write audit or observability records for rate-limit, authorization, validation, and suspicious request outcomes. No schema may store plaintext secrets, passwords, or raw tokens.

## API Contract

Security middleware must preserve standardized error responses, allow health endpoints as configured, enforce production CORS, and reject unsafe request sizes or methods with stable error codes.

## UI Contract

Frontend production origins must be configured explicitly. UI must not depend on wildcard CORS or development-only relaxed security behavior.

## Business Rules

1. Wildcard CORS is forbidden outside Development.
2. HSTS is enabled in Production.
3. Admin/auth routes are rate-limited.
4. Request bodies above configured size are rejected.
5. Secrets/tokens/passwords are never logged.
6. Security middleware must preserve standard error contract.

## Edge Cases

- Preflight request.
- Local development origin.
- Production wildcard origin.
- Oversized request.
- Missing security header.
- Auth brute-force burst.
- Suspicious input string.

## Required Tests

- Header presence tests.
- CORS production negative test.
- Rate-limit behavior test.
- Request-size test.
- Secret scan command.
- Dependency audit command if available.

## Acceptance Criteria

- Security baseline is enforceable by tests.
- Unsafe production config fails.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "SecurityBaseline|Cors|HSTS|RateLimit" backend/src backend/tests docs/product
```

## Commit

`sec(p9.6): add security baseline`

## Push

```bash
git push origin main
```

# Add Production Deployment Config

## Task

P9.9 - Add Production Deployment Config

## Purpose

Provide safe staging/production configuration bindings for database, storage, auth, worker, frontend API URL, and observability.

## Detailed Scope

- Add staging/production config templates.
- Add environment variable mapping docs.
- Add secret token conventions using `USE_SECRET_MANAGER_VALUE`.
- Add startup config validation.
- Add deployment smoke commands.
- Add frontend production API base URL binding.

## Out Of Scope

- Real production credentials.
- DNS setup.
- Cloud resource purchase.
- Autoscaling implementation.

## Business Rules

1. SQLite fallback is forbidden outside Development.
2. In-memory storage is forbidden outside Development.
3. Demo auth/session is disabled outside Development.
4. Worker is enabled in Staging/Production.
5. CORS origins are explicit in Staging/Production.
6. Missing required config fails startup.

## Edge Cases

- Missing DB connection.
- Invalid storage credentials.
- Invalid auth issuer/audience.
- Frontend points to localhost in production.
- Worker disabled.
- Stale migration.

## Required Tests

- Production missing-key config test.
- Invalid production value test.
- Secret token scan.
- Deployment docs scan.
- Build/tests pass.

## Acceptance Criteria

- Config templates are safe and explicit.
- No secrets are committed.
- Startup validation prevents unsafe production boot.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "DeploymentConfig|USE_SECRET_MANAGER_VALUE|ConnectionStrings:ToeicDb" backend/src docs/product deployment .github
```

## Commit

`chore(p9.9): add production deployment config`

## Push

```bash
git push origin main
```

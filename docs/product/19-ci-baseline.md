# CI Baseline

## Purpose

The TOEIC platform must not accept broken backend commits on `main`.

P1.7 adds the first GitHub Actions backend quality gate.

## Workflow

```text
.github/workflows/backend-quality.yml
```

Triggers:

- push to `main`
- pull request

Permissions:

- `contents: read`

## Checks

The workflow runs:

```bash
dotnet restore backend/ToeicSystem.sln
dotnet build backend/ToeicSystem.sln --no-restore
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj --no-build
```

## Quality Rules

1. CI must build the whole backend solution.
2. CI must run the application unit test executable.
3. CI must not require secrets.
4. CI must run on pull requests and `main` pushes.
5. Future quality gates can add frontend, E2E, security, and deployment checks without weakening this baseline.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "backend-quality|dotnet build backend/ToeicSystem.sln|Application.UnitTest" .github/workflows/backend-quality.yml docs/product
```

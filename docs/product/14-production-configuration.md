# Production Configuration Strategy

## Purpose

This document defines how local, staging, and production configuration are handled for the TOEIC platform.

Configuration must allow the app to run locally without secrets while forcing production to provide explicit infrastructure settings.

## Environment Rules

| Environment | Database | Storage | Worker | Auth | Logging |
| --- | --- | --- | --- | --- | --- |
| Local/Development | SQLite default is allowed | local filesystem/emulator allowed | inline or disabled worker allowed | demo/dev auth allowed until P9 | console logs allowed |
| Staging | explicit managed DB connection required | object storage bucket required | background worker enabled | staging auth required | structured logs required |
| Production | explicit managed DB connection required | production object storage required | background worker enabled | production auth required | structured logs, metrics, traces required |

## Required Configuration Keys

| Key | Required In | Purpose | Secret |
| --- | --- | --- | --- |
| `ConnectionStrings:ToeicDb` | staging, production | primary database connection | yes |
| `Storage:Provider` | staging, production | object storage provider selection | no |
| `Storage:Bucket` | staging, production | source/media asset bucket | no |
| `Storage:Endpoint` | local, staging when using emulator/custom provider | object storage endpoint | no |
| `Storage:AccessKey` | staging, production when provider needs it | object storage credential | yes |
| `Storage:SecretKey` | staging, production when provider needs it | object storage credential | yes |
| `Worker:Enabled` | all | controls background job execution | no |
| `Worker:MaxRetryCount` | all | retry policy for extraction jobs | no |
| `Auth:Issuer` | staging, production | token/session issuer | no |
| `Auth:Audience` | staging, production | token/session audience | no |
| `Auth:SigningKey` | staging, production until managed identity replaces it | auth signing credential | yes |
| `Logging:MinimumLevel` | all | log verbosity | no |
| `Observability:ServiceName` | staging, production | metrics/tracing service name | no |

Secret values must be supplied by environment variables, deployment platform secret manager, or local user secrets. They must not be committed to the repository.

## Current Enforcement

P1.2 enforces the first production safety rule in code:

- Development can use the local SQLite fallback: `Data Source=toeic-normalization.db`.
- Production cannot use an implicit database fallback.
- Production startup requires `ConnectionStrings:ToeicDb`.

Implemented by:

- `Infrastructure.Configuration.ToeicPlatformOptions`
- `Infrastructure.DependencyInjection.AddInfrastructureDependencies`

Tested by:

- `production configuration requires explicit database`

## Configuration Source Priority

Runtime configuration should be loaded in this order:

1. committed non-secret defaults
2. environment-specific non-secret config
3. local user secrets for development
4. environment variables
5. deployment secret manager

Later sources override earlier sources.

## Commit Rules

Allowed:

- config key names
- local defaults with no credential value
- example placeholders
- documentation of required keys

Not allowed:

- real database credentials
- real object storage credentials
- real auth signing keys
- personal tokens
- production URLs that reveal private infrastructure

## Verification

P1.2 verification:

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ToeicPlatformOptions|ConnectionStrings:ToeicDb|Production Configuration Strategy" backend/src docs/product
```

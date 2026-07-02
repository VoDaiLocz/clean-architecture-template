# Production Configuration Strategy

## Purpose

This document defines the configuration contract for local, staging, and production runtime. It is the rule set every production-hardening task must obey when adding database, storage, worker, auth, logging, observability, deployment, and secret configuration.

## Non-Negotiable Rules

1. Development may use safe local defaults.
2. Staging and production must fail fast when required infrastructure settings are missing.
3. Secrets are never committed.
4. Production learner/admin behavior must not depend on demo configuration.
5. Every new configuration key must have owner, environment requirement, secret classification, validation rule, and test coverage.
6. Documentation examples must use non-routable example domains such as `example.invalid`.

## Environment Matrix

| Capability | Development | Staging | Production |
| --- | --- | --- | --- |
| Database | SQLite fallback allowed | Explicit PostgreSQL required | Explicit PostgreSQL required |
| Object storage | In-memory/local emulator allowed | Managed bucket required | Managed bucket required |
| Background worker | Inline/in-memory allowed | Worker enabled | Worker enabled |
| Authentication | Dev/demo auth allowed before P9.1 | Production auth required | Production auth required |
| Authorization | Optional before P9.2 | Required | Required |
| Logging | Console allowed | Structured console required | Structured console required |
| Observability | Optional local exporter | Health/log/metric baseline required | Health/log/metric baseline required |
| Secrets | User secrets/env vars | Secret manager/env vars | Secret manager/env vars |

## Current Implemented Contract

P1.2 currently enforces:

- Development can use local SQLite fallback.
- Production cannot use implicit database fallback.
- Production startup requires `ConnectionStrings:ToeicDb`.

Implemented by:

- `Infrastructure.Configuration.ToeicPlatformOptions`
- `Infrastructure.DependencyInjection.AddInfrastructureDependencies`

Tested by:

- `production configuration requires explicit database`

Any later task extending configuration must preserve this behavior.

## Configuration Key Registry

| Key | Owner Task | Required In | Secret | Validation Rule |
| --- | --- | --- | --- | --- |
| `ConnectionStrings:ToeicDb` | P1.2/P9.9 | Staging, Production | Yes | Must be non-empty and must not be SQLite/file path outside Development |
| `Storage:Provider` | P1.4/P9.9 | Staging, Production | No | Must be supported provider value |
| `Storage:Bucket` | P1.4/P9.9 | Staging, Production | No | Required when provider is remote |
| `Storage:Endpoint` | P1.4/P9.9 | Provider-specific | No | HTTPS required outside Development unless provider explicitly supports private endpoint |
| `Storage:AccessKey` | P1.4/P9.9 | Provider-specific | Yes | Required for credential-based providers |
| `Storage:SecretKey` | P1.4/P9.9 | Provider-specific | Yes | Required for credential-based providers |
| `Worker:Enabled` | P1.5/P9.9 | All | No | Must be true in Staging/Production |
| `Worker:MaxRetryCount` | P1.5 | All | No | Integer between 1 and 10 |
| `Auth:Issuer` | P9.1/P9.9 | Staging, Production | No | Must match token issuer validation |
| `Auth:Audience` | P9.1/P9.9 | Staging, Production | No | Must match token audience validation |
| `Auth:SigningKey` | P9.1/P9.9 | Staging, Production when symmetric token signing is configured | Yes | Minimum configured length; never default |
| `Auth:AccessTokenMinutes` | P9.1 | All | No | Positive; production maximum 60 |
| `Auth:RefreshTokenDays` | P9.1 | All | No | Positive; production maximum defined in auth spec |
| `Logging:MinimumLevel` | P9.3 | All | No | Must parse to supported level |
| `Observability:ServiceName` | P9.3 | Staging, Production | No | Non-empty, stable service id |
| `Observability:OtlpEndpoint` | P9.3/P9.9 | Optional | May include secret in headers only | HTTPS required outside Development |
| `Cors:AllowedOrigins` | P9.6/P9.9 | Staging, Production | No | Must not be wildcard outside Development |

## Configuration Source Priority

Runtime configuration is resolved in this order. Later sources override earlier sources.

1. Committed non-secret defaults.
2. Environment-specific non-secret config.
3. Local user secrets for development.
4. Environment variables.
5. Deployment secret manager.

## Commit Rules

Allowed:

- Key names and non-secret defaults.
- `.env.example` with clearly fake values.
- Placeholder tokens such as `USE_SECRET_MANAGER_VALUE`.
- Non-routable example URLs such as `https://auth.example.invalid`.
- Validation schema and docs.

Not allowed:

- Real database/storage/auth credentials.
- Personal API keys or tokens.
- Production private URLs or hostnames.
- Real JWT signing keys, private keys, refresh tokens, or cookies.
- Hardcoded production-only endpoints in code.

## Example Environment Variables

Use examples only. These are not deployable values.

```bash
TOEIC_CONNECTIONSTRINGS__TOEICDB=USE_SECRET_MANAGER_VALUE
TOEIC_STORAGE__PROVIDER=S3
TOEIC_STORAGE__BUCKET=toeic-assets-example
TOEIC_STORAGE__ACCESSKEY=USE_SECRET_MANAGER_VALUE
TOEIC_STORAGE__SECRETKEY=USE_SECRET_MANAGER_VALUE
TOEIC_AUTH__ISSUER=https://auth.example.invalid
TOEIC_AUTH__AUDIENCE=https://app.example.invalid
TOEIC_AUTH__SIGNINGKEY=USE_SECRET_MANAGER_VALUE
TOEIC_CORS__ALLOWEDORIGINS=https://app.example.invalid
```

## Required Tests For New Config Keys

Every task adding configuration must add tests for:

1. Development default behavior.
2. Production missing-key failure.
3. Production invalid-value failure.
4. Secret-scan or grep check showing no real secret is committed.
5. Documentation registry entry in this file.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ToeicPlatformOptions|ConnectionStrings:ToeicDb|Configuration Key Registry|USE_SECRET_MANAGER_VALUE" backend/src backend/tests docs/product
rg -n "password|secret|token|key" docs/product/14-production-configuration.md
```

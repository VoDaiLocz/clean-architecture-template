# PostgreSQL Migration Foundation

## Purpose

This document defines the first production database migration foundation for the TOEIC platform.

SQLite can remain a local development and fast test database. PostgreSQL is the production database target.

## Project

Migration code lives in:

```text
backend/src/DatabaseMigrations
```

The project is included in:

- `backend/ToeicSystem.sln`
- `backend/tests/Application.UnitTest/Application.UnitTest.csproj`

## Provider

Production migration provider:

```text
postgresql
```

The provider is exposed by:

```text
DatabaseMigrations.PostgresMigrationCatalog.Provider
```

## Initial Migration

First migration:

```text
001_platform_schema_history
```

Purpose:

- creates `platform_schema_history`
- records applied migration id
- records UTC apply time
- records migration checksum

This table is the foundation for future migration application and rollback evidence.

## Rules

1. Production migrations must use PostgreSQL SQL.
2. Production migration SQL must not use SQLite-specific syntax.
3. Migration IDs must be stable and ordered.
4. Every migration must be represented in `PostgresMigrationCatalog`.
5. Every migration must be testable without a live production database.
6. A future migration runner must record applied migrations in `platform_schema_history`.

## Current Enforcement

P1.3 adds a catalog-level test:

- catalog is not empty
- provider is `postgresql`
- first migration is `001_platform_schema_history`
- SQL creates `platform_schema_history`
- SQL does not contain SQLite-specific marker text

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "PostgresMigrationCatalog|platform_schema_history|DatabaseMigrations" backend/src backend/tests docs/product
```

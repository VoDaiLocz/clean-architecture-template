# Add Backup And Migration Strategy

## Task

P9.7 - Add Backup And Migration Strategy

## Purpose

Protect learner progress, content operations data, source inventory, and published TOEIC content from migration failure or data loss.

## Detailed Scope

- Document migration runbook.
- Document backup schedule contract.
- Add restore rehearsal checklist.
- Add migration rollback/forward-fix policy.
- Add validation commands for key table counts after restore.
- Add release gate requiring backup before destructive migration.

## Out Of Scope

- Purchasing managed database backups.
- Cross-region disaster recovery automation.
- Vendor-specific backup implementation if deployment target is not selected.

## Data Contract

Backup/restore must cover:

- auth users and tokens where retention permits
- learner profiles/state/attempts/reviews/mastery
- source manifests/assets/extraction/drafts
- published lessons/questions/tests
- migration history

## API Contract

No public learner API is added. Admin/ops endpoints, if introduced, must be role-protected and return backup/migration status without exposing credentials or raw database dumps.

## UI Contract

No learner UI dependency. Any admin release/ops screen must show backup and migration state as read-only unless a separate privileged operation task approves mutations.

## Business Rules

1. Destructive migration requires backup evidence.
2. Failed migration blocks deployment.
3. Restore rehearsal must verify key counts and app startup.
4. Production rollback strategy must prefer forward-fix when schema has changed.

## Edge Cases

- Failed migration.
- Partially applied migration.
- Corrupted backup.
- Restore to staging.
- Object storage not restored.
- Migration history mismatch.

## Required Tests

- Migration catalog test.
- Restore rehearsal command/checklist exists.
- Key table count validation command exists.
- Docs contain rollback/forward-fix policy.

## Acceptance Criteria

- Backup/migration runbook is executable.
- Restore evidence path is defined.
- Tests/build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "BackupMigration|restore rehearsal|migration rollback|forward-fix" backend/src backend/tests docs/product
```

## Commit

`chore(p9.7): add backup and migration strategy`

## Push

```bash
git push origin main
```

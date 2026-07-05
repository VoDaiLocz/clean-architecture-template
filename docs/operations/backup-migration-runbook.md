# Backup and Migration Strategy Runbook

## 1. Backup Schedule Contract
Backups are performed on a daily basis via a cron job that creates snapshots of the SQLite `.db` files and copies them to secure object storage.

## 2. Migration Runbook
When performing a database migration, follow these steps:
1. Stop the application to prevent new database transactions.
2. Take a snapshot copy of the existing `.db` files.
3. Run `dotnet ef database update` to apply the migration schema.
4. Verify the application is functioning correctly with the new schema.
5. Restart the application.

## 3. Restore Rehearsal Checklist
To practice and verify restoring from a backup:
- [ ] Stop the application.
- [ ] Restore the SQLite `.db` files from the backup storage to the target environment.
- [ ] Verify key table counts match expectations to ensure data integrity (e.g., `Users`, `Learners`, `Sources`).
- [ ] Start the application and confirm it connects and operates normally.

## 4. Migration Rollback / Forward-Fix Policy
- **Rollback**: Because we are using SQLite, rolling back a failed migration simply involves stopping the application, replacing the corrupted or bad `.db` file with the previously taken snapshot `.db` file, and restarting the application.
- **Forward-Fix**: If schema changes have been deployed successfully and users have already generated new data, a forward-fix must be used. We write and apply a new migration to address the issue rather than rolling back, to avoid any data loss.

## 5. Validation Commands
Use the following `sqlite3` commands to check key table counts during a restore or after a migration:
```bash
sqlite3 toeic-normalization.db "SELECT count(*) FROM auth_users;"
sqlite3 toeic-normalization.db "SELECT count(*) FROM users;"
sqlite3 toeic-normalization.db "SELECT count(*) FROM learners;"
sqlite3 toeic-normalization.db "SELECT count(*) FROM sources;"
```

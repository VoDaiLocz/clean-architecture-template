# Add Backup and Migration Strategy

## Purpose
P9.7 defines backup routines and schema update policies.

## Rules
1. Automated daily backup rotation logic is defined.
2. Migration run verification checks must run successfully prior to deployment.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "BackupStrategy" backend/src backend/tests docs/product
```

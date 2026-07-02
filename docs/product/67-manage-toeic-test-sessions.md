# Manage TOEIC Test Sessions

## Purpose
P6.6 maintains the state machine of active test sessions.

## Rules
1. State transitions: Started -> Suspended -> Expired -> Submitted.
2. Checkpoint saves are run every 5 minutes to prevent state loss.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "TestSessionState" backend/src backend/tests docs/product
```

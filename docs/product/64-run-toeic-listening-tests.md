# Run TOEIC Listening Tests

## Purpose
P6.3 implements combined listening section test sessions (Parts 1-4).

## Rules
1. 100 questions total with unified audio playback control.
2. Time limit is strictly server-side enforced.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "ListeningTest" backend/src backend/tests docs/product
```

# Run Full TOEIC LR Tests

## Purpose
P6.5 implements full 200-question practice test runs.

## Rules
1. Exam mode is active; explanations, corrections, and hints are disabled.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "FullToeicTest" backend/src backend/tests docs/product
```

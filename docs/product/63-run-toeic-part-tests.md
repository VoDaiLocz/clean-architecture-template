# Run TOEIC Part Tests

## Purpose
P6.2 implements runtime part-specific diagnostic tests.

## Rules
1. Part test composition must enforce official TOEIC counts (e.g. 30 questions for Part 5).

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "PartTest" backend/src backend/tests docs/product
```

# Calculate TOEIC Score Breakdown

## Purpose
P6.7 estimates overall scaled scores and part accuracies.

## Rules
1. Convert raw count to official scale score band via standard translation table.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "CalculateScoreBreakdown" backend/src backend/tests docs/product
```

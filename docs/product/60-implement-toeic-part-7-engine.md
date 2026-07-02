# Implement TOEIC Part 7 Engine

## Purpose
P5.8 implements reading comprehension passage behaviors for TOEIC Part 7.

## Rules
1. Single, double, and triple passages are supported.
2. Answer keys require evidence character offset spans matching ground text.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "Part7Engine" backend/src backend/tests docs/product
```

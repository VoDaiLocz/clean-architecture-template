# Implement TOEIC Part 3 Engine

## Purpose
P5.4 implements conversation groups for TOEIC Part 3.

## Rules
1. Part 3 questions are grouped; 3 child questions share 1 audio file.
2. Answers are scored individually but session tracks the group.
3. Script and transcript segments must match timestamps.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "Part3Engine" backend/src backend/tests docs/product
```

# Implement TOEIC Part 4 Engine

## Purpose
P5.5 implements talk groups for TOEIC Part 4.

## Rules
1. Part 4 requires talk audio and exactly 3 child questions.
2. Audio and text validation checks are executed during human review.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "Part4Engine" backend/src backend/tests docs/product
```

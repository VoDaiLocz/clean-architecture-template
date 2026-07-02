# Implement TOEIC Part 6 Engine

## Purpose
P5.7 implements text completion passage behaviors for TOEIC Part 6.

## Rules
1. Passage content contains numbered blank anchors matching linked questions.
2. Order of blanks must match the display order of child questions.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "Part6Engine" backend/src backend/tests docs/product
```

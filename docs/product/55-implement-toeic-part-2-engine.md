# Implement TOEIC Part 2 Engine

## Purpose
P5.3 implements question-response audio-first behaviors for TOEIC Part 2.

## Rules
1. Part 2 questions require audio; prompt text must not be shown to learners.
2. Exactly 3 answer choices (A, B, C) are supported.
3. Audio metadata duration check is enforced on publishing.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "Part2Engine" backend/src backend/tests docs/product
```

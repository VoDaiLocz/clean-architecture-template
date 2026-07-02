# Implement TOEIC Part 1 Engine

## Purpose
P5.2 implements photograph question behaviors for TOEIC Part 1.

## Rules
1. Part 1 requires both image and audio media asset references.
2. Exactly 4 answer options (A, B, C, D) are expected.
3. Prompt text is not required; audio playback is the primary source.
4. Content validation fails if the image asset checksum is missing.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "Part1Engine|photograph" backend/src backend/tests docs/product
```

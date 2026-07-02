# Define TOEIC Item Contracts

## Purpose
P5.1 defines a unified learning item contract with part-specific extension payloads to guarantee contract alignment.

## Domain Model
- `ToeicItemContract`
  - `QuestionId` (string, UUID)
  - `ToeicPart` (int, 1-7)
  - `Prompt` (string)
  - `Choices` (List of string)
  - `CorrectAnswer` (string)
  - `Explanation` (string)
  - `SkillTags` (List of string)
  - `MediaRef` (string, nullable)

## API Contract
- Endpoint: `GET /api/published-questions/{questionId}`
- Response JSON:
  ```json
  {
    "questionId": "q-101",
    "toeicPart": 1,
    "prompt": "Look at the picture.",
    "choices": ["A", "B", "C", "D"],
    "skillTags": ["photograph"],
    "mediaRef": "part1_audio.mp3"
  }
  ```

## Rules
1. Published questions must hide the correct answer in play mode.
2. Parts 1-4 require media reference keys in the payload.
3. Group and passage relations are mapped correctly by question type.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "ToeicItemContract|published_questions" backend/src backend/tests docs/product
```

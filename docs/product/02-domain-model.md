# Domain Model

## Bounded Contexts

Detailed ownership rules, phase owners, cross-context contract types, and task review checklist are defined in [04-bounded-context-ownership.md](./04-bounded-context-ownership.md). This file defines the domain vocabulary and core rules; the ownership file is the implementation governance contract.

### Content Factory

Purpose: transform source materials into reviewed published content.

Owns:

- `SourceManifestEntry`
- `SourceContainer`
- `SourceAsset`
- `ExtractedPage`
- `ExtractedTextBlock`
- `ExtractedMediaSegment`
- `ParserRun`
- `DraftContentItem`
- `ValidationIssue`
- `ReviewDecision`

Does not own:

- learner progression
- attempt scoring
- Today Plan assignment

### Learning Content

Purpose: store learner-ready content.

Owns:

- `PublishedLesson`
- `GuidedExample`
- `PublishedQuestion`
- `QuestionGroup`
- `Passage`
- `EvidenceSpan`
- `PublishedTest`
- `AnswerKey`
- `Transcript`
- `AudioAsset`
- `ImageAsset`

Does not own:

- raw PDF text
- extraction logs
- learner state

### Learner Journey

Purpose: decide what a learner should do next.

Owns:

- `LearnerProfile`
- `PlacementResult`
- `LearningPath`
- `LearningUnit`
- `LearnerAssignment`
- `ActivitySession`

Does not own:

- content parsing
- admin review
- raw source discovery

### Attempt And Review

Purpose: process answers, score work, create review, and evaluate mastery.

Owns:

- `Attempt`
- `AttemptAnswer`
- `ReviewItem`
- `RepairAttempt`
- `MasteryRecord`
- `TestResult`

### Analytics And Operations

Purpose: provide visibility into content quality, learner progress, system health, and release readiness.

Owns read models and metrics only. It must not become the source of truth for learner or content behavior.

## Core Business Rules

### Unit Completion Rule

```text
UnitCompleted =
  LessonCompleted
  AND RequiredGuidedExamplesCompleted
  AND RequiredDrillsCompleted
  AND MiniTestScore >= UnitThreshold
  AND BlockingReviewItemsResolved
  AND NoCriticalWeaknessBlockingNextUnit
```

Threshold defaults:

- foundation unit: 75%
- normal unit: 80%
- final part unit: 85%

### Review Rule

Every wrong answer must create or update a `ReviewItem`.

A review item must contain:

- learner answer
- correct answer
- explanation
- skill/error tag
- linked source content
- transcript or evidence when relevant
- repair action

### Unlock Rule

The system can unlock the next unit only when the previous unit is completed. The system must return learner-facing lock reasons.

### Publish Rule

Draft content cannot become published content unless it passes the part-specific validation gate or receives explicit human approval after validation issue resolution.

## TOEIC Part Contracts

- Part 1 requires image and audio.
- Part 2 requires audio.
- Part 3 and Part 4 require grouped audio and grouped questions.
- Part 5 requires sentence, answer options, correct answer, and explanation.
- Part 6 requires passage context.
- Part 7 requires passage and evidence span.

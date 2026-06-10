# TOEIC 800+ Hybrid Learning System Design

## Purpose

Build a production-grade TOEIC study system for end users who want to reach 800+ efficiently. The system must not expose raw PDFs, Google Sheet rows, source folders, import queues, or technical validation language to learners. Source material is normalized into structured learning content inside the database, then used by a mastery-based learning engine.

The learner experience is simple: continue the next assigned activity, see concise progress, review mistakes, and optionally browse the 7 TOEIC parts. The detailed learning roadmap is business logic inside the system, not a long roadmap UI.

## Product Direction

Use a hybrid model:

- The system owns a required 800+ learning path.
- The user can browse the 7 parts, but cannot bypass mastery gates.
- Each part adapts practice and review based on the learner's errors.
- Each unit must be completed 100% before the next unit unlocks. In this spec, 100% means all required gates are satisfied; it does not mean the learner must score 100% on every quiz.

The UI should show only what helps the learner act:

- next activity
- current part and unit
- progress toward unlocking the next unit
- mistakes that must be reviewed
- available 7-part overview

The UI must not show internal roadmap trees, source paths, PDF links, corpus pipelines, validation gates, or admin/import concepts.

## Learning Content Model

This is a learning platform, not a question bank. Every learning unit must teach before it tests.

Required unit sequence:

1. Concept lesson
2. Guided examples
3. Focus drill
4. Mini test
5. Mistake repair
6. Unlock decision

### Concept Lesson

The concept lesson explains the skill that the learner needs before practice. It should be short, concrete, and tied to a TOEIC use case.

Examples:

- Part 5 word form: how to identify whether a blank needs a noun, verb, adjective, or adverb.
- Part 2 WH-question: how the first question word predicts the expected answer type.
- Part 7 detail question: how to scan for names, dates, and paraphrases.

Concept lessons are normalized content in the database, not static UI copy.

### Guided Examples

Guided examples show solved items before the learner starts the drill.

Each guided example contains:

- prompt or media
- correct answer
- step-by-step reasoning
- why distractors are wrong
- linked grammar/vocabulary/listening/reading point
- source trace for admin audit

Guided examples are required because learners targeting 800+ need method and reasoning, not only answer checking.

### Focus Drill

Focus drills isolate one skill or trap at a time. They must not be random mixed questions.

Examples:

- Part 5: only word form questions until the unit is mastered.
- Part 2: only WH-question responses until the unit is mastered.
- Part 7: only detail scanning questions before inference questions.

### Mini Test

Mini tests verify whether the learner can apply the skill without guidance. Mini tests are used for unlock decisions.

### Mistake Repair

Mistake repair is required after wrong answers. It includes:

- error explanation
- correct reasoning
- one or more repair questions
- updated review item

The learner cannot unlock the next unit while blocking mistakes remain unresolved.

## Practice Test Mode

The system must include structured practice tests. Learning units build skill; practice tests measure performance under TOEIC-like conditions.

Practice test levels:

1. Mini test
2. Part test
3. Skill test
4. Full TOEIC test

### Mini Test

Mini tests belong to one learning unit and are used for unit unlock decisions.

Example:

- Part 5 word form unit: 10 mixed word-form questions
- Part 2 WH-question unit: 10 audio response questions

Mini tests should be short and focused. They prove the learner has mastered one concept.

### Part Test

Part tests simulate one TOEIC part after the learner completes all required units in that part.

Required part test structure:

- Part 1: 6 questions with images and audio
- Part 2: 25 audio response questions
- Part 3: 39 questions grouped by conversations
- Part 4: 30 questions grouped by talks
- Part 5: 30 incomplete sentence questions
- Part 6: 16 text completion questions
- Part 7: 54 reading comprehension questions

Part tests are timed. They produce:

- score
- accuracy by unit
- accuracy by error tag
- average time per item or passage
- review queue
- unlock or repair decision

### Skill Test

Skill tests combine related TOEIC parts:

- Listening test: Parts 1-4
- Reading test: Parts 5-7

Skill tests help learners practice endurance before full TOEIC tests.

### Full TOEIC Test

Full TOEIC tests simulate a complete TOEIC Listening and Reading exam:

- 200 questions
- Listening section: 100 questions
- Reading section: 100 questions
- TOEIC-like timing
- no guided hints during test mode
- result screen after submission

Full test result must include:

- estimated score band
- Listening accuracy
- Reading accuracy
- part-by-part breakdown
- slowest part or passage type
- strongest and weakest error tags
- recommended repair plan

The full test is not the default starting experience. It unlocks after enough foundation units are completed or can be used as a placement test at onboarding.

### Test Review

Every practice test must produce a review session. Review is not optional for mastery progression.

Review includes:

- wrong answer
- correct answer
- explanation
- transcript or passage evidence when relevant
- error tag
- repair activity

The next learning assignment must use test results. If a learner fails Part 7 timing, the system assigns scan/detail drills before another full Part 7 test.

## Learning Path

The system uses this internal 800+ path:

1. Placement test
2. Foundation: Part 5 and Part 2
3. Core: Part 1 and Part 6
4. Listening expansion: Part 3 and Part 4
5. Score builder: Part 7
6. Full test cycles
7. Weakness repair and timing optimization

This path can adapt within each stage, but it remains structured. Learners should not be dropped into a blank 7-part menu with no guidance.

## Mastery Unlock Rule

Every part is divided into ordered learning units. A unit is completed only when all required gates pass:

```text
UnitCompleted =
  LessonViewed
  && RequiredDrillsCompleted
  && MiniTestScore >= UnitThreshold
  && WrongItemsReviewed
  && NoBlockingWeakness
```

Default threshold:

- normal unit: 80%
- pre-test or foundation unit: 75%
- final part test: 85%

If a learner fails a gate:

- the next unit remains locked
- the system assigns repair drills
- mistakes are added to review
- repeated mistakes increase review priority

The UI should communicate this plainly, for example:

- "Lam them 5 cau word form de mo khoa bai tiep theo"
- "Can sua 3 loi sai truoc khi lam mini test"
- "Dat 80% de mo khoa Unit 2"

## TOEIC Part Engines

Each TOEIC part has a dedicated learning engine because the item types are different.

### Part 1 - Photographs

Required content types:

- image
- audio prompt
- answer options
- transcript
- explanation
- visual trap tag

Internal unit path:

1. people/action description
2. object/location description
3. passive and present continuous traps
4. distractor vocabulary
5. mini test
6. part test

Learning behavior:

- show image and play audio
- learner chooses answer
- review highlights why distractor is wrong
- wrong answers are grouped by visual trap type

### Part 2 - Question-Response

Required content types:

- audio question
- audio/text answer options
- transcript
- question type tag
- distractor type tag

Internal unit path:

1. WH-question recognition
2. Yes/No and choice questions
3. indirect responses
4. sound-alike traps
5. short response speed drill
6. mini test
7. part test

Learning behavior:

- learner hears question and options
- optional replay is limited during test mode
- review explains expected response type
- adaptive practice prioritizes repeated question-type mistakes

### Part 3 - Conversations

Required content types:

- conversation audio
- transcript with speaker turns
- 3 linked questions
- answer options
- intent/detail/inference tags

Internal unit path:

1. preview questions before audio
2. identify speakers and situation
3. detail questions
4. inference questions
5. graphic-based questions if available
6. mini test
7. part test

Learning behavior:

- questions are grouped by conversation
- review shows transcript segment after answering
- mistakes are tracked by question role

### Part 4 - Talks

Required content types:

- talk audio
- transcript
- 3 linked questions
- answer options
- talk type tag
- detail/time/number tags

Internal unit path:

1. announcements
2. voicemail and phone messages
3. advertisements
4. workplace talks
5. number/time detail drills
6. mini test
7. part test

Learning behavior:

- learner previews questions, listens once, answers group
- review links each answer to transcript evidence
- repeated timing/detail errors trigger extra drills

### Part 5 - Incomplete Sentences

Required content types:

- sentence prompt
- answer options
- correct answer
- explanation
- grammar/vocabulary point tag
- error trap tag

Internal unit path:

1. word form
2. verb tense and agreement
3. prepositions
4. conjunctions and clauses
5. collocations
6. vocabulary in context
7. speed drill
8. mini test
9. part test

Learning behavior:

- show one sentence at a time
- explanation teaches the rule, not only the answer
- speed mode limits time per question
- repeated grammar tags drive adaptive review

### Part 6 - Text Completion

Required content types:

- passage
- blanks
- answer options
- sentence insertion item when available
- explanation
- cohesion tag

Internal unit path:

1. sentence context
2. paragraph flow
3. transition words
4. pronoun/reference clues
5. sentence insertion
6. mini test
7. part test

Learning behavior:

- show passage with blanks
- require reading surrounding sentences
- review explains context evidence
- wrong items are grouped by cohesion issue

### Part 7 - Reading Comprehension

Required content types:

- single, double, or triple passage
- question group
- answer options
- evidence span
- question type tag
- timing data

Internal unit path:

1. single passage scanning
2. detail questions
3. inference questions
4. vocabulary in context
5. double passage linking
6. triple passage timing
7. full Part 7 timed set

Learning behavior:

- learners answer passage groups
- review shows evidence span
- timing is measured per passage
- repeated slow/incorrect passage types trigger extra scan drills

## Content Normalization

Source material from Google Sheet and PDFs must be transformed into structured database content. Learner flows must never open source PDFs as the learning experience.

Normalized entities:

- SourceDocument
- SourceBlock
- Lesson
- GuidedExample
- LearningUnit
- FocusDrill
- QuestionItem
- AudioAsset
- ImageAsset
- Transcript
- Passage
- AnswerOption
- Explanation
- VocabularyPoint
- GrammarPoint
- ErrorTag
- PartTest
- PracticeTest
- FullTest
- TestAttempt
- TestSectionResult
- ReviewItem

Every learner-facing item must keep internal source trace:

- source document id
- page or sheet row
- block id
- extraction confidence
- validation status

Source trace is for admin/audit/debugging, not normal learner UI.

## Validation Rules

Content can be published to learner flow only when its type-specific gates pass.

Common gates:

- has source trace
- has TOEIC part
- has skill
- has unit
- has content role: concept lesson, guided example, drill, mini test, review, or part test
- has answer key
- has explanation
- has difficulty
- has error tags
- has validation status

Type-specific gates:

- Part 1 requires image and audio
- Part 2 requires audio question and transcript
- Part 3/4 require grouped audio, transcript, and grouped questions
- Part 5 requires sentence prompt and grammar/vocabulary tag
- Part 6 requires passage and blank mapping
- Part 7 requires passage group and evidence span

Invalid content goes to admin review and must not appear in learner flow.

## Learner State

The system tracks learning progress independently from raw content.

Core learner state:

- current path stage
- active part
- active unit
- completed lesson ids
- completed drill ids
- mini test attempts
- part test attempts
- wrong item history
- review queue
- unlocked units
- locked units with reasons

Unlock calculation must be deterministic and testable.

## Adaptive Review

Every wrong answer creates or updates a review item:

- part
- unit
- question id
- error tag
- mistake count
- last attempted at
- next review due at
- resolved status

Review priority:

1. repeated error tag
2. current unit blocking issue
3. upcoming mini test dependency
4. old unresolved mistakes

The review UI should show concise tasks, not raw analytics.

## End-User Screens

### Home

Primary purpose: tell learner what to do now.

Required elements:

- continue learning card
- current part/unit
- unlock requirement
- today review count
- recent score trend
- link to 7 parts

Forbidden elements:

- corpus statistics
- import pipeline
- source IDs
- validation status
- admin terminology

### 7 Parts

Primary purpose: let learner understand the TOEIC structure and inspect progress.

Required elements:

- Part 1 to Part 7 cards
- locked/unlocked/current state
- concise progress per part
- CTA to continue if unlocked
- locked reason if locked

### Part Detail

Primary purpose: continue the current unit for that part.

Required elements:

- active unit
- next required task
- unit completion gates
- available review items
- mini test or part test when unlocked

The internal roadmap should not be displayed as a long checklist.

### Practice Player

Primary purpose: complete one learning activity.

Required variants:

- image + audio player for Part 1
- audio response player for Part 2
- grouped audio/passage player for Part 3/4/6/7
- sentence drill player for Part 5

### Practice Tests

Primary purpose: let learners train under TOEIC-like conditions after they have enough foundation.

Required elements:

- available mini tests, part tests, skill tests, and full tests
- locked/unlocked state
- test duration
- question count
- last score if attempted
- start/resume CTA
- post-test review CTA

Practice test screens must not replace the guided learning path. If a test is locked, the screen must show the next learning or review task required to unlock it.

### Review

Primary purpose: clear blocking mistakes.

Required elements:

- due mistakes
- why the answer was wrong
- short repair drill
- completion status

## Admin Screens

Admin screens can expose technical workflow:

- source imports
- extraction status
- validation issues
- content normalization
- source trace
- publish gates

Admin is separate from learner UI.

## API Boundaries

Learner API:

- `GET /api/learner/home`
- `GET /api/learner/parts`
- `GET /api/learner/parts/{partId}`
- `GET /api/learner/activities/{activityId}`
- `POST /api/learner/activities/{activityId}/attempts`
- `GET /api/learner/review`
- `POST /api/learner/review/{reviewItemId}/attempts`

Admin/content API:

- `POST /api/raw-sources`
- `POST /api/normalized-items`
- `POST /api/content-validation`
- `POST /api/publish`
- `GET /api/dashboard`

Learner APIs must not leak admin/import terms.

## Testing Strategy

Tests must prove business behavior, not just render cards.

Backend tests:

- unit cannot unlock before all gates pass
- unit without concept lesson cannot publish
- unit without guided examples cannot publish
- mini test score below threshold blocks next unit
- part test cannot unlock before all required units are complete
- full test creates part-by-part result and review queue
- wrong answer creates review item
- resolved review item can unblock a unit
- each TOEIC part validates required content type
- invalid audio/image/passage content cannot publish
- adaptive priority selects repeated errors first

Frontend tests:

- home shows next action, not admin pipeline
- 7 parts show locked/current/unlocked states
- part detail shows next action, not full internal roadmap
- practice flow shows lesson/example before drill for an incomplete unit
- practice test mode hides hints and shows timing
- full test result shows score band, part breakdown, and repair plan
- Part 1 player requires image and audio
- Part 2 player shows audio response flow
- Part 5 player shows sentence drill and explanation
- locked next unit shows reason
- review task clears after successful repair

E2E smoke tests:

- new learner takes placement and receives next activity
- learner completes a Part 5 unit and unlocks next unit
- learner fails a Part 2 mini test and receives repair review
- learner completes a Part 5 part test and receives weakness breakdown
- learner completes a full test and receives a repair plan
- learner cannot open a locked unit directly by URL
- admin can validate and publish content, then learner sees only learner-safe fields

## Phasing

### Phase 1 - Learning Engine Foundation

- domain models for path, part, unit, activity, attempt, review, unlock gate
- tests for mastery unlock and adaptive review
- learner API contracts
- no UI polish until business behavior is testable

### Phase 2 - Part 5 and Part 2 Vertical Slices

- full Part 5 sentence drill flow
- full Part 2 audio-response flow
- review queue
- unlock behavior
- learner home next-action UI

### Phase 3 - Extend to Parts 1, 3, 4, 6, 7

- image/audio player for Part 1
- grouped listening players for Part 3/4
- passage players for Part 6/7
- content validation gates for all part types

### Phase 4 - Corpus Ingestion

- map Google Sheet/PDF content into structured DB entities
- source trace
- validation reports
- admin review workflow

### Phase 5 - 800+ Optimization

- timing analytics
- full test cycles
- score estimate
- weakness-based repair plan
- practice test history and trend analysis

## Acceptance Criteria

The system is not considered production-ready until:

- all 7 TOEIC parts have real activity types
- each part has internal unit progression
- locked content cannot be bypassed
- completing one unit requires all mastery gates
- the system supports mini tests, part tests, skill tests, and full TOEIC tests
- practice tests are timed and create review/repair assignments
- wrong answers create review items
- review items affect next assigned activity
- learner UI never exposes admin/import/source pipeline concepts
- source PDFs are not the learner experience
- content from sources is normalized into structured DB entities
- tests cover unlock, review, validation, scoring, and at least one activity per major content type

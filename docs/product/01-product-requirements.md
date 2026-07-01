# Product Requirements

## Problem Statement

TOEIC learners often fail to improve because they study scattered PDFs, do random questions, skip mistake repair, and do not know which part or skill to study next. They may complete many exercises but still lack a reliable path to higher scores because practice is not connected to diagnosis, review, timing, and mastery.

The product must convert a large source library into structured learning and testing workflows. The learner should experience a guided system, not a file archive.

## Target User Segments

### Foundation Learner

- Current score: below 450 or unknown.
- Goal: build basic TOEIC vocabulary, Part 2 response skill, and Part 5 grammar foundation.
- Product need: clear starting point, short lessons, guided drills, confidence, and basic review.

### Growth Learner

- Current score: 450-650.
- Goal: reach 700+.
- Product need: structured path across all parts, mistake repair, timing discipline, and part-level tests.

### Score Builder

- Current score: 650-800.
- Goal: reach 800+.
- Product need: advanced Part 3/4/7 practice, full tests, detailed weakness analysis, and targeted repair cycles.

### Internal Content Operator

- Goal: turn source material into validated learner-ready content.
- Product need: source inventory, extraction state, draft review, validation issue workflow, publish queue, and coverage dashboard.

## Product Outcomes

The platform succeeds when:

- new users complete placement and receive a personalized Today Plan
- users understand exactly what to study next
- wrong answers become actionable review
- users can progress through all 7 TOEIC parts through mastery
- practice tests generate repair plans, not just scores
- content operators can safely publish high-quality structured content

## Success Metrics

Learner metrics:

- onboarding completion rate
- placement completion rate
- Today Plan start rate
- activity completion rate
- review completion rate
- unit mastery rate
- part test improvement
- full test score estimate improvement
- retention at day 7 and day 30

Content metrics:

- source discovery completion
- extraction success rate
- draft validation pass rate
- human review throughput
- published item defect rate
- missing audio rate
- missing answer key rate
- missing evidence rate

Technical metrics:

- API p95 latency
- background job success rate
- test suite pass rate
- deploy success rate
- error rate by endpoint
- database migration success rate

## Product Scope Rules

In the first market release, the product must focus on TOEIC Listening and Reading. TOEIC Speaking and Writing sources can be stored in inventory, but must not drive learner UX or initial learning path logic.

The learner product and admin content product must be separated. Shared data is allowed; shared screens and mixed mental models are not.


# User Journeys

## New Learner Journey

1. Learner opens app.
2. System checks whether learner profile exists.
3. If profile is missing, system shows onboarding.
4. Learner enters target score, current estimate, test date, and available study time.
5. System creates learner profile.
6. System recommends placement.
7. Learner starts placement.
8. System assigns balanced TOEIC LR questions.
9. Learner submits answers.
10. System scores by part and skill tag.
11. System creates learning path.
12. System creates Today Plan.
13. Learner starts first assigned activity.

Completion requirements:

- profile persisted
- placement result persisted
- path generated
- Today Plan generated from server-side logic

## Daily Study Journey

1. Learner opens Today screen.
2. System returns primary assignment and review blockers.
3. If blocking review exists, repair activity is prioritized.
4. If no blocker exists, next incomplete unit activity is assigned.
5. Learner completes activity.
6. System updates progress.
7. System returns next activity.

Completion requirements:

- FE does not compute next activity
- assignment state persists
- review blockers change assignment priority

## Learning Unit Journey

Required order:

1. Concept lesson
2. Guided example
3. Focus drill
4. Mini test
5. Mistake repair if needed
6. Unlock decision

The system may add extra repair or drill activities when performance is weak.

## Practice Test Journey

1. Learner selects eligible test.
2. System creates test session.
3. Learner answers questions under timing rules.
4. System scores the test.
5. System generates breakdown by part, skill tag, and timing.
6. System creates review items and repair plan.
7. Today Plan updates based on result.

## Admin Content Journey

1. Admin imports or refreshes source manifest.
2. System discovers containers and assets.
3. System extracts raw text/media metadata.
4. Parser creates draft content.
5. Validation gates mark pass/fail.
6. Admin reviews issues.
7. Admin approves, rejects, relabels, or requests re-extraction.
8. Approved content publishes to learner content tables.


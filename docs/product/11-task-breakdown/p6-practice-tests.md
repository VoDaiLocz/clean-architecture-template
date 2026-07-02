# P6 - Practice Test System

## Phase Goal

Provide TOEIC-like practice modes with durable sessions, backend-owned timing, scoring, and repair plans.

## Source Of Truth

Detailed implementation contracts live in the standalone task specs below. This phase file is an execution index only.

| Task | Detailed Spec | Purpose | Key Pass Criteria | Commit |
| --- | --- | --- | --- | --- |
| P6.1 | [62-run-toeic-mini-tests.md](../62-run-toeic-mini-tests.md) | Verify unit mastery | Unit-scoped result feeds mastery gate | `feat(p6.1): run TOEIC mini tests` |
| P6.2 | [63-run-toeic-part-tests.md](../63-run-toeic-part-tests.md) | Test one TOEIC part | Part blueprint enforced | `feat(p6.2): run TOEIC part tests` |
| P6.3 | [64-run-toeic-listening-tests.md](../64-run-toeic-listening-tests.md) | Combine Parts 1-4 | Audio availability and section breakdown supported | `feat(p6.3): run TOEIC listening tests` |
| P6.4 | [65-run-toeic-reading-tests.md](../65-run-toeic-reading-tests.md) | Combine Parts 5-7 | Passage grouping and timing supported | `feat(p6.4): run TOEIC reading tests` |
| P6.5 | [66-run-full-toeic-lr-tests.md](../66-run-full-toeic-lr-tests.md) | Simulate LR exam | 200 questions, no hints, timed | `feat(p6.5): run full TOEIC LR tests` |
| P6.6 | [67-manage-toeic-test-sessions.md](../67-manage-toeic-test-sessions.md) | Prevent unreliable testing | Resume/expire/submit rules defined | `feat(p6.6): manage TOEIC test sessions` |
| P6.7 | [68-calculate-toeic-score-breakdown.md](../68-calculate-toeic-score-breakdown.md) | Explain performance | Part/tag/time breakdown returned | `feat(p6.7): calculate TOEIC score breakdown` |
| P6.8 | [69-generate-toeic-test-repair-plans.md](../69-generate-toeic-test-repair-plans.md) | Convert test into study plan | Weakness repair assignments created | `feat(p6.8): generate TOEIC test repair plans` |

## Required P6 Acceptance Standard

- Practice sessions freeze question assignments at start.
- Timer, scoring, expiration, and final submit are backend-owned.
- Test results feed weakness tagging, review queue, mastery, or repair plans as specified.

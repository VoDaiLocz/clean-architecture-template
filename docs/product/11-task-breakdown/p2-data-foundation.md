# P2 - Production Data Foundation

## Phase Goal

Create durable data structures for content, learners, assignments, attempts, reviews, and mastery.

| Task | Purpose | Key Pass Criteria | Commit |
| --- | --- | --- | --- |
| P2.1 Source/container/asset schema | Represent real source inventory and assets | Source, container, asset tables with indexes and statuses | `feat(p2.1): model TOEIC source assets` |
| P2.2 Extracted content schema | Store extracted PDF/web/audio metadata | Extracted pages and blocks persist with confidence | `feat(p2.2): model extracted TOEIC content` |
| P2.3 Draft content schema | Store parser output safely | Drafts cannot appear in learner APIs | `feat(p2.3): model TOEIC draft content` |
| P2.4 Published lesson schema | Store lessons and guided examples | Lessons linked to units and skill tags | `feat(p2.4): model published TOEIC lessons` |
| P2.5 Published question schema | Store part-specific questions | Required fields enforced by part | `feat(p2.5): model published TOEIC questions` |
| P2.6 Test schema | Store mini/part/skill/full tests | Test sections and item ordering persist | `feat(p2.6): model TOEIC test structures` |
| P2.7 Learner profile schema | Persist learner profile | Learner survives restart; goal fields stored | `feat(p2.7): model learner profiles` |
| P2.8 Assignment/attempt schema | Persist work lifecycle | Assignment and attempt relations enforced | `feat(p2.8): model learner assignments and attempts` |
| P2.9 Review/mastery schema | Persist review and unlock state | Wrong answer creates review; mastery queryable | `feat(p2.9): model review and mastery records` |
| P2.10 Integrity and indexes | Protect data quality and performance | FK/index tests pass; invalid rows rejected | `feat(p2.10): enforce TOEIC data integrity` |

## Required Tests For P2

- migration test
- repository integration test
- invalid insert rejection
- idempotent write test where applicable
- domain rule test for required relationships


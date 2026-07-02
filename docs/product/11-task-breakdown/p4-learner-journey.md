# P4 - Learner Journey Core

## Phase Goal

Replace demo learner flow with a persisted, backend-owned production journey from onboarding to mastery unlock.

## Source Of Truth

Detailed implementation contracts live in the standalone task specs below. This phase file is an execution index only; do not add competing business rules here.

| Task | Detailed Spec | Purpose | Key Pass Criteria | Commit |
| --- | --- | --- | --- | --- |
| P4.1 | [43-learner-onboarding.md](../43-learner-onboarding.md) | Capture learner goal | Profile persisted; next action returned | `feat(p4.1): implement learner onboarding` |
| P4.2 | [44-persisted-learner-home.md](../44-persisted-learner-home.md) | Remove memory-only learner state | Profile survives restart | `feat(p4.2): persist learner profile state` |
| P4.3 | [45-placement-session-start.md](../45-placement-session-start.md) | Create placement session | Duplicate active session handled | `feat(p4.3): start TOEIC placement session` |
| P4.4 | [46-score-toeic-placement.md](../46-score-toeic-placement.md) | Diagnose part weaknesses | Diagnostic band and part/tag breakdowns persisted | `feat(p4.4): score TOEIC placement` |
| P4.5 | [47-generate-learner-path-from-placement.md](../47-generate-learner-path-from-placement.md) | Create path from diagnosis | Starting units generated from placement | `feat(p4.5): generate learner path from placement` |
| P4.6 | [48-assign-learner-today-plan.md](../48-assign-learner-today-plan.md) | Assign next best activity | Review blockers outrank new lessons | `feat(p4.6): assign learner today plan` |
| P4.7 | [49-manage-learner-activity-sessions.md](../49-manage-learner-activity-sessions.md) | Track activity state | Start, resume, complete states persist | `feat(p4.7): manage learner activity sessions` |
| P4.8 | [50-process-learner-attempts.md](../50-process-learner-attempts.md) | Score learner work | Attempt persists and returns result | `feat(p4.8): process learner attempts` |
| P4.9 | [51-create-learner-review-queue.md](../51-create-learner-review-queue.md) | Force repair | Wrong answer creates review item | `feat(p4.9): create learner review queue` |
| P4.10 | [52-enforce-mastery-unlocks.md](../52-enforce-mastery-unlocks.md) | Enforce progression | Unit unlocks only after all gates pass | `feat(p4.10): enforce mastery unlocks` |

## Required P4 Acceptance Standard

- Learner journey is durable across repository restart.
- `DemoLearnerSession` is not used by production learner endpoints.
- Frontend does not own placement, scoring, today assignment, review, or unlock logic.
- Every task is implemented with tests before commit and pushed separately.

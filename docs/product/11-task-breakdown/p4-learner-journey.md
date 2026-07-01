# P4 - Learner Journey Core

## Phase Goal

Replace demo learner flow with persisted production journey.

| Task | Purpose | Key Pass Criteria | Commit |
| --- | --- | --- | --- |
| P4.1 Learner onboarding | Capture learner goal | Profile persisted; next action returned | `feat(p4.1): implement learner onboarding` |
| P4.2 Learner profile persistence | Remove memory-only learner state | Profile survives restart | `feat(p4.2): persist learner profile state` |
| P4.3 Placement test start | Create placement session | Duplicate active session handled | `feat(p4.3): start TOEIC placement session` |
| P4.4 Placement scoring | Diagnose part weaknesses | Result has accuracy by part and tags | `feat(p4.4): score TOEIC placement` |
| P4.5 Learning path generation | Create path from diagnosis | Starting units generated from placement | `feat(p4.5): generate learner path from placement` |
| P4.6 Today Plan assignment | Assign next best activity | Review blockers outrank new lessons | `feat(p4.6): assign learner today plan` |
| P4.7 Activity session lifecycle | Track activity state | Start, resume, complete states persist | `feat(p4.7): manage learner activity sessions` |
| P4.8 Attempt submission | Score learner work | Attempt persists and returns result | `feat(p4.8): process learner attempts` |
| P4.9 Mistake review queue | Force repair | Wrong answer creates review item | `feat(p4.9): create learner review queue` |
| P4.10 Mastery unlock engine | Enforce progression | Unit unlocks only after all gates pass | `feat(p4.10): enforce mastery unlocks` |

## Required P4 Acceptance Standard

`DemoLearnerSession` is not used by production endpoints after this phase.


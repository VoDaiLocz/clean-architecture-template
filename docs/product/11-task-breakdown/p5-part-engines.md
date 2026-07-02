# P5 - TOEIC Part Engines

## Phase Goal

Implement correct domain behavior for each TOEIC Listening/Reading part using learner-safe payload contracts.

## Source Of Truth

Detailed implementation contracts live in the standalone task specs below. This phase file is an execution index only.

| Task | Detailed Spec | Purpose | Key Pass Criteria | Commit |
| --- | --- | --- | --- | --- |
| P5.1 | [53-define-toeic-item-contracts.md](../53-define-toeic-item-contracts.md) | Shared learner-safe item contracts | Play payload hides answers; review payload is authorized | `feat(p5.1): define TOEIC item contracts` |
| P5.2 | [54-implement-toeic-part-1-engine.md](../54-implement-toeic-part-1-engine.md) | Image/audio photograph questions | Image and audio required | `feat(p5.2): implement TOEIC Part 1 engine` |
| P5.3 | [55-implement-toeic-part-2-engine.md](../55-implement-toeic-part-2-engine.md) | Audio question-response | Audio required; spoken prompt hidden | `feat(p5.3): implement TOEIC Part 2 engine` |
| P5.4 | [56-implement-toeic-part-3-engine.md](../56-implement-toeic-part-3-engine.md) | Conversation groups | Grouped audio and child questions required | `feat(p5.4): implement TOEIC Part 3 engine` |
| P5.5 | [57-implement-toeic-part-4-engine.md](../57-implement-toeic-part-4-engine.md) | Talk groups | Talk audio and group relation required | `feat(p5.5): implement TOEIC Part 4 engine` |
| P5.6 | [58-implement-toeic-part-5-engine.md](../58-implement-toeic-part-5-engine.md) | Grammar/vocab sentence questions | One gap, four choices, explanation required | `feat(p5.6): implement TOEIC Part 5 engine` |
| P5.7 | [59-implement-toeic-part-6-engine.md](../59-implement-toeic-part-6-engine.md) | Passage completion | Passage and blank positions required | `feat(p5.7): implement TOEIC Part 6 engine` |
| P5.8 | [60-implement-toeic-part-7-engine.md](../60-implement-toeic-part-7-engine.md) | Reading comprehension | Passage and evidence span required | `feat(p5.8): implement TOEIC Part 7 engine` |
| P5.9 | [61-tag-toeic-learner-weaknesses.md](../61-tag-toeic-learner-weaknesses.md) | Cross-part analytics | Attempts produce stable weakness tags | `feat(p5.9): tag TOEIC learner weaknesses` |

## Required P5 Acceptance Standard

- Learner payloads never expose correct answers before submission.
- Part-specific media/passage/group requirements are enforced before publish and before learner payload creation.
- All part engines read published content from DB/read models, not raw PDFs or source links.

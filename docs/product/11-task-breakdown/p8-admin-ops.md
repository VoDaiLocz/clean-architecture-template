# P8 - Admin Content Operations

## Phase Goal

Give internal content operators a safe production workflow for source inventory, extraction, validation, review, publish, and coverage.

## Source Of Truth

Detailed implementation contracts live in the standalone task specs below. This phase file is an execution index only.

| Task | Detailed Spec | Purpose | Key Pass Criteria | Commit |
| --- | --- | --- | --- | --- |
| P8.1 | [80-build-admin-source-inventory.md](../80-build-admin-source-inventory.md) | Inspect source state | 73 sources and blocked items visible | `feat(p8.1): build admin source inventory` |
| P8.2 | [81-build-admin-asset-discovery.md](../81-build-admin-asset-discovery.md) | Inspect assets | Containers and assets visible | `feat(p8.2): build admin asset discovery` |
| P8.3 | [82-build-extraction-operations-dashboard.md](../82-build-extraction-operations-dashboard.md) | Operate jobs | Status, retry, failure reason visible | `feat(p8.3): build extraction operations dashboard` |
| P8.4 | [83-build-draft-review-queue.md](../83-build-draft-review-queue.md) | Human review | Approve/reject/relabel actions available | `feat(p8.4): build draft review queue` |
| P8.5 | [84-build-validation-issue-workflow.md](../84-build-validation-issue-workflow.md) | Resolve quality issues | Issues have status and required action | `feat(p8.5): build validation issue workflow` |
| P8.6 | [85-build-content-publish-queue.md](../85-build-content-publish-queue.md) | Control publish | Only approved drafts publish | `feat(p8.6): build content publish queue` |
| P8.7 | [86-build-content-coverage-dashboard.md](../86-build-content-coverage-dashboard.md) | Know content gaps | Coverage by part/media/status visible | `feat(p8.7): build content coverage dashboard` |

## Required P8 Acceptance Standard

- Admin UX shows normalized source/content state, not raw spreadsheet navigation.
- Publish and mutation actions are backend-authorized, audited, and validated.
- Operators can identify blockers and next actions without inspecting the database manually.

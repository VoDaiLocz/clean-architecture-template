# Build Draft Review Queue

## Purpose
P8.4 implements parser drafts evaluation dashboard.

## Rules
1. Side-by-side text parser compare, approve/reject decision button.

## Verification
```bash
npm --prefix frontend run test
rg -n "DraftReviewQueue" frontend/src
```

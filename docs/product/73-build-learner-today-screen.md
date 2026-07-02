# Build Learner Today Screen

## Purpose
P7.4 implements Today task dashboard view.

## Rules
1. Display primary blocker card at top; keep new units locked until blocker is resolved.

## Verification
```bash
npm --prefix frontend run test
rg -n "TodayScreen" frontend/src
```

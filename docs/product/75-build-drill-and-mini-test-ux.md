# Build Drill and Mini Test UX

## Purpose
P7.6 implements attempt answer sheet templates.

## Rules
1. Timer countdown is synchronized with server; display score only after completion.

## Verification
```bash
npm --prefix frontend run test
rg -n "DrillUX" frontend/src
```

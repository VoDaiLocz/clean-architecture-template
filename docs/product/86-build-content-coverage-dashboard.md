# Build Content Coverage Dashboard

## Purpose
P8.7 implements content gaps visual grid.

## Rules
1. Gaps metrics by TOEIC part/media requirement, checklist alerts.

## Verification
```bash
npm --prefix frontend run test
rg -n "ContentCoverageDashboard" frontend/src
```

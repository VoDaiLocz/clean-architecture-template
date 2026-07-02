# Build Mistake Repair UX

## Purpose
P7.7 implements review workspace interface.

## Rules
1. Focus explanations, highlight text passage evidence, play audio file.

## Verification
```bash
npm --prefix frontend run test
rg -n "MistakeRepairUX" frontend/src
```

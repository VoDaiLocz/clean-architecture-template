# Build TOEIC Practice Test UX

## Purpose
P7.9 implements dense practice test interface.

## Rules
1. Question list navigation panel, audio playback widget, flag status indicator.

## Verification
```bash
npm --prefix frontend run test
rg -n "PracticeTestUX" frontend/src
```

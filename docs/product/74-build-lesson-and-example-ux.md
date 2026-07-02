# Build Lesson and Example UX

## Purpose
P7.5 implements lesson reader and step-by-step example views.

## Rules
1. Slide-based example display with reveal-answer toggle.

## Verification
```bash
npm --prefix frontend run test
rg -n "LessonUX" frontend/src
```

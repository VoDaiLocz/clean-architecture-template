# UX Specification

## Purpose

This document defines the production UX contract for the TOEIC learner and admin web application.

The frontend stack is Angular with TypeScript. Angular is selected as the production frontend framework for route structure, form-heavy workflows, typed services, dependency injection, guards, interceptors, and scalable feature modules.

The visual design source of truth is [08a-angular-design-system.md](./08a-angular-design-system.md). Feature screens must follow that white and sea-blue `Ocean Classroom` system.

## UX Principles

- The learner always knows the next required action.
- The learner sees progress, blockers, and score meaning in plain language.
- The learner never sees raw source/import/admin terminology.
- The learner never studies by opening raw PDFs, Drive links, spreadsheets, or source files.
- The UI supports repeated daily study, not landing-page browsing.
- The product must feel like a serious learning cockpit: focused, dense enough for study, but not visually noisy.
- Desktop and mobile web must both be usable for core learner workflows.
- Backend decisions are displayed, not recreated, in the frontend.

## Angular Frontend Standard

### Application Structure

Required structure:

- `src/app/core`: auth, API client, interceptors, error handling, app shell services.
- `src/app/shared`: reusable UI primitives, pipes, directives, layout utilities.
- `src/app/features/learner`: Today, Learn, Practice, Review, Tests, Progress, Part Overview.
- `src/app/features/admin`: source inventory, assets, extraction, review, validation, publish, coverage.
- `src/app/features/auth`: login, registration, session restore, guards.

Rules:

1. Use standalone Angular components unless a module is justified by a large feature boundary.
2. Use Angular Router with route guards for learner/admin/auth access.
3. Use typed API services generated from or aligned with backend contracts.
4. Use interceptors for auth token, correlation id, and standard error handling.
5. Use reactive forms for onboarding, placement, login, admin filters, and review actions.
6. Use route-level lazy loading for major features.
7. Do not store source-of-truth learning state in client stores.

### UI System

The app must define a real design system before building feature screens:

- typography scale for app shell, page titles, section headings, dense tables, question text, answer choices, and captions
- spacing scale
- color tokens for primary action, success, warning, danger, locked, active, review, listening, reading
- button variants for primary, secondary, destructive, quiet, icon-only
- form controls and validation messages
- status badges
- progress indicators
- locked reason chips
- answer choice controls
- audio player controls
- passage reader layout
- data table controls
- modal/dialog patterns
- toast/error banner patterns

No feature screen passes if it creates one-off styling that cannot be reused.

### Visual Direction

The learner app should feel premium, modern, focused, and exam-oriented:

- clean information architecture
- strong hierarchy around the primary action
- clear route identity
- refined motion for route transitions, progress changes, answer selection, unlock states, and review completion
- restrained but polished visual assets, especially for listening/audio and progress states
- no generic hero marketing layout inside the app
- no placeholder cards that exist only to fill space

Animations are allowed only when they improve understanding or perceived responsiveness:

- route entrance
- primary action hover/press
- answer selection
- submit confirmation
- score/progress reveal
- unlock transition
- review resolved transition
- table row status update

Animations must not block exam timing, audio playback, keyboard navigation, or accessibility.

## Learner Information Architecture

Primary navigation:

- Today
- Learn
- Practice
- Review
- Tests
- Progress

Secondary navigation:

- 7 TOEIC parts
- current path
- current unit
- active test session
- profile/settings

Rules:

1. Today is the default authenticated learner landing route.
2. The 7-part overview is a structured roadmap, not a free-form content browser.
3. Practice routes must separate drills, mini tests, part tests, section tests, and full TOEIC LR tests.
4. Review is a mandatory workflow when blockers exist.
5. Progress explains improvement and weakness, not just chart decoration.

## Learner Screens

### Today Screen

Purpose: the learner's daily command center.

Must show:

- primary assignment
- current unit and TOEIC part
- why this task is next
- review blockers
- daily study target
- current streak or recent completion signal
- path progress
- weakest skill or part
- next unlock requirement

Primary action:

- continue current activity, start assigned activity, start placement, or repair blockers

Layout contract:

- desktop: left or top navigation, main work column, compact progress/blocker side panel
- mobile: primary action first, blockers second, progress collapsed into compact sections

States:

- no profile: onboarding CTA
- profile but no placement: placement CTA
- placement completed but no path: generate path CTA
- blocking review exists: repair CTA
- active assignment exists: resume CTA
- no published content: content unavailable state with no fake questions

### Onboarding And Placement

Purpose: collect learner goal and run diagnostic placement.

Must support:

- target TOEIC score
- current estimate
- daily study minutes
- timezone
- study goal context
- placement start/resume
- question navigation
- explicit skip
- submit confirmation
- diagnostic result band
- weakness summary

Rules:

1. Placement cannot expose correct answers.
2. Placement result must say diagnostic estimate, not official TOEIC score.
3. The UI follows backend `NextAction`.
4. Refresh during placement must resume active session.

### Lesson And Guided Example

Purpose: teach before practice.

Must show:

- learning objective
- TOEIC part context
- concept explanation
- guided example
- why the correct answer is correct
- common trap
- next practice action

Interaction:

- reveal example answer only when the learner asks
- mark lesson complete through backend
- keep passage/audio context attached when relevant

### Drill And Mini Test

Purpose: practice and verify unit mastery.

Must support:

- answer selection
- explicit skip
- progress through items
- audio player for listening parts
- passage reader for reading parts
- submit confirmation
- server result
- no pre-submit answer leak
- retry or review path based on backend response

Mini test rules:

1. Mini tests are unit-scoped.
2. Passing threshold and unlock state come from backend.
3. Timer authority, if present, is backend-owned.

### Mistake Repair

Purpose: force useful correction of wrong answers.

Must show:

- original question context
- learner answer
- correct answer
- explanation
- transcript, image, or passage evidence when relevant
- skill tag
- why it blocks progress
- repair task

Resolution rule:

- the UI cannot mark a blocker resolved without backend repair result.

### Seven Part Overview

Purpose: show real progress through all TOEIC parts.

Must show for each part:

- part name and TOEIC skill context
- progress percentage
- locked/unlocked state
- lock reason
- active unit
- weakness tags
- available practice tests
- recommended next action

Must not show:

- empty placeholder part cards
- generic lorem text
- raw source-file links
- fake progress

### Practice Test UX

Purpose: simulate TOEIC-like practice without compromising reliability.

Must support:

- timed test shell
- section navigation
- question palette
- unanswered indicator
- flagged question indicator
- audio controls
- passage panes
- submit confirmation
- expired session handling
- result breakdown

Exam-mode rules:

1. No hints during exam mode.
2. No explanation before submit.
3. Backend owns timer and final submit.
4. Refresh must restore session state.

### Progress UX

Purpose: make improvement and next work visible.

Must show:

- target score
- diagnostic bands over time
- part accuracy trend
- weakness tags
- review completion
- test history
- unlocked units
- upcoming blockers

Charts must be explainable; every chart needs a clear user action or interpretation.

## Admin Screens

Admin screens are separate from learner screens and use an operational layout.

Required screens:

- source inventory
- asset discovery
- extraction status
- draft review queue
- validation issue detail
- publish queue
- content coverage

Admin UX rules:

1. Admin tables must support filtering, search, status badges, and clear empty/error states.
2. Publish/retry/reject actions require confirmation and backend audit.
3. Admin UI may show source identifiers; learner UI may not.
4. Coverage screens must reveal missing media, missing answer keys, blocked sources, and publish gaps.

## Accessibility And Responsiveness

Required:

- keyboard navigation for all forms, answer choices, dialogs, tabs, audio controls, and tables
- visible focus states
- semantic headings
- labels for all form controls
- sufficient color contrast
- no text overflow in buttons/cards/tables
- mobile layout for every learner critical path
- desktop layout for dense study and admin operations

## Quality Gates

A frontend task is not complete unless:

- Angular build passes
- unit/component tests cover loading, error, empty, and success states
- Playwright covers the core user path
- desktop and mobile viewport smoke checks pass
- no hardcoded learner content exists
- no raw source/PDF/Drive flow is used as learner study experience
- no answer key appears before submit/review authorization
- visual layout has no obvious overlap, clipped text, or placeholder card

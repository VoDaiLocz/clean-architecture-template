# Angular Design System

## Purpose

This document defines the visual design system for the TOEIC production web app. It is the source of truth for Angular UI implementation, design review, visual QA, and Playwright screenshot checks.

The selected direction is based on Refero-style light product systems: education workspace clarity, clean white surfaces, sea-blue brand color, structured learning cards, and polished motion. The product must not look like a generic AI-generated SaaS template.

## Selected Direction

Name: `Ocean Classroom`

Primary colors:

- white
- sea blue
- cool blue-gray

Tone:

- clean
- premium
- calm
- focused
- trustworthy
- exam-oriented
- modern education, not childish gamification

Design references to follow:

- Google for Education style: friendly white/blue education clarity.
- Relate style: clean modern SaaS layout, spacious light panels.
- Seline Analytics style: crisp dashboard density and sharp chart surfaces.

Design references to avoid:

- dark command-center styles as the main learner theme
- purple gradient SaaS templates
- playful cartoon learning apps
- generic rounded card grids with no hierarchy
- raw admin dashboard visuals inside learner screens

## Product Surface Model

### Ocean Canvas

Used for:

- authenticated learner app
- Today
- 7-part overview
- progress
- practice dashboard

Visual rules:

- page background is near-white with a faint blue tint
- major panels are white
- sea-blue is used for route identity and primary action
- borders are light and crisp
- elevation is subtle, never heavy

### Study Paper

Used for:

- lessons
- passages
- transcripts
- explanations
- review evidence

Visual rules:

- white or warm-white panel
- readable long text
- max text width `65-80ch`
- passage evidence highlight uses pale cyan, not yellow marker overload
- no decorative imagery behind reading text

### Exam Deck

Used for:

- placement
- drills
- mini tests
- part tests
- full tests

Visual rules:

- timer, question palette, and submit state stay stable
- answer choices are large and calm
- selected state is sea-blue outline/fill
- correct/incorrect states appear only after authorized submit/review
- no animation competes with exam timing

### Admin Operations

Used for:

- source inventory
- extraction jobs
- validation issues
- draft review
- publish queue
- coverage

Visual rules:

- dense white tables
- blue-gray filters and status badges
- strong row scanning
- detail drawer for actions
- no learner illustration or motivational copy

## Color Tokens

### Foundation

| Token | Value | Role |
| --- | --- | --- |
| `--toeic-canvas` | `#f4f9fc` | app background with sea tint |
| `--toeic-canvas-strong` | `#eaf5fb` | route bands and empty states |
| `--toeic-surface` | `#ffffff` | primary panels/cards |
| `--toeic-surface-raised` | `#ffffff` | focused panel |
| `--toeic-surface-soft` | `#f8fbfd` | table headers, subtle sections |
| `--toeic-border` | `#d9e7ef` | default border |
| `--toeic-border-strong` | `#b9d4e3` | active panel border |
| `--toeic-text` | `#10222f` | primary text |
| `--toeic-text-muted` | `#526879` | secondary text |
| `--toeic-text-faint` | `#7c91a1` | metadata |

### Sea Blue Brand

| Token | Value | Role |
| --- | --- | --- |
| `--toeic-primary` | `#0787c8` | primary action and brand |
| `--toeic-primary-hover` | `#006fa8` | primary hover |
| `--toeic-primary-soft` | `#dff3fb` | selected/active pale background |
| `--toeic-primary-ring` | `rgba(7, 135, 200, 0.22)` | focus ring |
| `--toeic-primary-ink` | `#ffffff` | text on primary |
| `--toeic-navy` | `#0b3551` | headings, nav active text |
| `--toeic-aqua` | `#18bfd3` | listening/audio identity |

### Learning Status

| Token | Value | Role |
| --- | --- | --- |
| `--toeic-reading` | `#2f8fdd` | reading part marker |
| `--toeic-listening` | `#10b9ca` | listening part marker |
| `--toeic-success` | `#178a5b` | complete/pass/resolved |
| `--toeic-success-soft` | `#dcf7ea` | success background |
| `--toeic-warning` | `#b7791f` | weak/pending |
| `--toeic-warning-soft` | `#fff2cc` | warning background |
| `--toeic-danger` | `#d64545` | failed/blocking/destructive |
| `--toeic-danger-soft` | `#ffe3e3` | error background |
| `--toeic-locked` | `#8ba0ad` | locked state |
| `--toeic-locked-soft` | `#edf3f6` | locked background |

Color rules:

1. White and sea-blue must dominate.
2. No purple/blue gradient theme.
3. Only one filled primary action appears in the main work region.
4. Listening and Reading use different markers, but the whole system remains sea-blue-first.
5. Status colors are functional only; they cannot become the page palette.

## Typography

### Fonts

Primary UI:

- preferred: `Geist`
- fallback: `Inter`, `system-ui`, `sans-serif`

Reading surface:

- preferred: `Source Serif 4`
- fallback: `Georgia`, serif

Admin metadata:

- preferred: `JetBrains Mono`
- fallback: `IBM Plex Mono`, monospace

Rules:

1. Page titles use confident medium weight, not oversized marketing hero type.
2. Study text uses generous line height.
3. Admin tables use compact UI text.
4. Button text must never wrap awkwardly or overflow.
5. Letter spacing is `0` by default; uppercase labels may use `0.04em`.

### Type Scale

| Token | Size | Weight | Line Height | Role |
| --- | --- | --- | --- | --- |
| `--text-display` | `48px` | `560` | `1.05` | rare score/result reveal |
| `--text-page` | `32px` | `560` | `1.15` | page title |
| `--text-section` | `22px` | `560` | `1.25` | section heading |
| `--text-card` | `18px` | `560` | `1.35` | card title |
| `--text-body` | `16px` | `400` | `1.55` | standard UI text |
| `--text-reading` | `18px` | `400` | `1.75` | passage/lesson body |
| `--text-label` | `13px` | `560` | `1.3` | field labels, tabs |
| `--text-micro` | `12px` | `520` | `1.25` | metadata |

## Spacing And Layout

Base unit: `4px`

| Token | Value | Role |
| --- | --- | --- |
| `--space-1` | `4px` | icon/text gap |
| `--space-2` | `8px` | compact control gap |
| `--space-3` | `12px` | component gap |
| `--space-4` | `16px` | default gap |
| `--space-5` | `20px` | dense panel padding |
| `--space-6` | `24px` | standard panel padding |
| `--space-8` | `32px` | major section gap |
| `--space-10` | `40px` | large screen gap |
| `--space-12` | `48px` | page rhythm |

Layout rules:

1. Learner pages use a white shell on `--toeic-canvas`.
2. Main content max width is `1280px`.
3. Study reading width is `65-80ch`.
4. Exam screens can use full width but must keep timer, palette, and submit controls stable.
5. Cards cannot be nested inside other cards.
6. Major layouts use CSS grid with named regions.
7. Mobile first viewport must show the primary action or required blocker.

## Shape, Border, Elevation

| Element | Radius |
| --- | --- |
| app shell panels | `14px` |
| study cards | `12px` |
| reading panels | `10px` |
| buttons | `8px` |
| inputs | `8px` |
| answer choices | `12px` |
| status badges | `999px` |
| admin tables | `10px` |

Elevation rules:

- default border: `1px solid var(--toeic-border)`
- active border: `1px solid var(--toeic-primary)`
- default shadow: `0 10px 30px rgba(11, 53, 81, 0.06)`
- focused panel shadow: `0 18px 50px rgba(7, 135, 200, 0.12)`
- avoid heavy black shadows
- avoid glassmorphism blur as a core surface

## Motion System

Motion must feel like water: smooth, quick, and controlled.

Durations:

| Token | Value | Role |
| --- | --- | --- |
| `--motion-fast` | `120ms` | hover/focus |
| `--motion-medium` | `220ms` | answer select, panel enter |
| `--motion-slow` | `420ms` | score/progress/unlock reveal |

Easing:

- default: `cubic-bezier(0.2, 0.8, 0.2, 1)`
- exit: `cubic-bezier(0.4, 0, 1, 1)`

Allowed motion:

- soft route panel fade/slide
- sea-blue ripple on primary action press
- answer selection outline/fill transition
- progress ring count-up
- unlock reveal
- review item resolved collapse
- admin row status update

Forbidden motion:

- decorative looping animation during tests
- moving passage/question text while reading
- layout shifts after answer selection
- animation that hides timer, audio, or submit controls

## Core Components

### App Shell

Required:

- white top bar or side rail
- sea-blue active route marker
- route title
- contextual action slot
- user/profile menu
- mobile drawer or bottom navigation
- global error banner

### Primary Button

Rules:

- background `--toeic-primary`
- text `--toeic-primary-ink`
- height at least `40px`
- one filled primary action per main region
- disabled state must show reason nearby or in tooltip

### TOEIC Part Card

Required fields:

- part number
- Listening or Reading marker
- current status
- progress
- lock reason or next action
- weakness tags
- available tests

States:

- locked
- unlocked
- in progress
- completed
- needs repair
- content unavailable

### Activity Card

Required fields:

- activity type
- TOEIC part
- unit
- objective
- estimated duration
- blocker/reason
- primary action

### Answer Choice

Required:

- stable height
- large hit target
- keyboard focus
- selected state
- skipped state
- correct/incorrect state only after submit/review authorization
- no layout shift after state change

### Audio Player

Required:

- custom sea-blue control surface
- play/pause
- replay where allowed
- progress bar
- duration
- loading state
- error state
- transcript availability indicator only when authorized

Native browser controls alone do not pass production UX.

### Passage Reader

Required:

- white/warm-white surface
- readable line length
- evidence highlight in pale cyan
- sticky question context on desktop when needed
- mobile tabs or accordion between passage and questions

### Review Evidence Panel

Required:

- original learner answer
- correct answer
- explanation
- evidence
- repair action
- blocker reason

### Progress Visualization

Required:

- each chart answers a learner question
- no decorative chart without next action
- mobile-readable legend
- distinguish diagnostic estimate, practice score, and mastery completion

### Admin Data Table

Required:

- search
- filters
- sort
- pagination or virtualization
- row status badge
- empty state
- error state
- detail drawer
- mutation confirmation

## Screen Composition Rules

### Today

Composition:

1. route title and study state
2. dominant primary assignment module
3. blocker module
4. progress/weakness module
5. next unlock context

The primary assignment must be visually dominant. Progress widgets cannot compete with the main CTA.

### Lesson

Composition:

1. command header with unit and objective
2. reading panel
3. guided example module
4. trap/exam note
5. next action footer

### Drill And Mini Test

Composition:

1. compact session header
2. question/media/passage region
3. answer region
4. progress/palette region
5. submit/next action region

### Practice Test

Composition:

1. fixed exam header with timer
2. question palette
3. content region
4. answer sheet
5. submit confirmation

### Review

Composition:

1. review queue
2. selected mistake detail
3. evidence panel
4. repair action
5. resolution feedback

### Admin

Composition:

1. operational page title
2. filters/search
3. table or queue
4. detail drawer
5. action bar with audit-aware confirmation

## Visual QA Gates

A frontend task fails if any of these are true:

1. Screen looks like a generic SaaS template.
2. White/sea-blue brand direction is not visible.
3. More than one filled primary CTA appears in the main work region.
4. Learner route shows raw source/admin/import terminology.
5. Placeholder cards exist where backend state should be displayed.
6. Text overflows buttons/cards/table cells.
7. Mobile first viewport does not show the primary action or required blocker.
8. Exam screen has layout shift after answer selection.
9. Audio player uses unstyled browser-default controls only.
10. Reading passage exceeds readable width.
11. Any answer key appears before submit or authorized review.

## Implementation Gates

Required implementation artifacts:

- `src/styles/tokens.css` or Angular-equivalent token file
- shared Angular primitives for button, badge, panel, answer choice, audio player, passage reader, empty state, error state, skeleton, data table
- Playwright screenshots for desktop and mobile critical routes
- visual regression checklist in PR notes until automated visual regression exists

Required screenshot routes:

- Today
- Onboarding/placement
- Lesson
- Drill or mini test
- Mistake repair
- 7-part overview
- Practice test
- Progress
- Admin source inventory
- Admin draft review

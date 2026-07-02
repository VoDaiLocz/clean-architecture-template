# P3 - Content Factory Pipeline

## Phase Goal

Transform audited sources into reviewed published content.

| Task | Purpose | Key Pass Criteria | Commit |
| --- | --- | --- | --- |
| P3.1 Import 73 source manifest rows | Persist audited inventory | 73 rows, 13 blocked, evidence counts match audit | `feat(p3.1): import TOEIC source manifest` |
| P3.2 Discover Drive folders/files | Expand containers | Folder children become assets; blocked source creates issue | `feat(p3.2): discover Drive source assets` |
| P3.3 Resolve shortlinks/external sources | Normalize external references | Original and resolved URLs stored | `feat(p3.3): resolve TOEIC external sources` |
| P3.4 Register PDF/audio/image assets | Classify asset roles | PDF/audio/image roles detected | `feat(p3.4): register TOEIC source assets` |
| P3.5 Extract PDF pages/text blocks | Make PDFs machine readable | Page blocks created; confidence stored | `feat(p3.5): extract TOEIC PDF blocks` |
| P3.6 Extract audio metadata | Support listening content | Duration and format stored | `feat(p3.6): extract TOEIC audio metadata` |
| P3.7 Parse answer keys | Map correct answers | Answer key draft records created | `feat(p3.7): parse TOEIC answer keys` |
| P3.8 Parse transcripts | Support listening review | Transcript blocks linked to source | `feat(p3.8): parse TOEIC transcripts` |
| P3.9 Parse Part 5/7 drafts | Produce reading draft content | Draft questions have tags and source trace | `feat(p3.9): parse TOEIC reading drafts` |
| P3.10 Parse listening groups | Produce Part 1-4 draft groups | Group relationships created | `feat(p3.10): parse TOEIC listening groups` |
| P3.11 Validate draft content | Block bad content | Part-specific validation issues created | `feat(p3.11): validate TOEIC draft content` |
| P3.12 Review and publish workflow | Human gate before learner visibility | Approved draft publishes; rejected draft hidden | `feat(p3.12): publish reviewed TOEIC content` |

## Required P3 Acceptance Standard

No content reaches learner APIs unless it is published and passes validation.

## P3.1 - Import TOEIC Source Manifest

**Context:** Content Factory  
**Purpose:** Import the audited TOEIC source manifest into normalized source inventory tables.  
**User/Business Value:** Starts the content factory from the real audited TOEIC material inventory instead of raw PDF/Drive navigation or frontend hardcoded data.  
**Dependencies:** P2.1, P2.10.  
**Detailed Scope:** Import 73 audited source rows; classify provider/source type/material class/access status/evidence flags; return audit summary counts; make repeated imports idempotent.  
**Out Of Scope:** Drive folder child discovery, shortlink resolution, asset registration, PDF/audio extraction, parser jobs, learner-facing content publication.  
**Data Contract:** `source_manifest_entries` receives stable `sheet-row-{row}` ids with title, url, provider, source type, material class, access status, evidence flags, and audit notes.  
**API Contract:** `ImportToeicSourceManifestResult` returns imported, accessible, blocked, PDF, audio, image, transcript, and answer-key counts.  
**UI Contract:** none for P3.1. Admin dashboards may display source manifest summary after import.  
**Business Rules:** Import count must be 73; blocked count must be 13; accessible count must be 60; evidence counts must match audit; rerunning import must update rows without duplicating them.  
**Edge Cases:** inaccessible rows remain explicit blocked inventory; Speaking/Writing rows remain classified but do not enter learner scope until later; repeated imports are idempotent by source id.  
**Required Tests:** Application test covers 73 imported rows, 60 accessible, 13 blocked, evidence counts, provider/source summary, and idempotent rerun.  
**Acceptance Criteria:** Handler persists audited rows; summary counts match audit; tests and build pass; import doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "ImportToeicSourceManifestResult|AccessibleCount|SourcesWithImage|P3.1" backend/src backend/tests docs/product`.  
**Definition Of Done:** TOEIC source manifest import is committed and pushed.  
**Commit:** `feat(p3.1): import TOEIC source manifest`  
**Push:** `git push origin main`

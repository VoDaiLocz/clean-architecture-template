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


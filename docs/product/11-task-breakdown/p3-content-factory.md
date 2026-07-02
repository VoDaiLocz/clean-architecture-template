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

## P3.2 - Discover Drive Source Assets

**Context:** Content Factory  
**Purpose:** Expand accessible Google Drive source folders into source containers and concrete source assets, while recording blocked source discovery issues.  
**User/Business Value:** Converts the audited source manifest into machine-processable asset inventory for later PDF/audio/image extraction, without exposing Drive folders to learners.  
**Dependencies:** P2.1, P3.1.  
**Detailed Scope:** Add Drive discovery gateway contract; add Drive source discovery handler; persist source containers and source assets for accessible Drive folders; detect PDF/audio/image roles; persist source discovery issues for blocked Drive folders; add PostgreSQL migration `012_source_discovery_issues`; add tests and docs.  
**Out Of Scope:** real Google Drive API adapter, OAuth/auth refresh, recursive folder traversal, object storage upload, extraction jobs, shortlink resolution.  
**Data Contract:** Accessible Drive folder rows create `source_containers` and `source_assets`; blocked Drive folder rows create `source_discovery_issues` with stable source id and issue code.  
**API Contract:** none for P3.2. Future admin API may trigger the handler with a real Drive adapter.  
**UI Contract:** none for P3.2. Admin UI may later display discovery issues.  
**Business Rules:** Only Google Drive folders are discovered in P3.2; blocked folders are not sent to the gateway; discovered assets store metadata/object keys only; repeated issue upsert is idempotent.  
**Edge Cases:** blocked Drive folder creates issue instead of failing whole discovery; asset role is inferred from file metadata; fake gateway tests keep Google auth out of unit tests.  
**Required Tests:** Application test covers accessible folder discovery into container/assets, blocked source issue, role detection, and counts; migration test covers `012_source_discovery_issues`.  
**Acceptance Criteria:** Handler exists; gateway contract exists; source discovery issue model/table exists; tests and build pass; Drive discovery doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "DiscoverDriveSourceAssets|SourceDiscoveryIssue|012_source_discovery_issues" backend/src backend/tests docs/product`.  
**Definition Of Done:** Drive source asset discovery is committed and pushed.  
**Commit:** `feat(p3.2): discover Drive source assets`  
**Push:** `git push origin main`

## P3.3 - Resolve TOEIC External Sources

**Context:** Content Factory  
**Purpose:** Resolve shortlinks, external web sources, and SharePoint URLs into stable source resolution records.  
**User/Business Value:** Gives the content factory durable original/final URL evidence before asset registration and prevents fragile shortlinks from being the only persisted address.  
**Dependencies:** P3.1.  
**Detailed Scope:** Add external source resolver contract; add resolution handler; persist source resolution records with original URL, resolved URL, HTTP status, redirect count, status, and timestamp; add PostgreSQL migration `013_source_resolution_records`; add tests and docs.  
**Out Of Scope:** real HTTP client implementation, retry/backoff policy, page scraping, asset registration, auth-gated SharePoint login handling.  
**Data Contract:** `source_resolution_records` belongs to `source_manifest_entries` and stores original URL, resolved URL, status code, redirect count, resolution status, and timestamp.  
**API Contract:** none for P3.3. Future admin API may trigger resolver with a real HTTP adapter.  
**UI Contract:** none for P3.3. Admin UI may later display failed resolution records.  
**Business Rules:** Only accessible shortlink/external/SharePoint sources are resolved; 2xx/3xx resolver statuses are marked resolved; failed statuses remain durable resolution records; original URL is always preserved.  
**Edge Cases:** shortlinks with redirects persist redirect count; external web URLs persist final canonical URL; blocked sources are skipped here because P3.2 already records blocked discovery issues.  
**Required Tests:** Application test covers shortlink and external source resolution, original/final URL persistence, status, counts, and migration coverage for `013_source_resolution_records`.  
**Acceptance Criteria:** Handler exists; resolver contract exists; source resolution model/table exists; tests and build pass; source resolution doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "ResolveToeicExternalSources|SourceResolutionRecord|013_source_resolution_records" backend/src backend/tests docs/product`.  
**Definition Of Done:** TOEIC external source resolution is committed and pushed.  
**Commit:** `feat(p3.3): resolve TOEIC external sources`  
**Push:** `git push origin main`

## P3.4 - Register TOEIC Source Assets

**Context:** Content Factory  
**Purpose:** Register PDF, audio, and image asset placeholders from audited source evidence flags.  
**User/Business Value:** Prepares source materials for extraction jobs by creating explicit DB asset rows with stable roles, without exposing raw files to learners or requiring downloads in this task.  
**Dependencies:** P2.1, P3.1.  
**Detailed Scope:** Add source asset registration handler; create source container rows for accessible evidence sources; create source asset rows for PDF/audio/image evidence; skip blocked sources; add tests and docs.  
**Out Of Scope:** file download, checksum calculation from bytes, object storage upload, transcript/answer-key registration, PDF/audio parsing.  
**Data Contract:** `source_containers` stores a registration container per source; `source_assets` stores one row per evidence role with role, mime type, extension, provider URL, object key, and pending checksum.  
**API Contract:** none for P3.4. Future admin API may trigger registration after source manifest import/resolution.  
**UI Contract:** none for P3.4. Admin UI may later show registered assets.  
**Business Rules:** Only accessible sources create containers/assets; blocked sources are counted and skipped; PDF/audio/image evidence creates explicit roles; missing evidence must not create fake asset rows; upserts are idempotent.  
**Edge Cases:** one source can register multiple assets; zero-byte/pending checksum is allowed before object storage upload; role-specific extension and MIME type are assigned deterministically.  
**Required Tests:** Application test covers PDF/image asset registration, blocked source skip, missing audio not registered, and role persistence.  
**Acceptance Criteria:** Handler exists; source asset roles persist; tests and build pass; asset registration doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "RegisterToeicSourceAssets|RegisteredAssetCount|source asset registration" backend/src backend/tests docs/product`.  
**Definition Of Done:** TOEIC source asset registration is committed and pushed.  
**Commit:** `feat(p3.4): register TOEIC source assets`  
**Push:** `git push origin main`

## P3.5 - Extract TOEIC PDF Blocks

**Context:** Content Factory  
**Purpose:** Extract PDF pages and text blocks into durable extracted content tables.  
**User/Business Value:** Makes TOEIC books machine-readable for later parser/validation/review workflows without re-opening PDFs during learner flows.  
**Dependencies:** P2.2, P3.4.  
**Detailed Scope:** Add PDF text block extractor contract; add extraction handler for PDF source assets; persist extracted pages and blocks with confidence and coordinates; add tests and docs.  
**Out Of Scope:** real PDF parser implementation, OCR, table reconstruction, image extraction, draft content parsing, learner APIs.  
**Data Contract:** `extracted_pages` belongs to a PDF source asset; `extracted_text_blocks` belongs to both source asset and page and stores block type, text, confidence, and coordinates JSON.  
**API Contract:** none for P3.5. Future admin jobs may call the handler with a real PDF extractor adapter.  
**UI Contract:** none for P3.5.  
**Business Rules:** Only PDF source assets can use this handler; extractor confidence must persist; repeated extraction upserts stable page/block ids; extracted content is not learner-visible published content.  
**Edge Cases:** non-PDF asset is rejected; multi-block pages preserve page relationship and block confidence; fixture extractor keeps tests deterministic.  
**Required Tests:** Application test covers fixture PDF page/block extraction, persistence, block type, text, and confidence.  
**Acceptance Criteria:** Handler exists; extractor contract exists; pages/blocks persist; tests and build pass; PDF extraction doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "ExtractToeicPdfBlocks|PdfExtractedPageResult|pdf block extraction" backend/src backend/tests docs/product`.  
**Definition Of Done:** TOEIC PDF block extraction is committed and pushed.  
**Commit:** `feat(p3.5): extract TOEIC PDF blocks`  
**Push:** `git push origin main`

## P3.6 - Extract TOEIC Audio Metadata

**Context:** Content Factory  
**Purpose:** Extract audio duration and technical metadata for listening assets.  
**User/Business Value:** Enables Part 1-4 validation, timing, transcript alignment, and listening test preparation without relying on raw audio inspection in learner flows.  
**Dependencies:** P3.4.  
**Detailed Scope:** Add audio metadata domain record; add repository table/methods; add audio probe contract; add extraction handler for audio source assets; add PostgreSQL migration `014_source_audio_metadata`; add tests and docs.  
**Out Of Scope:** waveform processing, speech-to-text, transcript parsing, audio download/upload, playback API.  
**Data Contract:** `source_audio_metadata` belongs to a source asset and stores duration seconds, format, sample rate, bitrate, and extraction timestamp.  
**API Contract:** none for P3.6. Future admin jobs may call the handler with a real media probe adapter.  
**UI Contract:** none for P3.6.  
**Business Rules:** Only audio source assets can be probed; duration/sample rate/bitrate must be positive; metadata is queryable by asset; upsert is idempotent.  
**Edge Cases:** non-audio asset is rejected; missing audio metadata blocks later listening publication; fake probe keeps tests deterministic.  
**Required Tests:** Application test covers audio metadata extraction and persistence; migration test covers `014_source_audio_metadata`.  
**Acceptance Criteria:** Handler exists; probe contract exists; audio metadata persists; tests and build pass; audio metadata doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "ExtractToeicAudioMetadata|SourceAudioMetadata|014_source_audio_metadata" backend/src backend/tests docs/product`.  
**Definition Of Done:** TOEIC audio metadata extraction is committed and pushed.  
**Commit:** `feat(p3.6): extract TOEIC audio metadata`  
**Push:** `git push origin main`

## P3.7 - Parse TOEIC Answer Keys

**Context:** Content Factory  
**Purpose:** Parse answer key assets into draft answer mapping records.  
**User/Business Value:** Creates durable answer evidence for later scoring and validation without directly publishing parser output to learners.  
**Dependencies:** P2.3, P3.4.  
**Detailed Scope:** Add answer key parser contract; add answer key parsing handler; persist one `DraftContentItem` per answer mapping with payload, source trace, confidence, and pending validation status; add tests and docs.  
**Out Of Scope:** real answer key OCR/parser implementation, question linking, scoring service, validation/publish workflow.  
**Data Contract:** Answer mappings are stored as draft content with item type `AnswerKeyMapping`, JSON payload containing test id/question number/correct answer, source trace JSON, parser confidence, and draft status.  
**API Contract:** none for P3.7.  
**UI Contract:** none for P3.7.  
**Business Rules:** Only `AnswerKey` source assets can be parsed; parser confidence must persist; draft mappings remain non-learner-visible until validation and publishing.  
**Edge Cases:** multiple mappings can come from one asset; stable draft ids make repeated parser runs idempotent; non-answer-key asset is rejected.  
**Required Tests:** Application test covers fixture answer key mappings, draft persistence, payload, item type, and confidence.  
**Acceptance Criteria:** Handler exists; parser contract exists; draft mappings persist; tests and build pass; answer-key parsing doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "ParseToeicAnswerKeys|AnswerKeyMapping|answer key parsing" backend/src backend/tests docs/product`.  
**Definition Of Done:** TOEIC answer key parsing is committed and pushed.  
**Commit:** `feat(p3.7): parse TOEIC answer keys`  
**Push:** `git push origin main`

## P3.8 - Parse TOEIC Transcripts

**Context:** Content Factory  
**Purpose:** Parse transcript assets into draft transcript segments linked to listening audio/test groups.  
**User/Business Value:** Enables future listening review, explanation, and validation workflows to use transcript evidence instead of raw transcript files.  
**Dependencies:** P2.3, P3.6.  
**Detailed Scope:** Add transcript parser contract; add transcript parsing handler; persist transcript segment draft content with linked audio asset id, group id, speaker, text, timing, source trace, and confidence; add tests and docs.  
**Out Of Scope:** real speech-to-text, forced alignment, transcript editor UI, listening item publishing.  
**Data Contract:** Transcript segments are stored as draft content with item type `TranscriptSegment`, payload JSON, source trace JSON, parser confidence, and pending validation status.  
**API Contract:** none for P3.8.  
**UI Contract:** none for P3.8.  
**Business Rules:** Only `Transcript` source assets can be parsed; transcript segments remain draft content; each segment must preserve link to audio/test group evidence.  
**Edge Cases:** multiple segments can come from one transcript asset; confidence persists for validation; non-transcript asset is rejected.  
**Required Tests:** Application test covers fixture transcript parsing, draft persistence, group/audio links, item type, and confidence.  
**Acceptance Criteria:** Handler exists; parser contract exists; transcript draft segments persist; tests and build pass; transcript parsing doc exists.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "ParseToeicTranscripts|TranscriptSegment|transcript parsing" backend/src backend/tests docs/product`.  
**Definition Of Done:** TOEIC transcript parsing is committed and pushed.  
**Commit:** `feat(p3.8): parse TOEIC transcripts`  
**Push:** `git push origin main`

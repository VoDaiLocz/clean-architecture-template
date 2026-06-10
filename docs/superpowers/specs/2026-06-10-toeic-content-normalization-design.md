# TOEIC Content Normalization Design

## Purpose

Convert the user's TOEIC source collection into validated database content for a production learning system. The learner must study structured lessons, drills, tests, transcripts, explanations, and review items inside the app. The learner must not click raw PDFs, Google Drive folders, SharePoint files, shortlinks, or source spreadsheets as the primary learning experience.

This design covers the content pipeline only. It does not redesign the learner UI, payment, authentication, or admin styling.

## Source Audit Snapshot

The Google Sheet is a source manifest, not the final corpus.

- Sheet title: `Tài liệu TOEIC.xlsx`
- Sheet structure: `STT`, `NỘI DUNG`, `LINK BÀI HỌC`
- Material rows: 73
- Drive files: 14
- Drive folders: 36
- Google Sheet links: 1
- Google Doc links: 1
- SharePoint links: 4
- Shortlinks: 4
- Other web links: 13
- Inaccessible sources during audit: 13
- Sources with visible PDF evidence: 33
- Sources with visible audio/listening evidence: 20
- Sources with visible transcript evidence: 6
- Sources with visible answer key evidence: 5
- Sources with visible image evidence: 11

The number `2701` shown by Google Sheets is the sheet row count, not the number of learning materials.

## Material Classes

Every source must be classified before extraction. A source can belong to multiple classes, but one primary class controls parsing.

### Test Book

Examples:

- SPARTA TOEIC
- TOEIC Preparation 2019
- Economy TOEIC 1-5
- ABC TOEIC
- Tactics for TOEIC
- Very Easy TOEIC
- Target TOEIC
- Developing Skill TOEIC

Expected outputs:

- `TestBook`
- `TestSet`
- `ToeicPart`
- `QuestionGroup`
- `QuestionItem`
- `AnswerKey`
- `Transcript`
- `AudioAsset`
- `ImageAsset`
- `Passage`

### Skill Book

Examples:

- Taking the TOEIC - Skills and Strategies
- Tactics for TOEIC book sections
- Học theo Part 5,6,7 có giải thích
- Cẩm nang giải TOEIC Part 7

Expected outputs:

- `ConceptLesson`
- `StrategyLesson`
- `GuidedExample`
- `SkillTag`
- `TrapTag`
- `RecommendedDrill`

### Vocabulary Source

Examples:

- 1500 từ vựng TOEIC thường gặp
- 300 từ vựng TOEIC cho người mất gốc
- 600 Essential Words for the TOEIC
- Web học 600 từ vựng có hình ảnh

Expected outputs:

- `VocabularyEntry`
- `Definition`
- `ExampleSentence`
- `PartTag`
- `FrequencyTag`
- `ImageCue` when available
- `AudioCue` when available

### Roadmap and Strategy Source

Examples:

- 30 ngày tự ôn
- Lộ trình TOEIC 700+ từ A-Z
- 8 tuần đạt target 750+
- Hệ thống mẹo trong bài thi
- Kế hoạch ôn thi TOEIC 30 ngày

Expected outputs:

- `LearningPlanTemplate`
- `Stage`
- `Milestone`
- `DiagnosticRule`
- `StudyRecommendation`

These sources must influence business logic and learning order. They must not be displayed as long roadmap text to learners.

### Speaking and Writing Source

Examples:

- Tài liệu TOEIC SW
- Collins SW
- Tomato SW
- SW test IIG
- Đề Writing
- Kênh YouTube giải đề SW
- 100 đề luyện Speaking

Expected outputs:

- Deferred into a separate `ToeicSpeakingWriting` pipeline.
- Stored as source inventory now.
- Not mixed into the initial TOEIC Listening and Reading item bank.

## Recommended Approach

Use a staged, auditable pipeline.

### Approach A - Direct Import

Extract everything from every source and immediately create learner content.

Trade-off: fastest prototype, but unsafe. It would mix books, answer keys, transcripts, roadmap advice, and inaccessible sources without enough validation. This repeats the current product problem: content exists, but the learning flow is unreliable.

### Approach B - Manual Curation First

A human manually turns selected PDFs into lessons and tests.

Trade-off: high quality, but too slow for a large corpus. It is useful for the first pilot set, but not enough as the long-term production pipeline.

### Approach C - Staged Normalization With Human Review

Import all source inventory, extract raw assets, parse by material class, validate into TOEIC domain objects, then require review for low-confidence items before publication.

This is the recommended approach. It gives scale without allowing bad content into the learner app.

## Pipeline

### 1. Source Inventory

Store each row from the sheet as `SourceManifestEntry`.

Fields:

- `sourceId`
- `sheetRowNumber`
- `title`
- `url`
- `sourceType`
- `accessStatus`
- `primaryMaterialClass`
- `detectedTags`
- `auditNotes`
- `lastCheckedAt`

Rules:

- Inaccessible sources become `AccessBlocked`.
- Shortlinks are resolved and stored with both original URL and final URL.
- Drive folders are containers, not learning content.
- Drive files are raw assets, not learning items.

### 2. Container Discovery

Drive folders, SharePoint folders, shortlinks, and Google Docs must be expanded into a `SourceContainerManifest`.

Fields:

- `containerId`
- `parentSourceId`
- `containerTitle`
- `provider`
- `externalId`
- `childrenCount`
- `visibleChildrenCount`
- `accessStatus`

Child asset fields:

- `assetId`
- `containerId`
- `fileName`
- `mimeType`
- `extension`
- `sizeBytes`
- `providerUrl`
- `detectedRole`

Detected roles:

- `BookPdf`
- `TestPdf`
- `ListeningAudio`
- `TranscriptPdf`
- `AnswerKeyPdf`
- `ReadingPdf`
- `StrategyPdf`
- `VocabularyPdf`
- `Archive`
- `Video`
- `ExternalWebPage`
- `Unknown`

### 3. Raw Extraction

Raw extraction converts files into machine-readable blocks.

PDF output:

- page text
- page images
- table candidates
- question number candidates
- answer option candidates
- headers and footers
- confidence score per page

Audio output:

- file metadata
- duration
- test/book association
- track number
- optional speech-to-text transcript if no official transcript exists

Image output:

- image file metadata
- page crop metadata for Part 1 or visual vocabulary
- associated question candidates

Web output:

- page title
- main text
- outbound links
- media embeds
- screenshot hash
- extraction confidence

### 4. Material Parsing

Parsing must use parser profiles, not one generic parser.

Parser profiles:

- `FullTestBookParser`
- `ListeningBookParser`
- `ReadingBookParser`
- `AnswerKeyParser`
- `TranscriptParser`
- `VocabularyParser`
- `StrategyLessonParser`
- `RoadmapParser`
- `SpeakingWritingParser`

Each parser produces draft domain objects, never published objects.

Draft states:

- `Parsed`
- `NeedsLinking`
- `NeedsReview`
- `Rejected`

### 5. Cross-Asset Linking

The system must link book PDFs, audio, answer keys, and transcripts before publishing test items.

Examples:

- SPARTA TOEIC listening PDF + transcript PDF + answer key PDF + audio folder
- TOEIC Preparation book PDF + script/answer PDF + audio zip
- ABC TOEIC Listening PDF + Reading PDF + Audio + Transcript

Linking rules:

- A listening test item cannot publish without audio.
- A Part 3 or Part 4 group should publish with transcript when transcript is available.
- A test item cannot publish without answer key.
- A Part 1 item cannot publish without image or verified image crop.
- Reading items must link to passage evidence.

### 6. TOEIC Domain Normalization

Draft content must become one of the TOEIC domain item types.

Part 1:

- image
- audio prompt
- answer options
- correct answer
- transcript when available
- visual trap tag

Part 2:

- audio prompt
- answer options
- correct answer
- transcript when available
- question type tag

Part 3:

- conversation audio
- grouped questions
- transcript
- speaker/context tags
- evidence spans

Part 4:

- talk audio
- grouped questions
- transcript
- announcement/message/report type tags
- evidence spans

Part 5:

- sentence
- blank
- options
- correct answer
- grammar/vocabulary tag
- explanation

Part 6:

- passage
- blanks
- options
- correct answers
- cohesion/grammar/vocabulary tags
- explanation

Part 7:

- passage set
- questions
- answers
- evidence spans
- passage type tag
- timing difficulty tag

### 7. Validation Gates

No content reaches learners until validation passes.

Universal gates:

- source trace exists
- material class exists
- TOEIC part exists when applicable
- question text exists
- correct answer exists
- duplicate check passes
- parser confidence meets threshold or human review approved

Listening gates:

- audio exists
- audio duration is valid
- audio is linked to the right item or group
- transcript exists or transcript is explicitly marked unavailable

Reading gates:

- passage exists
- answer options exist
- evidence span exists for answerable questions

Lesson gates:

- lesson has learning objective
- lesson maps to part/unit/skill tag
- at least one guided example or drill recommendation exists

Roadmap gates:

- roadmap advice maps to internal learning stages
- no raw roadmap text is shown directly as learner business logic

### 8. Human Review Queue

The review queue is required because the material formats differ.

Review reasons:

- low OCR confidence
- missing answer key
- ambiguous part
- transcript/audio mismatch
- duplicate candidate
- inaccessible source
- external web content not stable
- parser profile missing

Review actions:

- approve
- reject
- relabel material class
- link missing asset
- split question group
- correct answer mapping
- mark source as deferred

## Database Boundary

The database should preserve both raw traceability and clean learner objects.

Core tables:

- `source_manifest_entries`
- `source_containers`
- `source_assets`
- `extracted_pages`
- `extracted_text_blocks`
- `extracted_media_segments`
- `parser_runs`
- `draft_learning_items`
- `draft_test_items`
- `validation_issues`
- `published_learning_items`
- `published_test_items`
- `audio_assets`
- `image_assets`
- `transcripts`
- `answer_keys`
- `source_traces`

The learner app reads only published tables and learner progress tables. It must not read raw source tables.

## Initial Production Slice

Start with TOEIC Listening and Reading only.

Priority 1 sources:

- SPARTA TOEIC
- TOEIC Preparation 2019
- ABC TOEIC
- Tactics for TOEIC
- Bộ xanh cam TOEIC
- Very Easy TOEIC
- 1000 câu giải đề Format mới
- Học theo Part 5,6,7 có giải thích

This slice should prove:

- source manifest import
- Drive folder/file discovery
- PDF extraction
- answer key extraction
- transcript extraction
- audio asset registration
- Part 5 and Part 7 parsing
- one listening part parsing path
- validation queue
- publish only validated items

## Deferred Work

The following are intentionally deferred from the first implementation plan:

- Full Speaking and Writing engine
- YouTube transcript ingestion
- SharePoint automation for blocked links
- Automatic OCR for every scanned book
- Full recursive import of all nested folders
- Learner UI redesign
- Payment and account features

Deferred does not mean ignored. These sources stay in inventory with status and audit notes.

## Testing Strategy

Use test-driven implementation for each pipeline layer.

Unit tests:

- source type classification
- material class classification
- validation gate rules
- TOEIC part-specific required fields
- duplicate detection
- parser confidence behavior

Integration tests:

- import 73-row manifest fixture
- mark 13 inaccessible sources correctly
- discover container children from a mocked Drive response
- extract PDF pages into raw blocks
- parse answer key fixture into answer mappings
- link transcript/audio/book assets into a test set
- reject listening item without audio
- reject test item without answer key

End-user smoke tests:

- learner never sees raw Drive/PDF source links as the primary study flow
- only published validated items appear in practice
- mistakes link back to explanation/transcript/evidence, not raw source files

Admin smoke tests:

- reviewer can see source trace
- reviewer can approve or reject a draft item
- rejected items do not publish
- blocked sources remain visible as operational issues

## Success Criteria

The pipeline is successful when:

- all 73 sheet rows are imported as source inventory
- every source has a clear access status
- accessible Drive folders produce child asset manifests
- selected priority sources produce validated draft content
- no draft content is visible to learners
- published TOEIC items satisfy part-specific validation
- listening items have audio links
- test items have answer keys
- review queue captures low-confidence or incomplete items
- learner flow consumes structured content from DB, not raw PDFs or source links

## Open Decisions

The first implementation plan should choose:

- whether to store extracted binary assets locally, in object storage, or as provider references first
- whether audio transcription is included in phase one or only official transcripts are used
- whether parser fixtures come from manually downloaded files or mocked extraction output
- whether blocked sources are retried automatically or only by admin action

The recommended default is provider references plus extracted text metadata first. Binary mirroring can be added after the parser and validation workflow is stable.

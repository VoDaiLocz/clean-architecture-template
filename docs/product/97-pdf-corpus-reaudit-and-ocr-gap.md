# PDF Corpus Reaudit And OCR Gap

## Status

Date: 2026-07-06

This audit was created after reviewing every downloaded PDF under `downloads/` and comparing it with the local SQLite runtime database at `backend/src/Api/toeic-normalization.db`.

The current production risk is severe: the project has many real TOEIC PDFs, but a large portion of those PDFs either never entered extraction or entered page extraction without usable text blocks. Any claim that the database has fully normalized the downloaded corpus is false.

## Current Counts

| Metric | Count |
| --- | ---: |
| Files ending in `.pdf` / `.PDF` under `downloads/` | 86 |
| Valid PDF files by header and `pdfinfo` | 74 |
| Bad PDF placeholder/error files | 12 |
| Valid PDF pages on disk | 8,493 |
| `source_assets` rows in SQLite | 75 |
| `extracted_pages` rows in SQLite | 6,522 |
| `extracted_text_blocks` rows in SQLite | 23,034 |
| Published learning questions | 117 |
| Published Part 5 questions | 20 |
| Published Part 6 questions | 75 |
| Published Part 7 questions | 22 |
| Published Part 1-4 questions | 0 |

## Critical Findings

### F1. Valid PDFs Are Not Fully Extracted

34 source assets match valid PDFs on disk but have `0` extracted pages and `0` extracted text blocks.

That accounts for approximately 2,467 pages that are currently present on disk but absent from the extraction layer.

High-impact examples:

| PDF | Pages | Current DB state |
| --- | ---: | --- |
| `200-350_ABC TOEIC LISTENING_1.pdf` | 281 | 0 pages, 0 blocks |
| `200-350_ABC TOEIC READING_2.pdf` | 230 | 0 pages, 0 blocks |
| `Sách Sparta TOEIC LCRC.pdf` | 210 | 0 pages, 0 blocks |
| `Tactics for TOEIC - Book.pdf` | 199 | 0 pages, 0 blocks |
| `1000 CÂU GIẢI ĐỀ TOEIC FORMAT MỚI 2019.pdf` | 198 | 0 pages, 0 blocks |
| `Sách Sparta TOEIC - Phần nghe.pdf` | 139 | 0 pages, 0 blocks |
| `Cẩm nang giải part 7 TOEIC.pdf` | 73 | 0 pages, 0 blocks |
| `Tactics for TOEIC - Answer Key.pdf` | 77 | 0 pages, 0 blocks |

### F2. Many Extracted PDFs Are Image-Only Or OCR-Missing

20 source assets have extracted page rows but `0` extracted text blocks.

That accounts for approximately 4,052 pages that the system knows about as pages but cannot parse into TOEIC content.

High-impact examples:

| PDF | Pages | Current DB state |
| --- | ---: | --- |
| `ĐỀ ĐỌC (1).pdf` | 330 | 330 pages, 0 blocks |
| `Target Toeic Students Book.pdf` | 320 | 320 pages, 0 blocks |
| `Taking the TOEIC - Skills and Strategies 2.pdf` | 313 | 313 pages, 0 blocks |
| `Starter_TOEIC_3rd_Edition.pdf` | 306 | 306 pages, 0 blocks |
| `200-350_ABC TOEIC LISTENING_1.pdf` | 281 | 281 pages, 0 blocks |
| `Taking the TOEIC - Skills and Strategies 1.pdf` | 279 | 279 pages, 0 blocks |
| `DevelopingSkills fortheTOEICTest.pdf` | 267 | 267 pages, 0 blocks |
| `TOEIC+Very+Easy.PDF` | 262 | 262 pages, 0 blocks |
| `TRANSCRIPT QUYỂN XANH (T6 - T10).pdf` | 236 | 236 pages, 0 blocks |
| `Tactics for TOEIC - Book.pdf` | 199 | 199 pages, 0 blocks |
| `ĐỀ NGHE (1).pdf` | 159 | 159 pages, 0 blocks |
| `TRANSCRIPT QUYỂN XANH (T1 - T5).pdf` | 119 | 119 pages, 0 blocks |

Verification with `pdftotext` on representative files returned only form-feed characters, which means these PDFs are image/scanned PDFs from the perspective of text extraction. They require OCR.

### F3. Placeholder PDFs Exist And Must Stay Blocked

12 downloaded `.pdf` files are not valid PDFs. They are small Google/HTML/error placeholder files and must not enter learning content normalization.

Examples:

| File | Size |
| --- | ---: |
| `downloads/folders/TOEIC Preparation LC + RC Volume 1, 2/Toeic Preparation 1.pdf` | 2,627 bytes |
| `downloads/folders/Taking the TOEIC - Skills and Strategies 2/Taking the TOEIC - Skills and Strategies 2.pdf` | 2,650 bytes |
| `downloads/folders/ĐỀ WRITING SAMPLE/ĐỀ 1.pdf` | 1,789 bytes |
| `downloads/folders/ĐỀ WRITING SAMPLE/ĐỀ 10.pdf` | 1,891 bytes |

### F4. Listening Cannot Be Published Yet

The current SQLite `source_assets` table has only role `Pdf`.

No usable audio/image assets are registered. Files named `AudioTOEICPreparation1.zip` and `AudioTOEICPreparation2.zip` are HTML placeholders, not valid ZIP audio archives. The `.rar` archives in downloads are large but could not be opened with the available `7z` command.

Therefore Part 1-4 must remain locked until a real media ingestion path exists.

## Required Production Fixes

### P0. Add A PDF Corpus Audit Command

The system needs a repeatable command that scans `downloads/` and writes a durable audit table/report with:

- physical file path
- file size
- PDF header validity
- `pdfinfo` page count
- DB `source_asset_id`
- DB extracted page count
- DB text block count
- extraction status:
  - `VALID_TEXT_EXTRACTED`
  - `VALID_PDF_NOT_EXTRACTED`
  - `IMAGE_PDF_OCR_REQUIRED`
  - `PLACEHOLDER_OR_INVALID_PDF`
  - `DUPLICATE_ASSET`

Completion criteria:

- The command must account for every file ending in `.pdf` or `.PDF`.
- The command must fail if any valid PDF has neither extraction nor an explicit blocked/OCR-required status.
- The command must be idempotent.

### P1. Add OCR Stage For Image PDFs

Current text extraction is not enough. The corpus contains many image/scanned PDFs.

Required pipeline:

1. Render PDF pages to images using `pdftoppm`.
2. OCR rendered pages using a production OCR engine.
3. Store OCR text into `extracted_text_blocks` with provenance:
   - source asset id
   - page id/page number
   - OCR engine
   - confidence if available
   - extraction method: `ocr`
4. Mark low-confidence OCR blocks for manual review before publishing.

Completion criteria:

- At least one currently zero-text PDF such as `ĐỀ ĐỌC (1).pdf` produces text blocks.
- OCR blocks must be distinguishable from embedded-text blocks.
- Parser must not publish OCR-derived questions without answer evidence and source trace.

### P2. Re-run Reading Parsers After OCR

Once OCR exists, re-run parsers for:

- Part 5 incomplete sentences
- Part 6 text completion
- Part 7 reading comprehension
- answer key extraction
- passage grouping

Completion criteria:

- Parser reruns must remain semantic-key idempotent.
- Published content count must increase only from validated drafts.
- No parser may create learner-visible content from a PDF page that lacks answer evidence.

### P3. Add Media Asset Recovery For Listening

Listening must not be faked from PDF text.

Required work:

- Recover valid audio archives or re-download audio from source links.
- Register audio assets as `SourceAssetRole.Audio`.
- Extract audio metadata.
- Link transcripts/answer keys to audio assets.
- Add image extraction for Part 1 photographs where needed.

Completion criteria:

- Part 1 cannot publish without image + audio.
- Part 2 cannot publish without audio.
- Part 3/4 cannot publish without audio + grouped questions + transcript/answer evidence.

## Current Truth For Product/UI

The learner app may truthfully show:

- Part 5: available with 20 published questions.
- Part 6: available with 75 published questions.
- Part 7: available with 22 published questions.
- Part 1-4: locked because required listening media assets are not usable yet.

The learner app must not claim:

- the full PDF corpus has been normalized;
- all downloaded PDFs are represented as learning content;
- listening parts are ready;
- extracted pages equal usable learning content.


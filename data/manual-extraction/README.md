# Manual Extraction Workspace

This folder stores manually transcribed TOEIC items from scanned/image-only PDFs before they are imported into the normalized database.

Manual extraction is allowed only as a controlled production fallback when embedded text/OCR is missing. It must not bypass validation.

## Required Fields

Each JSONL row must include:

- `sourceFile`
- `sourcePdfPage`
- `sourcePrintedPage`
- `answerEvidencePdfPage`
- `sourceTest`
- `toeicPart`
- `questionNumber`
- `prompt` or `passageText`
- `options`
- `correctAnswer`
- `extractionMethod`
- `confidence`

## Status Rules

- Rows without `correctAnswer` stay out of publishable drafts.
- Rows without a real PDF page trace stay out of publishable drafts.
- Part 6 and Part 7 rows must preserve their passage/group context.
- Listening items must not be manually created from reading PDFs.
- Auto-parsed text blocks must be sample-audited before publish; text extraction is not the same as validated learning content.

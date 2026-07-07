# Listening Part 1-4 Source Audit

## Conclusion

- The downloaded PDFs do contain real TOEIC Part 1-4 material.
- The strongest complete text bundle is `Sparta Toeic`: listening question book, transcript, and answer key are all text-extracted.
- Part 1 also has embedded images in `Sách Sparta TOEIC - Phần nghe.pdf`; `pdfimages` detected 278 image objects in that PDF.
- No publishable listening runtime item should be marked complete until a valid audio asset is linked.

## Audio Readiness

- Direct audio files found: `162`
- Direct audio files playable by `ffprobe`: `162`
- Direct audio files invalid by `ffprobe`: `0`
- Total direct audio duration: `158.78` minutes
- Longest direct audio: `downloads/noinoi/Audio Taking the TOEIC 1-20260706T075154Z-3-001/Audio Taking the TOEIC  1/162 Track162.mp3` at `47.36` minutes
- Zip files found: `3`
- Zip files that are HTML/placeholders, not real zip archives: `2`
- MP4 files found: `49`
- MP4 files that are actually HTML Google Drive error pages: `49`
- MP4 files with readable audio stream: `0`
- MP4 files invalid or without readable audio: `49`
- RAR files found: `2`
- RAR files unreadable by `7z l`: `2`

## PDF Queue State

- `IMAGE_PDF_OCR_OR_MANUAL_REQUIRED`: `26`
- `PLACEHOLDER_OR_INVALID_PDF`: `12`
- `VALID_TEXT_EXTRACTED`: `48`

## Strong Part 1-4 Candidates From Text Corpus

| Source | Roles | Part hits | Question markers | Text file |
|---|---:|---:|---:|---|
| `downloads/folders/Thư mục/TOEIC Analyst Book.pdf` | `question_book` | `P1:15, P2:9, P3:63, P4:95` | `1082` | `data/pdf-text-corpus/049-toeic-analyst-book-pdf.txt` |
| `downloads/folders/Sparta Toeic/Sách Sparta TOEIC - Phần nghe.pdf` | `question_book` | `P1:21, P2:19, P3:54, P4:83` | `652` | `data/pdf-text-corpus/027-s-ch-sparta-toeic-ph-n-nghe-pdf.txt` |
| `downloads/folders/Spart Toeic Quyển 2/Sách Sparta TOEIC LCRC.pdf` | `question_book` | `P1:17, P2:5, P3:34, P4:44` | `698` | `data/pdf-text-corpus/024-s-ch-sparta-toeic-lcrc-pdf.txt` |
| `downloads/ZENLISH - TÀI LIỆU CHO MỨC NỀN 450 LÊN 800Đ NGHE ĐỌC.pdf` | `question_book` | `P1:5, P2:11, P3:8, P4:3` | `6` | `data/pdf-text-corpus/017-zenlish-t-i-li-u-cho-m-c-n-n-450-l-n-800d-nghe-d-c-pdf.txt` |
| `downloads/KỸ NĂNG LÀM BÀI NGHE CÓ HÌNH ẢNH TRONG ĐỀ THI TOEIC FORMAT MỚI.pdf` | `question_book` | `P3:6, P4:5` | `0` | `data/pdf-text-corpus/008-k-n-ng-l-m-b-i-nghe-c-h-nh-nh-trong-d-thi-toeic-format-m-i-pdf.txt` |
| `downloads/HƯỚNG DẪN GIẢI BÀI NGHE CÓ BA GIỌNG ĐỌC.pdf` | `question_book` | `P3:4, P4:1` | `0` | `data/pdf-text-corpus/006-h-ng-d-n-gi-i-b-i-nghe-c-ba-gi-ng-d-c-pdf.txt` |
| `downloads/folders/Sparta Toeic/Lời thoại (transcript) Sách Sparta TOEIC.pdf` | `transcript` | `P1:14, P2:10, P3:131, P4:90` | `990` | `data/pdf-text-corpus/026-l-i-tho-i-transcript-s-ch-sparta-toeic-pdf.txt` |
| `downloads/folders/Spart Toeic Quyển 2/Lời thoại (transcript) Sách Sparta TOEIC LC+RC.pdf` | `transcript` | `P1:10, P2:5, P3:71, P4:40` | `582` | `data/pdf-text-corpus/023-l-i-tho-i-transcript-s-ch-sparta-toeic-lc-rc-pdf.txt` |
| `downloads/folders/TOEIC Preparation LC + RC Volume 1, 2/TPLCRC2-ScriptsAK.pdf` | `transcript` | `P1:12, P2:24, P3:18, P4:32` | `0` | `data/pdf-text-corpus/035-tplcrc2-scriptsak-pdf.txt` |
| `downloads/folders/Thư mục/TPLCRC2-ScriptsAK.pdf` | `transcript` | `P1:12, P2:24, P3:18, P4:32` | `0` | `data/pdf-text-corpus/051-tplcrc2-scriptsak-pdf.txt` |

## Best Production Bundles

1. `downloads/noinoi/`
   - Question book: `Taking the TOEIC - Skills and Strategies 1.pdf`
   - Audio archive: `Audio Taking the TOEIC 1-20260706T075154Z-3-001.zip`
   - Extracted audio: `162` playable MP3 tracks, numbered `001-162`, no missing track numbers.
   - PDF status: `279` pages with `279` image objects; text layer is effectively empty, so OCR is required.
   - Production status: best current audio source, but question extraction needs OCR before publish.
2. `downloads/folders/Sparta Toeic/`
   - Question book: `Sách Sparta TOEIC - Phần nghe.pdf`
   - Transcript: `Lời thoại (transcript) Sách Sparta TOEIC.pdf`
   - Answer key: `Đáp án (answer key) Sách Sparta TOEIC - Phần nghe.pdf`
   - Status: text and images are available; matching audio still needs to be linked.
3. `downloads/folders/Spart Toeic Quyển 2/`
   - Question book: `Sách Sparta TOEIC LCRC.pdf`
   - Transcript: `Lời thoại (transcript) Sách Sparta TOEIC LC+RC.pdf`
   - Answer key: `Đáp án (answer key) Sách Sparta TOEIC LC & RC.pdf`
   - Status: text is available; matching audio still needs to be linked.
4. `downloads/folders/TOEIC Preparation LC + RC Volume 1, 2/` and duplicated `downloads/folders/Thư mục/`
   - Script/answer file: `TPLCRC2-ScriptsAK.pdf`
   - Status: text is available; the downloaded audio zip files are placeholders, not usable archives.
5. `downloads/folders/New TOEIC 700/*.rar`
   - Status: RAR headers exist, but `7z` cannot list or test either archive, so their contents are not currently usable as audio evidence.


## Image/OCR Listening Candidates

These are likely listening-related PDFs but currently require OCR/manual extraction before reliable parsing.

| Source | Pages | Next action |
|---|---:|---|
| `downloads/folders/ABC TOEIC/200-350_ABC TOEIC LISTENING_1.pdf` | `281` | `manual_or_ocr_extract` |
| `downloads/folders/ABC TOEIC/200-350_ABC TOEIC READING_2.pdf` | `230` | `manual_or_ocr_extract` |
| `downloads/folders/TACTICS FOR TOEIC/Tactics for TOEIC - Answer Key.pdf` | `77` | `manual_or_ocr_extract` |
| `downloads/folders/TACTICS FOR TOEIC/Tactics for TOEIC - Book.pdf` | `199` | `manual_or_ocr_extract` |
| `downloads/folders/TACTICS FOR TOEIC/Tactics for TOEIC - Practice Test 1.pdf` | `69` | `manual_or_ocr_extract` |
| `downloads/folders/TACTICS FOR TOEIC/Tactics for TOEIC - Practice Test 2.pdf` | `67` | `manual_or_ocr_extract` |
| `downloads/folders/Taking the TOEIC - Skills and Strategies 1/Taking the TOEIC - Skills and Strategies 1.pdf` | `279` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/200-350_ABC TOEIC LISTENING_1.pdf` | `281` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/200-350_ABC TOEIC READING_2.pdf` | `230` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/DevelopingSkills fortheTOEICTest.pdf` | `267` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/Starter_TOEIC_3rd_Edition.pdf` | `306` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/TOEIC+Very+Easy.PDF` | `262` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/TRANSCRIPT QUYỂN XANH (T1 - T5).pdf` | `119` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/TRANSCRIPT QUYỂN XANH (T6 - T10).pdf` | `236` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/Tactics for TOEIC - Answer Key.pdf` | `77` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/Tactics for TOEIC - Book.pdf` | `199` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/Tactics for TOEIC - Practice Test 1.pdf` | `69` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/Tactics for TOEIC - Practice Test 2.pdf` | `67` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/Taking the TOEIC - Skills and Strategies 2.pdf` | `313` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/Target Toeic Students Book.pdf` | `320` | `manual_or_ocr_extract` |
| `downloads/folders/Thư mục/ĐỀ NGHE (1).pdf` | `159` | `manual_or_ocr_extract` |

## Production Interpretation

- `Part 1-4 content exists`: yes.
- `47 mp3 files in the current workspace`: not exactly. The current scan finds `162` direct playable MP3 files under `downloads/noinoi`.
- `Part 1-4 ready to publish as TOEIC listening practice`: partially. Audio exists for `Taking the TOEIC 1`, but its question PDF is scanned/image-only and needs OCR before item-level publish.
- Safe next implementation: OCR `downloads/noinoi/Taking the TOEIC - Skills and Strategies 1.pdf`, align question groups with tracks `001-162`, then validate and publish only groups with matched audio/question/answer evidence.

# TOEIC Data Production Strategy

## Muc Dich

File nay la tai lieu tham chieu trung tam cho bai toan du lieu cua he thong hoc TOEIC. Muc tieu khong phai la mo PDF, Drive link, hay hien thi file tho cho learner. Muc tieu production la bien toan bo kho tai lieu thanh du lieu hoc TOEIC that trong database, co trace nguon, co validation, co review, co publish, va co coverage do duoc.

He thong chi duoc coi la dung huong khi learner hoc tu `published_lessons`, `published_questions`, `published_tests`, `published_media_assets`, va learner state. Learner khong duoc hoc truc tiep tu raw PDF, Google Sheet, Drive folder, draft parser output, hay admin source evidence.

## Ket Luan Hien Trang

Trang thai hien tai cua repo:

- Da co huong DB-first dung.
- Da co `source_manifest_entries` de luu inventory tu Google Sheet/tai lieu audit.
- Da import duoc 73 source rows vao source inventory.
- Da co schema/handler/test fixture cho source asset, extraction, draft, validation, review, publish.
- Chua co corpus-scale extraction tu toan bo 73 source thanh lesson/question/test that.
- Chua co published content du lon, du da dang, du dung de goi la san pham hoc TOEIC production.

So lieu inventory dang co:

| Chi so | Gia tri |
| --- | ---: |
| Total audited sources | 73 |
| Accessible sources | 60 |
| Blocked sources | 13 |
| Sources with PDF evidence | 33 |
| Sources with audio evidence | 20 |
| Sources with image evidence | 11 |
| Sources with transcript evidence | 6 |
| Sources with answer key evidence | 5 |
| Drive folders | 36 |
| Drive files | 14 |
| Shortlinks | 4 |

Dieu nay co nghia: he thong da nam duoc ban do kho tai lieu, nhung chua chuan hoa het noi dung hoc.

## Nguyen Tac Bat Buoc

1. Source manifest khong phai learning content.
2. Raw extraction khong phai learning content.
3. Draft content khong phai learning content.
4. Chi published content moi duoc learner API doc.
5. Moi published item phai co source trace.
6. Moi TOEIC part phai co validation rieng.
7. Validation that bai thi khong publish.
8. Human review la bat buoc voi low-confidence, OCR loi, thieu evidence, hoac content map mo ho.
9. Frontend khong duoc tu tao question, score, unlock, mastery, recommendation.
10. Coverage dashboard phai cho biet ro dang thieu gi, o part nao, tu source nao.

## Kien Truc Production Can Co

Pipeline du lieu production gom 7 tang ro rang:

```text
Source Manifest
  -> Source Asset Discovery
  -> Raw Extraction
  -> Draft Normalization
  -> Validation Gate
  -> Human Review
  -> Published Learner Content
```

Moi tang co bang du lieu rieng, handler rieng, test rieng, va status rieng. Khong gop cac tang vao mot bang lon.

## Tang 1: Source Manifest

Muc dich: luu danh sach nguon tai lieu duoc audit tu Google Sheet va cac link lien quan.

Bang/contract chinh:

- `source_manifest_entries`

Du lieu can luu:

- source id
- sheet row number
- title
- url
- provider: Google Drive, Google Docs, SharePoint, Shortlink, ExternalWeb
- source type: DriveFile, DriveFolder, GoogleSheet, GoogleDoc, Shortlink, ExternalWeb
- access status
- evidence flags: pdf/audio/image/transcript/answer key
- audit notes

Pass criteria:

- Import idempotent.
- Re-run import khong tao duplicate.
- Dashboard dem duoc total, accessible, blocked, evidence flags.

## Tang 2: Source Asset Discovery

Muc dich: bien moi source thanh cac asset that co the xu ly.

Bang/contract chinh:

- `source_containers`
- `source_assets`
- `source_discovery_issues`
- `source_resolution_records`

Asset role can co:

- PDF
- Audio
- Image
- Transcript
- AnswerKey
- WebPage
- VideoLink
- Unknown

Du lieu moi asset can luu:

- asset id
- source id
- container id
- file name
- mime type
- extension
- size bytes
- detected role
- provider url
- object key
- checksum

Production yeu cau:

- Drive folder phai bung thanh file con.
- Drive file phai dang ky thanh asset.
- Shortlink/web phai resolve va luu redirect status.
- Blocked source phai tao issue, khong silently skip.
- Unknown asset khong bi xoa, nhung khong di tiep vao parser neu chua classify.

## Tang 3: Raw Extraction

Muc dich: trich noi dung tho tu asset nhung van chua cho learner thay.

Bang/contract chinh:

- `extracted_pages`
- `extracted_text_blocks`
- `source_audio_metadata`
- image metadata neu can
- transcript raw segment neu can

Extraction theo asset:

### PDF

Can trich:

- page number
- width/height
- text block
- block type
- coordinates
- confidence
- source asset id

### Audio

Can trich:

- duration
- format
- sample rate
- bitrate
- language/voice metadata neu co
- link voi source/test/part neu detect duoc

### Image

Can trich:

- dimensions
- checksum
- role
- possible Part 1 mapping

### Transcript

Can trich:

- segment id
- group id neu co
- speaker label neu co
- timestamp neu co
- text
- confidence

### Answer Key

Can trich:

- test id
- part
- question number
- correct answer
- source trace
- confidence

Production yeu cau:

- Raw extraction phai idempotent.
- Co confidence.
- Co source trace.
- Extraction loi phai tao issue, khong lam mat asset.

## Tang 4: Draft Normalization

Muc dich: bien raw extraction thanh draft TOEIC domain object.

Bang/contract chinh:

- `draft_content_items`

Draft item types:

- LessonDraft
- GuidedExampleDraft
- ReadingQuestion
- ListeningQuestion
- AnswerKeyMapping
- TranscriptSegment
- QuestionGroup
- PassageDraft
- TestBlueprint
- MediaMapping

Draft payload phai structured JSON, khong de text blob mo ho.

### Part 1 Draft

Required:

- image asset
- audio asset
- prompt/question
- options
- correct answer
- explanation
- source trace

Validation hard fail neu thieu image hoac audio.

### Part 2 Draft

Required:

- audio asset
- response choices hoac answer mapping
- correct answer
- explanation/transcript neu co
- source trace

Validation hard fail neu thieu audio.

### Part 3 Draft

Required:

- audio group
- transcript group neu co
- 3 questions/group theo format TOEIC
- choices
- correct answers
- explanation/evidence
- source trace

Validation hard fail neu group thieu audio hoac question count khong dung policy.

### Part 4 Draft

Required:

- audio talk
- transcript neu co
- grouped questions
- choices
- correct answers
- explanation/evidence
- source trace

Validation hard fail neu thieu audio.

### Part 5 Draft

Required:

- sentence/prompt
- 4 options
- correct answer
- explanation
- skill tags: grammar, vocabulary, word form, tense, preposition, conjunction
- source trace

### Part 6 Draft

Required:

- passage
- blanks/questions
- options
- correct answers
- explanation
- source trace

Validation hard fail neu thieu passage.

### Part 7 Draft

Required:

- passage set
- question group
- options
- correct answers
- evidence span
- explanation
- source trace

Validation hard fail neu thieu passage hoac evidence.

## Tang 5: Validation Gate

Muc dich: dam bao du lieu dung TOEIC domain truoc khi review/publish.

Bang/contract chinh:

- `validation_issues`

Validation categories:

- MissingRequiredMedia
- MissingPassage
- MissingAnswerKey
- MissingExplanation
- InvalidQuestionCount
- InvalidOptionCount
- UnsupportedToeicPart
- LowParserConfidence
- DuplicateQuestion
- BrokenSourceTrace
- MismatchedAnswerKey
- OCRNoise
- AudioTranscriptMismatch

Issue severity:

- Blocker: khong duoc publish
- Major: can human review
- Minor: co the review/publish neu approved
- Info: metadata/correction note

Pass criteria:

- Moi draft co status ro: PendingValidation, ReadyForReview, ValidationFailed.
- Validation fail tao issue co code, message, source id, asset id, draft id.
- Khong co draft invalid nao vao learner API.

## Tang 6: Human Review

Muc dich: nguoi van hanh kiem tra draft, evidence, issue va quyet dinh approve/reject.

Bang/contract chinh:

- review decisions
- draft status transitions
- audit trail

Review decision:

- Approve
- Reject
- NeedsCorrection
- MergeDuplicate
- RelabelPart
- RelabelSkill

Production yeu cau:

- Approved draft tao published content.
- Rejected draft bi an khoi learner.
- Moi decision co actor, time, reason.
- Khong approve draft dang ValidationFailed neu khong co override policy ro rang.

## Tang 7: Published Learner Content

Muc dich: day la tang duy nhat learner duoc doc.

Bang/contract chinh:

- `published_lessons`
- `guided_examples`
- `published_questions`
- `published_question_groups`
- `published_passages`
- `published_media_assets`
- `published_tests`
- `published_test_sections`
- `published_test_items`

Published content phai co:

- stable id
- TOEIC part
- unit id
- skill tags
- source trace
- status
- version
- evidence

Learner API chi doc tu published tables va learner state tables.

## Coverage Dashboard Bat Buoc

Neu khong co coverage dashboard thi khong biet du lieu da du chua. Production phai co cac chi so sau:

### Source Coverage

| Metric | Y nghia |
| --- | --- |
| total_sources | Tong source da audit |
| accessible_sources | Source co the xu ly |
| blocked_sources | Source bi chan |
| sources_with_pdf | Source co PDF |
| sources_with_audio | Source co audio |
| sources_with_image | Source co image |
| sources_with_transcript | Source co transcript |
| sources_with_answer_key | Source co answer key |

### Asset Coverage

| Metric | Y nghia |
| --- | --- |
| discovered_assets | Tong asset bung ra tu source |
| pdf_assets | PDF assets |
| audio_assets | Audio assets |
| image_assets | Image assets |
| answer_key_assets | Answer key assets |
| transcript_assets | Transcript assets |
| unknown_assets | Asset chua classify |

### Extraction Coverage

| Metric | Y nghia |
| --- | --- |
| extracted_pdf_pages | So page PDF da extract |
| extracted_text_blocks | So block text |
| extracted_audio_metadata | So audio da probe |
| parsed_answer_keys | So answer key mappings |
| parsed_transcripts | So transcript segments |

### Draft Coverage

| Metric | Y nghia |
| --- | --- |
| draft_items_total | Tong draft |
| draft_by_part | Draft theo Part 1-7 |
| draft_by_type | Lesson/question/group/test |
| pending_validation | Cho validate |
| ready_for_review | San sang review |
| validation_failed | Loi validation |

### Published Coverage

| Metric | Y nghia |
| --- | --- |
| published_lessons | Lesson da publish |
| published_questions | Question da publish |
| published_tests | Test da publish |
| published_by_part | Published theo Part 1-7 |
| published_with_explanation | Co explanation |
| published_with_media | Co audio/image |
| published_with_evidence | Co evidence/source trace |

### TOEIC Domain Coverage

| Part | Minimum production coverage can do |
| --- | --- |
| Part 1 | Image + audio + question + answer |
| Part 2 | Audio response items |
| Part 3 | Conversation groups + 3 questions/group |
| Part 4 | Talk groups + questions |
| Part 5 | Grammar/vocab sentence questions |
| Part 6 | Passage completion groups |
| Part 7 | Reading passage groups with evidence spans |

## Dinh Nghia "Du Day Du Va Da Dang"

Khong duoc danh gia bang cam tinh. Phai co gate.

### Alpha Content Gate

Muc tieu: he thong hoc duoc end-to-end bang content that.

- It nhat 1 unit published cho moi Part 1-7.
- It nhat 1 lesson + 1 guided example + 1 drill cho moi part.
- It nhat 1 mini test cho Listening va 1 mini test cho Reading.
- Moi published question co explanation va source trace.
- Listening parts co audio.
- Reading parts co passage neu can.

### Private Beta Content Gate

Muc tieu: learner co the hoc co lo trinh that.

- It nhat 5 units/part cho Part 1-7.
- Moi part co drill va mini test.
- Co review repair flow tao tu wrong answers.
- Coverage dashboard khong con unknown asset quan trong.
- Validation failed duoc gom thanh issue queue.

### Public Beta Content Gate

Muc tieu: san pham co the ban cho user that.

- Du 7 parts voi lo trinh tu basic den target 800+.
- Co part tests cho 7 parts.
- Co section tests Listening/Reading.
- Co full TOEIC LR tests.
- Co scoring breakdown va repair plan.
- Co coverage theo part/skill de chung minh content khong lech.

### Market Release Gate

Muc tieu: production commercial.

- Published content du lon, co versioning, co audit.
- Validation va review workflow chay on dinh.
- Admin co dashboard source/extraction/draft/issue/publish/coverage.
- Learner khong gap placeholder/fake content.
- Moi content learner thay deu trace duoc ve source.

## Huong Trien Khai Dung Tu Day

### Phase D1: Content Coverage Baseline

Muc tieu: biet chinh xac he thong dang co gi.

Tasks:

1. Tao `GetContentCoverageHandler`.
2. Them repository queries/counts cho source, asset, extraction, draft, validation, published.
3. Tao API `/api/admin/content-coverage`.
4. Test coverage khong duoc nham source manifest la published content.
5. Dashboard/admin sau nay doc API nay.

Pass:

- API tra du count cho tung tang.
- Published count hien tai thap thi phai hien thap, khong fake.
- Coverage by TOEIC part ro rang.

### Phase D2: Asset Inventory Expansion

Muc tieu: tu 73 source, bung thanh asset inventory that.

Tasks:

1. Chay source manifest import.
2. Register asset tu evidence flags.
3. Discover Drive folder files bang adapter that hoac authenticated automation.
4. Resolve shortlink/web source.
5. Luu blocked/failed issue.

Pass:

- Co asset count that theo role.
- Moi source accessible co container/asset hoac issue ro rang.
- Unknown asset duoc track.

### Phase D3: Extraction Jobs

Muc tieu: moi asset co raw extraction tuong ung.

Tasks:

1. PDF extraction job.
2. Audio metadata job.
3. Image metadata job.
4. Transcript parse job.
5. Answer key parse job.

Pass:

- PDF co pages/blocks.
- Audio co metadata.
- Answer key co mappings.
- Transcript co segments.
- Loi extraction tao issue.

### Phase D4: TOEIC Draft Normalization

Muc tieu: tao draft domain object cho 7 parts.

Tasks:

1. Reading draft normalizer cho Part 5/6/7.
2. Listening group normalizer cho Part 1/2/3/4.
3. Lesson/guided example normalizer tu strategy/grammar/vocab source.
4. Test blueprint normalizer tu test books.
5. Source trace builder.

Pass:

- Draft co part, type, payload JSON structured, source trace.
- Co draft_by_part coverage.
- Khong publish tu dong.

### Phase D5: Validation And Review

Muc tieu: ngan data sai vao learner.

Tasks:

1. Validation rules theo 7 TOEIC parts.
2. Validation issue workflow.
3. Admin review queue.
4. Approve/reject/needs correction.
5. Publish transaction.

Pass:

- Draft invalid khong vao published.
- Approved draft tao published content.
- Rejected draft bi hidden.
- Validation issue co code/severity/source trace.

### Phase D6: Learner API From Published Content

Muc tieu: FE learner doc content that.

Tasks:

1. Lesson API doc `published_lessons` va `guided_examples`.
2. Practice API doc `published_questions`.
3. Part overview API doc published/progress/mastery.
4. Review API doc learner review state.
5. Test API doc published tests.

Pass:

- FE khong can mock content.
- Khong co raw/admin/source terminology trong learner API.
- Khong co fake fallback question.

## Thu Tu Uu Tien Thuc Te

Nen lam theo thu tu nay:

1. Content coverage API truoc.
2. Asset inventory expansion.
3. Extraction jobs cho PDF/audio/answer key.
4. Draft normalizer Part 5 va Part 7 truoc vi reading de validate hon audio.
5. Draft normalizer Part 3/4 sau vi can group/audio/transcript.
6. Validation gate.
7. Review/publish.
8. Learner API dung published content.
9. Admin coverage dashboard.

Ly do: neu khong co coverage API truoc, minh khong biet da chuan hoa duoc bao nhieu. Neu nhay vao parser ngay, se lai roi vao tinh trang co fixture nhung khong biet production corpus dang thieu gi.

## Nhung Dieu Khong Duoc Lam

- Khong hardcode cau hoi vao frontend.
- Khong coi Google Sheet row la learning item.
- Khong cho learner click mo PDF/Drive lam trai nghiem hoc chinh.
- Khong publish draft neu chua validate.
- Khong bo qua source trace.
- Khong dem fixture test nhu production content.
- Khong gom extraction, validation, publish vao mot command lon kho test.
- Khong lam UI dashboard dep truoc khi API coverage dung.

## Definition Of Done Cho Data Production

Mot phase data chi duoc coi la xong khi:

- Co failing test truoc.
- Co implementation pass test.
- Co count/coverage chung minh ket qua.
- Co idempotency.
- Co issue path cho source/asset/content loi.
- Co source trace.
- Khong expose raw/draft ra learner API.
- Build/test pass.
- Commit rieng va push remote.

## Task Tiep Theo Nen Lam

Task production tiep theo nen la:

`feat(data): add content coverage baseline`

Pham vi task:

- Them domain response `ContentCoverageSnapshot`.
- Them handler `GetContentCoverageHandler`.
- Them repository count helpers neu can.
- Them API `/api/admin/content-coverage`.
- Them tests khang dinh:
  - source manifest count la 73 sau import
  - published question count khong bi fake
  - draft/validation/published counts tach rieng
  - coverage by part tra dung published data hien co

Sau task nay moi nen lam asset discovery/extraction tiep, vi luc do moi co thang do ro rang de noi "da chuan hoa bao nhieu".

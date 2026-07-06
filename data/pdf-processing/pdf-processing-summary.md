# PDF Processing Queue

Generated from local `downloads/` and `backend/src/Api/toeic-normalization.db`.

- Total PDF-like files: 86
- Total valid pages: 8493

## By Extraction Class

- `IMAGE_PDF_OCR_OR_MANUAL_REQUIRED`: 26
- `PLACEHOLDER_OR_INVALID_PDF`: 12
- `VALID_TEXT_EXTRACTED`: 48

## Processing Rule

1. Invalid/placeholder PDFs stay blocked.
2. Text PDFs go through source registration, block extraction, parser, validation, review, publish.
3. Image PDFs require OCR or manual extraction before parser/publish.
4. Manual rows must include source page and answer evidence page.

## Next Queue Items

- 1. `downloads/1000 CÂU GIẢI ĐỀ TOEIC FORMAT MỚI 2019.pdf` | VALID_TEXT_EXTRACTED | pages=198 | manual=not_started | next=parse_or_review_drafts
- 2. `downloads/1500 từ vựng TOEIC thường gặp.pdf` | VALID_TEXT_EXTRACTED | pages=117 | manual=not_started | next=parse_or_review_drafts
- 3. `downloads/Các dạng câu hỏi part 2 thường gặp trong TOEIC format mới.pdf` | VALID_TEXT_EXTRACTED | pages=14 | manual=not_started | next=parse_or_review_drafts
- 4. `downloads/Cẩm nang giải part 7 TOEIC.pdf` | VALID_TEXT_EXTRACTED | pages=73 | manual=not_started | next=parse_or_review_drafts
- 5. `downloads/GIAI THICH CHI TIET SACH HACKER TOEIC STYLE KOREA.pdf` | VALID_TEXT_EXTRACTED | pages=26 | manual=not_started | next=parse_or_review_drafts
- 6. `downloads/HƯỚNG DẪN GIẢI BÀI NGHE CÓ BA GIỌNG ĐỌC.pdf` | VALID_TEXT_EXTRACTED | pages=33 | manual=not_started | next=parse_or_review_drafts
- 7. `downloads/KẾ HOẠCH 30 NGÀY ÔN TOEIC.pdf` | VALID_TEXT_EXTRACTED | pages=61 | manual=not_started | next=parse_or_review_drafts
- 8. `downloads/KỸ NĂNG LÀM BÀI NGHE CÓ HÌNH ẢNH TRONG ĐỀ THI TOEIC FORMAT MỚI.pdf` | VALID_TEXT_EXTRACTED | pages=32 | manual=not_started | next=parse_or_review_drafts
- 9. `downloads/Lộ trình chinh phục TOEIC Nói Viết 250+.pdf` | VALID_TEXT_EXTRACTED | pages=7 | manual=not_started | next=parse_or_review_drafts
- 10. `downloads/Lộ trình từ A đến Z đạt TOEIC 700+.pdf` | VALID_TEXT_EXTRACTED | pages=77 | manual=not_started | next=parse_or_review_drafts
- 11. `downloads/Thu thuat lam bai thi TOEIC.pdf` | VALID_TEXT_EXTRACTED | pages=34 | manual=not_started | next=parse_or_review_drafts
- 12. `downloads/TÀI LIỆU 30 NGÀY TỰ ÔN TOEIC.pdf` | VALID_TEXT_EXTRACTED | pages=1 | manual=not_started | next=parse_or_review_drafts
- 13. `downloads/Tổng hợp 300 từ vựng TOEIC cho trình độ mất gốc.pdf` | VALID_TEXT_EXTRACTED | pages=128 | manual=not_started | next=parse_or_review_drafts
- 14. `downloads/Tổng hợp Từ vựng part 2 - Duonghiephl.pdf` | VALID_TEXT_EXTRACTED | pages=22 | manual=not_started | next=parse_or_review_drafts
- 15. `downloads/Tự-Học-TOEIC-Để-Tiết-Kiệm-Tiền-Cho-Bố-Mẹ-Các-Bạn (1).pdf` | VALID_TEXT_EXTRACTED | pages=141 | manual=not_started | next=parse_or_review_drafts
- 16. `downloads/VƯỢT QUA DẠNG ĐIỀN CÂU VÀO CHỖ TRỐNG TRONG PART 6 DỄ DÀNG.pdf` | VALID_TEXT_EXTRACTED | pages=30 | manual=not_started | next=parse_or_review_drafts
- 17. `downloads/ZENLISH - TÀI LIỆU CHO MỨC NỀN 450 LÊN 800Đ NGHE ĐỌC.pdf` | VALID_TEXT_EXTRACTED | pages=12 | manual=not_started | next=parse_or_review_drafts
- 18. `downloads/[FIRE TOEIC] GIẢI THÍCH CHI TIẾT SÁCH NEW ECONOMY 2018 - PART 5.pdf` | VALID_TEXT_EXTRACTED | pages=62 | manual=not_started | next=parse_or_review_drafts
- 19. `downloads/folders/ABC TOEIC/200-350_ABC TOEIC LISTENING_1.pdf` | IMAGE_PDF_OCR_OR_MANUAL_REQUIRED | pages=281 | manual=not_started | next=manual_or_ocr_extract
- 20. `downloads/folders/ABC TOEIC/200-350_ABC TOEIC READING_2.pdf` | IMAGE_PDF_OCR_OR_MANUAL_REQUIRED | pages=230 | manual=not_started | next=manual_or_ocr_extract
- 21. `downloads/folders/EBOOK 10 NGUYÊN TẮC TRONG TỰ HỌC TOEIC - QM/EBOOK 10 NGUYÊN TẮC TRONG TỰ HỌC TOEIC - QM.pdf` | VALID_TEXT_EXTRACTED | pages=6 | manual=not_started | next=parse_or_review_drafts
- 22. `downloads/folders/New TOEIC 700/huong-dan-lay-pass-11.pdf` | VALID_TEXT_EXTRACTED | pages=2 | manual=not_started | next=parse_or_review_drafts
- 23. `downloads/folders/Spart Toeic Quyển 2/Lời thoại (transcript) Sách Sparta TOEIC LC+RC.pdf` | VALID_TEXT_EXTRACTED | pages=37 | manual=not_started | next=parse_or_review_drafts
- 24. `downloads/folders/Spart Toeic Quyển 2/Sách Sparta TOEIC LCRC.pdf` | VALID_TEXT_EXTRACTED | pages=210 | manual=not_started | next=parse_or_review_drafts
- 25. `downloads/folders/Spart Toeic Quyển 2/Đáp án (answer key) Sách Sparta TOEIC LC & RC.pdf` | VALID_TEXT_EXTRACTED | pages=3 | manual=not_started | next=parse_or_review_drafts
- 26. `downloads/folders/Sparta Toeic/Lời thoại (transcript) Sách Sparta TOEIC.pdf` | VALID_TEXT_EXTRACTED | pages=70 | manual=not_started | next=parse_or_review_drafts
- 27. `downloads/folders/Sparta Toeic/Sách Sparta TOEIC - Phần nghe.pdf` | VALID_TEXT_EXTRACTED | pages=139 | manual=not_started | next=parse_or_review_drafts
- 28. `downloads/folders/Sparta Toeic/Đáp án (answer key) Sách Sparta TOEIC - Phần nghe.pdf` | VALID_TEXT_EXTRACTED | pages=3 | manual=not_started | next=parse_or_review_drafts
- 29. `downloads/folders/Sparta Toeic/Đáp án (answer key) Sách Sparta TOEIC - Phần đọc.pdf` | VALID_TEXT_EXTRACTED | pages=3 | manual=not_started | next=parse_or_review_drafts
- 30. `downloads/folders/TACTICS FOR TOEIC/Tactics for TOEIC - Answer Key.pdf` | IMAGE_PDF_OCR_OR_MANUAL_REQUIRED | pages=77 | manual=not_started | next=manual_or_ocr_extract

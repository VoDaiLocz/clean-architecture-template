#!/usr/bin/env python3
"""Parse Fire TOEIC 1000 Part 5 text corpus into validated JSONL rows."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_INPUT = ROOT / "data/pdf-text-corpus/001-1000-c-u-gi-i-d-toeic-format-m-i-2019-pdf.txt"
DEFAULT_OUTPUT = ROOT / "data/manual-extraction/fire-1000-part5-from-txt.jsonl"
DEFAULT_REJECT_OUTPUT = ROOT / "data/manual-extraction/fire-1000-part5-from-txt-rejected.jsonl"
SOURCE_ORIGINAL = "downloads/1000 CÂU GIẢI ĐỀ TOEIC FORMAT MỚI 2019.pdf"
LEFT_COLUMN_WIDTH = 60
VIETNAMESE_CHARS = "ăâđêôơưáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵĂÂĐÊÔƠƯÁÀẢÃẠẤẦẨẪẬẮẰẲẴẶÉÈẺẼẸẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌỐỒỔỖỘỚỜỞỠỢÚÙỦŨỤỨỪỬỮỰÝỲỶỸỴ"


QUESTION_START = re.compile(r"^\s*(\d{1,4})\s+(.+)")
OPTION_LINE = re.compile(r"^\s*\(([A-D])\)\s+(.+)")
ANSWER_PATTERN = re.compile(r"Đáp án\s+([A-D])", re.IGNORECASE)
NOISE_PATTERNS = [
    re.compile(r"^\s*Câu\s+Đề bài\s+Giải thích chi tiết\s*$", re.IGNORECASE),
    re.compile(r"^\s*Tài liệu được biên soạn bởi", re.IGNORECASE),
    re.compile(r"^\s*Lửa TOEIC", re.IGNORECASE),
    re.compile(r"^\s*fire\s*$", re.IGNORECASE),
    re.compile(r"^\s*toe\s*$", re.IGNORECASE),
    re.compile(r"^\s*ic@\s*$", re.IGNORECASE),
    re.compile(r"^\s*ed\s*$", re.IGNORECASE),
    re.compile(r"^\s*u\.v\s*$", re.IGNORECASE),
    re.compile(r"^\s*n\s*$", re.IGNORECASE),
]
QUALITY_REJECT_PATTERN = re.compile(
    "[" + re.escape(VIETNAMESE_CHARS) + r"]|\b(Dịch|Mẹo|Cần|Đáp|dấu hiệu|câu này|Ta có|Chọn|nghĩa|dịch)\b",
    re.IGNORECASE,
)


def is_noise(line: str) -> bool:
    return any(pattern.search(line) for pattern in NOISE_PATTERNS)


def left_column(line: str) -> str:
    cut_candidates: list[int] = []
    for marker in ["Đáp", "Dịch", "Dấu", "Cần", "Sau ", "A. ", "B. ", "C. ", "D. "]:
        position = line.find(marker)
        if position >= 40:
            cut_candidates.append(position)

    for index, character in enumerate(line):
        if index >= 44 and character in VIETNAMESE_CHARS:
            previous_gap = line.rfind("  ", 0, index)
            cut_candidates.append(previous_gap if previous_gap >= 35 else index)
            break

    cut_at = min(cut_candidates) if cut_candidates else LEFT_COLUMN_WIDTH
    return line[:cut_at].rstrip()


def clean_text(value: str) -> str:
    value = value.replace("\x0c", " ")
    value = re.sub(r"\s+", " ", value)
    return value.strip(" -\t")


def split_pages(text: str) -> list[str]:
    return text.split("\f")


def collect_blocks(pages: list[str]) -> list[tuple[int, list[str]]]:
    blocks: list[tuple[int, list[str]]] = []
    current_page = 1
    current: list[str] = []

    for page_index, page in enumerate(pages, start=1):
        for raw_line in page.splitlines():
            line = raw_line.rstrip()
            if not line.strip() or is_noise(line):
                continue

            match = QUESTION_START.match(line)
            if match:
                number = int(match.group(1))
                if 1 <= number <= 1000 and current:
                    blocks.append((current_page, current))
                    current = []
                if 1 <= number <= 1000:
                    current_page = page_index
                    current.append(line)
                    continue

            if current:
                current.append(line)

    if current:
        blocks.append((current_page, current))
    return blocks


def parse_block(page_number: int, lines: list[str]) -> dict | None:
    first = QUESTION_START.match(lines[0])
    if not first:
        return None

    number = int(first.group(1))
    if any(re.match(r"^\s*\d{1,4}\s*$", line) for line in lines[1:]):
        return None
    answer = None
    first_left = left_column(lines[0])
    first_left = QUESTION_START.match(first_left).group(2) if QUESTION_START.match(first_left) else first.group(2)
    prompt_lines = [first_left]
    options: dict[str, str] = {}
    active_option: str | None = None

    for raw_line in lines:
        answer_match = ANSWER_PATTERN.search(raw_line)
        if answer_match:
            answer = answer_match.group(1).upper()

    for raw_line in lines[1:]:
        line = left_column(raw_line)
        option_match = OPTION_LINE.match(line)
        if option_match:
            active_option = option_match.group(1)
            options[active_option] = option_match.group(2).strip()
            continue

        if active_option and len(options) < 4:
            continuation = line.strip()
            if continuation and not ANSWER_PATTERN.search(raw_line):
                options[active_option] = clean_text(f"{options[active_option]} {continuation}")
            continue

        if not options:
            prompt_lines.append(line)

    prompt = clean_text(" ".join(prompt_lines))
    options = {key: clean_text(value) for key, value in options.items() if clean_text(value)}

    if not answer or set(options) != {"A", "B", "C", "D"} or not prompt:
        return None

    return {
        "sourceFile": "downloads/1000 CAU GIAI DE TOEIC FORMAT MOI 2019.pdf",
        "sourceFileOriginalName": SOURCE_ORIGINAL,
        "sourceTest": "FIRE TOEIC 1000 PART 5",
        "toeicPart": 5,
        "questionNumber": number,
        "sourcePdfPage": page_number,
        "sourcePrintedPage": page_number,
        "answerEvidencePdfPage": page_number,
        "prompt": prompt,
        "options": options,
        "correctAnswer": answer,
        "extractionMethod": "txt_manual_parser_fire_part5_v1",
        "confidence": "high",
    }


def quality_issues(item: dict) -> list[str]:
    issues: list[str] = []
    text = item["prompt"] + " " + " ".join(item["options"].values())
    if QUALITY_REJECT_PATTERN.search(text):
        issues.append("mixed_explanation_or_vietnamese_text")
    if len(item["prompt"]) > 260:
        issues.append("prompt_too_long")
    for key, value in item["options"].items():
        if len(value) > 80:
            issues.append(f"option_{key.lower()}_too_long")
    if "--" not in item["prompt"] and "____" not in item["prompt"]:
        issues.append("missing_blank_marker")
    return issues


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", default=str(DEFAULT_INPUT))
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT))
    parser.add_argument("--reject-output", default=str(DEFAULT_REJECT_OUTPUT))
    args = parser.parse_args()

    text = Path(args.input).read_text(encoding="utf-8", errors="ignore")
    pages = split_pages(text)
    blocks = collect_blocks(pages)
    parsed = [item for page, block in blocks if (item := parse_block(page, block))]
    parsed.sort(key=lambda item: item["questionNumber"])

    accepted = []
    rejected = []
    for item in parsed:
        issues = quality_issues(item)
        if issues:
            rejected.append({**item, "qualityIssues": issues})
        else:
            accepted.append(item)

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(
        "".join(json.dumps(item, ensure_ascii=False) + "\n" for item in accepted),
        encoding="utf-8",
    )
    reject_output = Path(args.reject_output)
    reject_output.parent.mkdir(parents=True, exist_ok=True)
    reject_output.write_text(
        "".join(json.dumps(item, ensure_ascii=False) + "\n" for item in rejected),
        encoding="utf-8",
    )

    print(f"FIRE_PART5_PARSE blocks={len(blocks)} parsed={len(parsed)} accepted={len(accepted)} rejected={len(rejected)} output={output}")
    missing = sorted(set(range(1, 1001)) - {item["questionNumber"] for item in parsed})
    if missing:
        print(f"MISSING_COUNT {len(missing)}")
        print("MISSING_FIRST", ",".join(map(str, missing[:80])))
    return 0 if len(accepted) >= 800 else 2


if __name__ == "__main__":
    raise SystemExit(main())

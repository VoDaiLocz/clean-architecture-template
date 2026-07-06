#!/usr/bin/env python3
"""Parse Sparta TOEIC LC+RC Reading Test 1 Part 6/7 into validated JSONL rows."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_BOOK = ROOT / "data/pdf-text-corpus/024-s-ch-sparta-toeic-lcrc-pdf.txt"
DEFAULT_ANSWER_KEY = ROOT / "data/pdf-text-corpus/025-d-p-n-answer-key-s-ch-sparta-toeic-lc-rc-pdf.txt"
DEFAULT_OUTPUT = ROOT / "data/manual-extraction/sparta-lcrc-test1-part6-7-from-txt.jsonl"
DEFAULT_REJECT_OUTPUT = ROOT / "data/manual-extraction/sparta-lcrc-test1-part6-7-from-txt-rejected.jsonl"
SOURCE_ORIGINAL = "downloads/folders/Spart Toeic Quyển 2/Sách Sparta TOEIC LCRC.pdf"

GROUP_HEADER = re.compile(
    r"Questions?\s+(\d{3})\s*-\s*(\d{3})\s+refer to the following[^\n]*",
    re.IGNORECASE,
)
QUESTION_START = re.compile(r"^\s*(\d{3})\s*[\.\)]\s*(.+)")
OPTION_START = re.compile(r"^\s*[\(\[]([A-D0O8])[\)\]I]\s*(.*)", re.IGNORECASE)
ANSWER_PAIR = re.compile(r"(\d{1,3})\s*\.\s*([^\s]+)")
NOISE_LINE = re.compile(
    r"^\s*(Go on to the next page|PART[67]|Directions:|-----|SOURCE_|QUEUE_|PAGE_COUNT|EXTRACTION_CLASS)",
    re.IGNORECASE,
)


def clean(value: str) -> str:
    value = value.replace("\x0c", " ")
    value = value.replace("RE3turn", "return")
    value = re.sub(r"[~§■•]+", " ", value)
    value = re.sub(r"\s+", " ", value)
    return value.strip(" -\t")


def clean_option(value: str) -> str:
    value = clean(value)
    value = re.sub(r"\s+(3[0-9]|4[0-9]|5[0-9])$", "", value)
    return value.strip()


def normalize_label(value: str) -> str:
    value = value.upper()
    if value == "8":
        return "B"
    if value in {"0", "O"}:
        return "D"
    return value


def normalize_answer_token(token: str) -> str | None:
    token = token.upper()
    if "A" in token:
        return "A"
    if "B" in token:
        return "B"
    if "C" in token:
        return "C"
    if "D" in token or "O" in token:
        return "D"
    return None


def parse_test1_answers(answer_key_text: str) -> dict[int, str]:
    answers: dict[int, str] = {}
    first_table = answer_key_text.split("TESTJ", 1)[0]
    for line in first_table.splitlines():
        pairs = ANSWER_PAIR.findall(line)
        if not pairs:
            continue

        # The PDF text has TEST 1 in the left half and TEST 2 in the right half.
        # When both halves are present, the second half repeats the same numbers.
        first_half = pairs
        if len(pairs) >= 10 and pairs[0][0] == pairs[len(pairs) // 2][0]:
            first_half = pairs[: len(pairs) // 2]

        for number_text, token in first_half:
            number = int(number_text)
            if 101 <= number <= 200 and number not in answers:
                answer = normalize_answer_token(token)
                if answer:
                    answers[number] = answer

    missing = [number for number in range(101, 201) if number not in answers]
    if missing:
        raise ValueError(f"Missing TEST 1 answers: {missing[:20]}")
    return answers


def reading_test_one(text: str) -> str:
    start = text.find("Questions 131-134")
    if start < 0:
        raise ValueError("Cannot find Sparta Reading Test 1 Part 6 start.")
    end = text.find("LISTENING TEST", start)
    if end < 0:
        raise ValueError("Cannot find end of Sparta Reading Test 1.")
    return text[start:end]


def split_group_chunks(test_text: str) -> list[tuple[int, int, str, str]]:
    headers = list(GROUP_HEADER.finditer(test_text))
    chunks: list[tuple[int, int, str, str]] = []
    for index, header in enumerate(headers):
        start_q = int(header.group(1))
        end_q = int(header.group(2))
        chunk_end = headers[index + 1].start() if index + 1 < len(headers) else len(test_text)
        chunks.append((start_q, end_q, header.group(0), test_text[header.end() : chunk_end]))
    return chunks


def split_columns(line: str) -> list[str]:
    for pattern in (
        r"\s{2,}(?=\d{3}\s*[\.\)])",
        r"\s{6,}(?=[\(\[][A-D0O8][\)\]I])",
    ):
        match = re.search(pattern, line[35:])
        if match:
            split_at = 35 + match.start()
            return [line[:split_at], line[split_at:]]
    if len(line) <= 70:
        return [line]
    return [line[:60], line[60:]]


def parse_question_columns(lines: list[str]) -> dict[int, dict]:
    questions: dict[int, dict] = {}

    for column_index in (0, 1):
        current_number: int | None = None
        current_option: str | None = None
        for raw_line in lines:
            parts = split_columns(raw_line)
            if column_index >= len(parts):
                continue
            segment = parts[column_index].rstrip()
            if not segment.strip() or NOISE_LINE.match(segment):
                continue

            question_match = QUESTION_START.match(segment)
            if question_match:
                current_number = int(question_match.group(1))
                current_option = None
                questions.setdefault(current_number, {"prompt": [], "options": {}})
                remainder = question_match.group(2).strip()
                option_match = OPTION_START.match(remainder)
                if option_match:
                    label = normalize_label(option_match.group(1))
                    current_option = label
                    questions[current_number]["options"][label] = clean(option_match.group(2))
                else:
                    questions[current_number]["prompt"].append(remainder)
                continue

            option_match = OPTION_START.match(segment)
            if option_match and current_number is not None:
                label = normalize_label(option_match.group(1))
                current_option = label
                questions[current_number]["options"][label] = clean(option_match.group(2))
                continue

            if current_number is None:
                continue

            continuation = clean(segment)
            if not continuation:
                continue
            if re.fullmatch(r"\d{1,3}", continuation):
                continue
            if current_option:
                previous = questions[current_number]["options"].get(current_option, "")
                questions[current_number]["options"][current_option] = clean(f"{previous} {continuation}")
            else:
                questions[current_number]["prompt"].append(continuation)

    return questions


def parse_group(start_q: int, end_q: int, header: str, chunk: str, answers: dict[int, str]) -> tuple[list[dict], list[dict]]:
    part = 6 if start_q <= 146 else 7
    if part == 6:
        chunk = chunk.split("PART7", 1)[0]
    if part == 6:
        first_question = re.search(rf"(?m)^\s*{start_q}\s*[\.\)]\s*[\(\[]?[A-D0O8]", chunk)
    else:
        first_question = re.search(rf"(?m)^\s*{start_q}\s*[\.\)]", chunk)
    if not first_question:
        return [], [{"group": f"{start_q}-{end_q}", "reason": "missing_question_section"}]

    passage_text = clean(f"{header}. {chunk[: first_question.start()]}")
    question_lines = chunk[first_question.start() :].splitlines()
    parsed_questions = parse_question_columns(question_lines)

    accepted: list[dict] = []
    rejected: list[dict] = []
    group_id = f"sparta-lcrc-test1-p{part}-{start_q}-{end_q}"
    for number in range(start_q, end_q + 1):
        question = parsed_questions.get(number)
        if not question:
            rejected.append({"questionNumber": number, "groupId": group_id, "reason": "missing_question"})
            continue

        prompt = clean(" ".join(question["prompt"]))
        if part == 6:
            prompt = f"Complete blank ({number}) in the passage."
        options = {key: clean_option(value) for key, value in question["options"].items() if clean_option(value)}
        item = {
            "sourceFile": SOURCE_ORIGINAL,
            "sourceFileOriginalName": SOURCE_ORIGINAL,
            "sourceTest": "SPARTA TOEIC LC+RC TEST 1",
            "toeicPart": part,
            "questionNumber": number,
            "sourcePdfPage": 0,
            "sourcePrintedPage": 0,
            "answerEvidencePdfPage": 1,
            "prompt": prompt,
            "options": options,
            "correctAnswer": answers.get(number),
            "extractionMethod": "txt_manual_parser_sparta_lcrc_test1_v1",
            "confidence": "high",
            "groupId": group_id,
            "passageType": header,
            "passageText": passage_text,
        }
        issues = quality_issues(item, start_q, end_q)
        if issues:
            rejected.append({**item, "qualityIssues": issues})
        else:
            accepted.append(item)
    return accepted, rejected


def quality_issues(item: dict, start_q: int, end_q: int) -> list[str]:
    issues: list[str] = []
    if item["questionNumber"] < start_q or item["questionNumber"] > end_q:
        issues.append("question_outside_group")
    if item["correctAnswer"] not in {"A", "B", "C", "D"}:
        issues.append("missing_answer")
    if set(item["options"]) != {"A", "B", "C", "D"}:
        issues.append("missing_options")
    if len(item["passageText"]) < 120:
        issues.append("passage_too_short")
    if item["toeicPart"] == 7 and len(item["prompt"]) < 12:
        issues.append("prompt_too_short")
    if "ANSWER Key" in item["passageText"] or "correctAnswer" in item["passageText"]:
        issues.append("answer_leak")
    if re.search(r"Questions?\s+\d{3}\s*-\s*\d{3}", " ".join(item["options"].values())):
        issues.append("next_group_leaked_into_options")
    return issues


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--book", default=str(DEFAULT_BOOK))
    parser.add_argument("--answer-key", default=str(DEFAULT_ANSWER_KEY))
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT))
    parser.add_argument("--reject-output", default=str(DEFAULT_REJECT_OUTPUT))
    args = parser.parse_args()

    book_text = Path(args.book).read_text(encoding="utf-8", errors="ignore")
    answer_key_text = Path(args.answer_key).read_text(encoding="utf-8", errors="ignore")
    answers = parse_test1_answers(answer_key_text)
    test_text = reading_test_one(book_text)
    groups = split_group_chunks(test_text)

    accepted: list[dict] = []
    rejected: list[dict] = []
    for start_q, end_q, header, chunk in groups:
        group_accepted, group_rejected = parse_group(start_q, end_q, header, chunk, answers)
        accepted.extend(group_accepted)
        rejected.extend(group_rejected)

    accepted.sort(key=lambda item: item["questionNumber"])
    rejected.sort(key=lambda item: item.get("questionNumber", 0))

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

    expected = set(range(131, 201))
    parsed = {item["questionNumber"] for item in accepted}
    missing = sorted(expected - parsed)
    part6 = sum(1 for item in accepted if item["toeicPart"] == 6)
    part7 = sum(1 for item in accepted if item["toeicPart"] == 7)
    print(
        "SPARTA_LCRC_TEST1_PARSE "
        f"groups={len(groups)} accepted={len(accepted)} rejected={len(rejected)} "
        f"part6={part6} part7={part7} missing={len(missing)} output={output}"
    )
    if missing:
        print("MISSING", ",".join(map(str, missing)))
    if rejected:
        print(f"REJECT_OUTPUT {reject_output}")
    return 0 if len(accepted) == 70 and not rejected and part6 == 16 and part7 == 54 else 2


if __name__ == "__main__":
    raise SystemExit(main())

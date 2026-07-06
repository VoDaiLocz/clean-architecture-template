#!/usr/bin/env python3
"""Audit downloaded TOEIC PDFs for real Part 1-4 listening content readiness."""

from __future__ import annotations

import json
import re
import subprocess
from collections import Counter
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
QUEUE_PATH = ROOT / "data/pdf-processing/pdf-processing-queue.json"
TEXT_CORPUS = ROOT / "data/pdf-text-corpus"
REPORT_PATH = ROOT / "data/pdf-processing/listening-part-source-audit.md"

PART_PATTERNS = {
    1: re.compile(r"\bpart\s*1\b|part1|photographs?", re.IGNORECASE),
    2: re.compile(r"\bpart\s*2\b|part2|question[- ]response", re.IGNORECASE),
    3: re.compile(r"\bpart\s*3\b|part3|conversations?", re.IGNORECASE),
    4: re.compile(r"\bpart\s*4\b|part4|short talks?|talks?", re.IGNORECASE),
}
LISTENING_PATTERN = re.compile(r"listening test|part\s*1|part\s*2|part\s*3|part\s*4", re.IGNORECASE)
TRANSCRIPT_PATTERN = re.compile(r"transcript|lời thoại|scripts?", re.IGNORECASE)
ANSWER_KEY_PATTERN = re.compile(r"answer key|đáp án|TEST\s*1", re.IGNORECASE)
QUESTION_RANGE_PATTERN = re.compile(
    r"Questions?\s+\d{1,3}\s+(?:through|[-–])\s+\d{1,3}|^\s*\d{1,3}\.\s+",
    re.IGNORECASE | re.MULTILINE,
)


def read_text_corpus() -> list[dict]:
    entries: list[dict] = []
    for path in sorted(TEXT_CORPUS.glob("*.txt")):
        text = path.read_text(encoding="utf-8", errors="ignore")
        source = ""
        name = ""
        for line in text.splitlines()[:6]:
            if line.startswith("SOURCE_RELATIVE_PATH:"):
                source = line.split(":", 1)[1].strip()
            if line.startswith("SOURCE_FILE_NAME:"):
                name = line.split(":", 1)[1].strip()
        entries.append({"path": path, "source": source, "name": name, "text": text})
    return entries


def audio_inventory() -> dict:
    audio_exts = {".mp3", ".wav", ".m4a", ".ogg", ".aac", ".flac", ".wma"}
    direct_audio = [path for path in (ROOT / "downloads").rglob("*") if path.is_file() and path.suffix.lower() in audio_exts]
    zip_files = sorted((ROOT / "downloads").rglob("*.zip"))
    mp4_files = sorted((ROOT / "downloads").rglob("*.mp4"))

    invalid_zips: list[str] = []
    for path in zip_files:
        try:
            header = path.read_bytes()[:16]
        except OSError:
            header = b""
        if not header.startswith(b"PK"):
            invalid_zips.append(str(path.relative_to(ROOT)))

    valid_mp4_audio = 0
    invalid_mp4 = 0
    for path in mp4_files:
        result = subprocess.run(
            [
                "ffprobe",
                "-v",
                "error",
                "-select_streams",
                "a",
                "-show_entries",
                "stream=codec_name",
                "-of",
                "csv=p=0",
                str(path),
            ],
            text=True,
            capture_output=True,
            timeout=5,
            check=False,
        )
        if result.returncode == 0 and result.stdout.strip():
            valid_mp4_audio += 1
        else:
            invalid_mp4 += 1

    return {
        "directAudio": len(direct_audio),
        "zipFiles": len(zip_files),
        "invalidZipFiles": invalid_zips,
        "mp4Files": len(mp4_files),
        "validMp4Audio": valid_mp4_audio,
        "invalidMp4": invalid_mp4,
    }


def image_count(pdf_path: str) -> int | None:
    absolute = ROOT / pdf_path
    if not absolute.exists():
        return None
    result = subprocess.run(
        ["pdfimages", "-list", str(absolute)],
        text=True,
        capture_output=True,
        timeout=30,
        check=False,
    )
    if result.returncode != 0:
        return None
    return sum(1 for line in result.stdout.splitlines() if re.search(r"\bimage\b", line))


def classify_entries(entries: list[dict]) -> list[dict]:
    rows: list[dict] = []
    for entry in entries:
        text = entry["text"]
        part_hits = {part: len(pattern.findall(text)) for part, pattern in PART_PATTERNS.items()}
        if not any(part_hits.values()) and not LISTENING_PATTERN.search(text):
            continue

        source = entry["source"]
        name = entry["name"] or Path(source).name
        role: list[str] = []
        lower = f"{source} {name}".lower()
        if "transcript" in lower or "lời thoại" in lower or "scripts" in lower:
            role.append("transcript")
        if "answer key" in lower or "đáp án" in lower:
            role.append("answer_key")
        question_book_name = re.search(
            r"listening|nghe|sách sparta|lcrc|toeic analyst|toeic preparation|tactics|đề nghe|sách listening",
            lower,
            re.IGNORECASE,
        )
        if not role and (question_book_name or re.search(r"LISTENING TEST", text[:2500], re.IGNORECASE)):
            role.append("question_book")
        if not role:
            role.append("strategy_or_reference")

        rows.append(
            {
                "source": source,
                "name": name,
                "textFile": str(entry["path"].relative_to(ROOT)),
                "roles": sorted(set(role)),
                "partHits": part_hits,
                "questionMarkers": len(QUESTION_RANGE_PATTERN.findall(text)),
            }
        )
    return rows


def queue_summary() -> tuple[Counter, list[dict]]:
    queue = json.loads(QUEUE_PATH.read_text(encoding="utf-8"))
    counter = Counter(item["extractionClass"] for item in queue)
    listening_scan = [
        item
        for item in queue
        if item["extractionClass"] == "IMAGE_PDF_OCR_OR_MANUAL_REQUIRED"
        and re.search(r"listening|nghe|tactics|toeic|transcript", item["relativePath"], re.IGNORECASE)
    ]
    return counter, listening_scan


def render_report(rows: list[dict], audio: dict, queue_counts: Counter, image_scan: list[dict]) -> str:
    strong = [
        row
        for row in rows
        if {"question_book", "transcript", "answer_key"}.intersection(row["roles"])
    ]
    role_weight = {"question_book": 3, "transcript": 2, "answer_key": 2}
    strong.sort(
        key=lambda row: (
            -sum(role_weight.get(role, 0) for role in row["roles"]),
            -(sum(row["partHits"].values()) + row["questionMarkers"]),
            row["source"],
        )
    )

    sparta_images = image_count("downloads/folders/Sparta Toeic/Sách Sparta TOEIC - Phần nghe.pdf")

    lines = [
        "# Listening Part 1-4 Source Audit",
        "",
        "## Conclusion",
        "",
        "- The downloaded PDFs do contain real TOEIC Part 1-4 material.",
        "- The strongest complete text bundle is `Sparta Toeic`: listening question book, transcript, and answer key are all text-extracted.",
        "- Part 1 also has embedded images in `Sách Sparta TOEIC - Phần nghe.pdf`; `pdfimages` detected "
        + (str(sparta_images) if sparta_images is not None else "unknown")
        + " image objects in that PDF.",
        "- No publishable listening runtime item should be marked complete until a valid audio asset is linked.",
        "",
        "## Audio Readiness",
        "",
        f"- Direct audio files found: `{audio['directAudio']}`",
        f"- Zip files found: `{audio['zipFiles']}`",
        f"- Zip files that are HTML/placeholders, not real zip archives: `{len(audio['invalidZipFiles'])}`",
        f"- MP4 files found: `{audio['mp4Files']}`",
        f"- MP4 files with readable audio stream: `{audio['validMp4Audio']}`",
        f"- MP4 files invalid or without readable audio: `{audio['invalidMp4']}`",
        "",
        "## PDF Queue State",
        "",
    ]
    for key, value in sorted(queue_counts.items()):
        lines.append(f"- `{key}`: `{value}`")

    lines.extend(
        [
            "",
        "## Strong Part 1-4 Candidates From Text Corpus",
            "",
            "| Source | Roles | Part hits | Question markers | Text file |",
            "|---|---:|---:|---:|---|",
        ]
    )
    for row in strong[:30]:
        part_hits = ", ".join(f"P{part}:{count}" for part, count in row["partHits"].items() if count)
        lines.append(
            f"| `{row['source']}` | `{', '.join(row['roles'])}` | `{part_hits or 'none'}` | "
            f"`{row['questionMarkers']}` | `{row['textFile']}` |"
        )

    lines.extend(
        [
            "",
            "## Best Production Bundles",
            "",
            "1. `downloads/folders/Sparta Toeic/`",
            "   - Question book: `Sách Sparta TOEIC - Phần nghe.pdf`",
            "   - Transcript: `Lời thoại (transcript) Sách Sparta TOEIC.pdf`",
            "   - Answer key: `Đáp án (answer key) Sách Sparta TOEIC - Phần nghe.pdf`",
            "   - Status: text and images are available; audio is missing.",
            "2. `downloads/folders/Spart Toeic Quyển 2/`",
            "   - Question book: `Sách Sparta TOEIC LCRC.pdf`",
            "   - Transcript: `Lời thoại (transcript) Sách Sparta TOEIC LC+RC.pdf`",
            "   - Answer key: `Đáp án (answer key) Sách Sparta TOEIC LC & RC.pdf`",
            "   - Status: text is available; audio is missing.",
            "3. `downloads/folders/TOEIC Preparation LC + RC Volume 1, 2/` and duplicated `downloads/folders/Thư mục/`",
            "   - Script/answer file: `TPLCRC2-ScriptsAK.pdf`",
            "   - Status: text is available; the downloaded audio zip files are placeholders, not usable archives.",
            "",
        ]
    )

    lines.extend(
        [
            "",
            "## Image/OCR Listening Candidates",
            "",
            "These are likely listening-related PDFs but currently require OCR/manual extraction before reliable parsing.",
            "",
            "| Source | Pages | Next action |",
            "|---|---:|---|",
        ]
    )
    for item in image_scan[:30]:
        lines.append(f"| `{item['relativePath']}` | `{item['pageCount']}` | `{item['nextAction']}` |")

    lines.extend(
        [
            "",
            "## Production Interpretation",
            "",
            "- `Part 1-4 content exists`: yes.",
            "- `Part 1-4 ready to publish as TOEIC listening practice`: no, because valid audio is missing.",
            "- Safe next implementation: create listening drafts from Sparta as `BlockedMissingAudio`, then import/publish only after audio is re-downloaded or linked.",
        ]
    )
    return "\n".join(lines) + "\n"


def main() -> int:
    entries = read_text_corpus()
    rows = classify_entries(entries)
    audio = audio_inventory()
    queue_counts, image_scan = queue_summary()
    REPORT_PATH.write_text(render_report(rows, audio, queue_counts, image_scan), encoding="utf-8")
    print(f"LISTENING_AUDIT rows={len(rows)} report={REPORT_PATH}")
    print(
        "AUDIO "
        f"direct={audio['directAudio']} zip={audio['zipFiles']} invalidZip={len(audio['invalidZipFiles'])} "
        f"mp4={audio['mp4Files']} validMp4Audio={audio['validMp4Audio']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

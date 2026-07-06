#!/usr/bin/env python3
"""Build a processing queue for downloaded TOEIC PDF files.

The queue is a production control artifact, not learner content. It keeps the
manual/OCR extraction work honest by accounting for every downloaded PDF.
"""

from __future__ import annotations

import csv
import json
import os
import sqlite3
import subprocess
import sys
import tempfile
from dataclasses import asdict, dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DOWNLOADS = ROOT / "downloads"
DB_PATH = ROOT / "backend/src/Api/toeic-normalization.db"
OUT_DIR = ROOT / "data/pdf-processing"


@dataclass(frozen=True)
class PdfQueueEntry:
    order: int
    relativePath: str
    fileName: str
    sizeBytes: int
    isPdfHeader: bool
    pdfInfoStatus: str
    pageCount: int
    textSampleChars: int
    extractionClass: str
    sourceAssetId: str
    dbExtractedPages: int
    dbTextBlocks: int
    manualStatus: str
    nextAction: str


def run(command: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, text=True, capture_output=True, check=False)


def read_pdf_page_count(path: Path) -> tuple[str, int]:
    result = run(["pdfinfo", str(path)])
    if result.returncode != 0:
        return "invalid_pdfinfo", 0

    for line in result.stdout.splitlines():
        if line.startswith("Pages:"):
            try:
                return "ok", int(line.split(":", 1)[1].strip())
            except ValueError:
                return "invalid_page_count", 0

    return "missing_page_count", 0


def read_text_sample_chars(path: Path, page_count: int) -> int:
    if page_count <= 0:
        return 0

    last_page = min(page_count, 3)
    with tempfile.NamedTemporaryFile(suffix=".txt") as output:
        result = run([
            "pdftotext",
            "-f",
            "1",
            "-l",
            str(last_page),
            "-layout",
            str(path),
            output.name,
        ])
        if result.returncode != 0:
            return 0

        try:
            text = Path(output.name).read_text(encoding="utf-8", errors="ignore")
        except OSError:
            return 0

    return len("".join(text.split()))


def load_db_asset_state() -> dict[str, tuple[str, int, int]]:
    if not DB_PATH.exists():
        return {}

    connection = sqlite3.connect(DB_PATH)
    try:
        rows = connection.execute(
            """
            SELECT
                source_assets.file_name,
                source_assets.asset_id,
                COUNT(DISTINCT extracted_pages.page_id) AS page_count,
                COUNT(DISTINCT extracted_text_blocks.block_id) AS block_count
            FROM source_assets
            LEFT JOIN extracted_pages ON extracted_pages.asset_id = source_assets.asset_id
            LEFT JOIN extracted_text_blocks ON extracted_text_blocks.asset_id = source_assets.asset_id
            GROUP BY source_assets.asset_id, source_assets.file_name
            """
        ).fetchall()
    finally:
        connection.close()

    return {
        file_name: (asset_id, int(page_count), int(block_count))
        for file_name, asset_id, page_count, block_count in rows
    }


def classify_entry(
    is_pdf_header: bool,
    pdf_info_status: str,
    page_count: int,
    text_sample_chars: int,
    source_asset_id: str,
    db_extracted_pages: int,
    db_text_blocks: int,
) -> tuple[str, str]:
    if not is_pdf_header or pdf_info_status != "ok":
        return "PLACEHOLDER_OR_INVALID_PDF", "blocked_invalid_pdf"

    if text_sample_chars == 0:
        if db_text_blocks == 0:
            return "IMAGE_PDF_OCR_OR_MANUAL_REQUIRED", "manual_or_ocr_extract"
        return "IMAGE_PDF_DB_TEXT_AVAILABLE", "review_existing_blocks"

    if not source_asset_id:
        return "VALID_TEXT_PDF_NOT_REGISTERED", "register_source_asset"

    if db_extracted_pages == 0:
        return "VALID_TEXT_PDF_NOT_EXTRACTED", "run_pdf_block_extraction"

    if db_text_blocks == 0:
        return "VALID_TEXT_PDF_PAGES_WITHOUT_TEXT_BLOCKS", "run_text_block_extraction"

    return "VALID_TEXT_EXTRACTED", "parse_or_review_drafts"


def manual_status(relative_path: str) -> str:
    manual_root = ROOT / "data/manual-extraction"
    if not manual_root.exists():
        return "not_started"

    normalized = (
        relative_path.lower()
        .replace("/", "-")
        .replace(" ", "-")
        .replace("đ", "d")
    )
    for path in manual_root.glob("*.jsonl"):
        if "de-doc-1" in path.name and "ĐỀ ĐỌC (1).pdf" in relative_path:
            return "partial"
        if normalized in path.name:
            return "partial"

    return "not_started"


def build_queue() -> list[PdfQueueEntry]:
    db_state = load_db_asset_state()
    entries: list[PdfQueueEntry] = []
    pdf_paths = sorted(
        path for path in DOWNLOADS.rglob("*")
        if path.is_file() and path.suffix.lower() == ".pdf"
    )

    for index, path in enumerate(pdf_paths, start=1):
        relative_path = path.relative_to(ROOT).as_posix()
        size_bytes = path.stat().st_size
        is_pdf_header = path.read_bytes()[:4] == b"%PDF"
        pdf_info_status, page_count = read_pdf_page_count(path) if is_pdf_header else ("invalid_header", 0)
        text_sample_chars = read_text_sample_chars(path, page_count)
        source_asset_id, db_pages, db_blocks = db_state.get(path.name, ("", 0, 0))
        extraction_class, next_action = classify_entry(
            is_pdf_header,
            pdf_info_status,
            page_count,
            text_sample_chars,
            source_asset_id,
            db_pages,
            db_blocks,
        )
        entries.append(PdfQueueEntry(
            order=index,
            relativePath=relative_path,
            fileName=path.name,
            sizeBytes=size_bytes,
            isPdfHeader=is_pdf_header,
            pdfInfoStatus=pdf_info_status,
            pageCount=page_count,
            textSampleChars=text_sample_chars,
            extractionClass=extraction_class,
            sourceAssetId=source_asset_id,
            dbExtractedPages=db_pages,
            dbTextBlocks=db_blocks,
            manualStatus=manual_status(relative_path),
            nextAction=next_action,
        ))

    return entries


def write_outputs(entries: list[PdfQueueEntry]) -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    json_path = OUT_DIR / "pdf-processing-queue.json"
    csv_path = OUT_DIR / "pdf-processing-queue.csv"
    summary_path = OUT_DIR / "pdf-processing-summary.md"

    json_path.write_text(
        json.dumps([asdict(entry) for entry in entries], ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )

    with csv_path.open("w", newline="", encoding="utf-8") as output:
        writer = csv.DictWriter(output, fieldnames=list(asdict(entries[0]).keys()) if entries else [])
        writer.writeheader()
        for entry in entries:
            writer.writerow(asdict(entry))

    by_class: dict[str, int] = {}
    for entry in entries:
        by_class[entry.extractionClass] = by_class.get(entry.extractionClass, 0) + 1

    summary_lines = [
        "# PDF Processing Queue",
        "",
        "Generated from local `downloads/` and `backend/src/Api/toeic-normalization.db`.",
        "",
        f"- Total PDF-like files: {len(entries)}",
        f"- Total valid pages: {sum(entry.pageCount for entry in entries if entry.isPdfHeader and entry.pdfInfoStatus == 'ok')}",
        "",
        "## By Extraction Class",
        "",
    ]
    for key, count in sorted(by_class.items()):
        summary_lines.append(f"- `{key}`: {count}")

    summary_lines.extend([
        "",
        "## Processing Rule",
        "",
        "1. Invalid/placeholder PDFs stay blocked.",
        "2. Text PDFs go through source registration, block extraction, parser, validation, review, publish.",
        "3. Image PDFs require OCR or manual extraction before parser/publish.",
        "4. Manual rows must include source page and answer evidence page.",
        "",
        "## Next Queue Items",
        "",
    ])
    for entry in entries[:30]:
        summary_lines.append(
            f"- {entry.order}. `{entry.relativePath}` | {entry.extractionClass} | pages={entry.pageCount} | manual={entry.manualStatus} | next={entry.nextAction}"
        )

    summary_path.write_text("\n".join(summary_lines) + "\n", encoding="utf-8")

    print(f"Wrote {json_path}")
    print(f"Wrote {csv_path}")
    print(f"Wrote {summary_path}")


def main() -> int:
    if not DOWNLOADS.exists():
        print(f"downloads folder not found: {DOWNLOADS}", file=sys.stderr)
        return 1

    entries = build_queue()
    write_outputs(entries)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

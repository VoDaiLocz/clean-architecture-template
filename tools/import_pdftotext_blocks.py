#!/usr/bin/env python3
"""Import local text-PDF pages into SQLite using poppler pdftotext.

This is a pragmatic batch extractor for local corpus ingestion. It writes one
text block per PDF page, preserving source asset and page provenance. Scanned
PDFs still need OCR/manual extraction and are intentionally skipped.
"""

from __future__ import annotations

import argparse
import json
import sqlite3
import subprocess
import tempfile
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DB_PATH = ROOT / "backend/src/Api/toeic-normalization.db"
QUEUE_PATH = ROOT / "data/pdf-processing/pdf-processing-queue.json"


def run_pdftotext(path: Path, first_page: int, last_page: int, output_path: Path) -> bool:
    result = subprocess.run(
        [
            "pdftotext",
            "-f",
            str(first_page),
            "-l",
            str(last_page),
            "-layout",
            str(path),
            str(output_path),
        ],
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode != 0:
        print(f"FAIL pdftotext | {path} | {result.stderr.strip()}")
        return False
    return True


def import_asset(connection: sqlite3.Connection, entry: dict, root: Path) -> tuple[int, int]:
    asset_id = entry["sourceAssetId"]
    expected_pages = int(entry["pageCount"])
    existing_pages, existing_blocks = connection.execute(
        """
        SELECT COUNT(DISTINCT extracted_pages.page_id), COUNT(DISTINCT extracted_text_blocks.block_id)
        FROM extracted_pages
        LEFT JOIN extracted_text_blocks ON extracted_text_blocks.asset_id = extracted_pages.asset_id
        WHERE extracted_pages.asset_id = ?
        """,
        (asset_id,),
    ).fetchone()
    if existing_pages >= expected_pages and existing_blocks > 0:
        print(f"SKIP complete | {asset_id} | {entry['fileName']}")
        return 0, 0

    path = root / entry["relativePath"]
    if not path.exists():
        print(f"FAIL missing-file | {entry['relativePath']}")
        return 0, 0

    imported_pages = 0
    imported_blocks = 0
    now = datetime.now(timezone.utc).isoformat()
    print(f"START pdftotext | order={entry['order']} | {asset_id} | {entry['fileName']} | pages={expected_pages}")
    for page_number in range(1, expected_pages + 1):
        with tempfile.NamedTemporaryFile(suffix=".txt") as output:
            if not run_pdftotext(path, page_number, page_number, Path(output.name)):
                continue
            text = Path(output.name).read_text(encoding="utf-8", errors="ignore").strip()

        page_id = f"extracted-page-{asset_id}-{page_number}"
        connection.execute(
            """
            INSERT INTO extracted_pages(page_id, asset_id, page_number, width, height, extracted_at_utc)
            VALUES (?, ?, ?, 0, 0, ?)
            ON CONFLICT(page_id) DO UPDATE SET
                width = excluded.width,
                height = excluded.height,
                extracted_at_utc = excluded.extracted_at_utc
            """,
            (page_id, asset_id, page_number, now),
        )
        imported_pages += 1

        if not text:
            continue

        block_id = f"extracted-block-{asset_id}-{page_number}-pdftotext"
        connection.execute(
            """
            INSERT INTO extracted_text_blocks(block_id, asset_id, page_id, page_number, block_type, text, confidence, coordinates_json)
            VALUES (?, ?, ?, ?, 'Unknown', ?, 0.95, ?)
            ON CONFLICT(block_id) DO UPDATE SET
                text = excluded.text,
                confidence = excluded.confidence,
                coordinates_json = excluded.coordinates_json
            """,
            (
                block_id,
                asset_id,
                page_id,
                page_number,
                text,
                json.dumps({"method": "pdftotext-layout-page"}),
            ),
        )
        imported_blocks += 1

    connection.commit()
    print(f"DONE pdftotext | {asset_id} | pages={imported_pages} blocks={imported_blocks}")
    return imported_pages, imported_blocks


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--queue", default=str(QUEUE_PATH))
    parser.add_argument("--db", default=str(DB_PATH))
    parser.add_argument("--max-assets", type=int, default=5)
    args = parser.parse_args()

    queue = json.loads(Path(args.queue).read_text(encoding="utf-8"))
    targets = [
        entry
        for entry in queue
        if entry.get("nextAction") == "run_pdf_block_extraction"
        and entry.get("sourceAssetId")
    ]

    connection = sqlite3.connect(args.db)
    processed = 0
    total_pages = 0
    total_blocks = 0
    try:
        for entry in targets:
            if processed >= args.max_assets:
                break
            pages, blocks = import_asset(connection, entry, ROOT)
            if pages or blocks:
                processed += 1
                total_pages += pages
                total_blocks += blocks
    finally:
        connection.close()

    print(f"PDFTOTEXT_IMPORT assets={processed} pages={total_pages} blocks={total_blocks}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

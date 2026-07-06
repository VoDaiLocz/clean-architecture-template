#!/usr/bin/env python3
"""Export downloaded TOEIC PDFs into plain text files.

This creates the intermediate corpus the data pipeline should parse/review
before creating draft questions. It intentionally separates "PDF can be read"
from "question is valid and publishable".
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
QUEUE_PATH = ROOT / "data/pdf-processing/pdf-processing-queue.json"
OUT_DIR = ROOT / "data/pdf-text-corpus"


def slugify(value: str) -> str:
    value = value.lower().replace("đ", "d")
    value = re.sub(r"[^a-z0-9]+", "-", value)
    return value.strip("-")[:120]


def run_pdftotext(pdf_path: Path, txt_path: Path) -> bool:
    txt_path.parent.mkdir(parents=True, exist_ok=True)
    result = subprocess.run(
        ["pdftotext", "-layout", str(pdf_path), str(txt_path)],
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode != 0:
        txt_path.write_text(
            f"PDFTOTEXT_FAILED\n{result.stderr}\n",
            encoding="utf-8",
        )
        return False
    return True


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--queue", default=str(QUEUE_PATH))
    parser.add_argument("--max-files", type=int, default=9999)
    parser.add_argument(
        "--include-scan",
        action="store_true",
        help="Also create placeholder txt files for image/scan PDFs.",
    )
    args = parser.parse_args()

    queue = json.loads(Path(args.queue).read_text(encoding="utf-8"))
    exported = 0
    placeholders = 0
    failed = 0

    for entry in queue:
        if exported + placeholders >= args.max_files:
            break

        extraction_class = entry["extractionClass"]
        if extraction_class == "PLACEHOLDER_OR_INVALID_PDF":
            continue
        if extraction_class == "IMAGE_PDF_OCR_OR_MANUAL_REQUIRED" and not args.include_scan:
            continue

        pdf_path = ROOT / entry["relativePath"]
        txt_path = OUT_DIR / f"{entry['order']:03d}-{slugify(entry['fileName'])}.txt"

        header = [
            f"SOURCE_RELATIVE_PATH: {entry['relativePath']}",
            f"SOURCE_FILE_NAME: {entry['fileName']}",
            f"QUEUE_ORDER: {entry['order']}",
            f"EXTRACTION_CLASS: {extraction_class}",
            f"PAGE_COUNT: {entry['pageCount']}",
            "",
            "----- BEGIN PDF TEXT -----",
            "",
        ]

        if extraction_class == "IMAGE_PDF_OCR_OR_MANUAL_REQUIRED":
            txt_path.parent.mkdir(parents=True, exist_ok=True)
            txt_path.write_text(
                "\n".join(header)
                + "SCAN_OR_IMAGE_PDF_REQUIRES_OCR_OR_MANUAL_TRANSCRIPTION\n",
                encoding="utf-8",
            )
            placeholders += 1
            print(f"PLACEHOLDER {txt_path}")
            continue

        temp_path = txt_path.with_suffix(".body.tmp")
        ok = run_pdftotext(pdf_path, temp_path)
        body = temp_path.read_text(encoding="utf-8", errors="ignore") if temp_path.exists() else ""
        if temp_path.exists():
            temp_path.unlink()

        txt_path.write_text("\n".join(header) + body, encoding="utf-8")
        if ok:
            exported += 1
            print(f"EXPORTED {txt_path}")
        else:
            failed += 1
            print(f"FAILED {txt_path}")

    print(f"PDF_TEXT_EXPORT exported={exported} placeholders={placeholders} failed={failed}")
    return 0 if failed == 0 else 2


if __name__ == "__main__":
    raise SystemExit(main())

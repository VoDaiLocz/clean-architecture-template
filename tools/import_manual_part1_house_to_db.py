#!/usr/bin/env python3
"""Import manually verified Taking the TOEIC 1 Part 1 A House manifest into DB."""

from __future__ import annotations

import hashlib
import json
import sqlite3
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DB_PATH = ROOT / "backend/src/Api/toeic-normalization.db"
MANIFEST_PATH = ROOT / "data/manual-extraction/taking-toeic-1/part1-house/items.jsonl"

SOURCE_ID = "manual-taking-toeic-1-part1-house"
CONTAINER_ID = "manual-container-taking-toeic-1-part1-house"


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def import_source(connection: sqlite3.Connection) -> None:
    connection.execute(
        """
        INSERT INTO source_manifest_entries(
            source_id, sheet_row_number, title, url, provider, source_type,
            material_class, access_status, has_pdf, has_audio, has_image,
            has_transcript, has_answer_key, audit_notes
        )
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        ON CONFLICT(source_id) DO UPDATE SET
            title = excluded.title,
            has_audio = excluded.has_audio,
            has_image = excluded.has_image,
            has_transcript = excluded.has_transcript,
            has_answer_key = excluded.has_answer_key,
            audit_notes = excluded.audit_notes
        """,
        (
            SOURCE_ID,
            200_001,
            "Taking the TOEIC Skills and Strategies 1 - Part 1 A House manual extraction",
            "file://downloads/noinoi/Taking the TOEIC - Skills and Strategies 1.pdf",
            "Unknown",
            "Other",
            "TestBook",
            "Accessible",
            1,
            1,
            1,
            1,
            1,
            "Manual extraction from question pages, transcript page, answer key page, and local MP3 assets.",
        ),
    )
    connection.execute(
        """
        INSERT OR REPLACE INTO source_containers(
            container_id, source_id, provider, external_id, title, access_status, discovered_at_utc
        )
        VALUES (?, ?, ?, ?, ?, ?, ?)
        """,
        (
            CONTAINER_ID,
            SOURCE_ID,
            "Unknown",
            "data/manual-extraction/taking-toeic-1/part1-house",
            "Taking the TOEIC 1 Part 1 A House manual media set",
            "Accessible",
            "2026-07-07T00:00:00+00:00",
        ),
    )


def upsert_image_asset(connection: sqlite3.Connection, row: dict) -> str:
    image_path = ROOT / row["imageRelativePath"]
    if not image_path.exists():
        raise FileNotFoundError(image_path)

    question_number = int(row["questionNumber"])
    asset_id = f"manual-image-taking-toeic-1-part1-house-q{question_number:03d}"
    connection.execute(
        """
        INSERT INTO source_assets(
            asset_id, container_id, source_id, file_name, mime_type, extension,
            size_bytes, detected_role, provider_url, object_key, checksum
        )
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        ON CONFLICT(asset_id) DO UPDATE SET
            file_name = excluded.file_name,
            size_bytes = excluded.size_bytes,
            checksum = excluded.checksum,
            object_key = excluded.object_key
        """,
        (
            asset_id,
            CONTAINER_ID,
            SOURCE_ID,
            image_path.name,
            "image/jpeg",
            ".jpg",
            image_path.stat().st_size,
            "Image",
            f"file://{row['imageRelativePath']}",
            row["imageRelativePath"],
            sha256_file(image_path),
        ),
    )
    return asset_id


def upsert_draft(connection: sqlite3.Connection, row: dict, image_asset_id: str) -> None:
    question_number = int(row["questionNumber"])
    audio_asset = connection.execute(
        "SELECT asset_id FROM source_assets WHERE asset_id = ? AND detected_role = 'Audio'",
        (row["audioAssetId"],),
    ).fetchone()
    if audio_asset is None:
        raise ValueError(f"Missing audio asset for question {question_number}: {row['audioAssetId']}")

    payload = {
        "schemaVersion": "toeic-draft.v1",
        "kind": "ListeningQuestion",
        "data": {
            "groupId": "taking-toeic-1-part1-house",
            "questionNumber": question_number,
            "prompt": "Listen and choose the statement that best describes what you see in the picture.",
            "skillTags": ["part-1", "photographs", "house", "manual-verified"],
            "parserPayload": {
                "options": row["choices"],
                "correctAnswer": row["correctAnswer"],
                "explanation": f"Correct statement: {row['correctAnswerText']}",
                "audioAssetId": row["audioAssetId"],
                "imageAssetId": image_asset_id,
                "trackNumber": row["trackNumber"],
            },
        },
    }
    source_trace = {
        "source": row["source"],
        "section": row["section"],
        "pdfPage": row["pdfPage"],
        "bookPage": row["bookPage"],
        "audioAssetId": row["audioAssetId"],
        "imageAssetId": image_asset_id,
        "manualEvidence": row["manualEvidence"],
    }
    connection.execute(
        """
        INSERT INTO draft_content_items(
            draft_id, asset_id, material_class, toeic_part, item_type,
            payload_json, source_trace_json, parser_confidence, status
        )
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        ON CONFLICT(draft_id) DO UPDATE SET
            payload_json = excluded.payload_json,
            source_trace_json = excluded.source_trace_json,
            parser_confidence = excluded.parser_confidence,
            status = CASE
                WHEN draft_content_items.status IN ('Approved', 'Published') THEN draft_content_items.status
                ELSE excluded.status
            END
        """,
        (
            f"draft-manual-taking-toeic-1-part1-house-q{question_number:03d}",
            row["audioAssetId"],
            "TestBook",
            1,
            "ListeningQuestion",
            json.dumps(payload, ensure_ascii=False, separators=(",", ":")),
            json.dumps(source_trace, ensure_ascii=False, separators=(",", ":")),
            1.0,
            "PendingValidation",
        ),
    )


def main() -> int:
    rows = [
        json.loads(line)
        for line in MANIFEST_PATH.read_text(encoding="utf-8").splitlines()
        if line.strip()
    ]
    now = datetime.now(timezone.utc).isoformat()
    with sqlite3.connect(DB_PATH) as connection:
        import_source(connection)
        image_count = 0
        draft_count = 0
        for row in rows:
            image_asset_id = upsert_image_asset(connection, row)
            upsert_draft(connection, row, image_asset_id)
            image_count += 1
            draft_count += 1
        connection.commit()

        db_image_count = connection.execute(
            "SELECT COUNT(*) FROM source_assets WHERE source_id = ? AND detected_role = 'Image'",
            (SOURCE_ID,),
        ).fetchone()[0]
        db_draft_count = connection.execute(
            "SELECT COUNT(*) FROM draft_content_items WHERE draft_id LIKE 'draft-manual-taking-toeic-1-part1-house-q%'",
        ).fetchone()[0]

    print(
        "MANUAL_PART1_HOUSE_DB_IMPORT "
        f"input={len(rows)} imageAssets={image_count} drafts={draft_count} "
        f"dbImageAssets={db_image_count} dbDrafts={db_draft_count} at={now}"
    )
    return 0 if db_image_count == len(rows) and db_draft_count == len(rows) else 2


if __name__ == "__main__":
    raise SystemExit(main())

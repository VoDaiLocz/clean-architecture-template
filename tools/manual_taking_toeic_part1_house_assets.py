#!/usr/bin/env python3
"""Create manually verified Part 1 A House assets from Taking the TOEIC 1.

This is intentionally not an OCR/parser. The coordinates and text below were
read manually from the scanned book pages:

- Question pages: PDF pages 14-17, book pages 16-19.
- Transcript page: PDF page 240, transcript page 4.
- Answer key page: PDF page 255, answer key page 19.
"""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
RENDERED_PAGE_DIR = Path("/tmp/toeic-taking1-pages")
OUT_DIR = ROOT / "data/manual-extraction/taking-toeic-1/part1-house"
IMAGE_DIR = OUT_DIR / "images"
MANIFEST_PATH = OUT_DIR / "items.jsonl"

AUDIO_BASE = (
    "downloads/noinoi/Audio Taking the TOEIC 1-20260706T075154Z-3-001/"
    "Audio Taking the TOEIC  1"
)


def short_hash(value: str) -> str:
    return hashlib.sha256(value.encode("utf-8")).hexdigest()[:16]


ITEMS = [
    {
        "questionNumber": 1,
        "pdfPage": 14,
        "bookPage": 16,
        "crop": [220, 375, 435, 295],
        "choices": {
            "A": "She is vacuuming the floor.",
            "B": "She is lifting a table.",
            "C": "She is sitting on the floor.",
            "D": "She is watching TV.",
        },
        "correctAnswer": "A",
    },
    {
        "questionNumber": 2,
        "pdfPage": 14,
        "bookPage": 16,
        "crop": [220, 760, 435, 295],
        "choices": {
            "A": "A woman is chopping a vegetable.",
            "B": "Plants are sitting on shelves.",
            "C": "Dishes are sitting in a sink.",
            "D": "A woman is washing some vegetables.",
        },
        "correctAnswer": "A",
    },
    {
        "questionNumber": 3,
        "pdfPage": 14,
        "bookPage": 16,
        "crop": [220, 1085, 435, 420],
        "choices": {
            "A": "A man is stacking some dishes.",
            "B": "A man is stirring the food in a pot.",
            "C": "Two men are peeling potatoes.",
            "D": "Some men are cutting some vegetables.",
        },
        "correctAnswer": "B",
    },
    {
        "questionNumber": 4,
        "pdfPage": 15,
        "bookPage": 17,
        "crop": [155, 378, 435, 300],
        "choices": {
            "A": "A man is cutting the grass.",
            "B": "Some people are planting flowers.",
            "C": "Some people are shoveling dirt.",
            "D": "A woman is holding a watering can.",
        },
        "correctAnswer": "D",
    },
    {
        "questionNumber": 5,
        "pdfPage": 15,
        "bookPage": 17,
        "crop": [155, 765, 435, 290],
        "choices": {
            "A": "The cake is next to the young boy at the end.",
            "B": "A woman is holding a large cake.",
            "C": "People are eating dessert in the dining room.",
            "D": "People are sitting down for a meal.",
        },
        "correctAnswer": "D",
    },
    {
        "questionNumber": 6,
        "pdfPage": 15,
        "bookPage": 17,
        "crop": [155, 1138, 435, 300],
        "choices": {
            "A": "The woman is opening the refrigerator.",
            "B": "The woman is wiping a counter.",
            "C": "The woman is filling some bottles.",
            "D": "The woman is putting on rubber gloves.",
        },
        "correctAnswer": "B",
    },
    {
        "questionNumber": 7,
        "pdfPage": 16,
        "bookPage": 18,
        "crop": [225, 378, 430, 295],
        "choices": {
            "A": "A woman is reaching for a handle.",
            "B": "A woman is pressing a button.",
            "C": "A woman is opening a cabinet.",
            "D": "A woman is installing an appliance.",
        },
        "correctAnswer": "B",
    },
    {
        "questionNumber": 8,
        "pdfPage": 16,
        "bookPage": 18,
        "crop": [220, 765, 430, 295],
        "choices": {
            "A": "A sofa is placed in front of a window.",
            "B": "A lamp has been turned on.",
            "C": "Some books have been opened.",
            "D": "Some pillows are stacked on a table.",
        },
        "correctAnswer": "A",
    },
    {
        "questionNumber": 9,
        "pdfPage": 16,
        "bookPage": 18,
        "crop": [220, 1148, 430, 315],
        "choices": {
            "A": "A woman is washing a dish.",
            "B": "A woman is opening a window.",
            "C": "Some dishes have been stacked on a counter.",
            "D": "Some pots are lined up along a window.",
        },
        "correctAnswer": "A",
    },
    {
        "questionNumber": 10,
        "pdfPage": 17,
        "bookPage": 19,
        "crop": [175, 378, 430, 300],
        "choices": {
            "A": "A man is rolling up some blinds.",
            "B": "A woman is setting the table.",
            "C": "A man is lighting some candles.",
            "D": "A woman is moving a chair.",
        },
        "correctAnswer": "B",
    },
    {
        "questionNumber": 11,
        "pdfPage": 17,
        "bookPage": 19,
        "crop": [238, 700, 305, 450],
        "choices": {
            "A": "A ladder is propped against a building.",
            "B": "A hat is sitting on a ladder.",
            "C": "A man is putting on some gloves.",
            "D": "A man is repairing a light fixture.",
        },
        "correctAnswer": "D",
    },
    {
        "questionNumber": 12,
        "pdfPage": 17,
        "bookPage": 19,
        "crop": [175, 1145, 430, 300],
        "choices": {
            "A": "Tables are pushed against a window.",
            "B": "A lamp is sitting on a coffee table.",
            "C": "A pillow has been placed on a chair.",
            "D": "A chair is placed by a window.",
        },
        "correctAnswer": "D",
    },
]


def crop_image(item: dict) -> str:
    page_path = RENDERED_PAGE_DIR / f"page-{item['pdfPage']:03d}.png"
    if not page_path.exists():
        raise FileNotFoundError(f"Rendered page missing: {page_path}")

    image = Image.open(page_path)
    x, y, width, height = item["crop"]
    cropped = image.crop((x, y, x + width, y + height))
    output_name = f"taking-toeic-1-part1-house-q{item['questionNumber']:03d}.jpg"
    output_path = IMAGE_DIR / output_name
    IMAGE_DIR.mkdir(parents=True, exist_ok=True)
    cropped.save(output_path, quality=92)
    return str(output_path.relative_to(ROOT))


def main() -> int:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    rows = []

    for item in ITEMS:
        question_number = item["questionNumber"]
        track_number = f"{question_number:03d}"
        audio_relative_path = f"{AUDIO_BASE}/{track_number} Track{track_number}.mp3"
        image_relative_path = crop_image(item)
        audio_asset_id = f"local-asset-{short_hash(f'noinoi/{AUDIO_BASE.split('noinoi/', 1)[-1]}/{track_number} Track{track_number}.mp3')}"

        rows.append(
            {
                "source": "Taking the TOEIC Skills and Strategies 1",
                "section": "Chapter 1 Listening Practice / Part 1 Photographs / Mini-Tests / A House",
                "toeicPart": 1,
                "questionNumber": question_number,
                "pdfPage": item["pdfPage"],
                "bookPage": item["bookPage"],
                "trackNumber": track_number,
                "audioRelativePath": audio_relative_path,
                "audioAssetId": audio_asset_id,
                "imageRelativePath": image_relative_path,
                "choices": item["choices"],
                "correctAnswer": item["correctAnswer"],
                "correctAnswerText": item["choices"][item["correctAnswer"]],
                "manualEvidence": {
                    "questionPage": "PDF pages 14-17 / book pages 16-19",
                    "transcriptPage": "PDF page 240 / transcript page 4",
                    "answerKeyPage": "PDF page 255 / answer key page 19",
                },
                "validationStatus": "manual_verified",
            }
        )

    with MANIFEST_PATH.open("w", encoding="utf-8") as output:
        for row in rows:
            output.write(json.dumps(row, ensure_ascii=False) + "\n")

    print(f"MANUAL_PART1_HOUSE items={len(rows)} manifest={MANIFEST_PATH}")
    print(f"IMAGE_DIR {IMAGE_DIR}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

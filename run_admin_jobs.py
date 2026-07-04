import urllib.request
import sqlite3
import json

db_path = "backend/src/Api/toeic-normalization.db"
conn = sqlite3.connect(db_path)
cur = conn.cursor()

# Get all test book asset IDs
cur.execute("SELECT asset_id FROM source_assets WHERE detected_role IN ('Pdf', 'TestBook')")
assets = [row[0] for row in cur.fetchall()]

print(f"Found {len(assets)} TestBook/Pdf assets.")

for asset_id in assets:
    print(f"Triggering parsing for asset: {asset_id}")
    url = f"http://localhost:5000/api/admin/source-assets/{asset_id}/parse-reading-drafts"
    req = urllib.request.Request(url, method='POST')
    try:
        with urllib.request.urlopen(req) as response:
            res_body = response.read().decode('utf-8')
            print(f"Result: {res_body}")
    except Exception as e:
        print(f"Failed for {asset_id}: {e}")

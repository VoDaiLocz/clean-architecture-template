#!/usr/bin/env python3
"""
Download tất cả files trong 38 Google Drive folders
Dùng Drive API v2internal với cookies auth từ Chrome Profile 19
"""

import browser_cookie3, requests, re, os, time, json
from urllib.parse import urlencode

BASE_OUT = '/home/vodailoc/toeci/downloads/folders'
os.makedirs(BASE_OUT, exist_ok=True)

API_KEY = os.environ.get('GOOGLE_API_KEY', '')

FOLDER_IDS = [
    '10uLpcqXViW3N_aOeHl_dGU92qJS2DTwR',
    '113MXRc_PsjHxX4pApI2sTbTNC1_kMs1t',
    '13rf_l2MmyxnLflhhOBQf0LLKRcvJ2X5E',
    '151setuOegLHfX8-lSB4T2QWzoBVTpNtz',
    '18S0sUppub8XBsGRA5FpwOu8Gqd1qZ2kU',
    '195o9EzC9LM8Biz0li4sXKSuDm_Q9WnW_',
    '19KJZzdamDyI2AMB8TozY9QJYcO3YZqH9',
    '19rZVeIno69yhmhri1HDzH_aYaRsty7--',
    '1AYEOt_cGfbmOMOhrg_VO_9ZoRE7h5QdU',
    '1AuaOzjgbP05TSMbgZEYEPIQcmSWug7Hd',
    '1C4z3G5x9JCheSVgcCGGMV-ax7_MwbtKX',
    '1CAvXhJ2cpNhpjnxh2z3_Qkdq5VOSfahx',
    '1Dcp9jizA0J4bIuiHjiqheZK9O1y7nelP',
    '1Dxkqq-ata3WAt4IwZGcfVKkPKgjNLvku',
    '1F9PSE7Ue6-4HPFCwh_QkjDDJ_MiPksGI',
    '1GEEo00wZNhRy45ZMZg4BGVdn1foh8_WS',
    '1K_wD7LZXPL8qMVvzpKFTorIKb8oLb-Vv',
    '1Lpyv1YbizxjlAI410GTc-To0i92hTZjf',
    '1M-nduM72brK9EB363Y1SjxxIttVsZjHq',
    '1Mvpp-jQkh6ovja4tJSw-axsT8VVlX41B',
    '1NVdN5RCP4CVRy5ZQpPMHc7H0jIM42jxF',
    '1O3tV3kMiCkpAPcvmr8PQiUh9eq4myMQq',
    '1QJzcF4wlRppJglbI0FFLic7OtvHX-9Yn',
    '1RUzAFA4d4Krufyz8ED9siLfCz3qvvEGP',
    '1UAzrC4qmNMdJWVmPcU-EHsHGCojKfMZy',
    '1UE1A3I3hmoY1EcMVMPCT2R7Y3TBCoqfq',
    '1VPdgG_UhhgnUlzrT-axnPtAzaLZJzXBT',
    '1_M5bM5EN1gMAgiLh7oMDeJ1dHKmPd7Mk',
    '1ahxUwHqe0Xw5Zh-aCxLQ08L_KMT-78vs',
    '1amL4HxxLrsNtSFKA2u5d5pXmeW0dIGBH',
    '1k_u5rtv8cecWNmJe3HyeVc9iPES-s2Il',
    '1mciUmcjs64nmEDR6iKB7VGvup2bEmtLc',
    '1oHUHYyEQ0T5H-rl_fXHMjljV4lGKCRB-',
    '1pnkrjkBoYovk-hYnAdCCav03yMdiafSi',
    '1t-3gsjblXZ5VW4WNkY5ECwtYSQQ2-JHj',
    '1v2hRKTU40DZrTPE4tEafVwZMgrOZD1TZ',
    '1vZBhP53Sp5wyMCPX7mqZcnMXohYdCvAM',
    '1znIXD0xTxnak3JqR_ShZHGRZELtLUH3U',
]

def get_session():
    cj = browser_cookie3.chrome(
        domain_name='.google.com',
        cookie_file='/home/vodailoc/.config/google-chrome/Profile 19/Cookies'
    )
    s = requests.Session()
    for c in cj:
        s.cookies.set(c.name, c.value, domain=c.domain)
    return s

def list_folder(session, folder_id, headers):
    """List files in a folder using Drive API v2."""
    url = 'https://www.googleapis.com/drive/v2/files'
    params = {
        'q': f"'{folder_id}' in parents and trashed=false",
        'fields': 'items(id,title,mimeType,fileSize,downloadUrl)',
        'maxResults': 1000,
        'key': API_KEY,
    }
    resp = session.get(url, params=params, headers=headers, timeout=15)
    if resp.status_code == 200:
        return resp.json().get('items', [])

    # Fallback: Drive v2internal
    url2 = f'https://clients6.google.com/drive/v2internal/files'
    params2 = {
        'q': f"'{folder_id}' in parents and trashed=false",
        'fields': 'items(id,title,mimeType)',
        'maxResults': 1000,
        'supportsTeamDrives': 'true',
        'includeTeamDriveItems': 'true',
        'key': API_KEY,
    }
    resp2 = session.get(url2, params=params2, headers=headers, timeout=15)
    if resp2.status_code == 200:
        return resp2.json().get('items', [])

    return None

def get_folder_title(session, folder_id, headers):
    """Lấy tên folder."""
    try:
        url = f'https://drive.google.com/drive/folders/{folder_id}'
        resp = session.get(url, headers=headers, timeout=10)
        m = re.search(r'<title>([^<]+)</title>', resp.text)
        if m:
            title = m.group(1).replace(' - Google Drive', '').strip()
            if title and title not in ('Google Drive', 'Error'):
                return re.sub(r'[<>:"/\\|?*\x00-\x1f]', '_', title)
    except:
        pass
    return folder_id[:12]

def download_file(session, file_id, fname, dest_dir, headers):
    """Tải một file."""
    url = f'https://drive.usercontent.google.com/download?id={file_id}&export=download&authuser=0&confirm=t'
    try:
        resp = session.get(url, headers=headers, stream=True, timeout=60)
        if resp.status_code != 200 or 'text/html' in resp.headers.get('Content-Type', ''):
            url2 = f'https://drive.google.com/uc?export=download&id={file_id}&confirm=t'
            resp = session.get(url2, headers=headers, stream=True, timeout=60)

        if resp.status_code != 200:
            return False, f'HTTP {resp.status_code}'

        fpath = os.path.join(dest_dir, fname)
        if os.path.exists(fpath):
            return True, f'Already exists: {fname}'  # skip

        size = 0
        with open(fpath, 'wb') as f:
            for chunk in resp.iter_content(65536):
                if chunk:
                    f.write(chunk)
                    size += len(chunk)

        if size < 1000:
            os.remove(fpath)
            return False, f'Too small ({size}b)'

        return True, f'{fname} ({size/1024:.0f} KB)'
    except Exception as e:
        return False, str(e)

def export_google_doc(session, file_id, title, mime, dest_dir, headers):
    """Export Google Docs/Sheets/Slides as PDF."""
    if 'spreadsheet' in mime:
        url = f'https://docs.google.com/spreadsheets/d/{file_id}/export?format=pdf'
        ext = '.pdf'
    elif 'presentation' in mime:
        url = f'https://docs.google.com/presentation/d/{file_id}/export?format=pdf'
        ext = '.pdf'
    else:  # document
        url = f'https://docs.google.com/document/d/{file_id}/export?format=pdf'
        ext = '.pdf'

    fname = re.sub(r'[<>:"/\\|?*\x00-\x1f]', '_', title) + ext
    fpath = os.path.join(dest_dir, fname)
    if os.path.exists(fpath):
        return True, f'Already exists: {fname}'

    try:
        resp = session.get(url, headers=headers, stream=True, timeout=30)
        if resp.status_code == 200 and 'pdf' in resp.headers.get('Content-Type', '').lower():
            size = 0
            with open(fpath, 'wb') as f:
                for chunk in resp.iter_content(65536):
                    if chunk:
                        f.write(chunk)
                        size += len(chunk)
            return True, f'{fname} ({size/1024:.0f} KB)'
        return False, f'HTTP {resp.status_code}'
    except Exception as e:
        return False, str(e)

def process_folder(session, folder_id, headers, depth=0):
    """Xử lý 1 folder: list → tải files."""
    indent = '  ' * depth
    folder_name = get_folder_title(session, folder_id, headers)
    dest_dir = os.path.join(BASE_OUT, folder_name)
    os.makedirs(dest_dir, exist_ok=True)

    print(f'{indent}📂 {folder_name}/')

    items = list_folder(session, folder_id, headers)
    if items is None:
        print(f'{indent}  ❌ Không thể list folder (403/404)')
        return 0, 0

    if not items:
        print(f'{indent}  (Folder trống hoặc không có quyền xem)')
        return 0, 0

    ok, fail = 0, 0
    for item in items:
        fid = item.get('id', '')
        title = re.sub(r'[<>:"/\\|?*\x00-\x1f]', '_', item.get('title', 'unknown'))
        mime = item.get('mimeType', '')

        # Subfolder
        if mime == 'application/vnd.google-apps.folder':
            sub_ok, sub_fail = process_folder(session, fid, headers, depth + 1)
            ok += sub_ok
            fail += sub_fail
            continue

        # Google Docs/Sheets/Slides
        if 'google-apps' in mime:
            success, msg = export_google_doc(session, fid, title, mime, dest_dir, headers)
        else:
            # Regular file
            success, msg = download_file(session, fid, title, dest_dir, headers)

        if success:
            print(f'{indent}  ✅ {msg}')
            ok += 1
        else:
            print(f'{indent}  ❌ {title}: {msg}')
            fail += 1

        time.sleep(0.3)

    return ok, fail

def main():
    print('=' * 65)
    print(f'Tải nội dung {len(FOLDER_IDS)} Google Drive folders')
    print(f'Output: {BASE_OUT}')
    print('=' * 65)

    session = get_session()
    headers = {
        'User-Agent': 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/126.0.0.0 Safari/537.36'
    }

    total_ok, total_fail = 0, 0

    for i, fid in enumerate(FOLDER_IDS, 1):
        print(f'\n[{i}/{len(FOLDER_IDS)}] Folder ID: {fid}')
        ok, fail = process_folder(session, fid, headers)
        total_ok += ok
        total_fail += fail
        time.sleep(0.5)

    print(f'\n{"=" * 65}')
    print(f'✅ Tổng tải được: {total_ok} files')
    print(f'❌ Tổng lỗi: {total_fail} files')

    # Thống kê
    total_size = 0
    file_count = 0
    for root, dirs, files in os.walk(BASE_OUT):
        for f in files:
            fp = os.path.join(root, f)
            total_size += os.path.getsize(fp)
            file_count += 1

    print(f'\n📁 Tổng: {file_count} files, {total_size/1024/1024:.1f} MB')
    print(f'📂 Lưu tại: {BASE_OUT}')

if __name__ == '__main__':
    main()

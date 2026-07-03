#!/usr/bin/env python3
"""
Dùng Playwright Python để vào từng Google Drive folder,
lấy file IDs từ HTML page, rồi tải xuống.
"""

import asyncio, re, os, time, json
import browser_cookie3, requests
from playwright.async_api import async_playwright

BASE_OUT = '/home/vodailoc/toeci/downloads/folders'
os.makedirs(BASE_OUT, exist_ok=True)

FOLDER_IDS = [
    ('10uLpcqXViW3N_aOeHl_dGU92qJS2DTwR', ''),
    ('113MXRc_PsjHxX4pApI2sTbTNC1_kMs1t', ''),
    ('13rf_l2MmyxnLflhhOBQf0LLKRcvJ2X5E', 'TACTICS FOR TOEIC'),
    ('151setuOegLHfX8-lSB4T2QWzoBVTpNtz', '05. DEVELOPING SKILL TOEIC'),
    ('18S0sUppub8XBsGRA5FpwOu8Gqd1qZ2kU', 'TOEIC Preparation LC+RC Vol 1-2'),
    ('195o9EzC9LM8Biz0li4sXKSuDm_Q9WnW_', ''),
    ('19KJZzdamDyI2AMB8TozY9QJYcO3YZqH9', 'ABC TOEIC'),
    ('19rZVeIno69yhmhri1HDzH_aYaRsty7--', 'Bài Giảng'),
    ('1AYEOt_cGfbmOMOhrg_VO_9ZoRE7h5QdU', '06. ANALYST TOEIC'),
    ('1AuaOzjgbP05TSMbgZEYEPIQcmSWug7Hd', 'TOEIC - BÀI LUYỆN TẬP THEO THANG ĐIỂM'),
    ('1C4z3G5x9JCheSVgcCGGMV-ax7_MwbtKX', '09. LONGMAN TOEIC NEW REAL'),
    ('1CAvXhJ2cpNhpjnxh2z3_Qkdq5VOSfahx', '01. VERY EASY TOEIC'),
    ('1Dcp9jizA0J4bIuiHjiqheZK9O1y7nelP', 'Taking the TOEIC Skills and Strategies 2'),
    ('1Dxkqq-ata3WAt4IwZGcfVKkPKgjNLvku', 'ĐỀ WRITING SAMPLE'),
    ('1F9PSE7Ue6-4HPFCwh_QkjDDJ_MiPksGI', ''),
    ('1GEEo00wZNhRy45ZMZg4BGVdn1foh8_WS', ''),
    ('1K_wD7LZXPL8qMVvzpKFTorIKb8oLb-Vv', '08. TARGET TOEIC'),
    ('1Lpyv1YbizxjlAI410GTc-To0i92hTZjf', '03. STARTER TOEIC'),
    ('1M-nduM72brK9EB363Y1SjxxIttVsZjHq', 'Sách Tomato TOEIC'),
    ('1Mvpp-jQkh6ovja4tJSw-axsT8VVlX41B', 'Tài liệu tự học SW'),
    ('1NVdN5RCP4CVRy5ZQpPMHc7H0jIM42jxF', 'HỌC THEO PART có giải thích'),
    ('1O3tV3kMiCkpAPcvmr8PQiUh9eq4myMQq', 'New TOEIC 700'),
    ('1QJzcF4wlRppJglbI0FFLic7OtvHX-9Yn', 'Bộ xanh cam TOEIC vol 2'),
    ('1RUzAFA4d4Krufyz8ED9siLfCz3qvvEGP', 'YBM TOEIC'),
    ('1UAzrC4qmNMdJWVmPcU-EHsHGCojKfMZy', 'Khóa học 40 tuyệt chiêu TOEIC cấp tốc'),
    ('1UE1A3I3hmoY1EcMVMPCT2R7Y3TBCoqfq', '600 ESSENTIAL WORDS FOR THE TOEIC'),
    ('1VPdgG_UhhgnUlzrT-axnPtAzaLZJzXBT', 'Tự Luyện 550+ trong 10 ngày - Benzen'),
    ('1_M5bM5EN1gMAgiLh7oMDeJ1dHKmPd7Mk', 'Bí kíp đạt 450 TOEIC mất gốc'),
    ('1ahxUwHqe0Xw5Zh-aCxLQ08L_KMT-78vs', 'Dễ dàng đạt TOEIC Listening 750+ - Unica'),
    ('1amL4HxxLrsNtSFKA2u5d5pXmeW0dIGBH', 'EBOOK 10 NGUYÊN TẮC TỰ HỌC TOEIC'),
    ('1k_u5rtv8cecWNmJe3HyeVc9iPES-s2Il', '04. BIG STEP TOEIC'),
    ('1mciUmcjs64nmEDR6iKB7VGvup2bEmtLc', 'Bí Quyết Chinh Phục TOEIC 500+ - Unica'),
    ('1oHUHYyEQ0T5H-rl_fXHMjljV4lGKCRB-', 'Sparta Toeic'),
    ('1pnkrjkBoYovk-hYnAdCCav03yMdiafSi', 'Sparta Toeic Quyển 2'),
    ('1t-3gsjblXZ5VW4WNkY5ECwtYSQQ2-JHj', 'Sách Economy TOEIC 1-5'),
    ('1v2hRKTU40DZrTPE4tEafVwZMgrOZD1TZ', 'Taking the TOEIC Skills and Strategies 1'),
    ('1vZBhP53Sp5wyMCPX7mqZcnMXohYdCvAM', 'HỆ THỐNG MẸO TRONG BÀI THI TOEIC'),
    ('1znIXD0xTxnak3JqR_ShZHGRZELtLUH3U', '3 đề Toeic giải chi tiết - Benzen'),
]

def get_cookies_for_playwright():
    cj = browser_cookie3.chrome(
        domain_name='.google.com',
        cookie_file='/home/vodailoc/.config/google-chrome/Profile 19/Cookies'
    )
    cookies = []
    for c in cj:
        domain = c.domain
        # Playwright requires domain to start with dot for subdomain cookies
        if not domain.startswith('.') and not domain.startswith('accounts'):
            domain = '.' + domain
        cookie = {
            "name": c.name,
            "value": c.value or "",
            "domain": domain,
            "path": c.path or "/",
            "sameSite": "Lax",
            "secure": bool(c.secure),
        }
        if c.expires and c.expires > 0:
            cookie["expires"] = float(c.expires)
        cookies.append(cookie)
    return cookies

def get_requests_session():
    cj = browser_cookie3.chrome(
        domain_name='.google.com',
        cookie_file='/home/vodailoc/.config/google-chrome/Profile 19/Cookies'
    )
    s = requests.Session()
    for c in cj:
        s.cookies.set(c.name, c.value, domain=c.domain)
    return s

def sanitize(name):
    return re.sub(r'[<>:"/\\|?*\x00-\x1f]', '_', name).strip()

def download_file_requests(session, file_id, fname, dest_dir):
    """Tải file bằng requests."""
    fpath = os.path.join(dest_dir, fname)
    if os.path.exists(fpath) and os.path.getsize(fpath) > 1000:
        return True, f'Skip (exists): {fname}'

    headers = {'User-Agent': 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/126.0.0.0 Safari/537.36'}
    url = f'https://drive.usercontent.google.com/download?id={file_id}&export=download&authuser=0&confirm=t'

    try:
        resp = session.get(url, headers=headers, stream=True, timeout=120, allow_redirects=True)
        if resp.status_code != 200:
            url2 = f'https://drive.google.com/uc?export=download&id={file_id}&confirm=t'
            resp = session.get(url2, headers=headers, stream=True, timeout=120)
        if resp.status_code != 200:
            return False, f'HTTP {resp.status_code}'

        # Get real filename from Content-Disposition
        cd = resp.headers.get('Content-Disposition', '')
        for pat in [r"filename\*=UTF-8''(.+)", r'filename="([^"]+)"', r'filename=([^\s;]+)']:
            m = re.search(pat, cd, re.I)
            if m:
                from urllib.parse import unquote
                real_name = sanitize(unquote(m.group(1).strip().strip('"')))
                if real_name:
                    fname = real_name
                    fpath = os.path.join(dest_dir, fname)
                break

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

async def get_folder_files_playwright(page, folder_id):
    """Dùng Playwright để vào folder và lấy file IDs."""
    url = f'https://drive.google.com/drive/folders/{folder_id}'
    try:
        await page.goto(url, wait_until='domcontentloaded', timeout=30000)
        await asyncio.sleep(3)

        # Lấy toàn bộ HTML
        content = await page.content()

        # Extract file IDs từ HTML
        file_ids = re.findall(r'"([a-zA-Z0-9_-]{25,})"', content)
        # Lọc chỉ lấy Drive file IDs (không phải folder IDs đã biết)
        known_folders = {fid for fid, _ in FOLDER_IDS}

        # Lấy tên file từ JS data
        # Pattern: ["filename", ..., "file_id"]
        items = []

        # Thử lấy từ data attributes
        title_id_pairs = re.findall(r'"([^"]{3,100}\.(?:pdf|docx|xlsx|pptx|mp3|mp4|zip|rar))"[^}]*?"([a-zA-Z0-9_-]{25,})"', content, re.IGNORECASE)
        for title, fid in title_id_pairs:
            if fid not in known_folders:
                items.append({'id': fid, 'name': title})

        # Nếu không tìm được pairs, lấy tất cả IDs lạ
        if not items:
            all_ids = set(re.findall(r'[a-zA-Z0-9_-]{25,}', content))
            for fid in all_ids:
                if fid not in known_folders and not fid.startswith('AAAA'):
                    items.append({'id': fid, 'name': f'file_{fid[:12]}'})

        # Lấy tên page để làm tên folder
        title = await page.title()
        folder_name = title.replace(' - Google Drive', '').strip()

        return folder_name, items[:100]  # max 100 items
    except Exception as e:
        return folder_id[:12], []

async def main():
    print('=' * 65)
    print(f'Download nội dung {len(FOLDER_IDS)} folders via Playwright')
    print('=' * 65)

    cookies = get_cookies_for_playwright()
    session = get_requests_session()

    total_ok = 0
    total_fail = 0
    all_folder_files = {}

    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True)
        context = await browser.new_context(
            user_agent='Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/126.0.0.0 Safari/537.36'
        )
        await context.add_cookies(cookies)
        page = await context.new_page()

        for i, (folder_id, known_name) in enumerate(FOLDER_IDS, 1):
            print(f'\n[{i}/{len(FOLDER_IDS)}] {known_name or folder_id[:20]}...')

            folder_name, items = await get_folder_files_playwright(page, folder_id)
            if not folder_name or folder_name in ('Error', 'Google Drive'):
                folder_name = known_name or folder_id[:16]

            folder_name = sanitize(folder_name)
            dest_dir = os.path.join(BASE_OUT, folder_name)
            os.makedirs(dest_dir, exist_ok=True)

            print(f'  📂 {folder_name} → {len(items)} items tìm thấy')
            all_folder_files[folder_id] = {'name': folder_name, 'items': items}

            if not items:
                print('  ⚠️  Folder trống hoặc không có quyền xem files')
                continue

            for item in items:
                fid = item['id']
                fname = sanitize(item['name'])
                if not fname:
                    fname = f'file_{fid[:12]}'

                ok, msg = download_file_requests(session, fid, fname, dest_dir)
                if ok:
                    print(f'  ✅ {msg}')
                    total_ok += 1
                else:
                    print(f'  ❌ {fid[:16]}: {msg}')
                    total_fail += 1

                await asyncio.sleep(0.2)

        await browser.close()

    # Lưu danh sách để debug
    with open('/home/vodailoc/toeci/folder_contents.json', 'w', encoding='utf-8') as f:
        json.dump(all_folder_files, f, ensure_ascii=False, indent=2)

    # Thống kê
    total_size = 0
    file_count = 0
    for root, dirs, files in os.walk(BASE_OUT):
        for f in files:
            fp = os.path.join(root, f)
            sz = os.path.getsize(fp)
            total_size += sz
            file_count += 1

    print(f'\n{"=" * 65}')
    print(f'✅ Tải được: {total_ok}  ❌ Lỗi: {total_fail}')
    print(f'📁 Tổng: {file_count} files, {total_size/1024/1024:.1f} MB')
    print(f'📂 Lưu tại: {BASE_OUT}')
    print('\nXem chi tiết folders tại: folder_contents.json')

asyncio.run(main())

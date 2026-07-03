#!/usr/bin/env python3
"""
Dùng Playwright Python để:
1. Mở Google Sheet với cookies auth
2. Intercept streamrows response để lấy tất cả hyperlinks
3. Tải toàn bộ 73 files
"""

import asyncio, json, re, os, time, browser_cookie3, requests
from playwright.async_api import async_playwright

SHEET_URL = "https://docs.google.com/spreadsheets/d/15LEfRffN1xzoWtQLu4H6d5efNkBv_V1d/edit?gid=1922356504"
DOWNLOAD_DIR = "/home/vodailoc/toeci/downloads"
os.makedirs(DOWNLOAD_DIR, exist_ok=True)

def get_browser_cookies():
    """Lấy cookies từ Chrome."""
    cj = browser_cookie3.chrome(
        domain_name='.google.com',
        cookie_file='/home/vodailoc/.config/google-chrome/Profile 19/Cookies'
    )
    cookies = []
    for c in cj:
        cookies.append({
            "name": c.name,
            "value": c.value,
            "domain": c.domain,
            "path": c.path or "/",
        })
    return cookies

def extract_links_from_response(body_text):
    """Tìm tất cả Google Drive/URL links trong streamrows response."""
    # Tìm URLs trong response
    links = []
    
    # Pattern 1: Drive file links
    file_ids = re.findall(r'/file/d/([a-zA-Z0-9_-]{25,})', body_text)
    for fid in set(file_ids):
        links.append(('drive_file', fid, f'https://drive.google.com/file/d/{fid}/view'))
    
    # Pattern 2: Drive folder links  
    folder_ids = re.findall(r'/folders/([a-zA-Z0-9_-]{25,})', body_text)
    for fid in set(folder_ids):
        links.append(('drive_folder', fid, f'https://drive.google.com/drive/folders/{fid}'))
    
    # Pattern 3: Any https URL in the data
    all_urls = re.findall(r'https?://[^\s\\"\'<>\\\\]+', body_text)
    for u in all_urls:
        u = u.rstrip('.,;)\\')
        if 'drive.google.com' in u or 'docs.google.com' in u:
            if u not in [l[2] for l in links]:
                links.append(('url', '', u))
    
    return links

async def main():
    print("=" * 60)
    print("Google Sheets → Extract ALL 73 Links via Playwright")
    print("=" * 60)
    
    cookies = get_browser_cookies()
    print(f"Loaded {len(cookies)} cookies")
    
    all_responses = []
    all_links = []
    
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True)
        context = await browser.new_context()
        
        # Set cookies
        await context.add_cookies(cookies)
        
        page = await context.new_page()
        
        # Intercept streamrows và externaldata responses
        async def handle_response(response):
            url = response.url
            if 'streamrows' in url or 'fetchData' in url or 'externaldata' in url:
                try:
                    body = await response.text()
                    if len(body) > 100:
                        all_responses.append((url, body))
                        print(f"  [CAPTURED] {url[:80]}... ({len(body)} bytes)")
                        
                        links = extract_links_from_response(body)
                        for l in links:
                            if l not in all_links:
                                all_links.append(l)
                except Exception as e:
                    pass
        
        page.on("response", handle_response)
        
        print(f"\n[1] Đang mở sheet...")
        await page.goto(SHEET_URL, wait_until="networkidle", timeout=60000)
        await asyncio.sleep(5)
        
        print(f"\n[2] Tìm thấy {len(all_links)} links từ network responses")
        
        # Lưu raw responses để debug
        for i, (url, body) in enumerate(all_responses):
            with open(f'/home/vodailoc/toeci/streamrows_{i}.txt', 'w') as f:
                f.write(f"URL: {url}\n\n{body}")
            print(f"  Saved streamrows_{i}.txt ({len(body)} bytes)")
        
        await browser.close()
    
    # In kết quả
    print(f"\n[3] Tổng cộng {len(all_links)} links:")
    for ltype, fid, url in all_links:
        print(f"  [{ltype}] {url[:80]}")
    
    # Lưu danh sách links
    with open('/home/vodailoc/toeci/all_links.json', 'w') as f:
        json.dump(all_links, f, indent=2)
    print(f"\nSaved to all_links.json")

asyncio.run(main())

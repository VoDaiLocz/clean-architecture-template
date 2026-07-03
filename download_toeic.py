#!/usr/bin/env python3
"""Download all files from Google Drive links found in the TOEIC sheet."""

import browser_cookie3, requests, re, os, time
from urllib.parse import urlparse, parse_qs

DOWNLOAD_DIR = "/home/vodailoc/toeci/downloads"
os.makedirs(DOWNLOAD_DIR, exist_ok=True)

def get_cookies():
    return browser_cookie3.chrome(
        domain_name='.google.com',
        cookie_file='/home/vodailoc/.config/google-chrome/Profile 19/Cookies'
    )

def get_all_links():
    """Lấy tất cả Drive links từ sheet HTML."""
    cj = get_cookies()
    sheet_id = '15LEfRffN1xzoWtQLu4H6d5efNkBv_V1d'
    url = f'https://docs.google.com/spreadsheets/d/{sheet_id}/edit?gid=1922356504'
    headers = {'User-Agent': 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/126.0.0.0 Safari/537.36'}
    resp = requests.get(url, cookies=cj, headers=headers)
    
    # Tìm tất cả Drive links
    raw = re.findall(r'https://drive\.google\.com/[^\"\\s\\\\<>]+', resp.text)
    # Clean up
    links = []
    seen = set()
    for u in raw:
        # Trim noise
        u = re.split(r'[\\"\'\s<>]', u)[0]
        u = u.replace('\\u003d', '=').replace('\\u0026', '&')
        if u not in seen:
            seen.add(u)
            links.append(u)
    return links

def extract_file_id(url):
    """Lấy file ID từ Drive URL."""
    # Pattern: /file/d/{id}/
    m = re.search(r'/file/d/([a-zA-Z0-9_-]+)', url)
    if m:
        return m.group(1), 'file'
    # Pattern: /folders/{id}
    m = re.search(r'/folders/([a-zA-Z0-9_-]+)', url)
    if m:
        return m.group(1), 'folder'
    return None, None

def get_file_info(file_id, cj, api_key=None):
    api_key = api_key or os.environ.get('GOOGLE_API_KEY', '')
    """Lấy thông tin file từ Drive API."""
    url = f'https://clients6.google.com/drive/v2internal/files/{file_id}'
    params = {
        'fields': 'id,title,mimeType,fileSize',
        'supportsTeamDrives': 'true',
        'key': api_key
    }
    resp = requests.get(url, params=params, cookies=cj)
    if resp.status_code == 200:
        return resp.json()
    return None

def download_file(file_id, filename, cj):
    """Tải file từ Google Drive."""
    # Direct download URL
    download_url = f'https://drive.google.com/uc?export=download&id={file_id}'
    
    session = requests.Session()
    # Transfer cookies
    for cookie in cj:
        session.cookies.set(cookie.name, cookie.value, domain=cookie.domain)
    
    headers = {'User-Agent': 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/126.0.0.0 Safari/537.36'}
    
    resp = session.get(download_url, headers=headers, allow_redirects=True, stream=True)
    
    # Handle virus scan warning (large files)
    if 'confirm' in resp.url or b'confirm' in resp.content[:500]:
        # Get confirm token
        m = re.search(r'confirm=([0-9A-Za-z_-]+)', resp.text)
        if m:
            token = m.group(1)
            resp = session.get(
                f'https://drive.google.com/uc?export=download&id={file_id}&confirm={token}',
                headers=headers, stream=True
            )
    
    if resp.status_code != 200:
        return False, f"HTTP {resp.status_code}"
    
    # Detect filename from Content-Disposition
    cd = resp.headers.get('Content-Disposition', '')
    m = re.findall('filename[^;=\n]*=([\'"]?)([^\n;]*)\1', cd)
    if m:
        filename = m[0][1].strip('"\'')
    
    # Sanitize filename
    filename = re.sub(r'[<>:"/\\|?*]', '_', filename)
    filepath = os.path.join(DOWNLOAD_DIR, filename)
    
    # Avoid overwrite
    if os.path.exists(filepath):
        base, ext = os.path.splitext(filename)
        filepath = os.path.join(DOWNLOAD_DIR, f"{base}_{file_id[:6]}{ext}")
    
    # Write file
    size = 0
    with open(filepath, 'wb') as f:
        for chunk in resp.iter_content(chunk_size=32768):
            if chunk:
                f.write(chunk)
                size += len(chunk)
    
    return True, filepath, size

def main():
    print("=" * 60)
    print("TOEIC Sheet - Google Drive File Downloader")
    print("=" * 60)
    
    print("\n[1] Lấy cookies từ Chrome...")
    cj = get_cookies()
    
    print("[2] Lấy danh sách links từ sheet...")
    all_links = get_all_links()
    print(f"    Tìm thấy {len(all_links)} Drive links")
    
    # Phân loại
    file_links = []
    folder_links = []
    for link in all_links:
        fid, ftype = extract_file_id(link)
        if fid:
            if ftype == 'file':
                file_links.append((fid, link))
            else:
                folder_links.append((fid, link))
    
    print(f"    → {len(file_links)} files trực tiếp")
    print(f"    → {len(folder_links)} folders")
    
    # In danh sách folders để tham khảo
    if folder_links:
        print("\n[FOLDERS - cần mở thủ công hoặc xử lý riêng]:")
        for fid, link in folder_links:
            print(f"  https://drive.google.com/drive/folders/{fid}")
    
    # Tải files
    print(f"\n[3] Bắt đầu tải {len(file_links)} files...")
    print(f"    → Lưu vào: {DOWNLOAD_DIR}\n")
    
    success = 0
    errors = []
    
    for i, (fid, link) in enumerate(file_links, 1):
        print(f"[{i}/{len(file_links)}] File ID: {fid}")
        
        try:
            result = download_file(fid, f"file_{fid}", cj)
            if result[0]:
                _, filepath, size = result
                print(f"  ✅ {os.path.basename(filepath)} ({size/1024:.1f} KB)")
                success += 1
            else:
                print(f"  ❌ Lỗi: {result[1]}")
                errors.append((fid, result[1]))
        except Exception as e:
            print(f"  ❌ Exception: {e}")
            errors.append((fid, str(e)))
        
        time.sleep(0.5)  # Rate limiting
    
    print(f"\n{'='*60}")
    print(f"✅ Thành công: {success}/{len(file_links)} files")
    if errors:
        print(f"❌ Lỗi ({len(errors)}):")
        for fid, err in errors:
            print(f"  - {fid}: {err}")
    print(f"\n📁 Files lưu tại: {DOWNLOAD_DIR}")

if __name__ == "__main__":
    main()

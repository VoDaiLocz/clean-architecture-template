#!/usr/bin/env python3
"""Download all files from TOEIC sheet - fixed version with full IDs."""

import browser_cookie3, requests, re, os, time

DOWNLOAD_DIR = '/home/vodailoc/toeci/downloads'
os.makedirs(DOWNLOAD_DIR, exist_ok=True)

FILE_IDS = [
    '0BydBQJwhLN_0VlRwZFZiZmtvdW9sakMyRXpndlRQSV9xemFV',
    '0BydBQJwhLN_0WjRxODZQSTdMUExtbmNXS3VJenRmMEZZY0lB',
    '1-CDfO78yfbq6-CpPtK5_If1XZObd8nlJ',
    '10EHD9VugvcN_y5HUsymy_0zuqOzienU6',
    '12vS_3vzr-o-8Tm5xk4WCBZ_X1JQgFd28',
    '13XENbt970Buy0TYZH4nHZDLvX5aXYjeS',
    '13lD3hYfN84vAqCWaPVsl90CLaPQhT6En',
    '16-GAvsP-qCGne2ikoKL_x8SxpxcfuSXV',
    '1Fe9emKWfhyGhMyNAT4eg6CZG6xDADQNe',
    '1H2Fb8lEcddOsY93rnm-aNF-Rv8T3ImtB',
    '1LLHrkEpbgFeFq7t-H3hbBvUULe2WhXDf',
    '1bOW-dO6nJ1WKe6unQe0jta6aInOOToHY',
    '1bnABUry-_ATAa0wZfzIt9OTKrGSC1Njs',
    '1sw2kHnOTQN2Ty7ALW6fClB1Y1vDm8a4w',
]

def download_file(session, fid, headers):
    url = 'https://drive.usercontent.google.com/download'
    params = {'id': fid, 'export': 'download', 'authuser': '0', 'confirm': 't'}
    resp = session.get(url, params=params, headers=headers, stream=True, timeout=60)
    
    if resp.status_code != 200:
        # Fallback to old URL
        url2 = f'https://drive.google.com/uc?export=download&id={fid}&confirm=t'
        resp = session.get(url2, headers=headers, stream=True, timeout=60)
    
    return resp

def get_filename(resp, fid):
    cd = resp.headers.get('Content-Disposition', '')
    # Try various patterns
    for pattern in [
        r"filename\*=UTF-8''(.+)",
        r'filename="([^"]+)"',
        r"filename='([^']+)'",
        r'filename=([^\s;]+)',
    ]:
        m = re.search(pattern, cd, re.IGNORECASE)
        if m:
            name = m.group(1).strip()
            from urllib.parse import unquote
            return unquote(name)
    
    ct = resp.headers.get('Content-Type', '')
    ext = ''
    if 'pdf' in ct:
        ext = '.pdf'
    elif 'zip' in ct:
        ext = '.zip'
    elif 'msword' in ct or 'wordprocessing' in ct:
        ext = '.docx'
    elif 'spreadsheet' in ct or 'excel' in ct:
        ext = '.xlsx'
    
    return f'file_{fid[:16]}{ext}'

def main():
    print("=" * 60)
    print(f"Tải {len(FILE_IDS)} files từ Google Drive")
    print("=" * 60)
    
    cj = browser_cookie3.chrome(
        domain_name='.google.com',
        cookie_file='/home/vodailoc/.config/google-chrome/Profile 19/Cookies'
    )
    
    session = requests.Session()
    for cookie in cj:
        session.cookies.set(cookie.name, cookie.value, domain=cookie.domain)
    
    headers = {
        'User-Agent': 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/126.0.0.0 Safari/537.36',
        'Accept': '*/*',
    }
    
    existing = set(os.listdir(DOWNLOAD_DIR))
    success, errors = 0, []
    
    for i, fid in enumerate(FILE_IDS, 1):
        print(f'\n[{i}/{len(FILE_IDS)}] ID: {fid[:30]}...')
        
        # Skip đã tải
        already = [f for f in existing if fid[:16] in f]
        if already:
            print(f'  ⏭  Đã có: {already[0]}')
            success += 1
            continue
        
        try:
            resp = download_file(session, fid, headers)
            
            if resp.status_code != 200:
                print(f'  ❌ HTTP {resp.status_code}')
                errors.append((fid, f'HTTP {resp.status_code}'))
                continue
            
            fname = get_filename(resp, fid)
            fname = re.sub(r'[<>:"/\\|?*\x00-\x1f]', '_', fname)
            fpath = os.path.join(DOWNLOAD_DIR, fname)
            
            # Tránh ghi đè
            if os.path.exists(fpath):
                base, ext = os.path.splitext(fname)
                fpath = os.path.join(DOWNLOAD_DIR, f"{base}_{fid[:6]}{ext}")
            
            size = 0
            with open(fpath, 'wb') as f:
                for chunk in resp.iter_content(65536):
                    if chunk:
                        f.write(chunk)
                        size += len(chunk)
            
            if size < 2000:
                os.remove(fpath)
                print(f'  ❌ File quá nhỏ ({size} bytes)')
                errors.append((fid, f'Too small: {size}b'))
            else:
                print(f'  ✅ {fname} ({size/1024:.0f} KB)')
                success += 1
                existing.add(fname)
        
        except Exception as e:
            print(f'  ❌ Exception: {e}')
            errors.append((fid, str(e)))
        
        time.sleep(0.5)
    
    print(f'\n{"=" * 60}')
    print(f'✅ Thành công: {success}/{len(FILE_IDS)}')
    if errors:
        print(f'❌ Lỗi ({len(errors)}):')
        for fid, err in errors:
            print(f'  {fid[:20]}: {err}')
    
    print(f'\n📁 Files đã tải ({DOWNLOAD_DIR}):')
    for f in sorted(os.listdir(DOWNLOAD_DIR)):
        sz = os.path.getsize(os.path.join(DOWNLOAD_DIR, f))
        print(f'  {f}  ({sz/1024:.0f} KB)')

if __name__ == '__main__':
    main()

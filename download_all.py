#!/usr/bin/env python3
"""Download all files from TOEIC sheet - complete version with all 73+ links."""

import browser_cookie3, requests, re, os, time, json

DOWNLOAD_DIR = '/home/vodailoc/toeci/downloads'
os.makedirs(DOWNLOAD_DIR, exist_ok=True)

# Tất cả file IDs từ sheet (bao gồm cả drive/open?id= format)
FILE_IDS = [
    # drive/file/d/ format
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
    # drive/open?id= format (mới tìm được)
    '14QVmZNzUgh15l9lAVNRybrSQdrKQbQd9',
    '1FCljY_WGfA1p5CU0tqtZeOc-VA_XKvx5',
    '1HQJ5Py4O08TwrENFZoO6fUgRfLlOAn6J',
    '1NzZ2CKwWpA1KiOaIglmsNZVpVmoBNQNq',
    '1OB9uBD0jvhcAPYvVIiYkwjC-OBgQKooI',
    '1bwYaZ2ACgL83FCLld7BNRPVDHexOxN5c',
    '1fn7uu-6UFZhJ4dhNxfJHaqUwJD0aRzCN',
    '1zk5H1IakMcuip2biTnbcxzlHszschW-o',
    '1zpeCs-zK4Ojj5R7H2l-8KtjvYZk9UmTG',
    # docs.google.com document
    '1949u7h61A1QIF2_o7jHVtC4PapZ07orIHN7MsIdbAuQ',
    '1JSMJuVVnFVIbaiPljM4CRthqB1ikppQv80IP-k_2_q8',
]

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
    session = requests.Session()
    for c in cj:
        session.cookies.set(c.name, c.value, domain=c.domain)
    return session

def get_filename(resp, fid):
    cd = resp.headers.get('Content-Disposition', '')
    for pattern in [
        r"filename\*=UTF-8''(.+)",
        r'filename="([^"]+)"',
        r"filename='([^']+)'",
        r'filename=([^\s;]+)',
    ]:
        m = re.search(pattern, cd, re.IGNORECASE)
        if m:
            from urllib.parse import unquote
            return unquote(m.group(1).strip().strip('"\''))
    ct = resp.headers.get('Content-Type', '')
    ext = '.pdf' if 'pdf' in ct else '.zip' if 'zip' in ct else '.docx' if 'word' in ct else ''
    return f'file_{fid[:16]}{ext}'

def get_file_title(session, fid, headers):
    """Lấy tên file từ Drive page."""
    try:
        resp = session.get(f'https://drive.google.com/file/d/{fid}/view', headers=headers, timeout=10)
        m = re.search(r'<title>([^<]+)</title>', resp.text)
        if m:
            title = m.group(1).replace(' - Google Drive', '').strip()
            if title and title != 'Google Drive':
                return title
    except:
        pass
    return None

def download_file(session, fid, headers):
    urls_to_try = [
        f'https://drive.usercontent.google.com/download?id={fid}&export=download&authuser=0&confirm=t',
        f'https://drive.google.com/uc?export=download&id={fid}&confirm=t',
    ]
    for url in urls_to_try:
        try:
            resp = session.get(url, headers=headers, stream=True, timeout=60, allow_redirects=True)
            if resp.status_code == 200 and 'text/html' not in resp.headers.get('Content-Type', ''):
                return resp
            if resp.status_code == 200:
                # Check for confirm page
                token = re.search(r'confirm=([0-9A-Za-z_-]+)', resp.text[:2000])
                if token:
                    url2 = f'https://drive.google.com/uc?export=download&id={fid}&confirm={token.group(1)}'
                    resp2 = session.get(url2, headers=headers, stream=True, timeout=60)
                    if resp2.status_code == 200:
                        return resp2
        except Exception as e:
            continue
    return None

def main():
    print("=" * 65)
    print(f"TOEIC Files Downloader - {len(FILE_IDS)} files + {len(FOLDER_IDS)} folders")
    print("=" * 65)

    session = get_session()
    headers = {'User-Agent': 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/126.0.0.0 Safari/537.36'}
    existing = {f: os.path.getsize(os.path.join(DOWNLOAD_DIR, f)) for f in os.listdir(DOWNLOAD_DIR)}

    success, skipped, errors = 0, 0, []

    for i, fid in enumerate(FILE_IDS, 1):
        print(f'\n[{i}/{len(FILE_IDS)}] {fid[:35]}...')

        # Kiểm tra đã tải chưa
        already = [f for f in existing if fid[:16] in f or fid[:12] in f]
        if already:
            print(f'  ⏭  Đã có: {already[0]}')
            skipped += 1
            continue

        resp = download_file(session, fid, headers)
        if not resp:
            # Try to get title to understand what it is
            title = get_file_title(session, fid, headers)
            print(f'  ❌ Không tải được (title: {title or "N/A"})')
            errors.append((fid, 'Download failed'))
            continue

        fname = get_filename(resp, fid)

        # Nếu tên file generic, lấy từ Drive page
        if fname.startswith('file_'):
            title = get_file_title(session, fid, headers)
            if title:
                ext = os.path.splitext(fname)[1] or '.pdf'
                fname = re.sub(r'[<>:"/\\|?*\x00-\x1f]', '_', title)
                if not fname.endswith(ext) and ext:
                    fname += ext

        fpath = os.path.join(DOWNLOAD_DIR, fname)
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
            print(f'  ❌ File quá nhỏ ({size}b) - likely error page')
            errors.append((fid, f'Too small: {size}b'))
        else:
            print(f'  ✅ {fname} ({size/1024:.0f} KB)')
            success += 1
            existing[fname] = size

        time.sleep(0.5)

    # In folders
    print(f'\n{"=" * 65}')
    print(f'✅ Tải được: {success}  ⏭ Bỏ qua (đã có): {skipped}  ❌ Lỗi: {len(errors)}')

    print(f'\n📂 FOLDERS ({len(FOLDER_IDS)} - cần dùng gdown để tải từng folder):')
    for fid in FOLDER_IDS:
        print(f'  https://drive.google.com/drive/folders/{fid}')

    print(f'\n📁 Files trong {DOWNLOAD_DIR}:')
    total = 0
    for f in sorted(os.listdir(DOWNLOAD_DIR)):
        sz = os.path.getsize(os.path.join(DOWNLOAD_DIR, f))
        total += sz
        print(f'  📄 {f} ({sz/1024:.0f} KB)')
    print(f'\nTổng: {total/1024/1024:.1f} MB')

    if errors:
        print(f'\n❌ Lỗi:')
        for fid, err in errors:
            print(f'  {fid[:25]}: {err}')

if __name__ == '__main__':
    main()

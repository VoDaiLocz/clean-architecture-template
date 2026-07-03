#!/usr/bin/env python3
"""Extract & decrypt Google cookies from Chrome Profile 19 → Playwright state JSON.
Handles v10 (PBKDF2+AES-CBC) and v20 (app-bound AES-256-GCM) formats.
"""

import base64, json, os, shutil, sqlite3, sys, tempfile
import secretstorage

CHROME_PROFILE = os.path.expanduser("~/.config/google-chrome/Profile 19")
COOKIES_DB = os.path.join(CHROME_PROFILE, "Cookies")

def get_keys_from_keyring():
    """Lấy tất cả Chrome keys từ Secret Service."""
    bus = secretstorage.dbus_init()
    collection = secretstorage.get_default_collection(bus)
    
    keys = {}
    for item in collection.get_all_items():
        label = item.get_label()
        attrs = item.get_attributes()
        secret = item.get_secret()
        
        schema = attrs.get('xdg:schema', '')
        app = attrs.get('application', '') or attrs.get('app_id', '')
        
        if schema == 'chrome_libsecret_os_crypt_password_v2' and app == 'chrome':
            keys['v10_password'] = secret
            print(f"  v10 key found: {label}")
        
        if schema == 'org.freedesktop.portal.Secret' and 'Chrome' in (app or label):
            keys['v20_app_key'] = secret
            print(f"  v20 app key found: {label} ({len(secret)} bytes)")
    
    return keys

def derive_key_v10(password_bytes):
    from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
    from cryptography.hazmat.primitives import hashes
    from cryptography.hazmat.backends import default_backend
    kdf = PBKDF2HMAC(
        algorithm=hashes.SHA1(), length=16,
        salt=b'saltysalt', iterations=1,
        backend=default_backend()
    )
    return kdf.derive(password_bytes)

def decrypt_v10(data, key):
    from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
    from cryptography.hazmat.backends import default_backend
    payload = data[3:]  # strip 'v10'/'v11'
    iv = b' ' * 16
    cipher = Cipher(algorithms.AES(key), modes.CBC(iv), backend=default_backend())
    dec = cipher.decryptor()
    decrypted = dec.update(payload) + dec.finalize()
    pad = decrypted[-1]
    return decrypted[:-pad].decode('utf-8', errors='replace')

def decrypt_v20(data, app_key_32):
    """v20: 3 bytes prefix + 12 bytes nonce + ciphertext + 16 bytes GCM tag."""
    from cryptography.hazmat.primitives.ciphers.aead import AESGCM
    payload = data[3:]  # strip 'v20'
    nonce = payload[:12]
    ct_and_tag = payload[12:]
    try:
        aesgcm = AESGCM(app_key_32)
        decrypted = aesgcm.decrypt(nonce, ct_and_tag, None)
        # Skip first 32 bytes (app-bound prefix)
        if len(decrypted) > 32:
            decrypted = decrypted[32:]
        return decrypted.decode('utf-8', errors='replace')
    except Exception as e:
        return None

def main():
    print("=== Extracting Chrome cookies from Profile 19 ===\n")
    print("Getting keys from keyring...")
    keys = get_keys_from_keyring()
    
    # v10 key
    v10_key = None
    if 'v10_password' in keys:
        v10_key = derive_key_v10(keys['v10_password'])
        print(f"  v10 AES key derived: {v10_key.hex()}")
    
    # v20 app key (raw 64 bytes, use first 32 for AES-256)
    v20_key = keys.get('v20_app_key')
    if v20_key:
        print(f"  v20 app key: {len(v20_key)} bytes")
    
    # Copy DB to tmp (avoid lock)
    tmp_db = tempfile.mktemp(suffix=".db")
    shutil.copy2(COOKIES_DB, tmp_db)
    
    conn = sqlite3.connect(tmp_db)
    cur = conn.cursor()
    cur.execute("""
        SELECT host_key, name, value, path, expires_utc, is_secure, is_httponly, encrypted_value
        FROM cookies
        WHERE host_key LIKE '%google.com%'
        ORDER BY host_key, name
    """)
    rows = cur.fetchall()
    conn.close()
    os.remove(tmp_db)
    
    print(f"\nFound {len(rows)} Google cookies\n")
    
    cookies = []
    ok, fail = 0, 0
    
    for host_key, name, value, path, expires_utc, is_secure, is_httponly, encrypted_value in rows:
        actual_value = value or ""
        
        if not actual_value and encrypted_value:
            prefix = encrypted_value[:3]
            if prefix in (b'v10', b'v11') and v10_key:
                try:
                    actual_value = decrypt_v10(encrypted_value, v10_key)
                    ok += 1
                except Exception as e:
                    fail += 1
                    actual_value = ""
            elif prefix == b'v20' and v20_key:
                result = decrypt_v20(encrypted_value, v20_key[:32])
                if result:
                    actual_value = result
                    ok += 1
                else:
                    fail += 1
                    actual_value = ""
            else:
                fail += 1
        
        unix_ts = (expires_utc / 1_000_000) - 11_644_473_600 if expires_utc > 0 else -1
        
        cookies.append({
            "name": name,
            "value": actual_value,
            "domain": host_key,
            "path": path or "/",
            "expires": unix_ts,
            "httpOnly": bool(is_httponly),
            "secure": bool(is_secure),
            "sameSite": "Lax"
        })
        
        preview = actual_value[:60] if actual_value else "(empty)"
        print(f"  [{host_key}] {name} = {preview}")
    
    print(f"\n✅ Decrypted: {ok}, ❌ Failed: {fail}")
    
    state = {"cookies": cookies, "origins": []}
    out = "/home/vodailoc/toeci/google_auth_state.json"
    with open(out, "w") as f:
        json.dump(state, f, indent=2)
    
    print(f"💾 Saved → {out}")

if __name__ == "__main__":
    main()

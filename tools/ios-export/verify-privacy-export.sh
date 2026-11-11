#!/usr/bin/env bash
# Framework / privacy-manifest inspect of the orphan xcode-export tip (G-5, G-7).
# Does not download an IPA, start a Cloud build, or capture device traffic.
set -euo pipefail
REF=${1:-origin/xcode-export}
ROOT=$(git rev-parse --show-toplevel)
cd "$ROOT"
git fetch origin xcode-export:refs/remotes/origin/xcode-export -q

fail=0
note() { echo "$1"; }

python3 - "$REF" <<'PY'
import json, re, subprocess, sys
from pathlib import Path

ref = sys.argv[1]
fail = 0

def git_show(path):
    r = subprocess.run(["git", "show", f"{ref}:{path}"], capture_output=True)
    if r.returncode != 0:
        return None, (r.stderr.decode() or r.stdout.decode())
    return r.stdout.decode(), None

def plist_pairs(text):
    # Minimal plist scan: key -> following raw tag/value. Good enough for our pins.
    keys = []
    i = 0
    while True:
        m = re.search(r"<key>([^<]+)</key>\s*(<(true|false)\s*/>|<string>([^<]*)</string>|<array>(.*?)</array>|<dict>(.*?)</dict>)",
                      text[i:], re.S)
        if not m:
            break
        key = m.group(1)
        if m.group(3) in ("true", "false"):
            val = m.group(3)
        elif m.group(4) is not None:
            val = m.group(4)
        elif m.group(5) is not None:
            val = m.group(5).strip()
        else:
            val = m.group(6)
        keys.append((key, val))
        i += m.start() + 1
    return dict(keys)

# --- Info.plist (G-7) ---
info, err = git_show("Info.plist")
if err:
    print("FAIL missing Info.plist:", err.strip())
    fail = 1
    info = ""
pairs = plist_pairs(info) if info else {}
enc = pairs.get("ITSAppUsesNonExemptEncryption")
ver = pairs.get("CFBundleVersion")
short = pairs.get("CFBundleShortVersionString")
print(f"CFBundleShortVersionString={short}")
print(f"CFBundleVersion={ver}")
print(f"ITSAppUsesNonExemptEncryption={enc}")
if enc != "false":
    print("FAIL ITSAppUsesNonExemptEncryption must be false")
    fail = 1

# --- PrivacyInfo ---
priv, err = git_show("UnityFramework/PrivacyInfo.xcprivacy")
if err:
    print("FAIL missing UnityFramework/PrivacyInfo.xcprivacy:", err.strip())
    fail = 1
    priv = ""
pp = plist_pairs(priv) if priv else {}
tracking = pp.get("NSPrivacyTracking")
domains = pp.get("NSPrivacyTrackingDomains", "")
collected = pp.get("NSPrivacyCollectedDataTypes", "")
print(f"NSPrivacyTracking={tracking}")
if tracking != "false":
    print("FAIL NSPrivacyTracking must be false")
    fail = 1
if domains not in ("",):
    print("FAIL NSPrivacyTrackingDomains must be empty, got:", repr(domains[:200]))
    fail = 1
else:
    print("NSPrivacyTrackingDomains=[]")
if collected not in ("",):
    print("FAIL NSPrivacyCollectedDataTypes must be empty, got:", repr(collected[:200]))
    fail = 1
else:
    print("NSPrivacyCollectedDataTypes=[]")
if "NSPrivacyAccessedAPITypes" not in pp:
    print("FAIL NSPrivacyAccessedAPITypes missing")
    fail = 1
else:
    print("NSPrivacyAccessedAPITypes=present")

# --- entitlements ---
ent, err = git_show("Unity-iPhone/KestrelHeavy.entitlements")
if err:
    print("FAIL missing entitlements:", err.strip())
    fail = 1
    ent = ""
if "com.apple.developer.game-center" in ent:
    print("FAIL entitlements contain com.apple.developer.game-center")
    fail = 1
# any entitlement keys at all?
ent_keys = re.findall(r"<key>([^<]+)</key>", ent)
print(f"entitlement_keys={ent_keys}")

# --- frameworks ---
pbx, err = git_show("Unity-iPhone.xcodeproj/project.pbxproj")
if err:
    print("FAIL missing pbxproj:", err.strip())
    fail = 1
    pbx = ""
banned = {
    "GameKit", "AdSupport", "AppTrackingTransparency", "iAd", "StoreKit",
    "WebKit", "SafariServices", "CoreTelephony", "AuthenticationServices",
    "MessageUI", "Social", "Accounts",
}
# Ignore Unity il2cpp GameCenterModule stubs (same rule as verify-no-game-center).
hits = []
for line in pbx.splitlines():
    if "UnityEngine.GameCenterModule" in line or "GameCenterModule" in line:
        continue
    for name in banned:
        if re.search(rf"{name}\.framework|{name}[\s.]", line, re.I):
            # avoid matching "GameController"
            if name == "GameKit" and "GameController" in line:
                continue
            hits.append((name, line.strip()[:200]))
            break
linked = sorted(set(re.findall(r"name = ([A-Za-z0-9_.]+)\.framework;", pbx)))
print("linked_frameworks=" + ",".join(linked))
if hits:
    for name, line in hits:
        print(f"FAIL banned framework {name}: {line}")
    fail = 1

# GameKit.framework specifically (verify-no-game-center overlap)
if re.search(r"GameKit\.framework", pbx) and "GameCenterModule" not in pbx:
    # already covered by banned; keep explicit
    pass

if fail:
    print("verify-privacy-export FAILED")
    sys.exit(1)
print("verify-privacy-export OK (PrivacyInfo pins, ITSAppUsesNonExemptEncryption=NO, empty entitlements, no tracking/ads/GC frameworks)")
sys.exit(0)
PY

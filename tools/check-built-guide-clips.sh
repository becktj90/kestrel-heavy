#!/usr/bin/env bash
# Fail the build if the player output has no Thrum clips.
# Unity copies StreamingAssets to iOS Data/Raw and WebGL StreamingAssets.
# An empty narration/guide folder means the device and the browser stay silent.
set -euo pipefail
root="${1:?usage: check-built-guide-clips.sh <build-output-dir>}"
if [[ ! -d "$root" ]]; then
  echo "guide clip check: build output missing: $root" >&2
  exit 1
fi

python3 - "$root" <<'PY'
import json, os, sys
root = sys.argv[1]
found = []
for dirpath, dirnames, filenames in os.walk(root):
    norm = dirpath.replace("\\", "/")
    if not norm.endswith("/narration/guide"):
        continue
    audio = []
    for name in filenames:
        path = os.path.join(dirpath, name)
        ext = os.path.splitext(name)[1].lower()
        if ext in (".mp3", ".ogg", ".wav") and os.path.isfile(path) and os.path.getsize(path) > 0:
            audio.append(name)
    manifest = os.path.join(dirpath, "manifest.json")
    expected = None
    if os.path.isfile(manifest):
        try:
            expected = len(json.load(open(manifest))["clips"])
        except Exception as e:
            print("guide clip check: unreadable manifest %s (%s)" % (manifest, e), file=sys.stderr)
            sys.exit(1)
    found.append((dirpath, len(audio), expected))

if not found:
    print("guide clip check: no narration/guide folder under %s" % root, file=sys.stderr)
    shown = 0
    for dirpath, dirnames, filenames in os.walk(root):
        print("  " + dirpath, file=sys.stderr)
        shown += 1
        if shown >= 40:
            break
    sys.exit(1)

total = 0
for path, n, expected in found:
    print("guide clips in build: %d under %s" % (n, path))
    total += n
    if n < 1:
        print("guide clip check: streaming clips folder is empty: %s" % path, file=sys.stderr)
        sys.exit(1)
    if expected is not None and n < expected:
        print("guide clip check: %s has %d audio files, manifest lists %d" % (path, n, expected), file=sys.stderr)
        sys.exit(1)
if total < 1:
    print("guide clip check: streaming clips folder is empty", file=sys.stderr)
    sys.exit(1)
PY

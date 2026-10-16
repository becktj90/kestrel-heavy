#!/usr/bin/env bash
# Copy assets/narration/guide into the Unity player layout.
# StreamingAssets is what iOS and WebGL can read. Resources gets the manifest only.
# BuildScript.PackGuideClips does the same copy inside Unity; this step runs first so
# the files are already imported when the editor opens.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
src="$root/assets/narration/guide"
stream="$root/unity/Assets/StreamingAssets/narration/guide"
res="$root/unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration/guide"
mkdir -p "$stream" "$res"
cp "$src/manifest.json" "$stream/manifest.json"
cp "$src/manifest.json" "$res/manifest.json"
cp "$src"/*.mp3 "$stream/"
python3 - "$src/manifest.json" "$stream" <<'PY'
import json, os, sys
man = json.load(open(sys.argv[1]))
folder = sys.argv[2]
missing = []
for clip in man["clips"]:
    name = os.path.basename(clip["file"])
    path = os.path.join(folder, name)
    if not os.path.isfile(path) or os.path.getsize(path) == 0:
        missing.append(clip["id"])
print("guide clips staged: %d/%d" % (len(man["clips"]) - len(missing), len(man["clips"])))
if missing:
    raise SystemExit("missing guide clips: " + ", ".join(missing))
PY

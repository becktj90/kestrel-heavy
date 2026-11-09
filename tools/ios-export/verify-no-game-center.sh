#!/usr/bin/env bash
# Precise Game Center / GameKit check for the orphan xcode-export tip (DECISIONS #98).
# Ignores UnityEngine.GameCenterModule* il2cpp stubs (not a GameKit framework link).
set -euo pipefail
REF=${1:-origin/xcode-export}
ROOT=$(git rev-parse --show-toplevel)
cd "$ROOT"
git fetch origin xcode-export:refs/remotes/origin/xcode-export main:refs/remotes/origin/main -q

fail=0
pbx=$(git show "$REF:Unity-iPhone.xcodeproj/project.pbxproj")
while IFS= read -r line; do
  [[ "$line" == *UnityEngine.GameCenterModule* ]] && continue
  [[ "$line" == *GameCenterModule* ]] && continue
  if echo "$line" | grep -Eqi 'GameKit\.framework|[[:space:]]GameKit[[:space:]]'; then
    echo "FAIL GameKit hit: $line"
    fail=1
  fi
done <<< "$pbx"

while IFS= read -r f; do
  [[ -z "$f" ]] && continue
  body=$(git show "$REF:$f")
  if echo "$body" | grep -q 'com.apple.developer.game-center'; then
    echo "FAIL entitlement $f has com.apple.developer.game-center"
    fail=1
  fi
done < <(git ls-tree -r --name-only "$REF" | grep '\.entitlements$' || true)

sa=$(git show origin/main:unity/ProjectSettings/ProjectSettings.asset | grep -E 'submitAnalytics:' | head -1 | awk '{print $2}')
echo "submitAnalytics=$sa"
[[ "$sa" == "0" ]] || { echo "FAIL submitAnalytics=$sa (want 0)"; fail=1; }

if [[ $fail -ne 0 ]]; then
  echo "verify-no-game-center FAILED"
  exit 1
fi
echo "verify-no-game-center OK (no GameKit.framework, no game-center entitlement, submitAnalytics=0)"

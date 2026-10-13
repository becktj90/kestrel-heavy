#!/usr/bin/env bash
# Unity CLI iOS export → orphan `xcode-export` branch (DECISIONS #59, docs/XCODE_CLOUD_PLAN.md).
# Only the lead agent (sole Unity CLI driver) runs this. Never raw Editor -batchmode.
#
# Usage:
#   tools/ios-export/export.sh <build-number> [git-ref]
#
# Examples:
#   tools/ios-export/export.sh 102
#   tools/ios-export/export.sh 102 hangar/g3-customizer-polish
#
# Requires: Unity CLI (`unity` on PATH, signed in), Editor 6000.3.26f1 + iOS module,
# git-lfs, rsync. Publishes with tools/ios-export/publish.sh.
set -euo pipefail

BUILD_NUMBER="${1:?build number required (e.g. 102)}"
GIT_REF="${2:-origin/main}"
REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
EDITOR_VERSION="6000.3.26f1"
WORKTREE="/tmp/kh-ios-export"
OUT="/tmp/kh-ios-out"
UNITY_BIN="${UNITY_BIN:-$HOME/.local/bin/unity}"
if [[ ! -x "$UNITY_BIN" ]]; then UNITY_BIN="$(command -v unity || true)"; fi
if [[ -z "${UNITY_BIN:-}" ]]; then
  echo "error: Unity CLI not found. Install: curl -fsSL https://unity.com/install.sh | bash" >&2
  exit 1
fi

export PATH="$(dirname "$UNITY_BIN"):$PATH"

echo "Unity CLI: $($UNITY_BIN --version 2>/dev/null || echo unknown)"
AUTH_JSON="$($UNITY_BIN auth status --json 2>/dev/null || true)"
if ! echo "$AUTH_JSON" | grep -q '"loggedIn": true'; then
  echo "error: Unity CLI is not signed in. Run: unity auth login" >&2
  exit 1
fi

# Ensure Editor + iOS module
if ! $UNITY_BIN editors list --json 2>/dev/null | grep -q "$EDITOR_VERSION"; then
  echo "Installing Unity Editor $EDITOR_VERSION with iOS module..."
  $UNITY_BIN install "$EDITOR_VERSION" -m ios
fi

git -C "$REPO_ROOT" fetch origin --prune
# Resolve ref to a commit on the remote when possible
if git -C "$REPO_ROOT" rev-parse --verify "$GIT_REF" >/dev/null 2>&1; then
  REF_SHA=$(git -C "$REPO_ROOT" rev-parse "$GIT_REF")
else
  echo "error: unknown ref '$GIT_REF'" >&2
  exit 1
fi
SHORT_SHA=$(git -C "$REPO_ROOT" rev-parse --short=12 "$REF_SHA")
LABEL=$(git -C "$REPO_ROOT" name-rev --name-only --no-undefined "$REF_SHA" 2>/dev/null || echo "$GIT_REF")
LABEL=${LABEL#remotes/origin/}
LABEL=${LABEL#origin/}

rm -rf "$WORKTREE" "$OUT"
git -C "$REPO_ROOT" worktree prune
git -C "$REPO_ROOT" worktree add --detach "$WORKTREE" "$REF_SHA"
cleanup() {
  git -C "$REPO_ROOT" worktree remove --force "$WORKTREE" 2>/dev/null || rm -rf "$WORKTREE"
}
trap cleanup EXIT

# Sync catalogs the same way SliceBootstrap / CI does (Resources is gitignored)
mkdir -p "$WORKTREE/unity/Assets/Resources/KestrelData"
cp "$WORKTREE"/data/*.json "$WORKTREE"/web/js/economy-data/constants.json \
  "$WORKTREE/unity/Assets/Resources/KestrelData/"

mkdir -p "$OUT"
echo "Building iOS export from $LABEL ($SHORT_SHA) → $OUT (build $BUILD_NUMBER)..."
(
  cd "$WORKTREE/unity"
  KH_BUILD_PATH="$OUT" KH_BUILD_NUMBER="$BUILD_NUMBER" \
    $UNITY_BIN build . --target iOS \
      --execute-method Kestrel.Slice.Editor.BuildScript.BuildIOS
)

# unity build may nest under build/iOS or write directly to KH_BUILD_PATH
EXPORT_DIR="$OUT"
if [[ -d "$OUT/iOS" ]]; then EXPORT_DIR="$OUT/iOS"; fi
if [[ ! -e "$EXPORT_DIR/Unity-iPhone.xcodeproj" ]]; then
  # GameCI-style layout: OUT/build/iOS
  if [[ -d "$OUT/build/iOS" ]]; then EXPORT_DIR="$OUT/build/iOS"; fi
fi

"$REPO_ROOT/tools/ios-export/publish.sh" "$BUILD_NUMBER" "$EXPORT_DIR" "$SHORT_SHA" "$LABEL"
echo "Done. Start an Xcode Cloud build from App Store Connect → Manual Start → xcode-export when ready."

#!/usr/bin/env bash
# Replace the orphan `xcode-export` branch with a fresh Unity iOS export tree.
# Never merge xcode-export into main. See docs/XCODE_CLOUD_PLAN.md / DECISIONS #59.
#
# Usage:
#   tools/ios-export/publish.sh <build-number> <export-dir> [source-sha] [source-ref-label]
#
# Example:
#   tools/ios-export/publish.sh 102 /tmp/kh-ios-out abcdef1 hangar/g3-customizer-polish
set -euo pipefail

BUILD_NUMBER="${1:?build number required (e.g. 102)}"
EXPORT_DIR="${2:?path to Unity iOS export directory required}"
SOURCE_SHA="${3:-}"
SOURCE_LABEL="${4:-main}"
REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
BRANCH="xcode-export"
VERSION="2.0.0"
WORK="$(mktemp -d "${TMPDIR:-/tmp}/kh-xcode-publish.XXXXXX")"
cleanup() { rm -rf "$WORK"; }
trap cleanup EXIT

if [[ ! "$BUILD_NUMBER" =~ ^[0-9]+$ ]] || (( BUILD_NUMBER < 100 )); then
  echo "error: build number must be an integer >= 100 (got '$BUILD_NUMBER')" >&2
  exit 1
fi
if [[ ! -d "$EXPORT_DIR" ]]; then
  echo "error: export dir not found: $EXPORT_DIR" >&2
  exit 1
fi
if [[ ! -d "$EXPORT_DIR/Unity-iPhone.xcodeproj" && ! -f "$EXPORT_DIR/Unity-iPhone.xcodeproj/project.pbxproj" ]]; then
  # Unity writes either a bundle dir or nested project; accept either shape.
  if [[ ! -e "$EXPORT_DIR/Unity-iPhone.xcodeproj" ]]; then
    echo "error: $EXPORT_DIR does not look like a Unity iOS export (missing Unity-iPhone.xcodeproj)" >&2
    exit 1
  fi
fi

PLIST="$EXPORT_DIR/Info.plist"
if [[ -f "$PLIST" ]]; then
  bid=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$PLIST" 2>/dev/null || true)
  ver=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$PLIST" 2>/dev/null || true)
  bld=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleVersion' "$PLIST" 2>/dev/null || true)
  # Linux runners: fall back to plutil/python
  if [[ -z "${bid:-}" ]]; then
    bid=$(python3 - <<PY
import plistlib,sys
p=plistlib.load(open("$PLIST","rb"))
print(p.get("CFBundleIdentifier",""))
PY
)
    ver=$(python3 - <<PY
import plistlib
p=plistlib.load(open("$PLIST","rb"))
print(p.get("CFBundleShortVersionString",""))
PY
)
    bld=$(python3 - <<PY
import plistlib
p=plistlib.load(open("$PLIST","rb"))
print(p.get("CFBundleVersion",""))
PY
)
  fi
  echo "Info.plist: id=$bid version=$ver build=$bld"
  # Unity leaves CFBundleIdentifier as ${PRODUCT_BUNDLE_IDENTIFIER}. The real id is in the Xcode project.
  # Shipping id is com.beckify.kestrelheavy (DECISIONS #30 / #59), which is what xcode-export already carries.
  EXPECTED_ID="com.beckify.kestrelheavy"
  PBX="$EXPORT_DIR/Unity-iPhone.xcodeproj/project.pbxproj"
  if [[ "$bid" == '${PRODUCT_BUNDLE_IDENTIFIER}' ]]; then
    if [[ ! -f "$PBX" ]]; then
      echo "error: Info.plist bundle id is unresolved and $PBX is missing" >&2
      exit 1
    fi
    if ! grep -q "PRODUCT_BUNDLE_IDENTIFIER = ${EXPECTED_ID};" "$PBX"; then
      echo "error: Xcode project is not $EXPECTED_ID" >&2
      exit 1
    fi
  elif [[ -n "$bid" && "$bid" != "$EXPECTED_ID" ]]; then
    echo "error: refused to publish bundle id '$bid' (expected $EXPECTED_ID)" >&2
    exit 1
  fi
  if [[ -f "$PBX" ]] && grep -q "kestrelheavy.unityproto" "$PBX"; then
    echo "error: refused proto bundle id in $PBX" >&2
    exit 1
  fi
  if [[ -n "$ver" && "$ver" != "$VERSION" ]]; then
    echo "error: refused to publish version '$ver' (expected $VERSION)" >&2
    exit 1
  fi
  if [[ -n "$bld" && "$bld" != "$BUILD_NUMBER" ]]; then
    echo "warn: CFBundleVersion=$bld does not match requested build $BUILD_NUMBER" >&2
  fi
fi

LATEST_TAG=$(git -C "$REPO_ROOT" ls-remote --tags origin 'export/2.0.0-*' 2>/dev/null \
  | sed 's#.*/export/2.0.0-##' | grep -E '^[0-9]+$' | sort -n | tail -1 || true)
if [[ -n "${LATEST_TAG:-}" ]] && (( BUILD_NUMBER <= LATEST_TAG )); then
  echo "error: build $BUILD_NUMBER is not greater than latest export/2.0.0-$LATEST_TAG" >&2
  exit 1
fi

if [[ -z "$SOURCE_SHA" ]]; then
  SOURCE_SHA=$(git -C "$REPO_ROOT" rev-parse --short=12 HEAD)
fi

STAGE="$WORK/tree"
mkdir -p "$STAGE"
# Copy export contents (no nested .git)
rsync -a --delete --exclude .git "$EXPORT_DIR"/ "$STAGE"/

# Branch-only LFS + ignore rules (never on main)
cat > "$STAGE/.gitattributes" <<'EOF'
*.a filter=lfs diff=lfs merge=lfs -text
EOF
cat > "$STAGE/.gitignore" <<'EOF'
xcuserdata/
*.xcuserstate
project.xcworkspace/xcuserdata/
.DS_Store
EOF

# Keep Xcode Cloud LFS pull helper when present in the previous export
if git -C "$REPO_ROOT" show "origin/$BRANCH:ci_scripts/ci_post_clone.sh" >/dev/null 2>&1; then
  mkdir -p "$STAGE/ci_scripts"
  git -C "$REPO_ROOT" show "origin/$BRANCH:ci_scripts/ci_post_clone.sh" > "$STAGE/ci_scripts/ci_post_clone.sh"
  chmod +x "$STAGE/ci_scripts/ci_post_clone.sh"
else
  mkdir -p "$STAGE/ci_scripts"
  cat > "$STAGE/ci_scripts/ci_post_clone.sh" <<'EOF'
#!/bin/sh
# Xcode Cloud: make sure Git LFS objects (Libraries/*.a) are present after the clone.
set -e
if ! command -v git-lfs >/dev/null 2>&1; then brew install git-lfs; fi
git lfs install
git lfs pull
EOF
  chmod +x "$STAGE/ci_scripts/ci_post_clone.sh"
fi

cat > "$STAGE/README.md" <<EOF
# xcode-export

Generated Unity 6000.3.26f1 iOS export of Kestrel Heavy $VERSION ($BUILD_NUMBER), com.beckify.kestrelheavy, minimum iOS 16.0, from $SOURCE_LABEL $SOURCE_SHA. Never merge into main. Setup: docs/XCODE_CLOUD_FIRST_SETUP.md
EOF

cd "$STAGE"
git init -q
git lfs install --local
git checkout -q -b "$BRANCH"
git add -A
# Single orphan commit; force-push replaces the branch each export.
MSG="xcode-export $VERSION ($BUILD_NUMBER), iOS 16.0 minimum, from $SOURCE_LABEL $SOURCE_SHA"
git -c user.name='kestrel-ios-export' -c user.email='ios-export@users.noreply.github.com' commit -q -m "$MSG"
TAG="export/$VERSION-$BUILD_NUMBER"
git tag -a "$TAG" -m "$MSG"

echo "Publishing $BRANCH @ $TAG ..."
git remote add origin "$(git -C "$REPO_ROOT" remote get-url origin)"
# In GitHub Actions, actions/checkout keeps the token as an http extraheader in the checkout's own git config
# (directly or through an includeIf file). This staging repo is fresh, so carry the header over or the push
# (and the LFS upload) has no credentials. Locally there is no header and your own git credentials are used.
AUTH_HEADER=$(git -C "$REPO_ROOT" config --get http.https://github.com/.extraheader 2>/dev/null || true)
if [[ -n "$AUTH_HEADER" ]]; then
  git config --local http.https://github.com/.extraheader "$AUTH_HEADER"
fi
git push -f origin "HEAD:refs/heads/$BRANCH"
git push -f origin "refs/tags/$TAG"

echo "Published $BRANCH ($MSG)"

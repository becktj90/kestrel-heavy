#!/usr/bin/env bash
# Every scene in unity/ProjectSettings/EditorBuildSettings.asset, plus the generated Resources assets the
# player needs, must be tracked in git, so a clean checkout (Unity Build Automation, CI) builds with no
# manual "Kestrel > Setup" step.
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"
fail=0
settings=unity/ProjectSettings/EditorBuildSettings.asset
git ls-files --error-unmatch "$settings" >/dev/null 2>&1 || { echo "::error::$settings is not tracked"; exit 1; }
scenes=$(sed -n 's/^ *path: *\(Assets\/.*\.unity\) *$/\1/p' "$settings")
[ -n "$scenes" ] || { echo "::error::no scenes listed in $settings"; exit 1; }
required="$scenes
Assets/Resources/UI/KH_PanelSettings.asset
Assets/Resources/UI/KH_ShellPanelSettings.asset
Assets/Resources/Materials/KH_Body.mat
Assets/Resources/Materials/KH_Plume.mat"
while IFS= read -r p; do
  for f in "unity/$p" "unity/$p.meta"; do
    if git ls-files --error-unmatch "$f" >/dev/null 2>&1; then echo "ok  $f"; else echo "::error::$f is not tracked in git"; fail=1; fi
  done
done <<< "$required"
exit $fail

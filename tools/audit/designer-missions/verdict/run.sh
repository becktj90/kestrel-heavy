#!/usr/bin/env bash
# Rerun the designer-vs-flight harness. Read-only against the repo.
set -euo pipefail
export PATH=/opt/dotnet:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1
HERE0=$(cd "$(dirname "$0")" && pwd); export KESTREL_REPO_ROOT=${KESTREL_REPO_ROOT:-$(cd "$HERE0/../../../.." && pwd)}   # lets BoardStarters/EngineModel find data/
HERE=$(cd "$(dirname "$0")" && pwd)
dotnet build -c Release "$HERE/Verdict.csproj" >/dev/null
BIN="$HERE/bin/Release/net8.0/Verdict.dll"
mkdir -p "$HERE/out" "$HERE/out2500"
dotnet "$BIN" "$KESTREL_REPO_ROOT" "$HERE/out" > "$HERE/out/stdout.txt"          # results.tsv + summary.txt (pad emulation tMax 600 like FlightDriver)
LIVE_TMAX=2500 dotnet "$BIN" "$KESTREL_REPO_ROOT" "$HERE/out2500" > /dev/null    # same with the pad cap lifted
dotnet "$BIN" "$KESTREL_REPO_ROOT" "$HERE/out" sens  > "$HERE/out/sensitivity.txt"
dotnet "$BIN" "$KESTREL_REPO_ROOT" "$HERE/out" probe > "$HERE/out/probe.txt"
echo "done: $HERE/out/{results.tsv,summary.txt,sensitivity.txt,probe.txt,matrix.txt}"

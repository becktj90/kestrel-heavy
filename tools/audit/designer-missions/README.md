# Designer × mission audit harnesses

These are throwaway console apps behind `docs/audits/DESIGNER_MISSION_DISCREPANCIES.md`. Each one compiles `unity/Assets/Kestrel.Core` straight from source and reads `data/*.json`. They never write to the repo, apart from their own `bin/`, `obj/` and `out*/` folders, which are git-ignored.

All three need the .NET 8 SDK:

```sh
export PATH=/opt/dotnet:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1
```

| Harness | Command | What it produces |
|---|---|---|
| `verdict/` | `verdict/run.sh` | `out/results.tsv` (one row per design × mission), `summary.txt` (mismatches grouped by type), `sensitivity.txt`, `probe.txt`. It also writes `out2500/`, the same run with the pad tMax cap lifted to 2500 s (B-01). Takes about 1 min. |
| `missions/` | `dotnet run -c Release --project missions/Missions.csproj -- <mode>` | `campaign-winners live` flies the stock winning designs through FlightBatch and the pad setup. `board` runs the board starters, `fit` runs the payload-fit rules (B-04), and `credits` checks budgets, rewards and the credits ledger (M-03). |
| `options/` | `dotnet run -c Release --project options/Options.csproj` | Probes one designer option at a time: failure-model engagement (B-05) and M-17 to M-29. |

The repo root defaults to four levels above `verdict/` (seven above the built binary). Set `KESTREL_REPO_ROOT` to override it.

Each item in the discrepancy list shows which harness output it came from as an ID: `D-VER-xx` comes from verdict, `D-MIS-xx` from missions and `D-OPT-xx` from options. Once a fix lands, re-run the matching harness and check that the row or probe has flipped.

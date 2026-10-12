# Unity prototype results (Phase 0 §5 template; Trevor fills in)

**Pass criteria:** golden vectors pass; p95 frame ≤16.7 ms on the floor device and on a Pro during MECO; IPA download ≤40 MB; cold start ≤3 s;
clean build ≤10 min; edit→device ≤60 s; zero binary assets an agent can't diff, except engine-generated ones.

| Metric | Unity 6.x | Unreal 5.x | Phaser today |
|---|---|---|---|
| Engine version / Xcode | 6000.3.26f1 / | n/a (not prototyped) | Phaser 4.2.1 / |
| Golden vectors pass (n/N, max err) | dotnet CLI: 17/17 tests, 129/129 vectors, max rel err 3.3e-13 (sim), ≤3.2e-16 (closed form), economy exact. Unity Test Runner: __ / 17 | | n/a (reference) |
| fps p50/p95: floor device (____) | | | |
| fps p50/p95: iPhone 15/16 Pro | | | |
| fps p50/p95: iPad 13" | | | |
| Thermal state after 10 min | | | |
| IPA download / install size (MB) | | | ~12 / |
| Cold start (s) | | | |
| Clean build time / incremental | | | |
| Edit→device loop (s) | | | |
| Game Center submit OK (Y/N) | | | Y |
| VoiceOver / Dynamic Type OK | N/N expected (known gap) | | |
| Hand editor edits needed (#) | | | 0 |
| Gaps / blockers | | | |
| Gut feel (1–5) visuals / fun | | | |

## How to measure
- **fps:** Xcode → Debug Navigator (FPS/CPU/GPU) during liftoff→MECO with the Stock preset, or Instruments → Game Performance. Record p50/p95 frame time.
- **IPA size:** App Store Connect → TestFlight → build → *App File Sizes* (download and install size for the device you name).
- **Cold start:** time from tapping the icon to the DESIGN panel being interactive, best of 3 after a reboot.
- **Build time:** clean = delete `Library/` and `build/`, then Build; incremental = change one `.cs` and rebuild.
- **Edit→device:** change a HUD string, build and run, then stopwatch until it shows on the device.
- **Hand edits:** every click in the editor that changes a file in git (excluding generated `.meta` and bootstrap-created assets).

## Notes / gaps log
-

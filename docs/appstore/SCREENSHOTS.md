# Screenshot plan (Unity)

DECISIONS #14: screenshots are redone at the end. **Do not upload to ASC without Trevor's yes.**

## Sizes (portrait)

| Slot | Pixels | Notes |
|---|---|---|
| iPhone 6.9" | **1320 × 2868** | Optional if 6.3" provided (ASC fallback chain) |
| iPhone 6.3" | **1206 × 2622** | Required path for modern iPhones |
| iPad 13" | **2064 × 2752** | **Required** while the app targets iPad |

No alpha. Real game UI only. No mockups. No real agency marks. No Thrum corner character in frame (off by default).

## Existing CI / tooling path (prefer this)

The repo already has a **separate, non-required** workflow:

- `.github/workflows/appstore-screenshots.yml` (`workflow_dispatch` or path-filtered PR)
- `unity/Assets/Kestrel.Capture.Tests/AppStoreScreenshotTests.cs` (runs only with `-khAppStoreShots`)
- Output: `CaptureOut/appstore/<scene>-<device>.png` for scenes **starter, designer, countdown, flight, landing** at the three sizes above
- Artifact name: `appstore-screenshots` (gates: exact size, not blank, not magenta)

**Regenerate:** run the workflow on current `main` (or a release branch) via `workflow_dispatch`. Do **not** invent a new capture pipeline. Compare one CI frame to a TestFlight shot before trusting llvmpipe lighting.

## Shot list (upload order)

| # | Scene | Caption idea (factual) | Gate |
|---|---|---|---|
| 1 | Starter designer, KH-3 Ready | Pick a mission. Shape the rocket. | OK on main |
| 2 | Why? card open | Tap ? to learn why. | OK |
| 3 | X-ray cutaway | See inside your rocket. | OK |
| 4 | Countdown / pad | Light it up. | OK |
| 5 | Ascent HUD (max-Q or staging) | Watch max-Q, staging and cutoff. | OK |
| 6 | Booster → Haven | Bring the booster home. | Only after a real landed/attempt frame on device or honest CI landing |
| 7 | Results card | See what happened. Try again. | OK |
| 8 | Mission map / patches | 20 missions, a patch for each. | OK |
| 9 | Full designer (optional) | Engineers: every part and number. | Hold until device confirms layout |

iPad: re-capture designer shots at 2064×2752 (400 pt side panel — do not upscale phone frames).

## Related

- [`LISTING.md`](LISTING.md)
- [`TREVOR_OPEN_ITEMS.md`](TREVOR_OPEN_ITEMS.md)
- Broader storyboard notes still in [`../APPSTORE_CONNECT.md`](../APPSTORE_CONNECT.md) §9 (older; prefer this file + the CI workflow)

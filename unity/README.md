# Kestrel Heavy: Unity 6 vertical-slice prototype

Draft prototype for the Phase 0 engine decision (`ENGINE_PHASE0.md` §5). **Not shipping code.**
It has its own dev bundle id, **`com.beckify.kestrelheavy.unityproto`**, and is internal TestFlight only. The shipping
app (`com.beckify.kestrelheavy`, `ios/`) is never touched.

Everything here is text authored without an editor. Count every hand edit you have to make in the editor and
put the number in `RESULTS.md`.

## Layout
| Path | What |
|---|---|
| `Assets/Kestrel.Core/` | Engine-agnostic C# core (`noEngineReferences: true`): ISA atmosphere, aero, gravity/earth, orbit, rocket equation, RK4 + fixed stepper, headless ascent `AscentSim`/`Headless.Simulate`, design/mass/stack/serial stages, cost model + reliability. A faithful port of `web/js`. |
| `Assets/Kestrel.Core.Tests/` | NUnit EditMode tests against `../data/golden/*.json` (exported from JS by `tools/export-golden.mjs`). |
| `dotnet/` | `Kestrel.Core.csproj` and `Kestrel.Core.Tests.csproj` **link the same .cs files**, so the core builds and tests without Unity. |
| `Assets/Kestrel.Slice/Runtime/` | Slice: `SliceApp` (portrait design stub + flight + HUD), `FlightDriver` (fixed 1/120 s sim, floating origin), `RocketMeshBuilder` (lathe geometry from the design), `PlumeController` (ambient-pressure-driven plume), `GameCenterBridge`. |
| `Assets/Kestrel.Slice/GameCenter/` | Apple GameKit implementation. Compiles **only** when Apple's plug-in is installed (`KH_GAMEKIT`). |
| `Assets/Kestrel.Slice/Editor/` | `SliceBootstrap` (zero-hand-edit setup: data sync, URP asset, materials, PanelSettings, scene, PlayerSettings) and `BuildScript.BuildIOS`. |
| `Assets/Resources/UI/Slice.uxml/.uss` | UI Toolkit layout and styles. |

Catalogs are **not copied into git** here. `Kestrel > Sync Data` (which also runs automatically on load and before every build)
copies `../data/*.json` and `../web/js/economy-data/constants.json` into `Assets/Resources/KestrelData/`, which is gitignored.

## 1. Install
- **Unity 6.3 LTS, `6000.3.26f1`** (pinned in `ProjectSettings/ProjectVersion.txt`; released 2026-10-08, supported to Dec 2027).
  In Unity Hub, go to Installs → Install Editor → 6000.3.26f1 and add the **iOS Build Support** module.
- Xcode 26 or later (whatever your Mac already uses for Kestrel Heavy).
- Optional, for the core without Unity: .NET SDK 8 (`brew install --cask dotnet-sdk`).

## 2. Open
1. Unity Hub → **Add project from disk** → choose `kestrel-heavy/unity` (the folder, not the repo root).
2. On first import the bootstrap runs on its own. It creates `Assets/Settings/KH_URP*.asset`, `Assets/Resources/Materials/*.mat`,
   `Assets/Resources/UI/KH_PanelSettings.asset` and `Assets/Scenes/Slice.unity`, syncs the data, and sets portrait-only iOS PlayerSettings.
   If anything looks unset, run **Kestrel → Setup Project** (it's idempotent).
3. Open `Assets/Scenes/Slice.unity` and press Play. Set the Game view to a portrait resolution (e.g. 1179×2556).
4. Commit the generated `.meta` files and assets if you want them versioned. That is your call, and it doesn't count as a hand edit.

If the Package Manager rejects a version in `Packages/manifest.json` (URP `17.3.0`, Test Framework `1.6.0`), take the version it
offers for 6000.3 and note it in RESULTS.md.

## 3. Run the tests
- **In Unity:** Window → General → **Test Runner** → EditMode → Run All. You should see 17 tests (physics, vehicle, 9 simulate runs and economy).
- **Without Unity (same files):**
  ```bash
  cd unity/dotnet
  dotnet test Kestrel.Core.Tests/Kestrel.Core.Tests.csproj --logger "console;verbosity=detailed"
  ```
  The log ends with `[golden] max rel err ...` lines. If golden files can't be found, set `KESTREL_REPO_ROOT=/path/to/kestrel-heavy`.
- **Regenerating vectors:** after changing JS physics or catalogs, run `npm run gen:golden` at the repo root, then rerun both test suites.

## 4. Build to iOS (local)
1. File → Build Profiles → **iOS** → Switch Platform. (Or run `Kestrel.Slice.Editor.BuildScript.BuildIOS` in batch mode:
   `/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath unity -executeMethod Kestrel.Slice.Editor.BuildScript.BuildIOS`.)
2. Build → choose `unity/build/iOS`. Unity exports an Xcode project.
3. Open `build/iOS/Unity-iPhone.xcodeproj`. Under the Signing & Capabilities tab for target **Unity-iPhone**, check that it shows **Automatically manage signing**, Team **9TR6R5LV8M**
   and Bundle Identifier **com.beckify.kestrelheavy.unityproto**. All three are preset by the bootstrap, so this is only a check.
4. Choose your device and press Run. For TestFlight, use Product → Archive → Distribute App → App Store Connect → **TestFlight Internal Only**.

## 5. App Store Connect setup (one time, dev bundle only)
1. developer.apple.com → Identifiers → **+** → App ID `com.beckify.kestrelheavy.unityproto`, and tick **Game Center**.
   (Automatic signing can also register it on the first archive.)
2. App Store Connect → Apps → **+ New App**: iOS, name e.g. "Kestrel Heavy Proto" (it must be unique; it is never released),
   bundle id `com.beckify.kestrelheavy.unityproto`, and SKU `kh-unityproto`.
3. **Game Center test leaderboard:** open the new app → Services (or Features) → Game Center → Leaderboards → **+** → Classic, then:
   - Reference name `MECO altitude (proto)`, **Leaderboard ID `kh.unityproto.meco_alt_m`** (must match `GameCenterBridge.MecoAltitudeBoard`).
   - Score format Integer, sort High to Low, and add one localization (e.g. "MECO altitude", suffix " m").
   - Leaderboards work in the sandbox for TestFlight and dev builds before any review. Sign in to a Sandbox Apple Account on the device
     (Settings → Game Center or Developer → Sandbox Account).
4. TestFlight → Internal Testing → add yourself.

## 6. Game Center plug-in (Apple, not vendored)
Apple's official Unity plug-ins live at **https://github.com/apple/unityplugins** (check its LICENSE file before redistributing anything).
They're built locally, not committed:
```bash
git clone https://github.com/apple/unityplugins && cd unityplugins
python3 build.py -p Core GameKit      # needs Xcode, python3, npm, Unity
```
Then in Unity, go to Package Manager → **+** → *Add package from tarball…* and add `Build/com.apple.unityplugin.core-*.tgz` **first**, then
`com.apple.unityplugin.gamekit-*.tgz`. Unity records the tarballs as `file:` paths in `Packages/manifest.json`. Copy the two `.tgz` files
into `unity/Packages/LocalTarballs/` and point the manifest there if you want CI to have them. Without the plug-in the project still compiles: `GameCenterBridge` logs instead of submitting.
`AppleGameCenter.cs` was written against the plug-in's documented API (`GKLocalPlayer.Authenticate`, `GKLeaderboard.LoadLeaderboards`,
`SubmitScore`) and has **not been compiled yet**. Fix any signature drift there.

## 7. CI: `.github/workflows/unity-proto-ios.yml`
This runs on `workflow_dispatch` and on pushes to `unity-prototype` that touch `unity/**` or the workflow file itself. It has three jobs:
1. **ubuntu-latest (`core-dotnet`):** `dotnet test` on `unity/dotnet/Kestrel.Core.Tests` (no Unity).
2. **ubuntu-latest (`unity-export`):** GameCI `unity-test-runner@v4` (EditMode) and `unity-builder@v4` (`targetPlatform: iOS`,
   `buildMethod: Kestrel.Slice.Editor.BuildScript.BuildIOS`, build number = `github.run_number`). This produces the Xcode project as an artifact.
3. **macos-latest (`testflight`):** a guard that fails unless the bundle id is exactly `com.beckify.kestrelheavy.unityproto`, then `xcodebuild archive` with **automatic signing
   via the App Store Connect API key** (`-allowProvisioningUpdates` plus `-authenticationKeyPath` / `-authenticationKeyID` / `-authenticationKeyIssuerID`), `CURRENT_PROJECT_VERSION=run_number`, then
   `xcodebuild -exportArchive` with the same API-key flags, `destination=upload` and `testFlightInternalTestingOnly`, which uploads to TestFlight.

It does not touch the existing CI workflow, `ios/`, or the shipping bundle id.

**Signing role note:** Apple's docs and Xcode's cloud-managed distribution signing usually want an App Store Connect API key with **Admin**. Our current key is **App Manager** (key id `P2QM4S24MU`); that role successfully created the `com.beckify.kestrelheavy.unityproto` bundle ID and enabled Game Center via the API. The workflow always passes `-allowProvisioningUpdates` with the API key on both archive and export so Xcode can create/update profiles if App Manager is enough. If signing fails with a role error, regenerate the key as Admin and update `ASC_KEY_ID` / `ASC_KEY_P8`.

### CI secrets (repo → Settings → Secrets and variables → Actions → New repository secret)
| Secret | How to get it |
|---|---|
| `UNITY_LICENSE` | **Personal license:** activate Unity on your Mac (Unity Hub → sign in → Manage licenses → Add → Personal), then copy the contents of `~/Library/Application Support/Unity/Unity_lic.ulf` into the secret. See GameCI's guide: https://game.ci/docs/github/activation |
| `UNITY_EMAIL` | The Unity ID email that license belongs to. |
| `UNITY_PASSWORD` | That Unity ID's password. (Use an account without 2FA-by-SMS, or follow GameCI's activation notes.) |
| `UNITY_SERIAL` | **Pro/Plus only, instead of `UNITY_LICENSE`:** your serial key. Leave it unset for Personal. |
| `ASC_KEY_ID` | App Store Connect → Users and Access → Integrations → **App Store Connect API** → Team Keys → **+**. Prefer **Admin** for cloud-managed distribution signing; **App Manager** may work (ours created the proto bundle ID). The Key ID is shown in the list. |
| `ASC_ISSUER_ID` | Shown at the top of that same Team Keys page ("Issuer ID"). |
| `ASC_KEY_P8` | Download the `AuthKey_<KEYID>.p8` (one time only), then `base64 -i AuthKey_<KEYID>.p8 \| pbcopy` and paste. |

Before the first green run: Unity personal licensing in CI must be allowed for your account (GameCI docs), the App Store Connect app
record from step 5 must exist, and the GameCI Docker image for `6000.3.26f1` with the iOS module must exist (check https://game.ci/docs/docker/versions). If it doesn't, bump
`UNITY_VERSION` in the workflow and `ProjectVersion.txt` together.

## What is deliberately missing
- No shock diamonds or ground steam beyond the simple banding particle. There are no VoiceOver labels via `UnityEngine.Accessibility` and no Dynamic Type.
  These are scored as gaps in RESULTS.md.
- The sim is the M1 2-D headless model (strap-ons as a combined serial phase, no fairing jettison), the same one the web designer uses.

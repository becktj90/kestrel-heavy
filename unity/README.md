# Kestrel Heavy: Unity 6.3 LTS

Shipping iOS app. Version **2.0.0**, bundle **`com.beckify.kestrelheavy`**, portrait only (DECISIONS #1, #7, #30).
Phaser in `web/` is the web demo. `ios/` is the unreleased 1.0.1 wrapper. The held workflow
`.github/workflows/unity-proto-ios.yml` still names the old prototype id `com.beckify.kestrelheavy.unityproto`.
`SliceBootstrap` stamps `com.beckify.kestrelheavy` and `2.0.0`.

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

## 0. Agent / automation workflow: Unity CLI only

Unity ToS §17.2(ff) (June 30, 2026) lets AI agents and automation touch the Editor only through Authorized Agentic Access.
On this project, the **only** sanctioned way for agents or scripts to drive Unity is Unity's own CLI (`unity`, installed to `~/.local/bin/unity`
via `curl -fsSL https://unity.com/install.sh | bash`) together with the `com.unity.pipeline` package (now in `Packages/manifest.json`).
**Never invoke the raw Editor binary with `-batchmode` / `-executeMethod`**, whether locally or from an agent.

```bash
unity auth status                         # must show the account owner signed in
unity install 6000.3.26f1 -m ios          # Editor + iOS Build Support (Hub/CLI-managed install only)
cd unity && unity pipeline install        # adds com.unity.pipeline (already committed)
unity open .                              # opens the Editor; a human accepts any Editor terms dialog
unity command                             # list commands the running Editor exposes
unity command <test-command>              # run EditMode tests (golden vectors)
unity command <build-command>             # iOS Xcode export -> build/iOS
```

Use `unity command --query test` and `unity command --query build` to find the exact command names, since the pipeline package is experimental.
GameCI/CI workflows run on Unity's build infrastructure under the repo owner's license, separately from this agent path.

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
- **In Unity:** Window → General → **Test Runner** → EditMode → Run All. The suite is `Kestrel.Core.Tests` plus the other EditMode assemblies. The old "17 tests" figure was the first prototype slice.
- **Without Unity (same files):**
  ```bash
  cd unity/dotnet
  dotnet test Kestrel.Core.Tests/Kestrel.Core.Tests.csproj --logger "console;verbosity=detailed"
  ```
  The log ends with `[golden] max rel err ...` lines. If golden files can't be found, set `KESTREL_REPO_ROOT=/path/to/kestrel-heavy`.
- **Regenerating vectors:** after changing JS physics or catalogs, run `npm run gen:golden` at the repo root, then rerun both test suites.

## 4. Build to iOS (local)
1. File → Build Profiles → **iOS** → Switch Platform. (Agents use the Unity CLI flow in section 0 instead.)
2. Build → choose `unity/build/iOS`. Unity exports an Xcode project.
3. Open `build/iOS/Unity-iPhone.xcodeproj`. Under the Signing & Capabilities tab for target **Unity-iPhone**, check that it shows **Automatically manage signing**, Team **9TR6R5LV8M**
   and Bundle Identifier **com.beckify.kestrelheavy**. Version is **2.0.0**. Portrait is the only orientation. The bootstrap sets these.
4. Choose your device and press Run. For TestFlight, use Product → Archive → Distribute App → App Store Connect → **TestFlight Internal Only**.

## 5. App Store Connect setup (prototype record)

The shipping identifier is `com.beckify.kestrelheavy` (DECISIONS #30). The steps below are the old internal prototype record.
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

## 7. CI

Shipping checks:

- `.github/workflows/ci.yml` runs the node tests and the legacy iOS simulator build.
- `.github/workflows/core-dotnet.yml` runs the core tests with no Editor. It still runs `TimelineTests.Generation_IsFasterThanAFewMilliseconds`.
- `.github/workflows/unity.yml` runs EditMode, PlayMode screenshots, and an iOS export compile-check. It already skips `ReadoutV2Tests.Recompute_MeetsLatencyBudget` and `TimelineTests.Generation_IsFasterThanAFewMilliseconds` (PR #170). Those are the only wall-clock millisecond budgets in the suites.

`.github/workflows/unity-proto-ios.yml` is manual (`workflow_dispatch` only). It is the held prototype workflow, and it still guards `com.beckify.kestrelheavy.unityproto`. Its jobs:

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

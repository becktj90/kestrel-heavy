# Kestrel.Game (U2: shell + portrait)

Code-first: nothing here needs a hand-made scene or a Project Settings edit. Run **Kestrel → Setup Shell (U2)**
(also runs on load and before every build) and everything below is generated.

| Path | What |
|---|---|
| `Layout/` | `Kestrel.Game.Layout` (noEngineReferences): VISUAL_SPEC §5 envelop scale, k, safe/content rect (+8 pt), vehicle anchor 0.38, plume clamp 0.8; §4 `spaceBlend` + themed fill `lerpRgb(wash.horizon, wash.space, t)`. |
| `Quality/Core/` | `QualityGovernor` (noEngineReferences): 120-frame p95; >20 ms for 3 s → down one preset; ≤16.7 ms for 30 s → up to the chosen preset; floor Low. |
| `Quality/Runtime/` | `QualityTable` (loads `data/quality.json`), `QualityService` (applies preset: QualitySettings level, URP render scale = min(1, dprCap/dpr), MSAA, HDR, fps, battery saver 30 fps, particle budget). |
| `Shell/` | `AppShell` bootstrap, `SceneFlow` (Boot → Home/Design/Flight, additive), `PortraitGuard`, `SafeAreaRoot`, `ThemedFillCamera`, `VisualSpecData`, `ScreenStub`. |
| `Shell/UI/` | `KH_Theme.tss` + `KH_Tokens.uss` (UI Toolkit tokens in points). |
| `Editor/` | `GameDataSync` (data/visual-spec.json, tod.json, quality.json → Resources/KestrelData), `QualitySetup` (QualitySettings levels + one URP asset per preset), `ShellSetup` (scenes, PanelSettings, portrait orientation, build scene list), `PortraitCasesReport`. |
| `../Kestrel.Game.Tests/` | EditMode tests for Layout + Governor (also run under dotnet). |

Data sources (JSON is the single source, plan §1): `data/visual-spec.json` (VISUAL_SPEC §6, drift-tested against the doc),
`data/tod.json` (`npm run gen:tod` from `web/js/tod.js`), `data/quality.json` (G1 presets).

Unity CLI (Kestrel Arcade runs these; agents don't launch the editor):
- setup: `-executeMethod Kestrel.Game.Editor.ShellSetup.EnsureAllMenu`
- scaling cases: `-executeMethod Kestrel.Game.Editor.PortraitCasesReport.Run` → `Logs/portrait-cases.txt`
- tests: EditMode, assembly `Kestrel.Game.Tests`

Not set here (approval gates, plan §6): bundle id, team/signing, `UIRequiresFullScreen` (G5), Xcode project, CI.

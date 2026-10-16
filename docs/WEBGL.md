# Kestrel Heavy on the web (Unity WebGL)

Build: Actions > **WebGL build** (workflow_dispatch), or `Unity -batchmode -quit -projectPath unity -executeMethod Kestrel.Slice.Editor.BuildScript.BuildWebGL` via the official CLI.
Output artifact `webgl-build` = `index.html` + `Build/` (Brotli, `decompressionFallback` on, so GitHub Pages works without Content-Encoding headers) + `TemplateData`.

- Template: `unity/Assets/WebGLTemplates/Kestrel/index.html`. The canvas fills the viewport (`100dvh`, no portrait letterbox and no frame). TAP TO PLAY is the user gesture that unlocks audio and enters fullscreen. FULL/EXIT sits in the top-right corner the game reserves (88×44, 8px inside the safe area) so it does not cover the gear, the flight stage chip, or the designer panels. Resize and orientation restamp the stage. Inside the beckify iframe the page asks the host (`postMessage` source `beckify-kestrel-viewport`) to go immersive, because iOS Safari's native fullscreen letterboxes.
- Single-threaded: no SharedArrayBuffer/COOP/COEP needed. `Parallel.For` paths run sequentially on WebGL.
- Saves go to `Application.persistentDataPath` (IndexedDB-backed `/idbfs`). Separate from the old Phaser version's localStorage keys.
- iOS-only code (haptics, speech, Game Center, Xcode post-process, icon setup) is behind `UNITY_IOS` / assembly constraints / `#if !UNITY_WEBGL`.
- Not deployed by this repo. beckify.com consumes the artifact via a PR in becktj90/beckify.
- Preview: Actions → **WebGL build** (`workflow_dispatch`). Download the `webgl-build` artifact and open `index.html` from a static server (Brotli fallback is on, so `file://` will not load the data). The host page at `/games/kestrel-heavy` hides its own FULL button once this template announces `type: "chrome"`, and resizes the iframe from `type: "viewport"`.

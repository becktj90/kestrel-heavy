# Kestrel Heavy on the web (Unity WebGL)

Build: Actions > **WebGL build** (workflow_dispatch), or `Unity -batchmode -quit -projectPath unity -executeMethod Kestrel.Slice.Editor.BuildScript.BuildWebGL` via the official CLI.
Output artifact `webgl-build` = `index.html` + `Build/` (Brotli, `decompressionFallback` on, so GitHub Pages works without Content-Encoding headers) + `TemplateData`.

- Template: `unity/Assets/WebGLTemplates/Kestrel/index.html` (portrait stage, loading bar, TAP TO PLAY button = the user gesture that unlocks audio).
- Single-threaded: no SharedArrayBuffer/COOP/COEP needed. `Parallel.For` paths run sequentially on WebGL.
- Saves go to `Application.persistentDataPath` (IndexedDB-backed `/idbfs`). Separate from the old Phaser version's localStorage keys.
- iOS-only code (haptics, speech, Game Center, Xcode post-process, icon setup) is behind `UNITY_IOS` / assembly constraints / `#if !UNITY_WEBGL`.
- Not deployed by this repo. beckify.com consumes the artifact via a PR in becktj90/beckify.

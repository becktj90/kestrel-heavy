# Kestrel Heavy

Rocket launch game. **The shipping app is the Unity 2.0.0 build** (`unity/`, DECISIONS #1, #30). 1.0.1 was never released (archived on `release/1.0.1`). The app ships as a standalone iOS app (`com.beckify.kestrelheavy`, team 9TR6R5LV8M) and plays on the web at beckify.com/games/kestrel-heavy.

Split out of [becktj90/beckify](https://github.com/becktj90/beckify) with history preserved.

## Layout

- `unity/` – the Unity 6.3 game. **Ships as 2.0.0** (builds from 100).
- `web/` – the Phaser 4 build. **Web demo only** (beckify.com), not the App Store app.
- 1.0.1 (7) in `ios/KestrelHeavy.xcodeproj` was never released; it is the legacy Phaser/WKWebView wrapper.
- `ios/` – standalone Xcode project `KestrelHeavy.xcodeproj` (scheme `KestrelHeavy`). The `Pack Kestrel Heavy game` build phase runs `ios/scripts/pack_kestrelheavy_game.py`, which copies `web/` into `ios/KestrelHeavy/Game` with iOS/offline patches.
- `tests/` – Node tests (`npm test`).
- `docs/` – App Store listing notes.

## Common tasks

```sh
npm test                 # game logic tests + iOS pack check
npm run pack:ios         # refresh ios/KestrelHeavy/Game from web/
open ios/KestrelHeavy.xcodeproj   # Product > Archive for App Store
```

## Releasing to beckify.com

beckify vendors `web/` at a git tag. Tag a release here (`git tag web-vN && git push --tags`), then in beckify run
`node artifacts/beckify/scripts/sync-kestrel-heavy.mjs web-vN` and open a PR.

# Kestrel Heavy

Retro lander arcade game. Ships as a standalone iOS app (`com.beckify.kestrelheavy`, team 9TR6R5LV8M) and plays on the web at beckify.com/games/kestrel-heavy.

Split out of [becktj90/beckify](https://github.com/becktj90/beckify) with history preserved.

## Layout

- `web/` – the Phaser 4 game. **Single source of truth.**
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

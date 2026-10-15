# Narration audio license

The voice clips in `assets/narration/` (mission control, webcast host and co-host)
were generated on **2026-10-11** with ElevenLabs text-to-speech on Trevor Beck's
**paid ElevenLabs Creator subscription** (active at generation time).

Per ElevenLabs' terms, audio generated while on a paid plan carries a **perpetual
commercial license** that continues after the subscription ends, and **no
attribution is required**. Free-plan audio is not used anywhere in this game.

- Model: `eleven_multilingual_v2` (generally available; not alpha/beta)
- Voices: ElevenLabs premade library voices: Eric (mission control),
  Sarah (host), Chris (co-host)
- Scripts: original text from `web/js/narration/manifest.js` (no player-specific strings)
- Updated 2026-10-11 (same subscription, model and voices): wording fixes, new variants, loudness normalized to -16 LUFS / -1.5 dBTP, 96 kbps mono
- Per-clip details: `assets/narration/manifest.json`
- Regenerate/extend: `tools/clipgen/` (skips unchanged lines by hash; needs `ELEVENLABS_API_KEY`)

## Line list (planned additions)
- Mission control + two webcast hosts (DECISIONS #55).
- **Crew Talk** (DECISIONS #54): Bodie, Junie, Pearl, Sloane. Spanish-sourced lines voiced in English in each character's own dialect and style, full meaning kept, no stock or literal lines. Same Creator plan, no new spend.

## Thrum guide voice (added 2026-10-15)
- 78 clips in `assets/narration/guide/` (kid-line cards of `data/guide.json`, one file per card id, e.g. `first.2.c1.mp3`), voiced as Mabel "Thrum" Pike.
- Generated with ElevenLabs text-to-speech (`eleven_multilingual_v2`) on Trevor Beck's **paid Creator plan**, no new spend. Voice: "Junie Pell v2", a voice generated on Trevor's own account (not a shared-library voice).
- Cards that carry `{tokens}` use a token-free spoken variant; the on-screen caption keeps the live numbers. Ascent (`asc.*`) beats are not voiced.
- Per-clip details: `assets/narration/guide/manifest.json`. Regenerate: `python3 tools/clipgen/gen_guide.py` (needs `ELEVENLABS_API_KEY`).

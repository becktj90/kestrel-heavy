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

## Per-mission historical commentary (added 2026-11-06, DECISIONS #77)
- 80 clips (`host/color.*.m<n>.*`, `cohost/color.*.m<n>.*`), 4 per campaign mission (KH-1..KH-20),
  one historical theme per mission. Generated with ElevenLabs text-to-speech
  (`eleven_multilingual_v2`) on Trevor Beck's **paid Creator plan** — same model, voices
  (Sarah host, Chris co-host) and settings as the 2026-10-11 narration, no new voices.
- Script: `tools/clipgen/mission-lines.json` (machine-readable source of truth);
  plan, sources and per-mission character counts: `docs/plans/per-mission-commentary.md`.
- Characters sent: 5,084 (final script total 4,952; 132 chars were two superseded wordings
  re-recorded one word shorter to fit the 6 s cap). Account before: 6,756 / 229,144 used.
  Account after (counter, 2026-11-06): 8,992 used / 229,144 — the counter lags the 5,084
  sent; re-check before treating as final. Resets Dec 2, 2026. Hash-skipped: unchanged lines
  were not re-billed (raw cache `tools/clipgen/.raw`, local only).
- Every clip is gated to its own mission (`requires: {"mission": "m<n>"}`, `claims: ["mission"]`)
  with a `sources` list, so it only plays on that mission and stays silent in sandbox.
  Per DECISIONS #77 these lines may name real companies, rockets, programs, ships and people
  when telling true spaceflight history; the one verbatim quote is Armstrong's "That is one
  small step for a man, one giant leap for mankind" (Apollo 11, July 20 1969).
- Per-clip details: `assets/narration/manifest.json` (byte-equal Unity copy). Regenerate:
  `python3 /tmp/gen_mission.py` — see the Generation log in
  `docs/plans/per-mission-commentary.md` (the script is kept out of the repo; the lines JSON
  is the committed source).

## Thrum guide voice (added 2026-10-15)
- 78 clips in `assets/narration/guide/` (kid-line cards of `data/guide.json`, one file per card id, e.g. `first.2.c1.mp3`), voiced as Mabel "Thrum" Pike.
- Generated with ElevenLabs text-to-speech (`eleven_multilingual_v2`) on Trevor Beck's **paid Creator plan**, no new spend. Voice: "Junie Pell v2", a voice generated on Trevor's own account (not a shared-library voice).
- Cards that carry `{tokens}` use a token-free spoken variant; the on-screen caption keeps the live numbers. Ascent (`asc.*`) beats are not voiced.
- Per-clip details: `assets/narration/guide/manifest.json`. Regenerate: `python3 tools/clipgen/gen_guide.py` (needs `ELEVENLABS_API_KEY`).

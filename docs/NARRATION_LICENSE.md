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
- Per-clip details: `assets/narration/manifest.json`
- Regenerate/extend: `tools/clipgen/` (skips unchanged lines by hash; needs `ELEVENLABS_API_KEY`)

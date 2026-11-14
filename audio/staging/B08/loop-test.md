# B08 loop test: eleven_text_to_sound_v3, 5 s, loop=true

Official docs (2026-11-11): `POST /v1/sound-generation`, models `eleven_text_to_sound_v2` (default) and `eleven_text_to_sound_v3`. Parameters: `text`, `duration_seconds` (0.5–30), `prompt_influence` (0–1, default 0.3), `loop` (bool). Output `mp3_44100_128` (Creator; 44.1 kHz WAV/PCM needs Pro). The Nov 2, 2026 SFX v3 developer post said looping “currently only works with” v2. This test checks v3 anyway, as planned.

## Call
- model_id: `eleven_text_to_sound_v3`
- duration_seconds: 5, loop: true, prompt_influence: 0.5
- prompt: B08 kerolox roar line (continuous engine roar, no music)
- HTTP 200, ~5.00 s decoded, stereo 44.1 kHz mp3

## Seam (decoded, vs existing `roar-loop`)
v3 5 s loop:
- No fade-out: first/last 50 ms RMS are 0.81× / 0.92× of the body (existing roar-loop is 0.27× / 0.34× — it dips at the wrap).
- Click energy at the wrap is 0.84× the file’s median (no click). Existing is also click-free, because both ends fade down.
- 30 ms RMS at the wrap is 0.87× typical (existing: 0.18×, a hole).
- Sample jump 4.8% of peak; derivative is continuous. Band energy start-vs-end within ~2.6 dB.

**Decision: use v3 with `loop: true` for B08.** All B08 items are 30 s loops. Processing will downmix to mono, apply an 80 ms equal-power crossfade (to close the small sample jump), constant-gain normalize to −16 LUFS with true peak ≤ −1.5 dBTP (matching existing long SFX and the gen5 plan), and ship ogg (gapless loop) plus mp3 (Unity Sfx folder convention).

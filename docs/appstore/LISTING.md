# App Store listing text (Unity 2.0.0) — draft

> **Draft for Trevor. Nothing is entered in App Store Connect from this file.**
> Counts: Name 13/30 · Subtitle 28/30 · Promo ≤170 · Description ≤4000 · Keywords ≤100 bytes.
> Source applied by CI: `tools/asc/listing-1.0.json`.
> No real launch-provider trademarks in this copy.

## Name
```text
Kestrel Heavy
```

## Subtitle (28/30)
```text
Design, launch, land rockets
```

## Promotional text (≤170)
```text
Pick a mission, tune your rocket, then light it up. 20 KH missions, a Codex, and an X-ray cutaway. Offline. No ads. No in-app purchases.
```

## Keywords (≤100 bytes, no spaces after commas in ASC)
```text
orbit,space,booster,simulator,engineering,stem,science,builder,spaceflight,kids,missions,max-q
```

## Description
```text
Kestrel Heavy is a rocket design and launch game for curious kids and working engineers.

Every flight starts in the designer. Pick a mission, begin with a rocket built to try it, then make it yours.

THE DESIGNER
• Choose engine fuel (kerosene or methane), stages, fuel load, and side boosters when the mission allows them
• A readiness check flies your rocket in the game's predictor and says, in plain words, where it expects trouble
• See the speed you need for orbit next to the speed you have
• Tap ? on any row for a short Why? card
• Open the X-ray cutaway to see tanks, engines, fairing, and cargo
• Engineers: turn on Show full designer in Settings for the full parts workshop

THE LAUNCH
• Fly the full sequence: countdown, liftoff, max-Q, staging, fairing separation, and engine cutoff
• Mission control and webcast-style commentary call the flight. The voices are AI-generated
• Pick your camera while you fly
• Day, sunset, and night launches from a detailed pad
• On recovery missions the first stage turns around and tries to land on Haven, our recovery barge
• When a flight falls short, the report shows the real cause and your stars, then returns you to your rocket in the designer
• Optional coast time-warp between calls; skip to the next stage when you want to move on

THE MISSIONS
• 20 missions, KH-1 through KH-20, each with a fictional payload and a mission patch
• A Codex of short lessons on delta-v, thrust-to-weight, specific impulse, staging, max-Q, center of mass, orbits, and booster landing
• Progress saves on your device; Continue picks up where you left off
• Settings for music, sound, commentary, subtitles, time warp, and Reduce Motion

THE FINE PRINT
• No ads. No in-app purchases. No accounts. No internet needed to play
• iPhone and iPad, portrait only

Kestrel Heavy, Haven, and the KH missions are original fiction. Not affiliated with any launch provider or space agency.
```

## What's New (first field when ASC shows it)
```text
Welcome aboard. Kestrel Heavy 2.0 is the first release of the rebuilt game.

• Design in the Starter designer: pick a mission, change engine fuel, stages, fuel, and boosters, and see the readiness check
• Tap ? for Why? cards and open the X-ray cutaway
• 20 KH missions with mission patches, a Codex, and a post-flight report
• Full launch sequence with mission control and webcast-style commentary

Thanks for flying.
```

## App Review notes
```text
No account. No sign-in. Plays offline.

How to test
1. On Home, tap CONTINUE or LAUNCH.
2. Read the mission briefing.
3. On the Starter designer, change engine fuel, stages, fuel, or boosters.
4. Tap LAUNCH.
X-ray opens a cutaway. Tap ? for Why? cards.

Settings includes music, sound, commentary, subtitles, time warp, Reduce Motion, Codex, and Show full designer.

No in-app purchases, ads, analytics, or accounts.

AI-generated voices
Mission control and commentary are AI-generated. The developer generated them ahead of time with ElevenLabs text-to-speech on a paid plan. They ship as bundled audio files. The app does not contact any voice service. Music and art are bundled.

Fiction
Kestrel Heavy, Haven, and the KH missions are original and not affiliated with any launch provider or space agency.
```

## Do not claim (still true on main)

- **Game Center / leaderboards** — plug-in absent; `NullGameCenter` only.
- **"Land the booster" as a guaranteed skill claim** — say "tries to land" until Trevor device-passes Haven.
- **Performance, heat, ProMotion, "honest physics" marketing lines.**
- **Phaser / Pier 7 five-flight arcade** — web demo only.
- Real trademarks in store copy.

## Privacy + age (short)

| Field | Draft | Status |
|---|---|---|
| App privacy | **Data Not Collected** | Draft — needs T8 proof in [`TREVOR_OPEN_ITEMS.md`](TREVOR_OPEN_ITEMS.md) |
| Age rating | **4+**, Made for Kids **No** | Draft — needs T7 confirm |
| Export compliance | Exempt / `ITSAppUsesNonExemptEncryption = NO` | Code path in `IosXcodePostprocess`; confirm in exported plist |

Limits check (when editing): `node tools/asc/check_limits.mjs` and `tests/asc-limits.test.cjs`.

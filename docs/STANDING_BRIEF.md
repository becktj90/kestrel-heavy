# Kestrel Heavy — standing brief

Owner: Trevor Beck. Bundle: `com.beckify.kestrelheavy`. Repo: `becktj90/kestrel-heavy`.
First App Store release: Unity 6 (URP) **2.0.0**, portrait only, full screen on iPad, iOS 16 minimum.
Goal: a groundbreaking, breathtaking game that is fun for kids and deep enough for engineers.

This is the standing brief for Cursor cloud agents (Kestrel Arcade and lane agents). Do not rewrite for Claude Code unless Trevor asks for a second lane.

---

## Top priority: Vehicle Designer

This is the most important part of the game. It must be a comprehensive, highly detailed customizer.

- **Parts:** stages, engines, tanks, fairings, boosters, interstages, landing legs, grid fins, materials and liveries.
- **Live stats:** mass, TWR, delta-v, and per-stage readouts, with honest physics and a failure model.
- Spec and mockups ship to Trevor before net-new designer features that contradict `docs/DESIGNER_V3_SPEC.md`. Existing v3 specs and mockups under `docs/mockups/designer/` and `docs/designer/art/` are the approved package unless Trevor says otherwise.

Designer v3 work packages: `docs/designer-v3/prompts/`. Waves A→H. WP-A–G and WP-UI are on main; remaining: **WP-H**.

## Home and menu

The home screen must be rich and polished, in Beckify’s retro engineering style: dark charcoal, deep navy, cyan accents, and pixel-art circular tiles. Spec: `docs/HOME_MENU_SPEC.md`.

## Launch experience

- Full launch sequence: countdown, hold-down, ignition, liftoff, max-Q, staging, MECO and SECO, fairing separation, and landing.
- Spaceflight Now-style narrator with callouts.
- Realistic physics-driven sound: engine roar that changes with throttle and altitude, staging bangs, and ambient pad sound. Audio must actually play on device.
- Original Kestrel score only. The Suno theme is excluded.

## Visual detail

- Detailed launch pad: tower, flame trench, umbilicals, lighting.
- Detailed fictional environments and backgrounds: sky, horizon, coast, clouds, atmosphere as the rocket climbs.
- Detailed rocket art: engine bells, stringers, panels, legs, fins, fairings.
- Shaders must render on a real device, not just in the Editor.

## Design rules

- Player-first, offline-first. No ads, no IAP, no energy timers, no loot boxes, no pay-to-win. Cosmetics are earned.
- No real trademarks. Missions are named KH-n with fictional payloads; the recovery barge is **Haven**.
- Never use photos, drawings, or data from any site Trevor works at. Use only public or cleared references, logged in `docs/REFERENCE_LEDGER.md`.

## Process

- **$0 spend:** free tiers and free tools only; no paid assets. Ask Trevor before any spending.
- Use Cursor cloud agents. Squash-merge PRs once CI is green, delete branches after merging, keep the repo clean, merge in order.
- Builds go through Xcode Cloud: regenerate `xcode-export` from main, then a manual start sends the build to internal TestFlight.
- Ask Trevor before any App Store submission, signing, privacy, or App Store Connect change.
- Never put secrets in chat or commit them.

## Definition of done

A TestFlight build with working sound and narration, the full launch sequence, a detailed pad and environments, a detailed rocket, the comprehensive designer, and a polished home screen, verified on an iPhone with screenshots.

## Related docs

| Doc | Role |
|-----|------|
| `docs/DESIGNER_V3_SPEC.md` | Vehicle designer contract |
| `docs/HOME_MENU_SPEC.md` | Home / menu |
| `docs/MASTER_ROADMAP.md` | Lanes and critical path |
| `docs/UNITY_SHIP_PLAN.md` | U1–U9 ship plan |
| `docs/DECISIONS.md` | Logged decisions |
| `docs/PROJECT_STATE.md` | Living status |
| `docs/ENGAGEMENT_SPEC.md` | No dark patterns |
| `docs/MUSIC_BIBLE.md` | Original score |
| `docs/REFERENCE_LEDGER.md` | Cleared references |

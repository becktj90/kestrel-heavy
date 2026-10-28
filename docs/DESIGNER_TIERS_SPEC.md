# Designer tiers: one screen, three depths

_Approved by Trevor 2026-10-25 (DECISIONS #69). Mockups: https://claude.ai/artifact/46ccTwtxgKVTPkLjhxcYFo (Kid, Builder, Engineer, X-ray)._

## Why

The pre-#299 designer had four ways in: a 7-step stepper, a 10-tab v3 panel, the Hangar, and a template gallery. Each had its own warnings. It let players build almost anything and then warned them about it. Trevor found it "super complicated" and "too confusing".

The Starter screen (#301) fixed that confusion, but it shows no engineering at all: no numbers, no explanations and no Codex link. Trevor wants a designer that "showcases the engineering of rocket launches and vehicle design" and is still easy in kid mode.

## The rule

There is **one designer screen**: the Starter screen, grown. A **Kid / Builder / Engineer** switch sits at the top of it. Each tier adds detail to the same screen; none opens a different screen.

- The switch is always visible. **Kid is the default.** A player can move up or down at any time. The tier is saved in `kh.settings.tier` (`ExperienceSettings`; ExperienceTier Starter = Kid).
- The rocket preview, the mission header, the readiness line and LAUNCH stay in the same place in every tier.
- **Every control in every tier changes the physics.** Cosmetics live only in Look. Any knob that does nothing, or does the wrong thing, is fixed or removed. It is never shown broken (audit items M-17 to M-29, N-26).
- **Ready is honest.** The badge never says Ready when the launch predictor sees a deterministic red failure: max-Q break-up, thermal, cryo, over-g, short landing fuel, or not fitting the payload. Kid mode shows the plain reason in one line, for example "It would shake apart in the thick air. Try fewer engines or a stronger body." This closes B-10 for the Starter screen.

## Tiers

| | Kid | Builder | Engineer |
|---|---|---|---|
| Controls | Engine fuel, Stages, Fuel (5 notches), Boosters where the mission allows | Kid's controls, plus the engine part (from unlocked parts), engine count, and landing (throw away / back to pad / Haven deck) | Builder's controls, plus per-stage tabs (Engines, Tanks, Structure, Aero, Guidance, Recovery) with the working v3 knobs, and a Stage 1 / Stage 2 / booster selector |
| Readouts | A "Speed to reach orbit" bar with a need marker, a badge, one sentence | A speed budget bar split into orbit, gravity loss, drag loss and landing fuel. Push at liftoff (×weight). Landing fuel kept vs needed. A weight stack (fuel / rocket / cargo). Real units, plain names | A per-stage table (Δv vac, TWR start→end, burn time, m₀/m_f, Isp). The live rocket equation for the selected stage. A delta chip on each knob (for example "+42 m/s"). Vehicle checks: max-Q vs limit, peak g, stability, landing reserve vs need. Predicted risks with their cause |
| Teaching | A "?" on every row opens a short "Why?" card with a tiny picture, using the Codex `kid` text | A "How Δv works" link, using the Codex kid text plus one number | Codex `engineer` text inline, and the equations shown with the player's own numbers |

All three tiers read the same Core numbers: `Readout`, `LaunchGate`, `LaunchPredictor`, `RecoveryTradeoff`. There is one formatter, so the strip and the Hangar can no longer disagree (M-16).

## X-ray (all tiers)

The X-ray button on the rocket preview opens a cutaway:
- the stages with their tank fill levels, the landing reserve band, the interstage, the engines and the fairing with its cargo;
- one tap per part shows a plain-language line, plus numbers in Engineer;
- a flight plan strip with the planned event times: Liftoff, Max-Q, MECO, SECO, Landing.

This builds on the existing exploded view (DECISIONS #60) and the cutaway (#48).

## Retiring the old designer

Once Engineer covers what they do, the old surfaces and the Settings "Show full designer" toggle are removed:
- the v3 panel (`DesignerV3Panel` and its tabs);
- `HangarPanel`, the editor UI part;
- the stepper (`StepperShell`, `StepperControls`);
- the template gallery;
- Make it fly and DvMarginFix.

Their working edit operations (`HangarEditor`, `DesignerV3Edits`, `DesignerKnobs`) stay as the engine under the new screen. Dead code is deleted, not just flagged off: `Experience.Resolve`, `Experience.Available`, `DesignerValidation.Evaluate`, `EngineerEnabled`.

## Delivery (small PRs, in order)

1. **Honest Ready.** Starter readiness uses the launch predictor's red failures and gives a kid-wording reason. Add tests across every mission starter and preset.
2. **Tier switch, Kid "Why?" cards, X-ray.** The switch is persisted, and Kid explainers come from Codex. Add an X-ray view with tests.
3. **Builder.** Engine part picker (unlock-aware), engine count, landing choice, and the four meters.
4. **Engineer.** Per-stage table, live equation, per-stage knob tabs with delta chips. Fix or remove each carried knob: M-17 legs key, M-18 pressure default, M-19 guidance defaults, M-24 separation, M-25 tank length, M-27 throttle, M-28 gimbal, M-29 add-stage copy, and N-26 per-stage.
5. **Retire.** Remove the old surfaces, the toggle and the dead code. Update the tests, the NAV flow and the handoff notes.

Each PR adds EditMode tests, keeps 44 pt hit targets, and extends the device capture tests (`StarterPreviewDeviceTests` and others) so CI uploads PNGs of the new screens at SE, Pro Max and iPad sizes.

## Out of scope here

- Livery and Look changes, beyond keeping a Look tab in Builder and Engineer.
- New physics. This work only exposes physics that already exists.
- Credits and unlock economy changes. Builder only reads unlocks.

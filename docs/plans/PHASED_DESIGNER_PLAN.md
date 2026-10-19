# Kestrel Heavy: phased designer plan

Based on main @ 2029150 (#297 merged), audit draft PR #294, and today's screenshots in /workspace/kh-web-check/. Nothing gets deleted. Each phase hides the next one's features behind flags.

## Where we are today (inventory)

- **Home:** CONTINUE bar (HomeResumeBar), LAUNCH (HomeAutoBuild picks a starter), Design first, MISSIONS, CODEX (reader, or "coming soon"), credits chip, stats card, Thrum strip, and Settings gear. Trevor's iPhone shows overlaps in 5 of these.
- **Designer:** there are 4 ways in. Each one has its own warnings.
  - 7-step guided stepper: Mission, Engines, Tanks, Aero, Recovery, Look, Review. It has 56 check codes.
  - Free v3 panel with 10 tabs: Propellant, Engines, Tanks, Stages, Aero, Structure, Guidance, Recovery, Payload, Look. Simple, Engineer and All parts modes. Landscape adds a 4-tab "More…" variant.
  - Hangar panel with its part picker.
  - Template gallery: 5 templates plus 4 presets.
  - On top of those: the "N things to fix" pill, the warnings list, Fix it / Make it fly / DvMarginFix, Undo/Redo, and LaunchGate.
- **Parts:** 24 engines, 7 propellants, 7 tanks, 6 fairings, 5 fins, 6 materials, 5 interstages, 16 liveries, 12 finishes, 12 payloads. Limits allow 1–5 stages, 1–40 engines per stage, and 0/2/4 strap-ons.
- **What the missions need:**
  - KH-1 and KH-2: sub-orbital hops, 1 stage.
  - KH-3 to KH-20: orbit. Every known winner is 2–3 stages with a kerosene first stage and a hydrogen upper stage. KH-3 uses methane.
  - Landing: RTLS on KH-6, KH-10, KH-19. Haven barge on KH-7, KH-13, KH-15.
  - Boosters that must land: KH-10 and KH-19.
  - Strap-ons allowed: KH-5 and KH-14.
  - g-limit, reliability and random failures: KH-8 and KH-16.
  - Long coast: KH-9 and KH-17 to KH-20.
  - `data/golden/campaign-winners.json` holds a 5/5 winner for all 20 missions. `board-starters.json` covers KH-3 to KH-20.
- **Audit status:** B-01 and B-03 are fixed (#296). Still open: B-02, B-04 to B-10, plus 30 Major items. The audit found the 9 templates and presets fail every mission, and the Simple layer says "ready" on 217 designs that fail.

The main problem: the designer lets players build almost anything, then tries to warn them. Phase 1 turns that around. Players can only pick from combinations we have already flown in tests.

---

## Phase 1 "Starter" (ship now)

**For players:** "Pick a mission and you get a rocket that can do it. Change the engine type, the number of stages, the fuel size and the boosters. Every choice flies. Hit Launch."

**How it works**
- Each mission starts from its known winner in campaign-winners.json. The old templates are not used.
- There are 4 controls:
  - Engine type: Kerosene or Methane. Hydrogen upper stage is automatic from KH-9 on.
  - Stages: 1–3. KH-1 and KH-2 are 1 only.
  - Fuel size: a 5-notch slider, −20% to +20%.
  - Boosters: 0, 2 or 4, shown only where the mission allows them. KH-10 and KH-19 are locked to their landing pair.
- A small offline tool flies every combination for every mission through the shared live setup from #296. It uses 5 seeds, with the failure model on and the budget checked.
  - It writes `data/starter-rockets.json`, which lists only the combinations that pass.
  - The controls snap to the nearest passing combination. They clamp; they don't warn. A choice that can't fly is simply not offered. That's roughly 90 combinations per mission and about 1,800 in total.
- The fairing, landing fuel (including booster landing fuel), payload mass, strength and heat shield are all set automatically from the winner. The payload is locked to the mission's exact mass.
- One readiness line, for example: "Ready. Reaches 200 km with speed to spare." It comes from LaunchGate. LaunchGate should always say Ready on these combinations; a test fails if it doesn't.
- One big Launch button. It is always enabled.
- The flow is Home → briefing → Starter → flight → report. The report's "Next" button opens the next mission's Starter.
- Home has one CTA: CONTINUE, or "LAUNCH KH-n · Name". Below it are MISSIONS and a Settings gear. Settings gets a "Show full designer" toggle that is off by default.

**Hidden behind flags (code stays)**
- New `Core/Features/FeatureFlags.cs` reads `data/features.json`. Settings can override it.
- Designer flags:
  - `designer.stepper` (StepperShell/StepperControls)
  - `designer.free` (DesignerV3Panel free mode, all 10 tabs)
  - `designer.modeTabs` (Simple/Engineer/All parts)
  - `designer.hangar` (HangarPanel, PartPicker)
  - `designer.templates` (template gallery and presets)
  - `designer.fixPill`
  - `designer.warningsList`
  - `designer.makeItFly` (Fix it, Make it fly, DvMarginFix; see M-11)
  - `designer.payloadStepper`
  - `designer.v3Knobs` (pressure, mixture, tune, gimbal, guidance, separation)
  - `designer.look`
  - `designer.recoveryControls`
  - `designer.undoRedo` (Starter gets a Reset button instead)
- Home flags: `home.designFirst`, `home.codexTile`, `home.thrum`, `home.creditsChip`, `home.statsCard`.
  - Codex moves to Settings, then to the Missions screen in Phase 2. Thrum gets one line in the briefing.
- These existing settings become one tier value: `Experience` (simple/standard/engineer), `kh.designer.v3.engineer`, `kh.hangar.engineer`, and `SliceApp.EngineerEnabled`. The tier is starter, builder or engineer.

**Missions that may need retuning (the generator will show which)**
- KH-3: payload 20 kg is below the 50 kg minimum (B-02). Lower the minimum to 10 kg and keep the story.
- KH-12: overshot to 878 km even in calm air.
- KH-19: failed 0/5 in calm air; KH-10 was 3/5 live.
- KH-8 and KH-16: random failures and a 0.98 reliability requirement. Players could lose to bad luck. Turn random failures off in Starter, or retune.
- KH-1 and KH-2: the false "short" warning (M-10) must not reach the readiness line. There is also an open low-frame-rate KH-1 failure (#278 note).
- KH-7, 9, 10, 15 and 17–20 currently pass on paper speed (M-01). Leave scoring as it is in Phase 1, and retune when M-01 is fixed.
- Budgets are about 1000× too big (M-03). That's harmless here because the generator checks the budget.

**PRs, in merge order**
1. `fix(B-02)`: payload minimum 10 kg, plus a test that every winner and board starter has no schema block. Gameplay Dev.
2. `fix(B-04)`: one PayloadFit rule in Validate, Readout, Stepper and LaunchGate, and parse the payload diameter. Gameplay Dev.
3. `feat(flags)`: FeatureFlags, features.json, the tier setting and the Settings toggle. No visible change. Gameplay Dev.
4. `feat(starter-data)`: `tools/starter-gen`, `data/starter-rockets.json`, and `Core/Designer/StarterRocket.cs` (applies the knobs and snaps to a passing combination). Mission retunes go here.
   - Tests:
     - Every entry passes 5/5 on the live setup, LaunchGate says Ready, and the payload fits.
     - Every default combination wins.
   - If the full sweep is too slow for each PR, run a sample there and the full sweep nightly.
5. `feat(designer-starter)`: the Starter screen (Graphics Dev). Mission header, rocket preview, 4 controls, readiness line, Launch, Back. Default flag values hide the rest.
   - Text-fit and overlap tests at 335/390/430 portrait, iPad, and 1280×720/1366×768/1920×1080 web.
6. `feat(home-simple)`: Home with one CTA, Missions and Settings (Graphics Dev). The same size tests. This also removes most of the overlaps on Trevor's iPhone Home.
7. `test(flow)`: a PlayMode run of KH-1 to KH-3 with default choices through the whole flow, plus a Core test that all 20 missions win with default choices.
   - Then deploy the web build, run an iOS export, and do a browser play-through.

**Blockers:** fixed: B-02, B-04 (B-01/B-03 already done). Made moot for Starter players because they can only fly combinations we have flown: B-05, B-06, B-07, B-08, B-09 (landing fuel is automatic), B-10 (one line backed by flown results). Also moot: M-02, M-11, M-17/18/24/25/26/29. Those stay open for Phase 2/3.

---

## Phase 2 "Builder" (unlocks after KH-5; one toggle in data)

**For players:** "Your shop grew. Pick your own engines and fuel, choose a landing (back to pad or Haven barge), and paint your rocket."

- Controls are added one at a time, following `campaign.json` unlocks. This makes unlocks actually gate something (fixes M-04).
  - Stage 2 (KH-2), crossfeed (KH-5), legs and grid fins (KH-6), insulation (KH-7), redundant avionics (KH-8), hydrogen upper (KH-9).
- Engine picker with 3–4 engines per fuel type, engine count, and a fuel slider per stage. Recovery choice: Expend, Return to pad, or Haven barge. Landing fuel is still automatic, with an optional slider.
- Look and paint: liveries, finishes, nose shape. Earned looks unlock (M-05).
- The fairing and payload are still automatic. The readiness line stays. Warnings appear only as one plain line from LaunchGate.
- Builder offers combinations nobody has flown yet, so it **must** wait for: B-05 (failure model always on and a launch block on red), B-06 (one cryo rule), B-07 (heat-shield rule and control), B-08 (solids max-Q), B-09 (booster landing-fuel control), B-10 (Simple stop line).
- **PRs, in order:** B-05 → B-06 → B-07 → B-08 → B-09 → `feat(unlocks)` (M-04/M-05) → `feat(designer-builder)` UI behind `designer.builder` → device-size tests → export.
- **Tests:**
  - Run the #294 verdict harness in CI: the designer's verdict must match FlightBatch for every golden design (N-06).
  - Unlock tests per mission.
  - Text-fit and overlap at all sizes.

---

## Phase 3 "Engineer" (after KH-20, or a Settings toggle with a "for experts" note)

**For players:** "The full workshop: every part, material, interstage, guidance and engine tweak, the guided stepper and the free designer."

- This turns the existing flags back on: stepper, free panel with all 10 tabs, Hangar and part picker, v3 knobs, Make it fly, warnings list and fix pill.
- Before it ships, fix the Majors that make expert numbers lie:
  - M-01 and M-12 to M-30.
  - M-09, so contracts are playable after the campaign.
  - Fix Make it fly (M-11) or leave it hidden.
- **PRs:** one per audit item in tracker order. Then `feat(engineer-unlock)`, which only flips flags and adds the Settings note.
- **Tests:** the verdict harness again, the full designer text-fit sweep (already in CI from #291/#297), and a soak run of the template gallery against all 20 missions.

---

## Owners and order
- Gameplay Dev: Phase 1 PRs 1–4 and 7.
- Graphics Dev: PRs 5–6. The camera picker waits until after PR 6.
- UX: copy and the readiness-line wording.
- Every PR goes into the WebGL build, keeps iOS portrait-only, and doesn't touch xcode-export.
- Phase 1 total: 7 PRs. Phase 2 starts only after Trevor has played Phase 1 on iPhone and web.

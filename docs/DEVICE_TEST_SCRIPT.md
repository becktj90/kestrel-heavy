# Device test script (Trevor)

Run this on a **real iPhone** from the latest TestFlight build of Kestrel Heavy. Portrait only. About **15–20 minutes**.

**Do not claim device verification for anything Trevor has not marked pass here.** CI (llvmpipe) is not a phone.

## Before you start

Write these down:

| Field | Your note |
|---|---|
| TestFlight build number | |
| `main` SHA the export came from (export tag message) | |
| iPhone model | |
| iOS version | |
| Date / time (America/New_York) | |

Leave Low Power Mode off. Volume up. Silent switch **off** unless a step says otherwise.

Mark each step **Pass** or **Fail** and jot what you saw on a Fail.

---

## Checklist

### 1. First launch

- [ ] Pass · [ ] Fail

**Do:** Install (or update) from TestFlight. Fully close any old copy. Open the app once.

**Expect:** Title / Home loads without a crash or blank hang. No network error. Game stays **portrait** if you tip the phone sideways or upside down. Title, primary buttons and text stay on screen (nothing under the notch / Dynamic Island / home bar).

---

### 2. Home navigation (every button + Back)

- [ ] Pass · [ ] Fail

**Do:** From Home, tap each visible primary control in turn: **LAUNCH** (or the rocket), **DESIGN FIRST** (or designer entry), **Missions** / campaign map if shown, **Settings** (gear), **Codex** if shown. From every screen you open, use **Back** / ◀ until you are on Home again.

**Expect:** Every tap lands on a named screen. Every screen has a way back. No dead end, no frozen overlay, no throw to a blank Design/None. CONTINUE (if shown) opens something usable, not a soft-lock.

---

### 3. Starter designer

- [ ] Pass · [ ] Fail

**Do:** Open the Starter designer for **KH-1** (via LAUNCH briefing → designer, or DESIGN FIRST). Change at least one control (engine fuel, stages, fuel notches, or boosters if offered). Open a **?** Why? card if present. Toggle **X-ray** if present. Confirm the readiness badge is readable.

**Expect:** Controls respond to touch (≈44 pt targets feel hittable). Why? / X-ray open and close cleanly. Ready / Tight / Short text is readable and not cut off. **Launch** is enabled when the badge says the rocket can fly.

---

### 4. KH-1 flight

- [ ] Pass · [ ] Fail

**Do:** Launch the KH-1 starter (10 km hop). Let Auto-assist fly. Do not force a failure.

**Expect:** Countdown → liftoff → short hop. A results / report card appears when the flight ends (about half a minute). The flight feels winnable for a first try. Note anything confusing.

---

### 5. Stage separation — land booster vs stay with upper stage

- [ ] Pass · [ ] Fail

**Do:** Fly a recovery mission that stages (for example **KH-6** or **KH-7** with Auto-assist). After MECO / staging, use **Cam** (or the booster-follow path the HUD offers) so you can see the booster. Then switch back so you are following the upper stage / stack again if the UI allows.

**Expect:** Staging is visible and in order. You can tell when the camera is on the booster vs the upper stage (label matches view). HUD does not leave you stuck on a blank follow. No magenta / pink error shapes.

---

### 6. Haven landing

- [ ] Pass · [ ] Fail

**Do:** On a Haven / barge recovery flight (for example **KH-7**), watch the booster through entry to touchdown. If the build offers the player-flown final burn (Settings / Kid vs Engineer path per DECISIONS #79), try one assisted landing and, if time allows, one with the final burn in your hands.

**Expect:** Booster attempts Haven. Deck / barge stays in frame enough to judge the attempt. A grade or miss reason appears afterward. No crash out of the app. (If Auto-assist misses, note sink / offset / tilt — still a Pass for “the attempt ran,” Fail only if the flow breaks.)

---

### 7. Results card

- [ ] Pass · [ ] Fail

**Do:** After any finished flight, read the results card. Tap near the edges once (first tap). Confirm **Retry**, **Back to designer** / **Fix it**, **Next mission** (if shown), **Missions**, and **Home** are present as appropriate.

**Expect:** First tap does **not** get swallowed with no effect (P0 #419). Card shows cause / stars / copy that fits. Pass flights celebrate (MISSION COMPLETE style). Miss flights stay honest (not a false Ready).

---

### 8. Retry

- [ ] Pass · [ ] Fail

**Do:** From the results card tap **Retry** / **One more launch**.

**Expect:** Clean pad, new flight — not a stuck previous plume, half-finished countdown, or leftover pause state (P0 #432). You can complete another flight.

---

### 9. Settings — Thrum off by default + toggle

- [ ] Pass · [ ] Fail

**Do:** Fresh install or clear the corner-character preference if you turned it on earlier. Open **Settings**. Confirm **Show corner character** (Thrum) is **off**. Fly briefly and confirm no corner portrait / tip spam. Turn the toggle **on**, fly once, turn it **off** again. Confirm it survives fully closing the app.

**Expect:** Off by default (DECISIONS #72). On shows Thrum; off hides portrait, tips and her lines. Home Thrum strip stays gated separately (`home.thrum` is off in features). Also spot-check **Music**, **Sound**, **Voice** / **Commentary**, **Subtitles**, **Time warp**, **Reduce motion** if visible — each toggles and persists.

---

### 10. Sound and narration

- [ ] Pass · [ ] Fail

**Do:** Silent switch **off**, volume up. Start a flight and listen through liftoff and at least one call. Flip silent switch **on**, then **off**. If you have headphones, plug in for one short flight. Optional: take a call / ask Siri mid-flight, then return.

**Expect:** Engine / music / voice at a sane level, no harsh clipping. Narration matches what the rocket is doing (right pad / stage events — no invented agency names in store sense; historical commentary may name real programs per DECISIONS #77). Audio returns after interruption. Commentary setting (Full / Calls only / Off) matches what you hear.

---

### 11. Pause, restart, quit

- [ ] Pass · [ ] Fail

**Do:** Mid-flight tap **Pause** → **Resume**. Pause again → **Restart** (full reset). Start another flight, Pause → **Quit** / exit to Home or designer per the menu labels.

**Expect:** Pause freezes the flight; Resume continues. Restart yields a clean new pad flight (#432). Quit leaves flight and lands on a real screen with working controls. (Escape/back hardware key may still not open Pause — NAV-08; on-screen Pause is the iPhone path.)

---

### 12. Relaunch persistence

- [ ] Pass · [ ] Fail

**Do:** Mid-designer or mid-campaign, force-quit the app (app switcher swipe away). Reopen. Use **CONTINUE** if offered, or re-enter the same mission’s designer. Also lock the phone 10 s mid-flight once, unlock; then app-switch away and back once.

**Expect:** Progress / draft still there. CONTINUE does not soft-lock. After lock or app-switch, flight is paused or still runnable — no black screen, no dead controls.

---

### 13. Text does not overflow (small screen)

- [ ] Pass · [ ] Fail

**Do:** On the narrowest phone you have (SE-class if available; otherwise any iPhone). Visit Home, Starter designer (KH-1 and one orbital mission), in-flight HUD (including warp chip and max-Q banners if they appear), and a results card. Rotate only within portrait (no landscape expectation).

**Expect:** No clipped labels, no overlapping primary buttons, no text through the notch or home indicator. Warp chip does not collapse the T+ clock. Designer warning / cost rows do not jump taps onto the wrong control.

---

## After you finish

1. Photo or screen-record any Fail (D4-style failure explosion and a full KH-3 / landing pass help most).
2. Send the filled checklist (build number + SHA + Pass/Fail notes) to Kestrel Arcade / Chief of Staff.
3. Arcade updates `docs/RELEASE_READINESS.md` only for steps you marked Pass — never from CI alone.

## Related

- Living checklist: [`docs/RELEASE_READINESS.md`](RELEASE_READINESS.md)
- App Store listing draft: [`docs/APPSTORE_LISTING_UNITY.md`](APPSTORE_LISTING_UNITY.md) and [`docs/appstore/`](appstore/)
- Export tags: `export/2.0.0-<build>` on `becktj90/kestrel-heavy` (message names the `main` SHA baked into that Xcode export)

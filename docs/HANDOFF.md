# Handoff notes (for Muse or any agent picking this up)

_Written 2026-11-15 by Claude Code. Main is at 9d1eced (#556). Latest export tag: `export/2.0.0-225`._

This page says where the work stands, what is unfinished and where it lives, and what Trevor has decided. It replaces `docs/HANDOFF_GROK.md` as the current handoff; that page is still good for build commands, the CI layout and older traps. The long-lived docs win if they disagree with this page.

## 1. Read these first

| Doc | Why |
|---|---|
| `docs/PROJECT_STATE.md` | House rules and the "Pending with Trevor" list. |
| `docs/DECISIONS.md` | Numbered decisions. Rows are not in number order; the highest number today is #103. Log new calls there. |
| `docs/RELEASE_READINESS.md` | Living release checklist (50/62 verified by CI or config on Nov 10; **0 device-verified**). |
| `docs/DEVICE_TEST_SCRIPT.md` | The 15–20 minute script Trevor runs on a real iPhone. Nothing counts as device-verified until he marks it there. |
| `docs/HANDOFF_GROK.md` | Build and test commands (§3), CI jobs (§4), traps (§7), harnesses (§8). Still accurate except the state sections. |
| `docs/ECONOMY.md` | Economy 2 numbers (DECISIONS #87, #100). |
| `docs/DESIGNER_TIERS_SPEC.md` | Designer plan: Kid / Builder / Engineer. Steps 1–2 shipped; 3–5 not started. |

## 2. Rules that are easy to break

- **Trevor's yes is required for:** spend, App Store and App Store Connect changes, signing, Xcode project changes, privacy or legal changes, and **every export** (`xcode-export.yml`). The full list is in PROJECT_STATE.
- **Merging:** Trevor's standing instruction (Nov 7) is "merge all the PRs when green". Squash-merge, or turn on auto-merge when you open the PR (DECISIONS #66).
- **Never touch `xcode-export` by hand.** Don't open the Unity editor GUI. No new packages or paid assets.
- **Never skip, disable or quarantine a test to get green.**
- **One worktree per agent.** Two agents shared `/home/user/kestrel-heavy` on Nov 14 and switched branches under each other; a commit landed on the wrong branch and uncommitted work was at risk. Give every agent its own `git worktree`, and never run `checkout`, `switch` or `reset` in a checkout another agent uses.
- **Commit early when a usage limit is near.** Agents here stopped three times on rate limits with uncommitted work. Uncommitted files in a cloud container are lost when the container is reclaimed.
- **Real names:** allowed only in the historical commentary lines (DECISIONS #77). `docs/WRITING_BIBLE.md` §6.6 and §8 still ban them everywhere else. Two PRs that proposed widening this (#496, #506) were closed without merging. The game's own world stays fictional (Pier 7, Haven, KH-n). No real trade dress in art or UI.

## 3. What Trevor asked for, and where each item stands

| Ask (his words, shortened) | Status |
|---|---|
| "Tower needs more detail" | Done: #363. |
| "Camera buttons need to work better" | Done: #364. |
| "I still haven't been able to land the booster" | Done: #367 (autopilot fixed on KH-6, 7, 10, 13, 15, 19), then #452 (player flies the final burn, DECISIONS #79). |
| "Designer more technical, kid mode easy" | Partly done: spec #359, honest Ready #365, tier switch + Why cards + X-ray #366. **Builder, Engineer and retiring the old designer (spec steps 3–5) are not started.** Builder and Engineer chips sit behind flags `designer.tierBuilder` / `designer.tierEngineer` (off). |
| "Launches too long… skip to the next stage" | Done: #451 (DECISIONS #78). Coast warp also exists (DECISIONS #88). |
| "ElevenLabs commentary, custom per level, historical/technical/engineering" | Done: #450 and #493 (80 clips each). See §5 for later narration work. |
| "Landing like an arcade game, reminiscent of New Glenn on Jacklyn" | Done: #452. A second version sits unmerged on `feat/arcade-haven-landing` (full manual throttle slider capped at T/W 3.5, suicide-burn gauge, score multipliers). Port pieces onto #452 only if Trevor asks after trying it. |
| "I always get this message on KH-3" (Wrong place, −394 × 470 km) | Diagnosed and fixed in #456: his saved Starter setting (kerosene, lowest fuel dot) can't reach orbit; the badge now warns, the card explains. Later KH-3 fixes: #482, #488, #490. |
| "Make the decisions for me" | DECISIONS #85 (forgiving Starter), #86 (briefing wind on device), #87 (hardware cost matters). See §4. |
| "Make liftoff more fun, it's kind of boring" | **In progress, unfinished.** See §4. |

## 4. Unfinished work (pick up here)

All three branches are based on a30ee9b (Nov 11). Main has moved about 30 commits since, so merge `origin/main` first.

### 4a. Liftoff fun pass: draft PR #558, branch `feat/liftoff-juice` (WIP commit 85af6ab)

Trevor: "Is there anything we can do to make the launch more fun upon liftoff like it's just kind of boring." The plan I committed to:

1. Haptics: rumble at ignition, a kick at clamp release, a buzz through Max-Q.
2. A "release the clamps" tap: thrust gauge climbs into a green zone; release in green earns timing credit; early release waits on the pad (honest physics); late auto-releases with no credit. Kid mode and Auto-assist auto-release. Auto-release at the green time must reproduce today's flight, so goldens and winners still win.
3. Cinematic liftoff camera (only when the player hasn't picked a view; see `FlightCameraControl`, #364).
4. Sound: crackle delayed by distance, bass swell, a sting at liftoff.
5. VFX: deluge steam burst, ground shock ring, heat shimmer, gulls scatter, vapor cone near Mach 1, inside `PadVfxBudgets`.
6. Milestone pop-ups: Tower clear, Mach 1, Max-Q.

What the WIP commit holds (31 files, about 1,700 lines):
- Core: `Flight/ClampRelease.cs`, `ClampGaugeLayout.cs`, `LiftoffTuning.cs`, `Feel/LiftoffHaptics.cs`, edits to `AscentDecisions.cs`, `LiftoffHandoff.cs`, `FlightPresenter.cs`.
- Slice: `ClampGaugeView.cs`, `FlightHaptics.cs`, edits to `FlightJuice.cs`, `SliceApp.cs`, `SliceData.cs`.
- `unity/Assets/Plugins/iOS/KHFlightHaptics.mm`: a native haptics plugin. **Check that it needs no Xcode project change; if it does, ask Trevor first.**
- `data/liftoff.json`, a Haptics setting in `MainMenuOverlay.cs`, `LiftoffClampTests.cs`, `SettingsHapticsTests.cs`, `LiftoffCaptureTests.cs` (the agent was writing this one when it stopped).

Not known: whether it compiles, whether tests pass, and how much of items 3–6 exists. No DECISIONS row is written yet; add one (next free number) saying Trevor asked and Claude chose this package. Nothing here has been felt on a phone.

### 4b. Forgiving Starter (DECISIONS #85): draft PR #559, branch `feat/forgiving-starter` (WIP commit d439dd5, on top of 4c)

Problem: on KH-3 only 2 of 30 Starter settings reach orbit. Decision: every setting with enough delta-v must reach its band on the live pad; the rest read a cause before launch.

What exists: `data/starter-kicks.json` (a per-setting stage-1 pitch kick table, generated by `tools/starter-kicks` against the campaign winners in the seed-1 briefing wind), `Core/Designer/StarterKicks.cs`, three lines in `StarterRocket.Build` that apply the kick, loading in `StarterBasesBoot.cs`, and the data sync entry in `SliceBootstrap.cs`.

What is missing:
- Any test. The required gate is a sweep over every Starter setting on every mission: each one either wins the live pad in the shipped wind or shows a non-Ready cause. Split it per mission so each case fits the per-test timeout (see #448).
- Upper-stage cutoff per setting, if the kick alone is not enough.
- Before/after counts of winning settings per mission.
- A check against DECISIONS #97, which says no pitch-kick edit is handed to the *player*. This table is internal and automatic, so it should be compatible, but read #97 and say so in the PR.
- Regenerating the table after merging main (physics changed in #463–#467).

### 4c. Briefing wind on device (DECISIONS #86): branch `fix/device-briefing-wind` (commit 7afb63b, also the first commit of draft PR #559)

Main already ships the briefing data to the device (`BriefingBooks`, `MissionBriefingBoot`). This commit adds proof: `DeviceBriefingTests.cs` flies the predictor with no checkout and the shipped books installed and checks it matches the editor path; `StarterDefaultWindGateTests` and `LivePadKh3Tests` are extended; `ReferenceAscent.ForgetBriefingRoot()` is a test hook. The agent reported no Starter default or board starter needed a retune. The dotnet tests were not confirmed green after the Nov 14 untangling, so run them before merging.

### 4d. Designer steps 3–5 (not started)

Per `docs/DESIGNER_TIERS_SPEC.md`: (3) Builder tier, (4) Engineer tier with the knob fixes M-17…M-29 and N-26 from `docs/audits/DESIGNER_MISSION_DISCREPANCIES.md` (re-scored in #492), (5) retire the v3 panel, Hangar UI, stepper and templates. Then turn on the two tier flags.

### 4e. Smaller follow-ups

- The countdown capture test (`CountdownCaptureTests.CountdownInEveryCameraAtEveryPhoneSize`) uses about 89% of its 30-minute limit on a normal runner and timed out once on a slow one (#367). Split it by phone size or raise the limit in a small PR.
- `cs-stubcheck --all` fails in `Kestrel.Game.Tests` on missing `LogAssert` / `Assume` stubs. It fails the same way on clean main. Add the stubs.
- #525's strap-on refund change in `PostFlightSource.cs` has no automated test.
- No negative-signal metric exists for the range credit (ENGAGEMENT_SPEC). Suggested: how often it fires per mission.

## 5. State of main

- **Open PRs on Nov 15:** only this page's PR (#557) and the two WIP drafts in §4 (#558, #559). The drafts may show red CI; they are snapshots, not finished work.
- **Narration and sound:** #547 wired the first batch of mission-status calls and #529 added Max-Q narration. About 25 "staging" PRs of unwired clips (#497–#520) and a flight-feedback wiring plan (#522) were open on Nov 14 and are closed now without appearing on main; ask Trevor before redoing any of them. Check `docs/NARRATION_LICENSE.md` and `docs/narration-facts.md` before adding clips. ElevenLabs: the key is a network secret on the cloud environment for `api.elevenlabs.io` (header `xi-api-key`); run generators with `ELEVENLABS_API_KEY=proxy-injected`. About 9k of 229k characters were used when I last checked (Nov 7); check `GET /v1/user/subscription` before generating.
- **Economy 2:** #525 (DECISIONS #87) plus Trevor's "Poly Bridge" steer (DECISIONS #100): over budget blocks launch, a pass pays an unspent-budget bonus, starting bank 100k Kc, range-credit floor 60k Kc, one-time save migration with a `.bak`.
- **Release:** export 225 is the latest tag. App Store listing and screenshots were refreshed (#551, #552, #554). Game Center is out of 1.0 (DECISIONS #98). Test Flight mode is in (DECISIONS #102, #508). Read `docs/RELEASE_READINESS.md` for the true verified/unverified split.
- **Not device-verified:** everything. In particular the player-flown landing, skip to next stage, commentary playback, economy migration and the Starter warnings have only been seen through CI captures.

## 6. Traps from this stretch (in addition to HANDOFF_GROK §7)

- **Unity's NUnit:** `Does.Contain` / `Does.Not.Contain` take only a string. On a collection of non-strings they fail to compile in Unity (CS1503) and break every Unity job, while dotnet passes. `Has.Member` is not in the stub NUnit either. Use `Assert.IsTrue/IsFalse(collection.Contains(x))`.
- **UI Toolkit timing in tests:** style changes (show/hide) apply on the next panel update. Wait a panel update or two before asserting visibility (#366).
- **Wall-clock tests on CI:** runners vary 12–44% in speed. A PlayModeDevice or PlayMode timeout with no assertion failure is usually a slow runner; compare job durations with a passing run before changing code, and re-run once.
- **The phone is not the editor:** a player build has no repo checkout. Anything that reads `data/` through the repo root silently falls back on device. Ship it through Resources and add a no-checkout test (see `DeviceBriefingTests`).
- **Changing a campaign winner moves other tests:** the Starter screen builds its settings from `data/golden/campaign-winners.json`. Retuning a winner (KH-13 in #367) changed which Starter settings fail and why.
- **Parallel PRs from other contributors:** Trevor and other agents (Codex, Cursor, Grok) merge to main constantly. Twice a feature I had agents building was merged by someone else first (#450–#452). Run `git fetch` and check `git log origin/main` and the open PR list before starting, and again before opening a PR.
- **DECISIONS numbering:** rows are appended out of order and numbers collide when two PRs add rows at once. Take the highest existing number plus one right before you merge.
- **Agent safety:** tell subagents explicitly: no `reset --hard`, no force-push, no `pkill` of shared processes, work only in their own worktree.

## 7. Open questions for Trevor

1. Try the landing (#452) on a phone. Does he want the extra suicide-burn gauge and manual throttle from `feat/arcade-haven-landing`?
2. Liftoff haptics need a native iOS plugin. Is that acceptable if it needs no Xcode project change? If it does need one, does he approve it?
3. When does he want the next export? Everything in §3 is on main but not on his phone until an export runs.

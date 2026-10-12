# Gap analysis: every Kestrel Heavy brief, reconciled (X1)

Rule: newest explicit Trevor instruction wins, then DECISIONS.md, then UNITY_SHIP_PLAN.md. Delegation (Trevor, 10-11 10:20 PM) covers design/tech calls; spend, App Store, signing, Xcode, privacy label, legal positions and `UIRequiresFullScreen` stay with Trevor.

## 1. Contradictions

| # | Conflict | Resolution | Source |
|---|----------|------------|--------|
| C1 | Landscape vs portrait | **Portrait-only**, themed fill on big screens; iPad full-screen portrait | Trevor 10:11 PM; DECISIONS #7 |
| C2 | Monorepo (beckify) vs own repo | **Own repo** `becktj90/kestrel-heavy`; beckify.com keeps only the Phaser web demo | Trevor 10:11 PM; DECISIONS #1 |
| C3 | Phaser vs Unity | **Unity 6.3 LTS** ships as 2.0 (same bundle/record); Phaser = web demo, no new G/C/D work; `release/1.0.1` archived, not released | Trevor 10:11 PM; DECISIONS #1 |
| C4 | Paid backend (Fly.io narrator, leaderboards) vs $0 | **Offline-first, $0**. No server in 1.0; Game Center for boards; any future online = deterministic re-sim validation | Trevor 10:15 PM "I don't want to spend anything"; DECISIONS #2 |
| C5 | Rivals / market sim vs player-first | **Player-first**; rivals are flavor + light market (contract prices drift), never blocking progress | Delegation 10:20 PM; DECISIONS #15 |
| C6 | Narrator: "no ElevenLabs" (ship plan §3/U7) vs "I still want ElevenLabs" | **ElevenLabs clips, pre-generated once** on Trevor's paid Creator plan (commercial license persists); on-device TTS only for player-specific strings; no live server | Trevor t92u + t97s545; DECISIONS #16 (supersedes #13 and ship plan U7 text) |
| C7 | Mac setup checklist vs "no setup on my Mac" | **Zero-Mac**: Unity Build Automation free tier, no card, builds only when Trevor says ready | Trevor 10:12/10:18 PM; DECISIONS #2 |
| C8 | Music: AI music vs strict originality / $0 | **Generative + MIDI-rendered stems, free instruments**; AI music only if terms clearly allow paid apps; paid options = one optional ask | Music brief 10:27 PM; DECISIONS #17 |
| C9 | Engineer realism vs kid fun | Physics honest; difficulty via layers/assists (Simple → Standard → Engineer toggle) | DECISIONS #4, #5, #12 |

## 2. Earlier briefs → where they land

| Brief | Status / resolution | Lands in |
|-------|--------------------|----------|
| Designer overhaul M1–M6 | Kept; ported to Unity | U1, U3, U4, U5, U8 |
| Vehicle Design Lab (C1a–C7) | C1a/C2/C4/C6-core in 2.0; C1b/C1c/C3/C7 → 1.1 | U1/U3/U4/U6 |
| Webcast narrator (N1–N6) | Pre-generated ElevenLabs library + subtitles + webcast overlay; no Fly.io | U7 (see C6) |
| SFX (S1, S3) | S1 buses/levels are the spec for the Unity Audio Mixer; S3-core in 2.0 | U7 |
| Studio layer (company, rivals) | Light: company name/logo, rival flavor, light market; deep sim later | U8-lite, X3 |
| Education / Codex | Codex L1 + moment cards + report explanations in 2.0; educator kit X7 | U5, U8, X7 |
| Campaign economy | **Moderate failure penalty**; basic budget in 2.0, full economy/tech tree 1.1 | U8, 1.1 |
| Templates + 3D hangar | T1 templates in U3; hangar 3D reveal in U8 | U3, U8 |
| Adaptive pad + graphics revamp (G1–G7) | VISUAL_SPEC.md is the contract; G7 photo mode → 1.1 | U2, U4, U6 |
| Unity migration | U1–U9, own repo, CLI-only automation | UNITY_SHIP_PLAN.md |
| Music + adaptive score | Music bible, motif, adaptive engine on placeholder stems | X2 → U7 |
| "Every remaining topic" | Specs in X3–X7, tiered 1.0 / 1.1 / later | MASTER_ROADMAP.md |

## 3. Open gaps (owned, not blocking)
- Audience/age rating decision (kids vs 9+/12+) → X6, needs Trevor.
- Names trademark check (Kestrel Heavy, rivals, Haven) → X6 draft for counsel.
- IPA ≤40 MB with URP + voice + music → audit at U4, U7.
- UI Toolkit VoiceOver/Dynamic Type gap → U9.
- Unity ToS note on agent-driven automation → support email drafted by Chief of Staff, Trevor sends.

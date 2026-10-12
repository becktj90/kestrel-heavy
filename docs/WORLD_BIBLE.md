# Kestrel Heavy: World Bible

Player-first. The story is seasoning, not a gate. Every story beat can be skipped,
and none of it ever blocks a launch, a part, or a setting.

Tier tags: **[1.0]** ships in the current build, **[1.1]** next feature release,
**[Later]** parked (2.0 or after). If a tag here disagrees with `docs/DECISIONS.md` or
`docs/UNITY_SHIP_PLAN.md`, those docs win and this one gets fixed.

Companion doc: `docs/WRITING_BIBLE.md` (tone, line limits, narrator spec, dialects).

---

## 1. Ground rules [1.0]

- **All fictional.** No real companies, people, vehicles, ships, pads, or trademarks.
  Banned examples: Blue Origin, New Glenn, Jacklyn, LC-36, SpaceX, NASA branding.
  Generic aerospace words (Max-Q, MECO, LOX, methane, GTO) are fine.
- **Honest physics.** The world never bends the sim. Story explains what happened; it
  doesn't change what happened.
- **Skippable everywhere.** One tap skips any scene. A Settings toggle ("Story: On /
  Light / Off") hides story beats entirely. Off still gets missions, contracts, and parts.
- **No darkness for kids.** Rivals are friendly. Failure is a lesson and a cleanup job,
  never a tragedy. No one is ever hurt; crewed payloads always have a working escape story.
- **No real-money hooks.** In-game money is a score you budget with, nothing more.

## 2. Setting and tone [1.0]

**When:** a near-future, slightly optimistic present. Reusable rockets are normal work.
They aren't a miracle, and they aren't routine enough to be boring.

**Where:** a working coastline. A launch pier pokes out into grey-green water, a
harbor town hums behind it, and a recovery barge waits offshore. Weather is real
but kind (no weather holds in KID or Relaxed, per DECISIONS #5).

**Mood:** a hard-working small outfit that loves the job. Think of a good machine
shop at dawn: coffee, checklists, salt air, the occasional cheer. Spectacle on every
flight (DECISIONS #4), and quiet competence between them.

**Two audiences, one world:**
- Kids get big shapes, clear goals, a crew who are glad they showed up, and a booster
  that stands up on a barge.
- Engineers get real units, real failure modes, and a crew who'll talk TWR, Max-Q and
  margins when asked (Codex, Engineer layer), and never otherwise.

## 3. The launch company and home site [1.0]

> **Provisional names, pending a trademark check by Trevor:** Kestrel Launch Works,
> Tidewater Orbital, Lantern Rocketry, Northwind Aerospace.

### Kestrel Launch Works [1.0, provisional name]
The player runs (or joins as chief engineer, then runs) **Kestrel Launch Works**,
"Kestrel" for short. It's a small reusable-launch outfit whose one product is
dependable rides. Its flagship stack is the game's namesake, the **Kestrel Heavy**.

- **Motto (internal):** "Fly it, catch it, fly it again."
- **Logo:** a stylized kestrel hovering (the bird hovers into the wind, as the booster
  hovers into its landing). Original art only; see `docs/VISUAL_SPEC.md`.
- **Fleet speak:** "the stack," "the booster," "the upper." Missions are **KH-n**
  (KH-1, KH-2...). No mission is named after a real person or place.

### Pier 7 (launch site) [1.0]
A generic fictional pad (DECISIONS #9). It's the seventh pier on an old cargo
breakwater that Kestrel converted into a launch pad. It's the "living title pad" from the menu
(DECISIONS #10): the player's own rocket stands there between flights.
- Features: deluge tower, flame trench into the sea, a stubby lighthouse that blinks
  during terminal count, a gantry painted Kestrel orange.
- Pier 7 is a **location only**. It is never a person and never a call sign.

### Haven (recovery barge) [1.0]
The recovery barge downrange. A flat, scuffed deck with a painted target. Spoken of
fondly, like a dependable truck. "Into Haven" is the landing call. Haven is a ship,
not a character; it doesn't talk.

### Around the pier [1.1 / Later]
- **The Hangar [1.1, 3D]** where parts and liveries live. three.js 3D hangar arrives in
  1.1, and a 2.5D treatment in 2.0 (UNITY_SHIP_PLAN U8). The crew hang out here for
  Hangar Talk.
- **Harbor town [Later]**: backdrop vignettes, a diner, kids watching from the seawall.
- **Tracking hut [Later]**: a codex nook for the Engineer layer.

## 4. Voice cast vs. crew cast [1.0]

Two different groups. Keep them apart.

| Group | Who | Where they appear | Medium |
|---|---|---|---|
| **Broadcast voice cast** | Eric (mission control), Sarah (host), Chris (co-host) | In flight, on bus events | Static audio clips + captions |
| **Crew cast** | Rook, Marlo, Hollis, Tamsin (+ extras below) | Hangar, briefings, debriefs, Hangar Talk | Text bubbles (1.0/1.1); voice is Later |

The voice cast spec lives in `docs/WRITING_BIBLE.md` section 6 and is the source of truth for them.
Summary for world use:
- **Eric, Mission Control** [1.0]. He's the flight director at Pier 7 and the only voice that gives
  orders. Flat, clipped, unflappable. He's earned one dry joke per flight.
- **Sarah, Host** [1.0]. She's the broadcast anchor for the launch stream that folks in
  town watch. Warm and a little awed, calm on bad days. She never gives orders.
- **Chris, Co-host** [1.0]. He's the explainer on the stream and gives one plain fact per milestone.
  Curious, never cute.

Crew members never speak over the flight channels. Eric never shows up in Hangar Talk
(he's busy), though the crew mention him.

## 5. The crew cast [1.0 core four; 1.1 extras]

> Kestrel-only originals, defined here first. Dialect rules are in WRITING_BIBLE section 7.

### Rook, propulsion lead [1.0]
- **Role:** Runs engines and the test stand. He's the first one to the pier and the last to leave.
- **Specialty:** Engines and feed systems: thrust, Isp, throttle range, ignition,
  turbopumps, chamber pressure.
- **Voice:** Big, easygoing, practical. Short sentences, shop words, a lot of
  "yep." Talks about engines like they're good dogs.
- **Teaches:** Why Isp matters, why you can't throttle to zero, why a relight needs propellant settled.
- **Kid hook:** He names every engine. He'll explain the "huff" before ignition.

### Marlo, guidance and landing [1.0]
- **Role:** Writes and tunes the landing guidance. She's the youngest on crew and the fastest thinker.
- **Specialty:** GNC: attitude control, the landing burn
  (called the **landing burn** or **hoverslam** in-game), grid fins, the descent corridor.
- **Voice:** Quick and bright. She thinks out loud in numbered steps and loves a good diagram.
- **Teaches:** Why you light late, why you aim off-deck then divert, why tipping happens.
- **Kid hook:** She keeps a chart of every landing on the hangar wall, with stickers.

### Hollis, structures and materials [1.0]
- **Role:** Senior structures engineer. Owns tanks, interstage, legs, heat shielding.
- **Specialty:** Loads and materials: Max-Q, g-limits, tank pressure, thermal margins,
  stainless vs. aluminum-lithium vs. composite (see `web/js/data/materials.js`).
- **Voice:** Measured, dry, kind. She has seen everything once. Speaks in complete sentences.
  Never wastes a word, never makes anyone feel small.
- **Teaches:** Why Max-Q bends rockets, why payloads have g-limits, why margins aren't optional.
- **Kid hook:** She keeps a jar of bolts from every recovered booster.

### Tamsin, mission manager and customers [1.0]
- **Role:** Runs the manifest. Brings in contracts, briefs the player, handles the
  customers, and keeps the books (once economy lands in 1.1).
- **Specialty:** Mission design and orbits: delta-v budgets, target orbits (LEO, SSO,
  GTO, trans-lunar), payload fit, the schedule.
- **Voice:** Crisp, upbeat, organized. Bullet points out loud. Likes the word "lovely."
- **Teaches:** Why GTO costs more than LEO, why a heavier payload means less margin to land.
- **Kid hook:** She gives every customer's payload a nickname.

### Extras [1.1]
Optional, light-touch. Add only if Hangar Talk needs them.
- **Tomas, pad and ground ops.** He runs the deluge and fueling and is proud of Pier 7.
  Specialty: ground systems, propellant loading, the cryogenics. Voice: proud, chatty, harbor slang.
- **Wren, Haven's deck boss.** She skippers the barge and secures the booster after a
  catch. Specialty: recovery, sea state, tie-downs. Voice: salty and few words. She's the only
  crew member stationed offshore.
- **Dot, the hangar cat [Later].** Cosmetic. Sleeps on warm engines. Never speaks.

### Crew rules [1.0]
- Nobody is a genius who's always right. Everyone has a blind spot, and they help each other.
- No romance, no feuds, no tragic backstories.
- They know your name only as "Chief." No player-name placeholders in audio.
- Diverse by default, with no stereotypes. Personalities come from their job and their habits.

## 6. Rival outfits [1.1]

Friendly competition. Rivals post results on the same contract board, wave from
their own piers, and send congratulations. They never sabotage, steal, or gloat.
Rivals don't fly in the player's sim; they appear as board entries and brief notes.

### Tidewater Orbital [1.1, provisional name]
- Big, polished, by the book. Their rockets are expendable workhorses and they
  are proud of it.
- **Flavor:** "Never lost a payload." Takes the safe contracts.
- **Function:** Sets the reliability benchmark on the board. They win a contract if your
  reputation is low.
- **Line sample (Tamsin):** "Tidewater bid on it too. They're good. We're cheaper if we catch it."

### Lantern Rocketry [1.1, provisional name]
- Two-person startup up the coast, all small solid sounding rockets and cubesat rideshares.
- **Flavor:** scrappy and cheerful, always tinkering. Sometimes the kids' favorite.
- **Function:** Competes for suborbital and CubeSat jobs. Offers occasional co-op
  "rideshare" contracts [Later].

### Northwind Aerospace [Later, provisional name]
- A methane-reusable rival on the far side of the ocean, racing for a first reusable upper stage.
- **Function:** Late-game benchmark for heavy GTO and trans-lunar contracts.
  Gives the arc's final act a goal ("who catches the upper first?").

All company names are provisional until Trevor completes a trademark check (O2).

## 7. Narrative arc [1.0 Act I; 1.1 Acts II–III; Later Act IV]

**Rules:** Story beats fire only on the hangar or debrief screens, never in flight
(flight belongs to the voice cast). Each beat is 1 to 4 short Hangar Talk cards, one tap
each, with a "Skip" button always visible. Skipped beats can be reread in the
Logbook. Progress triggers come from mission success, never from watching story.

### Act I: "First Light" [1.0], missions KH-1 to KH-5
- **Premise:** The player arrives at Pier 7 as new chief engineer. The booster has
  never been caught.
- **Beats:**
  1. KH-1 brief: Tamsin welcomes the Chief. Rook shows the stack. (Simple layer.)
  2. KH-2 debrief: First suborbital hop (Science box payload). Hollis's bolt jar starts.
  3. KH-3 debrief: First orbit. Unlocks Standard designer layer (DECISIONS #4); Marlo
     says landing is next.
  4. KH-4/KH-5: First attempted catch on Haven. On success, the crew cheer and the booster
     goes on the hangar wall. On failure, the crew offer kindly support and a "try again."
- **Payloads:** Science box, CubeSat rideshare, LEO satellite.

### Act II: "Regulars" [1.1], about KH-6 to KH-15
- **Premise:** Contracts open (economy lands 1.1). Customers start to trust Kestrel.
- **Beats:** First paid contract, first rival on the board (Tidewater), a reflight of a
  caught booster ("flight-proven" badge), Lantern asks for a rideshare.
- **Payloads:** LEO satellites, Sun-sync imagers, first GTO comsat.

### Act III: "Heavy" [1.1], about KH-16 to KH-25
- **Premise:** The full Heavy configuration and GTO work. Hard margins.
- **Beats:** Hollis's big tank upgrade, a crew-capsule contract (escape system
  always works, and the story says so), first rough sea at Haven (Wren appears), a
  milestone flight with town watching from the seawall.
- **Payloads:** GTO comsats, crew capsule.

### Act IV: "Far Shore" [Later]
- **Premise:** Trans-lunar and recovering the upper stage. Northwind races too.
  Ends with both outfits congratulating each other.
- **Payloads:** Lunar probe.

### Sandbox and Free Flight [1.0]
No story at all. All the parts the player has unlocked, no contracts.

## 8. Light market model [1.1, economy]

Economy lands in **1.1** (UNITY_SHIP_PLAN U8: "C/E economy 1.1-lite"). In 1.0, missions
are just goals with stars, and nothing costs money. Numbers live in `economy.json` /
`web/js/economy-data/constants.js`; this section is the design intent, not the values.

### Currency [1.1]
- **Credits** (in-world name, shown as "cr"). Earned only by flying. No purchase,
  no exchange, no real-money conversion, no premium currency, ever.
- Running out isn't game over: Tamsin offers a small bridge contract or a reserve
  advance (the "reserves/loans" in U8), with friendly terms.

### Contracts [1.1]
- A **contract board** in the hangar shows 3 to 5 offers at a time. Each card has a payload,
  target orbit, a pay range, a deadline in missions (not real time), and a rival bid if any.
- Contract types: **Standard** (deliver to orbit), **Rideshare** (several small payloads),
  **Reflight** (customer asks for a flight-proven booster, which pays a bonus), and **Test** (try
  a new part, low pay).
- No timers in real time. No energy meters. Offers refresh after each flight.

### Payload classes [1.0 data, 1.1 pay]
Mapped to the existing catalog (`web/js/data/payloads.js`):

| Class | Catalog payload | Orbit | Notes |
|---|---|---|---|
| Starter | Science box (`kid-box`) | LEO 200 km | Kid path default |
| Small | CubeSat rideshare (`cubesat`) | LEO 400 km | Lantern competes |
| Medium | LEO satellite (`leo-sat`) | LEO 400 km | Bread-and-butter |
| Heavy | GTO comsat (`gto-comsat`) | GTO | Tidewater competes |
| Crewed | Crew capsule (`crew-capsule`) | LEO 400 km | Act III, gentler g-limits |
| Deep | Lunar probe (`lunar-probe`) | Trans-lunar | Act IV, Later |

Payload limits (mass, volume, max g, heat flux at fairing jettison) are physical and
come from the catalog. The market never fudges them.

### Pricing [1.1]
- **Price paid** = base by class and orbit × reputation factor + bonuses (on time, flight-proven,
  soft catch).
- **Costs** come from the core cost model (`web/js/economy/costModel.js`): hardware,
  propellant, ops/crew days, recovery. Reusing a caught booster is the big savings,
  which is the whole point of the game.
- Show money as a breakdown the player can read, with no hidden fees.

### Reputation [1.1]
- 0 to 5 stars. Goes up for on-orbit delivery, soft catches, and finishing contracts.
- Goes down a little for a lost payload (the "moderate failure penalty," DECISIONS #12;
  `reputationDip`). It never drops below the floor that keeps starter contracts available.
- Shown as a crew mood too: the hangar is busier and brighter with higher reputation.

### Not in scope
Stock markets, real-time timers, ads, loot boxes, gacha, trading, multiplayer economy.

## 9. Feature tier summary

| Feature | Tier |
|---|---|
| Pier 7, Haven, Kestrel Launch Works, KH-n naming | 1.0 |
| Voice cast (Eric, Sarah, Chris) in flight | 1.0 |
| Crew cast core four, Hangar Talk text cards | 1.0 |
| Act I story beats, Logbook, Story On/Light/Off | 1.0 |
| Economy: credits, contract board, pricing, reputation | 1.1 |
| three.js hangar (3D) | 1.1 |
| Acts II–III, rivals Tidewater and Lantern, Tomas, Wren | 1.1 |
| Hangar 2.5D treatment | 2.0 (Later) |
| Act IV, Northwind, harbor town, Dot, crew voice audio | Later |

## 10. Open questions

- **O1.** Do the crew (Rook, Marlo, Hollis, Tamsin) ever get voiced? Currently text-only
  (no new ElevenLabs clips without Trevor's confirmation, DECISIONS #13).
- **O2.** Trademark clearance on Kestrel Launch Works, Tidewater Orbital, Lantern
  Rocketry, Northwind Aerospace before 1.1 ships.
- **O3.** Does the player start as "Chief" (engineer) or own the company? Draft says Chief.

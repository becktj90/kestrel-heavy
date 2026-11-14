# Credits

Fictional credits sit beside the existing Kc career bank. Kc still settles the flight (reward, hardware cost, reuse refund) and the designer strip still shows that engineering quote; the Kc numbers are at the end of this file. Credits are the small hangar wallet: earn them by flying, spend them on hardware and hangar items.

Nothing on the engagement ban list applies. Credits are not purchasable. A miss pays nothing and takes nothing. There is no timer, energy, ad, or streak. Liveries stay earned only (DECISIONS #65) and are not in this table.

Prices live in `data/credits.json`. `data/golden/credits.json` locks the Hopper quote, two part prices, the KH-1 payout, and the gift rule.

## Earn

A pass pays the mission amount every time. The first clear of that mission adds 10. Each new medal on that mission adds 6, once per medal. A miss pays 0.

| Mission | Clear | First clear | One new medal |
|---------|------:|------------:|--------------:|
| KH-1 First Hop | 12 | +10 | +6 |
| KH-2 Edge of Space | 14 | +10 | +6 |
| KH-3 Tiny Orbit | 16 | +10 | +6 |
| KH-4 Weather Eye | 18 | +10 | +6 |
| KH-5 Heavy Lifter | 20 | +10 | +6 |
| KH-6 Come Back | 22 | +10 | +6 |
| KH-7 Haven at Sea | 24 | +10 | +6 |
| KH-8 Crew Day | 26 | +10 | +6 |
| KH-9 Moon Shot | 28 | +10 | +6 |
| KH-10 Big Reuse | 30 | +10 | +6 |
| KH-11 Dawn Ledger | 32 | +10 | +6 |
| KH-12 Glass Meridian | 34 | +10 | +6 |
| KH-13 Flock Yard | 36 | +10 | +6 |
| KH-14 Harbor Trunk | 38 | +10 | +6 |
| KH-15 Relay West | 40 | +10 | +6 |
| KH-16 Second Watch | 42 | +10 | +6 |
| KH-17 Lumen Down | 44 | +10 | +6 |
| KH-18 Nightjar | 46 | +10 | +6 |
| KH-19 Keel Reuse | 48 | +10 | +6 |
| KH-20 Far Hold | 50 | +10 | +6 |

KH-1 with one new medal on the first clear pays 28. The same clear again pays 12.

## Spend

The free starter is the Hopper template. Its quote is 0, so a new hangar can always fly. A quote of 0 always applies. An edit the wallet cannot pay is refused and the previous rocket stays. Removing a part refunds the credits the player actually paid for it.

A rocket that was already on the pad (an older save, a mission handoff, a stock rocket) is a gift. Stripping it does not pay credits. Only the part of the quote the player bought comes back.

| Item | Credits |
|------|--------:|
| Extra stage | 40 |
| Strap-on booster (count above 0) | 28, plus its engine and material |
| Landing legs | 24 |
| Grid fins | 24 |
| RCS | 16 |
| Redundant avionics | 36 |
| Fins, fairing, basic avionics, standard avionics | 0 |
| `gg-kerolox-small` | 0 |
| `al-li` | 0 |
| Steel / Al-Cu / composite / glass-phenolic / titanium | 4 / 6 / 8 / 10 / 14 |
| Unknown engine / unknown material | 22 / 8 |
| Non-starter finishes | 6 or 8 |
| `decal_first_orbit_patch` / `decal_comet_patch` | 6 / 8 |

Starter paint, starter decals, and every livery quote 0.

## Where it shows

Home has a credits chip (`home.creditsChip` on). When a draft, a saved design, or a cleared mission exists, Home also shows CONTINUE. The designer cost readout is still the Kc strip.

On the Starter screen a **Build** row (Shop / Base / Safe) picks the manufacturing grade: Shop is cheaper and more likely to blow up; Safe costs more Kc and holds together better. Base is the mission winner. Hardware cost vs the mission budget and a hold-together percent (`Reliability.Of`) lead the readiness line.

## Saves

Career, designs, the mid-design draft, settings, and the credits wallet are files under `Application.persistentDataPath/saves` (`kh.save.v1.*`). On WebGL that folder is IndexedDB. A write, a pause, and quit call `PlayerPrefs.Save` and `FS.syncfs`, so closing the tab keeps them. The draft is not one of the 8 design slots.

# Kc career bank (economy 2, DECISIONS #87)

Hardware cost matters, gently. Kc settles each campaign flight: a pass pays the mission reward, minus the rocket's hardware cost (the designer strip's recurring quote, `CostModel.PriceDesign`), plus the reuse refund. A miss pays no reward and still pays for the hardware. Kc is never bought, never timed, and never blocks a launch.

Numbers live in `data/campaign.json` (`economy: 2`). Each mission's reward equals its budget, and the budget is still the pass gate for cost (`MissReason` "budget"). Each mission's **Starter default** is its campaign winner (`data/golden/campaign-winners.json`); its hardware is about 11% of the reward, and `EconomyBalanceTests` holds every mission inside 5–20%.

| Mission | Starter default (Kc) | Reward = budget (Kc) | Default / reward | Dearest Starter setting (Kc) |
|---------|------:|------:|------:|------:|
| KH-1 First Hop | 4,839 | 40,000 | 12.1% | 7,533 |
| KH-2 Edge of Space | 4,925 | 45,000 | 10.9% | 8,147 |
| KH-3 Tiny Orbit | 7,926 | 70,000 | 11.3% | 17,140 |
| KH-4 Weather Eye | 16,069 | 140,000 | 11.5% | 29,987 |
| KH-5 Heavy Lifter | 24,332 | 210,000 | 11.6% | 46,757 |
| KH-6 Come Back | 23,279 | 200,000 | 11.6% | 34,157 |
| KH-7 Haven at Sea | 32,180 | 280,000 | 11.5% | 50,405 |
| KH-8 Crew Day | 24,537 | 220,000 | 11.2% | 42,810 |
| KH-9 Moon Shot | 23,478 | 210,000 | 11.2% | 33,420 |
| KH-10 Big Reuse | 43,022 | 380,000 | 11.3% | 76,554 |
| KH-11 Dawn Ledger | 17,133 | 150,000 | 11.4% | 30,349 |
| KH-12 Glass Meridian | 22,855 | 200,000 | 11.4% | 40,930 |
| KH-13 Flock Yard | 18,916 | 165,000 | 11.5% | 32,467 |
| KH-14 Harbor Trunk | 24,643 | 215,000 | 11.5% | 47,212 |
| KH-15 Relay West | 37,676 | 330,000 | 11.4% | 53,023 |
| KH-16 Second Watch | 26,092 | 230,000 | 11.3% | 47,963 |
| KH-17 Lumen Down | 26,662 | 235,000 | 11.3% | 42,470 |
| KH-18 Nightjar | 22,900 | 200,000 | 11.5% | 34,128 |
| KH-19 Keel Reuse | 48,517 | 420,000 | 11.6% | 83,220 |
| KH-20 Far Hold | 26,506 | 230,000 | 11.5% | 42,672 |

Every Starter setting (manufacturing grade, fuel, stages, boosters) stays under the budget, so the budget never fails a Starter rocket. The full designer can still build past it; that is the choice the budget shapes.

Open contracts after KH-20 follow the same rule: a cell's budget (and reward) in `data/golden/contracts-viable.json` is ten times its cheapest flown design, rounded up to 5,000 Kc (about 10%).

## Reuse

A pass that lands the core well refunds 70% of its engines, tanks and recovery gear, minus refurbishment (10% + 5% per earlier flight of that article). On the landing missions that is about 25–32% of the Starter default, so the card shows the cheaper flight ("Recovery 6.6k Kc"). On KH-10 and KH-19 the landed strap-on boosters now join the refund too.

## Never stuck

- The career starts at **100,000 Kc**.
- **Range credit.** A settled flight never leaves the bank below `rangeFloorKc` = **60,000 Kc**. The floor is above the dearest Starter default (KH-19, 48.5k), so the open mission's Starter default is always affordable, however many flights miss and however dear the rocket was. When the range covers part of a bill, the card says "Range credit 4.8k Kc." It is automatic and unlimited: no timer, no wait, no ad, nothing to buy, no counter.
- The Hopper stays free (0 credits), and a mission's Starter default is a gift in credits.
- Hangar items are paid in credits, never in Kc, so they cannot drain the bank.

## Migration from economy 1

Economy 1 paid 2 M–240 M Kc per mission from a 2 M start, so a rocket was about 0.05% of a reward. A career file without `"economy": 2` is rescaled once on load:

bank × (100,000 + new rewards of the cleared missions) / (2,000,000 + old rewards of those missions), rounded to whole minor units, then lifted to the 60,000 Kc floor.

The old reward table is kept in `campaign.json` `bankMigration`. The pre-migration file is kept once as `kh.save.v1.career.json.bak`, the rewritten file records `"economy": 2`, and it is never rescaled again. Example: KH-1 to KH-3 cleared, 8.85M Kc → × 255,000 / 24,000,000 → 94.0k Kc. A fresh career writes `"economy": 2` on its first save.

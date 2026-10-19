# Credits

Fictional credits sit beside the existing Kc career bank. Kc still settles the flight (reward, hardware cost, reuse refund) and the designer strip still shows that engineering quote. Credits are the small hangar wallet: earn them by flying, spend them on hardware and hangar items.

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

Home has a credits chip. When a draft, a saved design, or a cleared mission exists, Home also shows CONTINUE. The designer cost readout is still the Kc strip.

## Saves

Career, designs, the mid-design draft, settings, and the credits wallet are files under `Application.persistentDataPath/saves` (`kh.save.v1.*`). On WebGL that folder is IndexedDB. A write, a pause, and quit call `PlayerPrefs.Save` and `FS.syncfs`, so closing the tab keeps them. The draft is not one of the 8 design slots.

# Similarity check vs. top space-game App Store icons

Source: iTunes Search API (terms: rocket game, space game, rocket landing; US; limit 50 each). 111 unique icons compared.
Metric: 64-bit pHash + dHash Hamming distance (0 = identical; random unrelated images ~32 each). Flag if pHash <= 12 or pHash+dHash <= 22.

| Concept | Closest store icon | pHash | pHash+dHash | Flag |
|---|---|---|---|---|
| c1-kestrel-booster | Solar Smash 2D: Space Game | 26 | 47 | ok |
| c2-haven-landing | Galaxy Attack: Alien Shooter | 24 | 44 | ok |
| c3-mission-patch | Rocket Touchdown | 24 | 38 | ok |
| c4-gridfin-legs | Falcon Lander - SpaceX edition | 22 | 41 | ok |
| c5-kh-monogram | Bacterial Takeover - Idle game | 18 | 41 | ok |
| c6-ascent-arc | Ellipse: Rocket Simulator | 20 | 39 | ok |

Flags: none.
No concept uses real rocket brands, liveries, or logos; all vehicles are the fictional Kestrel Heavy booster and Haven barge.

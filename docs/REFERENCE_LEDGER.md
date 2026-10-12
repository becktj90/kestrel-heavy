# Reference ledger (pad + vehicle detail)

Separate from LICENSES.md (that ledger covers shipped assets/data). This one logs **references that informed design**, never shipped. Allowed sources: general text knowledge, and public-domain US-government imagery (verify PD status per item). Not allowed: real-facility or real-vehicle imagery under copyright, trademarks or markings, anything from Trevor's workplace. No geometry may be traceable to one specific real pad or vehicle.

| # | Date | Source (URL or "text knowledge") | Licence / PD basis | Informed | Reviewer | No-direct-copy attested |
|---|------|------------------------------------|--------------------|----------|----------|------|
| 1 | 2026-10-12 | Text knowledge: general lattice service-tower construction (square column frame, girts, X/K bracing, grated decks per level) | n/a (no imagery) | D1 `TowerLayout` | Graphics Dev | Yes: generic proportions, seeded; no specific pad |
| 2 | 2026-10-12 | Text knowledge: swing-arm umbilical service arms (horizontal box truss, hinge at tower, swing-away retract) | n/a (no imagery) | D1 `SwingArm`, `SwingArmTruss` | Graphics Dev | Yes: dimensions from vehicle fit, not from any real arm |
| 3 | 2026-10-12 | Text knowledge: hinged strongback erector and inclined launch rail for small solids | n/a (no imagery) | D1 `PadGenerator` archetypes | Graphics Dev | Yes: planar lattice / A-frame rail, seeded; no specific facility |

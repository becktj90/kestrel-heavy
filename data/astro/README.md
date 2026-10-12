# data/astro

`horizons-sun-moon.json`: geocentric apparent RA/Dec (of date) of Sun and Moon from NASA/JPL Horizons at 50 seeded TT epochs across 2000–2100. Golden vectors for `Kestrel.Core.Astro.Ephemeris` (R2/S1), tolerance 0.1° (test: `AstroGoldenTests`). Regenerate with `python3 tools/fetch-horizons-astro.py`. Test-only, not shipped. Ledger row in /LICENSES.md.

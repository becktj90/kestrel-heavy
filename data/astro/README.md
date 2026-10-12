# data/astro

`horizons-sun-moon.json`: geocentric apparent RA/Dec (of date) of Sun and Moon from NASA/JPL Horizons at 50 seeded TT epochs across 2000–2100. Golden vectors for `Kestrel.Core.Astro.Ephemeris` (R2/S1), tolerance 0.1° (test: `AstroGoldenTests`). Regenerate with `python3 tools/fetch-horizons-astro.py`. Test-only, not shipped. Ledger row in /LICENSES.md.

`bsc5p-compact.csv`: the 9,000 brightest stars of the Bright Star Catalogue (NASA HEASARC BSC5P, public domain), `id,raDeg,decDeg,vmag,bv`. Loaded by `Kestrel.Core.Astro.StarCatalog.Parse`; tiers take the first 2000 / 5000 / 9000 rows. Regenerate with `python3 tools/fetch-bsc5p.py`. Ships in the app.

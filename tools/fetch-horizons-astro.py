#!/usr/bin/env python3
"""Fetch geocentric apparent RA/Dec of Sun (10) and Moon (301) from JPL Horizons for the
astronomy golden vectors (data/astro/horizons-sun-moon.json). Seeded epochs across 2000-2100, TT.
Usage: python3 tools/fetch-horizons-astro.py > /tmp/raw.json (then reshape as in PR R2/S1)."""
import urllib.request, urllib.parse, json, random
random.seed(42)
jds = [j for j in (2451545.0 + i * 730.5 + random.random() * 365 for i in range(51)) if j < 2488070]
out = {}
for body in ['10', '301']:
    q = {'format': 'json', 'COMMAND': f"'{body}'", 'EPHEM_TYPE': "'OBSERVER'", 'CENTER': "'500@399'",
         'TLIST': "'" + ' '.join(f'{j:.6f}' for j in jds) + "'", 'TLIST_TYPE': "'JD'", 'TIME_TYPE': "'TT'",
         'QUANTITIES': "'2,20'", 'ANG_FORMAT': "'DEG'", 'EXTRA_PREC': "'YES'", 'CSV_FORMAT': "'YES'", 'OBJ_DATA': "'NO'"}
    r = json.load(urllib.request.urlopen('https://ssd.jpl.nasa.gov/api/horizons.api?' + urllib.parse.urlencode(q)))
    out[body] = r['result'].split('$$SOE')[1].split('$$EOE')[0].strip().splitlines()
print(json.dumps({'jds': jds, 'rows': out}))

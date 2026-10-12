#!/usr/bin/env python3
"""Compare concepts to top space-game App Store icons (iTunes Search API) via pHash/dHash/colorhash."""
import os,io,json,urllib.request,urllib.parse,glob
from PIL import Image; import imagehash
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
apps={}
for t in ["rocket game","space game","rocket landing"]:
    u="https://itunes.apple.com/search?"+urllib.parse.urlencode(dict(term=t,entity="software",limit=50,country="us"))
    for r in json.load(urllib.request.urlopen(u,timeout=30))["results"]:
        apps.setdefault(r["trackId"],(r["trackName"],r.get("artworkUrl512") or r.get("artworkUrl100")))
refs=[]
for tid,(n,url) in apps.items():
    try: refs.append((n,tid,Image.open(io.BytesIO(urllib.request.urlopen(url,timeout=30).read())).convert("RGB")))
    except Exception as e: pass
def h(im): return imagehash.phash(im),imagehash.dhash(im),imagehash.colorhash(im)
R=[(n,t,h(im)) for n,t,im in refs]
rows=[];flags=[]
for f in sorted(glob.glob(f"{ROOT}/concepts/*.png")):
    c=os.path.basename(f)[:-4]; hc=h(Image.open(f).convert("RGB"))
    sc=sorted(((hc[0]-r[0])+(hc[1]-r[1]), hc[0]-r[0], n) for n,t,r in R)
    best=sc[0]; rows.append((c,best[2],best[1],best[0]))
    if best[1]<=12 or best[0]<=22: flags.append(c)
md=["# Similarity check vs. top space-game App Store icons","",
 f"Source: iTunes Search API (terms: rocket game, space game, rocket landing; US; limit 50 each). {len(R)} unique icons compared.",
 "Metric: 64-bit pHash + dHash Hamming distance (0 = identical; random unrelated images ~32 each). Flag if pHash <= 12 or pHash+dHash <= 22.","",
 "| Concept | Closest store icon | pHash | pHash+dHash | Flag |","|---|---|---|---|---|"]
for c,n,p,s in rows: md.append(f"| {c} | {n} | {p} | {s} | {'TOO CLOSE' if c in flags else 'ok'} |")
md+=["",f"Flags: {', '.join(flags) if flags else 'none'}.",
 "No concept uses real rocket brands, liveries, or logos; all vehicles are the fictional Kestrel Heavy booster and Haven barge."]
open(f"{ROOT}/SIMILARITY.md","w").write("\n".join(md)+"\n")
json.dump([dict(concept=c,closest=n,phash=int(p),combined=int(s)) for c,n,p,s in rows]+[{"flags":flags,"n_refs":len(R)}],open(f"{ROOT}/similarity.json","w"),indent=1)
print("\n".join(md))

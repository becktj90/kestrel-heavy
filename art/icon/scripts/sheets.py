#!/usr/bin/env python3
"""Legibility grid, light/dark/tinted previews, and the contact-sheet board.png."""
import os,glob,json
from PIL import Image,ImageDraw,ImageFont,ImageOps,ImageFilter
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SIZES=[29,40,58,60,80,87,120,180]
try: F=lambda s: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",s)
except: F=lambda s: ImageFont.load_default()
cs=sorted(glob.glob(f"{ROOT}/concepts/*.png"))
def mask(im,s):
    im=im.resize((s,s),Image.LANCZOS); m=Image.new("L",(s*4,s*4),0)
    ImageDraw.Draw(m).rounded_rectangle([0,0,s*4-1,s*4-1],radius=int(s*4*0.2237),fill=255)
    im.putalpha(m.resize((s,s),Image.LANCZOS)); return im
def dark(im): return Image.eval(im,lambda v:int(v*0.72))
def tinted(im,tint=(120,190,255)):
    g=ImageOps.grayscale(im); g=ImageOps.autocontrast(g)
    return Image.merge("RGB",[g.point(lambda v,c=c:int(v*c/255)) for c in tint])
# grid
W=sum(SIZES)+20*len(SIZES)+260; rowh=200
grid=Image.new("RGB",(W,rowh*len(cs)+60),"#e9edf2"); d=ImageDraw.Draw(grid)
x=260
for s in SIZES: d.text((x,15),f"{s}px",fill="black",font=F(18)); x+=s+20
for i,f in enumerate(cs):
    im=Image.open(f).convert("RGB"); y=60+i*rowh; d.text((10,y+80),os.path.basename(f)[:-4],fill="black",font=F(18)); x=260
    for s in SIZES: m=mask(im,s); grid.paste(m,(x,y+(180-s)//2),m); x+=s+20
grid.save(f"{ROOT}/legibility-grid.png")
# previews
P=Image.new("RGB",(3*560,len(cs)*200+60),"white"); d=ImageDraw.Draw(P)
modes=[("Light","#dfe7f1",lambda i:i),("Dark","#101216",dark),("Tinted","#0a0a0c",tinted)]
for j,(n,bgc,fn) in enumerate(modes):
    P.paste(Image.new("RGB",(560,P.height),bgc),(j*560,0)); d.text((j*560+20,15),n,fill="#888",font=F(24))
    for i,f in enumerate(cs):
        im=fn(Image.open(f).convert("RGB")); y=60+i*200
        for k,s in enumerate([60,120,180]):
            m=mask(im,s); P.paste(m,(j*560+20+k*(200 if k<2 else 160)-(0 if k<2 else -40),y+(180-s)//2),m)
P.save(f"{ROOT}/previews-light-dark-tinted.png")
# board
sim=json.load(open(f"{ROOT}/similarity.json"))
B=Image.new("RGB",(3600,0+3000),"#0b1630"); d=ImageDraw.Draw(B)
d.text((40,30),"Kestrel Heavy — App Icon Concepts (A2)",fill="#f2e6cc",font=F(56))
for i,f in enumerate(cs):
    m=mask(Image.open(f).convert("RGB"),300); x=40+i*330; B.paste(m,(x,130),m)
    d.text((x,440),os.path.basename(f)[:-4],fill="#3fd0e0",font=F(20))
g=grid.copy(); g.thumbnail((1800,1300)); B.paste(g,(40,500))
p=P.copy(); p.thumbnail((1700,1300)); B.paste(p,(1880,500))
y=1860; d.text((40,y),"Similarity vs. top space-game App Store icons (pHash / pHash+dHash; flag if <=12 / <=22)",fill="#f2e6cc",font=F(32))
for k,r in enumerate([r for r in sim if "concept" in r]):
    d.text((40,y+60+k*44),f"{r['concept']:<22} closest: {r['closest'][:40]:<42} {r['phash']:>3} / {r['combined']:>3}  {'FLAG' if r['concept'] in sim[-1]['flags'] else 'ok'}",fill="white",font=F(26))
d.text((40,y+60+7*44),f"{sim[-1]['n_refs']} store icons compared. Flags: {', '.join(sim[-1]['flags']) or 'none'}.",fill="#ffc94a",font=F(28))
fin=mask(Image.open(f"{ROOT}/final/AppIcon-1024.png"),600); B.paste(fin,(2900,2300),fin)
d.text((2900,2240),"Top pick: c1-kestrel-booster",fill="#ffc94a",font=F(30))
B.save(f"{ROOT}/board.png"); print("ok")

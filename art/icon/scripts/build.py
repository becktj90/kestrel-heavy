#!/usr/bin/env python3
"""Reproducible build: writes concept SVGs (+layers), renders PNGs. Usage: python3 art/icon/scripts/build.py"""
import os,sys,io; sys.path.insert(0,os.path.dirname(__file__))
import cairosvg; from PIL import Image
from concepts import CONCEPTS, svg
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FINAL=os.environ.get("KH_FINAL","c1-kestrel-booster")
def render(s,size=1024):
    return Image.open(io.BytesIO(cairosvg.svg2png(bytestring=s.encode(),output_width=size,output_height=size))).convert("RGBA")
for name,(fn,_) in CONCEPTS.items():
    L=fn(); d=os.path.join(ROOT,"concepts"); os.makedirs(d,exist_ok=True)
    full=svg(L["background"]+L["mid"]+L["foreground"])
    open(f"{d}/{name}.svg","w").write(full)
    render(full).convert("RGB").save(f"{d}/{name}.png")
    if name==FINAL:
        fd=os.path.join(ROOT,"final"); ld=os.path.join(fd,"layers"); os.makedirs(ld,exist_ok=True)
        open(f"{fd}/kestrel-heavy-icon.svg","w").write(full)
        render(full).convert("RGB").save(f"{fd}/AppIcon-1024.png")
        for i,k in enumerate(["background","mid","foreground"]):
            s=svg(L[k]); open(f"{ld}/{i}-{k}.svg","w").write(s); render(s).save(f"{ld}/{i}-{k}.png")
print("built", len(CONCEPTS), "concepts; final =", FINAL)

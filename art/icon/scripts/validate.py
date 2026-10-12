#!/usr/bin/env python3
"""Validate App Store icon: 1024x1024, RGB, no alpha channel, fully opaque. Exit 1 on failure."""
import sys; from PIL import Image
p=sys.argv[1] if len(sys.argv)>1 else __import__("os").path.join(__import__("os").path.dirname(__file__),"..","final","AppIcon-1024.png")
im=Image.open(p); errs=[]
if im.format!="PNG": errs.append(f"format {im.format} != PNG")
if im.size!=(1024,1024): errs.append(f"size {im.size} != (1024,1024)")
if im.mode!="RGB": errs.append(f"mode {im.mode} != RGB")
if "A" in im.getbands() or "transparency" in im.info: errs.append("has alpha/transparency")
if "A" in im.getbands() and im.getchannel("A").getextrema()[0]<255: errs.append("not fully opaque")
print(("FAIL: "+"; ".join(errs)) if errs else f"PASS: {p} 1024x1024 RGB opaque")
sys.exit(1 if errs else 0)

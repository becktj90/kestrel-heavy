"""Renders the Hangar designer mockups (docs/DESIGNER_SPEC.md). MOCKUPS ONLY, not shipping art.
Run: python3 docs/mockups/designer/render_mockups.py   (needs Pillow). Output is 2x points."""
import os
from PIL import Image, ImageDraw, ImageFont
S = 2
OUT = os.path.dirname(os.path.abspath(__file__))
NAVY=(14,22,40); PANEL=(22,30,48); PANEL2=(32,42,64); CYAN=(80,210,235); TXT=(232,238,248); DIM=(150,162,185)
WARN=(245,190,70); BAD=(240,95,95); OK=(110,215,140)
def F(sz, bold=False):
    for p in ["/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"]:
        if os.path.exists(p): return ImageFont.truetype(p, int(sz*S))
    return ImageFont.load_default()
def R(d, x, y, w, h, fill, r=10, outline=None, width=1):
    d.rounded_rectangle([x*S, y*S, (x+w)*S, (y+h)*S], r*S, fill=fill, outline=outline, width=width*S)
def T(d, x, y, s, sz=13, c=TXT, bold=False, anchor="la"):
    d.text((x*S, y*S), s, font=F(sz, bold), fill=c, anchor=anchor)
def sky(img, w, h):
    d = ImageDraw.Draw(img)
    for yy in range(h*S):
        t = yy/(h*S); d.line([(0,yy),(w*S,yy)], fill=(int(30+70*t), int(50+90*t), int(95+100*t)))
def rocket(d, cx, base, height, wid, exploded=False):
    gap = 14 if exploded else 0
    s1 = height*0.55; s2 = height*0.28; fa = height*0.17
    y = base
    R(d, cx-wid*0.9, base, wid*1.8, 10, (90,92,96), r=2)            # pad deck
    d.rectangle([(cx+wid*1.2)*S, (base-height*0.9)*S, (cx+wid*1.5)*S, base*S], fill=(70,74,82))  # tower
    for i,(hh,col) in enumerate([(s1,(236,238,242)),(s2,(236,238,242))]):
        d.rectangle([(cx-wid/2)*S,(y-hh)*S,(cx+wid/2)*S,y*S], fill=col, outline=(40,60,110), width=S)
        T(d, cx, y-hh/2, f"S{i+1}", 9, (40,60,110), True, "mm"); y -= hh+gap
    d.polygon([((cx-wid/2)*S,y*S),((cx+wid/2)*S,y*S),((cx+wid/2)*S,(y-fa*0.6)*S),(cx*S,(y-fa)*S),((cx-wid/2)*S,(y-fa*0.6)*S)], fill=(225,228,235), outline=(40,60,110))
    for sx in (-1,1):  # strap-ons
        bx = cx+sx*(wid/2+wid*0.32+(gap if exploded else 0))
        d.rectangle([(bx-wid*0.3)*S,(base-s1*0.75)*S,(bx+wid*0.3)*S,base*S], fill=(220,224,232), outline=(40,60,110), width=S)
def badge(d, x, y, s, col):
    R(d, x, y, 9+len(s)*6.6, 20, col, r=10); T(d, x+5, y+3, s, 10.5, NAVY, True)
def header(d, w, top, kid):
    R(d, 12, top, 74, 44, PANEL, r=10); T(d, 49, top+22, "◀ Home", 13, TXT, False, "mm")
    cx=(86+ (w-12-44*3-8))/2; R(d, 92, top, (w-12-44*3-8)-98, 44, PANEL, r=10); T(d, cx, top+14, "Slot 2 · Workhorse" if w>400 else "Slot 2", 12, TXT, True, "mm"); T(d, cx, top+31, "on this device", 9.5, DIM, False, "mm")
    x = w-12-44*3-8
    for i,s in enumerate(["↶","↷","A|B"]):
        R(d, x+i*48, top, 44, 44, PANEL, r=10); T(d, x+i*48+22, top+22, s, 15 if i<2 else 12, CYAN, True, "mm")
def stepper(d, x, y, w, label, val, unit, sub=None, warn=False):
    R(d, x, y, w, 52, PANEL2, r=10, outline=WARN if warn else None, width=2 if warn else 1)
    T(d, x+12, y+8, label, 11, DIM); T(d, x+12, y+25, f"{val} {unit}", 15, TXT, True)
    if sub: T(d, x+12+len(f'{val} {unit}')*9.5+6, y+29, sub, 9.5, WARN if warn else DIM)
    R(d, x+w-100, y+4, 44, 44, PANEL, r=8); T(d, x+w-78, y+26, "–", 20, CYAN, True, "mm")
    R(d, x+w-50, y+4, 44, 44, PANEL, r=8); T(d, x+w-28, y+26, "+", 20, CYAN, True, "mm")
def readout(d, x, y, w, kid):
    R(d, x, y, w, 46, (10,16,30), r=10)
    items = [("Δv","9,640","m/s",OK),("Liftoff push","1.38","× weight" if kid else "TWR",OK),("Cost","412","kc",TXT)] if kid else \
            [("Δv","9,640","m/s",OK),("TWR pad","1.38","",OK),("Max-Q","31","kPa",TXT),("Margin","1.4","cal",OK),("Mass","612","t",TXT),("Cost","412","kc",TXT)]
    cw = w/len(items)
    for i,(a,b,u,c) in enumerate(items):
        T(d, x+cw*i+cw/2, y+12, a, 9, DIM, False, "mm"); T(d, x+cw*i+cw/2, y+31, f"{b} {u}".strip(), 11.5 if not kid else 13, c, True, "mm")
def tabs(d, x, y, w, names, sel=1):
    xx = x
    for i,n in enumerate(names):
        tw = 14+len(n)*7.2
        if xx+tw > x+w: T(d, x+w-6, y+22, "›", 18, DIM, True, "rm"); break
        R(d, xx, y, tw, 44, CYAN if i==sel else PANEL2, r=22); T(d, xx+tw/2, y+22, n, 12, NAVY if i==sel else TXT, True, "mm"); xx += tw+6
def stacklist(d, x, y, w, sel=1):
    rows = [("Fairing · ogive 5.2 m",None),("Stage 2 · 1× vac methalox",None),("Stage 1 · 9× methalox, 1+8","!"),("Strap-ons · 2× solid",None)]
    for i,(s,f) in enumerate(rows):
        R(d, x, y+i*48, w, 44, CYAN if i==2 else PANEL2, r=8); T(d, x+12, y+i*48+22, s, 12, NAVY if i==2 else TXT, i==2, "lm")
        if f: R(d, x+w-34, y+i*48+10, 24, 24, WARN, r=12); T(d, x+w-22, y+i*48+22, "!", 13, NAVY, True, "mm")
def label(d, w, h, s):
    R(d, 8, 2, w-16, 18, (0,0,0), r=6); T(d, w/2, 11, s, 9.5, (255,220,120), True, "mm")
def phone(name, w, h, top_inset, bot_inset, kid):
    img = Image.new("RGB", (w*S, h*S)); sky(img, w, h); d = ImageDraw.Draw(img)
    header(d, w, top_inset+6, kid)
    panel_h = int(h*0.50)
    py = h - panel_h
    # chip
    badge(d, 12, top_inset+58, ("1 thing to fix" if kid else "1 block · 1 warning"), WARN)
    rocket(d, w/2, py-14, (py-14)-(top_inset+92), 26 if w<400 else 30)
    R(d, 0, py, w, panel_h+20, PANEL, r=18)
    R(d, w/2-22, py+8, 44, 5, DIM, r=3); T(d, w/2, py+24, "▼  Hangar", 12, DIM, True, "mm")
    y = py+40
    readout(d, 12, y, w-24, kid); y += 54
    tabs(d, 12, y, w-24, ["Stack","Engines","Tanks","Boosters","Fairing","Payload","Recovery","Look"] if not kid else ["Presets","Engines","Fuel","Payload","Look"]); y += 52
    if kid:
        for i,n in enumerate(["Hopper","Workhorse","Heavy","Lander"]):
            cw = (w-24-18)/4; R(d, 12+i*(cw+6), y, cw, 56, CYAN if i==1 else PANEL2, r=10); T(d, 12+i*(cw+6)+cw/2, y+28, n, 11.5 if w<400 else 13, NAVY if i==1 else TXT, True, "mm")
        y += 64
        if y+52 < h-bot_inset-66: stepper(d, 12, y, w-24, "Stage 1 engines (ring of 9)", "9", "engines", "too wide", warn=True); y += 60
        if y+52 < h-bot_inset-66: stepper(d, 12, y, w-24, "Fuel in stage 1", "420", "t"); y += 60
    else:
        if h > 800: stacklist(d, 12, y, w-24); y += 196
        stepper(d, 12, y, w-24, "Engine · full-flow methalox (ff-methalox)", "9", "× 2,300 kN SL", "1+8"); y += 60
        stepper(d, 12, y, w-24, "Nozzle expansion ratio", "34", ":1", "Isp 327 SL / 350 vac"); y += 60
        if h > 800: stepper(d, 12, y, w-24, "Stage 1 tank length · Ø 5.0 m", "38.5", "m", "propellant 420 t"); y += 60
    R(d, 12, h-bot_inset-58, w-24, 50, (60,66,82), r=12)
    T(d, w/2, h-bot_inset-41, "LAUNCH", 15, DIM, True, "mm"); T(d, w/2, h-bot_inset-22, ("Fix first: too many engines for this width" if kid else "Fix first: engines don't fit under 5.0 m"), 10, WARN, False, "mm")
    label(d, w, top_inset+2+24 if False else h, f"MOCKUP · {name} · {w}x{h} pt @2x · {'kid layer' if kid else 'engineer layer'}")
    return img
def ipad():
    w,h = 1032,1376; img = Image.new("RGB",(w*S,h*S)); sky(img,w,h); d = ImageDraw.Draw(img)
    side = 400; vx = w-side
    R(d, 12, 30, 74, 44, PANEL, r=10); T(d, 49, 52, "◀ Home", 13, TXT, False, "mm")
    R(d, vx/2-90, 30, 180, 44, PANEL, r=10); T(d, vx/2, 44, "Slot 2 · Workhorse", 12, TXT, True, "mm"); T(d, vx/2, 61, "saved on this device", 9.5, DIM, False, "mm")
    for i,s in enumerate(["↶","↷","A|B","⇕ Explode"]):
        bw = 44 if i<3 else 96; bx = [vx-220, vx-172, vx-124, vx-76-44][i] if i<3 else 12
        if i==3: R(d, 12, h-80, 110, 44, PANEL, r=10); T(d, 67, h-58, "⇕ Exploded", 12, CYAN, True, "mm")
        else: R(d, bx, 30, 44, 44, PANEL, r=10); T(d, bx+22, 52, s, 14, CYAN, True, "mm")
    badge(d, 12, 86, "1 block · 1 warning", WARN)
    rocket(d, vx/2, h-120, h-280, 54, exploded=True)
    T(d, vx/2, h-96, "drag to rotate · pinch to zoom · double-tap to reframe", 11, TXT, False, "mm")
    R(d, vx, 0, side, h, PANEL, r=0); x = vx+16; ww = side-32; y = 30
    T(d, x, y, "HANGAR", 18, TXT, True); T(d, x+ww, y+4, "Engineer", 12, CYAN, True, "ra"); y += 36
    readout(d, x, y, ww, False); y += 54
    R(d, x, y, ww, 120, (10,16,30), r=10)
    for i,(s,dv,tw) in enumerate([("Strap-ons + S1","3,410 m/s","pad 1.38"),("S1 alone","2,180 m/s","1.9 vac"),("S2","4,050 m/s","0.81 vac")]):
        T(d, x+12, y+14+i*34, s, 12, TXT, True); T(d, x+ww/2+20, y+14+i*34, dv, 12, TXT); T(d, x+ww-12, y+14+i*34, tw, 12, DIM, False, "ra")
    y += 130
    tabs(d, x, y, ww, ["Stack","Engines","Tanks","Boosters","Fairing"]); y += 52
    stacklist(d, x, y, ww); y += 200
    stepper(d, x, y, ww, "Engine · full-flow methalox", "9", "×", "pattern 1+8"); y += 60
    R(d, x, y, ww, 92, PANEL2, r=10); T(d, x+12, y+10, "Cluster pattern", 11, DIM)
    for i,p in enumerate(["single","3-row","ring 7","1+8","3+9+21"]):
        pw = (ww-24-16)/5; R(d, x+12+i*(pw+4), y+32, pw, 48, CYAN if i==3 else PANEL, r=8); T(d, x+12+i*(pw+4)+pw/2, y+56, p, 10.5, NAVY if i==3 else TXT, True, "mm")
    y += 100
    stepper(d, x, y, ww, "Nozzle expansion ratio", "34", ":1", "327 / 350 s"); y += 60
    stepper(d, x, y, ww, "Tank length · Ø 5.0 m", "38.5", "m", "", warn=True); y += 60
    R(d, x, y, ww, 56, (60,40,20), r=10, outline=WARN, width=2); T(d, x+12, y+10, "Engines don't fit (engines-dont-fit)", 11.5, WARN, True); T(d, x+12, y+30, "9 engines need Ø 5.3 m. Try 7, or widen the stage.", 11, TXT)
    R(d, x, h-24-58, ww, 50, (60,66,82), r=12); T(d, x+ww/2, h-24-41, "LAUNCH", 15, DIM, True, "mm"); T(d, x+ww/2, h-24-22, "Fix first: engines don't fit", 10, WARN, False, "mm")
    label(d, w, h, "MOCKUP · iPad 13in portrait · 1032x1376 pt @2x · engineer layer, side panel, exploded view")
    return img
shots = {"designer-se-kid.png": phone("iPhone SE", 375, 667, 20, 0, True),
         "designer-se-engineer.png": phone("iPhone SE", 375, 667, 20, 0, False),
         "designer-promax-engineer.png": phone("iPhone Pro Max", 440, 956, 62, 34, False),
         "designer-ipad13-engineer.png": ipad()}
for n, im in shots.items(): im.save(os.path.join(OUT, n)); print(n, im.size)

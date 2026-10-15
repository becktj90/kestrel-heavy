"""Static, Latin-subset instances of Fraunces and Space Grotesk (both SIL OFL 1.1) for UI Toolkit.
Unity cannot use variable-font axes, so we pin weights and subset to ASCII plus a few punctuation marks.
Needs fontTools (pip install fonttools). Sources: Google Fonts (github.com/undercasetype/Fraunces, github.com/floriankarsten/space-grotesk)."""
import sys
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools import subset
SRC = "/usr/share/fonts/truetype/sand-box/google/"
OUT = sys.argv[1] if len(sys.argv) > 1 else "."
JOBS = [
 ("Fraunces/Fraunces-VariableFont_SOFT,WONK,opsz,wght.ttf", dict(wght=900, opsz=72, SOFT=0, WONK=0), "Fraunces-Black.ttf", "Fraunces Black"),
 ("Fraunces/Fraunces-VariableFont_SOFT,WONK,opsz,wght.ttf", dict(wght=700, opsz=36, SOFT=0, WONK=0), "Fraunces-Bold.ttf", "Fraunces Bold"),
 ("Space Grotesk/SpaceGrotesk-VariableFont_wght.ttf", dict(wght=500), "SpaceGrotesk-Medium.ttf", "Space Grotesk Medium"),
 ("Space Grotesk/SpaceGrotesk-VariableFont_wght.ttf", dict(wght=700), "SpaceGrotesk-Bold.ttf", "Space Grotesk Bold"),
]
UNI = list(range(0x20, 0x7F)) + [0xB7, 0x2013, 0x2014, 0x2192, 0x2019]
for src, axes, name, family in JOBS:
    f = TTFont(SRC + src)
    f = instantiateVariableFont(f, axes, inplace=False)
    opt = subset.Options(); opt.layout_features = ["kern", "liga"]; opt.hinting = False; opt.name_IDs = [0, 1, 2, 3, 4, 5, 6, 13, 14]; opt.notdef_outline = True
    s = subset.Subsetter(opt); s.populate(unicodes=UNI); s.subset(f)
    f.save(f"{OUT}/{name}")
    cm = f.getBestCmap(); print(name, [hex(u) for u in (0xB7, 0x2192) if u not in cm])

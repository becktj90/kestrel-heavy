"""Kestrel Heavy icon concepts as layered SVG (1024x1024, full-bleed, no text except KH monogram).
Each concept returns {'background','mid','foreground'} SVG fragment strings. Fictional world only."""
NAVY="#0b1630"; DEEP="#060d1f"; CHAR="#2a2f3a"; CREAM="#f2e6cc"; RUST="#c8642a"
FLAME="#ff7a1a"; GOLD="#ffc94a"; CYAN="#3fd0e0"; STEEL="#8a96aa"

def svg(body):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024" viewBox="0 0 1024 1024">{body}</svg>'

def bg_grad(top, bot, id_="g"):
    return (f'<defs><linearGradient id="{id_}" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{top}"/>'
            f'<stop offset="1" stop-color="{bot}"/></linearGradient></defs><rect width="1024" height="1024" fill="url(#{id_})"/>')

def booster(cx, top, h, w, body=CREAM, cap=RUST):
    x=cx-w/2
    return (f'<path d="M{x} {top+w*0.9} Q{cx} {top-w*0.35} {x+w} {top+w*0.9} L{x+w} {top+h} L{x} {top+h} Z" fill="{body}"/>'
            f'<rect x="{x}" y="{top+h*0.62}" width="{w}" height="{h*0.08}" fill="{cap}"/>')

def kestrel_booster():
    bg=bg_grad("#1b3a6b",NAVY)
    mid=(f'<circle cx="512" cy="430" r="300" fill="{RUST}" opacity="0.18"/>'
         # swept falcon wings
         f'<path d="M512 470 L120 330 L230 420 L150 430 L300 500 L250 520 L512 560 Z" fill="{RUST}"/>'
         f'<path d="M512 470 L904 330 L794 420 L874 430 L724 500 L774 520 L512 560 Z" fill="{RUST}"/>')
    fg=(booster(512,170,560,150)+
        f'<path d="M437 730 L512 900 L587 730 Z" fill="{FLAME}"/><path d="M472 730 L512 830 L552 730 Z" fill="{GOLD}"/>'
        f'<circle cx="512" cy="330" r="22" fill="{NAVY}"/>')
    return dict(background=bg,mid=mid,foreground=fg)

def haven_landing():
    bg=bg_grad("#14284d",DEEP)
    mid=(f'<rect y="800" width="1024" height="224" fill="#0f2a44"/>'
         f'<rect x="232" y="770" width="560" height="60" rx="8" fill="{CHAR}"/>'
         f'<rect x="232" y="770" width="560" height="14" fill="{GOLD}"/>'
         f'<circle cx="512" cy="760" r="0" fill="none"/>'
         f'<ellipse cx="512" cy="770" rx="230" ry="40" fill="{FLAME}" opacity="0.55"/>')
    fg=(f'<path d="M430 690 L512 770 L594 690 Z" fill="{FLAME}"/><path d="M470 690 L512 745 L554 690 Z" fill="{GOLD}"/>'
        +booster(512,170,520,130)+
        f'<path d="M447 600 L360 720 L380 720 L460 640 Z" fill="{STEEL}"/><path d="M577 600 L664 720 L644 720 L564 640 Z" fill="{STEEL}"/>')
    return dict(background=bg,mid=mid,foreground=fg)

def mission_patch():
    bg=f'<rect width="1024" height="1024" fill="{NAVY}"/>'
    mid=(f'<circle cx="512" cy="512" r="400" fill="{RUST}"/><circle cx="512" cy="512" r="350" fill="#13274a"/>'
         f'<path d="M162 700 Q512 520 862 700 L862 862 L162 862 Z" fill="#1e5a7a" clip-path="url(#c)"/>'
         f'<defs><clipPath id="c"><circle cx="512" cy="512" r="350"/></clipPath></defs>'
         f'<ellipse cx="512" cy="512" rx="380" ry="120" fill="none" stroke="{CYAN}" stroke-width="22" transform="rotate(-28 512 512)"/>'
         f'<circle cx="300" cy="320" r="18" fill="{GOLD}"/><circle cx="720" cy="300" r="12" fill="{CREAM}"/>')
    fg=booster(512,230,480,120)+f'<path d="M452 710 L512 830 L572 710 Z" fill="{FLAME}"/>'
    return dict(background=bg,mid=mid,foreground=fg)

def gridfin_legs():
    bg=f'<rect width="1024" height="1024" fill="{CHAR}"/>'
    mid=f'<circle cx="512" cy="512" r="380" fill="{NAVY}"/>'
    fins=''
    for x in (312,632):
        fins+=f'<rect x="{x}" y="250" width="80" height="80" fill="none" stroke="{CYAN}" stroke-width="16"/>'
        fins+=f'<path d="M{x} 290 H{x+80} M{x+40} 250 V330" stroke="{CYAN}" stroke-width="10"/>'
    fg=(f'<rect x="432" y="200" width="160" height="560" rx="10" fill="{CREAM}"/>'
        f'<rect x="432" y="430" width="160" height="40" fill="{RUST}"/>'+fins+
        f'<path d="M440 680 L300 840 L330 840 L470 720 Z" fill="{CREAM}"/><path d="M584 680 L724 840 L694 840 L554 720 Z" fill="{CREAM}"/>'
        f'<rect x="270" y="840" width="90" height="20" fill="{GOLD}"/><rect x="664" y="840" width="90" height="20" fill="{GOLD}"/>')
    return dict(background=bg,mid=mid,foreground=fg)

def kh_monogram():
    bg=bg_grad(NAVY,DEEP)
    hexpts=" ".join(f"{512+400*__import__('math').cos(__import__('math').radians(a)):.0f},{512+400*__import__('math').sin(__import__('math').radians(a)):.0f}" for a in range(-90,270,60))
    mid=f'<polygon points="{hexpts}" fill="{RUST}"/><polygon points="{hexpts}" fill="#13274a" transform="translate(512 512) scale(0.86) translate(-512 -512)"/>'
    # K and H as blocky shapes; H crossbar is a booster
    fg=(f'<rect x="250" y="330" width="80" height="360" fill="{CREAM}"/>'
        f'<path d="M330 500 L460 330 L540 330 L400 510 L540 690 L460 690 Z" fill="{CREAM}"/>'
        f'<rect x="560" y="330" width="80" height="360" fill="{CREAM}"/><rect x="694" y="330" width="80" height="360" fill="{CREAM}"/>'
        f'<rect x="640" y="485" width="54" height="50" fill="{FLAME}"/>'
        f'<path d="M512 120 L540 190 L484 190 Z" fill="{GOLD}"/>')
    return dict(background=bg,mid=mid,foreground=fg)

def ascent_arc():
    bg=bg_grad("#0d1d3d","#2b1a3a")
    mid=(f'<circle cx="512" cy="1500" r="760" fill="#163a5c"/><circle cx="512" cy="1500" r="760" fill="none" stroke="{CYAN}" stroke-width="14"/>'
         f'<path d="M180 820 Q300 300 820 180" fill="none" stroke="{GOLD}" stroke-width="18" stroke-dasharray="40 34"/>')
    fg=(f'<g transform="rotate(40 600 420)">'+booster(600,230,380,110)+
        f'<path d="M545 610 L600 740 L655 610 Z" fill="{FLAME}"/></g>')
    return dict(background=bg,mid=mid,foreground=fg)

CONCEPTS={
 "c1-kestrel-booster":(kestrel_booster,"Kestrel wings swept off a cream heavy-lift booster on a flame plume"),
 "c2-haven-landing":(haven_landing,"Booster with legs touching down on the Haven barge, plume splashing the deck"),
 "c3-mission-patch":(mission_patch,"Retro circular mission patch: booster crossing a cyan orbit ring over a planet limb"),
 "c4-gridfin-legs":(gridfin_legs,"Minimal mark: booster body, cyan grid fins, splayed landing legs with gold pads"),
 "c5-kh-monogram":(kh_monogram,"Bold KH monogram in a rust hex badge; the H crossbar is a lit engine"),
 "c6-ascent-arc":(ascent_arc,"Tilted booster riding a dashed trajectory arc above a cyan horizon"),
}

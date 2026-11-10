#!/usr/bin/env python3
"""Kestrel Heavy App Store screenshot renderer.

Builds each size's set as ONE panorama (sky-to-space backdrop, blueprint grid, stars, the flight trajectory), then slices
it into store panels, so the row reads as one continuous picture on the product page. Devices hold real, unaltered in-game
captures from the Unity 'App Store screenshots' workflow; nothing inside the screen is drawn or edited.

  python3 tools/appstore-shots/render.py [--src DIR] [--out DIR] [--contact DIR]

Needs Python 3 with Pillow and numpy. Fonts: Fraunces Black + Space Grotesk from unity/Assets/Art/Fonts (in the repo).
Output PNGs are opaque RGB at the exact store sizes (Apple rejects alpha).
"""
import argparse, json, math, os, random, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FONTS = os.path.join(ROOT, "unity/Assets/Art/Fonts/Resources/KHFonts")
TITLE_FONT = os.path.join(FONTS, "Fraunces-Black.ttf")
SUB_FONT = os.path.join(FONTS, "SpaceGrotesk-Medium.ttf")
LABEL_FONT = os.path.join(FONTS, "SpaceGrotesk-Bold.ttf")

# Kestrel palette (docs: art direction): night ramp, cream text, amber/ember warmth only at the horizon and the flame.
NIGHT = [(0.00, (5, 8, 22)), (0.40, (12, 19, 52)), (0.70, (27, 30, 78)), (0.86, (58, 42, 94)), (0.95, (150, 70, 82)), (1.00, (255, 168, 92))]
SPACE = [(0.00, (3, 4, 12)), (0.50, (7, 11, 32)), (0.85, (16, 22, 60)), (1.00, (30, 34, 84))]
CREAM = (246, 235, 210)
MUTED = (170, 180, 208)
AMBER = (255, 162, 62)
TEAL = (70, 217, 204)
EMBER = (242, 84, 61)


def ramp(stops, t):
    t = np.clip(t, 0, 1)
    out = np.zeros(t.shape + (3,), np.float32)
    for (a, ca), (b, cb) in zip(stops, stops[1:]):
        m = (t >= a) & (t <= b)
        k = ((t - a) / max(b - a, 1e-6))[m][:, None]
        out[m] = np.array(ca) * (1 - k) + np.array(cb) * k
    return out


def smooth(x):
    x = np.clip(x, 0, 1)
    return x * x * (3 - 2 * x)


class Set:
    def __init__(self, cfg, size_name, spec, src_dir):
        self.cfg, self.name, self.kind = cfg, size_name, spec["kind"]
        self.W, self.H = spec["w"], spec["h"]
        self.src_dir = os.path.join(src_dir, size_name)
        self.panels = []
        for p in cfg["panels"]:
            if p.get("hero") == "right":
                self.panels.append(dict(p, capture=None)); continue
            cap = next((os.path.join(self.src_dir, s + ".png") for s in p["scene"] if os.path.exists(os.path.join(self.src_dir, s + ".png"))), None)
            if cap is None:
                if p.get("optional"):
                    print(f"  {size_name}: skip {p['id']} (no capture for {p['scene']})"); continue
                sys.exit(f"{size_name}: no capture for {p['id']} ({p['scene']}) in {self.src_dir}")
            self.panels.append(dict(p, capture=cap))
        self.n = len(self.panels)
        phone = self.kind == "phone"
        W, H = self.W, self.H
        # Layout, in pixels of one panel.
        self.margin = int(W * (0.075 if phone else 0.068))
        self.title_px = int(W * (0.098 if phone else 0.072))
        self.sub_px = int(W * (0.041 if phone else 0.029))
        self.title_top = int(H * (0.055 if phone else 0.05))
        self.screen_w = int(W * (0.70 if phone else 0.68))
        self.dev_top = int(H * (0.255 if phone else 0.275))

    # ---------- global geometry (panorama pixels) ----------
    def traj(self):
        """The flight: ascent from the pad (lower left) to staging, then the upper stage to orbit and the booster home."""
        W, H, n = self.W, self.H, self.n
        idx = {p["id"]: i for i, p in enumerate(self.panels)}
        sep_x = (idx.get("06_staging", n - 3) + 0.0) * W   # separation happens at the seam into the staging panel
        land_i = idx.get("07_landing", n - 2)
        ys = lambda f: f * H
        # Gravity turn: steep off the pad, flattening toward staging.
        asc = []
        for k in range(240):
            t = k / 239
            x = 0.12 * W + (sep_x - 0.12 * W) * t
            y = ys(1.02 - 0.62 * (1 - (1 - t) ** 2.3))
            asc.append((x, y))
        sx, sy = asc[-1]
        up = [(sx + (n * W + 0.2 * W - sx) * k / 99, sy - (0.10 * H) * (k / 99) ** 0.8) for k in range(100)]
        # Booster: boostback loop, then straight down the seam into the landing panel to Haven.
        bx = (land_i + 0.0) * W
        boost = []
        for k in range(160):
            t = k / 159
            x = sx + (bx - sx) * smooth(t) + 0.22 * W * math.sin(math.pi * t) * (1 - t)
            y = sy + (ys(1.03) - sy) * t ** 1.8 - 0.06 * H * math.sin(math.pi * min(1, t * 1.6))
            boost.append((x, y))
        events = [("LIFTOFF", 0.04), ("MAX-Q", 0.30), ("MECO", 0.78)]
        marks = [(lab, asc[int(f * 239)]) for lab, f in events]
        marks += [("STAGE SEP", (sx, sy)), ("ORBIT", up[80]), ("BOOSTBACK", boost[40]), ("TOUCHDOWN", boost[150])]
        return asc, up, boost, marks

    def device_boxes(self):
        """Screen rectangle per panel (global x), and the hero's centre/angle/size."""
        W, H = self.W, self.H
        boxes = {}
        for i, p in enumerate(self.panels):
            if p.get("hero"):
                continue
            with Image.open(p["capture"]) as im:
                cw, ch = im.size
            sw = self.screen_w; sh = int(sw * ch / cw)
            boxes[i] = (i * W + (W - sw) // 2, self.dev_top, sw, sh)
        return boxes

    # ---------- backdrop (low-res, smooth layers) ----------
    def backdrop(self, scale=0.25):
        W, H, n = self.W, self.H, self.n
        bw, bh = int(n * W * scale), int(H * scale)
        y = np.linspace(0, 1, bh)[:, None] * np.ones((1, bw))
        x = np.linspace(0, n, bw)[None, :] * np.ones((bh, 1))
        # How far into space each column is: dusk on the pad, black by staging, a sea-dusk glow again under the landing.
        idx = {p["id"]: i for i, p in enumerate(self.panels)}
        land = idx.get("07_landing", n - 2)
        space = smooth((x - 1.6) / 3.2) * (1 - 0.65 * np.exp(-((x - (land + 0.5)) / 0.9) ** 2))
        img = ramp(NIGHT, y) * (1 - space[..., None]) + ramp(SPACE, y) * space[..., None]
        # Horizon glow: warm, wide, strongest under the launch.
        warm = np.exp(-((1 - y) / 0.10) ** 2) * (0.75 * np.exp(-((x - 1.0) / 1.4) ** 2) + 0.35 * np.exp(-((x - (land + 0.4)) / 0.8) ** 2))
        img += warm[..., None] * np.array([120, 60, 20], np.float32)
        base = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")

        # Soft colour pools behind devices (large enough to bleed across seams).
        glow = Image.new("RGB", (bw, bh), (0, 0, 0))
        g = ImageDraw.Draw(glow)
        cols = [AMBER, TEAL, (110, 90, 255), TEAL, AMBER, (110, 90, 255), TEAL, AMBER]
        for i, p in enumerate(self.panels):
            if p.get("hero") == "right":
                continue
            cx = (i + (1.0 if p.get("hero") else 0.5)) * W * scale
            cy = H * 0.62 * scale
            r = W * (0.55 if not p.get("hero") else 0.8) * scale
            c = cols[i % len(cols)]
            g.ellipse([cx - r, cy - r * 0.8, cx + r, cy + r * 0.8], fill=tuple(int(v * 0.33) for v in c))
        glow = glow.filter(ImageFilter.GaussianBlur(W * 0.14 * scale))
        base = Image.fromarray(np.clip(np.asarray(base, np.int16) + np.asarray(glow, np.int16), 0, 255).astype(np.uint8))

        # The trail's wide exhaust glow.
        asc, up, boost, _ = self.traj()
        tg = Image.new("RGB", (bw, bh), (0, 0, 0))
        d = ImageDraw.Draw(tg)
        d.line([(px * scale, py * scale) for px, py in asc], fill=(150, 80, 20), width=max(2, int(W * 0.05 * scale)), joint="curve")
        d.line([(px * scale, py * scale) for px, py in up], fill=(90, 70, 40), width=max(2, int(W * 0.03 * scale)), joint="curve")
        d.line([(px * scale, py * scale) for px, py in boost], fill=(20, 90, 90), width=max(2, int(W * 0.03 * scale)), joint="curve")
        tg = tg.filter(ImageFilter.GaussianBlur(W * 0.03 * scale))
        base = Image.fromarray(np.clip(np.asarray(base, np.int16) + np.asarray(tg, np.int16), 0, 255).astype(np.uint8))
        return base, scale

    # ---------- device ----------
    def device(self, cap_path, screen_w):
        phone = self.kind == "phone"
        cap = Image.open(cap_path).convert("RGB")
        sw = screen_w; sh = int(round(sw * cap.size[1] / cap.size[0]))
        scr = cap.resize((sw, sh), Image.LANCZOS)
        r = int(sw * (0.115 if phone else 0.035))
        bz = int(sw * (0.032 if phone else 0.026))
        ow, oh = sw + 2 * bz, sh + 2 * bz
        ss = 2  # supersample the frame edges
        body = Image.new("RGBA", (ow * ss, oh * ss), (0, 0, 0, 0))
        d = ImageDraw.Draw(body)
        R = (r + bz) * ss
        d.rounded_rectangle([0, 0, ow * ss - 1, oh * ss - 1], R, fill=(120, 132, 160, 255))          # bright rim
        d.rounded_rectangle([3 * ss, 3 * ss, ow * ss - 1 - 3 * ss, oh * ss - 1 - 3 * ss], R - 3 * ss, fill=(34, 39, 54, 255))
        d.rounded_rectangle([7 * ss, 7 * ss, ow * ss - 1 - 7 * ss, oh * ss - 1 - 7 * ss], R - 7 * ss, fill=(9, 11, 18, 255))
        body = body.resize((ow, oh), Image.LANCZOS)
        mask = Image.new("L", (sw * ss, sh * ss), 0)
        ImageDraw.Draw(mask).rounded_rectangle([0, 0, sw * ss - 1, sh * ss - 1], r * ss, fill=255)
        mask = mask.resize((sw, sh), Image.LANCZOS)
        body.paste(scr, (bz, bz), mask)
        # Rim highlight on the top-left edge for a metal read.
        hl = Image.new("L", (ow, oh), 0)
        ImageDraw.Draw(hl).rounded_rectangle([0, 0, ow - 1, oh - 1], r + bz, outline=255, width=2)
        grad = np.linspace(1, 0.15, ow)[None, :] * np.linspace(1, 0.3, oh)[:, None]
        hl = Image.fromarray((np.asarray(hl, np.float32) * grad * 0.8).astype(np.uint8))
        white = Image.new("RGBA", (ow, oh), (235, 240, 255, 255))
        body = Image.alpha_composite(body, Image.merge("RGBA", (*white.split()[:3], hl)))
        if phone:  # side buttons
            pad = Image.new("RGBA", (ow + 8, oh), (0, 0, 0, 0))
            pd = ImageDraw.Draw(pad)
            for y0, y1 in ((0.17, 0.21), (0.25, 0.32), (0.34, 0.41)):
                pd.rounded_rectangle([0, int(oh * y0), 8, int(oh * y1)], 3, fill=(70, 78, 98, 255))
            pd.rounded_rectangle([ow, int(oh * 0.27), ow + 7, int(oh * 0.38)], 3, fill=(70, 78, 98, 255))
            pad.alpha_composite(body, (4, 0))
            body = pad
        return body

    @staticmethod
    def shadow(dev, blur, alpha=170):
        a = dev.split()[3].point(lambda v: alpha if v > 0 else 0)
        pad = int(blur * 3)
        sh = Image.new("L", (dev.width + 2 * pad, dev.height + 2 * pad), 0)
        sh.paste(a, (pad, pad))
        return sh.filter(ImageFilter.GaussianBlur(blur)), pad

    @staticmethod
    def perspective(img, quad):
        """Warp img onto quad (TL, TR, BR, BL) in an output of the quad's bounding box. Returns (image, (x0, y0))."""
        xs = [p[0] for p in quad]; ys = [p[1] for p in quad]
        x0, y0 = int(math.floor(min(xs))), int(math.floor(min(ys)))
        ow, oh = int(math.ceil(max(xs))) - x0, int(math.ceil(max(ys))) - y0
        dst = [(px - x0, py - y0) for px, py in quad]
        src = [(0, 0), (img.width, 0), (img.width, img.height), (0, img.height)]
        A, B = [], []
        for (X, Y), (u, v) in zip(dst, src):
            A.append([X, Y, 1, 0, 0, 0, -u * X, -u * Y]); B.append(u)
            A.append([0, 0, 0, X, Y, 1, -v * X, -v * Y]); B.append(v)
        coef = np.linalg.solve(np.array(A, float), np.array(B, float))
        out = img.transform((ow, oh), Image.PERSPECTIVE, tuple(coef), Image.BICUBIC)
        return out, (x0, y0)

    def hero(self):
        """The launch capture on a tilted device that straddles the seam between panels 1 and 2."""
        i = next(k for k, p in enumerate(self.panels) if p.get("hero") == "left")
        phone = self.kind == "phone"
        W, H = self.W, self.H
        sw = int(W * (0.80 if phone else 0.78))
        dev = self.device(self.panels[i]["capture"], sw)
        # Big, then laid back in 3D: the top edge narrower and further away, the whole thing turned.
        target_h = H * (0.78 if phone else 0.76)
        s = target_h / dev.height
        w, h = dev.width * s, dev.height * s
        cx, cy = (i + 1.0) * W + W * 0.06, H * (0.615 if phone else 0.625)
        ang = math.radians(-17 if phone else -13)
        k = 0.86  # top width vs bottom width
        pts = [(-w * k / 2, -h / 2), (w * k / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2)]
        quad = [(cx + x * math.cos(ang) - y * math.sin(ang), cy + x * math.sin(ang) + y * math.cos(ang)) for x, y in pts]
        warped, (x0, y0) = self.perspective(dev, quad)
        return warped, (x0, y0)

    # ---------- per-panel drawing ----------
    def render(self, out_dir):
        os.makedirs(out_dir, exist_ok=True)
        W, H, n = self.W, self.H, self.n
        bg, sc = self.backdrop()
        asc, up, boost, marks = self.traj()
        boxes = self.device_boxes()
        hero_img, hero_xy = self.hero()
        rnd = random.Random(7)
        stars = []
        for _ in range(int(n * 260 * (W * H) / (1206 * 2622))):
            x = rnd.uniform(0, n * W); yf = rnd.random() ** 1.8
            if yf > 0.8: continue
            stars.append((x, yf * H, rnd.choice([1, 1, 1, 2, 2, 3]) * W / 1206, rnd.uniform(0.25, 1.0) * (1 - yf / 0.8)))
        f_title = lambda px: ImageFont.truetype(TITLE_FONT, px)
        f_sub = ImageFont.truetype(SUB_FONT, self.sub_px)
        f_lab = ImageFont.truetype(LABEL_FONT, int(self.sub_px * 0.7))
        files = []
        for i, p in enumerate(self.panels):
            ox = i * W
            crop = bg.crop((int(ox * sc), 0, int((ox + W) * sc), bg.height)).resize((W, H), Image.BICUBIC)
            img = crop.convert("RGBA")
            over = Image.new("RGBA", (W, H), (0, 0, 0, 0))
            d = ImageDraw.Draw(over)
            # Blueprint grid, aligned to the panorama so it runs straight through every seam; fades out toward the ground.
            gs = W / 9
            k0 = int(ox // gs)
            for k in range(k0, k0 + 12):
                gx = k * gs - ox
                d.line([(gx, 0), (gx, H)], fill=(TEAL[0], TEAL[1], TEAL[2], 13), width=max(1, W // 900))
            for k in range(int(H / gs) + 1):
                gy = k * gs
                d.line([(0, gy), (W, gy)], fill=(TEAL[0], TEAL[1], TEAL[2], int(13 * (1 - gy / H) + 3)), width=max(1, W // 900))
            for sx, sy, sr, sa in stars:
                if ox - 5 <= sx <= ox + W + 5:
                    d.ellipse([sx - ox - sr, sy - sr, sx - ox + sr, sy + sr], fill=(255, 250, 235, int(255 * sa)))
            # Trajectory core lines.
            lw = max(3, int(W * 0.006))
            pt = lambda q: (q[0] - ox, q[1])
            d.line([pt(q) for q in asc], fill=(255, 214, 150, 235), width=lw, joint="curve")
            d.line([pt(q) for q in up], fill=(255, 214, 150, 140), width=max(2, lw - 1), joint="curve")
            # Booster return: dashed teal.
            seg = 7
            for a in range(0, len(boost) - seg, seg * 2):
                d.line([pt(q) for q in boost[a:a + seg + 1]], fill=TEAL + (220,), width=max(2, lw - 1), joint="curve")
            for lab, (mx, my) in marks:
                if not (ox - W * 0.3 <= mx <= ox + W * 1.3): continue
                rr = lw * 2.6
                c = TEAL if lab in ("BOOSTBACK", "TOUCHDOWN") else AMBER
                d.ellipse([mx - ox - rr, my - rr, mx - ox + rr, my + rr], fill=(8, 12, 30, 255), outline=c + (255,), width=max(2, lw // 1))
                tw = d.textlength(lab, font=f_lab)
                tx = mx - ox + rr * 1.8
                if lab in ("ORBIT", "STAGE SEP"): tx = mx - ox - rr * 1.8 - tw
                d.text((tx, my - rr * 3.6), lab, font=f_lab, fill=CREAM + (245,), stroke_width=max(2, lw // 2), stroke_fill=(6, 9, 24, 230))
            img = Image.alpha_composite(img, over)

            # Devices: hero first (it spans two panels), then this panel's own device, each with a soft drop shadow.
            shadows = []
            if p.get("hero"):
                shadows.append((hero_img, (hero_xy[0] - ox, hero_xy[1])))
            if i in boxes:
                gx, gy, sw, sh = boxes[i]
                dev = self.device(p["capture"], sw)
                bz = (dev.width - sw) // 2 if self.kind != "phone" else (dev.width - 8 - sw) // 2
                shadows.append((dev, (gx - ox - bz - (4 if self.kind == "phone" else 0), gy - bz)))
            for dev, (dx, dy) in shadows:
                sh, pad = self.shadow(dev, W * 0.025)
                black = Image.new("RGBA", sh.size, (0, 0, 0, 255)); black.putalpha(sh)
                layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
                layer.paste(black, (int(dx - pad), int(dy - pad + W * 0.02)), black)
                img = Image.alpha_composite(img, layer)
                layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
                layer.paste(dev, (int(dx), int(dy)), dev)
                img = Image.alpha_composite(img, layer)

            # Copy: Fraunces Black headline (cream), Space Grotesk subtitle, a short amber rule above.
            d = ImageDraw.Draw(img)
            maxw = W - 2 * self.margin
            px = self.title_px
            while px > 40 and max(d.textlength(t, font=f_title(px)) for t in p["title"]) > maxw: px -= 4
            ft = f_title(px)
            y = self.title_top
            rule_w, rule_h = int(W * 0.07), max(4, int(W * 0.0065))
            d.rounded_rectangle([self.margin, y, self.margin + rule_w, y + rule_h], rule_h // 2, fill=AMBER)
            y += rule_h + int(px * 0.42)
            for t in p["title"]:
                d.text((self.margin + 3, y + 4), t, font=ft, fill=(60, 30, 10))   # hard under-shadow, the Kestrel title look
                d.text((self.margin, y), t, font=ft, fill=CREAM)
                y += int(px * 1.04)
            y += int(px * 0.30)
            sub = p["sub"]
            if d.textlength(sub, font=f_sub) > maxw: sys.exit(f"{self.name} {p['id']}: subtitle too long")
            d.text((self.margin, y), sub, font=f_sub, fill=MUTED)
            path = os.path.join(out_dir, p["id"] + ".png")
            out = img.convert("RGB")
            assert out.size == (W, H)
            out.save(path, optimize=True)
            files.append(path)
            print("  wrote", os.path.relpath(path, ROOT))
        return files


def contact(files, path, gap=0, height=900):
    ims = [Image.open(f) for f in files]
    w = int(ims[0].width * height / ims[0].height)
    sheet = Image.new("RGB", (len(ims) * w + (len(ims) - 1) * gap, height), (0, 0, 0))
    for k, im in enumerate(ims):
        sheet.paste(im.resize((w, height), Image.LANCZOS), (k * (w + gap), 0))
    sheet.save(path, optimize=True)
    print("  contact", path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--config", default=os.path.join(os.path.dirname(__file__), "config.json"))
    ap.add_argument("--src"); ap.add_argument("--out"); ap.add_argument("--contact")
    ap.add_argument("--only")
    a = ap.parse_args()
    cfg = json.load(open(a.config))
    src = a.src or os.path.join(ROOT, cfg["sources"])
    out = a.out or os.path.join(ROOT, cfg["out"])
    for name, spec in cfg["sizes"].items():
        if a.only and a.only != name: continue
        print(name)
        files = Set(cfg, name, spec, src).render(os.path.join(out, name))
        cdir = a.contact or out
        os.makedirs(cdir, exist_ok=True)
        contact(files, os.path.join(cdir, "contact-" + ("iphone" if spec["kind"] == "phone" else "ipad") + ".png"))


if __name__ == "__main__":
    main()

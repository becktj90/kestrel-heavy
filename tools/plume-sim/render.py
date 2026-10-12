# Reference renderer for the #119 plume: Monte-Carlo particles from the PlumeController/PlumeShape emitter
# parameters, projected with the slice chase camera (vFOV 50, DesiredOffset/Aim at liftoff), depth-tested
# against the opaque ground (y=-1), deck disc (y=0) and vehicle, splatted with the SoftParticle falloff.
# Approximates what Unity should show; not a substitute for a real capture.
import math, sys, numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, '/workspace/plume-sim'); import plumeshape as P
W, H = 1290, 2796; VFOV = 50.0
VH, VR, CD, DECK, GROUND = 70.0, 7.35, float(__import__('os').environ.get('KH_CLUSTER', 14.0)), 20.0, -1.0
rng = np.random.default_rng(7)
def alt_at(t): return 6.15 * t * t if t < 4 else 6.15*16 + 49.2*(t-4) + 8*(t-4)**2   # from HUD: v=5 m/s @0.4, 16 @1.3

def camera(alt):
    piv = np.array([0, alt, 0.]); off = np.array([0.8*VH, -0.05*VH, -1.6*VH])
    cam = piv + off; cam[1] = max(cam[1], GROUND + 3.0)
    d = piv + np.array([0, 0.5*VH, 0]) - cam; horiz = math.hypot(d[0], d[2]); pitch = math.atan2(d[1], horiz)
    t2 = 2*math.tan(math.radians(VFOV/2)); pitch = min(max(pitch, math.atan(0.15*t2)), math.atan(0.35*t2))
    yaw = math.atan2(d[0], d[2])
    fwd = np.array([math.sin(yaw)*math.cos(pitch), math.sin(pitch), math.cos(yaw)*math.cos(pitch)])
    right = np.cross([0, 1, 0], fwd); right /= np.linalg.norm(right); up = np.cross(fwd, right)
    f = (H/2) / math.tan(math.radians(VFOV/2))
    return cam, fwd, right, up, f

def rays(C):
    cam, fwd, right, up, f = C
    xs = (np.arange(W) - W/2 + 0.5); ys = (H/2 - np.arange(H) - 0.5)
    X, Y = np.meshgrid(xs, ys)
    return (fwd[None, None]*f + right[None, None]*X[..., None] + up[None, None]*Y[..., None]) / f  # depth-normalised dirs

def opaque(C, alt):
    cam = C[0]; R = rays(C)
    img = np.zeros((H, W, 3)); depth = np.full((H, W), np.inf)
    # sky gradient
    sky = np.array([110, 155, 220.]); img[:] = sky
    # ground plane y = GROUND
    ty = (GROUND - cam[1]) / np.where(np.abs(R[..., 1]) < 1e-9, -1e-9, R[..., 1])
    m = ty > 0; hz = np.clip(ty / 4000, 0, 1)[..., None]
    img = np.where(m[..., None], np.array([150, 175, 160.]) * (1-hz) + sky*hz, img); depth = np.where(m, ty, depth)
    # deck top disc y = 0
    td = (0 - cam[1]) / np.where(np.abs(R[..., 1]) < 1e-9, -1e-9, R[..., 1]); px = cam[0] + td*R[..., 0]; pz = cam[2] + td*R[..., 2]
    m = (td > 0) & (px**2 + pz**2 < DECK**2) & (td < depth); img[m] = [205, 208, 212]; depth[m] = td[m]
    # deck plinth side (cylinder r=DECK, y in [GROUND,0]) and vehicle cylinders: core r=2.6, boosters at +-4.9 r=2.4
    def cyl(cx, cz, r, y0, y1, col, shade=True):
        nonlocal img, depth
        ox, oz = cam[0]-cx, cam[2]-cz; a = R[..., 0]**2 + R[..., 2]**2; b = 2*(ox*R[..., 0] + oz*R[..., 2]); c = ox*ox + oz*oz - r*r
        disc = b*b - 4*a*c; ok = disc > 0; s = np.sqrt(np.where(ok, disc, 0)); t = (-b - s) / (2*a + 1e-12)
        y = cam[1] + t*R[..., 1]; mm = ok & (t > 0) & (y > y0) & (y < y1) & (t < depth)
        if shade:
            nx = (ox + t*R[..., 0]) / r; lam = np.clip(0.55 + 0.45*nx, 0.3, 1)[..., None]
        else: lam = 1
        img = np.where(mm[..., None], np.array(col)*lam, img); depth = np.where(mm, t, depth)
    cyl(0, 0, DECK, GROUND, 0, [185, 188, 192], False)
    for bx in (-4.9, 0, 4.9):
        cyl(bx, 0, 2.6 if bx == 0 else 2.4, alt, alt + (VH if bx == 0 else 0.55*VH), [235, 238, 242])
    return img, depth

def gradient(u, keys, alphas):
    cols = np.array([k[0] for k in keys]); ts = np.array([k[1] for k in keys])
    c = np.stack([np.interp(u, ts, cols[:, i]) for i in range(3)], -1)
    a = np.interp(u, [0, 0.4, 1], alphas); return c, a

def hexc(h): return [((h >> 16) & 255), ((h >> 8) & 255), (h & 255)]
CORE, MID, TAIL, HEAD = hexc(0xfff4d8), hexc(0xffb040), hexc(0xff6a1a), hexc(0xffd27a)

def particles(t, v):
    """Return list of (pos[N,3], size[N], rgb[N,3], alpha[N], additive) for every emitter at real time t."""
    alt = alt_at(t); out = []
    tf, pa = 0.9, math.exp(-alt/8500)
    if v in ('v5', 'v6', 'v7', 'v8', 'v9', 'v10', 'v11'):
        L = P.layout(tf, pa, CD, VR, VH, DECK); ssf = P.SSF; collide_body = True; cloudH = L['CloudHeight']; ramp = 0.25; burst = 0.35
        core_spawn = L["CoreSpawnRadius"]; ncore = 12; csz = np.vectorize(P.csize)
    else:  # v4 (fee69ee) parameters
        P.SSF = 0.6; P.NOZ = 0.0; P.BodySizeStart = 0.2; P.CoreSizeStart = 0.3
        L = P.layout(tf, pa, CD, VR, VH, DECK); L['CoreSize'] = 0.9*CD/0.6
        bo = co = 0
        for i in range(41):
            u = i/40; bo = max(bo, 0.4*L['BodyMax']*P.bsize(L, u) - L['Length']*u)
            co = max(co, 0.4*L['CoreSize']*(0.3+0.7*u/0.15 if u < 0.15 else 1-0.2*(u-0.15)/0.85) - L['CoreLength']*u)
        L['BodyEmitOffset'], L['CoreEmitOffset'] = bo, co
        collide_body = False; cloudH = -0.1*DECK; ramp = 0.75; burst = 0.0; core_spawn = 0.05*CD; ncore = 8
        csz = lambda u: np.where(u < 0.15, 0.3+0.7*u/0.15, 1-0.2*(u-0.15)/0.85)
        P.SSF, P.NOZ, P.BodySizeStart, P.CoreSizeStart = 0.36, 0.15, 0.3, 0.35
    if v == 'v11':
        return particles_v11(t, L, alt) + cloud_part(t, v, L, cloudH, ramp, burst)
    if v == 'v10':
        return particles_v10(t, L, alt) + cloud_part(t, v, L, cloudH, ramp, burst)
    if v == 'v9':
        return particles_v9(t, L, alt) + cloud_part(t, v, L, cloudH, ramp, burst)
    if v in ('v7', 'v8'):
        return particles_v7(t, L, alt) + cloud_part(t, v, L, cloudH, ramp, burst)
    if v == 'v6':
        return particles_v6(t, L, alt, cloudH, ramp, burst) + cloud_part(t, v, L, cloudH, ramp, burst)
    # ---- body (Local space: moves with the vehicle) ----
    n = 432; life = L['BodyLife']; u = rng.random(n); age = u*life
    ang = math.radians(L['BodyConeDeg']); phi = rng.random(n)*2*np.pi; rr = np.sqrt(rng.random(n))*L['BodySpawnRadius']
    theta = rng.random(n)*ang; spd = L['Length']/life
    pos = np.stack([rr*np.cos(phi) + np.sin(theta)*np.cos(phi)*spd*age, alt - L['BodyEmitOffset'] - np.cos(theta)*spd*age, rr*np.sin(phi) + np.sin(theta)*np.sin(phi)*spd*age], -1)
    size = rng.uniform(L['BodyMin'], L['BodyMax'], n) * np.array([P.bsize(L, x) for x in u])
    rgb, a = gradient(u, [(HEAD, 0), (MID, 0.3), (TAIL, 1)], [0.9, 0.6, 0] if v == 'v5' else [0.8, 0.55, 0])
    if collide_body:   # v5: lifetimeLoss 1 -> particles that reached the deck are gone
        k = pos[:, 1] > 0.25*size*0.36; pos, size, rgb, a = pos[k], size[k], rgb[k], a[k]
    out.append((pos, size, rgb, a, False))
    # ---- core (additive) ----
    n = ncore; u = rng.random(n); clen = L['CoreLength']; phi = rng.random(n)*2*np.pi; rr = np.sqrt(rng.random(n))*core_spawn
    pos = np.stack([rr*np.cos(phi), alt - L['CoreEmitOffset'] - clen*u, rr*np.sin(phi)], -1); size = L['CoreSize']*csz(u)
    rgb, a = gradient(u, [([255, 255, 255], 0), (CORE, 0.35), (MID, 1)], [0.45, 0.3, 0]); pos = deflect(pos, size)
    out.append((pos, size, rgb*1.5 if v == 'v5' else rgb*1.5, a, True))
    out += cloud_part(t, v, L, cloudH, ramp, burst)
    return out

# Ground-cloud parameters per build. 'u129' = what #129 built, calibrated to the 129-v6 Unity probe
# (cap 160 on Medium; outward spread 0.55 s -> ~19 m, 1.36 s -> ~24 m, 4 s -> ~38 m => effective damping k ~ 1/s,
# not the 3.8/s the v5/v6 sim assumed; billows born at 0.3 size: avg 14 m at T+0.55 vs probe 14.3).
CLOUD = {
    'v7': dict(cap=160, spd=(0.08, 0.25), k=1.0, start=0.45, vy=(-0.3, 0.6), lift=0.5, R0f=2, spawnf=1.5, sizef=1.0, life=(3.0, 4.5)),
    'u129': dict(cap=160, spd=(0.35, 0.8), k=1.0, start=0.3, vy=(-0.5, 1.0), lift=0.5, R0f=2, spawnf=1.5, sizef=1.0, life=(3.0, 4.5)),
}
def cloud_part(t, v, L, cloudH, ramp, burst):
    C = CLOUD['v7' if v in ('v7', 'v8', 'v9', 'v10', 'v11') else 'u129']
    out = []
    R0 = C['R0f']*max(DECK, 10); smax = C['sizef']*max(0.55*DECK, 0.2*VH)/0.36; smin = 0.55*smax
    spawnR = max(C['spawnf']*DECK, max(1.5*VR, 1.5*CD) + 0.4*smax*C['start'])
    cap = C['cap']; ps = []; dt = 1/30.; acc = cap*burst; T = 0.0
    while T < t:
        h = alt_at(T); since = T
        frac = min(1, max(0, since/ramp)); frac = frac*frac*(3-2*frac)
        byh = 1 - P.ss(80, 200, h); byt = 1 - P.ss(5.5, 7.5, since); e = frac*max(byh, byt)
        acc += cap/sum(C['life'])*2*e*dt
        k = int(acc); acc -= k
        for _ in range(k):
            ps.append([T, rng.uniform(*C['life']), rng.random()*2*np.pi, rng.uniform(*C['spd'])*R0, rng.uniform(*C['vy']), rng.uniform(smin, smax)])
        T += dt
    if ps:
        A = np.array(ps); age = t - A[:, 0]; keep = age < A[:, 1]; A, age = A[keep][-cap:], age[keep][-cap:]
        lim = 0.08*R0; v0 = A[:, 3]; k = C['k']; vexc = np.maximum(v0 - lim, 0)
        dist = np.minimum(v0, lim)*age + vexc*(1-np.exp(-k*age))/k
        r = spawnR + dist; uu = age/A[:, 1]
        pos = np.stack([r*np.cos(A[:, 2]), C['lift'] + A[:, 4]*age, r*np.sin(A[:, 2])], -1)
        size = A[:, 5]*(C['start'] + (1.6 - C['start'])*uu)
        a = np.interp(uu, [0, 0.05, 0.7, 1], [0.85, 1, 0.95, 0])
        out.append((pos, size, np.tile(np.array([232, 232, 233.]), (len(uu), 1)), a, False))
        print(f'  cloud T+{t}: live {len(uu)} avg size {size.mean():.1f} max {size.max():.1f} m, outer radius {(r+0.18*size).max():.0f} m', file=sys.stderr)
    return out

def particles_v7(t, L, alt):
    """#129-v7: alpha exactly 0 until each sprite's top is below the nozzle plane (FadeKeys), body tail alpha
    0.04 from u=0.7 so deck-deflected particles (u >= 0.75 after the loss) are invisible, no bounce;
    core drifts 1 D in 0.3 s; diamonds at the exit plane with a fade-in."""
    out = []; d = L['VisualD']
    def fade(u, fi, peak, tail, ramp=0.04):
        up = min(0.95, fi + max(0.002, ramp)); xs = [0, max(fi, 1e-3), up] + [tt for a_, tt in tail if tt > up] + [1]
        ys = [0, 0, peak] + [a_ for a_, tt in tail if tt > up] + [0]; return np.interp(u, xs, ys)
    n = 208; life = L['BodyLife']; u = rng.random(n); spd = L['Length']/life
    fi = min(0.35, L['BodyEmitOffset']/max(1, L['Length']))
    ang = math.radians(L['BodyConeDeg']); phi = rng.random(n)*2*np.pi; rr = np.sqrt(rng.random(n))*L['BodySpawnRadius']; th = rng.random(n)*ang
    travel = spd*u*life; dy = np.cos(th)*travel; hit = (alt - dy) < 0
    ue = np.where(hit, u + 0.75, u); alive = ue < 1
    rad = rr + np.sin(th)*travel + np.where(hit, 0.2*(travel - alt), 0)
    y = np.where(hit, 0.1*0.5*L['BodyMax'], alt - dy)
    pos = np.stack([rad*np.cos(phi), y, rad*np.sin(phi)], -1)
    size = rng.uniform(L['BodyMin'], L['BodyMax'], n)*np.array([P.bsize(L, min(x, 1)) for x in ue])
    rgb, _ = gradient(np.minimum(ue, 1), [(HEAD, 0), (MID, 0.3), (TAIL, 1)], [0, 0, 0])
    a = fade(np.minimum(ue, 1), fi, 0.9, [(0.6, 0.4), (0.04, 0.7)], ramp=min(0.04, max(0.002, 0.02*d/L['Length'])))
    out.append((pos[alive], size[alive], rgb[alive], a[alive], False))
    n = 8; u = rng.random(n); phi = rng.random(n)*2*np.pi; rr = np.sqrt(rng.random(n))*L['CoreSpawnRadius']
    pos = np.stack([rr*np.cos(phi), alt - L['CoreLength']*u, rr*np.sin(phi)], -1); size = L['CoreSize']*(1 - 0.2*u)
    cfi = min(0.9, L['CoreEmitOffset']/max(0.1, L['CoreLength']))
    rgb, _ = gradient(u, [([255, 255, 255], 0), (CORE, 0.35), (MID, 1)], [0, 0, 0])
    out.append((pos, size, np.minimum(rgb*1.5, 255), fade(u, cfi, 0.8, [(0.3, 0.75)]), False))
    n = 18; u = rng.random(n); pos = np.stack([np.zeros(n), alt - 0.5*L['Length']*u, np.zeros(n)], -1); k = pos[:, 1] > 0
    out.append((pos[k], np.full(k.sum(), 0.5*d), np.tile([255, 230, 170.], (k.sum(), 1)), fade(u[k], 0.08, 0.35, [(0.25, 0.4)]), False))
    return out

def particles_v9(t, L, alt):
    """#130 review fix: deck-deflected body keeps life/alpha but shrinks by size-by-speed (dampen 0.85);
    core/diamonds die on deck contact; diamonds drift 1.5 D over 0.3 s with fade-in 0.13 (locked to the exit)."""
    out = []; d = L['VisualD']
    def fade(u, fi, peak, tail, ramp=0.04):
        up = min(0.95, fi + max(0.002, ramp)); xs = [0, max(fi, 1e-3), up] + [tt for a_, tt in tail if tt > up] + [1]
        ys = [0, 0, peak] + [a_ for a_, tt in tail if tt > up] + [0]; return np.interp(u, xs, ys)
    n = 208; life = L['BodyLife']; u = rng.random(n); spd = L['Length']/life
    fi = min(0.35, L['BodyEmitOffset']/max(1, L['Length']))
    ang = math.radians(L['BodyConeDeg']); phi = rng.random(n)*2*np.pi; rr = np.sqrt(rng.random(n))*L['BodySpawnRadius']; th = rng.random(n)*ang
    travel = spd*u*life; dy = np.cos(th)*travel; hit = (alt - dy) < 0
    rad = rr + np.sin(th)*np.where(hit, alt, travel) + np.where(hit, 0.15*(travel - alt), 0)
    shrink = np.where(hit, 0.19, 1.0)
    size = rng.uniform(L['BodyMin'], L['BodyMax'], n)*np.array([P.bsize(L, x) for x in u])*shrink
    y = np.where(hit, 0.1*0.5*size, alt - dy)
    pos = np.stack([rad*np.cos(phi), y, rad*np.sin(phi)], -1)
    rgb, _ = gradient(u, [(HEAD, 0), (MID, 0.3), (TAIL, 1)], [0, 0, 0])
    a = fade(u, fi, 0.9, [(0.6, 0.4), (0.04, 0.7)], ramp=min(0.04, max(0.002, 0.02*d/L['Length'])))
    out.append((pos, size, rgb, a, False))
    n = 8; u = rng.random(n); phi = rng.random(n)*2*np.pi; rr = np.sqrt(rng.random(n))*L['CoreSpawnRadius']
    pos = np.stack([rr*np.cos(phi), alt - L['CoreLength']*u, rr*np.sin(phi)], -1); size = L['CoreSize']*(1 - 0.2*u); k = pos[:, 1] > 0
    cfi = min(0.9, L['CoreEmitOffset']/max(0.1, L['CoreLength']))
    rgb, _ = gradient(u, [([255, 255, 255], 0), (CORE, 0.35), (MID, 1)], [0, 0, 0])
    out.append((pos[k], size[k], np.minimum(rgb*1.5, 255)[k], fade(u, cfi, 0.8, [(0.3, 0.75)])[k], False))
    n = 18; u = rng.random(n); pos = np.stack([np.zeros(n), alt - 1.5*d*u, np.zeros(n)], -1); k = pos[:, 1] > 0
    out.append((pos[k], np.full(k.sum(), 0.5*d), np.tile([255, 230, 170.], (k.sum(), 1)), fade(u[k], 0.13, 0.35, [(0.25, 0.4)]), False))
    return out

PFF = 0.74; PSF = 0.71
def yfull(y0, smax, travel, cone=0.0): return min(0.6, max(0.02, (1-y0)*0.5*PFF*smax/max(0.1, 0.8*travel*math.cos(cone))))
def first_clear(travel, cone, smax, size, ysize):
    c = math.cos(cone + math.radians(1)); last = 0.0
    for i in range(401):
        u = i/400
        if 0.5*PFF*smax*size(u)*ysize(u) > travel*u*c: last = min(1, (i+1)/400)
    return min(0.9, last)

def particles_v11(t, L, alt):
    return particles_v10(t, L, alt, v11=True)

def particles_v10(t, L, alt, v11=False):
    """#130-v9 review fix: flat-born sprites (full width, YStart height growing as fast as travel allows) so the
    flame starts at the exit; exact fade-in; body visible to ~u 0.97; every emitter deflects on the deck with no
    loss and shrinks to 5% / greys to alpha 0.3 when slow."""
    out = []; d = L['VisualD']; grey = np.array([217, 212, 204.])
    def fade(u, fi, peak, tail, ramp=0.04):
        up = min(0.95, fi + max(0.002, ramp)); xs = [0, max(fi, 1e-3), up] + [tt for a_, tt in tail if tt > up] + [1]
        ys = [0, 0, peak] + [a_ for a_, tt in tail if tt > up] + [0]; return np.interp(u, xs, ys)
    def ysz(u, y0, fa, ye=1.0): return y0 + (ye-y0)*np.clip(u/fa, 0, 1)
    # body
    n = 208; life = L['BodyLife']; u = rng.random(n); spd = L['Length']/life; cone = math.radians(L['BodyConeDeg'])
    byf = yfull(0.06, L['BodyMax']*1.15, L['Length'], cone); yend = 1.0; dal = 0.3
    if v11: byf = max(min(0.6, 1.5*d/L['Length']), byf); yend = 0.9; dal = 0.04
    fi = first_clear(L['Length'], cone, L['BodyMax'], lambda x: P.bsize(L, x), lambda x: ysz(x, 0.06, byf, yend))
    phi = rng.random(n)*2*np.pi; rr = np.sqrt(rng.random(n))*L['BodySpawnRadius']; th = rng.random(n)*cone
    travel = spd*u*life; dy = np.cos(th)*travel; hit = (alt - dy) < 0
    rad = rr + np.sin(th)*np.where(hit, alt, travel) + np.where(hit, 0.15*(travel - alt)*0.3, 0)
    size = rng.uniform(L['BodyMin'], L['BodyMax'], n)*np.array([P.bsize(L, x) for x in u])*np.where(hit, 0.05, 1.0)
    ysc = np.where(hit, 1.0, ysz(u, 0.06, byf, yend))
    y = np.where(hit, 0.05*size, alt - dy)
    rgb, _ = gradient(u, [(HEAD, 0), (MID, 0.3), (TAIL, 1)], [0, 0, 0]); rgb = np.where(hit[:, None], grey, rgb)
    a = fade(u, fi, 0.9, [(0.75, 0.6), (0.35, 0.9)], ramp=min(0.04, max(0.002, 0.02*d/L['Length'])))*np.where(hit, dal, 1)
    out.append((np.stack([rad*np.cos(phi), y, rad*np.sin(phi)], -1), size, rgb, a, False, ysc))
    # core
    n = 8; u = rng.random(n); phi = rng.random(n)*2*np.pi; rr = np.sqrt(rng.random(n))*L['CoreSpawnRadius']
    cyf = yfull(0.1, L['CoreSize'], L['CoreLength']); csz = lambda x: P.csize(x) if hasattr(P, 'csize') else 1.0
    cfi = first_clear(L['CoreLength'], 0, L['CoreSize'], csz, lambda x: ysz(x, 0.1, cyf))
    yy = alt - L['CoreLength']*u; hit = yy < 0
    size = L['CoreSize']*(1 - 0.2*u)*np.where(hit, 0.05, 1); ysc = np.where(hit, 1, ysz(u, 0.1, cyf))
    rgb, _ = gradient(u, [([255, 255, 255], 0), (CORE, 0.35), (MID, 1)], [0, 0, 0]); rgb = np.where(hit[:, None], grey, np.minimum(rgb*1.5, 255))
    out.append((np.stack([rr*np.cos(phi), np.maximum(yy, 0.05*size), rr*np.sin(phi)], -1), size, rgb, fade(u, cfi, 0.8, [(0.3, 0.75)])*np.where(hit, dal, 1), False, ysc))
    # diamonds
    n = 18; u = rng.random(n); yy = alt - 1.5*d*u; hit = yy < 0
    dfi = first_clear(1.5*d, 0, 0.5*d, lambda x: 1.0, lambda x: ysz(x, 0.1, 0.5))
    size = np.full(n, 0.5*d)*np.where(hit, 0.05, 1); ysc = np.where(hit, 1, ysz(u, 0.1, 0.5))
    rgb = np.where(hit[:, None], grey, np.array([255, 230, 170.]))
    out.append((np.stack([np.zeros(n), np.maximum(yy, 0.05*size), np.zeros(n)], -1), size, rgb, fade(u, dfi, 0.35, [(0.25, 0.4)])*np.where(hit, dal, 1), False, ysc))
    return out

def particles_v6(t, L, alt, cloudH, ramp, burst):
    """#129 emitters as Unity builds them (probe-checked): body emitter AT the exit plane with an alpha fade-in
    over BodyFadeIn, deck collision bounce 0.05 / dampen 0.6 / lifetimeLoss 0.5; core = up to 8 small
    alpha-blended sprites over a 0.4 D disc drifting 0.5 D in 0.3 s; diamonds faint."""
    out = []; d = L['VisualD']
    n = 208; life = L['BodyLife']; u = rng.random(n); spd = L['Length']/life
    fi = min(0.35, L['BodyEmitOffset']/max(1, L['Length']))
    ang = math.radians(L['BodyConeDeg']); phi = rng.random(n)*2*np.pi; rr = np.sqrt(rng.random(n))*L['BodySpawnRadius']; th = rng.random(n)*ang
    travel = spd*u*life; dy = np.cos(th)*travel; hit = (alt - dy) < 0
    # deflection: travel past the deck becomes radial at 0.4x (dampen 0.6); age jumps by lifetimeLoss 0.5
    u_hit = np.where(hit, alt/np.maximum(spd*life, 1e-6), u); ue = np.where(hit, u + 0.75, u)
    alive = ue < 1; rad = rr + np.sin(th)*travel + np.where(hit, 0.3*(travel - alt), 0)
    y = np.where(hit, 0.1*0.5*L['BodyMax'], alt - dy)
    pos = np.stack([rad*np.cos(phi), y, rad*np.sin(phi)], -1)
    size = rng.uniform(L['BodyMin'], L['BodyMax'], n)*np.array([P.bsize(L, min(x, 1)) for x in ue])
    rgb, _ = gradient(np.minimum(ue, 1), [(HEAD, 0), (MID, 0.3), (TAIL, 1)], [0.9, 0.6, 0])
    a = np.interp(np.minimum(ue, 1), [0, max(fi, 1e-3), 0.4, 1], [0, 0.9, 0.6, 0])
    out.append((pos[alive], size[alive], rgb[alive], a[alive], False))
    n = 8; u = rng.random(n); phi = rng.random(n)*2*np.pi; rr = np.sqrt(rng.random(n))*L['CoreSpawnRadius']
    pos = np.stack([rr*np.cos(phi), alt - L['CoreLength']*u, rr*np.sin(phi)], -1); size = L['CoreSize']*(1 - 0.2*u)
    cfi = min(0.6, L['CoreEmitOffset']/max(0.1, L['CoreLength']))
    rgb, _ = gradient(u, [([255, 255, 255], 0), (CORE, 0.35), (MID, 1)], [0, 0, 0])
    a = np.interp(u, [0, max(cfi, 1e-3), max(cfi + 0.01, 0.7), 1], [0, 0.8, 0.3, 0])
    out.append((pos, size, np.minimum(rgb*1.5, 255), a, False))
    n = 18; u = rng.random(n); pos = np.stack([np.zeros(n), alt - 0.5*L['Length']*u, np.zeros(n)], -1)
    pos = pos[pos[:, 1] > 0]; uu = u[:len(pos)] if len(pos) else u[:0]
    out.append((pos, np.full(len(pos), 0.5*d), np.tile([255, 230, 170.], (len(pos), 1)), np.interp(uu, [0, .4, 1], [.35, .25, 0]), False))
    return out

def deflect(pos, size):
    """Planes collision with the deck top (y=0), dampen 0.35: the part of the travel below the plane turns radial."""
    pos = pos.copy(); below = pos[:, 1] < 0.25*size*0.36
    exc = np.where(below, 0.25*size*0.36 - pos[:, 1], 0)
    rad = np.hypot(pos[:, 0], pos[:, 2]) + 1e-6; phi = np.arctan2(pos[:, 2], pos[:, 0])
    phi = np.where(rad < 0.05, np.random.default_rng(1).random(len(rad))*2*np.pi, phi)
    newr = rad + 0.65*exc
    pos[:, 0] = np.where(below, newr*np.cos(phi), pos[:, 0]); pos[:, 2] = np.where(below, newr*np.sin(phi), pos[:, 2])
    pos[:, 1] = np.where(below, 0.25*size*0.36, pos[:, 1]); return pos

def render(t, v, path):
    alt = alt_at(t); C = camera(alt); cam, fwd, right, up, f = C
    img, depth = opaque(C, alt); img = img.astype(np.float64)
    allp = []
    plist = particles(t, v)
    for ei, e in enumerate(plist):
        pos, size, rgb, a, add = e[:5]; ys = e[5] if len(e) > 5 else np.ones(len(pos))
        tight = (v in ('v7', 'v8', 'v9', 'v10', 'v11') and ei < 3)
        for i in range(len(pos)): allp.append((pos[i], size[i], rgb[i], a[i], add, tight, ys[i]))
    def z(p): return np.dot(p[0]-cam, fwd)
    allp.sort(key=lambda p: -z(p))
    lut = np.array([P.falloff(r) for r in np.linspace(0, 1.5, 1501)]); lutT = np.array([P.tight(r) for r in np.linspace(0, 1.5, 1501)])
    for p, s, rgb, a, add, tg, ysc in allp:
        d = p - cam; zc = np.dot(d, fwd)
        if zc < 0.5 or a <= 0.003: continue
        sx = W/2 + f*np.dot(d, right)/zc; sy = H/2 - f*np.dot(d, up)/zc; rp = 0.5*s*f/zc
        x0, x1 = int(max(0, sx-rp)), int(min(W, sx+rp+1)); y0, y1 = int(max(0, sy-rp*ysc)), int(min(H, sy+rp*ysc+1))
        if x0 >= x1 or y0 >= y1: continue
        X, Y = np.meshgrid(np.arange(x0, x1)+0.5, np.arange(y0, y1)+0.5)
        rr = np.hypot((X-sx)/max(rp, 1e-6), (Y-sy)/max(rp*ysc, 1e-6)); al = (lutT if tg else lut)[np.minimum((rr*1000).astype(int), 1500)]*a
        al = np.where(depth[y0:y1, x0:x1] > zc, al, 0)[..., None]   # depth test against opaques
        if add: img[y0:y1, x0:x1] += al*np.asarray(rgb)
        else: img[y0:y1, x0:x1] = img[y0:y1, x0:x1]*(1-al) + al*np.asarray(rgb)
    im = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)); dr = ImageDraw.Draw(im)
    dr.text((30, 30), f"REFERENCE SIM {v}  T+{t:.1f}s  alt {alt:.1f} m  cluster {CD} m  (approximation, not Unity)", fill=(0, 0, 0))
    im.save(path); return C

if __name__ == '__main__':
    v = sys.argv[1]; outdir = sys.argv[2]
    for t in [float(x) for x in sys.argv[3:]]:
        render(t, v, f"{outdir}/{v}-T{t:.1f}-iPhone-1290x2796.png"); print('wrote', v, t)

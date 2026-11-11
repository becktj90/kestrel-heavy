"""Mission technical commentary generator: mission-tech-lines.json -> ElevenLabs TTS -> two-pass loudnorm -> mp3 + manifest entries.
Run from repo root:  python3 tools/clipgen/gen_mission_tech.py [takes_json]
Only takes whose (voice, model, spoken text, settings) hash is not in the raw cache call the API
(same hash scheme as gen_static.py). Existing manifest clips are never touched: new clips are
appended to assets/narration/manifest.json and the Unity copy (kept byte-identical), new mp3s are
written to both trees with fresh .meta files, and test pins are NOT updated here (do that by hand).
Validates every take BEFORE spending credits; exits non-zero on any violation.
Credential: sealed ElevenLabs connection via dynamic_credentials (never a raw key)."""
import json, os, re, sys, hashlib, subprocess, urllib.request, time, uuid

sys.path.insert(0, "/opt/muse/skills/custom-connector-setup/bin")
from dynamic_credentials import add_surrogate_to_request

MODEL = 'eleven_multilingual_v2'
SRC_FMT = 'mp3_44100_128'
I, TP, LRA, BR = -16, -1.5, 7, '96k'  # loudnorm targets (same as gen_static.py)
VOICES = {
    'host': ('host', 'EXAVITQu4vr4xnSDxMaL', 'Sarah',
             dict(stability=0.45, similarity_boost=0.8, style=0.25, use_speaker_boost=True)),
    'cohost': ('cohost', 'iP95p4xoKVk53GoZ742B', 'Chris',
               dict(stability=0.45, similarity_boost=0.8, style=0.25, use_speaker_boost=True)),
}
SUBS = [(r'\bLOX\b', 'locks'), (r'\bRUD\b', 'R.U.D.'), (r'\bSES-1\b', 'S.E.S. one'), (r' — ', ', ')]
# Banned in technical lines: every statedFacts trigger in tests/narration-facts-lint.test.cjs
# that a claims:["mission"]-only take cannot carry, plus the plan-doc bans.
BANNED_RES = [
    (re.compile(r'\bengines?\b', re.I), 'engine(s)'),
    (re.compile(r'\bbooster\b', re.I), 'booster'),
    (re.compile(r'\bstrap-?ons?\b', re.I), 'strap-on(s)'),
    (re.compile(r'\bsecond stage\b', re.I), 'second stage'),
    (re.compile(r'\bthird stage\b', re.I), 'third stage'),
    (re.compile(r'\bupper\b', re.I), 'upper'),
    (re.compile(r'\blegs\b', re.I), 'legs'),
    (re.compile(r'\bhalves\b', re.I), 'halves'),
    (re.compile(r'\bkerosene\b', re.I), 'kerosene'),
    (re.compile(r'\bmethane\b', re.I), 'methane'),
    (re.compile(r'\bhydrogen\b', re.I), 'hydrogen'),
    (re.compile(r'\bHaven\b'), 'Haven'),
    (re.compile(r'\bbarge\b', re.I), 'barge'),
    (re.compile(r'\bdeck\b', re.I), 'deck'),
    (re.compile(r'\bsea state\b', re.I), 'sea state'),
    (re.compile(r'\bin the drink\b', re.I), 'in the drink'),
    (re.compile(r'\bhit the ground\b', re.I), 'hit the ground'),
    (re.compile(r'\bcoming home\b', re.I), 'coming home'),
    (re.compile(r'\bheading home\b', re.I), 'heading home'),
    (re.compile(r'\bwill fly again\b', re.I), 'will fly again'),
    (re.compile(r'\bsingle engine\b', re.I), 'single engine'),
    (re.compile(r'\ball (one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve)\b', re.I), 'all + number'),
    (re.compile(r'\banother stage\b', re.I), 'another stage'),
    (re.compile(r'\bcarries the payload\b', re.I), 'carries the payload'),
    (re.compile(r'\bfinal push\b', re.I), 'final push'),
    (re.compile(r'\binsertion (is )?complete\b', re.I), 'insertion complete'),
    (re.compile(r'\bpad landing\b', re.I), 'pad landing'),
    (re.compile(r'\bcluster\b', re.I), 'cluster'),
    (re.compile(r'\bcores\b', re.I), 'cores'),
    (re.compile(r'\bchambers\b', re.I), 'chambers'),
    (re.compile(r'\d'), 'digits'),
    (re.compile(r'\b(blue origin|new glenn|jacklyn|lc-36|nasa|spacex|falcon|saturn|apollo|shuttle)\b', re.I), 'real program/company name'),
    (re.compile(r'\b(Pier 7|Coastal Pad B|Salt Flat Pad|North Reach)\b'), 'game site name'),
]
WINDOWS = {
    'm1': ['color.pad', 'color.pad', 'color.ascent', 'color.ascent'],
    'm2': ['color.pad', 'color.pad', 'color.ascent', 'color.karman'],
    'm3': ['color.pad', 'color.pad', 'color.ascent', 'color.coast'],
    'm4': ['color.pad', 'color.pad', 'color.ascent', 'color.coast'],
    'm5': ['color.pad', 'color.pad', 'color.ascent', 'color.ascent'],
    'm6': ['color.pad', 'color.pad', 'color.ascent', 'color.recovery'],
    'm7': ['color.pad', 'color.pad', 'color.coast', 'color.recovery'],
    'm8': ['color.pad', 'color.pad', 'color.ascent', 'color.coast'],
    'm9': ['color.pad', 'color.pad', 'color.ascent', 'color.coast'],
    'm10': ['color.pad', 'color.pad', 'color.ascent', 'color.recovery'],
    'm11': ['color.pad', 'color.pad', 'color.ascent', 'color.coast'],
    'm12': ['color.pad', 'color.pad', 'color.ascent', 'color.coast'],
    'm13': ['color.pad', 'color.pad', 'color.ascent', 'color.coast'],
    'm14': ['color.pad', 'color.pad', 'color.ascent', 'color.coast'],
    'm15': ['color.pad', 'color.pad', 'color.ascent', 'color.coast'],
    'm16': ['color.pad', 'color.pad', 'color.ascent', 'color.coast'],
    'm17': ['color.pad', 'color.pad', 'color.ascent', 'color.coast'],
    'm18': ['color.pad', 'color.pad', 'color.ascent', 'color.coast'],
    'm19': ['color.pad', 'color.pad', 'color.ascent', 'color.recovery'],
    'm20': ['color.pad', 'color.pad', 'color.ascent', 'color.coast'],
}


def spoken(t):
    for a, b in SUBS:
        t = re.sub(a, b, t)
    return t


def validate(takes):
    errs = []
    seen_ids = set()
    by_mission = {}
    for t in takes:
        tid = t.get('id', '')
        if tid in seen_ids:
            errs.append(f"{tid}: duplicate id")
        seen_ids.add(tid)
        ch, ev, m = t.get('channel'), t.get('event'), t.get('mission')
        if ch not in VOICES:
            errs.append(f"{tid}: bad channel {ch}")
        if m not in WINDOWS:
            errs.append(f"{tid}: bad mission {m}")
        by_mission.setdefault(m, []).append(t)
        txt = t.get('text', '')
        words = txt.split()
        if len(words) > 14:
            errs.append(f"{tid}: {len(words)} words (>14): {txt!r}")
        if len(txt) > 80:
            errs.append(f"{tid}: {len(txt)} chars (>80): {txt!r}")
        if txt.count('.') + txt.count('!') + txt.count('?') > 0:
            n_sent = len([s for s in re.split(r'[.!?]\s+', txt.strip()) if s])
            if n_sent > 2:
                errs.append(f"{tid}: {n_sent} sentences (>2): {txt!r}")
        for rx, name in BANNED_RES:
            if rx.search(txt):
                errs.append(f"{tid}: banned {name!r} in {txt!r}")
        if not t.get('sources'):
            errs.append(f"{tid}: no sources")
        n_here = len([x for x in by_mission[m] if x['event'] == ev])
        if t.get('id') != f"{ch}/{ev}.{m}.t{n_here}":
            errs.append(f"{tid}: id does not follow channel/event.mission.t<n> (n counts within mission+event)")
    for m, ts in by_mission.items():
        if len(ts) != 4:
            errs.append(f"{m}: {len(ts)} takes (want 4)")
        if [t['event'] for t in ts] != WINDOWS[m]:
            errs.append(f"{m}: windows {[t['event'] for t in ts]} != {WINDOWS[m]}")
        if [t['channel'] for t in ts] != ['host', 'cohost', 'host', 'cohost']:
            errs.append(f"{m}: channels {[t['channel'] for t in ts]} != host/cohost/host/cohost")
    if len(by_mission) != 20:
        errs.append(f"missions covered: {sorted(by_mission)} (want m1..m20)")
    return errs


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args):
        raise RuntimeError('redirect refused, verify the endpoint')


def tts(vid, text, settings):
    req = urllib.request.Request(
        f"https://api.elevenlabs.io/v1/text-to-speech/{vid}?output_format={SRC_FMT}",
        data=json.dumps(dict(text=text, model_id=MODEL, voice_settings=settings)).encode(),
        headers={'Content-Type': 'application/json', 'Accept': 'audio/mpeg'})
    add_surrogate_to_request(req, 'custom.elevenlabs', allowed_hosts=['api.elevenlabs.io'])
    for k in range(3):
        try:
            return urllib.request.build_opener(NoRedirect()).open(req, timeout=60).read()
        except Exception as e:
            print('retry', e)
            time.sleep(3)
    raise SystemExit('tts failed for ' + vid)


def _ln(src, pre, extra=''):
    r = subprocess.run(['ffmpeg', '-hide_banner', '-nostats', '-i', src, '-af',
                        pre + f'loudnorm=I={I}:TP={TP}:LRA={LRA}{extra}:print_format=json',
                        '-f', 'null', '-'], capture_output=True, text=True).stderr
    return json.loads(r[r.rindex('{'):r.rindex('}') + 1])


def loudnorm(src, dst):
    m0 = _ln(src, '')
    g = I - float(m0['input_i'])
    pre = (f'aresample=176400,volume={g:.2f}dB,alimiter=limit=0.70:level=false:attack=0.5:release=40,'
           f'aresample=44100,')
    m = _ln(src, pre)
    af = (pre + f"loudnorm=I={I}:TP={TP}:LRA={LRA}:measured_I={m['input_i']}:measured_TP={m['input_tp']}:"
          f"measured_LRA={m['input_lra']}:measured_thresh={m['input_thresh']}:offset={m['target_offset']}:"
          f"linear=true:print_format=json")
    r = subprocess.run(['ffmpeg', '-hide_banner', '-nostats', '-y', '-i', src, '-af', af,
                        '-ar', '44100', '-ac', '1', '-b:a', BR, dst], capture_output=True, text=True)
    r.check_returncode()
    o = json.loads(r.stderr[r.stderr.rindex('{'):r.stderr.rindex('}') + 1])
    return o['normalization_type']


def measure(f):
    r = subprocess.run(['ffmpeg', '-hide_banner', '-nostats', '-i', f, '-af',
                        'loudnorm=print_format=json', '-f', 'null', '-'],
                       capture_output=True, text=True).stderr
    m = json.loads(r[r.rindex('{'):r.rindex('}') + 1])
    return float(m['input_i']), float(m['input_tp'])


META_TEMPLATE = None


def meta_for(guid):
    return META_TEMPLATE.replace('GUIDPLACEHOLDER', guid)


def main():
    takes_path = sys.argv[1] if len(sys.argv) > 1 else 'tools/clipgen/mission-tech-lines.json'
    data = json.load(open(takes_path))
    takes = data['takes']
    errs = validate(takes)
    if errs:
        print('VALIDATION FAILED:')
        for e in errs:
            print(' -', e)
        raise SystemExit(1)
    global META_TEMPLATE
    sample_meta = 'unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration/host/color.pad.m1.1.mp3.meta'
    META_TEMPLATE = open(sample_meta).read().replace(
        open(sample_meta).read().split('guid: ')[1].split('\n')[0], 'GUIDPLACEHOLDER')
    raw = 'tools/clipgen/.raw'
    os.makedirs(raw, exist_ok=True)
    man_path = 'assets/narration/manifest.json'
    man = json.load(open(man_path))
    have_ids = {c['id'] for c in man['clips']}
    new_clips, used, over6 = [], 0, []
    for t in takes:
        if t['id'] in have_ids:
            print('skip (already in manifest)', t['id'])
            continue
        vdir, vid, vname, vs = VOICES[t['channel']]
        sp = spoken(t['text'])
        h = hashlib.sha1(f"{vid}|{MODEL}|{sp}|{json.dumps(vs, sort_keys=True)}".encode()).hexdigest()[:12]
        rf = f"{raw}/{h}.mp3"
        if not os.path.exists(rf):
            open(rf, 'wb').write(tts(vid, sp, vs))
            used += len(sp)
            print('gen', vdir, t['id'], len(sp))
        rel = t['id'].split('/', 1)[1] + '.mp3'  # e.g. color.pad.m1.t1.mp3
        out = f"assets/narration/{vdir}/{rel}"
        os.makedirs(os.path.dirname(out), exist_ok=True)
        nt = loudnorm(rf, out)
        li, tp = measure(out)
        d = float(subprocess.check_output(['ffprobe', '-v', 'error', '-show_entries',
                                           'format=duration', '-of', 'csv=p=0', out]).decode())
        if d > 6:
            over6.append((t['id'], round(d, 3)))
        new_clips.append(dict(
            id=t['id'], event=t['event'], channel=t['channel'], layer='color',
            text=t['text'], spokenText=sp, voice=vname, voiceId=vid, voiceDir=vdir,
            model=MODEL, file=f"{vdir}/{rel}", durationSec=round(d, 3), lufs=li,
            truePeakDb=tp, normalization=nt, hash=h,
            sha256=hashlib.sha256(open(out, 'rb').read()).hexdigest(),
            requires={'mission': t['mission']}, claims=['mission'],
            mission=t['mission'], sources=t['sources']))
    if not new_clips:
        print('nothing new; chars sent 0')
        return
    man['clips'].extend(new_clips)
    man['generatedAt'] = time.strftime('%Y-%m-%d')
    src = man.get('source', '')
    if 'mission-tech-lines.json' not in src:
        man['source'] = src + '; tools/clipgen/mission-tech-lines.json (per-mission technical commentary)'
    blob = json.dumps(man, indent=1)
    open(man_path, 'w').write(blob)
    unity_man = 'unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration/manifest.json'
    open(unity_man, 'w').write(blob)
    import shutil
    for c in new_clips:
        unity_base = 'unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration'
        dstf = os.path.join(unity_base, c['file'])
        os.makedirs(os.path.dirname(dstf), exist_ok=True)
        shutil.copyfile(os.path.join('assets/narration', c['file']), dstf)
        guid = hashlib.md5(f"narration/{c['file']}".encode()).hexdigest()
        open(dstf + '.meta', 'w').write(meta_for(guid))
    print('clips added', len(new_clips), 'chars sent', used)
    if over6:
        print('OVER 6s (reword shorter and re-run; hash-skip re-bills only changed lines):', over6)


if __name__ == '__main__':
    main()

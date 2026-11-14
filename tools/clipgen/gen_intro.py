"""First-run intro clip generator: tools/clipgen/intro-lines.json -> ElevenLabs TTS -> two-pass loudnorm -> mp3 + manifest entries.

Run from repo root:  python3 tools/clipgen/gen_intro.py [takes_json]
Only takes whose (voice, model, spoken text, settings) hash is not in the raw cache call the API
(same hash scheme as gen_mission_tech.py). Existing manifest clips are never touched: new narration
clips are appended to assets/narration/manifest.json and the Unity copy (kept byte-identical), new
mp3s are written to both trees with fresh .meta files; the Thrum close clip is appended to
assets/narration/guide/manifest.json (the build-time pack script stages guide clips into
StreamingAssets/Resources, so no Unity copy is written here). Test pins are NOT updated here
(do that by hand). Validates every take BEFORE spending credits; exits non-zero on any violation.
Credential: sealed ElevenLabs connection via dynamic_credentials (never a raw key).
No clip in this batch uses Pearl.
"""
import json, os, re, sys, hashlib, subprocess, urllib.request, time, shutil

sys.path.insert(0, "/opt/muse/skills/custom-connector-setup/bin")
from dynamic_credentials import add_surrogate_to_request

MODEL = 'eleven_multilingual_v2'
SRC_FMT = 'mp3_44100_128'
I, TP, LRA, BR = -16, -1.5, 7, '96k'  # loudnorm targets (same as gen_mission_tech.py)
VOICES = {
    'host': ('host', 'EXAVITQu4vr4xnSDxMaL', 'Sarah',
             dict(stability=0.45, similarity_boost=0.8, style=0.25, use_speaker_boost=True)),
    'cohost': ('cohost', 'iP95p4xoKVk53GoZ742B', 'Chris',
               dict(stability=0.45, similarity_boost=0.8, style=0.25, use_speaker_boost=True)),
    'guide': ('guide', 'm2X69O7Xomk9219qo0Gy', 'Junie Pell v2',
              dict(stability=0.4, similarity_boost=0.8, style=0.35, use_speaker_boost=True)),
}
SUBS = [(r'\bLOX\b', 'locks'), (r'\bRUD\b', 'R.U.D.'), (r'\bSES-1\b', 'S.E.S. one'), (r' — ', ', ')]


def spoken(t):
    for a, b in SUBS:
        t = re.sub(a, b, t)
    return t


def validate(takes):
    errs = []
    seen = set()
    for t in takes:
        tid = t.get('id', '')
        if tid in seen:
            errs.append(f"{tid}: duplicate id")
        seen.add(tid)
        ch = t.get('channel')
        if ch not in VOICES:
            errs.append(f"{tid}: bad channel {ch}")
        txt = t.get('text', '')
        words = txt.split()
        if len(words) > 14:
            errs.append(f"{tid}: {len(words)} words (>14): {txt!r}")
        if len(txt) > 80:
            errs.append(f"{tid}: {len(txt)} chars (>80): {txt!r}")
        if re.search(r'\d', txt):
            errs.append(f"{tid}: digits in text: {txt!r}")
        if not t.get('sources'):
            errs.append(f"{tid}: no sources")
        if ch == 'guide':
            if not t.get('beat') or not t.get('trigger'):
                errs.append(f"{tid}: guide take needs beat and trigger")
        else:
            if t.get('event') != 'intro':
                errs.append(f"{tid}: narration take must use event intro")
            is_n = t.get('neutral') is True
            is_g = t.get('requires') is not None or t.get('claims') is not None
            if is_n == is_g:
                errs.append(f"{tid}: needs exactly one of neutral or requires+claims")
            if is_n and not (t.get('neutralReason') or '').strip():
                errs.append(f"{tid}: neutral needs neutralReason")
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
    takes_path = sys.argv[1] if len(sys.argv) > 1 else 'tools/clipgen/intro-lines.json'
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
    gman_path = 'assets/narration/guide/manifest.json'
    gman = json.load(open(gman_path))
    ghave_ids = {c['id'] for c in gman['clips']}
    new_clips, gnew_clips, used, over6 = [], [], 0, []
    for t in takes:
        vdir, vid, vname, vs = VOICES[t['channel']]
        sp = spoken(t['text'])
        h = hashlib.sha1(f"{vid}|{MODEL}|{sp}|{json.dumps(vs, sort_keys=True)}".encode()).hexdigest()[:12]
        rf = f"{raw}/{h}.mp3"
        if t['channel'] == 'guide':
            if t['id'] in ghave_ids:
                print('skip (already in guide manifest)', t['id'])
                continue
        elif t['id'] in have_ids:
            print('skip (already in manifest)', t['id'])
            continue
        if not os.path.exists(rf):
            open(rf, 'wb').write(tts(vid, sp, vs))
            used += len(sp)
            print('gen', vdir, t['id'], len(sp))
        if t['channel'] == 'guide':
            rel = t['id'] + '.mp3'
            out = f"assets/narration/guide/{rel}"
        else:
            rel = t['id'].split('/', 1)[1] + '.mp3'  # e.g. intro.b2.1.mp3
            out = f"assets/narration/{vdir}/{rel}"
        os.makedirs(os.path.dirname(out), exist_ok=True)
        nt = loudnorm(rf, out)
        li, tp = measure(out)
        d = float(subprocess.check_output(['ffprobe', '-v', 'error', '-show_entries',
                                           'format=duration', '-of', 'csv=p=0', out]).decode())
        if d > 6:
            over6.append((t['id'], round(d, 3)))
        sha = hashlib.sha256(open(out, 'rb').read()).hexdigest()
        if t['channel'] == 'guide':
            gnew_clips.append(dict(
                id=t['id'], beat=t['beat'], trigger=t['trigger'], text=t['text'], spokenText=sp,
                tokenFree='{' not in t['text'], voice=vname, voiceId=vid, model=MODEL,
                file=f"guide/{rel}", durationSec=round(d, 3), lufs=li, truePeakDb=tp,
                hash=h, sha256=sha))
        else:
            entry = dict(
                id=t['id'], event=t['event'], channel=t['channel'], text=t['text'], spokenText=sp,
                voice=vname, voiceId=vid, voiceDir=vdir, model=MODEL, file=f"{vdir}/{rel}",
                durationSec=round(d, 3), lufs=li, truePeakDb=tp, normalization=nt, hash=h,
                sha256=sha, sources=t['sources'])
            if t.get('neutral') is True:
                entry['neutral'] = True
                entry['neutralReason'] = t['neutralReason']
            else:
                entry['requires'] = t['requires']
                entry['claims'] = t['claims']
            new_clips.append(entry)
    if new_clips:
        man['clips'].extend(new_clips)
        man['generatedAt'] = time.strftime('%Y-%m-%d')
        src = man.get('source', '')
        if 'intro-lines.json' not in src:
            man['source'] = src + '; tools/clipgen/intro-lines.json (first-run intro)'
        blob = json.dumps(man, indent=1)
        open(man_path, 'w').write(blob)
        unity_man = 'unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration/manifest.json'
        open(unity_man, 'w').write(blob)
        for c in new_clips:
            unity_base = 'unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration'
            dstf = os.path.join(unity_base, c['file'])
            os.makedirs(os.path.dirname(dstf), exist_ok=True)
            shutil.copyfile(os.path.join('assets/narration', c['file']), dstf)
            guid = hashlib.md5(f"narration/{c['file']}".encode()).hexdigest()
            open(dstf + '.meta', 'w').write(meta_for(guid))
    if gnew_clips:
        gman['clips'].extend(gnew_clips)
        gman['generatedAt'] = time.strftime('%Y-%m-%d')
        open(gman_path, 'w').write(json.dumps(gman, indent=1))
    print('clips added', len(new_clips) + len(gnew_clips),
          '(narration', len(new_clips), 'guide', len(gnew_clips), ')', 'chars sent', used)
    if over6:
        print('OVER 6s (reword shorter and re-run; hash-skip re-bills only changed lines):', over6)


if __name__ == '__main__':
    main()

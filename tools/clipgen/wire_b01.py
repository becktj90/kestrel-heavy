#!/usr/bin/env python3
"""Wire the B01 mission-status takes (PR #495, audio/staging/B01) into the game narration manifest.

Moves take 1 of every wired line from audio/staging/B01/ into assets/narration/<voice>/, copies it into the Unity
Resources tree with an AudioImporter .meta, and appends a manifest entry to both manifest copies (byte-equal).
Take 2 and every held line stay in staging. Nothing is generated (no ElevenLabs call). Idempotent: lines already in
the manifest are skipped.

Held, never wired here:
  * hold:true takes (every fuel-low and landing.fuel-low line: no 12%-propellant trigger yet).
  * outcome.* (no emitter in the game yet).
  * status.offcourse / status.offcourse.clear (no corridor-deviation signal in the Unity sim).
  * sep-wide (no rough-separation signal in the Unity sim).
  * landing.sink.good / landing.sink.high / landing.drift (the live test needs a Physics ruling).
  * HELD_IDS below: a take that says more than the trigger checks.
"""
import hashlib, json, os, shutil, subprocess, sys, uuid

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
STAGE = os.path.join(ROOT, 'audio/staging/B01')
ASSETS = os.path.join(ROOT, 'assets/narration')
UNITY = os.path.join(ROOT, 'unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration')
META_TEMPLATE = os.path.join(UNITY, 'mission-control/pad.1.mp3.meta')

WIRED_EVENTS = {
    'status.ontrack', 'status.short', 'status.short.clear', 'status.speed', 'status.heating',
    'engine-out', 'engine-out.ok', 'sep-clear',
    'landing.alt.1k', 'landing.alt.500', 'landing.alt.100', 'landing.alt.10', 'landing.legs',
}
# Takes in a wired event that state more than the trigger checks.
HELD_IDS = {
    'mission-control/landing.alt.1k.g5-6': 'says "on profile"; the callout does not check the landing profile',
    'mission-control/landing.alt.500.g5-5': 'says "looking good"; the callout does not check the sink rate',
    'mission-control/landing.alt.500.g5-6': 'says "steady"; the callout does not check the sink rate',
    'mission-control/landing.alt.100.g5-3': 'says "slowing"; the callout does not check deceleration',
    'mission-control/landing.alt.100.g5-6': 'says "steady"; the callout does not check the sink rate',
    'mission-control/landing.alt.10.g5-3': 'says "easy"; the callout does not check the sink rate',
    'mission-control/landing.legs.g5-1': 'says "deploying"; the call fires when the legs finish deploying',
}
RECOVERED = ['barge', 'rtls']
CHANNEL = {'mission-control': 'mc', 'host': 'host', 'cohost': 'cohost'}
BANNED_EVENT = 'engine.roar.far.v3'


def gate(e):
    """The fact tag for one take: (neutral reason) or (requires, claims)."""
    ev = e['event']
    if ev == 'sep-clear':
        return None, {'stages': '>=2'}, ['stages']
    if ev.startswith('landing.alt.'):
        return None, {'recovery': RECOVERED}, ['recovery']
    if ev == 'landing.legs':
        return None, {'legs': True, 'recovery': RECOVERED}, ['legs', 'recovery']
    tags = e['tags']
    if tags.startswith('neutral:'):
        return tags[len('neutral:'):].strip(), None, None
    if tags.startswith('requires '):
        req_s, claims_s = tags[len('requires '):].split('; claims ')
        return None, json.loads(req_s), json.loads(claims_s)
    raise SystemExit('untagged take ' + e['id'])


def main():
    staging = json.load(open(os.path.join(STAGE, 'manifest.json')))
    paths = [os.path.join(ASSETS, 'manifest.json'), os.path.join(UNITY, 'manifest.json')]
    man = json.load(open(paths[0]))
    have = {c['id'] for c in man['clips']}
    meta = open(META_TEMPLATE).read()
    old_guid = meta.split('guid: ')[1].split('\n')[0]
    wired, held = [], []
    for e in staging:
        if e['take'] != 1:
            continue
        ev = e['event']
        reason = None
        if e.get('hold'):
            reason = 'hold:true (' + e.get('hold_reason', '') + ')'
        elif ev.startswith('outcome.'):
            reason = 'outcome.*: no emitter yet'
        elif ev == BANNED_EVENT:
            reason = 'banned clip'
        elif ev not in WIRED_EVENTS:
            reason = 'event not wired in this step'
        elif e['id'] in HELD_IDS:
            reason = HELD_IDS[e['id']]
        if reason:
            held.append((e['id'], reason))
            continue
        if len(e['text'].split()) > 14:
            raise SystemExit('over the 14-word in-flight cap: ' + e['id'])
        voice_dir, name = e['id'].split('/')
        rel = voice_dir + '/' + name + '.mp3'
        src = os.path.join(STAGE, e['file'])
        dst = os.path.join(ASSETS, rel)
        if e['id'] in have:
            wired.append(e['id'])
            continue
        if not os.path.exists(dst):
            subprocess.check_call(['git', 'mv', src, dst], cwd=ROOT)
        data = open(dst, 'rb').read()
        sha = hashlib.sha256(data).hexdigest()
        if sha != e['sha256']:
            raise SystemExit('sha256 mismatch ' + e['id'])
        shutil.copyfile(dst, os.path.join(UNITY, rel))
        with open(os.path.join(UNITY, rel + '.meta'), 'w') as f:
            f.write(meta.replace(old_guid, uuid.uuid4().hex))
        neutral, req, claims = gate(e)
        clip = {
            'id': e['id'], 'event': ev, 'channel': CHANNEL[voice_dir], 'layer': 'call', 'interruptible': False,
            'text': e['text'], 'spokenText': e['spokenText'], 'voice': e['voice'], 'voiceId': e['voiceId'],
            'voiceDir': voice_dir, 'model': e['model'], 'file': rel, 'durationSec': e['duration'],
            'lufs': e['LUFS'], 'truePeakDb': e['dBTP'], 'normalization': e['normalization'],
            'hash': sha[:12], 'sha256': sha,
        }
        if neutral is not None:
            clip['neutral'] = True
            clip['neutralReason'] = neutral
        else:
            clip['requires'] = req
            clip['claims'] = claims
        clip['batch'] = 'B01'
        man['clips'].append(clip)
        wired.append(e['id'])
    note = 'audio/staging/B01 (#495, B01 mission-status takes; take 1 wired by tools/clipgen/wire_b01.py)'
    if note not in man['source']:
        man['source'] += '; ' + note
    out = json.dumps(man, indent=1, ensure_ascii=True)
    for p in paths:
        open(p, 'w').write(out)
    with open(os.path.join(STAGE, 'WIRED.md'), 'w') as f:
        f.write('# B01 wiring (tools/clipgen/wire_b01.py)\n\n')
        f.write('Take 1 of each wired line moved to `assets/narration/<voice>/<id>.mp3` (Unity copy in Resources/Narration).\n')
        f.write('Take 2 of every line, and every held line, stay here. `manifest.json` in this folder is the recording record and still lists the moved files under their staging names.\n\n')
        f.write('## Wired (%d)\n\n' % len(wired))
        for i in wired:
            f.write('- `%s`\n' % i)
        f.write('\n## Held (%d)\n\n| id | why |\n|---|---|\n' % len(held))
        for i, r in held:
            f.write('| `%s` | %s |\n' % (i, r))
    print('wired', len(wired), 'held', len(held), 'manifest clips', len(man['clips']))


if __name__ == '__main__':
    main()

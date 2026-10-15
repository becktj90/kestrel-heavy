"""Thrum (Mabel Thrum Pike) guide voice clips: data/guide.json kid cards -> ElevenLabs TTS -> loudnorm -> mp3.
Run from repo root: python3 tools/clipgen/gen_guide.py [raw_cache_dir]   (needs ELEVENLABS_API_KEY)
Cards with {tokens} use a token-free spoken variant from GENERIC below (the on-screen caption keeps the numbers).
Ascent beats (asc.*) are skipped: Thrum does not appear in flight. Cached by hash; unchanged lines cost nothing."""
import json,os,re,sys,hashlib,subprocess,urllib.request,time,glob
KEY=os.environ.get('ELEVENLABS_API_KEY'); MODEL='eleven_multilingual_v2'; SRC_FMT='mp3_44100_128'
I,TP,LRA,BR=-16,-1.5,7,'96k'  # loudnorm targets
SUBS=[(r'\bLOX\b','locks'),(r'\bRUD\b','R.U.D.'),(r'\bSES-1\b','S.E.S. one'),(r' — ',', ')]
def spoken(t):
  for a,b in SUBS: t=re.sub(a,b,t)
  return t
def _ln(src,pre,extra=''):
  r=subprocess.run(['ffmpeg','-hide_banner','-nostats','-i',src,'-af',pre+f'loudnorm=I={I}:TP={TP}:LRA={LRA}{extra}:print_format=json','-f','null','-'],capture_output=True,text=True).stderr
  return json.loads(r[r.rindex('{'):r.rindex('}')+1])
def loudnorm(src,dst):
  """Pre-gain to target + peak limiter (so linear mode can reach -16 without clipping), then two-pass linear loudnorm."""
  m0=_ln(src,''); g=I-float(m0['input_i'])
  pre=f'aresample=176400,volume={g:.2f}dB,alimiter=limit=0.70:level=false:attack=0.5:release=40,aresample=44100,'
  m=_ln(src,pre)
  af=(pre+f"loudnorm=I={I}:TP={TP}:LRA={LRA}:measured_I={m['input_i']}:measured_TP={m['input_tp']}:measured_LRA={m['input_lra']}"
      f":measured_thresh={m['input_thresh']}:offset={m['target_offset']}:linear=true:print_format=json")
  r=subprocess.run(['ffmpeg','-hide_banner','-nostats','-y','-i',src,'-af',af,'-ar','44100','-ac','1','-b:a',BR,dst],capture_output=True,text=True)
  r.check_returncode(); o=json.loads(r.stderr[r.stderr.rindex('{'):r.stderr.rindex('}')+1]); return o['normalization_type']
def measure(f):
  r=subprocess.run(['ffmpeg','-hide_banner','-nostats','-i',f,'-af','loudnorm=print_format=json','-f','null','-'],capture_output=True,text=True).stderr
  m=json.loads(r[r.rindex('{'):r.rindex('}')+1]); return float(m['input_i']),float(m['input_tp'])
VID,VNAME='m2X69O7Xomk9219qo0Gy','Junie Pell v2'
VS=dict(stability=0.4,similarity_boost=0.8,style=0.35,use_speaker_boost=True)
GENERIC={
'brief.wind.c1':'There is a breeze out there. The rocket will lean. That is fine. We plan for it.',
'step.2.pass.c1':'Tidy! Plenty of push for the weight. That rises.',
'step.2.fail.mix.c1':'That mixture is off. Engines sulk when they are fed wrong. Nudge it back.',
'step.3.fail.dv.c1':'You run dry before the goal. Add fuel or a second stage.',
'step.5.fail.reserve.c1':'Not enough fuel set aside to land. Hold back a little more.',
'pre.odds.c1':'There is a chance an engine sulks mid-flight. More engines and good avionics help.',
'win.margin.c1':'You had fuel to spare. Spare is wise. Too much spare is just heavy.',
'win.budget.c1':'And under budget. Spend it on something shiny.',
'win.unlock.c1':'New toy! Open the Hangar and try it on.',
'win.next.c1':'Next up, the next mission. Heavier, higher. Want it?',
'fail.structural-maxq.c1':'She bent in the middle. The air pushed too hard.',
'fail.structural-maxq.c2':'The walls could not hold. Thicker walls, or ease the throttle.',
'fail.prop-depletion.c1':'Out of fuel, and short of the goal. More fuel, or another stage.',
'fail.miss.c1':'Great flight, wrong place. So close.',
'medal.bonus.c1':'Bonus goal done. Extra shine.',
'medal.cosmetic.c1':'New paint! Free, earned, yours.'}
def spoken(t): return t.replace(' — ',', ')
G=json.load(open('data/guide.json')); raw=sys.argv[1] if len(sys.argv)>1 else 'tools/clipgen/.raw'
os.makedirs(raw,exist_ok=True); base='assets/narration/guide'; os.makedirs(base,exist_ok=True); man=[]; used=0
for l in G['lines']:
  if l['id'].startswith('asc.'): continue
  for c in l['cards']:
    cid=c['id']; sp=spoken(GENERIC.get(cid,c['kid']))
    assert '{' not in sp,cid
    h=hashlib.sha1(f"{VID}|{MODEL}|{sp}|{json.dumps(VS,sort_keys=True)}".encode()).hexdigest()[:12]; rf=f"{raw}/{h}.mp3"
    if not os.path.exists(rf):
      req=urllib.request.Request(f"https://api.elevenlabs.io/v1/text-to-speech/{VID}?output_format={SRC_FMT}",
        data=json.dumps(dict(text=sp,model_id=MODEL,voice_settings=VS)).encode(),
        headers={'xi-api-key':KEY,'Content-Type':'application/json','Accept':'audio/mpeg'})
      for k in range(3):
        try: open(rf,'wb').write(urllib.request.urlopen(req,timeout=90).read()); break
        except Exception as e: print('retry',cid,e); time.sleep(3)
      else: raise SystemExit('fail '+cid)
      used+=len(sp); print('gen',cid,len(sp),flush=True)
    out=f"{base}/{cid}.mp3"; nt=loudnorm(rf,out); li,tp=measure(out)
    d=float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','csv=p=0',out]).decode())
    man.append(dict(id=cid,beat=l['id'],trigger=l['trigger'],text=c['kid'],spokenText=sp,tokenFree='{' not in c['kid'],voice=VNAME,voiceId=VID,
      model=MODEL,file=f"guide/{cid}.mp3",durationSec=round(d,3),lufs=li,truePeakDb=tp,hash=h,sha256=hashlib.sha256(open(out,'rb').read()).hexdigest()))
json.dump(dict(generatedAt=time.strftime('%Y-%m-%d'),model=MODEL,voice=VNAME,voiceId=VID,voiceSettings=VS,format=f'mp3 44.1kHz mono {BR}',
  note='Generated with ElevenLabs on Trevor Beck\'s paid Creator plan. Kid lines; token lines use token-free spoken variants. Ascent beats skipped.',clips=man),open(base+'/manifest.json','w'),indent=1)
print('clips',len(man),'chars sent',used)

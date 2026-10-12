"""Static narration clip generator: manifest lines -> ElevenLabs TTS -> two-pass loudnorm -> mp3.
Run from repo root:  node tools/clipgen/extract_lines.mjs > /tmp/lines.json
                     python3 tools/clipgen/gen_static.py /tmp/lines.json [raw_cache_dir]
Only lines whose (voice, model, spoken text, settings) hash is not in the raw cache call the API."""
import json,os,re,sys,hashlib,subprocess,urllib.request,time,glob
KEY=os.environ.get('ELEVENLABS_API_KEY'); MODEL='eleven_multilingual_v2'; SRC_FMT='mp3_44100_128'
I,TP,LRA,BR=-16,-1.5,7,'96k'  # loudnorm targets
VOICES={'mc':('mission-control','cjVigY5qzO86Huf0OWal','Eric',dict(stability=0.75,similarity_boost=0.8,style=0.0,use_speaker_boost=True)),
 'host':('host','EXAVITQu4vr4xnSDxMaL','Sarah',dict(stability=0.45,similarity_boost=0.8,style=0.25,use_speaker_boost=True)),
 'cohost':('cohost','iP95p4xoKVk53GoZ742B','Chris',dict(stability=0.45,similarity_boost=0.8,style=0.25,use_speaker_boost=True))}
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
L=json.load(open(sys.argv[1])); raw=sys.argv[2] if len(sys.argv)>2 else 'tools/clipgen/.raw'
os.makedirs(raw,exist_ok=True); base='assets/narration'; man=[]; used=0; keep=set()
for l in L:
  vdir,vid,vname,vs=VOICES[l['channel']]
  cid=f"{l['event']}.{l['variant']+1}"; sp=spoken(l['text'])
  h=hashlib.sha1(f"{vid}|{MODEL}|{sp}|{json.dumps(vs,sort_keys=True)}".encode()).hexdigest()[:12]
  rf=f"{raw}/{h}.mp3"
  if not os.path.exists(rf):
    req=urllib.request.Request(f"https://api.elevenlabs.io/v1/text-to-speech/{vid}?output_format={SRC_FMT}",
      data=json.dumps(dict(text=sp,model_id=MODEL,voice_settings=vs)).encode(),
      headers={'xi-api-key':KEY,'Content-Type':'application/json','Accept':'audio/mpeg'})
    for k in range(3):
      try: open(rf,'wb').write(urllib.request.urlopen(req,timeout=60).read()); break
      except Exception as e: print('retry',cid,e); time.sleep(3)
    else: raise SystemExit('fail '+cid)
    used+=len(sp); print('gen',vdir,cid,len(sp))
  out=f"{base}/{vdir}/{cid}.mp3"; os.makedirs(os.path.dirname(out),exist_ok=True); keep.add(out)
  nt=loudnorm(rf,out); li,tp=measure(out)
  d=float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','csv=p=0',out]).decode())
  man.append(dict(id=f"{vdir}/{cid}",event=l['event'],channel=l['channel'],text=l['text'],spokenText=sp,voice=vname,voiceId=vid,voiceDir=vdir,
    model=MODEL,file=f"{vdir}/{cid}.mp3",durationSec=round(d,3),lufs=li,truePeakDb=tp,normalization=nt,
    hash=h,sha256=hashlib.sha256(open(out,'rb').read()).hexdigest()))
for f in glob.glob(base+'/*/*.mp3'):
  if f not in keep: os.remove(f); print('removed',f)
json.dump(dict(generatedAt=time.strftime('%Y-%m-%d'),model=MODEL,format=f'mp3 44.1kHz mono {BR}',
  loudness=dict(targetLUFS=I,truePeakDb=TP,lra=LRA,method='pre-gain + alimiter (4x oversampled), then ffmpeg loudnorm two-pass linear'),
  source='web/js/narration/manifest.js (mc/host/cohost channels; token lines excluded)',clips=man),open(base+'/manifest.json','w'),indent=1)
print('clips',len(man),'chars sent',used)

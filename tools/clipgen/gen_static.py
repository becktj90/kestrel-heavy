import json,os,re,hashlib,subprocess,urllib.request,time
KEY=os.environ['ELEVENLABS_API_KEY']; MODEL='eleven_multilingual_v2'; FMT='mp3_44100_64'
VOICES={'mc':('mission-control','cjVigY5qzO86Huf0OWal','Eric',dict(stability=0.75,similarity_boost=0.8,style=0.0,use_speaker_boost=True)),
 'host':('host','EXAVITQu4vr4xnSDxMaL','Sarah',dict(stability=0.45,similarity_boost=0.8,style=0.25,use_speaker_boost=True)),
 'cohost':('cohost','iP95p4xoKVk53GoZ742B','Chris',dict(stability=0.45,similarity_boost=0.8,style=0.25,use_speaker_boost=True))}
SUBS=[(r'\bLCH4\b','liquid methane'),(r'\bLOX\b','locks'),(r'\bRUD\b','R.U.D.'),(r'\bINT bar\b','integrity bar'),(r'\bSES-1\b','S.E.S. one'),
 (r'\bHOLD\b','hold'),(r'BOOSTER RECOVERED','Booster recovered'),(r'\bq is peaking','Q is peaking'),(r' — ',', ')]
def spoken(t):
  for a,b in SUBS: t=re.sub(a,b,t)
  return t
L=json.load(open('lines.json')); base='assets/narration'; man=[]; used=0
mpath=base+'/manifest.json'
for l in L:
  vdir,vid,vname,vs=VOICES[l['channel']]
  cid=f"{l['event']}.{l['variant']+1}".replace('/','-')
  sp=spoken(l['text'])
  h=hashlib.sha1(f"{vid}|{MODEL}|{sp}|{json.dumps(vs,sort_keys=True)}".encode()).hexdigest()[:12]
  out=f"{base}/{vdir}/{cid}.mp3"; os.makedirs(os.path.dirname(out),exist_ok=True)
  hf=out+'.hash'
  if not(os.path.exists(out) and os.path.exists(hf) and open(hf).read()==h):
    req=urllib.request.Request(f"https://api.elevenlabs.io/v1/text-to-speech/{vid}?output_format={FMT}",
      data=json.dumps(dict(text=sp,model_id=MODEL,voice_settings=vs)).encode(),
      headers={'xi-api-key':KEY,'Content-Type':'application/json','Accept':'audio/mpeg'})
    for k in range(3):
      try: open(out,'wb').write(urllib.request.urlopen(req,timeout=60).read()); break
      except Exception as e: print('retry',cid,e); time.sleep(3)
    else: raise SystemExit('fail '+cid)
    open(hf,'w').write(h); used+=len(sp)
  d=float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','csv=p=0',out]).decode())
  man.append(dict(id=f"{vdir}/{cid}",event=l['event'],channel=l['channel'],text=l['text'],spokenText=sp,voice=vname,voiceId=vid,voiceDir=vdir,
    model=MODEL,format=FMT,file=f"{vdir}/{cid}.mp3",durationSec=round(d,3),hash=h))
json.dump(dict(generatedAt=time.strftime('%Y-%m-%d'),model=MODEL,format=FMT,source='web/js/narration/manifest.js (mc/host/cohost channels; token lines excluded)',clips=man),open(mpath,'w'),indent=1)
print('clips',len(man),'chars sent',used)

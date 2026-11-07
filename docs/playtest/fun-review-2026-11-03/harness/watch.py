# Kid persona reflex: during flight, take a frame every ~0.7s; if a bright cyan prompt button appears in the
# lower third, tap its centre (the "obvious glowing button"). Logs everything through the controller.
import sys,time,json,urllib.request,urllib.parse
from PIL import Image
port,secs,tag=sys.argv[1],float(sys.argv[2]),sys.argv[3]; react=len(sys.argv)<5 or sys.argv[4]!='noreact'
def C(q): return json.loads(urllib.request.urlopen(f'http://localhost:{port}/?'+q).read())
t0=time.time(); k=0; lastTap=0
while time.time()-t0<secs:
    k+=1; r=C(f'c=shot&name={tag}-{k:03d}'); im=Image.open(r['file']).convert('RGB'); W,H=im.size
    px=im.load(); xs=[];ys=[]
    for y in range(int(H*0.6),H,3):
        for x in range(0,W,3):
            R,G,B=px[x,y]
            if R<90 and G>195 and B>225: xs.append(x);ys.append(y)
    if react and len(xs)>120 and time.time()-lastTap>1.5:
        cx,cy=sum(xs)//len(xs),sum(ys)//len(ys)
        C(f'c=tap&x={cx}&y={cy}&note='+urllib.parse.quote(f'kid reflex: tapped glowing prompt ({len(xs)} cyan px) seen in {r["file"].split("/")[-1]}'))
        lastTap=time.time()
    time.sleep(0.2)

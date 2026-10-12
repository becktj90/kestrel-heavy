# Python port of Kestrel.Slice.PlumeShape (fix/p0-plume-look v5) for self-checks and the reference renderer.
import math
SSF=0.36; SVF=0.8; PSF=0.71; PFF=0.74; NOZ=0.0; BodySizeStart=0.9; CoreSizeStart=1.0; BodyFullAt=0.15; CoreSizeOverLife=0.8; CoreFullAt=0.6; N=40
def ss(e0,e1,x):
    t=min(1,max(0,(x-e0)/(e1-e0))); return t*t*(3-2*t)
def falloff(r):
    t=min(1,max(0,1-r)); s=t*t*(3-2*t); return s*s
def compute(s,pa,d,pd=0.57):
    s=min(1,max(0,s)); pa=max(0,pa); x=pd/max(pa,1e-4)
    L=d*(6+10*s)*(1+0.6*ss(0.2,0,pa)); W=d*min(6,max(0.7,math.sqrt(x))); return L,W
def bsize(L,u): return BodySizeStart+(1-BodySizeStart)*u/BodyFullAt if u<BodyFullAt else 1+(L['BodyGrow']-1)*(u-BodyFullAt)/(1-BodyFullAt)
def csize(u): return CoreSizeStart+(1-CoreSizeStart)*u/CoreFullAt if u<CoreFullAt else 1+(CoreSizeOverLife-1)*(u-CoreFullAt)/(1-CoreFullAt)
def layout(tf,pa,cd,R,H,deck):
    d=max(0.5,cd); tf=min(1,max(0,tf)); sl,sw=compute(tf,pa,d); L=dict(VisualD=d)
    L['Length']=max(sl,0.6*H*(0.4+0.6*tf)); L['Length']=min(L['Length'],dense_length(H,tf,pa)); L['Width']=max(sw,1.2*d)
    L['CoreSize']=0.3*d/PSF; L['CoreSpawnRadius']=0.4*d; L['CoreLength']=1.0*d
    L['BodySpawnRadius']=0.08*d; L['BodyMax']=1.15*d/PSF; L['BodyMin']=0.95*d/PSF; L['BodyGrow']=1.15; L['BodyLife']=0.6
    solidEnd=L['BodyMax']*PSF*L['BodyGrow']+2*L['BodySpawnRadius']
    L['BodyConeDeg']=math.degrees(math.atan2(max(0,L['Width']-solidEnd)*0.5,L['Length']))
    bo=co=0
    for i in range(N+1):
        u=i/N
        bo=max(bo,0.5*PFF*L['BodyMax']*bsize(L,u)-L['Length']*u-NOZ*d)
        co=max(co,0.5*PFF*L['CoreSize']*csize(u)-L['CoreLength']*u-NOZ*d)
    L['BodyEmitOffset']=bo; L['CoreEmitOffset']=co
    dk=max(deck,5); L['CloudRadius']=2*max(dk,10); L['CloudSizeMax']=max(0.55*dk,0.2*H)/SSF; L['CloudSizeMin']=0.55*L['CloudSizeMax']; L['CloudSizeStart']=0.45
    L['CloudClearRadius']=max(1.5*R,1.5*d); half0=0.5*SVF*L['CloudSizeMax']*L['CloudSizeStart']
    L['CloudSpawnRadius']=max(1.5*dk,L['CloudClearRadius']+half0); L['CloudHeight']=0.5
    return L
def top_edge(L):
    t=-1e9
    for i in range(N+1):
        u=i/N
        t=max(t,0.5*PFF*L['BodyMax']*bsize(L,u)-L['BodyEmitOffset']-L['Length']*u,0.5*PFF*L['CoreSize']*csize(u)-L['CoreEmitOffset']-L['CoreLength']*u)
    return t
def start_gap(L): return L['BodyEmitOffset']-0.5*PSF*L['BodyMax']*BodySizeStart
def body_width(L): return 2*L['BodySpawnRadius']+2*L['Length']*math.tan(math.radians(L['BodyConeDeg']))+L['BodyMax']*PSF*L['BodyGrow']
if __name__=='__main__':
    # solid / visible radius of the sprite
    import numpy as np
    rs=np.linspace(0,1,100001); f=np.array([falloff(r) for r in rs[::100]])
    print('Falloff=0.5 at r=%.3f, 0.01 at r=%.3f'%(rs[::100][np.argmin(abs(f-0.5))],rs[::100][np.argmin(abs(f-0.01))]))
    for tf,pa in [(1,1),(0.4,1),(1,0.3),(1,0.01),(1,0)]:
        for d in (3,12,14):
            L=layout(tf,pa,d,6,70,15); print(tf,pa,d,'top=%.3f (allow %.3f) gap=%.3f (<=%.2f) width=%.2f (%.2fD) emit=%.3f/%.3f coregap=%.3f cloudH=%.2f'%(top_edge(L),NOZ*d,start_gap(L),0.1*d,body_width(L),body_width(L)/d,L['BodyEmitOffset'],L['CoreEmitOffset'],L['CoreEmitOffset']-0.5*SSF*L['CoreSize']*CoreSizeStart,L['CloudHeight']))

def tight(r):
    t=min(1,max(0,(r-0.67)/0.08)); return 1-t*t*(3-2*t)

def dense_length(H,tf,pa): return max(5,H)*(0.6+0.4*min(1,max(0,tf)))*(1+0.6*ss(0.2,0,max(0,pa)))
def ramp(L): return min(0.04,max(0.002,0.02*L['VisualD']/max(1,L['Length'])))
def near_gap(L): return start_gap(L)+0.5*ramp(L)*L['Length']
def near_overlap(L,n,span):
    k=n*min(1,span/max(1,L['Length'])); solid=0.5*(L['BodyMin']+L['BodyMax'])*BodySizeStart*PSF; return k*solid/span

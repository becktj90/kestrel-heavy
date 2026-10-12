from PIL import Image; import numpy as np, glob, json
out={}
for f in sorted(glob.glob('119-v4/*.png')):
    a=np.asarray(Image.open(f).convert('RGB')).astype(float); H,W,_=a.shape
    r,g,b=a[...,0],a[...,1],a[...,2]
    sky=(b-r>40)                      # blue sky
    lum=(r+g+b)/3
    # sky colour reference per row = median of leftmost 3% columns
    # vehicle: grey/white non-sky pixels; find rows where non-sky span exists right of tower: use column right half
    nonsky=~sky
    # nozzle row: lowest row above horizon where bright white narrow spikes (nozzles) exist: detect horizon first
    rowsky=sky[:, int(W*0.9):].mean(1)
    horizon=int(np.argmax(rowsky<0.5)) if (rowsky<0.5).any() else H
    # find vehicle columns: rows 55-75% height, non-sky in x>0.38W
    res={'size':[W,H],'horizon_y':horizon}
    x0=int(W*0.37)
    # vehicle base: scan upward from horizon for the first row whose non-sky run in x>x0 is wide (>15% W)
    base=None
    for y in range(horizon-2, int(H*0.3), -1):
        row=nonsky[y,x0:]
        xs=np.where(row)[0]
        if len(xs) and (xs.max()-xs.min())>0.15*W and row.mean()>0.12:
            base=y;break
    if base:
        xs=np.where(nonsky[base-int(0.02*H),x0:])[0]+x0
        res['vehicle_base_row']=base; res['vehicle_base_x']=[int(xs.min()),int(xs.max())]; res['vehicle_base_width_px']=int(xs.max()-xs.min())
    # plume: very bright warm/white pixels (lum>235) below base
    yb=base or horizon
    pl=(lum>232)&(r>=b-5)
    reg=pl[yb:min(H,yb+int(0.25*H)), :]
    ys,xs=np.where(reg)
    if len(xs):
        res['plume_bright_bbox']=[int(xs.min()),int(ys.min()+yb),int(xs.max()),int(ys.max()+yb)]
        # widest row
        widths=[(reg[i].sum(),i) for i in range(reg.shape[0])]
        w,i=max(widths); rx=np.where(reg[i])[0]
        res['plume_max_width_px']=int(rx.max()-rx.min()); res['plume_max_width_row']=int(i+yb)
        res['plume_gap_below_base_px']=int(ys.min())
    # ground cloud: whitish low-sat pixels below horizon brighter than ground median
    gnd=a[horizon:,:]
    gl=lum[horizon:,:]; sat=a[horizon:].max(2)-a[horizon:].min(2)
    gmed=np.median(gl[-int(0.05*H):,:])
    cl=(gl>gmed+25)&(sat<40)
    ys,xs=np.where(cl)
    if len(xs):
        res['cloud_bbox']=[int(xs.min()),int(ys.min()+horizon),int(xs.max()),int(ys.max()+horizon)]
        res['cloud_px_fraction_of_frame']=round(len(xs)/(W*H),4)
        res['cloud_mean_lum_excess']=round(float((gl[cl]-gmed).mean()),1)
        res['cloud_est_opacity']=round(float(np.clip((gl[cl]-gmed)/(245-gmed),0,1).mean()),2)
    res['ground_median_lum']=round(float(gmed),1)
    out[f]=res
print(json.dumps(out,indent=1))

import { MANIFEST } from '../../web/js/narration/manifest.js';
const seen=new Map(); const out=[];
for (const e of Object.values(MANIFEST)) for (const l of e.lines) {
  if (l.channel==='coach') continue;
  l.variants.forEach((t,i)=>{
    if (/\{[a-z]+\}/.test(t)) return;
    const k=l.channel+'|'+t.trim(); if(seen.has(k)) return; seen.set(k,1);
    out.push({event:e.id,channel:l.channel,variant:i,text:t.trim()});
  });
}
console.log(JSON.stringify(out,null,1));

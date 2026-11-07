// Fun-review bot controller: one browser, HTTP commands, every action timestamped to JSONL.
const { chromium } = require('/usr/local/lib/pnpm/5/.pnpm/playwright-core@1.59.1/node_modules/playwright-core');
const http=require('http'), fs=require('fs'), url=require('url');
const [,, outDir, W, H, port] = process.argv;
fs.mkdirSync(outDir,{recursive:true});
const log=fs.createWriteStream(outDir+'/actions.jsonl',{flags:'a'});
let T0=Date.now(), p, b, n=0;
const L=(o)=>{o.t=((Date.now()-T0)/1000).toFixed(2); o.wall=new Date().toISOString(); log.write(JSON.stringify(o)+'\n'); return o;};
(async()=>{
 b=await chromium.launch({executablePath:'/usr/bin/google-chrome',args:['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--autoplay-policy=no-user-gesture-required']});
 const ctx=await b.newContext({viewport:{width:+W,height:+H},deviceScaleFactor:1,hasTouch:false});
 p=await ctx.newPage();
 p.on('console',m=>L({ev:'console',type:m.type(),text:m.text().slice(0,300)}));
 p.on('pageerror',e=>L({ev:'pageerror',text:String(e).slice(0,300)}));
 p.on('request',r=>{const u=r.url(); if(!u.startsWith('http://localhost:8797')) L({ev:'netreq',url:u.slice(0,200)});});
 await p.addInitScript(()=>{window.__fps=[];let c=0,l=performance.now();const f=()=>{c++;const n=performance.now();if(n-l>=1000){window.__fps.push(Math.round(c*1000/(n-l)));c=0;l=n;}requestAnimationFrame(f)};requestAnimationFrame(f);});
 T0=Date.now(); L({ev:'goto'});
 await p.goto('http://localhost:8797/index.html',{waitUntil:'load'});
 http.createServer(async(req,res)=>{const q=url.parse(req.url,true).query; let r={};
  try{
   const c=q.c;
   if(c==='tap'){const x=+q.x,y=+q.y; await p.mouse.move(x,y,{steps:3}); await p.mouse.down(); await p.waitForTimeout(+q.hold||120); await p.mouse.up(); r=L({ev:'tap',x,y,hold:+q.hold||120,note:q.note||''});}
   else if(c==='drag'){const [x1,y1,x2,y2]=[q.x1,q.y1,q.x2,q.y2].map(Number); await p.mouse.move(x1,y1); await p.mouse.down(); await p.mouse.move(x2,y2,{steps:+q.steps||15}); await p.mouse.up(); r=L({ev:'drag',x1,y1,x2,y2,note:q.note||''});}
   else if(c==='hold'){const x=+q.x,y=+q.y; await p.mouse.move(x,y); await p.mouse.down(); await p.waitForTimeout(+q.ms); await p.mouse.up(); r=L({ev:'hold',x,y,ms:+q.ms,note:q.note||''});}
   else if(c==='key'){await p.keyboard.press(q.k); r=L({ev:'key',k:q.k,note:q.note||''});}
   else if(c==='shot'){n++; const f=`${outDir}/${String(n).padStart(3,'0')}-${q.name||'shot'}.png`; await p.screenshot({path:f}); r=L({ev:'shot',file:f,note:q.note||''});}
   else if(c==='wait'){await p.waitForTimeout(+q.ms); r=L({ev:'wait',ms:+q.ms,note:q.note||''});}
   else if(c==='start'){const s=Date.now(); await p.waitForSelector('#start',{state:'visible',timeout:300000}); r=L({ev:'loader-ready',load_s:(Date.now()-s)/1000}); await p.click('#start'); L({ev:'tap',sel:'#start',note:'TAP TO PLAY'});}
   else if(c==='fps'){r=L({ev:'fps',fps:await p.evaluate(()=>window.__fps.slice(-10))});}
   else if(c==='note'){r=L({ev:'note',text:q.text||''});}
   else if(c==='eval'){r=L({ev:'eval',result:await p.evaluate(q.js)});}
   else if(c==='quit'){L({ev:'quit'}); res.end('bye'); await b.close(); process.exit(0);}
  }catch(e){r=L({ev:'error',text:String(e).slice(0,300)});}
  res.end(JSON.stringify(r)+'\n');}).listen(+port);
 console.log('ready');
})();

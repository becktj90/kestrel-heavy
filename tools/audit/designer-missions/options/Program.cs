using System; using System.Linq; using System.IO; using System.Collections.Generic;
using Kestrel.Core.Data; using Kestrel.Core.Json; using Kestrel.Core.Designer; using Kestrel.Core.Hangar; using Kestrel.Core.Vehicle; using Kestrel.Core.Economy; using Kestrel.Core.Flight;
static class P {
  static readonly string Root = System.Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT") ?? System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppContext.BaseDirectory, "../../../../../../.."));
  static Catalog C; static HangarData H;
  static JsonObject Tpl(string id){ var t=H.Template(id); return HangarEditor.ForkTemplate(t); }
  static JsonObject Preset(string id){ foreach(JsonObject p in MiniJson.ParseObject(File.ReadAllText(Root+"/data/presets.json")).Arr("presets")) if(p.Str("id")==id) return Kestrel.Core.Save.DesignMigration.Migrate(p); return null; }
  static string Rep(JsonObject d){ var r=Validate.Engineering(C,d); return $"ok={r.ok} blocks=[{string.Join(" | ",r.blocks.Select(b=>b.code+":"+b.msg))}] warns=[{string.Join(",",r.warnings.Select(b=>b.code))}]" + (r.readout!=null?$" twr={r.readout.liftoffTWR:F3} dv={r.readout.totalDeltaV:F0} margin={r.readout.margin:F3} cost={r.readout.cost} fits={r.readout.fairingFits} volMargin={r.readout.fairingVolumeMargin_m3:F2} diaMargin={r.readout.fairingDiameterMargin_m:F2}":""); }
  static JsonObject S0(JsonObject d)=> (JsonObject)d.Arr("stages")[0];
  static JsonObject Clone(JsonObject d)=>JsonMut.CloneObject(d);
  static void Main(string[] a){
    C=Catalog.LoadFromRepo(Root); H=HangarData.LoadFromRepo(Root);
    Console.WriteLine("templates: "+string.Join(",",H.templates.Select(t=>t.id)));
    Console.WriteLine("== 0. Validate every template/preset/starter");
    foreach(var t in H.templates) Console.WriteLine(" tpl "+t.id+": "+Rep(Tpl(t.id)));
    foreach(JsonObject p in MiniJson.ParseObject(File.ReadAllText(Root+"/data/presets.json")).Arr("presets")) Console.WriteLine(" preset "+p.Str("id")+": "+Rep(Kestrel.Core.Save.DesignMigration.Migrate(p)));
    foreach(JsonObject m in MiniJson.ParseObject(File.ReadAllText(Root+"/data/board-starters.json")).Arr("missions")) foreach(JsonObject d in m.Arr("designs")) Console.WriteLine(" starter "+m.Str("id")+" "+m.Str("mode")+": "+Rep(d));

    Console.WriteLine("== 1. legsKind written by DesignerV3Edits.SetLegsKind (landing.legsKind) vs Design.FromJson (legs.kind)");
    { var d=Tpl("lander"??"workhorse"); if(d==null) d=Tpl("workhorse"); var l=d.Obj("landing"); JsonMut.Set(l,"legs",true); JsonMut.Set(l,"legsKind","heavy");
      var des=Design.FromJson(d); Console.WriteLine(" landing.legsKind=heavy -> Design.legsKind="+(des.legsKind??"null")+" dryS1="+Mass.Of(C,des).stages[0].dry.ToString("F1")+" engages="+FailureMonitor.Engages(des));
      var d2=Clone(d); JsonMut.Set(d2,"legs",DJ(("kind","heavy"))); var des2=Design.FromJson(d2); Console.WriteLine(" legs.kind=heavy -> Design.legsKind="+des2.legsKind+" dryS1(Mass)="+Mass.Of(C,des2).stages[0].dry.ToString("F1")+" ; Balance mass(not shown) uses LegDryFraction="+RecoveryModel.LegDryFraction("heavy")); }

    Console.WriteLine("== 2. Cost: landing propellant double count");
    { var d=Tpl("workhorse"); var s=S0(d); JsonMut.Set(s,"landingPropMass",0d); var q0=CostModel.PriceDesign(C,Design.FromJson(d)); double pm=s.Num("propMass");
      JsonMut.Set(s,"landingPropMass",Math.Floor(pm*0.3)); var q1=CostModel.PriceDesign(C,Design.FromJson(d));
      Console.WriteLine($" propMass={pm} landing0 propellant cost={q0.byCategory["propellant"]} ; landing30% cost={q1.byCategory["propellant"]} ; total prop mass unchanged={Mass.Of(C,Design.FromJson(d)).stages[0].prop}"); }

    Console.WriteLine("== 3. Cost ignores engineParams.pc / nozzle / tune");
    { var d=Tpl("workhorse"); var s=S0(d); var base0=CostModel.PriceDesign(C,Design.FromJson(d)).byCategory["engines"];
      var d1=Clone(d); JsonMut.Set(S0(d1),"engineParams",DJ(("pc",C.EngineById[S0(d1).Str("engine")].pcRated*1.15))); var c1=CostModel.PriceDesign(C,Design.FromJson(d1)).byCategory["engines"];
      var d2=Clone(d); JsonMut.Set(S0(d2),"engineParams",DJ(("pc",C.EngineById[S0(d2).Str("engine")].pcMin))); var c2=CostModel.PriceDesign(C,Design.FromJson(d2)).byCategory["engines"];
      var d3=Clone(d); HangarEditor.SetNozzle(C,d3,PartRef.Stage(0),"extended"); var c3=CostModel.PriceDesign(C,Design.FromJson(d3)).byCategory["engines"];
      var d4=Clone(d); HangarEditor.SetTune(d4,PartRef.Stage(0),15,15); var c4=CostModel.PriceDesign(C,Design.FromJson(d4)).byCategory["engines"];
      var d5=Clone(d); JsonMut.Set(S0(d5),"tank",DJ(("insulation","mli"),("commonBulkhead",true))); var c5=CostModel.PriceDesign(C,Design.FromJson(d5)); var c5b=CostModel.PriceDesign(C,Design.FromJson(d));
      var d6=Clone(d); HangarEditor.SetFins(d6,true); var c6=CostModel.PriceDesign(C,Design.FromJson(d6)).recurring;
      Console.WriteLine($" engine={S0(d).Str("engine")} base={base0} pc115%={c1} pcMin={c2} extNozzle={c3} tune+15={c4} ; tank mli+cb structure={c5.byCategory["structure"]} vs plain={c5b.byCategory["structure"]} ; fins on total={c6} vs off={c5b.recurring}");
      Console.WriteLine($"  thrust pc115 vs base: {EnginePoint.Of(C,Design.FromJson(d1).stages[0]).thrustVac:F0} vs {EnginePoint.Of(C,Design.FromJson(d).stages[0]).thrustVac:F0}"); }

    Console.WriteLine("== 4. Tank-length stepper collapse");
    { var d=Tpl("workhorse"); var p=PartRef.Stage(1<d.Arr("stages").Count?1:0); var s=HangarParts.Body(d,p); JsonMut.Set(s,"propMass",3000d); double L=HangarEditor.TankLength(C,s);
      Console.WriteLine($" stage dia={s.Num("diameter")} prop={s.Str("propellant")} propMass=3000 tankLen={L:F3}"); var e=HangarEditor.SetTankLength(C,d,p,L-0.5); Console.WriteLine($" after -0.5 m: {e.what} {e.from}->{e.to} propMass={s.Num("propMass")}"); }

    Console.WriteLine("== 5. Payload diameter ignored + two volume models");
    { var d=Tpl("workhorse"); HangarEditor.SetPayloadType(C,d,"gto-comsat"); var f=d.Obj("fairing"); JsonMut.Set(f,"diameter",1.5); JsonMut.Set(f,"length",40d);
      Console.WriteLine(" gto-comsat (catalog dia 3.4 m) in 1.5x40 m fairing: "+Rep(d));
      var d2=Tpl("workhorse"); HangarEditor.SetPayloadType(C,d2,"leo-sat"); var f2=d2.Obj("fairing"); JsonMut.Set(f2,"diameter",2.5); JsonMut.Set(f2,"length",3.5); HangarEditor.SetPayloadMass(d2,3000);
      Console.WriteLine(" leo-sat 3000 kg in 2.5x3.5: "+Rep(d2));
      var d3=Clone(d2); HangarEditor.SetPayloadMass(d3,500); JsonMut.Set(d3.Obj("fairing"),"length",2.2); Console.WriteLine(" leo-sat 500 kg in 2.5x2.2: "+Rep(d3)); }

    Console.WriteLine("== 6. Cryo: composite+kerolox / glass+hydrolox");
    { var d=Preset("scout"); Console.WriteLine(" scout v2 as shipped: "+Rep(d)+" engages="+FailureMonitor.Engages(Design.FromJson(d)));
      var r0=ReferenceAscent.Run(C,d); Console.WriteLine("  predict: failure="+(r0.predictedFailure?.kind??"none"));
      var d1=Clone(d); Kestrel.Core.Designer.DesignerKnobs.SetMixtureOffset(d1,0,0); Console.WriteLine(" scout + mixtureOffset 0 (no physical change): "+Rep(d1)+" engages="+FailureMonitor.Engages(Design.FromJson(d1)));
      var r1=ReferenceAscent.Run(C,d1); Console.WriteLine("  predict: failure="+(r1.predictedFailure?.kind??"none")+" t="+(r1.predictedFailure?.t??-1));
      var d2=Tpl("workhorse"); foreach(JsonObject s in d2.Arr("stages")) { HangarEditor.SetPropellant(C,d2,PartRef.Stage(d2.Arr("stages").IndexOf(s)),"hydrolox"); JsonMut.Set(s,"material","glass-phenolic"); }
      Console.WriteLine(" workhorse all hydrolox + glass-phenolic: "+Rep(d2)); }

    Console.WriteLine("== 7. Gimbal override 0 on stage 1, unstable unguided");
    { var d=Tpl("workhorse"); var s=S0(d); Console.WriteLine(" base: "+Rep(d)); Kestrel.Core.Designer.DesignerKnobs.SetGimbal(C,d,0,0); var des=Design.FromJson(d);
      Console.WriteLine($" gimbalDeg=0: PadSteered={DesignRules.PadSteered(C,des)} EnginePoint.gimbal={EnginePoint.Of(C,des.stages[0]).gimbal} cal={Balance.Of(C,des).stabilityCal:F2} "+Rep(d)); }

    Console.WriteLine("== 8. Guidance shown defaults vs sim defaults");
    { var d=Tpl("workhorse"); Console.WriteLine($" shown pitchKickDeg={DesignerKnobs.ShownGuidance(d,"pitchKickDeg")} sim default={GuidanceProfile.DefaultKickDeg}; shown gLimit={DesignerKnobs.ShownGuidance(d,"gLimit")} sim default=none; shown kickAlt={DesignerKnobs.ShownGuidance(d,"pitchKickAlt")} sim default=kick at {GuidanceProfile.DefaultKickSpeed} m/s");
      var e=DesignerKnobs.NudgeGuidance(d,"pitchKickDeg",+1); Console.WriteLine($" one + nudge on pitch kick: {e.from}->{e.to} stored={d.Obj("guidance").Num("pitchKickDeg")}");
      var g=DesignerKnobs.SetGuidance(Tpl("workhorse"),"gLimit",12); var dd=Tpl("workhorse"); DesignerKnobs.SetGuidance(dd,"gLimit",12); Console.WriteLine($" SetGuidance gLimit 12 -> {dd.Obj("guidance").Num("gLimit")} (limits.json v3 max 12); pitchKickDeg 30 -> "+ (DesignerKnobs.SetGuidance(dd,"pitchKickDeg",30).to)); }

    Console.WriteLine("== 9. Limits parsed");
    { var L=DesignerLimits.Active; Console.WriteLine($" areal al-cu={L.Areal("al-cu")} ti={L.Areal("titanium")} ; fallback al-cu={DesignerLimits.Fallback().Areal("al-cu")} ; strapCounts={string.Join(",",L.strapOnCounts)}"); }

    Console.WriteLine("== 10. Engine envelopes vs v3 limits");
    foreach(var e in C.Engines){ var env=EngineModel.EnvelopeFor(e); double vmax=e.thrustSL==0?250:120; if(env.epsMax>vmax||env.epsMin<8) Console.WriteLine($" {e.id} eps {env.epsMin}-{env.epsMax} vs limits 8-{vmax}"); if(e.throttle[1]>1) Console.WriteLine($" {e.id} throttle max {e.throttle[1]}"); }

    Console.WriteLine("== 11. Readout vs flight mixture/tune and v3 tank");
    { var d=Tpl("workhorse"); var b=Readout.Quick(C,d).totalDeltaV; var d1=Clone(d); DesignerKnobs.SetTankPressure(d1,0,6); var r1=Readout.Quick(C,d1); var d2=Clone(d); DesignerKnobs.SetTankPressure(d2,0,1.5);
      Console.WriteLine($" dv base={b:F0} p=6bar dv={r1.totalDeltaV:F0} p=1.5bar dv={Readout.Quick(C,d2).totalDeltaV:F0}; default shown pressure={DesignerKnobs.ShownPressure(d,0)}");
      var d3=Clone(d); DesignerKnobs.SetTankPressure(d3,0,3); Console.WriteLine($" set pressure to shown default 3 bar: dv={Readout.Quick(C,d3).totalDeltaV:F0} dryS1 {Mass.Of(C,Design.FromJson(d)).stages[0].dry:F0} -> {Mass.Of(C,Design.FromJson(d3)).stages[0].dry:F0}"); }

    Console.WriteLine("== 12. Interstage shape truss/hot-staging rejected; AddStage on hydrolox etc");
    { var d=Tpl("workhorse"); Console.WriteLine(" SetInterstageShape truss: "+HangarEditor.SetInterstageShape(d,1,"truss").changed); }

    Console.WriteLine("== 13. Fairing jettison on suborbital hopper");
    { var d=Tpl("hopper"); var des=Design.FromJson(d); var req=Stack.RequiredDeltaV(C,des.payloadOrbit); Console.WriteLine($" hopper orbit={des.payloadOrbit} target apo={req.targetAlt} jettison={des.fairingJettisonAltitudeKm} km; min allowed={DesignerLimits.Active.jettisonMinKm}"); }

    Console.WriteLine("== 14. Strap-on count 3/6/8 in v3 limits");
    { var d=Tpl("workhorse"); Console.WriteLine(" SetBoosterCount 3: "+HangarEditor.SetBoosterCount(C,d,3).changed); JsonMut.Set(d.Obj("strapOns"),"count",6d); Console.WriteLine(" json count 6: "+Rep(d)); }

    Console.WriteLine("== 15. throttleMin knob below catalog & deep throttle in flight");
    { var d=Tpl("workhorse"); var e=C.EngineById[S0(d).Str("engine")]; Console.WriteLine($" engine {e.id} catalog throttle {e.throttle[0]}-{e.throttle[1]}"); DesignerKnobs.SetThrottleMin(C,d,0,0.05); var des=Design.FromJson(d); Console.WriteLine($" set 0.05 -> EnginePoint throttleMin={EnginePoint.Of(C,des.stages[0]).throttle[0]} massEach={EnginePoint.Of(C,des.stages[0]).mass:F1} vs {e.mass}"); }
    { var d=Tpl("workhorse"); HangarEditor.SetEngine(C,d,PartRef.Stage(0),"pf-hypergolic"); }


    Console.WriteLine("== 17. Home auto-build per campaign mission");
    { var camp=Campaign.FromJson(MiniJson.ParseObject(File.ReadAllText(Root+"/data/campaign.json"))); HomeAutoBuild.InstallMissionOrbits(C,camp);
      foreach(var m in camp.Missions){ var ch=HomeAutoBuild.Choose(C,H.templates,m); if(ch==null){Console.WriteLine(" "+m.Id+" null");continue;} var r=Validate.Engineering(C,ch.Design);
        Console.WriteLine($" {m.Id} tpl={ch.TemplateId} fits(PayloadFit)={ch.FitsPayload} reach={ch.ReachesOrbit} launchEnabled={ch.LaunchEnabled} blocks=[{string.Join(" | ",r.blocks.Select(b=>b.code+":"+b.msg))}] mission vol={m.VolumeM3} dia={m.DiameterM} mass={m.MassKg} payload={m.PayloadId}"); } }
    Console.WriteLine("== 18. gimbal 0 predicted flight");
    { var d=Tpl("workhorse"); DesignerKnobs.SetGimbal(C,d,0,0); var r=ReferenceAscent.Run(C,d); Console.WriteLine(" failure="+(r.predictedFailure?.kind??"none")+" t="+(r.predictedFailure?.t??-1)+" minStab="+r.minStability+" minCar="+r.minCar); }
    Console.WriteLine("== 19. tank pressure dry mass sweep");
    { foreach(var bar in new[]{1.5,3,6}){ var d=Tpl("workhorse"); DesignerKnobs.SetTankPressure(d,0,bar); var des=Design.FromJson(d); var s=des.stages[0]; Console.WriteLine($" {bar} bar: shell={Mass.StructureMass(C,s,des.safetyFactor):F0} legacy={Mass.StructuralFractionMass(C,s,des.safetyFactor):F0} cost structure={CostModel.PriceDesign(C,des).byCategory["structure"]}"); }
      var d0=Tpl("workhorse"); DesignerV3Insul(d0); var des0=Design.FromJson(d0); Console.WriteLine($" insulation mli only: shell={Mass.StructureMass(C,des0.stages[0],des0.safetyFactor):F0}"); }

    Console.WriteLine("== 20. sepMode chip (stages[0].sepMode)");
    foreach(var tid in new[]{"workhorse","heavy"}) foreach(var mode in new[]{"cold","hot","springs"}) { var d=Tpl(tid); JsonMut.Set(S0(d),"sepMode",mode); var des=Design.FromJson(d); var r=ReferenceAscent.Run(C,d); var v=Validate.Engineering(C,d);
      Console.WriteLine($" {tid} straps={des.StrapOnCount} sepMode={mode}: predicted={(r.predictedFailure?.kind??"none")} t={(r.predictedFailure?.t??-1):F1} validateOk={v.ok} warns=[{string.Join(",",v.warnings.Select(w=>w.code))}]"); }
    { var d=Tpl("workhorse"); var r=ReferenceAscent.Run(C,d); Console.WriteLine(" workhorse untouched predicted="+(r.predictedFailure?.kind??"none")); var d2=Tpl("heavy"); var r2=ReferenceAscent.Run(C,d2); Console.WriteLine(" heavy untouched predicted="+(r2.predictedFailure?.kind??"none")); }
    Console.WriteLine("== 21. crossfeed / fins / legs");
    { var d=Tpl("heavy"); var so=d.Obj("strapOns"); Console.WriteLine(" heavy strap kind="+so.Str("kind")+" count="+so.Num("count"));
      if(so.Str("kind")!="liquid") HangarEditor.SetBoosterKind(C,d,"liquid"); HangarEditor.SetCrossfeed(d,false); var qa=Readout.Quick(C,d); HangarEditor.SetCrossfeed(d,true); var qb=Readout.Quick(C,d); Console.WriteLine($" liquid boosters crossfeed off dv={qa.totalDeltaV:F1} on dv={qb.totalDeltaV:F1} cost {qa.cost} vs {qb.cost}"); }
    { var d=Tpl("workhorse"); HangarEditor.SetFins(d,false); var des0=Design.FromJson(d); var m0=Mass.Of(C,des0).liftoffMass; var cd0=Stack.BaseCd(des0);
      HangarEditor.SetFins(d,true); HangarEditor.SetFinCount(d,3); HangarEditor.SetFinSpan(d,0.2); var des1=Design.FromJson(d); var cd1=Stack.BaseCd(des1); var m1=Mass.Of(C,des1).liftoffMass; var cal1=Balance.Of(C,des1).stabilityCal;
      HangarEditor.SetFinCount(d,8); HangarEditor.SetFinSpan(d,6); var des2=Design.FromJson(d); Console.WriteLine($" fins off m={m0:F0} cd={cd0:F3}; 3x0.2m m={m1:F0} cd={cd1:F3} cal={cal1:F2}; 8x6m m={Mass.Of(C,des2).liftoffMass:F0} cd={Stack.BaseCd(des2):F3} cal={Balance.Of(C,des2).stabilityCal:F2} cost={CostModel.PriceDesign(C,des2).recurring} vs {CostModel.PriceDesign(C,des0).recurring}"); }
    Console.WriteLine("== 22. AddStage copies engineParams/interstageMass/tank from stage below");
    { var d=Tpl("workhorse"); var s1=(JsonObject)d.Arr("stages")[1]; JsonMut.Set(s1,"engineParams",DJ(("expansionRatio",8d),("pc",40d))); JsonMut.Set(s1,"interstageMass",5000d); JsonMut.Set(s1,"mixtureOffsetPct",9d);
      var e=HangarEditor.AddStage(C,d,2); var s2=(JsonObject)d.Arr("stages")[2]; Console.WriteLine($" new top engine={s2.Str("engine")} engineParams={(s2.Obj("engineParams")==null?"null":string.Join(";",s2.Obj("engineParams").Order.Select(k=>k+"="+s2.Obj("engineParams").Num(k))))} interstageMass={s2.Num("interstageMass")} mixture={s2.Num("mixtureOffsetPct")}"); }
    Console.WriteLine("== 23. HangarEditor.SetPropellant hybrid/udmh, and propellant without engines");
    { var d=Tpl("workhorse"); var e=HangarEditor.SetPropellant(C,d,PartRef.Stage(0),"udmh-nto"); Console.WriteLine(" stage1 -> udmh-nto: "+e.changed+" engine="+S0(d).Str("engine")+" ; "+Rep(d)); }

    Console.WriteLine("== 24. FailureMonitor engagement on templates");
    foreach(var t in H.templates){ var d=Tpl(t.id); DesignerKnobs.SetMixtureOffset(d,0,0); var r=ReferenceAscent.Run(C,d); var q=Readout.Quick(C,d);
      var d2=Clone(d); JsonMut.Set(d2,"heatShield",true); var r2=ReferenceAscent.Run(C,d2);
      Console.WriteLine($" {t.id} +mixture0: predicted={(r.predictedFailure?.kind??"none")} t={(r.predictedFailure?.t??-1):F1} maxQ={q.maxQ_kPa:F1} qLim={q.qLim_kPa:F1} s0engines={S0(d).Num("engineCount")} ; +heatShield: {(r2.predictedFailure?.kind??"none")} ; heatShield UI: none"); }

    Console.WriteLine("== 25. MakeItFly vs fixed cluster pattern");
    { var d=Tpl("hopper"); HangarEditor.SetPropMass(d,PartRef.Stage(0),30000); Console.WriteLine(" before: "+Rep(d)+" pattern="+S0(d).Obj("cluster")?.Str("pattern"));
      var (fx,ch)=Validate.MakeItFly(C,d); Console.WriteLine(" changes: "+string.Join("; ",ch)); Console.WriteLine(" after: "+Rep(fx)+" engines="+((JsonObject)fx.Arr("stages")[0]).Num("engineCount")+" pattern="+((JsonObject)fx.Arr("stages")[0]).Obj("cluster")?.Str("pattern")); }

    Console.WriteLine("== 26. Solid motor throttled via throttleMin knob");
    { var d=Tpl("hopper"); HangarEditor.SetPropellant(C,d,PartRef.Stage(0),"solid-apcp"); JsonMut.Set(S0(d),"engine","srm-small"); JsonMut.Set(S0(d),"engineCount",1d); JsonMut.Set(S0(d),"cluster",DJ(("pattern","auto")));
      DesignerKnobs.SetGuidance(d,"gLimit",1.5); var a1=ReferenceAscent.Simulate(C,d); var ev1=a1.events==null?"":string.Join(",",a1.events.Take(3).Select(e=>e.type+"@"+e.t.ToString("F1")));
      DesignerKnobs.SetThrottleMin(C,d,0,0.05); var a2=ReferenceAscent.Simulate(C,d); var ev2=a2.events==null?"":string.Join(",",a2.events.Take(3).Select(e=>e.type+"@"+e.t.ToString("F1")));
      Console.WriteLine($" srm-small catalog throttle={C.EngineById["srm-small"].throttle[0]}-{C.EngineById["srm-small"].throttle[1]} ; gLimit1.5 catalog-throttle maxG={a1.maxG:F2} events={ev1} ; throttleMin 0.05 maxG={a2.maxG:F2} events={ev2}"); }
    Console.WriteLine("== 16. Booster sepMode on stage 0 & sepMode/insulation no-op edits");
  }
  static void DesignerV3Insul(JsonObject d){ var s=(JsonObject)d.Arr("stages")[0]; var t=new JsonObject(); t.Add("insulation","mli"); JsonMut.Set(s,"tank",t);} 
  static JsonObject DJ(params (string k, object v)[] kv){ var o=new JsonObject(); foreach(var (k,v) in kv) o.Add(k,v); return o; }
}

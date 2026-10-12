using System.Collections.Generic;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class VehicleGoldenTests
    {
        const double REL = 1e-9, ABS = 1e-9;
        static void N(double a, object e, string what) => Golden.Near("vehicle", a, e, REL, ABS, what);

        [Test]
        public void MassStackAndSerialStages_MatchJs_ForEveryPreset()
        {
            var c = Golden.Catalog; int n = 0;
            foreach (var g in Golden.Cases("vehicle"))
            {
                string id = g.Str("preset"); var o = g.Obj("out");
                var d = Design.FromJson(g.Obj("design"));

                var sm = Mass.StageMasses(c, d); var esm = o.Arr("stageMasses");
                Assert.AreEqual(esm.Count, sm.Count, id);
                for (int i = 0; i < sm.Count; i++)
                {
                    var e = (JsonObject)esm[i];
                    N(sm[i].dry, e["dry"], $"{id}.stage{i}.dry"); N(sm[i].prop, e["prop"], $"{id}.stage{i}.prop"); N(sm[i].wet, e["wet"], $"{id}.stage{i}.wet");
                    N(sm[i].usableProp, e["usableProp"], $"{id}.stage{i}.usable"); N(sm[i].landingProp, e["landingProp"], $"{id}.stage{i}.landing");
                }
                var so = Mass.StrapOnMasses(c, d); var eso = o.Obj("strapOnMasses");
                N(so.count, eso["count"], id + ".so.count"); N(so.dry, eso["dry"], id + ".so.dry"); N(so.wet, eso["wet"], id + ".so.wet");
                N(so.eachDry, eso.Obj("each")["dry"], id + ".so.eachDry");

                var vm = Mass.Of(c, d); var evm = o.Obj("vehicleMass");
                N(vm.liftoffMass, evm["liftoffMass"], id + ".liftoff"); N(vm.dryMass, evm["dryMass"], id + ".dry"); N(vm.fairing, evm["fairing"], id + ".fairing");
                var er = evm.Arr("ratios");
                for (int i = 0; i < vm.ratios.Length; i++) { var e = (JsonObject)er[i]; N(vm.ratios[i].m0, e["m0"], $"{id}.r{i}.m0"); N(vm.ratios[i].massRatio, e["massRatio"], $"{id}.r{i}.MR"); N(vm.ratios[i].propFraction, e["propFraction"], $"{id}.r{i}.pf"); }
                N(Mass.FairingMass(d), o["fairingMass"], id + ".fairingMass");
                N(Stack.BaseCd(d), o["baseCd"], id + ".Cd"); N(Stack.ReferenceDiameter(d), o["referenceDiameter"], id + ".refD");

                var ts = Stack.ToSimStages(c, d); var ets = o.Arr("toSimStages");
                Assert.AreEqual(ets.Count, ts.Count, id + " toSimStages count");
                for (int i = 0; i < ts.Count; i++)
                {
                    var e = (JsonObject)ets[i]; var s = ts[i]; string w = $"{id}.sim{i}";
                    Assert.AreEqual(e.Str("role"), s.role, w + ".role");
                    N(s.thrustSL, e["thrustSL"], w + ".thrustSL"); N(s.thrustVac, e["thrustVac"], w + ".thrustVac"); N(s.ispSL, e["ispSL"], w + ".ispSL"); N(s.ispVac, e["ispVac"], w + ".ispVac");
                    N(s.propMass, e["propMass"], w + ".prop"); N(s.dryMass, e["dryMass"], w + ".dry"); N(s.diameter, e["diameter"], w + ".d"); N(s.Cd, e["Cd"], w + ".Cd");
                    if (e.ContainsKey("reserveMass")) N(s.reserveMass, e["reserveMass"], w + ".reserve");
                    if (e.ContainsKey("payloadMass")) { N(s.payloadMass, e["payloadMass"], w + ".payload"); N(s.fairingMass, e["fairingMass"], w + ".fairing"); N(s.fairingJettisonAltitudeM, e["fairingJettisonAltitudeM"], w + ".jett"); }
                }
                var idv = Stack.IdealDeltaV(ts); N(idv.total, o.Obj("idealDeltaV")["total"], id + ".idealDV");

                var tanks = o.Arr("tanks");
                for (int i = 0; i < d.stages.Count; i++)
                {
                    var t = DesignMath.SizeTank(c, d.stages[i].propellant, d.stages[i].propMass, d.stages[i].diameter); var e = (JsonObject)tanks[i];
                    N(t.volume, e["volume"], $"{id}.tank{i}.vol"); N(t.length, e["length"], $"{id}.tank{i}.len");
                }

                var ser = Stack.SerialStages(c, d); var eser = o.Obj("serialStages");
                N(ser.payload, eser["payload"], id + ".serial.payload");
                var ess = eser.Arr("stages"); Assert.AreEqual(ess.Count, ser.stages.Count, id + " serial count");
                for (int i = 0; i < ess.Count; i++)
                {
                    var e = (JsonObject)ess[i]; var s = ser.stages[i]; string w = $"{id}.serial{i}";
                    Assert.AreEqual(e.Str("role"), s.role, w); Assert.AreEqual(e.Str("label"), s.label, w);
                    N(s.thrustVac, e["thrustVac"], w + ".Fvac"); N(s.thrustSL, e["thrustSL"], w + ".Fsl"); N(s.ispVac, e["ispVac"], w + ".ispVac"); N(s.ispSL, e["ispSL"], w + ".ispSL");
                    N(s.propMass, e["propMass"], w + ".prop"); N(s.dryMass, e["dryMass"], w + ".dry"); N(s.diameter, e["diameter"], w + ".d"); N(s.cd, e["cd"], w + ".cd");
                }
                var req = Stack.RequiredDeltaV(c, d.payloadOrbit); var ereq = o.Obj("requiredDeltaV");
                N(req.targetAlt, ereq["targetAlt"], id + ".targetAlt"); N(req.required, ereq["required"], id + ".required");
                var sdv = Kestrel.Core.Physics.Headless.StackDeltaV(ser.stages.ConvertAll(x => x.ToSim()), ser.payload);
                N(sdv.total, o.Obj("stackDeltaV")["total"], id + ".stackDV");
                n++;
            }
            Assert.That(n, Is.GreaterThanOrEqualTo(6));
        }
    }
}

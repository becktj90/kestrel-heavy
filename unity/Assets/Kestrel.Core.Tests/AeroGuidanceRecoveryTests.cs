// Designer v3 aero, guidance and recovery (§2.4, §2.5, §2.7, §2.8).
// GV9 is the stability sign. The tumble FailureEvent is WP-E.
using System;
using System.IO;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class AeroGuidanceRecoveryTests
    {
        [Test]
        public void V2Identity_PresetsKeepBaseCd_AndFullTankBalance()
        {
            var cat = Golden.Catalog;
            foreach (var preset in cat.Presets)
            {
                Assert.IsFalse(AeroModel.HasV3Aero(preset), preset.id);
                Assert.AreEqual(LegacyBaseCd(preset), Stack.BaseCd(preset), 0, preset.id + " cd");
                var liftoff = Balance.Of(cat, preset);
                var full = Balance.Along(cat, preset, 1);
                Assert.AreEqual(liftoff.cg_m, full.cg_m, 0, preset.id + " cg");
                Assert.AreEqual(liftoff.cp_m, full.cp_m, 0, preset.id + " cp");
                Assert.AreEqual(liftoff.cpRaw_m, full.cpRaw_m, 0, preset.id + " cpRaw");
                Assert.AreEqual(liftoff.stabilityCal, full.stabilityCal, 0, preset.id + " S");
                Assert.AreEqual(liftoff.mass_kg, full.mass_kg, 0, preset.id + " mass");

                var stamped = preset.Clone();
                stamped.version = 3;
                stamped.guidance = new GuidanceSpec { maxQThrottleSet = true, maxQThrottle = 65, gLimitSet = true, gLimit = 4 };
                stamped.recoveryMode = "rtls";
                stamped.legsKind = "light";
                stamped.landingReserveSet = true;
                stamped.landingReserveFraction = 0.08;
                Assert.IsFalse(AeroModel.HasV3Aero(stamped), preset.id + " guidance is not aero");
                Assert.AreEqual(LegacyBaseCd(preset), Stack.BaseCd(stamped), 0, preset.id + " v3 non-aero");
            }
        }

        [Test]
        public void V2Identity_BuiltinGuidance_MatchesTheHeadlessKick()
        {
            var law = GuidanceProfile.Builtin();
            var rise = law.Command(new GuidanceState { vrel = 30, flightPathRad = Math.PI / 2, q = 40000, mass = 1e5, thrustN = 2e6 });
            Assert.AreEqual(Math.PI / 2, rise.pitchRad, 0);
            Assert.AreEqual(1, rise.throttle, 0, "no profile means full throttle");

            double kick = GuidanceProfile.DefaultKickDeg * Math.PI / 180.0;
            var hold = law.Command(new GuidanceState { vrel = 80, flightPathRad = Math.PI / 2 });
            Assert.AreEqual(Math.PI / 2 - kick, hold.pitchRad, 0);
            Assert.AreEqual(1, hold.throttle, 0);

            double caught = Math.PI / 2 - kick;
            var turn = law.Command(new GuidanceState { vrel = 80, flightPathRad = caught });
            Assert.AreEqual(caught, turn.pitchRad, 0, "once the flight path catches the kick, thrust follows it");
        }

        [Test]
        public void Gv9_FinlessPencil_StabilityIsNegative_AlongTheDrain()
        {
            Design pencil = null;
            double expect = 0;
            foreach (JsonObject c in Golden.File("designer-readout").Arr("cases"))
            {
                if (c.Str("name") != "cone-nofin") continue;
                pencil = Design.FromJson(c.Obj("design"));
                expect = c.Obj("out").Num("stabilityCal");
            }
            Assert.IsNotNull(pencil);
            Assert.IsFalse(pencil.fins);
            var cat = Golden.Catalog;
            var liftoff = Balance.Of(cat, pencil);
            Assert.Less(liftoff.stabilityCal, 0, "CP ahead of CG is the tumble sign");
            Assert.AreEqual(expect, liftoff.stabilityCal, 1e-9);
            var mid = Balance.Along(cat, pencil, 0.4, 20000, 0);
            Assert.Less(mid.cg_m, liftoff.cg_m, "draining the tank moves the CG aft");
            Assert.Less(mid.stabilityCal, 0, "still negative with 60% of the propellant gone");

            var held = pencil.Clone();
            held.stages[0].engineParams = new EngineParams { gimbalDegSet = true, gimbalDeg = 0 };
            var dead = Balance.Along(cat, held, 1, 20000, 0);
            Assert.Less(dead.stabilityCal, 0);
            Assert.AreEqual(0, dead.car, 0, "S < 0 and CAR < 1 is the tumble pair WP-E will raise");
            Assert.Less(dead.car, 1);
        }

        [Test]
        public void Cd0_UsesTheSpecBuildUp_OnlyWhenV3AeroFieldsArePresent()
        {
            double biconic = FairingCd("biconic");
            Assert.AreEqual(0.33, biconic, 0);
            var d = Golden.Catalog.Preset("scout").Clone();
            d.nose = "biconic";
            d.fins = false;
            Assert.IsTrue(AeroModel.HasV3Aero(d));
            Assert.AreEqual(biconic, AeroModel.NoseCd("biconic"), 0);
            Assert.AreEqual(biconic / AeroModel.NoseCd("ogive"), AeroModel.NoseHeatingFactor("biconic"), 0);
            Assert.AreEqual(biconic, Stack.BaseCd(d), 0, "nose only: fairing is not wider, no fins, no strap-ons");
            Assert.AreEqual(biconic, AeroModel.Cd(d, 0.5), 0);
            Assert.AreEqual(2 * biconic, AeroModel.Cd(d, Aero.M_PEAK), 1e-12);

            d.fairingDiameter = d.stages[d.stages.Count - 1].diameter * 2;
            double shoulder = AeroModel.FairingShoulderPerRatio * (2 - 1);
            Assert.AreEqual(biconic + shoulder, Stack.BaseCd(d), 1e-12);

            var finned = Golden.Catalog.Preset("scout").Clone();
            finned.finPlanform = "trapezoid";
            Assert.IsTrue(finned.fins);
            double span = 0.35, root = 0.4, tip = 0.6 * root, count = 4;
            double area = count * span * (root + tip) / 2;
            double dRef = Stack.ReferenceDiameter(finned);
            double sRef = Math.PI * dRef * dRef / 4;
            double cdFin = 0.02 * area / sRef;
            double nose = DesignMath.NoseShapes["ogive"];
            Assert.AreEqual(nose + cdFin, Stack.BaseCd(finned), 1e-12);
            Assert.AreEqual(8, AeroModel.FinMassKg(finned), 1e-9, "catalog geometry keeps the catalog mass");

            var grid = Golden.Catalog.Preset("stock").Clone();
            double legacy = LegacyBaseCd(grid);
            grid.finPlanform = "clipped-delta";
            grid.fins = false;
            Assert.AreEqual(legacy - 0.03 + AeroModel.GridStowedCd, Stack.BaseCd(grid), 1e-12);

            var truss = Golden.Catalog.Preset("stock").Clone();
            truss.stages[1].interstageShape = "truss";
            double drag = InterstageDrag("truss");
            Assert.AreEqual(0.04, drag, 0);
            Assert.AreEqual(legacy + drag - 0.03 + AeroModel.GridStowedCd, Stack.BaseCd(truss), 1e-12);
            Assert.AreEqual(AeroModel.GridDeployedCd, AeroModel.GridContribution(truss, true), 0);
        }

        [Test]
        public void Barrowman_SquareFin_MatchesTheClosedForm()
        {
            double s = 1.2, dRef = 2, radius = 1;
            int n = 4;
            double lf = s;
            double cna1 = (4.0 * n * (s / dRef) * (s / dRef)) / (1 + Math.Sqrt(1 + (2 * lf / (s + s)) * (2 * lf / (s + s))));
            double kfb = 1 + radius / (s + radius);
            Assert.AreEqual(kfb * cna1, AeroModel.FinCnAlpha(n, s, s, s, 0, dRef, radius), 0);
            Assert.AreEqual(0.55, Balance.NoseCpFactor("biconic"), 0);
            Assert.AreEqual(0.40, Balance.NoseCpFactor("hammerhead"), 0);
            Assert.AreEqual(1.0 / 3.0, Balance.NoseCpFactor("capsule-tower"), 0);
        }

        [Test]
        public void Car_IsControlOverAero_AtFourDegrees()
        {
            var cat = Golden.Catalog;
            var d = Golden.Catalog.Preset("scout").Clone();
            var low = Balance.Along(cat, d, 1, 10000, 0);
            var high = Balance.Along(cat, d, 1, 20000, 0);
            Assert.Greater(low.controlMoment, 0);
            Assert.AreEqual(low.aeroMoment * 2, high.aeroMoment, 1e-6);
            Assert.AreEqual(low.car / 2, high.car, 1e-9);
            Assert.IsTrue(double.IsPositiveInfinity(Balance.Along(cat, d, 1, 0, 0).car));

            var capped = d.Clone();
            capped.stages[0].engineParams = new EngineParams { gimbalDegSet = true, gimbalDeg = 20 };
            var atCap = d.Clone();
            atCap.stages[0].engineParams = new EngineParams { gimbalDegSet = true, gimbalDeg = cat.EngineById[d.stages[0].engine].gimbal };
            Assert.AreEqual(Balance.Along(cat, atCap, 1, 20000, 0).controlMoment, Balance.Along(cat, capped, 1, 20000, 0).controlMoment, 1e-6);

            var jets = d.Clone();
            jets.rcs = true;
            double pad = Balance.Along(cat, jets, 1, 20000, 0).car;
            double below = Balance.Along(cat, jets, 1, 20000, RecoveryModel.RcsAltitudeM).car;
            double above = Balance.Along(cat, jets, 1, 20000, RecoveryModel.RcsAltitudeM + 1).car;
            Assert.AreEqual(pad, below, 1e-12, "RCS is off at 60 km");
            Assert.Greater(above, below);
        }

        [Test]
        public void Guidance_IsDeterministic_AndAppliesTheProfile()
        {
            var profile = GuidanceProfile.From(new Design
            {
                guidance = new GuidanceSpec
                {
                    pitchKickAltSet = true, pitchKickAlt = 200,
                    pitchKickDegSet = true, pitchKickDeg = 5,
                    turnShapeSet = true, turnShape = 0.25,
                    maxQThrottleSet = true, maxQThrottle = 70,
                    gLimitSet = true, gLimit = 1.5,
                }
            }, 100000);
            var state = new GuidanceState
            {
                alt = 10100, vrel = 200, flightPathRad = 1.2, vr = 40, g = 9.2,
                q = 18000, mass = 10000, thrustN = 200000, throttleMin = 0.4, stageIndex = 0,
            };
            var first = profile.Command(state);
            for (int i = 0; i < 20; i++)
            {
                var again = profile.Command(state);
                Assert.AreEqual(first.pitchRad, again.pitchRad, 0);
                Assert.AreEqual(first.throttle, again.throttle, 0);
            }

            var below = profile.Command(new GuidanceState { alt = 100, vrel = 500, flightPathRad = 0.2 });
            Assert.AreEqual(Math.PI / 2, below.pitchRad, 0, "the kick waits for altitude");

            double bucket = GuidanceProfile.MaxQReferencePa * GuidanceProfile.MaxQBucketFraction;
            var inBucket = profile.Command(new GuidanceState { alt = 10100, q = bucket, mass = 1e6, thrustN = 1 });
            Assert.AreEqual(0.70, inBucket.throttle, 0);
            var under = profile.Command(new GuidanceState { alt = 10100, q = bucket - 1, mass = 1e6, thrustN = 1 });
            Assert.AreEqual(1, under.throttle, 0);

            double gCap = 1.5 * Atmosphere.G0 * 10000 / 200000;
            Assert.Greater(gCap, 0.70);
            Assert.AreEqual(0.70, first.throttle, 1e-12, "the bucket is tighter than this g-limit");
            var heavy = profile.Command(new GuidanceState { alt = 10100, q = 0, mass = 10000, thrustN = 200000 });
            // gLimit lives on the profile, not the state. 1.5 g on this thrust is under 1 and over the bucket.
            Assert.AreEqual(gCap, heavy.throttle, 1e-12);

            var floored = GuidanceProfile.From(new Design
            {
                guidance = new GuidanceSpec { gLimitSet = true, gLimit = 1.0 }
            }, 100000);
            double want = 1.0 * Atmosphere.G0 * 10000 / 200000;
            Assert.Less(want, 0.6);
            Assert.AreEqual(0.6, floored.Command(new GuidanceState { mass = 10000, thrustN = 200000, throttleMin = 0.6 }).throttle, 1e-12);

            var steep = GuidanceProfile.From(new Design
            {
                guidance = new GuidanceSpec { pitchKickAltSet = true, pitchKickAlt = 200, turnShapeSet = true, turnShape = 0 }
            }, 100000);
            var linear = GuidanceProfile.From(new Design
            {
                guidance = new GuidanceSpec { pitchKickAltSet = true, pitchKickAlt = 200, turnShapeSet = true, turnShape = 1 }
            }, 100000);
            var mid = new GuidanceState { alt = 10100, vrel = 100, flightPathRad = 1 };
            Assert.Greater(steep.Command(mid).pitchRad, linear.Command(mid).pitchRad);

            double aAxial = 300000.0 / 10000;
            var peg = GuidanceProfile.Builtin(400000).Command(new GuidanceState
            {
                stageIndex = 1, alt = 80000, vr = 200, g = 9.5, mass = 10000, thrustN = 300000,
            });
            Assert.AreEqual(1, peg.throttle, 0);
            Assert.AreEqual(GuidanceProfile.PegLitePitch(80000, 400000, 200, 9.5, aAxial), peg.pitchRad, 0);
            double vrCmd = 250;
            double sinP = (9.5 + 0.03 * (vrCmd - 200)) / aAxial;
            Assert.AreEqual(Math.Asin(sinP), peg.pitchRad, 1e-12);
        }

        [Test]
        public void Recovery_LandingBoostbackLegs_AndSeparation()
        {
            double g = Atmosphere.G0;
            double need = 100 + g * 5 * (1 - 1.0 / 2);
            Assert.AreEqual(need, RecoveryModel.LandingDeltaV(100, g, 5, 2), 0);
            Assert.AreEqual(2000, RecoveryModel.BoostbackDeltaV("rtls", 1000), 0);
            Assert.AreEqual(300, RecoveryModel.BoostbackDeltaV("barge", -1000), 0);
            Assert.AreEqual(0, RecoveryModel.BoostbackDeltaV("expendable", 1000), 0);
            Assert.AreEqual(RecoveryModel.LightVLeg, RecoveryModel.VLeg(null), 0);
            Assert.AreEqual(RecoveryModel.HeavyVLeg, RecoveryModel.VLeg("heavy"), 0);
            Assert.AreEqual(0.06, RecoveryModel.LegDryFraction(null), 0);
            Assert.AreEqual(0.09, RecoveryModel.LegDryFraction("heavy"), 0);
            Assert.IsFalse(RecoveryModel.LegCollapse(2, "light"));
            Assert.IsTrue(RecoveryModel.LegCollapse(2.01, "light"));
            Assert.IsFalse(RecoveryModel.LegCollapse(4, "heavy"));
            Assert.IsTrue(RecoveryModel.LegCollapse(4.01, "heavy"));

            var heavy = Golden.Catalog.Preset("stock").Clone();
            heavy.legsKind = "heavy";
            Assert.Greater(Balance.Of(Golden.Catalog, heavy).mass_kg, Balance.Of(Golden.Catalog, Golden.Catalog.Preset("stock")).mass_kg);

            double dv = RecoveryModel.ReserveDeltaV(300, 10000, 5000);
            Assert.AreEqual(300 * g * Math.Log(1.5), dv, 0);
            Assert.IsTrue(RecoveryModel.ReserveShort(dv, need * 20));
            Assert.IsFalse(RecoveryModel.ReserveShort(dv, 1));

            Assert.AreEqual(0.7, RecoveryModel.MinRelSpeed(10), 0);
            Assert.AreEqual(700 * (1.0 / 1000 + 1.0 / 1000), RecoveryModel.SeparationRelSpeed(700, 1000, 1000), 0);
            Assert.AreEqual(1.4, RecoveryModel.SeparationRelSpeed(700, 1000, 1000), 1e-12);
            Assert.IsTrue(RecoveryModel.SeparationCollision(0.699, 10, 2));
            Assert.IsFalse(RecoveryModel.SeparationCollision(0.7, 10, 2));
            Assert.IsFalse(RecoveryModel.SeparationCollision(0.1, 10, 2.01));
            Assert.IsTrue(RecoveryModel.HotStagingOverpressure("hot", "straight"));
            Assert.IsFalse(RecoveryModel.HotStagingOverpressure("hot", "hot-staging"));
            Assert.IsFalse(RecoveryModel.HotStagingOverpressure("cold", "straight"));
            Assert.AreEqual(1, RecoveryModel.BoosterSepFraction(new Design()), 0);
            var early = new Design { boosterSepSet = true, boosterSepPct = 85 };
            Assert.AreEqual(0.85, RecoveryModel.BoosterSepFraction(early), 0);
        }

        static double LegacyBaseCd(Design d)
        {
            double cd = DesignMath.NoseShapes.TryGetValue(d.nose ?? "", out var n) ? n : 0.32;
            if (d.fins) cd += 0.04;
            if (d.gridFins) cd += 0.03;
            if (d.StrapOnCount > 0) cd += 0.03 * d.StrapOnCount;
            var top = d.stages[d.stages.Count - 1];
            if (d.fairingDiameter > top.diameter) cd += 0.08 * (d.fairingDiameter / top.diameter - 1);
            cd += InterstageAero.DragCd(d);
            return cd;
        }

        static double FairingCd(string shape)
        {
            foreach (JsonObject o in MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "fairings.json"))).Arr("fairings"))
                if (o.Str("shape") == shape) return o.Num("cd");
            Assert.Fail(shape);
            return 0;
        }

        static double InterstageDrag(string shape)
        {
            foreach (JsonObject o in MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "interstages.json"))).Arr("interstages"))
                if (o.Str("shape") == shape) return o.Num("dragDeltaCd");
            Assert.Fail(shape);
            return 0;
        }
    }
}

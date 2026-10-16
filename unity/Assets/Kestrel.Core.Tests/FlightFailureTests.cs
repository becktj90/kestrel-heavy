// Cause id → effect, callout, and the post-flight number line. The picture is a
// function of the failure event and age, so a second pass matches the first.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FlightFailureTests
    {
        [Test]
        public void EachCauseMapsToItsEffectCalloutAndReport()
        {
            var rows = new[]
            {
                Make(FailureKind.EngineOut, 0, 1, 7, 2, 48, FlightFailure.SputterCut, "ENGINE OUT", "rud-no"),
                Make(FailureKind.EngineOverpressure, 1, 80, 60, 0, 12, FlightFailure.Fireball, "RUD", "clip"),
                Make(FailureKind.StructuralMaxQ, 0, 41000, 33000, -1, 62, FlightFailure.Shear, "BREAKUP", "proc"),
                Make(FailureKind.Tumble, 1, 22, 15, -1, 30, FlightFailure.TumbleSpin, "TUMBLE", "clip"),
                Make(FailureKind.SeparationCollision, 0, 1.2, 2.5, -1, 80, FlightFailure.Recontact, "RECONTACT", "clip"),
                Make(FailureKind.PropDepletion, 0, 40, 0, -1, 40, FlightFailure.DryCutoff, "FUEL OUT", "proc"),
            };
            var effects = new HashSet<string>();
            var sounds = new HashSet<string>();
            var waves = new HashSet<int>();
            var prints = new HashSet<int>();
            foreach (var row in rows)
            {
                var ev = Explain(row.Kind, row.Cause, row.Value, row.Limit, row.Engine, row.T);
                var cue = FlightFailure.CueOf(ev);
                Assert.IsTrue(cue.Live, ev.kind);
                Assert.AreEqual(ev.cause, cue.CauseId, ev.kind);
                Assert.AreEqual(ev.kind, cue.Kind);
                Assert.AreEqual(row.Effect, cue.EffectId, ev.cause);
                Assert.AreEqual(FlightDebrief.NumberLine(ev, null, false), cue.ReportText, ev.cause);
                Assert.AreEqual(FlightDebrief.TitleOf(ev.kind), cue.ReportTitle, ev.cause);
                Assert.That(cue.Callout, Does.Contain(cue.ReportText));
                Assert.That(cue.Callout, Does.StartWith(row.Title));
                Assert.AreEqual(row.Effect, cue.SoundId, "the sound id is the effect id");
                var tone = FlightFailure.ToneOf(cue.EffectId);
                Assert.AreEqual(cue.SoundId, tone.SoundId);
                Assert.AreEqual(cue.Wave, tone.Wave);
                if (row.Clip == "clip") Assert.IsNotEmpty(cue.Clip, ev.cause);
                else Assert.IsEmpty(cue.Clip, ev.cause);
                effects.Add(cue.EffectId);
                sounds.Add(cue.SoundId);
                waves.Add(cue.Wave);
                prints.Add(FlightFailure.Fingerprint(cue.EffectId));
            }
            Assert.AreEqual(6, effects.Count, "each of the six causes has its own picture");
            Assert.AreEqual(6, sounds.Count);
            Assert.AreEqual(6, waves.Count);
            Assert.AreEqual(6, prints.Count, "the synthesized cues do not share a waveform");
        }

        [Test]
        public void SiblingCausesShareThePictureAndKeepTheirOwnNumbers()
        {
            var cap = Explain(FailureKind.EngineOverpressure, 1, 80, 60, 0, 12);
            var hazard = Explain(FailureKind.EngineOverpressure, 0, 70, 60, 3, 18);
            var q = Explain(FailureKind.StructuralMaxQ, 0, 41000, 33000, -1, 62);
            var margin = Explain(FailureKind.StructuralMaxQ, 1, -0.2, 0, -1, 40);
            var hot = Explain(FailureKind.StructuralMaxQ, 2, 1, 0, -1, 70);
            var bulk = Explain(FailureKind.StructuralMaxQ, 3, 1, 0, -1, 0);
            var alpha = Explain(FailureKind.Tumble, 1, 22, 15, -1, 30);
            var stability = Explain(FailureKind.Tumble, 0, -0.4, 0, -1, 33);

            Assert.AreEqual("pc-cap", FlightFailure.CueOf(cap).CauseId);
            Assert.AreEqual("hazard", FlightFailure.CueOf(hazard).CauseId);
            Assert.AreEqual(FlightFailure.Fireball, FlightFailure.CueOf(cap).EffectId);
            Assert.AreEqual(FlightFailure.Fireball, FlightFailure.CueOf(hazard).EffectId);
            Assert.AreNotEqual(FlightFailure.CueOf(cap).ReportText, FlightFailure.CueOf(hazard).ReportText);

            Assert.AreEqual("q", FlightFailure.CueOf(q).CauseId);
            Assert.AreEqual("margin", FlightFailure.CueOf(margin).CauseId);
            Assert.AreEqual("hot-staging", FlightFailure.CueOf(hot).CauseId);
            Assert.AreEqual("common-bulkhead", FlightFailure.CueOf(bulk).CauseId);
            foreach (var ev in new[] { q, margin, hot, bulk })
            {
                var cue = FlightFailure.CueOf(ev);
                Assert.AreEqual(FlightFailure.Shear, cue.EffectId, ev.cause);
                Assert.AreEqual(FlightDebrief.NumberLine(ev, null, false), cue.ReportText);
                Assert.That(cue.Callout, Does.Contain(cue.ReportText));
            }
            Assert.AreNotEqual(FlightFailure.CueOf(q).ReportText, FlightFailure.CueOf(margin).ReportText);

            Assert.AreEqual("alpha", FlightFailure.CueOf(alpha).CauseId);
            Assert.AreEqual("stability", FlightFailure.CueOf(stability).CauseId);
            Assert.AreEqual(FlightFailure.TumbleSpin, FlightFailure.CueOf(alpha).EffectId);
            Assert.AreEqual(FlightFailure.TumbleSpin, FlightFailure.CueOf(stability).EffectId);
            Assert.That(FlightFailure.CueOf(alpha).Callout, Does.Contain("22"));
            Assert.That(FlightFailure.CueOf(stability).Callout, Does.Contain("-0.40"));
        }

        [Test]
        public void FuelCalloutKeepsTheReportShortfallWhenTheFactsHaveIt()
        {
            var ev = Explain(FailureKind.PropDepletion, 0, 40, 0, -1, 40);
            var facts = new FlightFacts { Margin = -0.04, RequiredMps = 2500 };
            var cue = FlightFailure.CueOf(ev, facts);
            string report = FlightDebrief.NumberLine(ev, facts, false);
            Assert.AreEqual("prop-depletion", cue.CauseId);
            Assert.AreEqual(report, cue.ReportText);
            Assert.That(cue.Callout, Does.Contain("Out of fuel at T+40 s."));
            Assert.That(cue.Callout, Does.Contain("Short 100 m/s."));
            Assert.That(report, Does.Contain("Short 100 m/s."));
        }

        [Test]
        public void PicturesFollowTheCauseAndRepeat()
        {
            var engine = FlightFailure.CueOf(Explain(FailureKind.EngineOut, 0, 1, 9, 4, 20));
            var early = FlightFailure.At(engine, 0.03, false);
            var lateGate = FlightFailure.At(engine, 0.09, false);
            var cut = FlightFailure.At(engine, 0.60, false);
            var again = FlightFailure.At(engine, 0.03, false);
            Assert.AreEqual(FlightFailure.SputterGate(0.03), early.PlumeScale, 1e-6);
            Assert.Greater(early.PlumeScale, 0.5f);
            Assert.Less(lateGate.PlumeScale, 0.1f);
            Assert.AreEqual(0.03f, early.SputterAge, 1e-5);
            Assert.AreEqual(0f, cut.PlumeScale);
            Assert.Less(cut.SputterAge, 0f);
            Assert.AreEqual(0, cut.Debris);
            Assert.AreEqual(early.PlumeScale, again.PlumeScale, 1e-6);
            var snapped = FlightFailure.At(engine, 0.1, true);
            Assert.AreEqual(0f, snapped.PlumeScale);
            Assert.IsTrue(snapped.Visible);

            var rud = FlightFailure.CueOf(Explain(FailureKind.EngineOverpressure, 1, 80, 60, 2, 12));
            var boom = FlightFailure.At(rud, 0.25, false);
            var boom2 = FlightFailure.At(rud, 0.25, false);
            Assert.AreEqual(0f, boom.PlumeScale);
            Assert.Greater(boom.Fireball, 2f);
            Assert.AreEqual(FlightFailure.FireballPieces, boom.Debris);
            Assert.Greater(boom.Heat, 0.4f);
            var bit = FlightFailure.BitAt(rud.EffectId, 0, boom.Debris, 0.25, false, rud.EngineIndex);
            var bitB = FlightFailure.BitAt(rud.EffectId, 0, boom.Debris, 0.25, false, rud.EngineIndex);
            Assert.Greater(Math.Abs(bit.X) + Math.Abs(bit.Z), 1f);
            Assert.AreEqual(bit.X, bitB.X, 1e-5);
            Assert.AreEqual(bit.Spin, bitB.Spin, 1e-5);
            Assert.AreEqual(boom.Fireball, boom2.Fireball, 1e-6);

            var bend = FlightFailure.CueOf(Explain(FailureKind.StructuralMaxQ, 0, 41000, 33000, -1, 62));
            var shut = FlightFailure.At(bend, 0.05, false);
            var open = FlightFailure.At(bend, 0.50, false);
            Assert.Less(shut.ShearGap, 1f);
            Assert.Greater(open.ShearGap, 5f);
            Assert.Greater(open.ShearDeg, 30f);
            Assert.AreEqual(0f, open.PlumeScale);
            Assert.AreEqual(FlightFailure.ShearPieces, open.Debris);

            var flip = FlightFailure.CueOf(Explain(FailureKind.Tumble, 1, 22, 15, -1, 30));
            var spin = FlightFailure.At(flip, 0.5, false);
            Assert.AreEqual(0.5f * FlightFailure.TumbleDegPerSec, spin.TumbleDeg, 1e-4);
            Assert.Greater(spin.PlumeScale, 0.2f, "a tumble keeps a crooked plume");
            Assert.AreEqual(0, spin.Debris);
            Assert.AreEqual(90f, FlightFailure.At(flip, 0.1, true).TumbleDeg);

            var bump = FlightFailure.CueOf(Explain(FailureKind.SeparationCollision, 0, 1.2, 2.5, -1, 80));
            Assert.AreEqual(FlightFailure.RecontactStartM, FlightFailure.At(bump, 0, false).RecontactM, 1e-4);
            Assert.AreEqual(0f, FlightFailure.At(bump, FlightFailure.RecontactCloseSec, false).RecontactM, 1e-4);
            var drifted = FlightFailure.At(bump, 0.7, false);
            Assert.Greater(drifted.RecontactM, 0.4f);
            Assert.Less(drifted.RecontactM, 2f, "recontact does not become a clean separation");
            Assert.AreEqual(0f, FlightFailure.At(bump, 0.2, true).RecontactM);

            var dry = FlightFailure.CueOf(Explain(FailureKind.PropDepletion, 0, 40, 0, -1, 40));
            var full = FlightFailure.At(dry, 0, false);
            var half = FlightFailure.At(dry, 0.20, false);
            var empty = FlightFailure.At(dry, 0.50, false);
            Assert.AreEqual(1f, full.PlumeScale, 1e-4);
            Assert.AreEqual(FlightFailure.DryScale(0.20, false), half.PlumeScale, 1e-4);
            Assert.Less(half.PlumeScale, full.PlumeScale);
            Assert.AreEqual(0f, empty.PlumeScale);
            Assert.Less(empty.SputterAge, 0f, "a dry cutoff does not sputter");
            Assert.AreEqual(0, empty.Debris);
            Assert.AreEqual(0f, empty.Fireball);
            Assert.AreEqual(0f, FlightFailure.At(dry, 0.1, true).PlumeScale);
        }

        [Test]
        public void OtherCausesStillDraw()
        {
            var over = FlightFailure.CueOf(Explain(FailureKind.OverG, 0, 12.5, 8, -1, 50));
            var mix = FlightFailure.CueOf(Explain(FailureKind.BadMixture, 0, 9, 6, 1, 22));
            var heat = FlightFailure.CueOf(Explain(FailureKind.Thermal, 0, 1400, 900, -1, 90));
            Assert.AreEqual("overg", over.CauseId);
            Assert.AreEqual(FlightFailure.Shear, over.EffectId);
            Assert.Greater(FlightFailure.At(over, 0.4, false).ShearGap, 2f);
            Assert.AreEqual(FlightFailure.SputterCut, mix.EffectId);
            Assert.AreEqual("bad-mixture", mix.CauseId);
            Assert.AreEqual(FlightFailure.Burst, heat.EffectId);
            Assert.Greater(FlightFailure.At(heat, 0.3, false).Debris, 0);
            Assert.That(over.Callout, Does.Contain(over.ReportText));
        }

        [Test]
        public void CueFollowsTheFailureSimEvent()
        {
            var ev = Explain(FailureKind.EngineOut, 0, 1, 7, 2, 48);
            var sim = new SimEvent { t = ev.t, type = "failure", stage = 1 };
            Assert.IsTrue(FlightFailure.Matches(ev, sim));
            Assert.IsFalse(FlightFailure.Matches(ev, new SimEvent { t = ev.t, type = "meco" }));
            Assert.IsFalse(FlightFailure.Matches(ev, new SimEvent { t = ev.t + 5, type = "failure" }));
            Assert.AreEqual(ev.t, FlightFailure.EventTime(new List<SimEvent> { sim }, ev), 1e-9);
            Assert.AreEqual(ev.t, FlightFailure.EventTime(null, ev), 1e-9);
            var cue = FlightFailure.CueOf(ev);
            Assert.AreEqual(ev.t, cue.T, 1e-9);
            Assert.AreEqual(ev.cause, cue.CauseId);
        }

        struct Row
        {
            public int Kind, Cause, Engine;
            public double Value, Limit, T;
            public string Effect, Title, Clip;
        }

        static Row Make(int kind, int cause, double value, double limit, int engine, double t, string effect, string title, string clip)
        {
            return new Row
            {
                Kind = kind, Cause = cause, Value = value, Limit = limit, Engine = engine, T = t,
                Effect = effect, Title = title, Clip = clip,
            };
        }

        static FailureEvent Explain(int kind, int cause, double value, double limit, int engine, double t)
        {
            double q = kind == FailureKind.StructuralMaxQ && cause == 0 ? value : 0;
            return FailureModel.Explain(new FailureHit
            {
                on = true,
                kind = kind,
                cause = cause,
                value = value,
                limit = limit,
                engine = engine,
                stage = 1,
            }, t, q);
        }
    }
}

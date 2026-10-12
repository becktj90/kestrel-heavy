using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Vehicle;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    public class HangarExplodeTests
    {
        static Catalog Cat()
        {
            var c = new Catalog();
            var p = new Propellant { id = "kerolox", bulkDensity = 800, storability = "cryo" };
            c.Propellants.Add(p); c.PropellantById[p.id] = p;
            var e = new Engine { id = "eng", label = "Kestrel", diameter = 1.2 };
            c.Engines.Add(e); c.EngineById[e.id] = e;
            return c;
        }

        static StagePart Stage(string name, int engines, double propKg, double diam) => new StagePart
        {
            name = name, propellant = "kerolox", engine = "eng", engineCount = engines, propMass = propKg, diameter = diam
        };

        static Design Vehicle(int engines = 1, double prop = 40000) => new Design
        {
            stages = new List<StagePart> { Stage("Core", engines, prop, 3.6), Stage("Upper", 1, 8000, 2.4) },
            fairingDiameter = 2.4, fairingLength = 8, nose = "ogive"
        };

        static List<HangarExplode.Part> Lay(Design d) { var list = new List<HangarExplode.Part>(); HangarExplode.Layout(Cat(), d, list); return list; }

        static HangarExplode.Part One(List<HangarExplode.Part> parts, HangarPartKind kind, int stage, bool strap = false)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (p.Kind == kind && p.StageIndex == stage && p.StrapOn == strap) return p;
            }
            Assert.Fail("missing " + kind + " " + stage);
            return default;
        }

        static int Count(List<HangarExplode.Part> parts, HangarPartKind kind)
        {
            int n = 0;
            for (int i = 0; i < parts.Count; i++) if (parts[i].Kind == kind) n++;
            return n;
        }

        [Test]
        public void PartsFollowTheDesign()
        {
            var parts = Lay(Vehicle());
            Assert.That(Count(parts, HangarPartKind.Stage), Is.EqualTo(2));
            Assert.That(Count(parts, HangarPartKind.Interstage), Is.EqualTo(1));
            Assert.That(Count(parts, HangarPartKind.Engine), Is.EqualTo(2));
            Assert.That(Count(parts, HangarPartKind.Fairing), Is.EqualTo(1));
            Assert.That(Count(parts, HangarPartKind.Booster), Is.EqualTo(0));

            var one = Vehicle();
            one.stages.RemoveAt(1);
            var single = Lay(one);
            Assert.That(Count(single, HangarPartKind.Interstage), Is.EqualTo(0));
            Assert.That(Count(single, HangarPartKind.Stage), Is.EqualTo(1));

            var three = Vehicle();
            three.stages.Add(Stage("Kick", 1, 1500, 1.6));
            Assert.That(Count(Lay(three), HangarPartKind.Interstage), Is.EqualTo(2));
            Assert.That(Count(Lay(three), HangarPartKind.Stage), Is.EqualTo(3));
        }

        [Test]
        public void EngineCountComesFromTheStage()
        {
            Assert.That(Count(Lay(Vehicle(1)), HangarPartKind.Engine), Is.EqualTo(2));
            Assert.That(Count(Lay(Vehicle(9)), HangarPartKind.Engine), Is.EqualTo(10));
            var nine = Lay(Vehicle(9));
            float lift = 0f; bool seen = false; int offAxis = 0;
            for (int i = 0; i < nine.Count; i++)
            {
                var p = nine[i];
                if (p.Kind != HangarPartKind.Engine || p.StageIndex != 0) continue;
                if (!seen) { lift = p.ExplodeLift; seen = true; }
                else Assert.That(p.ExplodeLift, Is.EqualTo(lift).Within(1e-4f));
                if (p.RestLocal.x * p.RestLocal.x + p.RestLocal.z * p.RestLocal.z > 1e-4f) offAxis++;
            }
            Assert.That(seen, Is.True);
            Assert.That(offAxis, Is.GreaterThan(0));
            var single = One(Lay(Vehicle(1)), HangarPartKind.Engine, 0);
            Assert.That(single.RestLocal.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(single.RestLocal.z, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void GapIsFifteenPercentOfThatStagesHeight()
        {
            var parts = Lay(Vehicle());
            var engines = One(parts, HangarPartKind.Engine, 0);
            var body = One(parts, HangarPartKind.Stage, 0);
            var inter = One(parts, HangarPartKind.Interstage, 0);
            var upper = One(parts, HangarPartKind.Stage, 1);
            var upperEng = One(parts, HangarPartKind.Engine, 1);
            Assert.That(body.ExplodeLift - engines.ExplodeLift, Is.EqualTo(HangarExplode.GapFraction * body.StageHeight).Within(1e-3f));
            Assert.That(inter.ExplodeLift - body.ExplodeLift, Is.EqualTo(HangarExplode.GapFraction * body.StageHeight).Within(1e-3f));
            Assert.That(upper.ExplodeLift - upperEng.ExplodeLift, Is.EqualTo(HangarExplode.GapFraction * upper.StageHeight).Within(1e-3f));
            Assert.That(body.StageHeight, Is.GreaterThan(1f));
        }

        [Test]
        public void MorePropellantOpensALargerGap()
        {
            var light = One(Lay(Vehicle(1, 20000)), HangarPartKind.Stage, 0);
            var heavy = One(Lay(Vehicle(1, 80000)), HangarPartKind.Stage, 0);
            Assert.That(heavy.StageHeight, Is.GreaterThan(light.StageHeight));
            Assert.That(heavy.ExplodeLift, Is.GreaterThan(light.ExplodeLift));
            Assert.That(heavy.AxialLength, Is.GreaterThan(light.AxialLength));
        }

        [Test]
        public void ExplodeMovesAlongTheAxisOnly()
        {
            var parts = Lay(Vehicle());
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                var closed = HangarExplode.Position(p, 0f);
                var open = HangarExplode.Position(p, 1f);
                Assert.That(closed.x, Is.EqualTo(p.RestLocal.x).Within(1e-4f));
                Assert.That(closed.y, Is.EqualTo(p.RestLocal.y).Within(1e-4f));
                Assert.That(closed.z, Is.EqualTo(p.RestLocal.z).Within(1e-4f));
                Assert.That(open.x, Is.EqualTo(p.RestLocal.x).Within(1e-4f));
                Assert.That(open.z, Is.EqualTo(p.RestLocal.z).Within(1e-4f));
                if (p.ExplodeLift > 0f) Assert.That(open.y, Is.EqualTo(p.RestLocal.y + p.ExplodeLift).Within(1e-3f));
            }
            var fairing = One(parts, HangarPartKind.Fairing, -1);
            for (int i = 0; i < parts.Count; i++)
                Assert.That(fairing.ExplodeLift, Is.GreaterThanOrEqualTo(parts[i].ExplodeLift));
        }

        [Test]
        public void BoostersComeFromStrapOnCountAndStayOffTheAxis()
        {
            var d = Vehicle();
            d.strapOns = Stage("Gem", 2, 12000, 1.8);
            d.strapOns.count = 4;
            var parts = Lay(d);
            Assert.That(Count(parts, HangarPartKind.Booster), Is.EqualTo(4));
            int engines = 0;
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (!p.StrapOn || p.Kind != HangarPartKind.Engine) continue;
                engines++;
                float r = p.RestLocal.x * p.RestLocal.x + p.RestLocal.z * p.RestLocal.z;
                Assert.That(r, Is.GreaterThan(0.2f));
            }
            Assert.That(engines, Is.EqualTo(8));
            var body = One(parts, HangarPartKind.Booster, 0, true);
            var eng = One(parts, HangarPartKind.Engine, 0, true);
            Assert.That(body.ExplodeLift - eng.ExplodeLift, Is.EqualTo(HangarExplode.GapFraction * body.StageHeight).Within(1e-3f));
            Assert.That(body.Label, Is.EqualTo("Gem 1"));
        }

        [Test]
        public void LabelsComeFromTheModel()
        {
            var d = Vehicle();
            var core = One(Lay(d), HangarPartKind.Stage, 0);
            Assert.That(core.Label, Is.EqualTo("Core"));
            var eng = One(Lay(d), HangarPartKind.Engine, 0);
            Assert.That(eng.Label, Is.EqualTo("Kestrel 1"));
            d.stages[0].name = "";
            Assert.That(One(Lay(d), HangarPartKind.Stage, 0).Label, Is.EqualTo("Stage 1"));
            d.stages[0].name = "Tank A";
            Assert.That(One(Lay(d), HangarPartKind.Stage, 0).Label, Is.EqualTo("Tank A"));
            Assert.That(One(Lay(d), HangarPartKind.Interstage, 0).Label, Is.EqualTo("Interstage 1"));
        }

        [Test]
        public void CalloutsTrackTheExplodedPart()
        {
            var parts = Lay(Vehicle());
            var calls = new List<HangarExplode.Callout>();
            HangarExplode.Callouts(parts, 1f, calls);
            Assert.That(calls.Count, Is.EqualTo(parts.Count));
            var body = One(parts, HangarPartKind.Stage, 0);
            HangarExplode.Callout call = default;
            bool found = false;
            for (int i = 0; i < calls.Count; i++) if (calls[i].PartId == body.Id) { call = calls[i]; found = true; }
            Assert.That(found, Is.True);
            Assert.That(call.Label, Is.EqualTo("Core"));
            Assert.That(call.Anchor.y, Is.EqualTo(body.RestLocal.y + body.ExplodeLift + body.AxialLength * 0.5f).Within(1e-3f));
            Assert.That(Mathf.Abs(call.End.x), Is.GreaterThan(Mathf.Abs(call.Anchor.x) + 0.2f));
            Assert.That(call.End.y, Is.EqualTo(call.Anchor.y).Within(1e-4f));
        }

        [Test]
        public void ReduceMotionFinishesImmediatelyAndTheEaseIs400ms()
        {
            Assert.That(HangarExplode.Progress(0f, true), Is.EqualTo(1f));
            Assert.That(HangarExplode.Progress(0f, false), Is.EqualTo(0f));
            Assert.That(HangarExplode.Progress(HangarExplode.Seconds * 0.5f, false), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(HangarExplode.Progress(HangarExplode.Seconds, false), Is.EqualTo(1f));
            Assert.That(HangarExplode.Seconds, Is.EqualTo(0.4f));
        }

        [Test]
        public void IdsAreUnique()
        {
            var d = Vehicle(5);
            d.strapOns = Stage(null, 3, 9000, 1.5);
            d.strapOns.count = 2;
            var parts = Lay(d);
            var ids = new HashSet<string>();
            for (int i = 0; i < parts.Count; i++)
                Assert.That(ids.Add(parts[i].Id), Is.True, parts[i].Id);
        }
    }
}

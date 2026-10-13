// Exploded Hangar layout. Parts, gaps and labels come from the design and the catalog
// (tank length via DesignMath, bells and interstages the same sizes as RocketMeshBuilder).
// Separation is along +Y, 0.15 × the owning stage's height. Nothing is a fixed vehicle.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Vehicle;
using UnityEngine;

namespace Kestrel.Slice
{
    public enum HangarPartKind { Engine, Stage, Interstage, Booster, Fairing }

    public static class HangarExplode
    {
        public const float GapFraction = 0.15f;
        public const float Seconds = 0.4f;

        public readonly struct Part
        {
            public readonly string Id, Label;
            public readonly HangarPartKind Kind;
            public readonly int Order, StageIndex;
            public readonly bool StrapOn;
            public readonly Vector3 RestLocal;
            public readonly float AxialLength, Radius, TopRadius, NoseFraction, StageHeight, ExplodeLift;
            public readonly string Cycle;
            public readonly float Expansion;

            public Part(string id, string label, HangarPartKind kind, int order, int stageIndex, bool strapOn,
                Vector3 rest, float axial, float radius, float topRadius, float noseFraction, float stageHeight, float lift,
                string cycle = null, float expansion = 0f)
            {
                Id = id; Label = label; Kind = kind; Order = order; StageIndex = stageIndex; StrapOn = strapOn;
                RestLocal = rest; AxialLength = axial; Radius = radius; TopRadius = topRadius;
                NoseFraction = noseFraction; StageHeight = stageHeight; ExplodeLift = lift;
                Cycle = cycle; Expansion = expansion;
            }

            public Part At(int order, float lift) => new Part(Id, Label, Kind, order, StageIndex, StrapOn,
                RestLocal, AxialLength, Radius, TopRadius, NoseFraction, StageHeight, lift, Cycle, Expansion);
        }

        public readonly struct Callout
        {
            public readonly string PartId, Label;
            public readonly Vector3 Anchor, End;
            public Callout(string id, string label, Vector3 anchor, Vector3 end)
            { PartId = id; Label = label; Anchor = anchor; End = end; }
        }

        /// <summary>Smoothstep 0→1 over <see cref="Seconds"/>. Reduce Motion is already finished.</summary>
        public static float Progress(float elapsed, bool reduceMotion)
        {
            if (reduceMotion) return 1f;
            if (!(elapsed > 0f) || float.IsNaN(elapsed)) return 0f;
            float u = Mathf.Clamp01(elapsed / Seconds);
            return u * u * (3f - 2f * u);
        }

        public static Vector3 Position(Part part, float t)
        {
            float u = float.IsNaN(t) ? 0f : Mathf.Clamp01(t);
            return part.RestLocal + new Vector3(0f, part.ExplodeLift * u, 0f);
        }

        public static void Layout(Catalog c, Design d, List<Part> into)
        {
            into.Clear();
            if (c == null || d == null || d.stages == null || d.stages.Count == 0) return;
            int order = 0;
            float lift = 0f;
            var spots = new List<Vector2>(16);

            void Add(Part p) => into.Add(p.At(order, lift));
            void EndGroup(float stageHeight, int before)
            {
                if (into.Count == before) return;
                lift += GapFraction * Mathf.Max(0.05f, stageHeight);
                order++;
            }

            if (d.strapOns != null && d.strapOns.count > 0)
            {
                var so = d.strapOns;
                EngineOf(c, so, out var bEng);
                float bBell = Bell(bEng, (float)so.diameter, so.engineCount);
                float bR = Mathf.Max(0.05f, (float)so.diameter * 0.5f);
                float coreR = Mathf.Max(0.05f, (float)d.stages[0].diameter * 0.5f);
                float bTank = Tank(c, so);
                float bNose = 2.2f * bR;
                float bH = bBell + bTank + bNose;
                float bNoseFrac = bNose / Mathf.Max(0.05f, bTank + bNose);
                int nBoost = Mathf.Max(0, so.count);
                int before = into.Count;
                for (int k = 0; k < nBoost; k++)
                {
                    var radial = Radial(k, nBoost, coreR + bR + 0.1f);
                    AddEngines(so, bEng, k, true, radial, 0f, bBell, bH, spots, Add);
                }
                EndGroup(bH, before);
                before = into.Count;
                for (int k = 0; k < nBoost; k++)
                {
                    var radial = Radial(k, nBoost, coreR + bR + 0.1f);
                    string label = string.IsNullOrEmpty(so.name) ? "Booster " + (k + 1) : (nBoost == 1 ? so.name : so.name + " " + (k + 1));
                    Add(new Part("booster-" + k, label, HangarPartKind.Booster, 0, k, true,
                        radial + new Vector3(0f, bBell, 0f), bTank + bNose, bR, bR, bNoseFrac, bH, 0f));
                }
                EndGroup(bH, before);
            }

            int n = d.stages.Count;
            var radius = new float[n];
            var stageH = new float[n];
            var bodyY = new float[n];
            var bodyLen = new float[n];
            var inter = new float[n];
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                var s = d.stages[i];
                EngineOf(c, s, out var eng);
                float bell = Bell(eng, (float)s.diameter, s.engineCount);
                radius[i] = Mathf.Max(0.05f, (float)s.diameter * 0.5f);
                float tank = Tank(c, s);
                float thrust = 0.6f * radius[i] + (i == 0 ? 0f : bell * 0.7f);
                stageH[i] = (i == 0 ? bell : 0f) + thrust + tank;
                bodyY[i] = y + (i == 0 ? bell : 0f);
                bodyLen[i] = thrust + tank;
                y = bodyY[i] + bodyLen[i];
                if (i < n - 1)
                {
                    float rn = Mathf.Max(0.05f, (float)d.stages[i + 1].diameter * 0.5f);
                    inter[i] = 1.2f * Mathf.Max(radius[i], rn);
                    y += inter[i];
                }
            }

            for (int i = 0; i < n; i++)
            {
                var s = d.stages[i];
                EngineOf(c, s, out var eng);
                float bell = Bell(eng, (float)s.diameter, s.engineCount);
                float engineY = i == 0 ? 0f : bodyY[i] - inter[i - 1];
                int before = into.Count;
                AddEngines(s, eng, i, false, Vector3.zero, engineY, bell, stageH[i], spots, Add);
                EndGroup(stageH[i], before);

                before = into.Count;
                Add(new Part("stage-" + i, StageLabel(s, i), HangarPartKind.Stage, 0, i, false,
                    new Vector3(0f, bodyY[i], 0f), bodyLen[i], radius[i], radius[i], 0f, stageH[i], 0f));
                EndGroup(stageH[i], before);

                if (i < n - 1)
                {
                    before = into.Count;
                    float rn = Mathf.Max(0.05f, (float)d.stages[i + 1].diameter * 0.5f);
                    Add(new Part("interstage-" + i, "Interstage " + (i + 1), HangarPartKind.Interstage, 0, i, false,
                        new Vector3(0f, bodyY[i] + bodyLen[i], 0f), inter[i], radius[i], rn, 0f, stageH[i], 0f));
                    EndGroup(stageH[i], before);
                }
            }

            float fairingL = (float)d.fairingLength;
            if (fairingL > 0.05f)
            {
                int before = into.Count;
                float rf = Mathf.Max(0.05f, (float)d.fairingDiameter * 0.5f);
                float nose = d.nose == "blunt" ? 0.25f : d.nose == "conic" ? 0.4f : 0.45f;
                Add(new Part("fairing", "Fairing", HangarPartKind.Fairing, 0, -1, false,
                    new Vector3(0f, y, 0f), fairingL, rf, 0f, nose, fairingL, 0f));
                EndGroup(fairingL, before);
            }
        }

        /// <summary>One leader per part. The column sits just outside the vehicle's own radius, alternating sides.</summary>
        public static void Callouts(IReadOnlyList<Part> parts, float t, List<Callout> into)
        {
            into.Clear();
            if (parts == null || parts.Count == 0) return;
            float maxR = 0.5f;
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                float radial = Mathf.Sqrt(p.RestLocal.x * p.RestLocal.x + p.RestLocal.z * p.RestLocal.z) + p.Radius;
                if (radial > maxR) maxR = radial;
            }
            float reach = maxR * 1.45f;
            float u = float.IsNaN(t) ? 0f : Mathf.Clamp01(t);
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                var anchor = p.RestLocal + new Vector3(0f, p.ExplodeLift * u + p.AxialLength * 0.5f, 0f);
                float side = (i & 1) == 0 ? reach : -reach;
                into.Add(new Callout(p.Id, p.Label, anchor, new Vector3(side, anchor.y, 0f)));
            }
        }

        static void AddEngines(StagePart s, Engine eng, int index, bool strap, Vector3 origin, float y, float bell, float stageH, List<Vector2> spots, System.Action<Part> add)
        {
            int count = s == null ? 0 : Mathf.Max(0, s.engineCount);
            if (count == 0) return;
            float diam = eng != null ? (float)eng.diameter : 0.5f;
            float re = RocketMeshBuilder.EngineSpots((float)s.diameter, diam, count, spots);
            string engName = eng != null && !string.IsNullOrEmpty(eng.label) ? eng.label : "Engine";
            string prefix = strap ? "booster-engine-" + index + "-" : "engine-" + index + "-";
            for (int j = 0; j < spots.Count; j++)
            {
                var at = origin + new Vector3(spots[j].x, y, spots[j].y);
                add(new Part(prefix + j, engName + " " + (j + 1), HangarPartKind.Engine, 0, index, strap,
                    at, bell, re, re, 0f, stageH, 0f, eng != null ? eng.cycle : null, eng != null ? (float)eng.expansionRatio : 0f));
            }
        }

        static Vector3 Radial(int k, int count, float radius)
        {
            float ang = k * Mathf.PI * 2f / Mathf.Max(1, count);
            return new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * radius;
        }

        static void EngineOf(Catalog c, StagePart s, out Engine eng)
        {
            eng = null;
            if (c == null || s == null || string.IsNullOrEmpty(s.engine)) return;
            c.EngineById.TryGetValue(s.engine, out eng);
        }

        static float Bell(Engine e, float stageDiameter, int count) => RocketMeshBuilder.BellHeight(e, stageDiameter, count);

        static float Tank(Catalog c, StagePart s)
        {
            if (s == null) return 0f;
            return Mathf.Max(0f, (float)DesignMath.SizeTank(c, s.propellant, s.propMass, s.diameter).length);
        }

        static string StageLabel(StagePart s, int index)
        {
            if (s != null && !string.IsNullOrEmpty(s.name)) return s.name;
            return "Stage " + (index + 1);
        }
    }
}

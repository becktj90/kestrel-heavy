// #119 v5: render-side truth for the plume. Three look passes failed while the EditMode tests (which check the
// layout formulas) passed, so this probe reports what the particle systems actually hold at runtime.
// Logs once per second in play mode, per emitter: live particleCount / maxParticles, average and max world-space
// particle size, bounding-box width in world metres and as a fraction of the cluster footprint, simulation space,
// scaling mode, and the material's blend state + shader keywords.
// Enable: add -khPlumeProbe to the player/editor command line (PlumeController.Create attaches it), call
// PlumeDebugProbe.AttachAll(), or run Kestrel.Slice.Editor.PlumeProbeCli.Run via -executeMethod (no play mode).
using System.Text;
using UnityEngine;

namespace Kestrel.Slice
{
    public sealed class PlumeDebugProbe : MonoBehaviour
    {
        public float Interval = 1f;
        float next;

        public static bool RequestedOnCommandLine()
        {
            foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-khPlumeProbe") return true;
            return false;
        }

        /// <summary>Attach a probe to every PlumeController in the loaded scenes.</summary>
        public static int AttachAll()
        {
            int n = 0;
            foreach (var pc in Object.FindObjectsByType<PlumeController>(FindObjectsSortMode.None))
                if (pc.GetComponent<PlumeDebugProbe>() == null) { pc.gameObject.AddComponent<PlumeDebugProbe>(); n++; }
            return n;
        }

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + Interval;
            var pc = GetComponent<PlumeController>(); if (pc == null) return;
            Debug.Log(Report(pc, $"t={Time.time:F1}s"));
        }

        /// <summary>One multi-line report for every emitter of a plume (shared with the CLI helper).</summary>
        public static string Report(PlumeController pc, string label)
        {
            var sb = new StringBuilder();
            float cluster = Mathf.Max(0.01f, pc.ClusterDiameter);
            sb.Append($"[PlumeProbe] {label} cluster={cluster:F2} m");
            var tmp = new ParticleSystem.Particle[256];
            foreach (var ps in pc.Emitters)
            {
                if (ps == null) continue;
                sb.Append('\n').Append(EmitterLine(ps, cluster, ref tmp));
            }
            return sb.ToString();
        }

        public static string EmitterLine(ParticleSystem ps, float cluster, ref ParticleSystem.Particle[] tmp)
        {
            var main = ps.main;
            if (tmp == null || tmp.Length < main.maxParticles) tmp = new ParticleSystem.Particle[Mathf.Max(16, main.maxParticles)];
            int n = ps.GetParticles(tmp);
            var t = ps.transform;
            // Size scale actually applied by Unity for this scaling mode.
            float sizeScale = main.scalingMode == ParticleSystemScalingMode.Hierarchy ? t.lossyScale.x
                            : main.scalingMode == ParticleSystemScalingMode.Local ? t.localScale.x : 1f;
            float sum = 0f, max = 0f;
            Vector3 lo = Vector3.positiveInfinity, hi = Vector3.negativeInfinity;
            // #129: also the width of the first 3 cluster diameters below the emitter, i.e. what is in frame near the
            // nozzles (the all-particle width averaged in sprites ~200 m down the body and read 2x the screen).
            Vector3 nlo = Vector3.positiveInfinity, nhi = Vector3.negativeInfinity; int nn = 0;
            // #129-v7: highest point (m) above the emitter (nozzle) plane reached by any sprite with alpha > 5%,
            // and mean alpha. Plume emitters must report <= 0 (nothing visible above the nozzles).
            bool isCloud = ps.name == "GroundCloud"; float topAbove = float.NegativeInfinity, asum = 0f;
            // Short-body PR: visible length (deepest alpha>5% sprite) and sprite overlap in the first 3 D (sum of solid diameters / 3 D).
            float visLen = 0f, nearSolid = 0f;
            // Width is measured perpendicular to the emitter axis (plume) or in the deck plane (cloud).
            var axis = t.forward;
            for (int i = 0; i < n; i++)
            {
                float s = tmp[i].GetCurrentSize(ps) * sizeScale; sum += s; max = Mathf.Max(max, s);
                float alpha = tmp[i].GetCurrentColor(ps).a / 255f; asum += alpha;
                var w = main.simulationSpace == ParticleSystemSimulationSpace.Local ? t.TransformPoint(tmp[i].position) : tmp[i].position;
                var flat = w - Vector3.Dot(w - t.position, axis) * axis;   // project onto the plane through the emitter
                if (!isCloud && alpha > 0.05f) topAbove = Mathf.Max(topAbove, -Vector3.Dot(w - t.position, axis) + 0.5f * PlumeShape.PlumeFringeFraction * s);
                float half = 0.5f * (ps.name == "GroundCloud" ? PlumeShape.SpriteSolidFraction : PlumeShape.PlumeSolidFraction) * s;   // solid half (cloud: soft sprite, plume: tight)
                lo = Vector3.Min(lo, flat - Vector3.one * half); hi = Vector3.Max(hi, flat + Vector3.one * half);
                float depth = Vector3.Dot(w - t.position, axis);
                if (!isCloud && alpha > 0.05f) visLen = Mathf.Max(visLen, depth + half);
                if (depth >= 0f && depth <= 3f * cluster) nearSolid += 2f * half;
                if (depth <= 3f * cluster) { nlo = Vector3.Min(nlo, flat - Vector3.one * half); nhi = Vector3.Max(nhi, flat + Vector3.one * half); nn++; }
            }
            float width = n > 0 ? Mathf.Max(hi.x - lo.x, hi.z - lo.z) : 0f;
            float near = nn > 0 ? Mathf.Max(nhi.x - nlo.x, nhi.z - nlo.z) : 0f;
            var r = ps.GetComponent<ParticleSystemRenderer>(); var m = r != null ? r.sharedMaterial : null;
            string mat = m == null ? "material=<none>"
                : $"material={m.name} queue={m.renderQueue} surface={(m.HasProperty("_Surface") ? m.GetFloat("_Surface") : -1)} blend={(m.HasProperty("_Blend") ? m.GetFloat("_Blend") : -1)} " +
                  $"src={(m.HasProperty("_SrcBlend") ? m.GetFloat("_SrcBlend") : -1)} dst={(m.HasProperty("_DstBlend") ? m.GetFloat("_DstBlend") : -1)} keywords=[{string.Join(",", m.shaderKeywords)}]";
            return $"  {ps.name,-12} live={n}/{main.maxParticles} emitting={ps.emission.enabled}@{ps.emission.rateOverTime.constant:F0}/s " +
                   $"size avg={(n > 0 ? sum / n : 0):F2} max={max:F2} m  solidWidth={width:F2} m ({width / cluster:F2}x cluster) near3D={near:F2} m ({near / cluster:F2}x, n={nn}) alphaAvg={(n > 0 ? asum / n : 0):F2}" + (isCloud ? "" : $" topAboveNozzle={(float.IsNegativeInfinity(topAbove) ? 0f : topAbove):F2} m visLen={visLen:F1} m overlap3D={nearSolid / Mathf.Max(0.1f, 3f * cluster):F1}x") + "  " +
                   $"pos={t.position} sim={main.simulationSpace} scaling={main.scalingMode} lossy={t.lossyScale.x:F3} " +
                   $"maxScreen={(r != null ? r.maxParticleSize : 0):F2} {mat}";
        }
    }
}

// The rocket as separate Hangar parts: one GameObject per stage, interstage, fairing, payload and booster set,
// each with its own material so paint, liveries, finishes, selection and issue tints show per part, and so a tap
// can pick a part and the camera can focus on it. Geometry comes from RocketMeshBuilder.BuildParts (the flight
// stations). The exploded view is R2's HangarExplodedView; this view only ever draws the assembled stack.
using System.Collections.Generic;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using UnityEngine;

namespace Kestrel.Slice
{
    public sealed class HangarRocketView
    {
        public sealed class Piece
        {
            public RocketMeshBuilder.PartMesh mesh;
            public Transform root;
            public Material body, engines;
            public Color baseColor, engineColor;
            public float metallic = 0.1f, smoothness = 0.55f;
            /// <summary>Booster copies (one per strap-on); empty for other parts.</summary>
            public readonly List<Transform> copies = new List<Transform>();
        }

        static readonly Color SelectTint = new Color(0.31f, 0.86f, 1f);
        static readonly Color BlockTint = new Color(0.9f, 0.24f, 0.24f);
        static readonly Color WarnTint = new Color(1f, 0.69f, 0f);
        static readonly Color PayloadGold = new Color(0.83f, 0.66f, 0.23f);
        static readonly Color EngineMetal = new Color(0.33f, 0.35f, 0.38f);

        readonly List<Piece> pieces = new List<Piece>();
        readonly Dictionary<PartRef, AdvisorySeverity> issues = new Dictionary<PartRef, AdvisorySeverity>();
        readonly HashSet<PartRef> hidden = new HashSet<PartRef>();
        bool payloadShown;
        Transform parent;
        PartRef selected = PartRef.Stage(0);
        bool hasSelection;

        public IReadOnlyList<Piece> Pieces => pieces;

        /// <summary>Finish slots this view can show. Bells, legs, grid fins and fins are drawn by the instanced
        /// clusters with their own materials, so the Look tab offers finishes only for these.</summary>
        public static readonly string[] PaintedSlots = { "tank", "booster", "interstage", "fairing" };
        public static bool Paints(string slot) => System.Array.IndexOf(PaintedSlots, slot) >= 0;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"), MetallicId = Shader.PropertyToID("_Metallic"), SmoothnessId = Shader.PropertyToID("_Smoothness");
        MaterialPropertyBlock shellBlock;

        /// <summary>Replace the parts. Old GameObjects and meshes are destroyed.</summary>
        public void Build(List<RocketMeshBuilder.PartMesh> parts, Transform parentTransform, Material template)
        {
            Clear();
            ClearHidden();
            parent = parentTransform;
            foreach (var p in parts)
            {
                var go = new GameObject("Part_" + p.Part);
                go.transform.SetParent(parent, false);
                var piece = new Piece { mesh = p, root = go.transform };
                piece.body = template != null ? new Material(template) : null;
                piece.engines = template != null && p.Engines != null && p.Engines.vertexCount > 0 ? new Material(template) : null;
                var at = p.Instances ?? new[] { Vector3.zero };
                for (int k = 0; k < at.Length; k++)
                {
                    var inst = at.Length == 1 && p.Instances == null ? go : new GameObject("Copy" + k);
                    if (inst != go) { inst.transform.SetParent(go.transform, false); inst.transform.localPosition = at[k]; piece.copies.Add(inst.transform); }
                    inst.AddComponent<MeshFilter>().sharedMesh = p.Mesh;
                    var mr = inst.AddComponent<MeshRenderer>();
                    if (piece.body != null) mr.sharedMaterial = piece.body;
                    if (piece.engines != null)
                    {
                        var eg = new GameObject("Engines");
                        eg.transform.SetParent(inst.transform, false);
                        eg.AddComponent<MeshFilter>().sharedMesh = p.Engines;
                        eg.AddComponent<MeshRenderer>().sharedMaterial = piece.engines;
                    }
                }
                pieces.Add(piece);
            }
            Place();
            Paint();
        }

        public void Clear()
        {
            foreach (var p in pieces)
            {
                if (p.root != null) Object.Destroy(p.root.gameObject);
                if (p.mesh?.Mesh != null) Object.Destroy(p.mesh.Mesh);
                if (p.mesh?.Engines != null) Object.Destroy(p.mesh.Engines);
                if (p.body != null) Object.Destroy(p.body);
                if (p.engines != null) Object.Destroy(p.engines);
            }
            pieces.Clear();
        }

        public Transform RootOf(PartRef p)
        {
            foreach (var x in pieces) if (x.mesh.Part == p) return x.root;
            return null;
        }

        public bool TryGet(PartRef p, out Piece piece)
        {
            foreach (var x in pieces) if (x.mesh.Part == p) { piece = x; return true; }
            piece = null; return false;
        }

        /// <summary>
        /// Colours from the design: livery primary on tanks, boosters and fairing, accent on interstages, the
        /// pattern's two-tone and nose rules, then any per-slot finish (albedo, metallic, smoothness) on top.
        /// </summary>
        public void ApplyLook(JsonObject design, HangarData data)
        {
            var lv = design?.Obj("livery");
            var primary = Hex(lv?.Str("primary"), new Color(0.95f, 0.95f, 0.95f));
            var accent = Hex(lv?.Str("accent"), new Color(0.1f, 0.24f, 0.43f));
            var look = design?.Obj("look");
            string pattern = null;
            if (look?.Str("livery") is string lid && data?.Livery(lid) is LiveryDef def) pattern = def.pattern;
            var fins = look?.Obj("finishes");
            foreach (var p in pieces)
            {
                string slot; Color c;
                switch (p.mesh.Part.kind)
                {
                    case PartKind.Stage:
                        slot = "tank";
                        c = pattern == "two_tone_stage" && p.mesh.Part.index % 2 == 1 ? accent : primary;
                        break;
                    case PartKind.Interstage: slot = "interstage"; c = accent; break;
                    case PartKind.Fairing: slot = "fairing"; c = pattern == "chevron_nose" || pattern == "split_vertical" ? accent : primary; break;
                    case PartKind.Boosters: slot = "booster"; c = pattern == "belt_stripes" ? accent : primary; break;
                    default: slot = null; c = PayloadGold; break;
                }
                p.metallic = p.mesh.Part.kind == PartKind.Payload ? 0.8f : 0.1f;
                p.smoothness = p.mesh.Part.kind == PartKind.Payload ? 0.7f : 0.55f;
                if (slot != null && fins?.Str(slot) is string fid && data?.Finish(fid) is FinishDef f)
                {
                    c = Hex(f.albedo, c);
                    p.metallic = (float)f.metallic;
                    p.smoothness = 1f - (float)f.roughness;
                }
                p.baseColor = c;
                p.engineColor = EngineMetal;
            }
            Paint();
        }

        public void SetSelected(PartRef p, bool show)
        {
            selected = p; hasSelection = show;
            Paint();
        }

        public void SetIssues(IEnumerable<Advisory> advisories)
        {
            issues.Clear();
            if (advisories != null)
                foreach (var a in advisories)
                {
                    if (a.severity == AdvisorySeverity.Note) continue;
                    if (!issues.TryGetValue(a.part, out var s) || a.severity < s) issues[a.part] = a.severity;
                }
            Paint();
        }

        void Paint()
        {
            foreach (var p in pieces)
            {
                var c = p.baseColor;
                if (issues.TryGetValue(p.mesh.Part, out var sev)) c = Color.Lerp(c, sev == AdvisorySeverity.Block ? BlockTint : WarnTint, 0.35f);
                if (hasSelection && p.mesh.Part == selected) c = Color.Lerp(c, SelectTint, 0.4f);
                Set(p.body, c, p.metallic, p.smoothness);
                Set(p.engines, p.engineColor, 0.8f, 0.45f);
                PaintShells(p, c);
            }
        }

        /// <summary>Re-apply colours, e.g. after tank shells were added under the parts.</summary>
        public void Repaint() => Paint();

        /// <summary>The tank shells sit just outside the part body and hide most of it, so the skin submesh of each
        /// shell takes the part's colour and finish too (a property block; the shared shell materials stay untouched).
        /// Welds and stiffeners keep their metal.</summary>
        void PaintShells(Piece p, Color c)
        {
            if (p.root == null) return;
            var shells = p.root.GetComponentsInChildren<Kestrel.Vehicle.TankShellView>(true);
            if (shells == null || shells.Length == 0) return;
            shellBlock ??= new MaterialPropertyBlock();
            shellBlock.Clear();
            c.a = 1f;
            shellBlock.SetColor(BaseColorId, c);
            shellBlock.SetFloat(MetallicId, p.metallic);
            shellBlock.SetFloat(SmoothnessId, p.smoothness);
            foreach (var sh in shells)
            {
                var mr = sh != null ? sh.GetComponent<MeshRenderer>() : null;
                if (mr != null) mr.SetPropertyBlock(shellBlock, Kestrel.Vehicle.TankShellBuilder.SubmeshShell);
            }
        }

        static void Set(Material m, Color c, float metallic, float smoothness)
        {
            if (m == null) return;
            c.a = 1f;
            FlightSolidBody.Apply(m, c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        }

        void Place()
        {
            foreach (var p in pieces)
            {
                if (p.root == null) continue;
                if (p.mesh.Instances != null)
                    for (int k = 0; k < p.copies.Count && k < p.mesh.Instances.Length; k++) p.copies[k].localPosition = p.mesh.Instances[k];
                // The payload sits inside the fairing; it is only seen in R2's exploded view (or in flight after the
                // fairing separates, see SetPayloadShown).
                p.root.gameObject.SetActive(Drawn(p.mesh.Part));
            }
        }

        /// <summary>Show or hide every part. Parts hidden with SetPartHidden stay hidden, and the payload is drawn only
        /// after SetPayloadShown(true) (it lives inside the fairing until the fairing separates).</summary>
        public void SetVisible(bool on)
        {
            foreach (var p in pieces)
                if (p.root != null) p.root.gameObject.SetActive(on && Drawn(p.mesh.Part));
        }

        /// <summary>
        /// Flight staging: a part that has left the stack (a released stage, the booster set, the separated fairing)
        /// is drawn by FlightStageShow's moving piece instead, so the painted part must not draw too. SetVisible and
        /// the per-frame flight picture respect this until ClearHidden (Arm/Reset) or the next Build.
        /// </summary>
        public void SetPartHidden(PartRef part, bool hide)
        {
            if (hide) hidden.Add(part); else hidden.Remove(part);
            // Hiding takes effect now; showing waits for the next SetVisible, which knows whether the stack is drawn.
            if (hide && TryGet(part, out var p) && p.root != null) p.root.gameObject.SetActive(false);
        }

        public bool IsPartHidden(PartRef part) => hidden.Contains(part);

        /// <summary>Draw the payload with the stack (after fairing separation). False puts it back inside the fairing.</summary>
        public void SetPayloadShown(bool show)
        {
            if (payloadShown == show) return;
            payloadShown = show;
            if (!show && TryGet(PartRef.Payload, out var p) && p.root != null) p.root.gameObject.SetActive(false);
        }

        public bool PayloadShown => payloadShown;

        /// <summary>Back to the assembled stack: nothing hidden, payload enclosed.</summary>
        public void ClearHidden()
        {
            hidden.Clear();
            payloadShown = false;
        }

        bool Drawn(PartRef part)
        {
            if (hidden.Contains(part)) return false;
            return part.kind != PartKind.Payload || payloadShown;
        }

        /// <summary>Axis of each visible part on screen, for tap picking.</summary>
        public List<ScreenPart> Project(Camera cam)
        {
            var o = new List<ScreenPart>();
            if (cam == null) return o;
            foreach (var p in pieces)
            {
                if (p.root == null || !p.root.gameObject.activeInHierarchy) continue;
                var axes = p.copies.Count > 0 ? p.copies : new List<Transform> { p.root };
                foreach (var t in axes)
                {
                    var w0 = t.TransformPoint(new Vector3(0, p.mesh.Y0, 0));
                    var w1 = t.TransformPoint(new Vector3(0, p.mesh.Y1, 0));
                    var s0 = cam.WorldToScreenPoint(w0); var s1 = cam.WorldToScreenPoint(w1);
                    if (s0.z <= 0f && s1.z <= 0f) continue;
                    var mid = (w0 + w1) * 0.5f;
                    var edge = cam.WorldToScreenPoint(mid + cam.transform.right * p.mesh.Radius);
                    var c = cam.WorldToScreenPoint(mid);
                    o.Add(new ScreenPart { part = p.mesh.Part, bottom = s0, top = s1, radiusPx = Mathf.Abs(edge.x - c.x), depth = c.z });
                }
            }
            return o;
        }

        /// <summary>World centre and height of a part as currently placed (for camera focus).</summary>
        public bool Bounds(PartRef part, out float centerY, out float partHeight)
        {
            centerY = 0; partHeight = 0;
            if (!TryGet(part, out var p) || p.root == null) return false;
            var t = p.copies.Count > 0 ? p.copies[0] : p.root;
            centerY = t.TransformPoint(p.mesh.Center).y;
            partHeight = Mathf.Max(0.5f, p.mesh.Y1 - p.mesh.Y0);
            return true;
        }

        public static Color Hex(string hex, Color fallback) => !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
    }
}

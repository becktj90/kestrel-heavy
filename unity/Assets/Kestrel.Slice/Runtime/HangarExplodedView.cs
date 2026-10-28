// Animated exploded vehicle. Meshes are built once from HangarExplode.Layout; the animation
// only writes transforms. UX calls HangarCamera.SetExploded. Parent this at the vehicle origin.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Vehicle;
using Kestrel.Vehicle;
using UnityEngine;

namespace Kestrel.Slice
{
    public sealed class HangarPartMarker : MonoBehaviour
    {
        public string Label;
        public string PartId;
        public HangarPartKind Kind;
    }

    public sealed class HangarExplodedView : MonoBehaviour
    {
        readonly List<HangarExplode.Part> parts = new List<HangarExplode.Part>(32);
        readonly List<Node> nodes = new List<Node>(32);
        readonly List<HangarExplode.Callout> callouts = new List<HangarExplode.Callout>(32);
        readonly List<Vector3> verts = new List<Vector3>(256);
        readonly List<Vector3> normals = new List<Vector3>(256);
        readonly List<Vector2> uvs = new List<Vector2>(256);
        readonly List<int> tris = new List<int>(512);
        readonly List<Vector2> profile = new List<Vector2>(16);
        Mesh rod;
        float shown, fromT, toT, elapsed;
        bool animating;

        sealed class Node
        {
            public HangarExplode.Part Part;
            public Transform Root, Leader;
        }

        public IReadOnlyList<HangarExplode.Part> Parts => parts;
        public bool IsExploded => toT > 0.5f;
        public float Shown => shown;
        public int PartCount => nodes.Count;

        /// <summary>Parent under the vehicle (stack axis is local +Y). Parts are not camera children.</summary>
        public void AttachTo(Transform vehicle)
        {
            if (vehicle == null) return;
            transform.SetParent(vehicle, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public Vector3 PartLocal(int index) => nodes[index].Root.localPosition;
        public Vector3 PartWorld(int index) => nodes[index].Root.position;
        public bool PartDrawn(int index) => nodes[index].Root != null && nodes[index].Root.gameObject.activeInHierarchy;

        public void Build(Catalog c, Design d, UnityEngine.Material material)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var root = nodes[i].Root;
                var mf = root != null ? root.GetComponentInChildren<MeshFilter>() : null;
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh != rod) Destroy(mf.sharedMesh);
            }
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            nodes.Clear();
            if (rod == null) rod = RodMesh();
            HangarExplode.Layout(c, d, parts);
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                var go = new GameObject(string.IsNullOrEmpty(p.Label) ? p.Id : p.Label);
                go.transform.SetParent(transform, false);
                var marker = go.AddComponent<HangarPartMarker>();
                marker.Label = p.Label;
                marker.PartId = p.Id;
                marker.Kind = p.Kind;
                var vis = new GameObject("mesh");
                vis.transform.SetParent(go.transform, false);
                vis.AddComponent<MeshFilter>().sharedMesh = PartMesh(p);
                var mr = vis.AddComponent<MeshRenderer>();
                if (material != null) mr.sharedMaterial = material;
                var leader = new GameObject(p.Label + " leader");
                leader.transform.SetParent(transform, false);
                leader.AddComponent<MeshFilter>().sharedMesh = rod;
                var lr = leader.AddComponent<MeshRenderer>();
                if (material != null) lr.sharedMaterial = material;
                nodes.Add(new Node { Part = p, Root = go.transform, Leader = leader.transform });
            }
            shown = 0f; fromT = 0f; toT = 0f; animating = false;
            Apply(0f);
        }

        public void SetExploded(bool on, bool reduceMotion)
        {
            toT = on ? 1f : 0f;
            if (reduceMotion || Mathf.Approximately(shown, toT))
            {
                animating = false;
                Apply(toT);
                return;
            }
            fromT = shown;
            elapsed = 0f;
            animating = true;
        }

        void LateUpdate()
        {
            if (!animating) return;
            float dt = Time.unscaledDeltaTime;
            if (dt < 0f || float.IsNaN(dt)) dt = 0f;
            if (dt > 0.05f) dt = 0.05f;
            elapsed += dt;
            float u = HangarExplode.Progress(elapsed, false);
            float t = Mathf.Lerp(fromT, toT, u);
            if (elapsed >= HangarExplode.Seconds) { t = toT; animating = false; }
            Apply(t);
        }

        void Apply(float t)
        {
            shown = t;
            // Closed: the assembled rocket is the picture. Part meshes stay at their rest pose but do not draw,
            // so a view that was parented at the camera cannot fill the frame with grey caps.
            bool open = t > 0.001f;
            HangarExplode.Callouts(parts, t, callouts);
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (n.Root != null)
                {
                    n.Root.localPosition = HangarExplode.Position(n.Part, t);
                    n.Root.gameObject.SetActive(open);
                }
                if (n.Leader == null) continue;
                n.Leader.gameObject.SetActive(open && i < callouts.Count);
                if (open && i < callouts.Count) PlaceLeader(n.Leader, callouts[i].Anchor, callouts[i].End);
            }
        }

        static void PlaceLeader(Transform leader, Vector3 a, Vector3 b)
        {
            var d = b - a;
            float len = d.magnitude;
            if (len < 1e-3f) { leader.gameObject.SetActive(false); return; }
            leader.gameObject.SetActive(true);
            leader.localPosition = (a + b) * 0.5f;
            leader.localRotation = Quaternion.FromToRotation(Vector3.up, d);
            leader.localScale = new Vector3(0.035f, len, 0.035f);
        }

        Mesh PartMesh(HangarExplode.Part p)
        {
            profile.Clear(); verts.Clear(); normals.Clear(); uvs.Clear(); tris.Clear();
            if (p.Kind == HangarPartKind.Engine)
            {
                float re = Mathf.Max(0.02f, p.Radius);
                if (p.Expansion > 1.5f && BellSpec.TryParseCycle(p.Cycle, out var cycle))
                {
                    var spec = new BellSpec
                    {
                        EngineId = p.Id, Cycle = cycle, ExpansionRatio = p.Expansion,
                        ExitDiameter = re * 2f, VehicleSeed = 1
                    };
                    if (spec.Validate().Count == 0)
                    {
                        var wall = BellProfile.Build(spec, 1).Wall;
                        for (int i = 0; i < wall.Count; i++) profile.Add(new Vector2(wall[i].R, wall[i].Y));
                    }
                }
                if (profile.Count < 2)
                {
                    float bell = Mathf.Max(0.05f, p.AxialLength);
                    profile.Add(new Vector2(re, 0f));
                    profile.Add(new Vector2(re * 0.45f, bell * 0.75f));
                    profile.Add(new Vector2(re * 0.3f, bell * 0.85f));
                    profile.Add(new Vector2(re * 0.5f, bell));
                    profile.Add(new Vector2(0f, bell));
                }
            }
            else if (p.Kind == HangarPartKind.Fairing || p.Kind == HangarPartKind.Booster)
            {
                float r = p.Radius, L = Mathf.Max(0.05f, p.AxialLength);
                float noseFrac = Mathf.Clamp(p.NoseFraction, 0f, 0.9f);
                float cyl = L * (1f - noseFrac), nose = L * noseFrac;
                bool conic = p.Kind == HangarPartKind.Booster || (noseFrac > 0.39f && noseFrac < 0.41f);
                string kind = conic ? "conic" : Mathf.Abs(noseFrac - RocketMeshBuilder.NoseFraction("von-karman")) < 0.001f ? "von-karman" : "ogive";
                // A fairing starts as wide as the stage under it (TopRadius) and flares to its barrel through the shared shoulder; boosters have no shoulder.
                float rJoint = p.Kind == HangarPartKind.Fairing && p.TopRadius > 0.05f ? p.TopRadius : r;
                float hs = Kestrel.Vehicle.FairingShoulder.Height(rJoint, r, L, noseFrac);
                profile.Add(new Vector2(0f, 0f));
                profile.Add(new Vector2(rJoint, 0f));
                if (hs > 0f) profile.Add(new Vector2(r, hs));   // the conical shoulder
                profile.Add(new Vector2(r, cyl));
                for (int k = 1; k <= 8; k++)
                {
                    float u = k / 8f;
                    float rr = RocketMeshBuilder.NoseRadius(kind, r, u);
                    profile.Add(new Vector2(k == 8 ? 0f : rr, cyl + nose * u));
                }
            }
            else
            {
                float top = p.TopRadius > 0f ? p.TopRadius : p.Radius;
                profile.Add(new Vector2(0f, 0f));
                profile.Add(new Vector2(p.Radius, 0f));
                profile.Add(new Vector2(top, p.AxialLength));
                profile.Add(new Vector2(0f, p.AxialLength));
            }
            RocketMeshBuilder.Lathe(profile, Vector3.zero, verts, normals, uvs, tris);
            var mesh = new Mesh { name = p.Id };
            mesh.SetVertices(verts); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh RodMesh()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            var prof = new List<Vector2> { new Vector2(1f, -0.5f), new Vector2(1f, 0.5f) };
            RocketMeshBuilder.Lathe(prof, Vector3.zero, v, n, uv, t);
            var mesh = new Mesh { name = "HangarLeader" };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

// Ties the generated trench to the slice pad: open the ground under the channel, hide the
// solid deck cylinder (the generated slab replaces it), and aim the ground cloud out the mouths.
// Plume speeds, lifetimes and collision are not touched here.
using System.Collections.Generic;
using Kestrel.Pad;
using Kestrel.Site;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class PadDetailLink
    {
        public const string GroundMeshName = "KH_GroundCut";

        public static void Apply(PlumeController plume, Transform tower, Transform ground, Transform deck, PadLayout layout)
        {
            var trench = layout != null ? layout.Trench : null;
            if (deck != null)
            {
                var mr = deck.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = trench == null;
            }
            if (ground != null) CutGround(ground, tower, trench);
            if (plume != null && trench != null) Aim(plume, tower, trench, layout);
        }

        static void CutGround(Transform ground, Transform tower, TrenchSpec trench)
        {
            var site = ground.GetComponent<SiteView>();
            if (site != null)
            {
                site.SetGroundOpenings(trench == null ? null : SiteOpenings(ground, tower, trench));
                return;
            }
            var mf = ground.GetComponent<MeshFilter>();
            if (mf == null) return;
            if (trench == null)
            {
                if (mf.sharedMesh != null && mf.sharedMesh.name == GroundMeshName)
                {
                    var old = mf.sharedMesh;
                    mf.sharedMesh = BuiltinPlane();
                    Kill(old);
                }
                return;
            }
            // The Unity plane is local ±5, scaled out to ±100 km. Ear-clip eps is absolute, so the
            // hole is cut in metres and the vertices are scaled back into the plane.
            float sx = Mathf.Max(1e-4f, Mathf.Abs(ground.lossyScale.x));
            float sz = Mathf.Max(1e-4f, Mathf.Abs(ground.lossyScale.z));
            var holes = new List<IList<Vec2>>
            {
                ToMetres(ground, tower, TrenchGrammar.GroundOpening(trench), sx, sz)
            };
            foreach (var pond in TrenchGrammar.PondPolys(trench))
                holes.Add(ToMetres(ground, tower, pond, sx, sz));
            float half = 5f * Mathf.Max(sx, sz);
            var data = PolyCut.Frame(half, holes, 0f);
            var mesh = PadMeshUpload.Upload(data, GroundMeshName);
            if (mesh == null) return;
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
                verts[i] = new Vector3(verts[i].x / sx, verts[i].y, verts[i].z / sz);
            mesh.vertices = verts;
            mesh.RecalculateBounds();
            var prev = mf.sharedMesh;
            mf.sharedMesh = mesh;
            if (prev != null && prev.name == GroundMeshName) Kill(prev);
        }

        static List<List<Vector2>> SiteOpenings(Transform ground, Transform tower, TrenchSpec trench)
        {
            var holes = new List<List<Vector2>>();
            holes.Add(AsLocal(ToMetres(ground, tower, TrenchGrammar.GroundOpening(trench), 1f, 1f)));
            foreach (var pond in TrenchGrammar.PondPolys(trench))
                holes.Add(AsLocal(ToMetres(ground, tower, pond, 1f, 1f)));
            return holes;
        }

        static List<Vector2> AsLocal(List<Vec2> ring)
        {
            var o = new List<Vector2>(ring.Count);
            for (int i = 0; i < ring.Count; i++) o.Add(new Vector2(ring[i].X, ring[i].Y));
            return o;
        }

        static List<Vec2> ToMetres(Transform ground, Transform tower, List<Vec2> padPoly, float sx, float sz)
        {
            var pad = tower != null ? tower.Find("Pad") : null;
            var o = new List<Vec2>(padPoly.Count);
            foreach (var p in padPoly)
            {
                Vector3 world = pad != null
                    ? pad.TransformPoint(new Vector3(p.X, 0f, p.Y))
                    : new Vector3(p.X, ground.position.y, p.Y);
                Vector3 local = ground.InverseTransformPoint(world);
                o.Add(new Vec2(local.x * sx, local.z * sz));
            }
            return o;
        }

        static void Aim(PlumeController plume, Transform tower, TrenchSpec trench, PadLayout layout)
        {
            float deck = layout != null ? TrenchGrammar.SurfaceRadius(layout) : 0f;
            var mouths = TrenchGrammar.Mouths(trench, deck);
            if (mouths.Count == 0) return;
            var pad = tower != null ? tower.Find("Pad") : null;
            var centers = new Vector3[mouths.Count];
            var outward = new Vector3[mouths.Count];
            var width = new float[mouths.Count];
            var height = new float[mouths.Count];
            for (int i = 0; i < mouths.Count; i++)
            {
                var m = mouths[i];
                Vector3 pos = new Vector3(m.X, 0f, m.Z);
                Vector3 dir = new Vector3(m.DirX, 0f, m.DirZ);
                if (pad != null && tower != null)
                {
                    centers[i] = tower.InverseTransformPoint(pad.TransformPoint(pos));
                    outward[i] = tower.InverseTransformDirection(pad.TransformDirection(dir));
                }
                else
                {
                    centers[i] = pos;
                    outward[i] = dir;
                }
                centers[i].y = 0f;
                width[i] = m.Width;
                height[i] = m.Height;
            }
            plume.AimGroundCloudAtMouths(centers, outward, width, height);
        }

        static Mesh BuiltinPlane()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Kill(go);
            return mesh;
        }

        static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }
    }
}

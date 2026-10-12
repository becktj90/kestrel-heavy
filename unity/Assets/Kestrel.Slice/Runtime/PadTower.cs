// Launch tower for the slice pad.
//
// The "small grey box at the bottom left of 02-liftoff" was the old tower: a bare 4 x 90 x 4 m
// CreatePrimitive(Cube) at (-14, 45, 0) with the default grey material (bodyMat was applied to the
// ground only) and a fixed 90 m height regardless of the rocket. At T+4 s (~100 m) the chase camera,
// below and to the right of the vehicle, sees only the tower's top end, so it read as a stray box.
// Now it is a dark steel mast sized to the rocket (top just above the fairing), with a service arm,
// set behind the vehicle (+z, away from the camera), with colliders removed.
using UnityEngine;

namespace Kestrel.Slice
{
    public static class PadTower
    {
        public static readonly Color Steel = new Color(0.16f, 0.17f, 0.19f);
        public const float MastWidth = 4f, Clearance = 6f, Behind = 6f, Overtop = 1.08f;

        /// <summary>Mast centre (x, z) and height for a rocket of height h and max half-width r (metres).</summary>
        public static (float x, float z, float height) Fit(float h, float r) =>
            (-(r + Clearance + MastWidth * 0.5f), Behind, Mathf.Max(20f, h * Overtop));

        public static Transform Create(Material template)
        {
            var root = new GameObject("PadTower").transform;
            Material m = null;
            if (template != null) m = new Material(template) { color = Steel, name = "KH_TowerSteel" };
            Part(root, "Mast", m); Part(root, "Arm", m);
            return root;
        }

        static void Part(Transform root, string name, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(root, false);
            Object.Destroy(go.GetComponent<Collider>());
            if (m != null) go.GetComponent<Renderer>().sharedMaterial = m;
        }

        /// <summary>Resize for the current design. root sits at the pad origin (anchored by FlightDriver).</summary>
        public static void Apply(Transform root, float h, float r)
        {
            var (x, z, height) = Fit(h, r);
            var mast = root.Find("Mast"); var arm = root.Find("Arm");
            mast.localScale = new Vector3(MastWidth, height, MastWidth); mast.localPosition = new Vector3(x, height * 0.5f, z);
            float armLen = -x - r * 0.5f;                       // reaches toward the vehicle without touching it
            arm.localScale = new Vector3(armLen, 1.5f, 2f); arm.localPosition = new Vector3(x + armLen * 0.5f, h * 0.85f, z);
        }
    }
}

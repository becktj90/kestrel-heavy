// Launch tower for the slice pad. Entry point kept from #81 (Create / Apply / Fit); since D1 the inside is the
// seeded pad generator + instanced structural steel (Kestrel.Pad, docs/PAD_VEHICLE_DETAIL_AUDIT.md §10 Q2).
// History: the original was a bare grey 4 x 90 x 4 m cube that read as a stray box at liftoff (#81 fixed sizing).
// The generated pad's +Z axis (umbilical contract: "+Z toward the pad tower") is yawed to point at the Fit position,
// behind the vehicle and away from the chase camera.
using Kestrel.Pad;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class PadTower
    {
        /// <summary>Kept for compatibility; the generated tower uses MaterialLibrary (Kit) roles.</summary>
        public static readonly Color Steel = new Color(0.16f, 0.17f, 0.19f);
        public const float MastWidth = 4f, Clearance = 6f, Behind = 6f, Overtop = 1.08f;
        public const int DefaultSeed = 1;

        /// <summary>Tower direction (x, z) and minimum height for a rocket of height h and max half-width r (metres).</summary>
        public static (float x, float z, float height) Fit(float h, float r) =>
            (-(r + Clearance + MastWidth * 0.5f), Behind, Mathf.Max(20f, h * Overtop));

        /// <summary>Same rule as Kestrel.Core.Timeline.Pad.For when only size is known: tower for stages >= 3 m across.</summary>
        public static PadArchetype DefaultArchetype(float r) => r * 2f >= 3f ? PadArchetype.Tower : PadArchetype.Rail;

        public static Transform Create(Material template)
        {
            // template no longer used (Kit materials are shared, SRP Batcher friendly); parameter kept for callers.
            var root = new GameObject("PadTower").transform;
            var pad = new GameObject("Pad").transform; pad.SetParent(root, false);
            return root;
        }

        public static void Apply(Transform root, float h, float r) => Apply(root, h, r, DefaultArchetype(r), DefaultSeed);

        /// <summary>Regenerate for the current design. root sits at the pad origin (anchored by FlightDriver).
        /// Same (h, r, archetype, seed) is a no-op.</summary>
        public static PadRenderer Apply(Transform root, float h, float r, PadArchetype type, int seed)
        {
            var pad = root.Find("Pad"); if (pad == null) { pad = new GameObject("Pad").transform; pad.SetParent(root, false); }
            var (x, z, _) = Fit(h, r);
            pad.localPosition = Vector3.zero; pad.localRotation = Quaternion.Euler(0, Mathf.Atan2(x, z) * Mathf.Rad2Deg, 0);
            var existing = pad.GetComponent<PadRenderer>();
            var L = existing != null ? existing.Layout : null;
            if (L != null && L.Seed == seed && L.Archetype == type && Mathf.Approximately(L.Fit.VehicleHeight, h) && Mathf.Approximately(L.Fit.VehicleRadius, r)) return existing;
            return PadBuilder.Build(pad, seed, type, new PadFit(h, r, Clearance));
        }
    }
}

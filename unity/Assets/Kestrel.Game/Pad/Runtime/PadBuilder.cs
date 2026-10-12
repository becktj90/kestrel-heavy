// Entry used by Kestrel.Slice.PadTower (and later the pad scene): generate + render + connect in one call.
using System.Collections.Generic;
using Kestrel.Kit.Contracts;
using UnityEngine;

namespace Kestrel.Pad
{
    public static class PadBuilder
    {
        /// <summary>Builds (or rebuilds) the pad under root. Returns the renderer. Arms are connected to the
        /// given or default attach points with vehicle frame == pad frame (vehicle on the pad centre line).</summary>
        public static PadRenderer Build(Transform root, int seed, PadArchetype type, PadFit fit, IReadOnlyList<AttachPoint> attach = null)
        {
            var pts = attach ?? PadGenerator.DefaultAttachPoints(fit, type);
            var layout = PadGenerator.Generate(seed, type, fit, pts);
            var r = root.GetComponent<PadRenderer>(); if (r == null) r = root.gameObject.AddComponent<PadRenderer>();
            r.Build(layout);
            var identity = UmbilicalRig.Identity;
            foreach (var arm in r.Arms) foreach (var p in pts) if (p.Id == arm.AttachId) arm.Connect(p, identity);
            foreach (var u in r.Umbilicals) foreach (var p in pts) if (p.Id == u.AttachId) u.Connect(p, identity);
            r.BindSequence(pts);
            return r;
        }
    }
}

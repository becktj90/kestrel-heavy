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
        public static PadRenderer Build(Transform root, int seed, PadArchetype type, PadFit fit, IReadOnlyList<AttachPoint> attach = null, FarmPlan propellant = null)
        {
            var pts = attach ?? PadGenerator.DefaultAttachPoints(fit, type);
            var layout = PadGenerator.Generate(seed, type, fit, pts, propellant);
            var r = root.GetComponent<PadRenderer>(); if (r == null) r = root.gameObject.AddComponent<PadRenderer>();
            r.Build(layout);
            var detail = root.GetComponent<PadDetailRenderer>(); if (detail == null) detail = root.gameObject.AddComponent<PadDetailRenderer>();
            detail.Build(layout);
            var identity = UmbilicalRig.Identity;
            foreach (var arm in r.Arms) foreach (var p in pts) if (p.Id == arm.AttachId) arm.Connect(p, identity);
            foreach (var u in r.Umbilicals) foreach (var p in pts) if (p.Id == u.AttachId) u.Connect(p, identity);
            r.BindSequence(pts);
            // D5: lights. Night follows the scene sun (0 by day, so realtime lights stay off); -khNight or
            // PadLights.ForceNight forces full night for shots.
            var lights = PadLights.Attach(r, r.Quality);
            lights.Night = PadLights.ResolveNight();
            r.FarmKey = layout.Propellant == null ? 0 : layout.Propellant.Key;
            PadVfxLink.Publish(layout, root);
            return r;
        }
    }
}

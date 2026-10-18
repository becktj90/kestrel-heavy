// Publishes a generated pad's deluge into the engine-free ignition state and leaves an anchor
// the environment views can find. The flight loop updates throttle and height through SetFlight.
using Kestrel.Kit;
using UnityEngine;

namespace Kestrel.Pad
{
    public static class PadVfxLink
    {
        public static void Publish(PadLayout layout, Transform pad)
        {
            var st = PadVfxState.Current;
            var d = layout != null ? layout.Deluge : null;
            st.Deluge = d != null && d.Active;
            st.TowerHeight = d != null && d.TowerHeight > 1f ? d.TowerHeight : 80f;
            st.BearingDeg = d != null ? d.BearingDeg : 0f;
            st.ExitDiameter = d != null && d.ExitDiameter > 0.2f ? d.ExitDiameter : 1.5f;
            int n = d != null ? d.Nozzles : 0;
            if (n > PadVfxState.MaxNozzles) n = PadVfxState.MaxNozzles;
            if (n < 0) n = 0;
            for (int i = 0; i < n; i++) st.SetNozzle(i, d.X[i], d.Y[i], d.Z[i], d.DirX[i], d.DirZ[i]);
            st.Nozzles = n;
            EnsureAnchor(pad);
        }

        public static void SetFlight(float throttle, float height)
        {
            var st = PadVfxState.Current;
            st.Throttle = throttle < 0f ? 0f : throttle;
            st.Height = height;
        }

        public static void EnsureAnchor(Transform pad)
        {
            if (pad == null) return;
            if (pad.Find(PadVfxState.AnchorName) != null) return;
            var go = new GameObject(PadVfxState.AnchorName);
            go.transform.SetParent(pad, false);
        }
    }
}

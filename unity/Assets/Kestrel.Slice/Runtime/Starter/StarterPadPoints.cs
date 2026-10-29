using Kestrel.Core.Designer;
using Kestrel.Pad;
using UnityEngine;

namespace Kestrel.Slice
{
    /// <summary>The pad art the Starter camera must frame (tower crown, base plates, pad circle), in world space.
    /// SliceApp.BuildRocket hands these to StarterPreviewCamera.SetVehicle, and the KH-1 capture test builds its camera the same way,
    /// so the camera and the test cannot disagree about where the art is: pad-frame points from <see cref="PadShowcase"/>, turned by
    /// <see cref="PadShowcase.YawDeg"/> exactly as PadTower.Apply turns the Pad child, then moved to the pad origin.</summary>
    public static class StarterPadPoints
    {
        public static PadPoint[] For(PadLayout layout, float vehicleHeight, float vehicleRadius, Vector3 padOrigin)
        {
            if (layout == null) return null;
            var show = PadShowcase.Points(layout);
            if (show.Count == 0) return null;
            float yaw = PadShowcase.YawDeg(vehicleHeight, vehicleRadius);
            var pts = new PadPoint[show.Count];
            for (int i = 0; i < pts.Length; i++)
                pts[i] = StarterPadFrame.World((int)show[i].Kind, show[i].X, show[i].Y, show[i].Z, yaw, padOrigin.x, padOrigin.y, padOrigin.z);
            return pts;
        }
    }
}

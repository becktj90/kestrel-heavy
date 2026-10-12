// Runtime view of data/visual-spec.json ("world" + "portrait") and data/tod.json ("wash").
// Both are synced into Resources/KestrelData by Kestrel > Sync U2 Data; never hand-copied.
using System;
using Kestrel.Game.Layout;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    [Serializable] public sealed class SpecWorld { public float W = 1280, H = 720, stageKMax = 2, stageEdgeMax = 4096, kStep = 0.25f; public string scaleMode; }
    [Serializable] public sealed class SpecPortraitJson
    {
        public bool letterbox; public string fill;
        public float sideMarginPt = 8, minTouchPt = 44, vehicleAnchorY = 0.38f, plumeMaxWidthOfVisible = 0.8f;
    }
    [Serializable] public sealed class SpecSky { public float[] spaceBlendKm = { 6, 56 }; public string[] forceSpaceStatus; }
    // JsonUtility.FromJson fills these. The compiler cannot see that write, so CS0649 is a false alarm.
#pragma warning disable CS0649
    [Serializable] sealed class SpecRoot { public int specVersion; public SpecWorld world; public SpecPortraitJson portrait; public SpecSky sky; }

    [Serializable] public sealed class Wash { public string space, mid, near, horizon, low; }
    [Serializable] public sealed class TodPalette { public string label; public Wash wash; public float stars; }
    [Serializable] sealed class TodPalettes { public TodPalette dawn, noon, golden, dusk, night; }
    [Serializable] sealed class TodRoot { public TodPalettes palettes; }
#pragma warning restore CS0649

    public sealed class VisualSpecData
    {
        public int SpecVersion;
        public SpecPortrait Portrait;
        public SpecSky Sky;
        TodPalettes _tod;

        public static VisualSpecData Load()
        {
            var spec = Resources.Load<TextAsset>("KestrelData/visual-spec");
            var tod = Resources.Load<TextAsset>("KestrelData/tod");
            if (spec == null || tod == null) throw new InvalidOperationException("[Kestrel] visual-spec/tod data missing. Run Kestrel > Sync U2 Data.");
            return Parse(spec.text, tod.text);
        }

        public static VisualSpecData Parse(string specJson, string todJson)
        {
            var r = JsonUtility.FromJson<SpecRoot>(specJson);
            if (r.portrait.letterbox) throw new InvalidOperationException("VISUAL_SPEC portrait.letterbox must be false");
            return new VisualSpecData
            {
                SpecVersion = r.specVersion,
                Sky = r.sky,
                Portrait = new SpecPortrait
                {
                    WorldW = r.world.W, WorldH = r.world.H, StageKMax = r.world.stageKMax, StageEdgeMax = r.world.stageEdgeMax, KStep = r.world.kStep,
                    Letterbox = r.portrait.letterbox, SideMarginPt = r.portrait.sideMarginPt, MinTouchPt = r.portrait.minTouchPt,
                    VehicleAnchorY = r.portrait.vehicleAnchorY, PlumeMaxWidthOfVisible = r.portrait.plumeMaxWidthOfVisible,
                },
                _tod = JsonUtility.FromJson<TodRoot>(todJson).palettes,
            };
        }

        /// <summary>tod.js todPalette(id): unknown ids fall back to noon.</summary>
        public TodPalette Palette(string id)
        {
            switch (id)
            {
                case "dawn": return _tod.dawn;
                case "golden": return _tod.golden;
                case "dusk": return _tod.dusk;
                case "night": return _tod.night;
                default: return _tod.noon;
            }
        }

        public double SpaceBlend(double altKm, string status = null)
            => SkyBlend.SpaceBlend(altKm, status, Sky.spaceBlendKm[0], Sky.spaceBlendKm[1]);

        public static Color ToColor(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

        /// <summary>Themed fill = lerpRgb(wash.horizon, wash.space, t) (VISUAL_SPEC §4/§5).</summary>
        public Color ThemedFill(string todId, double spaceT)
        {
            var w = Palette(todId).wash;
            return ToColor(SkyBlend.ThemedFill(SkyBlend.ParseHex(w.horizon), SkyBlend.ParseHex(w.space), spaceT));
        }
    }
}

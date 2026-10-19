// Dusk flight chrome. Fraunces for the clock and callout titles, Space Grotesk for
// the sentences. Ink plate, cream type, a sun or alarm edge. No pulse, no scale pop.
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public static class FlightDusk
    {
        public static readonly Color Ink = new Color(13f / 255f, 20f / 255f, 40f / 255f, 0.94f);
        public static readonly Color Cream = new Color(246f / 255f, 235f / 255f, 210f / 255f, 1f);
        public static readonly Color Sun = new Color(232f / 255f, 140f / 255f, 60f / 255f, 1f);
        public static readonly Color Teal = new Color(70f / 255f, 217f / 255f, 204f / 255f, 1f);
        public static readonly Color Alarm = new Color(242f / 255f, 84f / 255f, 61f / 255f, 1f);
        public static readonly Color AlarmInk = new Color(46f / 255f, 14f / 255f, 18f / 255f, 0.94f);

        public static Font TitleFace() => Resources.Load<Font>("KHFonts/Fraunces-Bold");
        public static Font BodyFace() => Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium");

        public static void Clock(Label label, float unitsPerPoint, float fontPt)
        {
            if (label == null) return;
            float u = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            Frame(label, u, false);
            // The header is 36 pt. Frame's 4 pt pads plus a 3 pt edge would shove a 28 pt clock out of it.
            label.style.paddingTop = 0;
            label.style.paddingBottom = 0;
            label.style.borderBottomWidth = 2f * u;
            label.style.fontSize = Mathf.Max(13f, fontPt) * u;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            Face(label, TitleFace());
        }

        public static void Banner(Label label, float unitsPerPoint, bool alarm)
        {
            if (label == null) return;
            float u = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            Frame(label, u, alarm);
            label.style.fontSize = 15f * u;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            Face(label, TitleFace());
        }

        public static void Callout(Label label, float unitsPerPoint, bool alarm)
        {
            if (label == null) return;
            float u = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            Frame(label, u, alarm);
            label.style.fontSize = 13f * u;
            label.style.minHeight = Kestrel.Core.Flight.FlightStaging.CalloutPt * u;
            label.style.height = StyleKeyword.Auto;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.overflow = Overflow.Hidden;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            Face(label, BodyFace());
        }

        public static void Sentence(Label label, float unitsPerPoint)
        {
            if (label == null) return;
            float u = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            label.style.fontSize = 13f * u;
            label.style.color = Cream;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.overflow = Overflow.Hidden;
            Face(label, BodyFace());
        }

        public static void Frame(VisualElement e, float u, bool alarm)
        {
            if (e == null) return;
            e.style.backgroundColor = alarm ? AlarmInk : Ink;
            e.style.color = Cream;
            e.style.paddingLeft = 8f * u;
            e.style.paddingRight = 8f * u;
            e.style.paddingTop = 4f * u;
            e.style.paddingBottom = 4f * u;
            float r = 10f * u;
            e.style.borderTopLeftRadius = r;
            e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r;
            e.style.borderBottomRightRadius = r;
            e.style.borderTopWidth = 0;
            e.style.borderLeftWidth = 0;
            e.style.borderRightWidth = 0;
            e.style.borderBottomWidth = 3f * u;
            e.style.borderBottomColor = alarm ? Alarm : Sun;
        }

        public static void Face(VisualElement e, Font font)
        {
            if (e == null || font == null) return;
            e.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
        }

        public static Color Tone(Kestrel.Core.Flight.VehicleHealthTone tone)
        {
            if (tone == Kestrel.Core.Flight.VehicleHealthTone.Red) return Alarm;
            if (tone == Kestrel.Core.Flight.VehicleHealthTone.Amber) return Sun;
            return Teal;
        }
    }
}

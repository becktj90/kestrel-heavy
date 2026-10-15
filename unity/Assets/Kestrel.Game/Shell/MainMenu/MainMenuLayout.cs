// Perspective layout for the Home hangar. Panels are real objects at different depths along one
// camera, placed by view fraction so SE, Pro Max and iPad portrait keep the same screen composition.
// Viewport y grows upward (0 bottom, 1 top), matching Unity's viewport.
using System.Globalization;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public enum MenuTextRole { Title, Subtitle, Launch, Glass, Hud, Dial }

    public readonly struct MenuPose
    {
        public readonly Vector3 Eye, Forward, Right, Up;
        public readonly float FovY;
        public MenuPose(Vector3 eye, Vector3 forward, Vector3 right, Vector3 up, float fovY)
        { Eye = eye; Forward = forward; Right = right; Up = up; FovY = fovY; }
    }

    public readonly struct MenuAnchor
    {
        public readonly string Id;
        public readonly float Depth, XFrac, YFrac, WidthFrac, HeightFrac;
        public readonly bool Clickable, Live;
        public readonly string Label, Spoken, Icon;
        public readonly MenuTextRole Role;
        public MenuAnchor(string id, float depth, float xFrac, float yFrac, float widthFrac, float heightFrac,
            bool clickable, bool live, string label, string spoken, string icon, MenuTextRole role)
        {
            Id = id; Depth = depth; XFrac = xFrac; YFrac = yFrac; WidthFrac = widthFrac; HeightFrac = heightFrac;
            Clickable = clickable; Live = live; Label = label; Spoken = spoken; Icon = icon; Role = role;
        }
    }

    /// <summary>Viewport rectangle. X0/X1 left/right, Y0/Y1 bottom/top, all 0..1.</summary>
    public readonly struct ScreenBox
    {
        public readonly float X0, X1, Y0, Y1;
        public ScreenBox(float x0, float x1, float y0, float y1) { X0 = x0; X1 = x1; Y0 = y0; Y1 = y1; }
        public float Width => X1 - X0;
        public float Height => Y1 - Y0;
        public bool Overlaps(ScreenBox o, float pad = 0f) =>
            X1 > o.X0 + pad && o.X1 > X0 + pad && Y1 > o.Y0 + pad && o.Y1 > Y0 + pad;
        public bool Contains(float x, float y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;
    }

    /// <summary>Crisp TextMeshPro settings. One glyph, no underlay (an underlay is a second copy of the
    /// letter and reads as a doubled stroke), no bold, no autosize, no rich text.</summary>
    public readonly struct MenuTextStyle
    {
        public readonly float FontSize, CharacterSpacing, OutlineWidth, FaceDilate, GlowOuter, GlowPower;
        public readonly bool ExtraPadding, AutoSize, WordWrap, RichText, Bold, Underlay;
        public MenuTextStyle(float fontSize, float characterSpacing, float outlineWidth, float faceDilate,
            float glowOuter, float glowPower, bool extraPadding, bool autoSize, bool wordWrap, bool richText, bool bold, bool underlay)
        {
            FontSize = fontSize; CharacterSpacing = characterSpacing; OutlineWidth = outlineWidth; FaceDilate = faceDilate;
            GlowOuter = glowOuter; GlowPower = glowPower; ExtraPadding = extraPadding; AutoSize = autoSize;
            WordWrap = wordWrap; RichText = richText; Bold = bold; Underlay = underlay;
        }
    }

    public readonly struct MenuReadout
    {
        public readonly string LeftFuel, LeftLox, LeftMass, RightThrust, RightWind, RightStatus, Designation;
        public MenuReadout(string fuel, string lox, string mass, string thrust, string wind, string status, string designation)
        {
            LeftFuel = fuel; LeftLox = lox; LeftMass = mass; RightThrust = thrust; RightWind = wind; RightStatus = status; Designation = designation;
        }
        public string[] All() => new[] { LeftFuel, LeftLox, LeftMass, RightThrust, RightWind, RightStatus, Designation };
    }

    public static class MainMenuLayout
    {
        public const float FovYDegrees = 33f;
        public const float MinHitPx = 44f;
        public const float DriftYawDegrees = 0.35f;
        public const float DriftSlideMetres = 0.012f;

        // Same PlayerPrefs the hangar already honours (HangarCamera / Comfort). Both kill the breathe and the drift.
        public const string ReduceMotionPref = "kh.a11y.reduceMotion";
        public const string ReduceFlashingPref = "kh.a11y.reduceFlashing";

        public const float RocketDepth = 14.6f;
        public const float RocketBaseYFrac = -0.06f;
        public const float RocketNoseYFrac = 0.46f;
        public const float CrewDepth = 12.0f;
        public const float HangarWallDepth = 24f;
        public const float VehicleHeightM = 71.4f;
        public const float VehicleDiameterM = 3.7f;
        public const float FairingDiameterM = 5.2f;
        public const float BoosterCenterM = 3.7f;
        public const float BoosterSpanM = 11.1f;
        public const float CrewHeightM = 1.75f;
        public const int CrewCount = 5;
        public const int RocketSegments = 48;

        public const string LaunchLabel = "INITIATE LAUNCH SEQUENCE";
        public const string TitleTop = "KESTREL";
        public const string TitleBottom = "HEAVY";
        public const string Tagline = "DESIGN \u00B7 BUILD \u00B7 LAUNCH";
        public const string Designation = "KH-1";

        // Eye sits above the deck. The look is slightly down so the floor at the rocket base stays
        // below the camera; the view fractions above still put the vehicle in the middle of the portrait.
        public static readonly Vector3 Eye = new Vector3(0f, 3.05f, -6.5f);
        public static readonly Vector3 LookAt = new Vector3(0f, 2.15f, 12f);

        static readonly MenuAnchor[] Table =
        {
            // Closest: the hero bar. Then the near glass row, the wordmark, the far glass row, then the HUD.
            // Left and right of a row sit a few tenths of a metre apart so a yaw shows parallax inside the row.
            new MenuAnchor("launch", 3.15f, 0f, -0.74f, 0.82f, 0.100f, true, true,
                LaunchLabel, "Launch. Opens the rocket designer on the pad.", null, MenuTextRole.Launch),
            new MenuAnchor("codex", 4.30f, -0.46f, -0.46f, 0.34f, 0.080f, true, false,
                "CODEX", "Codex. Coming soon.", "book", MenuTextRole.Glass),
            new MenuAnchor("settings", 4.60f, 0.46f, -0.46f, 0.34f, 0.080f, true, false,
                "SETTINGS", "Settings. Coming soon.", "gear", MenuTextRole.Glass),
            new MenuAnchor("subtitle", 5.15f, 0f, 0.58f, 0.70f, 0.030f, false, false,
                Tagline, "Design, build, launch.", null, MenuTextRole.Subtitle),
            new MenuAnchor("title", 5.35f, 0f, 0.75f, 0.82f, 0.088f, false, false,
                TitleTop, "Kestrel Heavy.", null, MenuTextRole.Title),
            new MenuAnchor("hangar", 5.90f, -0.46f, -0.20f, 0.32f, 0.072f, true, true,
                "HANGAR", "Hangar. Opens the rocket designer.", "wrench", MenuTextRole.Glass),
            new MenuAnchor("missions", 6.20f, 0.46f, -0.20f, 0.32f, 0.072f, true, false,
                "MISSIONS", "Missions. Coming soon.", "planet", MenuTextRole.Glass),
            new MenuAnchor("tel-l", 8.60f, -0.58f, 0.16f, 0.24f, 0.150f, false, false,
                "", "Fuel, oxidizer and mass.", null, MenuTextRole.Hud),
            new MenuAnchor("tel-r", 8.60f, 0.58f, 0.16f, 0.24f, 0.150f, false, false,
                "", "Thrust, wind and status.", null, MenuTextRole.Hud),
            new MenuAnchor("kh1", 9.15f, 0f, 0.16f, 0.15f, 0.140f, false, false,
                Designation, "KH-1 mission patch.", null, MenuTextRole.Dial),
        };

        public static MenuAnchor[] Anchors() => (MenuAnchor[])Table.Clone();

        public static MenuAnchor Find(string id)
        {
            for (int i = 0; i < Table.Length; i++) if (Table[i].Id == id) return Table[i];
            return default;
        }

        /// <summary>true = the 2D mix overlay (title card, LAUNCH bar, tiles) replaces the 3D title/launch/glass rows;
        /// false = the original 3D controls. The 3D rocket, hangar and telemetry stay either way.</summary>
        public const bool UseOverlay = true;

        /// <summary>3D plates the overlay replaces.</summary>
        public static bool ReplacedByOverlay(MenuAnchor a) => a.Clickable || a.Id == "title" || a.Id == "subtitle";

        public static bool ReducedMotion() =>
            PlayerPrefs.GetInt(ReduceMotionPref, 0) != 0 || PlayerPrefs.GetInt(ReduceFlashingPref, 0) != 0;

        public static MenuPose Pose()
        {
            Vector3 f = (LookAt - Eye).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, f).normalized;
            Vector3 up = Vector3.Cross(f, right);
            return new MenuPose(Eye, f, right, up, FovYDegrees);
        }

        /// <summary>Slow yaw and slide. Closer panels parallax more than the rocket. Held still when reduced.</summary>
        public static MenuPose PoseAt(float time, bool reduceMotion)
        {
            var p = Pose();
            if (reduceMotion) return p;
            float yaw = Mathf.Sin(time * 0.22f) * DriftYawDegrees;
            float slide = Mathf.Sin(time * 0.17f) * DriftSlideMetres;
            Quaternion q = Quaternion.AngleAxis(yaw, Vector3.up);
            Vector3 eye = p.Eye + q * new Vector3(slide, 0f, 0f);
            Vector3 f = (q * p.Forward).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, f).normalized;
            Vector3 up = Vector3.Cross(f, right);
            return new MenuPose(eye, f, right, up, FovYDegrees);
        }

        public static Vector3 World(float depth, float xFrac, float yFrac, float aspect)
        {
            var p = Pose();
            float tan = Mathf.Tan(FovYDegrees * 0.5f * Mathf.Deg2Rad);
            float halfH = depth * tan;
            float halfW = halfH * Mathf.Max(0.2f, aspect);
            return p.Eye + p.Forward * depth + p.Right * (xFrac * halfW) + p.Up * (yFrac * halfH);
        }

        public static Vector3 World(MenuAnchor a, float aspect) => World(a.Depth, a.XFrac, a.YFrac, aspect);

        public static Vector2 WorldSize(MenuAnchor a, float aspect)
        {
            float tan = Mathf.Tan(FovYDegrees * 0.5f * Mathf.Deg2Rad);
            float fullH = 2f * a.Depth * tan;
            float fullW = fullH * Mathf.Max(0.2f, aspect);
            return new Vector2(a.WidthFrac * fullW, a.HeightFrac * fullH);
        }

        public static Vector2 Viewport(Vector3 world, MenuPose pose, float aspect)
        {
            Vector3 d = world - pose.Eye;
            float x = Vector3.Dot(pose.Right, d);
            float y = Vector3.Dot(pose.Up, d);
            float z = Vector3.Dot(pose.Forward, d);
            float tan = Mathf.Tan(pose.FovY * 0.5f * Mathf.Deg2Rad);
            if (z < 1e-3f) return new Vector2(-1f, -1f);
            return new Vector2(
                0.5f + (x / z) / (2f * tan * Mathf.Max(0.2f, aspect)),
                0.5f + (y / z) / (2f * tan));
        }

        /// <summary>Rest-pose screen box. Independent of aspect because anchors are view fractions.</summary>
        public static ScreenBox RestBox(MenuAnchor a)
        {
            float cx = 0.5f + 0.5f * a.XFrac;
            float cy = 0.5f + 0.5f * a.YFrac;
            return new ScreenBox(cx - a.WidthFrac * 0.5f, cx + a.WidthFrac * 0.5f, cy - a.HeightFrac * 0.5f, cy + a.HeightFrac * 0.5f);
        }

        public static float RocketScale(float aspect)
        {
            Vector3 b = World(RocketDepth, 0f, RocketBaseYFrac, aspect);
            Vector3 n = World(RocketDepth, 0f, RocketNoseYFrac, aspect);
            float dy = n.y - b.y;
            return dy > 0.01f ? dy / VehicleHeightM : 0.03f;
        }

        public static Vector3 RocketBase(float aspect) => World(RocketDepth, 0f, RocketBaseYFrac, aspect);

        /// <summary>Soft breathe, one cycle every 2.6 s. A held value when Reduce Motion or Reduce Flashing is on.</summary>
        public static float Pulse(float time, bool reduceMotion)
        {
            if (reduceMotion) return 0.72f;
            return 0.72f + 0.28f * Mathf.Sin(time * (Mathf.PI * 2f / 2.6f));
        }

        public static float TitleGlow(float time, bool reduceMotion)
        {
            if (reduceMotion) return 0.5f;
            return 0.38f + 0.22f * Mathf.Sin(time * 1.15f);
        }

        /// <summary>Dial spin in degrees. direction is +1 or -1. Zero while reduced.</summary>
        public static float DialAngle(float time, bool reduceMotion, float direction)
        {
            if (reduceMotion) return 0f;
            return direction * 12f * time;
        }

        public static float Approach(float current, float target, float dt, bool reduceMotion)
        {
            if (reduceMotion || dt <= 0f) return target;
            float k = 1f - Mathf.Exp(-8f * dt);
            return current + (target - current) * k;
        }

        public static void GlassVisual(bool live, bool hover, out float alpha, out float glow)
        {
            if (!live) { alpha = hover ? 0.30f : 0.18f; glow = hover ? 0.55f : 0.16f; return; }
            alpha = hover ? 0.58f : 0.32f;
            glow = hover ? 2.1f : 0.45f;
        }

        public static MenuTextStyle Style(MenuTextRole role)
        {
            switch (role)
            {
                case MenuTextRole.Title: return S(84f, 8f, 0.06f, 0.45f, 0.55f);
                case MenuTextRole.Subtitle: return S(40f, 6f, 0.04f, 0.22f, 0.35f);
                case MenuTextRole.Launch: return S(64f, 4f, 0.05f, 0.28f, 0.50f);
                case MenuTextRole.Glass: return S(52f, 4f, 0.04f, 0.16f, 0.32f);
                case MenuTextRole.Hud: return S(44f, 2f, 0.05f, 0.30f, 0.45f);
                default: return S(72f, 8f, 0.06f, 0.40f, 0.60f);
            }
        }

        static MenuTextStyle S(float size, float spacing, float outline, float glowOuter, float glowPower) =>
            new MenuTextStyle(size, spacing, outline, 0f, glowOuter, glowPower, true, false, false, false, false, false);

        public static string DataLine(string key, string value) => key + ": " + value;

        /// <summary>Live-looking pad numbers. The sine is a few counts so the HUD is not a frozen texture.
        /// Reduced motion holds the canonical values (fuel 92%, thrust 850 kN).</summary>
        public static MenuReadout Readout(float time, bool reduceMotion)
        {
            float fuel = reduceMotion ? 0.92f : 0.92f + 0.008f * Mathf.Sin(time * 0.65f);
            float lox = reduceMotion ? 0.97f : 0.97f + 0.004f * Mathf.Sin(time * 0.5f + 1.2f);
            int thrust = reduceMotion ? 850 : 850 + Mathf.RoundToInt(2f * Mathf.Sin(time * 0.8f));
            float wind = reduceMotion ? 4f : 4f + 0.2f * Mathf.Sin(time * 0.4f);
            var ci = CultureInfo.InvariantCulture;
            return new MenuReadout(
                DataLine("FUEL", Mathf.RoundToInt(fuel * 100f).ToString(ci) + "%"),
                DataLine("LOX", Mathf.RoundToInt(lox * 100f).ToString(ci) + "%"),
                DataLine("MASS", "1,142 t"),
                DataLine("THRUST", thrust.ToString(ci) + " kN"),
                DataLine("WIND", wind.ToString("0.0", ci) + " m/s"),
                DataLine("STATUS", "GO"),
                Designation);
        }
    }
}

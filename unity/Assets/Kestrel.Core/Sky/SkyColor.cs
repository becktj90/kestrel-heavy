// Altitude → sky colour. One USSA76 sample (Atmosphere.At, the NASA standard atmosphere
// already used for drag) plus the VISUAL_SPEC §4 layer alphas. No second density table.
// Colours are linear light, 0..1 after clamp. The skybox gamma-encodes them for this project's
// gamma color space. Engine-free so EditMode and dotnet share the tests.
using System;
using Kestrel.Core.Physics;
using Kestrel.Core.Weather;

namespace Kestrel.Core.Sky
{
    /// <summary>Which density profile the sky reads. Only USSA76 is implemented.</summary>
    public enum SkyAtmosphereMode
    {
        /// <summary>U.S. Standard Atmosphere 1976, 0–1000 km, via Atmosphere.At. NASA/NOAA standard. Jacchia-71 is not a second table.</summary>
        Ussa76 = 0,
    }

    public struct Rgb
    {
        public double R, G, B;
        public Rgb(double r, double g, double b) { R = r; G = g; B = b; }

        public double Luminance => 0.2126 * R + 0.7152 * G + 0.0722 * B;
        public double Max => R > G ? (R > B ? R : B) : (G > B ? G : B);
        public double Min => R < G ? (R < B ? R : B) : (G < B ? G : B);

        /// <summary>0 white .. 1 a single channel. 0 when the colour is black.</summary>
        public double Saturation { get { double m = Max; return m <= 1e-8 ? 0 : (m - Min) / m; } }

        public Rgb Clamped()
        {
            return new Rgb(SkyColor.Clamp01(R), SkyColor.Clamp01(G), SkyColor.Clamp01(B));
        }

        public static Rgb Lerp(Rgb a, Rgb b, double t)
        {
            double k = SkyColor.Clamp01(t);
            return new Rgb(a.R + (b.R - a.R) * k, a.G + (b.G - a.G) * k, a.B + (b.B - a.B) * k);
        }

        public static Rgb operator *(Rgb c, double s) => new Rgb(c.R * s, c.G * s, c.B * s);

        public static double SrgbToLinear(double c)
        {
            if (c <= 0.04045) return c / 12.92;
            return Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        public static double LinearToSrgb(double c)
        {
            c = SkyColor.Clamp01(c);
            if (c <= 0.0031308) return 12.92 * c;
            return 1.055 * Math.Pow(c, 1.0 / 2.4) - 0.055;
        }

        public static Rgb FromSrgb(double r, double g, double b)
            => new Rgb(SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b));

        public static Rgb FromHex(int rgb)
            => FromSrgb(((rgb >> 16) & 255) / 255.0, ((rgb >> 8) & 255) / 255.0, (rgb & 255) / 255.0);

        public static Rgb FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) throw new ArgumentException("empty colour");
            return FromHex(Convert.ToInt32(hex.Trim().TrimStart('#'), 16));
        }

        public Rgb ToSrgb() => new Rgb(LinearToSrgb(R), LinearToSrgb(G), LinearToSrgb(B));
    }

    /// <summary>Art-direction wash. Runtime fills this from data/tod.json; tests pass a fixture.</summary>
    public struct SkyArt
    {
        public Rgb Horizon, Near, Space;
        /// <summary>tod.js palette.stars. Stars visible at night on the ground, before the altitude fade.</summary>
        public double StarFloor;

        /// <summary>Neutral daylight wash used only when tod.json is not loaded. Not a copy of the stop list.</summary>
        public static SkyArt Neutral => new SkyArt
        {
            Horizon = Rgb.FromSrgb(0.72, 0.84, 0.93),
            Near = Rgb.FromSrgb(0.22, 0.42, 0.68),
            Space = Rgb.FromSrgb(0.01, 0.012, 0.02),
            StarFloor = 0.08,
        };
    }

    /// <summary>Visual weather the sky actually reads. Maps the WeatherDay hook; does not generate weather.</summary>
    public struct SkyWeather
    {
        public double CloudFraction;
        public double CeilingM;
        public bool Precip;
        public bool Thunder;
        public double WindDeg;
        public double WindMs;

        /// <summary>A bright day with a few soft decks, so the pad is not a bare gradient.</summary>
        public static SkyWeather Clear => new SkyWeather
        {
            CloudFraction = 0.34,
            CeilingM = 8200,
            Precip = false,
            Thunder = false,
            WindDeg = 110,
            WindMs = 4.5,
        };

        public static SkyWeather From(WeatherDay day)
        {
            double cloud = day.cloudFraction;
            if (day.thunderDay) cloud = Math.Min(1, cloud + 0.12);
            return new SkyWeather
            {
                CloudFraction = cloud,
                CeilingM = day.ceilingM,
                Precip = day.precip,
                Thunder = day.thunderDay,
                WindDeg = day.directionDeg,
                WindMs = day.windMs,
            };
        }
    }

    public struct SkyGrade
    {
        public Rgb Zenith, Horizon, Limb, Ground, Fog, Sun;
        public double StarAlpha, CloudAlpha, Haze, Milky, Airglow, SunDisc, SunGlow;
        public double Column, SpaceT, SunElevationDeg;
        public double FogStartM, FogEndM;
        /// <summary>Three soft decks, low to high. Alpha already includes the altitude fade.</summary>
        public double Cloud0, Cloud1, Cloud2, Height0, Height1, Height2;
        public double WindEast, WindNorth;
        public SkyAtmosphereMode Mode;
    }

    public static class SkyColor
    {
        public const SkyAtmosphereMode Mode = SkyAtmosphereMode.Ussa76;

        // Sea-level zenith optical depth. Blue is deeper so the zenith stays blue while the horizon goes white.
        const double TauR = 0.055, TauG = 0.13, TauB = 0.34, ScatterGain = 3.35;

        public static double Clamp01(double x)
        {
            if (double.IsNaN(x)) return 0;
            if (x < 0) return 0;
            if (x > 1) return 1;
            return x;
        }

        public static double Smoothstep(double e0, double e1, double x)
        {
            double span = e1 - e0;
            if (span == 0) return x >= e1 ? 1 : 0;
            double t = Clamp01((x - e0) / span);
            return t * t * (3 - 2 * t);
        }

        public static double Lerp(double a, double b, double t) { double k = Clamp01(t); return a + (b - a) * k; }

        /// <summary>Portrait-only product: taller than wide, square counts as portrait.</summary>
        public static bool IsPortrait(double widthPx, double heightPx) => heightPx >= widthPx;

        /// <summary>
        /// VISUAL_SPEC §4 spaceBlend: smoothstep(6, 56 km). SEP, HAVEN and JACKLYN (the C# port's name) force 1.
        /// </summary>
        public static double SpaceBlend(double altKm, string status = null)
        {
            if (status == "SEP" || status == "HAVEN" || status == "JACKLYN") return 1;
            return Smoothstep(6, 56, altKm);
        }

        /// <summary>Hydrostatic column, p(z)/p(0), from the active atmosphere mode. 1 at the pad, ~0 in orbit.</summary>
        public static double Column(double altitudeM)
        {
            double z = double.IsNaN(altitudeM) ? 0 : Math.Max(0, altitudeM);
            double p0 = Atmosphere.At(0).p;
            if (p0 <= 0) return 0;
            return Clamp01(Atmosphere.At(z).p / p0);
        }

        /// <summary>Relative airmass. mu = 1 is the zenith, 0 is the horizon. Scales with the column above the vehicle.</summary>
        public static double Airmass(double column, double mu)
        {
            double sec = 1.0 / Math.Max(Clamp01(mu), 0.09);
            return Math.Max(0, column) * sec;
        }

        public static SkyGrade At(double altitudeM, double sunElevationRad, SkyArt art, SkyWeather weather, string status = null)
        {
            double z = double.IsNaN(altitudeM) ? 0 : Math.Max(0, altitudeM);
            double column = Column(z);
            double spaceT = SpaceBlend(z / 1000.0, status);
            double elevDeg = double.IsNaN(sunElevationRad) ? 0 : sunElevationRad * (180.0 / Math.PI);
            double day = 1.0 - SkySun.NightFactor((float)elevDeg);
            double skyAlpha = 1.0 - spaceT;

            var zenith = Shade(column, 1.0, day, art, weather) * skyAlpha;
            var horizon = Shade(column, 0.0, day, art, weather) * skyAlpha;
            var limb = Shade(column, 0.12, day, art, weather);
            double warm = 1.0 - Smoothstep(0.5, 18, Math.Max(-2, elevDeg));
            limb = Rgb.Lerp(limb, Rgb.Lerp(limb, new Rgb(1, 0.42, 0.10), 0.72), warm * day);
            limb = (limb * skyAlpha).Clamped();
            zenith = zenith.Clamped();
            horizon = horizon.Clamped();
            var ground = (horizon * 0.42).Clamped();

            double cover = CloudCover(z, spaceT, weather);
            double stars = Lerp(Clamp01(art.StarFloor) * (1.0 - day), 1.0, spaceT);
            stars *= 1.0 - 0.75 * cover;
            if (weather.Precip) stars *= 0.45;
            stars = Clamp01(stars);

            // Column term stays under 1 so cloud and precip can still thicken the pad haze.
            double haze = (1.0 - Smoothstep(0, 0.72, spaceT)) * (0.22 + 0.48 * column);
            if (weather.Precip) haze += 0.28;
            haze += 0.18 * Clamp01(weather.CloudFraction) * (1.0 - spaceT);
            haze = Clamp01(haze);

            double sunDisc = Smoothstep(-0.7, 0.45, elevDeg);
            double sunGlow = Smoothstep(-8, 3, elevDeg) * (1.0 - 0.72 * Clamp01(weather.CloudFraction));
            if (weather.Precip) sunGlow *= 0.35;
            var sun = Rgb.Lerp(new Rgb(1, 0.97, 0.90), new Rgb(1, 0.38, 0.07), Clamp01(warm));
            sun = (sun * (0.35 + 0.65 * sunDisc) * (0.25 + 0.75 * day)).Clamped();

            Decks(z, spaceT, weather, out var h0, out var h1, out var h2, out var a0, out var a1, out var a2);
            double wr = weather.WindDeg * Math.PI / 180.0;
            double milky = Smoothstep(0.18, 0.9, spaceT) * 0.74 * (1.0 - 0.6 * cover);
            double air = Math.Exp(-Math.Pow((z / 1000.0 - 90.0) / 28.0, 2)) * (0.2 + 0.8 * spaceT) * 0.35;

            return new SkyGrade
            {
                Zenith = zenith,
                Horizon = horizon,
                Limb = limb,
                Ground = ground,
                Fog = horizon,
                Sun = sun,
                StarAlpha = stars,
                CloudAlpha = cover,
                Haze = haze,
                Milky = Clamp01(milky),
                Airglow = Clamp01(air),
                SunDisc = sunDisc,
                SunGlow = Clamp01(sunGlow),
                Column = column,
                SpaceT = spaceT,
                SunElevationDeg = elevDeg,
                FogStartM = Lerp(5000, 150000, 1 - haze),
                FogEndM = Lerp(72000, 195000, 1 - haze),
                Cloud0 = a0, Cloud1 = a1, Cloud2 = a2,
                Height0 = h0, Height1 = h1, Height2 = h2,
                // Meteorological "from" direction; the shader scrolls clouds along the travel vector.
                WindEast = -weather.WindMs * Math.Sin(wr),
                WindNorth = -weather.WindMs * Math.Cos(wr),
                Mode = Mode,
            };
        }

        /// <summary>
        /// Gradient only (no sun disc, no clouds). mu = 1 zenith, 0 horizon, negative looks below the horizon.
        /// Matches the skybox lerp so a direction has one colour on iPhone and iPad.
        /// </summary>
        public static Rgb View(SkyGrade g, double mu)
        {
            double u = Clamp01(mu);
            double k = Math.Pow(1 - u, 0.62);
            var c = Rgb.Lerp(g.Zenith, g.Horizon, k);
            double limb = Math.Exp(-u * 10.0);
            c = Rgb.Lerp(c, g.Limb, limb * g.Haze * 0.45);
            if (mu < 0)
            {
                double below = Smoothstep(0, 0.22, -mu);
                c = Rgb.Lerp(c, g.Ground, below);
            }
            return c.Clamped();
        }

        static Rgb Scatter(double airmass)
        {
            double r = (1 - Math.Exp(-TauR * airmass)) * ScatterGain;
            double g = (1 - Math.Exp(-TauG * airmass)) * ScatterGain;
            double b = (1 - Math.Exp(-TauB * airmass)) * ScatterGain;
            return new Rgb(r, g, b);
        }

        static Rgb Shade(double column, double mu, double day, SkyArt art, SkyWeather weather)
        {
            double muC = Clamp01(mu);
            // Clamp before the art mix so a long horizon path stays a tinted white, not an un-tintable blowout.
            var scatter = Scatter(Airmass(column, muC)).Clamped() * (0.045 + 0.955 * day);
            var artCol = Rgb.Lerp(art.Near, art.Horizon, Math.Pow(1 - muC, 0.7));
            double artW = Lerp(0.2, 0.5, 1 - muC);
            // Twilight leans on the wash. Full night (day = 0) does not, or the zenith stays a daylight blue.
            double twilight = 4.0 * day * (1.0 - day);
            artW = Lerp(artW, 0.78, twilight);
            var col = Rgb.Lerp(scatter, artCol, artW);
            col = col * Lerp(0.06, 1.0, day);
            double over = Clamp01((weather.CloudFraction - 0.4) / 0.6);
            double y = Math.Max(0, col.Luminance);
            col = Rgb.Lerp(col, new Rgb(y, y, y) * 0.92, over * 0.5);
            if (weather.Precip) col = Rgb.Lerp(col, new Rgb(y * 0.78, y * 0.82, y * 0.88), 0.4);
            // In-scatter dies with the column. Art must die with it or a palette would stay lit in orbit.
            double presence = 1 - Math.Exp(-5.2 * Math.Max(Airmass(column, muC), column * 0.25));
            return col * presence;
        }

        public static double CloudCover(double altitudeM, double spaceT, SkyWeather weather)
        {
            Decks(altitudeM, spaceT, weather, out _, out _, out _, out var a0, out var a1, out var a2);
            return Clamp01(Math.Max(a0, Math.Max(a1, a2)));
        }

        static void Decks(double altitudeM, double spaceT, SkyWeather weather,
            out double h0, out double h1, out double h2, out double a0, out double a1, out double a2)
        {
            double ceil = weather.CeilingM;
            if (double.IsNaN(ceil) || ceil < 400) ceil = 2500;
            if (ceil > 13000) ceil = 13000;
            h0 = ceil * 0.48;
            h1 = ceil * 0.78;
            h2 = Math.Min(12500, ceil * 1.15);
            double layer = 1 - Smoothstep(0.04, 0.58, spaceT);
            double cover = Clamp01(weather.CloudFraction);
            if (weather.Precip) cover = Math.Max(cover, 0.72);
            a0 = cover * layer * Above(altitudeM, h0, 700);
            a1 = cover * 0.82 * layer * Above(altitudeM, h1, 900);
            a2 = cover * 0.5 * layer * Above(altitudeM, h2, 1100);
        }

        /// <summary>1 below the deck, 0 once the vehicle has climbed through it.</summary>
        static double Above(double altitudeM, double height, double thick)
            => 1 - Smoothstep(height - thick * 0.25, height + thick, Math.Max(0, altitudeM));
    }
}

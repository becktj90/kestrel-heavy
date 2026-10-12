// Flight sky. Replaces the flat camera clear with one URP skybox. The grade is SkyColor
// (USSA76 column + VISUAL_SPEC alphas). Stars are the public-domain BSC catalogue.
// Sun elevation is published on SkySun so pad lighting can read it without a reference back.
using System;
using System.IO;
using Kestrel.Core.Physics;
using Kestrel.Core.Sky;
using Kestrel.Core.Weather;
using Kestrel.Game.Shell;
using UnityEngine;

namespace Kestrel.Game.Environment
{
    [DisallowMultipleComponent]
    public sealed class SkyRig : MonoBehaviour
    {
        /// <summary>Degrees. Same value as SkySun.ElevationDeg. Pad: Night = PadLighting.NightFactor(SkyRig.SunElevationDeg).</summary>
        public static float SunElevationDeg => SkySun.ElevationDeg;

        public const string ShaderResource = "Shaders/KH_Sky";

        public Camera View;
        public Light SunLight;
        public string Tier = "medium";
        public string TodId;
        public SkyArt Art = SkyArt.Neutral;
        public SkyWeather Weather = SkyWeather.Clear;
        public double LatitudeDeg = SkyClock.LatitudeDeg;
        public double LongitudeDeg = SkyClock.LongitudeDeg;
        public int Year = SkyClock.Year, Month = SkyClock.Month, Day = SkyClock.Day;
        public double HoursUtc = SkyClock.HoursUtc;

        Material _mat;
        Texture2D _stars;
        string _bakedTier;
        double _altitudeM;
        bool _artExplicit, _artReady, _sunReady;
        double _elev;
        SkyVec _sunDir;

        static readonly int ZenithId = Shader.PropertyToID("_Zenith");
        static readonly int HorizonId = Shader.PropertyToID("_Horizon");
        static readonly int LimbId = Shader.PropertyToID("_Limb");
        static readonly int GroundId = Shader.PropertyToID("_Ground");
        static readonly int SunColorId = Shader.PropertyToID("_SunColor");
        static readonly int SunDirId = Shader.PropertyToID("_SunDir");
        static readonly int ParamsId = Shader.PropertyToID("_Params");
        static readonly int MilkyId = Shader.PropertyToID("_Milky");
        static readonly int CloudAId = Shader.PropertyToID("_CloudA");
        static readonly int CloudHId = Shader.PropertyToID("_CloudH");
        static readonly int WindId = Shader.PropertyToID("_Wind");
        static readonly int Eq0Id = Shader.PropertyToID("_Eq0");
        static readonly int Eq1Id = Shader.PropertyToID("_Eq1");
        static readonly int Eq2Id = Shader.PropertyToID("_Eq2");
        static readonly int GalNId = Shader.PropertyToID("_GalN");
        static readonly int StarsId = Shader.PropertyToID("_Stars");

        public SkyGrade Grade { get; private set; }

        public static SkyRig Attach(Camera cam, Light sun = null)
        {
            if (cam == null) return null;
            var rig = cam.GetComponent<SkyRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<SkyRig>();
            rig.View = cam;
            rig.SunLight = sun;
            rig.PickTier();
            rig.LoadArt();
            rig.LoadWeather();
            rig.Refresh();
            return rig;
        }

        /// <summary>Geometric altitude in metres. The flight loop calls this every frame.</summary>
        public void SetAltitude(double altitudeM)
        {
            _altitudeM = altitudeM;
            Refresh();
        }

        /// <summary>WeatherDay hook. Thunder only thickens the decks; there is no flash.</summary>
        public void SetWeather(WeatherDay day)
        {
            Weather = SkyWeather.From(day);
            Refresh();
        }

        public void SetTod(string todId)
        {
            TodId = todId;
            _artExplicit = true;
            _artReady = false;
            Refresh();
        }

        void PickTier()
        {
            int mem = SystemInfo.systemMemorySize;
            if (mem <= 0) Tier = "medium";
            else if (mem < 3500) Tier = "low";
            else if (mem >= 5500) Tier = "high";
            else Tier = "medium";
        }

        void LoadArt()
        {
            if (string.IsNullOrEmpty(TodId) && !_artExplicit) TodId = null;
            try
            {
                var spec = VisualSpecData.Load();
                string id = _artExplicit && !string.IsNullOrEmpty(TodId) ? TodId : TodForSun();
                var p = spec.Palette(id);
                var w = p.wash;
                Art = new SkyArt
                {
                    Horizon = Rgb.FromHex(w.horizon),
                    Near = Rgb.FromHex(w.near),
                    Space = Rgb.FromHex(w.space),
                    StarFloor = p.stars,
                };
                TodId = id;
            }
            catch (Exception)
            {
                if (Art.StarFloor <= 0 && Art.Horizon.B <= 0) Art = SkyArt.Neutral;
            }
        }

        string TodForSun()
        {
            double deg = SkyBodies.SunElevationRad(LatitudeDeg, LongitudeDeg, Year, Month, Day, HoursUtc) * SkyBodies.Rad2Deg;
            if (deg < -4) return "night";
            if (deg < 6) return "dusk";
            if (deg < 16) return "golden";
            if (deg < 28 && HoursUtc < 15) return "dawn";
            return "noon";
        }

        void LoadWeather()
        {
            try
            {
                int doy = DayOfYear(Year, Month, Day);
                Weather = SkyWeather.From(WeatherGenerator.Generate("haven", doy, 1u));
            }
            catch (Exception)
            {
                Weather = SkyWeather.Clear;
            }
        }

        static int DayOfYear(int year, int month, int day)
        {
            int[] md = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };
            int m = month < 1 ? 1 : (month > 12 ? 12 : month);
            int d = md[m - 1] + day;
            bool leap = (year % 4 == 0 && year % 100 != 0) || year % 400 == 0;
            if (leap && m > 2) d += 1;
            if (d < 1) d = 1;
            if (d > 365) d = 365;
            return d;
        }

        public void Refresh()
        {
            EnsureMaterial();
            if (!_sunReady)
            {
                _elev = SkyBodies.SunElevationRad(LatitudeDeg, LongitudeDeg, Year, Month, Day, HoursUtc);
                _sunDir = SkyBodies.SunUnity(LatitudeDeg, LongitudeDeg, Year, Month, Day, HoursUtc);
                _sunReady = true;
            }
            SkySun.Publish(_elev);
            if (!_artReady)
            {
                LoadArt();
                _artReady = true;
            }
            Grade = SkyColor.At(_altitudeM, _elev, Art, Weather, null);
            Apply(Grade, _sunDir);
        }

        void EnsureMaterial()
        {
            if (_mat != null) return;
            var shader = Resources.Load<Shader>(ShaderResource);
            if (shader == null) shader = Shader.Find("Kestrel/Sky");
            if (shader == null)
            {
                Debug.LogError("[Kestrel] Kestrel/Sky shader missing");
                return;
            }
            _mat = new Material(shader) { name = "KH_Sky" };
            _mat.hideFlags = HideFlags.HideAndDontSave;
            RenderSettings.skybox = _mat;
            if (View != null) View.clearFlags = CameraClearFlags.Skybox;
            BakeStars();
        }

        void BakeStars()
        {
            if (_stars != null && _bakedTier == Tier) return;
            string csv = ReadCatalog();
            if (string.IsNullOrEmpty(csv)) return;
            var stars = SkyBodies.LoadStars(csv, Tier);
            if (_stars != null) Destroy(_stars);
            _stars = StarBake.Bake(stars, Tier == "low" ? 256 : 512, Tier == "low" ? 128 : 256);
            _bakedTier = Tier;
            if (_mat != null && _stars != null) _mat.SetTexture(StarsId, _stars);
        }

        static string ReadCatalog()
        {
            var asset = Resources.Load<TextAsset>("KestrelData/bsc5p-compact");
            if (asset != null && !string.IsNullOrEmpty(asset.text)) return asset.text;
            try
            {
                string root = Kestrel.Core.Data.Catalog.FindRepoRoot(Application.dataPath);
                string path = Path.Combine(root, "data", "astro", "bsc5p-compact.csv");
                if (File.Exists(path)) return File.ReadAllText(path);
            }
            catch (Exception) { }
            string streaming = Path.Combine(Application.streamingAssetsPath, "bsc5p-compact.csv");
            try { if (File.Exists(streaming)) return File.ReadAllText(streaming); }
            catch (Exception) { }
            return null;
        }

        void Apply(SkyGrade g, SkyVec sun)
        {
            var horizon = Display(g.Horizon);
            if (_mat == null)
            {
                if (View != null)
                {
                    View.clearFlags = CameraClearFlags.SolidColor;
                    View.backgroundColor = horizon;
                }
                ApplyFog(g, horizon);
                AimSun(sun, g);
                return;
            }
            _mat.SetColor(ZenithId, Display(g.Zenith));
            _mat.SetColor(HorizonId, horizon);
            _mat.SetColor(LimbId, Display(g.Limb));
            _mat.SetColor(GroundId, Display(g.Ground));
            _mat.SetColor(SunColorId, Display(g.Sun));
            _mat.SetVector(SunDirId, new Vector4((float)sun.X, (float)sun.Y, (float)sun.Z, (float)g.SunDisc));
            _mat.SetVector(ParamsId, new Vector4((float)g.Haze, (float)g.StarAlpha, (float)g.SunGlow, (float)g.SunDisc));
            _mat.SetVector(MilkyId, new Vector4((float)g.Milky, (float)g.Airglow, Time.time, _stars != null ? 1f : 0f));
            _mat.SetVector(CloudAId, new Vector4((float)g.Cloud0, (float)g.Cloud1, (float)g.Cloud2, (float)g.CloudAlpha));
            _mat.SetVector(CloudHId, new Vector4((float)g.Height0, (float)g.Height1, (float)g.Height2, (float)_altitudeM));
            _mat.SetVector(WindId, new Vector4((float)g.WindEast, (float)g.WindNorth, 0.00012f, Weather.Precip ? 1f : 0f));
            Frames.JdUt1Parts(Year, Month, Day, HoursUtc, out var jd1, out var jd2);
            SkyBodies.EquatorialAxes(LatitudeDeg, LongitudeDeg, jd1, jd2, out var ax, out var ay, out var az);
            _mat.SetVector(Eq0Id, Vec(ax));
            _mat.SetVector(Eq1Id, Vec(ay));
            _mat.SetVector(Eq2Id, Vec(az));
            _mat.SetVector(GalNId, Vec(SkyBodies.GalacticNorthEquatorial()));
            if (_stars != null) _mat.SetTexture(StarsId, _stars);
            if (View != null)
            {
                View.clearFlags = CameraClearFlags.Skybox;
                View.backgroundColor = horizon;
            }
            RenderSettings.skybox = _mat;
            ApplyFog(g, horizon);
            AimSun(sun, g);
            float day = 1f - SkySun.NightFactor();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Display(g.Zenith);
            RenderSettings.ambientEquatorColor = horizon;
            RenderSettings.ambientGroundColor = Display(g.Ground);
            RenderSettings.ambientIntensity = Mathf.Lerp(0.15f, 1f, day);
        }

        static void ApplyFog(SkyGrade g, Color horizon)
        {
            // Distance haze. Colour tracks the sky horizon so the ground's far edge melts into it.
            // PadGround's one-shot setup is left in place as the fallback when this rig is absent.
            RenderSettings.fog = g.Haze > 0.02;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = horizon;
            RenderSettings.fogStartDistance = (float)g.FogStartM;
            RenderSettings.fogEndDistance = (float)g.FogEndM;
        }

        void AimSun(SkyVec sun, SkyGrade g)
        {
            if (SunLight == null) return;
            var dir = new Vector3((float)sun.X, (float)sun.Y, (float)sun.Z);
            if (dir.sqrMagnitude < 1e-8f) dir = Vector3.up;
            SunLight.transform.rotation = Quaternion.LookRotation(-dir.normalized);
            SunLight.color = Display(g.Sun);
            float day = 1f - SkySun.NightFactor();
            // In thin air the sun is harder, not dimmer. Night drops it.
            float air = Mathf.Exp(-(float)g.Column * 0.6f);
            SunLight.intensity = Mathf.Lerp(0.02f, Mathf.Lerp(1.05f, 1.35f, air), day) * Mathf.Lerp(0.35f, 1f, (float)g.SunDisc + 0.15f);
            RenderSettings.sun = SunLight;
        }

        static Vector4 Vec(SkyVec v) => new Vector4((float)v.X, (float)v.Y, (float)v.Z, 0);

        static Color Display(Rgb c)
        {
            var s = c.ToSrgb();
            return new Color((float)s.R, (float)s.G, (float)s.B, 1f);
        }

        void OnDestroy()
        {
            if (_stars != null) Destroy(_stars);
            if (_mat != null) Destroy(_mat);
        }
    }
}

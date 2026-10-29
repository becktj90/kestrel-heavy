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
    [DefaultExecutionOrder(10000)]   // LateUpdate after the flight / countdown camera has its pose for the frame
    public sealed class SkyRig : MonoBehaviour
    {
        /// <summary>Degrees. Same value as SkySun.ElevationDeg. Pad: Night = PadLighting.NightFactor(SkyRig.SunElevationDeg).</summary>
        public static float SunElevationDeg => SkySun.ElevationDeg;

        /// <summary>
        /// Force the sky and the published sun elevation from a local civil hour at the launch longitude.
        /// -khNight calls this with SkyBodies.NightHoursLocal. PadLighting.NightFactor(SkySun.ElevationDeg) follows.
        /// </summary>
        public static void ForceTimeOfDay(double hoursLocal)
        {
            _forced = true;
            _forcedLocal = hoursLocal;
            var rig = _active;
            double lat = rig != null ? rig.LatitudeDeg : SkyClock.LatitudeDeg;
            double lon = rig != null ? rig.LongitudeDeg : SkyClock.LongitudeDeg;
            int y = rig != null ? rig.Year : SkyClock.Year;
            int mo = rig != null ? rig.Month : SkyClock.Month;
            int d = rig != null ? rig.Day : SkyClock.Day;
            SkySun.ApplyLocalHours(hoursLocal, lat, lon, y, mo, d);
            if (rig == null) return;
            rig.HoursUtc = SkyBodies.UtcFromLocalHours(hoursLocal, lon);
            rig._sunReady = false;
            rig._artReady = false;
            rig.Refresh();
        }

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
        double _clockKey = double.NaN;
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
        static readonly int StarTexId = Shader.PropertyToID("_StarTex");
        static readonly int StarsId = Shader.PropertyToID("_Stars");

        static bool _forced, _cliChecked;
        static double _forcedLocal;
        static SkyRig _active;

        public SkyGrade Grade { get; private set; }

        public static SkyRig Attach(Camera cam, Light sun = null)
        {
            if (cam == null) return null;
            var rig = cam.GetComponent<SkyRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<SkyRig>();
            rig.View = cam;
            rig.SunLight = sun;
            rig.PickTier();
            rig.LoadWeather();
            rig.SuppressSkyPost();
            rig.Refresh();
            return rig;
        }

        void OnEnable() => _active = this;

        void OnDisable()
        {
            if (_active == this) _active = null;
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
            ApplyCommandLine();
            EnsureMaterial();
            SuppressSkyPost();
            bool newClock = SampleSun();
            SkySun.Publish(_elev);
            if (newClock) _artReady = false;
            if (!_artReady)
            {
                LoadArt();
                _artReady = true;
            }
            Grade = SkyColor.At(_altitudeM, _elev, Art, Weather, null);
            Apply(Grade, _sunDir);
        }

        static void ApplyCommandLine()
        {
            if (_cliChecked) return;
            _cliChecked = true;
            if (SkyBodies.HasNightArg(System.Environment.GetCommandLineArgs()))
                ForceTimeOfDay(SkyBodies.NightHoursLocal);
        }

        /// <summary>True when the clock changed, so the wash can follow day and night.</summary>
        bool SampleSun()
        {
            if (_forced) HoursUtc = SkyBodies.UtcFromLocalHours(_forcedLocal, LongitudeDeg);
            double key = HoursUtc + Day * 48.0 + Month * 2000.0 + Year * 40000.0 + LatitudeDeg * 0.01 + LongitudeDeg * 0.0001;
            if (_sunReady && key == _clockKey) return false;
            _clockKey = key;
            _elev = SkyBodies.SunElevationRad(LatitudeDeg, LongitudeDeg, Year, Month, Day, HoursUtc);
            _sunDir = SkyBodies.SunUnity(LatitudeDeg, LongitudeDeg, Year, Month, Day, HoursUtc);
            _sunReady = true;
            return true;
        }

        /// <summary>The flight camera's colour buffer is the sky. Post (motion blur especially) must not run on it.</summary>
        void SuppressSkyPost()
        {
            if (View == null) return;
            const string typeName = "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime";
            var t = Type.GetType(typeName);
            if (t == null) return;
            var data = View.GetComponent(t);
            if (data == null) data = View.gameObject.AddComponent(t);
            if (data == null) return;
            SetMember(t, data, "renderPostProcessing", false);
            SetMember(t, data, "dithering", false);
            var aa = t.GetProperty("antialiasing");
            if (aa != null && aa.CanWrite && aa.PropertyType.IsEnum)
                aa.SetValue(data, Enum.ToObject(aa.PropertyType, 0), null);
        }

        static void SetMember(Type t, object target, string name, bool value)
        {
            var p = t.GetProperty(name);
            if (p != null && p.CanWrite && p.PropertyType == typeof(bool)) p.SetValue(target, value, null);
        }

        void EnsureMaterial()
        {
            if (_mat != null) return;
            var shader = Resources.Load<Shader>(ShaderResource);
            if (shader == null) shader = Shader.Find("Kestrel/Sky");
            if (IsErrorShader(shader))
            {
                Debug.LogError("[Kestrel] Kestrel/Sky shader missing");
                return;
            }
            _mat = new Material(shader) { name = "KH_Sky" };
            if (IsErrorShader(_mat.shader))
            {
                Destroy(_mat);
                _mat = null;
                Debug.LogError("[Kestrel] Kestrel/Sky shader failed; solid clear instead of magenta");
                return;
            }
            _mat.hideFlags = HideFlags.HideAndDontSave;
            RenderSettings.skybox = _mat;
            if (View != null) View.clearFlags = CameraClearFlags.SolidColor;
            BakeStars();
        }

        void BakeStars()
        {
            if (_stars != null && _bakedTier == Tier) return;
            string csv = ReadCatalog();
            if (string.IsNullOrEmpty(csv)) return;
            var stars = SkyBodies.LoadStars(csv, Tier);
            for (int i = stars.Count - 1; i >= 0; i--)
                if (stars[i].VMag > SkyBodies.VisualMaxMag) stars.RemoveAt(i);
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

        static bool IsErrorShader(Shader shader)
        {
            if (shader == null) return true;
            string n = shader.name;
            return !string.IsNullOrEmpty(n) && n.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void Apply(SkyGrade g, SkyVec sun)
        {
            if (_mat != null && IsErrorShader(_mat.shader))
            {
                Destroy(_mat);
                _mat = null;
            }
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
            if (_stars != null)
            {
                _mat.SetTexture(StarsId, _stars);
                _mat.SetVector(StarTexId, new Vector4(_stars.width, _stars.height, 0f, 0f));
            }
            if (View != null)
            {
                // Cleared to the horizon grade, then the sky is drawn as a camera-locked backdrop (DrawBackdrop), not
                // as Unity's skybox. Under a Skybox camera URP never clears colour (depth only on desktop, DontCare on
                // Metal/Vulkan), so every pixel the skybox draw missed kept stale or black contents: the black corner
                // wedges in the countdown / flight-phase / App Store captures (#348, #381).
                View.clearFlags = CameraClearFlags.SolidColor;
                View.backgroundColor = horizon;
                _clearColor = horizon;
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
            var col = new Color((float)s.R, (float)s.G, (float)s.B, 1f);
            // Missing-shader magenta must not become a clear colour or a fog colour.
            if (col.r > 0.9f && col.b > 0.9f && col.g < 0.2f) return Color.black;
            return col;
        }

        void OnDestroy()
        {
            if (_stars != null) Destroy(_stars);
            if (_mat != null) Destroy(_mat);
            if (_backdrop != null) Destroy(_backdrop);
        }

        // ---- Sky backdrop ----------------------------------------------------------------------------------------
        // Unity's skybox is a mesh around the camera whose faces straddle the camera plane (w = 0) at the frame edges;
        // a rasteriser that drops or mis-clips those triangles (llvmpipe in CI did, at the corners) leaves holes, and
        // URP does not clear colour under a skybox. The backdrop is one quad in front of the camera, just past the
        // near plane and wider than the frustum, so nothing about it is ever clipped. KH_Sky colours each pixel
        // from its view ray (dirWS = model rotation x object position), so the picture is the same sky.
        Mesh _backdrop;
        Color _clearColor = new Color(-1f, -1f, -1f, -1f);

        /// <summary>Margin past the frustum edge, so the quad still covers the frame while the FOV punch widens it.</summary>
        public const float BackdropMargin = 1.25f;

        void LateUpdate() => DrawBackdrop();

        void DrawBackdrop()
        {
            if (_mat == null || View == null || !View.isActiveAndEnabled || View.orthographic) return;
            // A test or a caller that set its own clear owns the camera: no sky over its colour.
            if (View.clearFlags != CameraClearFlags.SolidColor || View.backgroundColor != _clearColor) return;
            if (_backdrop == null) _backdrop = BackdropMesh();
            var t = View.transform;
            float d = Mathf.Max(1f, View.nearClipPlane * 2f);
            if (d >= View.farClipPlane * 0.5f) return;
            float ty = Mathf.Tan(0.5f * View.fieldOfView * Mathf.Deg2Rad) * BackdropMargin;
            float tx = ty * Mathf.Max(0.05f, View.aspect);
            var m = Matrix4x4.TRS(t.position, t.rotation, new Vector3(tx * d, ty * d, d));
            int layer = 0;
            int mask = View.cullingMask;
            if ((mask & 1) == 0) for (int i = 1; i < 32; i++) if ((mask & (1 << i)) != 0) { layer = i; break; }
            var rp = new RenderParams(_mat)
            {
                camera = View,
                layer = layer,
                worldBounds = new Bounds(t.position, Vector3.one * (4f * d * (1f + tx + ty))),
                shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                receiveShadows = false,
                lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off,
            };
            Graphics.RenderMesh(rp, _backdrop, 0, m);
        }

        static Mesh BackdropMesh()
        {
            var mesh = new Mesh { name = "KH_SkyBackdrop", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = new[] { new Vector3(-1f, -1f, 1f), new Vector3(1f, -1f, 1f), new Vector3(1f, 1f, 1f), new Vector3(-1f, 1f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.bounds = new Bounds(new Vector3(0f, 0f, 1f), new Vector3(2f, 2f, 0.1f));
            return mesh;
        }
    }
}

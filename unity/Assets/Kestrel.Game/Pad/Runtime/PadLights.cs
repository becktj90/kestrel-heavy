// D5: draws a PadLighting plan. Fixtures are instanced emissive boxes (one batch per LightKind) plus a
// thin pole batch; realtime Lights only for specs flagged Realtime (KitBudgets.PadRealtimeLights per tier).
// Night = 0 by day: emission and realtime intensity scale with Night, so the same pad works day and night.
using System.Collections.Generic;
using Kestrel.Kit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Pad
{
    [DisallowMultipleComponent]
    public sealed class PadLights : MonoBehaviour
    {
        /// <summary>0 = day, 1 = full night. Set from the sky (PadLighting.NightFactor(sunElevation)).</summary>
        [Range(0, 1)] public float Night = 0f;
        public Camera ViewCamera;

        public List<LightSpec> Plan { get; private set; }
        public readonly List<Light> Realtime = new List<Light>();
        public readonly BudgetLedger Ledger = new BudgetLedger();
        public int BuiltQuality { get; private set; } = -1;

        readonly Dictionary<LightKind, InstanceBatch> _fix = new Dictionary<LightKind, InstanceBatch>();
        readonly Dictionary<LightKind, Material> _mats = new Dictionary<LightKind, Material>();
        readonly List<int> _blinkers = new List<int>();
        PadGlow _glow; public PadGlow Glow => _glow;
        InstanceBatch _poles; Mesh _box, _poleMesh; Matrix4x4 _builtFor; float _lastNight = -1f; bool _blinkOn = true;
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor"), BaseColor = Shader.PropertyToID("_BaseColor");

        /// <summary>When true, Night follows the scene sun every frame (see ResolveNight). Set false to drive Night yourself.</summary>
        public bool FollowSun = true;

        /// <summary>Global night override for shots: null = follow the sun. Also set by the -khNight command-line arg.</summary>
        public static bool? ForceNight
        {
            get { if (!_argChecked) { _argChecked = true; _force = ReadPersisted(); } return _force; }
            set { _argChecked = true; _force = value; WritePersisted(value); }
        }
        const string SessionKey = "Kestrel.PadLights.ForceNight";   // -1 follow sun, 0 day, 1 night

        // Survives the domain reload when Play starts: the Editor keeps the value in SessionState (per editor session),
        // and statics are re-read on load. -khNight on the command line always wins.
        static bool? ReadPersisted()
        {
            if (HasArg("-khNight")) return true;
#if UNITY_EDITOR
            int v = UnityEditor.SessionState.GetInt(SessionKey, -1); if (v >= 0) return v == 1;
#endif
            return null;
        }
        static void WritePersisted(bool? v)
        {
#if UNITY_EDITOR
            UnityEditor.SessionState.SetInt(SessionKey, v.HasValue ? (v.Value ? 1 : 0) : -1);
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReloadForceNight() { _argChecked = true; _force = ReadPersisted(); }
        static bool? _force; static bool _argChecked;
        static bool HasArg(string a) { try { foreach (var s in System.Environment.GetCommandLineArgs()) if (string.Equals(s, a, System.StringComparison.OrdinalIgnoreCase)) return true; } catch { } return false; }

        /// <summary>Directional light used as the sun: RenderSettings.sun, else the first enabled directional light.</summary>
        public static Light FindSun()
        {
            var s = RenderSettings.sun; if (s != null && s.type == LightType.Directional) return s;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l != null && l.enabled && l.type == LightType.Directional) return l;
            return null;
        }

        /// <summary>Sun elevation (deg above horizon) from a directional light's forward (light travels along forward).</summary>
        public static float SunElevation(Light sun) => Mathf.Asin(Mathf.Clamp(-sun.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        /// <summary>ForceNight → 1/0; else NightFactor(sun elevation) when a sun exists; else 0 (day).</summary>
        public static float ResolveNight()
        {
            var f = ForceNight; if (f.HasValue) return f.Value ? 1f : 0f;
            var sun = FindSun(); return sun != null ? PadLighting.NightFactor(SunElevation(sun)) : 0f;
        }

        public static PadLights Attach(PadRenderer pad, int quality)
        {
            var l = pad.GetComponent<PadLights>(); if (l == null) l = pad.gameObject.AddComponent<PadLights>();
            l.ViewCamera = pad.ViewCamera; l.Build(pad.Layout, quality); return l;
        }

        public void Build(PadLayout layout, int quality)
        {
            Clear(); if (layout == null) return;
            BuiltQuality = Mathf.Clamp(quality, 0, 2);
            Plan = PadLighting.Plan(layout, BuiltQuality);
            _box = KitMesh.Beam(1f, 1f); _poleMesh = KitMesh.Beam(0.25f, 0.25f);
            _poles = new InstanceBatch(_poleMesh, MaterialLibrary.Get(MaterialRole.Galvanized), DetailSize.Large);
            for (int i = 0; i < Plan.Count; i++)
            {
                var s = Plan[i];
                if (!_fix.TryGetValue(s.Kind, out var b))
                {
                    b = new InstanceBatch(_box, MatFor(s.Kind, s.Color), s.Kind == LightKind.Aviation ? DetailSize.Large : DetailSize.Medium) { Shadows = ShadowCastingMode.Off };
                    _fix[s.Kind] = b;
                }
                if (s.BlinkHz > 0) _blinkers.Add(i);
                if (s.Kind == LightKind.Pole)
                {
                    var foot = new Vector3(s.Position.X, 0, s.Position.Z);
                    _polesLocal.Add(Matrix4x4.TRS(foot, Quaternion.identity, new Vector3(1, s.Position.Y, 1)));
                }
                _fixLocal.Add((s.Kind, FixtureMatrix(s)));
                if (s.Realtime) Realtime.Add(MakeLight(s));
            }
            _glow = new PadGlow(Plan, BuiltQuality);
            _builtFor = default; _lastNight = -1f; Refresh();
        }

        readonly List<(LightKind, Matrix4x4)> _fixLocal = new List<(LightKind, Matrix4x4)>();
        readonly List<Matrix4x4> _polesLocal = new List<Matrix4x4>();

        /// <summary>Fixture box: floods 1.2×0.8×0.3 facing Aim, aviation 0.35 cube, strips 6×0.08×0.08 along X.</summary>
        public static Matrix4x4 FixtureMatrix(LightSpec s)
        {
            var p = new Vector3(s.Position.X, s.Position.Y, s.Position.Z); var aim = new Vector3(s.Aim.X, s.Aim.Y, s.Aim.Z);
            Vector3 size = s.Kind switch { LightKind.Flood => new Vector3(1.2f, 0.8f, 0.3f), LightKind.Pole => new Vector3(1.6f, 0.5f, 0.4f), LightKind.Aviation => new Vector3(0.35f, 0.35f, 0.35f), _ => new Vector3(6f, 0.08f, 0.08f) };
            var rot = aim.sqrMagnitude > 1e-6f && s.Kind != LightKind.Aviation && s.Kind != LightKind.DeckStrip ? Quaternion.LookRotation(aim) : Quaternion.identity;
            // KitMesh.Beam is a unit +Y beam from the origin: centre it, then scale.
            return Matrix4x4.TRS(p, rot, Vector3.one) * Matrix4x4.TRS(new Vector3(0, -size.y * 0.5f, 0), Quaternion.identity, size);
        }

        Material MatFor(LightKind k, Kestrel.Kit.Contracts.Vec3 c)
        {
            var src = MaterialLibrary.Get(MaterialRole.Glass);
            var m = src != null ? new Material(src) : new Material(Shader.Find(MaterialLibrary.LitShader));
            m.name = "Pad_Light_" + k; m.enableInstancing = true;
            m.SetColor(BaseColor, new Color(c.X * 0.3f, c.Y * 0.3f, c.Z * 0.3f));
            m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            _mats[k] = m; _matColor[k] = new Color(c.X, c.Y, c.Z); return m;
        }
        readonly Dictionary<LightKind, Color> _matColor = new Dictionary<LightKind, Color>();

        Light MakeLight(LightSpec s)
        {
            var go = new GameObject(s.Id); go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(s.Position.X, s.Position.Y, s.Position.Z);
            go.transform.localRotation = Quaternion.LookRotation(new Vector3(s.Aim.X, s.Aim.Y, s.Aim.Z));
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot; l.range = s.Range; l.spotAngle = s.SpotAngle; l.innerSpotAngle = s.InnerSpotAngle > 0 ? s.InnerSpotAngle : s.SpotAngle * 0.5f; l.color = new Color(s.Color.X, s.Color.Y, s.Color.Z);
            l.shadows = s.Shadows ? LightShadows.Soft : LightShadows.None; l.renderMode = LightRenderMode.ForcePixel; l.intensity = 0f; l.enabled = false;
            return l;
        }

        void Refresh()
        {
            var w = transform.localToWorldMatrix; if (w == _builtFor) return; _builtFor = w;
            foreach (var b in _fix.Values) b.Clear(); _poles.Clear();
            foreach (var (k, m) in _fixLocal) _fix[k].Add(w * m);
            foreach (var m in _polesLocal) _poles.Add(w * m);
        }

        /// <summary>Kind intensity → emission. Day keeps a faint glow so fixtures read as lamps.</summary>
        public void ApplyNight(double t)
        {
            bool on = true; foreach (var i in _blinkers) { on = PadLighting.Blink(Plan[i].BlinkHz, t) > 0.5f; break; }
            if (Mathf.Abs(Night - _lastNight) < 1e-3f && on == _blinkOn) return;
            _lastNight = Night; _blinkOn = on;
            foreach (var kv in _mats)
            {
                float k = kv.Key switch { LightKind.Flood => 6f, LightKind.Pole => 4f, LightKind.Aviation => (on ? 8f : 0.3f), _ => 1.2f };
                float day = kv.Key == LightKind.Aviation ? 0.6f : 0.05f;
                kv.Value.SetColor(EmissionColor, _matColor[kv.Key] * (k * Mathf.Lerp(day, 1f, Night)));
            }
            for (int i = 0, j = 0; i < Plan.Count; i++) if (Plan[i].Realtime && j < Realtime.Count)
                { var l = Realtime[j++]; l.enabled = Night > 0.05f; l.intensity = l.enabled ? PadLighting.RealtimeIntensity(Plan[i], Night) : 0f; }
        }

        void LateUpdate()
        {
            if (Plan == null) return;
            if (FollowSun) Night = ResolveNight();
            Refresh(); ApplyNight(Time.timeAsDouble);
            var cam = ViewCamera != null ? ViewCamera : Camera.main; if (cam == null) return;
            int q = BuiltQuality; Ledger.Reset(); var p = cam.transform.position;
            foreach (var b in _fix.Values) b.Draw(p, q, Ledger);
            _poles.Draw(p, q, Ledger);
            if (_glow != null && Night > 0.02f) { _glow.Draw(transform.localToWorldMatrix, cam, Night, Time.timeAsDouble, gameObject.layer); Ledger.Add((_glow.FlareCount > 0 ? 1 : 0) + (_glow.ConeCount > 0 ? 1 : 0), _glow.FlareCount * 2 + _glow.ConeTriangles); }
        }

        public void Clear()
        {
            foreach (var l in Realtime) if (l != null) DestroyImmediate(l.gameObject);
            Realtime.Clear(); _fix.Clear(); _fixLocal.Clear(); _polesLocal.Clear(); _blinkers.Clear(); _matColor.Clear();
            foreach (var m in _mats.Values) if (m != null) DestroyImmediate(m);
            _mats.Clear();
            if (_box != null) DestroyImmediate(_box); if (_poleMesh != null) DestroyImmediate(_poleMesh);
            _glow?.Dispose(); _glow = null;
            _box = _poleMesh = null; _poles = null; Plan = null; BuiltQuality = -1;
        }

        void OnDestroy() => Clear();
    }
}

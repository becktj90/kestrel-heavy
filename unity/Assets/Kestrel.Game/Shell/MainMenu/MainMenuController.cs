// Home is a perspective hangar, not a flat poster. The launch bar, the two glass rows, the
// telemetry and the KH-1 stack are separate objects along the camera, so a small yaw parallaxes
// them apart. Pointers hit colliders. Labels are OS-font quads in front of each slab: a runtime
// SDF atlas rendered as a solid white bar on device and left every control blank.
using System.Collections.Generic;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    public sealed class MainMenuController : MonoBehaviour
    {
        sealed class Plate
        {
            public MenuAnchor Anchor;
            public Transform Root, Slab, Icon;
            public Transform[] Trim;
            public readonly List<MainMenuLabel> Labels = new List<MainMenuLabel>();
            public MainMenuHotspot Hot;
        }

        AppShell _shell;
        Camera _cam;
        bool _captured, _ortho;
        float _fov, _near, _far, _aspect = -1f, _cool;
        CameraClearFlags _clear;
        Color _bg;
        Vector3 _pos;
        Quaternion _rot;
        Transform _root;
        Material _trimMat, _hudTrim;
        Mesh _cube;
        readonly List<Plate> _plates = new List<Plate>();
        readonly List<Object> _owned = new List<Object>();
        readonly List<MainMenuLabel> _glowLabels = new List<MainMenuLabel>();
        MainMenuLabel _fuel, _lox, _mass, _thrust, _wind, _status;
        string _fuelS, _loxS, _massS, _thrustS, _windS, _statusS;
        MainMenuHotspot _hover;

        /// <summary>Hide the 3D title/launch/glass plates (and their colliders) because the 2D overlay replaces them.</summary>
        public bool OverlayMode;

        public void Attach(AppShell shell)
        {
            _shell = shell;
            _cam = shell != null && shell.Fill != null ? shell.Fill.GetComponent<Camera>() : null;
            Capture();
            if (_root != null) Destroy(_root.gameObject);
            _plates.Clear();
            _root = new GameObject("MainMenu").transform;
            _root.SetParent(transform, false);
            _cube = Own(MainMenuMesh.UnitBox());
            BuildRocket();
            BuildHangar();
            MainMenuText.Warmup();
            BuildPlates();
            _aspect = AspectOfScreen();
            for (int i = 0; i < _plates.Count; i++) Place(_plates[i], _aspect);
            if (OverlayMode)
                for (int i = 0; i < _plates.Count; i++)
                    if (MainMenuLayout.ReplacedByOverlay(_plates[i].Anchor)) _plates[i].Root.gameObject.SetActive(false);
            ApplyCamera(MainMenuLayout.ReducedMotion());
        }

        void OnDestroy()
        {
            Restore();
            if (_root != null) Destroy(_root.gameObject);
            for (int i = 0; i < _owned.Count; i++) if (_owned[i] != null) Destroy(_owned[i]);
            _owned.Clear();
        }

        void Update()
        {
            if (_cam == null) return;
            Vector2 pos; bool down;
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                pos = t.position;
                down = t.phase == TouchPhase.Began;
            }
            else
            {
                pos = Input.mousePosition;
                down = Input.GetMouseButtonDown(0);
            }
            var hit = Ray(pos);
            _hover = hit;
            if (down && hit != null && hit.Live && _cool <= 0f)
            {
                _cool = 0.35f;
                hit.Pressed?.Invoke();
            }
        }

        void LateUpdate()
        {
            _cool = Mathf.Max(0f, _cool - Time.unscaledDeltaTime);
            bool reduce = MainMenuLayout.ReducedMotion();
            ApplyCamera(reduce);
            float aspect = _cam != null && _cam.aspect > 0.2f ? _cam.aspect : AspectOfScreen();
            if (Mathf.Abs(aspect - _aspect) > 0.01f)
            {
                _aspect = aspect;
                for (int i = 0; i < _plates.Count; i++) Place(_plates[i], aspect);
            }
            Animate(reduce);
        }

        void Animate(bool reduce)
        {
            float time = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;
            if (_trimMat != null)
            {
                float p = MainMenuLayout.Pulse(time, reduce);
                var c = MainMenuMaterials.Cyan;
                c.a = 0.25f + 0.7f * p;
                _trimMat.SetColor("_BaseColor", c);
                _trimMat.SetColor("_Color", c);
                if (_trimMat.HasProperty("_EmissionColor")) _trimMat.SetColor("_EmissionColor", MainMenuMaterials.Cyan * (1.2f + 4f * p));
            }
            if (_hudTrim != null)
            {
                // Steady, same cyan accent, no pulse and no bloom-level emission: the frame no longer glows into the text.
                var c = MainMenuMaterials.Cyan;
                c.a = 0.85f;
                _hudTrim.SetColor("_BaseColor", c);
                _hudTrim.SetColor("_Color", c);
                if (_hudTrim.HasProperty("_EmissionColor")) _hudTrim.SetColor("_EmissionColor", MainMenuMaterials.Cyan * 1.0f);
            }
            float title = MainMenuLayout.TitleGlow(time, reduce);
            for (int i = 0; i < _glowLabels.Count; i++) _glowLabels[i].SetGlow(title);
            for (int i = 0; i < _plates.Count; i++)
            {
                var h = _plates[i].Hot;
                if (h != null) h.Tick(h == _hover, dt, reduce);
            }
            var read = MainMenuLayout.Readout(time, reduce);
            SetLine(_fuel, ref _fuelS, read.LeftFuel);
            SetLine(_lox, ref _loxS, read.LeftLox);
            SetLine(_mass, ref _massS, read.LeftMass);
            SetLine(_thrust, ref _thrustS, read.RightThrust);
            SetLine(_wind, ref _windS, read.RightWind);
            SetLine(_status, ref _statusS, read.RightStatus);
        }

        static void SetLine(MainMenuLabel label, ref string cache, string next)
        {
            if (label == null || cache == next) return;
            cache = next;
            label.SetText(next);
        }

        MainMenuHotspot Ray(Vector2 screen)
        {
            if (_cam == null) return null;
            var ray = _cam.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out var hit, 80f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return null;
            return hit.collider.GetComponentInParent<MainMenuHotspot>();
        }

        void BuildPlates()
        {
            var anchors = MainMenuLayout.Anchors();
            for (int i = 0; i < anchors.Length; i++)
            {
                var a = anchors[i];
                var plate = new Plate { Anchor = a };
                plate.Root = New(a.Id, _root);
                bool slab = a.Clickable || a.Role == MenuTextRole.Hud;
                if (slab) plate.Slab = Slab(plate.Root, a);
                if (a.Id == "launch") plate.Trim = Trim(plate.Root, ref _trimMat);
                else if (a.Role == MenuTextRole.Hud) plate.Trim = Trim(plate.Root, ref _hudTrim);
                if (a.Clickable) plate.Hot = Hot(plate, a);
                if (!string.IsNullOrEmpty(a.Icon)) plate.Icon = Icon(plate.Root, a.Icon);
                FillLabels(plate, a);
                _plates.Add(plate);
            }
        }

        Transform Slab(Transform parent, MenuAnchor a)
        {
            var t = AddMesh(parent, "Slab", _cube, null, Vector3.zero, Quaternion.identity, Vector3.one);
            var rend = t.GetComponent<MeshRenderer>();
            // Telemetry sits on an opaque dark panel (no emissive cyan glass behind the text): high contrast on every phone.
            Material mat = a.Id == "launch"
                ? MainMenuMaterials.Body(MainMenuMaterials.LaunchRed, 0.35f, 0.58f, new Color(0.35f, 0.01f, 0.03f))
                : a.Role == MenuTextRole.Hud
                    ? MainMenuMaterials.Body(MainMenuMaterials.HudPanel, 0f, 0.05f, Color.black)
                    : MainMenuMaterials.Glass(MainMenuMaterials.GlassTint, 0.45f);
            rend.sharedMaterial = Own(mat);
            return t;
        }

        Transform[] Trim(Transform parent, ref Material mat)
        {
            if (mat == null) mat = Own(MainMenuMaterials.Additive(MainMenuMaterials.Cyan));
            var edges = new Transform[4];
            for (int i = 0; i < 4; i++)
                edges[i] = AddMesh(parent, "Trim", _cube, mat, Vector3.zero, Quaternion.identity, Vector3.one);
            return edges;
        }

        MainMenuHotspot Hot(Plate plate, MenuAnchor a)
        {
            var h = plate.Root.gameObject.AddComponent<MainMenuHotspot>();
            h.Id = a.Id;
            h.Live = a.Live;
            h.Spoken = a.Spoken;
            var rend = plate.Slab != null ? plate.Slab.GetComponent<MeshRenderer>() : null;
            h.Face = rend != null ? rend.sharedMaterial : null;
            h.RestTint = a.Id == "launch" ? MainMenuMaterials.LaunchRed : MainMenuMaterials.GlassTint;
            h.Pressed = () => { if (_shell != null && _shell.Flow != null) _shell.Flow.Go(AppScreen.Design); };
            var box = plate.Slab.gameObject.AddComponent<BoxCollider>();
            box.size = Vector3.one;
            h.Snap(false, true);
            return h;
        }

        Transform Icon(Transform parent, string kind)
        {
            var mesh = Own(MainMenuMesh.Icon(kind));
            var mat = Own(MainMenuMaterials.Lamp(MainMenuMaterials.Cyan * 1.6f));
            return AddMesh(parent, kind, mesh, mat, Vector3.zero, Quaternion.identity, Vector3.one);
        }

        void FillLabels(Plate plate, MenuAnchor a)
        {
            if (a.Id == "title")
            {
                Glow(MakeLabel(plate, MainMenuLayout.TitleTop, MenuTextRole.Title, true));
                Glow(MakeLabel(plate, MainMenuLayout.TitleBottom, MenuTextRole.Title, true));
            }
            else if (a.Id == "subtitle")
            {
                MakeLabel(plate, MainMenuLayout.Tagline, MenuTextRole.Subtitle, true);
            }
            else if (a.Id == "tel-l" || a.Id == "tel-r")
            {
                var read = MainMenuLayout.Readout(0f, true);
                if (a.Id == "tel-l")
                {
                    _fuel = MakeLabel(plate, read.LeftFuel, MenuTextRole.Hud, true);
                    _lox = MakeLabel(plate, read.LeftLox, MenuTextRole.Hud, true);
                    _mass = MakeLabel(plate, read.LeftMass, MenuTextRole.Hud, true);
                }
                else
                {
                    _thrust = MakeLabel(plate, read.RightThrust, MenuTextRole.Hud, true);
                    _wind = MakeLabel(plate, read.RightWind, MenuTextRole.Hud, true);
                    _status = MakeLabel(plate, read.RightStatus, MenuTextRole.Hud, true);
                }
            }
            else if (a.Id == "kh1")
            {
                MakeLabel(plate, MainMenuLayout.Designation, MenuTextRole.Dial, true);   // steady: no brightness pulse
            }
            else if (a.Id == "launch")
            {
                MakeLabel(plate, "LAUNCH", MenuTextRole.Launch, true);
                MakeLabel(plate, a.Label, MenuTextRole.Launch, true);
            }
            else if (!string.IsNullOrEmpty(a.Label))
            {
                MakeLabel(plate, a.Label, a.Role, a.Live);
            }
        }

        MainMenuLabel MakeLabel(Plate plate, string text, MenuTextRole role, bool live)
        {
            var t = New(string.IsNullOrEmpty(text) ? role.ToString() : text, plate.Root);
            var filter = t.gameObject.AddComponent<MeshFilter>();
            var rend = t.gameObject.AddComponent<MeshRenderer>();
            var mesh = Own(new Mesh { name = "KH_Label" });
            var mat = Own(MainMenuMaterials.Font());
            filter.sharedMesh = mesh;
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            var label = t.gameObject.AddComponent<MainMenuLabel>();
            label.Bind(filter, rend, mesh, mat, role, MainMenuMaterials.InkFor(role, live));
            label.SetText(text);
            plate.Labels.Add(label);
            return label;
        }

        void Glow(MainMenuLabel label)
        {
            if (label != null) _glowLabels.Add(label);
        }

        void Place(Plate p, float aspect)
        {
            Vector3 w = MainMenuLayout.World(p.Anchor, aspect);
            var pose = MainMenuLayout.Pose();
            Vector3 toCam = pose.Eye - w;
            p.Root.SetPositionAndRotation(w, Quaternion.LookRotation(toCam.sqrMagnitude > 1e-6f ? toCam : Vector3.back, Vector3.up));
            Vector2 size = MainMenuLayout.WorldSize(p.Anchor, aspect);
            float thick = p.Anchor.Id == "launch" ? 0.07f : 0.035f;
            float z = MainMenuText.FaceZ(thick);
            if (p.Slab != null) p.Slab.localScale = new Vector3(size.x, size.y, thick);
            if (p.Icon != null)
            {
                float s = size.y * 0.42f;
                p.Icon.localScale = new Vector3(s, s, s);
                // local +X is screen-left: the panel's +Z points back at the camera.
                p.Icon.localPosition = new Vector3(size.x * 0.30f, 0f, z);
            }
            PlaceLabels(p, size, z);
            if (p.Trim != null)
            {
                float b = Mathf.Clamp(Mathf.Min(size.x, size.y) * 0.04f, 0.006f, 0.016f);
                float tz = thick * 0.35f;
                float ez = -thick * 0.15f;
                Edge(p.Trim[0], new Vector3(-size.x * 0.5f, 0f, ez), new Vector3(b, size.y + b, tz));
                Edge(p.Trim[1], new Vector3(size.x * 0.5f, 0f, ez), new Vector3(b, size.y + b, tz));
                Edge(p.Trim[2], new Vector3(0f, size.y * 0.5f, ez), new Vector3(size.x + b, b, tz));
                Edge(p.Trim[3], new Vector3(0f, -size.y * 0.5f, ez), new Vector3(size.x + b, b, tz));
            }
        }

        static void PlaceLabels(Plate p, Vector2 size, float z)
        {
            var labels = p.Labels;
            if (labels == null || labels.Count == 0) return;
            if (p.Anchor.Id == "kh1")
            {
                labels[0].Place(new Vector3(0f, 0f, z), size.x * 0.72f, size.y * 0.34f);
                return;
            }
            if (p.Anchor.Id == "launch" && labels.Count >= 2)
            {
                labels[0].Place(new Vector3(0f, size.y * 0.16f, z), size.x * 0.78f, size.y * 0.46f);
                labels[1].Place(new Vector3(0f, -size.y * 0.26f, z), size.x * 0.88f, size.y * 0.24f);
                return;
            }
            if (labels.Count == 1)
            {
                bool glass = p.Icon != null;
                float x = glass ? -size.x * 0.10f : 0f;
                float w = glass ? size.x * 0.50f : size.x * 0.90f;
                float h = size.y * (p.Anchor.Role == MenuTextRole.Launch ? 0.62f : 0.70f);
                labels[0].Place(new Vector3(x, 0f, z), w, h);
                return;
            }
            float span = size.y * 0.82f;
            float step = span / labels.Count;
            float hLine = step * 0.88f;
            float top = span * 0.5f - step * 0.5f;
            float wLine = size.x * 0.88f;
            for (int i = 0; i < labels.Count; i++)
                labels[i].Place(new Vector3(0f, top - i * step, z), wLine, hLine);
        }

        static void Edge(Transform t, Vector3 pos, Vector3 scale)
        {
            t.localPosition = pos;
            t.localScale = scale;
        }

        void BuildRocket()
        {
            float aspect = 0.56f;
            var root = New("KH-1", _root);
            root.SetPositionAndRotation(MainMenuLayout.RocketBase(aspect), Quaternion.identity);
            root.localScale = Vector3.one * MainMenuLayout.RocketScale(aspect);

            var paint = Own(MainMenuMaterials.Body(new Color(0.9f, 0.91f, 0.93f), 0.22f, 0.64f, new Color(0.06f, 0.07f, 0.08f)));
            var metal = Own(MainMenuMaterials.Body(new Color(0.16f, 0.17f, 0.18f), 0.88f, 0.72f, new Color(0.02f, 0.02f, 0.025f)));
            var red = Own(MainMenuMaterials.Body(new Color(0.78f, 0.07f, 0.1f), 0.3f, 0.5f, new Color(0.3f, 0.02f, 0.03f)));
            var suit = Own(MainMenuMaterials.Lamp(new Color(1.5f, 0.95f, 0.2f)));
            if (suit != null && suit.HasProperty("_Cull")) suit.SetFloat("_Cull", 0f);
            var core = Own(MainMenuMesh.CoreBody());
            var booster = Own(MainMenuMesh.BoosterBody());
            var bell = Own(MainMenuMesh.Bell());
            AddMesh(root, "Core", core, paint, Vector3.zero, Quaternion.identity, Vector3.one);
            float bx = MainMenuLayout.BoosterCenterM;
            AddMesh(root, "Booster L", booster, paint, new Vector3(-bx, 0f, 0f), Quaternion.identity, Vector3.one);
            AddMesh(root, "Booster R", booster, paint, new Vector3(bx, 0f, 0f), Quaternion.identity, Vector3.one);
            Bells(root, bell, metal, Vector3.zero, 1.15f);
            Bells(root, bell, metal, new Vector3(-bx, 0f, 0f), 1.05f);
            Bells(root, bell, metal, new Vector3(bx, 0f, 0f), 1.05f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + 0.35f;
                var pos = new Vector3(Mathf.Cos(a) * 2.2f, 39.4f, Mathf.Sin(a) * 2.2f);
                AddMesh(root, "Fin", _cube, metal, pos, Quaternion.LookRotation(new Vector3(pos.x, 0f, pos.z), Vector3.up), new Vector3(0.5f, 0.08f, 1.25f));
            }
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                var pos = new Vector3(Mathf.Cos(a) * 1.65f, 0.75f, Mathf.Sin(a) * 1.65f);
                AddMesh(root, "Leg", _cube, metal, pos, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 24f), new Vector3(0.35f, 1.6f, 0.16f));
            }
            var bandPts = new List<Vector2> { new Vector2(2.05f, 0f), new Vector2(2.28f, 0.12f), new Vector2(2.05f, 0.24f) };
            var v = new List<Vector3>(); var n = new List<Vector3>(); var tri = new List<int>();
            MainMenuMesh.Lathe(bandPts, new Vector3(0f, 57.4f, 0f), Quaternion.identity, 40, v, n, tri);
            AddMesh(root, "Band", Own(MainMenuMesh.Bake("KH_Band", v, n, tri)), red, Vector3.zero, Quaternion.identity, Vector3.one);

            var crew = Own(MainMenuMesh.Crew());
            Vector3[] at =
            {
                new Vector3(-6.3f, 0f, -5.4f), new Vector3(-2.4f, 0f, -6.6f), new Vector3(0.4f, 0f, -7.3f),
                new Vector3(3.0f, 0f, -6.2f), new Vector3(6.2f, 0f, -4.8f),
            };
            for (int i = 0; i < at.Length; i++)
            {
                var yaw = Quaternion.LookRotation(new Vector3(-at[i].x, 0f, -at[i].z).normalized, Vector3.up);
                AddMesh(root, "Crew", crew, suit, at[i], yaw, Vector3.one);
            }
            // Service tower, just outside the left booster, so the bay has a structure at the vehicle's scale.
            for (int k = 0; k < 8; k++)
            {
                float y = 3f + k * 5.5f;
                AddMesh(root, "Tower", _cube, metal, new Vector3(-6.8f, y, 0.4f), Quaternion.identity, new Vector3(0.35f, 5f, 0.35f));
                AddMesh(root, "Tower X", _cube, metal, new Vector3(-6.8f, y, 0.4f), Quaternion.Euler(0f, 0f, 45f), new Vector3(0.12f, 4.2f, 0.12f));
            }
        }

        void Bells(Transform root, Mesh bell, Material mat, Vector3 center, float radius)
        {
            AddMesh(root, "Bell", bell, mat, center, Quaternion.identity, Vector3.one);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                AddMesh(root, "Bell", bell, mat, center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius), Quaternion.identity, Vector3.one);
            }
        }

        void BuildHangar()
        {
            float aspect = 0.85f;
            Vector3 deck = MainMenuLayout.RocketBase(aspect);
            Vector3 nose = MainMenuLayout.World(MainMenuLayout.RocketDepth, 0f, MainMenuLayout.RocketNoseYFrac, aspect);
            Vector3 wall = MainMenuLayout.World(MainMenuLayout.HangarWallDepth, 0f, 0f, aspect);
            float tan = Mathf.Tan(MainMenuLayout.FovYDegrees * 0.5f * Mathf.Deg2Rad);
            float fullW = 2f * MainMenuLayout.HangarWallDepth * tan * aspect;
            float height = Mathf.Max(3f, (nose.y - deck.y) + 1.4f);
            var concrete = Own(MainMenuMaterials.Body(new Color(0.22f, 0.23f, 0.24f), 0.02f, 0.18f, new Color(0.02f, 0.02f, 0.02f)));
            var steel = Own(MainMenuMaterials.Body(new Color(0.07f, 0.09f, 0.12f), 0.55f, 0.4f, new Color(0.015f, 0.02f, 0.03f)));
            var lamp = Own(MainMenuMaterials.Lamp(new Color(3.2f, 2.6f, 1.8f)));
            // Deck runs from in front of the launch bar back to the wall, and stays behind the camera.
            float nearZ = deck.z - 12f;
            float farZ = wall.z + 1.5f;
            float len = Mathf.Max(4f, farZ - nearZ);
            float midZ = (nearZ + farZ) * 0.5f;
            AddMesh(_root, "Deck", _cube, concrete, new Vector3(0f, deck.y - 0.06f, midZ), Quaternion.identity, new Vector3(fullW + 2f, 0.12f, len));
            AddMesh(_root, "Back", _cube, steel, new Vector3(0f, deck.y + height * 0.5f, wall.z), Quaternion.identity, new Vector3(fullW + 2f, height, 0.2f));
            AddMesh(_root, "Left", _cube, steel, new Vector3(-fullW * 0.5f, deck.y + height * 0.5f, midZ), Quaternion.identity, new Vector3(0.2f, height, len));
            AddMesh(_root, "Right", _cube, steel, new Vector3(fullW * 0.5f, deck.y + height * 0.5f, midZ), Quaternion.identity, new Vector3(0.2f, height, len));
            float ceil = deck.y + height;
            // The emissive white bay-light bars read as stray bars over the overlay title, so skip them in overlay mode
            // (the Key/Fill Light objects below still light the scene).
            for (int i = -1; i <= 1 && !OverlayMode; i++)
            {
                AddMesh(_root, "Bay light", _cube, lamp, new Vector3(i * fullW * 0.22f, ceil - 0.08f, deck.z + 1.2f), Quaternion.identity, new Vector3(0.55f, 0.08f, 0.18f));
            }
            var keyGo = New("Key", _root);
            keyGo.position = new Vector3(deck.x - 0.4f, ceil - 0.2f, deck.z - 1.5f);
            var key = keyGo.gameObject.AddComponent<Light>();
            key.type = LightType.Spot;
            key.color = new Color(1f, 0.94f, 0.82f);
            key.intensity = 7f;
            key.range = 24f;
            key.spotAngle = 48f;
            key.shadows = LightShadows.Soft;
            keyGo.LookAt(new Vector3(deck.x, (deck.y + nose.y) * 0.5f, deck.z));
            var fillGo = New("Fill", _root);
            fillGo.position = new Vector3(deck.x + 1.2f, deck.y + height * 0.6f, deck.z - 2f);
            var fill = fillGo.gameObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(0.65f, 0.8f, 1f);
            fill.intensity = 1.6f;
            fill.range = 16f;
            fill.shadows = LightShadows.None;
        }

        static float AspectOfScreen() => Screen.height > 0 ? (float)Screen.width / Screen.height : 0.56f;

        void ApplyCamera(bool reduce)
        {
            if (_cam == null) return;
            var pose = MainMenuLayout.PoseAt(Time.unscaledTime, reduce);
            _cam.orthographic = false;
            _cam.fieldOfView = pose.FovY;
            _cam.nearClipPlane = 0.04f;
            _cam.farClipPlane = 80f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = MainMenuMaterials.Bay;
            _cam.transform.SetPositionAndRotation(pose.Eye, Quaternion.LookRotation(pose.Forward, Vector3.up));
            // Lens shift: move the whole 3D view up by HeroLift of the panel height so it never sits under the controls.
            _cam.ResetProjectionMatrix();
            float lift = Mathf.Clamp(MainMenuLayout.HeroLift, 0f, 0.2f);
            if (lift > 0f)
            {
                var m = _cam.projectionMatrix;
                m[1, 2] = -2f * lift;    // NDC y shift up (view space looks down -Z)
                _cam.projectionMatrix = m;
            }
        }

        void Capture()
        {
            if (_cam == null || _captured) return;
            _ortho = _cam.orthographic;
            _fov = _cam.fieldOfView;
            _near = _cam.nearClipPlane;
            _far = _cam.farClipPlane;
            _clear = _cam.clearFlags;
            _bg = _cam.backgroundColor;
            _pos = _cam.transform.position;
            _rot = _cam.transform.rotation;
            _captured = true;
        }

        void Restore()
        {
            if (!_captured || _cam == null) return;
            _cam.orthographic = _ortho;
            _cam.fieldOfView = _fov;
            _cam.nearClipPlane = _near;
            _cam.farClipPlane = _far;
            _cam.clearFlags = _clear;
            _cam.backgroundColor = _bg;
            _cam.transform.SetPositionAndRotation(_pos, _rot);
            _cam.ResetProjectionMatrix();
            _captured = false;
        }

        Transform New(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        Transform AddMesh(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var t = New(name, parent);
            t.localPosition = pos;
            t.localRotation = rot;
            t.localScale = scale;
            var filter = t.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var rend = t.gameObject.AddComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            return t;
        }

        T Own<T>(T asset) where T : Object
        {
            if (asset != null) _owned.Add(asset);
            return asset;
        }
    }
}

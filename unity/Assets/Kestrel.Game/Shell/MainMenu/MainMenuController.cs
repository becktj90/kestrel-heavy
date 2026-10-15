// Home is a perspective hangar, not a flat poster. The launch bar, the two glass rows, the
// telemetry and the KH-1 stack are separate objects along the camera, so a small yaw parallaxes
// them apart. Pointers hit colliders. Text is TextMeshPro on world-space canvases.
using System.Collections.Generic;
using TMPro;
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
            public Transform Root, Slab, Icon, Dial;
            public Transform[] Trim;
            public Canvas Canvas;
            public float Pixels;
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
        Transform _root, _dialOuter, _dialInner;
        Material _trimMat, _hudTrim;
        Mesh _cube;
        readonly List<Plate> _plates = new List<Plate>();
        readonly List<Object> _owned = new List<Object>();
        readonly List<MainMenuLabel> _glowLabels = new List<MainMenuLabel>();
        MainMenuLabel _fuel, _lox, _mass, _thrust, _wind, _status;
        string _fuelS, _loxS, _massS, _thrustS, _windS, _statusS;
        MainMenuHotspot _hover;

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
            BuildPlates();
            _aspect = AspectOfScreen();
            for (int i = 0; i < _plates.Count; i++) Place(_plates[i], _aspect);
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
                float h = reduce ? 0.85f : 0.65f + 0.35f * MainMenuLayout.Pulse(time + 0.4f, false);
                var c = MainMenuMaterials.Cyan;
                c.a = 0.45f + 0.4f * h;
                _hudTrim.SetColor("_BaseColor", c);
                _hudTrim.SetColor("_Color", c);
                if (_hudTrim.HasProperty("_EmissionColor")) _hudTrim.SetColor("_EmissionColor", MainMenuMaterials.Cyan * (1.4f + 2.2f * h));
            }
            float title = MainMenuLayout.TitleGlow(time, reduce);
            for (int i = 0; i < _glowLabels.Count; i++) _glowLabels[i].SetGlow(title);
            if (_dialOuter != null) _dialOuter.localRotation = Quaternion.Euler(0f, 0f, MainMenuLayout.DialAngle(time, reduce, 1f));
            if (_dialInner != null) _dialInner.localRotation = Quaternion.Euler(0f, 0f, MainMenuLayout.DialAngle(time, reduce, -0.65f));
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
            if (label == null || label.Text == null || cache == next) return;
            cache = next;
            label.Text.text = next;
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
                var plate = new Plate { Anchor = a, Pixels = PixelsFor(a) };
                plate.Root = New(a.Id, _root);
                bool slab = a.Clickable || a.Role == MenuTextRole.Hud;
                if (slab) plate.Slab = Slab(plate.Root, a);
                if (a.Id == "launch") plate.Trim = Trim(plate.Root, ref _trimMat);
                else if (a.Role == MenuTextRole.Hud) plate.Trim = Trim(plate.Root, ref _hudTrim);
                if (a.Clickable) plate.Hot = Hot(plate, a);
                if (!string.IsNullOrEmpty(a.Icon)) plate.Icon = Icon(plate.Root, a.Icon);
                plate.Canvas = CanvasFor(plate, a);
                if (a.Id == "kh1") plate.Dial = Dials(plate.Root);
                _plates.Add(plate);
            }
        }

        Transform Slab(Transform parent, MenuAnchor a)
        {
            var t = AddMesh(parent, "Slab", _cube, null, Vector3.zero, Quaternion.identity, Vector3.one);
            var rend = t.GetComponent<MeshRenderer>();
            Material mat = a.Id == "launch"
                ? MainMenuMaterials.Body(MainMenuMaterials.LaunchRed, 0.35f, 0.58f, new Color(0.35f, 0.01f, 0.03f))
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

        Canvas CanvasFor(Plate plate, MenuAnchor a)
        {
            var canvas = MainMenuText.CreateCanvas(plate.Root, 1f, 0.4f, plate.Pixels, 400 - Mathf.RoundToInt(a.Depth * 12f), _cam);
            bool glass = a.Role == MenuTextRole.Glass;
            var align = glass ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            float inset = glass ? plate.Pixels * 0.34f : 8f;
            if (a.Id == "title")
            {
                AddGlow(MainMenuText.Line(canvas.transform, MainMenuLayout.TitleTop, MenuTextRole.Title, 0.26f, TextAlignmentOptions.Center, 0f));
                AddGlow(MainMenuText.Line(canvas.transform, MainMenuLayout.TitleBottom, MenuTextRole.Title, -0.26f, TextAlignmentOptions.Center, 0f));
            }
            else if (a.Id == "subtitle")
            {
                MainMenuText.Line(canvas.transform, MainMenuLayout.Tagline, MenuTextRole.Subtitle, 0f, TextAlignmentOptions.Center, 0f);
            }
            else if (a.Id == "tel-l" || a.Id == "tel-r")
            {
                var read = MainMenuLayout.Readout(0f, true);
                if (a.Id == "tel-l")
                {
                    _fuel = Hud(canvas.transform, read.LeftFuel, 0.30f);
                    _lox = Hud(canvas.transform, read.LeftLox, 0f);
                    _mass = Hud(canvas.transform, read.LeftMass, -0.30f);
                }
                else
                {
                    _thrust = Hud(canvas.transform, read.RightThrust, 0.30f);
                    _wind = Hud(canvas.transform, read.RightWind, 0f);
                    _status = Hud(canvas.transform, read.RightStatus, -0.30f);
                }
            }
            else if (a.Id == "kh1")
            {
                AddGlow(MainMenuText.Line(canvas.transform, MainMenuLayout.Designation, MenuTextRole.Dial, 0f, TextAlignmentOptions.Center, 0f));
            }
            else if (!string.IsNullOrEmpty(a.Label))
            {
                var line = MainMenuText.Line(canvas.transform, a.Label, a.Role, 0f, align, inset);
                if (line == null) MainMenuText.Fallback(plate.Root, a.Label, 0.6f, 0.15f, 64, _owned);
            }
            if (a.Id == "title" && canvas.transform.childCount == 0)
            {
                MainMenuText.Fallback(plate.Root, MainMenuLayout.TitleTop, 0.8f, 0.12f, 72, _owned);
                MainMenuText.Fallback(plate.Root, MainMenuLayout.TitleBottom, 0.7f, 0.1f, 72, _owned);
            }
            return canvas;
        }

        MainMenuLabel Hud(Transform canvas, string text, float y)
        {
            var line = MainMenuText.Line(canvas, text, MenuTextRole.Hud, y, TextAlignmentOptions.Center, 0f);
            if (line != null) _glowLabels.Add(line);
            return line;
        }

        void AddGlow(MainMenuLabel label) { if (label != null) _glowLabels.Add(label); }

        Transform Dials(Transform parent)
        {
            var rig = New("Dials", parent);
            var mat = Own(MainMenuMaterials.Additive(MainMenuMaterials.Cyan));
            var outer = Own(MainMenuMesh.Ticks(0.72f, 48, 0.12f, 0.025f, 0.02f));
            var inner = Own(MainMenuMesh.Ticks(0.5f, 24, 0.08f, 0.02f, 0.02f));
            _dialOuter = AddMesh(rig, "Outer", outer, mat, Vector3.zero, Quaternion.identity, Vector3.one);
            _dialInner = AddMesh(rig, "Inner", inner, mat, Vector3.zero, Quaternion.identity, Vector3.one);
            var disc = Own(MainMenuMesh.Disc(0.34f, 32));
            var face = Own(MainMenuMaterials.Body(new Color(0.05f, 0.08f, 0.12f, 0.85f), 0.1f, 0.7f, MainMenuMaterials.Cyan * 0.35f));
            AddMesh(rig, "Plate", disc, face, new Vector3(0f, 0f, 0.01f), Quaternion.identity, Vector3.one);
            return rig;
        }

        void Place(Plate p, float aspect)
        {
            Vector3 w = MainMenuLayout.World(p.Anchor, aspect);
            var pose = MainMenuLayout.Pose();
            Vector3 toCam = pose.Eye - w;
            p.Root.SetPositionAndRotation(w, Quaternion.LookRotation(toCam.sqrMagnitude > 1e-6f ? toCam : Vector3.back, Vector3.up));
            Vector2 size = MainMenuLayout.WorldSize(p.Anchor, aspect);
            float thick = p.Anchor.Id == "launch" ? 0.07f : 0.035f;
            if (p.Slab != null) p.Slab.localScale = new Vector3(size.x, size.y, thick);
            if (p.Icon != null)
            {
                float s = size.y * 0.5f;
                p.Icon.localScale = new Vector3(s, s, s);
                p.Icon.localPosition = new Vector3(-size.x * 0.3f, 0f, thick * 0.5f + 0.012f);
            }
            if (p.Canvas != null)
            {
                MainMenuText.FitCanvas(p.Canvas, size.x * 0.94f, size.y * 0.82f, p.Pixels);
                p.Canvas.transform.localPosition = new Vector3(0f, 0f, thick * 0.5f + 0.014f);
            }
            if (p.Trim != null)
            {
                float b = Mathf.Clamp(size.y * 0.14f, 0.008f, 0.03f);
                float z = -thick * 0.2f;
                float tz = thick * 0.4f;
                Edge(p.Trim[0], new Vector3(-size.x * 0.5f, 0f, z), new Vector3(b, size.y + b, tz));
                Edge(p.Trim[1], new Vector3(size.x * 0.5f, 0f, z), new Vector3(b, size.y + b, tz));
                Edge(p.Trim[2], new Vector3(0f, size.y * 0.5f, z), new Vector3(size.x + b, b, tz));
                Edge(p.Trim[3], new Vector3(0f, -size.y * 0.5f, z), new Vector3(size.x + b, b, tz));
            }
            if (p.Dial != null) p.Dial.localScale = Vector3.one * (size.y * 0.55f);
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
            for (int i = -1; i <= 1; i++)
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

        static float PixelsFor(MenuAnchor a)
        {
            if (a.Role == MenuTextRole.Launch || a.Role == MenuTextRole.Title) return 1400f;
            if (a.Role == MenuTextRole.Hud || a.Role == MenuTextRole.Dial) return 800f;
            return 1000f;
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

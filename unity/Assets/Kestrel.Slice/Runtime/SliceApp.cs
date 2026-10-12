// Vertical-slice entry point: DESIGN stub (UI Toolkit) -> flight liftoff..MECO -> HUD -> Game Center.
// Everything is created from code at runtime; the scene only needs one GameObject with this
// component (the editor bootstrap generates Assets/Scenes/Slice.unity).
using System.Linq;
using Kestrel.Core.Economy;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class SliceApp : MonoBehaviour
    {
        public PanelSettings PanelSettings;   // assigned by the editor bootstrap (Resources/UI/KH_PanelSettings)
        Design design; Camera cam; Transform worldRoot, vehicleRoot, tower, ground, deck, deckTop; FlightDriver flight; GameObject rocketGO;
        VisualElement ui, designPanel, designBody, hud; Button designToggle; bool designCollapsed; float dragStartY; Label rDv, rTwr, rCost, rRel, rMass, hT, hStage, hAlt, hVel, hMach, hQ, hThr, hDr, hMsg; Button again;
        Material bodyMat, plumeMat; bool submitted;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            bodyMat = Resources.Load<Material>("Materials/KH_Body");
            plumeMat = Resources.Load<Material>("Materials/KH_Plume");
            BuildScene();
            BuildUI();
            _ = GameCenterBridge.Impl.Authenticate();
            SelectPreset("stock");
        }

        void BuildScene()
        {
            cam = new GameObject("Camera").AddComponent<Camera>(); cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.45f, 0.62f, 0.85f);
            cam.nearClipPlane = 0.5f; cam.farClipPlane = 200000f; cam.fieldOfView = 50;
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.3f; sun.transform.rotation = Quaternion.Euler(40, -30, 0);
            worldRoot = new GameObject("World").transform;
            vehicleRoot = new GameObject("Vehicle").transform; vehicleRoot.SetParent(worldRoot, false);
            flight = gameObject.AddComponent<FlightDriver>(); flight.World = worldRoot; flight.Vehicle = vehicleRoot;
            // Ground: 200 km, 1 m below the deck top, follows the vehicle downrange (LateUpdate); see PadGround.
            var g = GameObject.CreatePrimitive(PrimitiveType.Plane); g.name = "Ground"; ground = g.transform; ground.SetParent(worldRoot, false);
            ground.localScale = new Vector3(PadGround.Size / 10f, 1, PadGround.Size / 10f);
            Destroy(g.GetComponent<Collider>());
            if (bodyMat != null) { var gm = new Material(bodyMat) { color = PadGround.GroundColor }; g.GetComponent<Renderer>().sharedMaterial = gm; }
            flight.Anchor(ground, 0, PadGround.GroundY, 0);
            PadGround.ApplyHaze(cam.farClipPlane);
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder); pad.name = "Pad"; deck = pad.transform; deck.SetParent(worldRoot, false);
            Destroy(pad.GetComponent<Collider>());
            deck.localScale = new Vector3(2 * PadGround.MinDeckRadius, -PadGround.GroundY * 0.5f, 2 * PadGround.MinDeckRadius); // plinth GroundY..0
            if (bodyMat != null) { var dm = new Material(bodyMat) { color = new Color(0.55f, 0.55f, 0.53f) }; pad.GetComponent<Renderer>().sharedMaterial = dm; }
            flight.Anchor(deck, 0, PadGround.GroundY * 0.5, 0);
            deckTop = new GameObject("DeckTop").transform; deckTop.SetParent(worldRoot, false); // plume collision plane (+Y)
            flight.Anchor(deckTop, 0, 0, 0);
            tower = PadTower.Create(bodyMat); tower.SetParent(worldRoot, false);
            flight.Anchor(tower, 0, 0, 0); // sized per design in BuildRocket
            flight.OnMeco += OnMeco;
        }

        void BuildUI()
        {
            var doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = PanelSettings != null ? PanelSettings : Resources.Load<PanelSettings>("UI/KH_PanelSettings");
            doc.visualTreeAsset = Resources.Load<VisualTreeAsset>("UI/Slice");
            ui = doc.rootVisualElement;
            designPanel = ui.Q("design"); hud = ui.Q("hud");
            designBody = ui.Q("design-body"); designToggle = ui.Q<Button>("design-toggle");
            ui.RegisterCallback<GeometryChangedEvent>(_ => { ApplyHudSafeArea(); ApplyDesignPanelMetrics(); ApplyDesignFraming(); });
            designPanel.RegisterCallback<GeometryChangedEvent>(_ => ApplyDesignFraming());
            designToggle.clicked += () => SetDesignCollapsed(!designCollapsed);
            // Drag on the handle: down collapses, up expands (tap still toggles via clicked).
            designToggle.RegisterCallback<PointerDownEvent>(e => dragStartY = e.position.y, TrickleDown.TrickleDown);
            designToggle.RegisterCallback<PointerUpEvent>(e =>
            {
                float dy = e.position.y - dragStartY, min = 24f * UnitsPerPoint();
                if (Mathf.Abs(dy) >= min) { SetDesignCollapsed(dy > 0); e.StopImmediatePropagation(); }
            }, TrickleDown.TrickleDown);
            SetDesignCollapsed(false);
            rDv = ui.Q<Label>("r-dv"); rTwr = ui.Q<Label>("r-twr"); rCost = ui.Q<Label>("r-cost"); rRel = ui.Q<Label>("r-rel"); rMass = ui.Q<Label>("r-mass");
            hT = ui.Q<Label>("h-t"); hStage = ui.Q<Label>("h-stage"); hAlt = ui.Q<Label>("h-alt"); hVel = ui.Q<Label>("h-vel");
            hMach = ui.Q<Label>("h-mach"); hQ = ui.Q<Label>("h-q"); hThr = ui.Q<Label>("h-thr"); hDr = ui.Q<Label>("h-dr"); hMsg = ui.Q<Label>("h-msg");
            again = ui.Q<Button>("again");

            var presets = SliceData.Catalog.Presets;
            var dd = ui.Q<DropdownField>("preset");
            dd.choices = presets.Select(p => p.name).ToList();
            dd.RegisterValueChangedCallback(e => SelectPreset(presets.First(p => p.name == e.newValue).id));
            ui.Q<SliderInt>("engines").RegisterValueChangedCallback(e => { design.stages[0].engineCount = e.newValue; Refresh(); });
            ui.Q<Slider>("prop").RegisterValueChangedCallback(e => { design.stages[0].propMass = Mathf.Round(e.newValue) * 1000.0; Refresh(); });
            ui.Q<Button>("launch").clicked += Launch;
            again.clicked += () => { flight.enabled = false; cam.ResetProjectionMatrix(); hud.AddToClassList("hidden"); designPanel.RemoveFromClassList("hidden"); again.AddToClassList("hidden"); SelectPreset(design.id); };
            // Accessibility labels (VoiceOver wiring via UnityEngine.Accessibility is a listed gap; see RESULTS.md)
            ui.Q<Button>("launch").tooltip = "Roll out and launch the current design";
        }

        /// <summary>Pad the flight HUD to Screen.safeArea (+8 pt sides) so the top band clears the notch /
        /// Dynamic Island and stays above the vehicle (see HudLayout).</summary>
        void ApplyHudSafeArea()
        {
            var size = ui.layout.size; if (size.x <= 0 || size.y <= 0) return;
            float dpr = Screen.dpi > 0 ? Mathf.Max(1f, Mathf.Round(Screen.dpi / 160f)) : 3f;
            var p = HudLayout.SafePadding(new Vector2(Screen.width, Screen.height), Screen.safeArea, size, dpr);
            hud.style.paddingLeft = p.x; hud.style.paddingTop = p.y; hud.style.paddingRight = p.z; hud.style.paddingBottom = p.w;
        }

        /// <summary>Panel units per iOS point (panel scales with screen width, so this differs per device).</summary>
        float UnitsPerPoint()
        {
            float wPt = Screen.width / DesignFraming.PixelsPerPoint(Screen.width, Screen.height, Screen.dpi);
            return ui != null && ui.layout.width > 0 && wPt > 0 ? ui.layout.width / wPt : 3f;
        }

        /// <summary>Handle is 64 pt tall (tap target ≥ 44 pt) and the panel clears the home indicator.</summary>
        void ApplyDesignPanelMetrics()
        {
            if (ui.layout.height <= 0) return;
            float upp = UnitsPerPoint();
            // 56 pt handle (44 pt min tap target), no padding above it. Collapsed = handle + bottom safe area only.
            designToggle.style.height = 56f * upp;
            designToggle.style.minHeight = 44f * upp;
            designToggle.style.paddingTop = 0; designToggle.style.paddingBottom = 0;
            designToggle.style.marginTop = 0; designToggle.style.marginBottom = 0;
            float bottom = Screen.safeArea.yMin * ui.layout.height / Mathf.Max(1, Screen.height);
            designPanel.style.paddingTop = 0;
            designPanel.style.paddingBottom = designCollapsed ? bottom : Mathf.Max(8f * upp, bottom + 8f * upp);
        }

        void SetDesignCollapsed(bool collapsed)
        {
            designCollapsed = collapsed;
            designPanel.EnableInClassList("collapsed", collapsed);
            if (ui != null) ApplyDesignPanelMetrics();
            ui.Q<Button>("launch").style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            designToggle.text = collapsed ? "▲  Design" : "▼  Design";
            // Accessibility pattern in this project is tooltip text (VoiceOver bridge is a listed gap, RESULTS.md).
            designToggle.tooltip = collapsed ? "Design panel, collapsed. Tap to expand." : "Design panel, expanded. Tap to collapse and see the whole rocket.";
        }

        /// <summary>Frame rocket + pad deck in the band above the panel's top edge (live rect, any device).</summary>
        void ApplyDesignFraming()
        {
            if (cam == null || ui.layout.height <= 0 || designPanel.ClassListContains("hidden")) return;
            float f = DesignFraming.VisibleFraction(ui.layout.height, designPanel.worldBound.yMin - ui.worldBound.yMin);
            FrameCamera(rocketHeight, f);
        }

        void SelectPreset(string id)
        {
            design = SliceData.Catalog.Preset(id).Clone();
            ui.Q<DropdownField>("preset").SetValueWithoutNotify(design.name);
            ui.Q<SliderInt>("engines").SetValueWithoutNotify(design.stages[0].engineCount);
            ui.Q<Slider>("prop").SetValueWithoutNotify((float)(design.stages[0].propMass / 1000));
            Refresh();
        }

        /// <summary>Live readout from the ported core (same numbers as the web designer).</summary>
        void Refresh()
        {
            var c = SliceData.Catalog;
            var (stages, payload) = Stack.SerialStages(c, design);
            var dv = Headless.StackDeltaV(stages.ConvertAll(s => s.ToSim()), payload);
            double m0 = payload; foreach (var s in stages) m0 += s.propMass + s.dryMass;
            double twr = stages[0].thrustSL / (m0 * Atmosphere.G0);
            var req = Stack.RequiredDeltaV(c, design.payloadOrbit);
            var price = CostModel.PriceDesign(c, design);
            var rel = Reliability.Of(c, design);
            rDv.text = $"Δv {dv.total:N0} m/s  (need {req.required:N0})";
            rTwr.text = $"Liftoff TWR {twr:F2}";
            rCost.text = $"Per launch {CostModel.FormatKc(c, price.recurring)}";
            rRel.text = $"Reliability {rel.reliability * 100:F1}%";
            rMass.text = $"Liftoff {m0 / 1000:N0} t";
            rTwr.style.color = twr < 1.15 ? new Color(1f, 0.4f, 0.4f) : new Color(0.78f, 0.86f, 1f);
            BuildRocket();
        }

        void BuildRocket()
        {
            if (rocketGO != null) Destroy(rocketGO);
            var r = RocketMeshBuilder.Build(SliceData.Catalog, design);
            rocketGO = new GameObject("Rocket"); rocketGO.transform.SetParent(vehicleRoot, false);
            rocketGO.AddComponent<MeshFilter>().sharedMesh = r.mesh;
            var mr = rocketGO.AddComponent<MeshRenderer>();
            if (bodyMat != null) { var m = new Material(bodyMat); ColorUtility.TryParseHtmlString(design.liveryPrimary, out var col); m.color = col; mr.sharedMaterial = m; }
            AddTankShells(r);
            flight.Plume = PlumeController.Create(rocketGO.transform, Vector3.zero, Mathf.Max(1f, Mathf.Sqrt(design.stages[0].engineCount) * r.exitDiameter), plumeMat);
            flight.Plume.SetDeck(deckTop);
            flight.Plume.Apply(0, Atmosphere.P0, 0);
            vehicleRoot.localPosition = Vector3.zero; vehicleRoot.localRotation = Quaternion.identity;
            float rr = r.mesh.bounds.extents.x;
            var padR = PadTower.Apply(tower, r.height, rr, PadTower.DefaultArchetype(rr), PadTower.DefaultSeed);
            float deckR = PadGround.DeckRadius(padR != null ? padR.Layout : null); // deck under every tower leg
            deck.localScale = new Vector3(2 * deckR, deck.localScale.y, 2 * deckR);
            rocketHeight = r.height; ApplyDesignFraming();
        }

        float rocketHeight = 70; Vector3 camOffset;
        void FrameCamera(float h, float visible)
        {
            rocketHeight = h; float k = DesignFraming.DistanceScale(visible);
            var aim = new Vector3(0, h * DesignFraming.AimHeight, 0);
            cam.transform.position = aim + new Vector3(h * 0.9f, h * (0.45f - DesignFraming.AimHeight), -h * 1.4f) * k;
            cam.transform.LookAt(aim);
            cam.ResetProjectionMatrix();
            cam.projectionMatrix = DesignFraming.ShiftedProjection(cam.projectionMatrix, visible); camOffset = cam.transform.position - (vehicleRoot != null ? vehicleRoot.position : Vector3.zero); }

        /// <summary>V2a: overlay a procedural tank shell (welds, stiffener rings, isogrid normal map, LOD by distance)
        /// on every tank barrel. The shell sits 0.5% proud of the lathe body; its domes stay inside the body.</summary>
        void AddTankShells(RocketMeshBuilder.Result r)
        {
            ulong seed = (ulong)(design.id ?? "kh").GetHashCode();
            for (int i = 0; i < r.tanks.Count; i++)
            {
                var t = r.tanks[i];
                if (t.BarrelLength <= 0.1f || t.Radius <= 0.05f) continue;
                var spec = Kestrel.Vehicle.TankShellSpec.Default(t.Radius * 1.005f, t.BarrelLength, seed + (ulong)i * 7919UL);
                if (spec.Validate().Count > 0) continue;
                var go = new GameObject($"TankShell_S{t.Stage + 1}{(t.StrapOn ? "_strap" + i : "")}", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(rocketGO.transform, false);
                go.transform.localPosition = t.Offset + Vector3.up * (t.BarrelBottom - spec.DomeHeight);
                var view = go.AddComponent<Kestrel.Vehicle.TankShellView>();
                view.ViewCamera = cam;
                view.Init(spec, Kestrel.Kit.KitBudgets.Medium);
            }
        }

        void Launch()
        {
            submitted = false;
            designPanel.AddToClassList("hidden"); hud.RemoveFromClassList("hidden"); cam.ResetProjectionMatrix(); hMsg.text = "";
            flight.enabled = true;
            flight.Launch(design);
        }

        void LateUpdate()
        {
            if (flight == null || flight.Sim == null || hud.ClassListContains("hidden")) return;
            // Chase camera, in floating-origin space (vehicle stays near the origin).
            // Smooth the offset from the vehicle, not the absolute position: the vehicle teleports on every
            // floating-origin rebase and moves ~800 m/s at max-Q (see ChaseCamera).
            var v = vehicleRoot.position; var up = vehicleRoot.up;
            camOffset = ChaseCamera.StepOffset(camOffset, ChaseCamera.DesiredOffset(up, rocketHeight, (float)flight.Altitude), 4f, Time.deltaTime);
            // Keep the ground in view from above, and keep it under the vehicle (no edge, horizon at any downrange).
            ground.position = new Vector3(v.x, ground.position.y, ground.position.z);
            cam.transform.position = PadGround.ClampAboveGround(v + camOffset, ground.position.y);
            var cp = cam.transform.position;
            cam.transform.rotation = ChaseCamera.Aim(cp, v + up * rocketHeight * 0.5f, cam.fieldOfView, (float)flight.Altitude, cp.y - ground.position.y);
            hT.text = $"T+ {flight.SimTime:F1}";
            hStage.text = $"S{flight.Sim.StageIdx + 1}";
            hAlt.text = $"ALT {flight.Altitude / 1000:F1} km";
            hVel.text = $"VEL {flight.Speed:N0} m/s";
            hMach.text = $"MACH {flight.Mach:F2}";
            hQ.text = $"Q {flight.DynPressure / 1000:F1} kPa";
            hThr.text = $"THR {flight.ThrustFraction * 100:F0}%";
            hDr.text = $"DR {flight.Downrange / 1000:F1} km";
            if (!flight.Running && !flight.Meco && flight.Sim.Done) { hMsg.text = "Flight ended before MECO"; again.RemoveFromClassList("hidden"); }
        }

        async void OnMeco(FlightDriver f)
        {
            if (submitted) return; submitted = true;
            hMsg.text = $"MECO  ·  {f.Altitude / 1000:F1} km  ·  {f.Speed:N0} m/s";
            again.RemoveFromClassList("hidden");
            bool ok = await GameCenterBridge.Impl.SubmitScore(GameCenterBridge.MecoAltitudeBoard, (long)System.Math.Round(f.Altitude));
            hMsg.text += ok ? "\nScore sent to Game Center" : "";
        }
    }
}

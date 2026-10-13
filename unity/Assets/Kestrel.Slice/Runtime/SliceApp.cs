// Vertical-slice entry point: DESIGN stub (UI Toolkit) -> flight liftoff..MECO -> HUD -> Game Center.
// Everything is created from code at runtime; the scene only needs one GameObject with this
// component (the editor bootstrap generates Assets/Scenes/Slice.unity).
using System.Collections.Generic;
using System.Linq;
using Kestrel.Core.Designer;
using Kestrel.Core.Economy;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using Kestrel.Game.Environment;
using Kestrel.Kit.Contracts;
using Kestrel.Site;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class SliceApp : MonoBehaviour
    {
        public PanelSettings PanelSettings;   // assigned by the editor bootstrap (Resources/UI/KH_PanelSettings)
        Design design; Camera cam; Transform worldRoot, vehicleRoot, tower, ground, deck, deckTop; FlightDriver flight; GameObject rocketGO;
        HangarCamera hangarCam; HangarExplodedView explodedView; Toggle explodeToggle;
        VisualElement ui, designPanel, designBody, hud; Button designToggle, homeButton; bool designCollapsed; float dragStartY; Label rDv, rTwr, rCost, rRel, rMass, hT, hStage, hAlt, hVel, hMach, hQ, hThr, hDr, hMsg; Button again;
        Material bodyMat, plumeMat; bool submitted; SkyRig sky;
        // U1 validation. Layer inputs are placeholders until save/Settings wiring (U4): simple by default.
        public int OrbitsCompleted; public bool EngineerEnabled;
        Label issueChip, launchReason; Button launchButton; DesignerState designerState;
        static readonly string[] IssueControls = { "engines", "prop", "preset" };

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
            cam.clearFlags = CameraClearFlags.Skybox;
            // The coast disc has to reach the geometric horizon the chase camera frames. SkyRig still owns fog.
            cam.nearClipPlane = 0.5f; cam.farClipPlane = SiteView.FarClip; cam.fieldOfView = 50;
            hangarCam = cam.gameObject.AddComponent<HangarCamera>();
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.3f; sun.transform.rotation = Quaternion.Euler(40, -30, 0);
            sky = SkyRig.Attach(cam, sun);
            worldRoot = new GameObject("World").transform;
            vehicleRoot = new GameObject("Vehicle").transform; vehicleRoot.SetParent(worldRoot, false);
            flight = gameObject.AddComponent<FlightDriver>(); flight.World = worldRoot; flight.Vehicle = vehicleRoot;
            // Pier 7: scrub, road, coast and curved ocean. Sea level stays at GroundY so the chase-camera
            // clamp still looks down onto the shore. The site is pad-anchored (it does not slide with the rocket).
            var site = SiteView.Create(worldRoot, SiteGenerator.Pier7Seed, SiteView.LodForQuality(QualitySettings.GetQualityLevel()));
            site.BindSun(sun);
            ground = site.transform;
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
            // Shell Design scene is loaded additively, so Awake runs while Boot is still the active scene:
            // keep everything SliceApp builds in its own scene so it unloads with Design.
            foreach (var root in new[] { cam.gameObject, sun.gameObject, worldRoot.gameObject })
                if (root.scene != gameObject.scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            // After PadGround's one-shot haze, the sky owns the live fog colour so the horizon matches.
            if (sky != null) sky.Refresh();
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
            homeButton = ui.Q<Button>("home");
            homeButton.clicked += GoHome;
            homeButton.tooltip = "Home. Back to the title screen.";
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
            explodeToggle = ui.Q<Toggle>("explode");
            if (explodeToggle != null)
            {
                explodeToggle.tooltip = "Exploded view. Separates stages, engines and the fairing along the stack.";
                explodeToggle.RegisterValueChangedCallback(e => SetExploded(e.newValue));
            }
            launchButton = ui.Q<Button>("launch");
            launchButton.clicked += Launch;
            issueChip = ui.Q<Label>("issue-chip"); launchReason = ui.Q<Label>("launch-reason");
            again.clicked += () =>
            {
                flight.enabled = false;
                if (hangarCam != null) hangarCam.enabled = true;
                cam.ResetProjectionMatrix();
                hud.AddToClassList("hidden"); designPanel.RemoveFromClassList("hidden"); homeButton.RemoveFromClassList("hidden"); again.AddToClassList("hidden");
                SelectPreset(design.id);
            };
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

        /// <summary>Handle is 56 pt tall (tap target ≥ 44 pt); collapsed panel = handle + bottom safe area only.</summary>
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
            // Home control: top-left inside the safe area (+8 pt), 44 pt min target. Bottom handle and the
            // flight HUD never share this corner: Home is hidden whenever the HUD is shown.
            float sx = ui.layout.width / Mathf.Max(1, Screen.width), sy = ui.layout.height / Mathf.Max(1, Screen.height);
            homeButton.style.left = Screen.safeArea.xMin * sx + HomeButtonMetrics.InsetPt * upp;
            homeButton.style.top = (Screen.height - Screen.safeArea.yMax) * sy + HomeButtonMetrics.InsetPt * upp;
            // Fixed size in points on every device (was min-size only, with USS padding/radius in panel units,
            // which are ~3.1 per pt on SE but ~1.1 per pt on iPad, so it grew on SE and shrank on iPad).
            HomeButtonMetrics.Apply(homeButton.style, upp);
            designPanel.style.paddingBottom = designCollapsed ? bottom : Mathf.Max(8f * upp, bottom + 8f * upp);
        }

        void SetDesignCollapsed(bool collapsed)
        {
            designCollapsed = collapsed;
            designPanel.EnableInClassList("collapsed", collapsed);
            if (ui != null) ApplyDesignPanelMetrics();
            ui.Q<Button>("launch").style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            var reason = ui.Q<Label>("launch-reason"); if (reason != null) reason.style.display = collapsed ? DisplayStyle.None : StyleKeyword.Null;
            designToggle.text = collapsed ? "▲  Design" : "▼  Design";
            // Accessibility pattern in this project is tooltip text (VoiceOver bridge is a listed gap, RESULTS.md).
            designToggle.tooltip = collapsed ? "Design panel, collapsed. Tap to expand." : "Design panel, expanded. Tap to collapse and see the whole rocket.";
        }

        /// <summary>Frame rocket + pad deck in the band above the panel's top edge (live rect, any device).</summary>
        void ApplyDesignFraming()
        {
            if (cam == null || ui.layout.height <= 0 || designPanel.ClassListContains("hidden")) return;
            float f = DesignFraming.VisibleFraction(ui.layout.height, designPanel.worldBound.yMin - ui.worldBound.yMin);
            FrameCamera(frameHeight, f);
        }

        /// <summary>Vehicle + tower/arms + deck rim, refreshed in BuildRocket; FrameCamera fits all of them.</summary>
        System.Collections.Generic.List<Vector3> framePoints;

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
            ApplyValidation();
            BuildRocket();
        }

        /// <summary>U1: Validate.Engineering + Experience.Present -> chip, Launch gate + reason, control rings.</summary>
        void ApplyValidation()
        {
            string layer = Experience.Resolve(OrbitsCompleted, EngineerEnabled, EngineerEnabled ? "engineer" : "standard").layer;
            designerState = DesignerValidation.Evaluate(SliceData.Catalog, design, layer);
            var st = designerState;
            issueChip.text = st.chip;
            issueChip.EnableInClassList("hidden", st.chip.Length == 0);
            issueChip.EnableInClassList("warn-only", st.blocks.Count == 0);
            issueChip.tooltip = st.chip.Length == 0 ? "" : st.chip + ". " + (st.blocks.Count > 0 ? DesignerValidation.Wording(st.blocks[0], layer) : DesignerValidation.Wording(st.warnings[0], layer));
            launchButton.SetEnabled(st.launchEnabled);
            launchReason.text = st.reason;
            launchReason.EnableInClassList("hidden", st.launchEnabled);
            launchButton.tooltip = st.launchEnabled ? "Roll out and launch the current design" : "Launch unavailable. " + st.reason;
            foreach (var n in IssueControls) { var el = ui.Q(n); el.RemoveFromClassList("issue-block"); el.RemoveFromClassList("issue-warn"); }
            foreach (var w in st.warnings) ui.Q(DesignerValidation.ControlFor(w))?.AddToClassList("issue-warn");
            foreach (var b in st.blocks) { var el = ui.Q(DesignerValidation.ControlFor(b)); el?.RemoveFromClassList("issue-warn"); el?.AddToClassList("issue-block"); }
        }

        void BuildRocket()
        {
            if (rocketGO != null) Destroy(rocketGO);
            var r = RocketMeshBuilder.Build(SliceData.Catalog, design);
            rocketGO = new GameObject("Rocket"); rocketGO.transform.SetParent(vehicleRoot, false);
            rocketGO.AddComponent<MeshFilter>().sharedMesh = r.mesh;
            var mr = rocketGO.AddComponent<MeshRenderer>();
            if (bodyMat != null) { var m = new Material(bodyMat); ColorUtility.TryParseHtmlString(design.liveryPrimary, out var col); m.color = col; mr.sharedMaterial = m; }
            var plates = new List<AttachPoint>();
            AddTankShells(r, plates);
            AddBells(r);
            flight.Plume = PlumeController.Create(rocketGO.transform, Vector3.zero, PlumeCluster.Footprint(PlumeCluster.LiftoffEngineCount(design), r.exitDiameter, r.nozzleExits), plumeMat);
            flight.Plume.SetDeck(deckTop);
            flight.Plume.Configure(r.height, r.mesh.bounds.extents.x);
            flight.Plume.Apply(0, Atmosphere.P0, 0);
            vehicleRoot.localPosition = Vector3.zero; vehicleRoot.localRotation = Quaternion.identity;
            float rr = r.mesh.bounds.extents.x;
            var padR = PadTower.Apply(tower, r.height, rr, PadTower.DefaultArchetype(rr), PadTower.DefaultSeed, plates.Count > 0 ? plates : null, rocketGO.transform);
            float deckR = PadGround.DeckRadius(padR != null ? padR.Layout : null); // deck under every tower leg
            deck.localScale = new Vector3(2 * deckR, deck.localScale.y, 2 * deckR);
            framePoints = DesignFraming.SubjectPoints(r.height, rr, deckR,
                padR != null ? DesignFraming.PadBounds(padR.Layout, tower.localToWorldMatrix) : null);
            // Tower top (levels + cap) x TowerMargin (1.06) is in the fit points, so the point fit never clips it.
            float towerTop = TowerTop(padR);
            if (towerTop > 0f)
            {
                float topY = DesignFraming.FrameHeight(0f, towerTop);
                foreach (var c in DesignFraming.PadBounds(padR.Layout, tower.localToWorldMatrix))
                    framePoints.Add(new Vector3(c.x, Mathf.Max(c.y, tower.position.y + topY), c.z));
            }
            flight.Plume.AttachGroundCloud(tower, deckR, PadTower.Fit(r.height, rr).height);
            PadDetailLink.Apply(flight.Plume, tower, ground, deck, padR != null ? padR.Layout : null);
            rocketHeight = r.height; frameHeight = r.height;
            var explodedGo = new GameObject("Exploded");
            explodedView = explodedGo.AddComponent<HangarExplodedView>();
            explodedView.AttachTo(rocketGO.transform);
            explodedView.Build(SliceData.Catalog, design, mr.sharedMaterial);
            if (hangarCam != null) hangarCam.ExplodedView = explodedView;
            SetExploded(explodeToggle != null && explodeToggle.value);
            ApplyDesignFraming();
        }

        /// <summary>Separated parts live on the vehicle. The assembled rocket draws only while the view is closed.</summary>
        void SetExploded(bool on)
        {
            if (hangarCam != null) hangarCam.SetExploded(on);
            else if (explodedView != null) explodedView.SetExploded(on, false);
            // While the ease is still open, the separated parts are the picture. The assembled
            // rocket comes back only once they have seated (or immediately, when Reduce Motion snaps shut).
            if (!on && explodedView != null && explodedView.Shown > 0.001f) return;
            SetAssembledDrawn(!on);
        }

        void SetAssembledDrawn(bool drawn)
        {
            if (rocketGO == null) return;
            var renderers = rocketGO.GetComponentsInChildren<Renderer>(true);
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || UnderExploded(r.transform)) continue;
                r.enabled = drawn;
            }
        }

        static bool UnderExploded(Transform t)
        {
            for (; t != null; t = t.parent)
                if (t.GetComponent<HangarExplodedView>() != null) return true;
            return false;
        }

        float rocketHeight = 70, frameHeight = 70; Vector3 camOffset;

        /// <summary>Tower top above the pad (levels + open cap) from the generated layout. The pad steel is drawn
        /// instanced, so there are no Renderers to measure; the layout is the source of truth.</summary>
        static float TowerTop(Kestrel.Pad.PadRenderer pad)
        {
            var t = pad != null && pad.Layout != null ? pad.Layout.Tower : null;
            return t != null ? t.Height + t.CapHeight : 0f;
        }

        void FrameCamera(float h, float visible)
        {
            if (cam == null || !DesignFraming.ValidViewport(cam.pixelWidth, cam.pixelHeight)) return;
            h = DesignFraming.SafeHeight(h); visible = DesignFraming.SafeVisible(visible);
            rocketHeight = h;
            if (hangarCam != null && hangarCam.enabled)
            {
                hangarCam.SetBottomVisible(visible);
                hangarCam.SetVehicle(h, framePoints);
                camOffset = cam.transform.position - (vehicleRoot != null ? vehicleRoot.position : Vector3.zero);
                return;
            }
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : cam.aspect;
            float k = DesignFraming.FitDistanceScale(framePoints, h, aspect, cam.fieldOfView, visible);
            var pos = DesignFraming.CameraPosition(h, k, out var aim);
            if (!DesignFraming.IsFinite(pos) || (pos - aim).sqrMagnitude < 1e-6f) return;
            cam.transform.position = pos;
            cam.transform.LookAt(aim);
            cam.ResetProjectionMatrix();
            var proj = DesignFraming.ShiftedProjection(cam.projectionMatrix, visible);
            if (DesignFraming.IsFinite(proj)) cam.projectionMatrix = proj; camOffset = cam.transform.position - (vehicleRoot != null ? vehicleRoot.position : Vector3.zero); }

        /// <summary>V2a: overlay a procedural tank shell (welds, stiffener rings, isogrid normal map, LOD by distance)
        /// on every tank barrel. The shell sits 0.5% proud of the lathe body; its domes stay inside the body.</summary>
        void AddTankShells(RocketMeshBuilder.Result r, List<AttachPoint> plates)
        {
            ulong seed = (ulong)(design.id ?? "kh").GetHashCode();
            int strapIndex = 1;
            var cat = SliceData.Catalog;
            for (int i = 0; i < r.tanks.Count; i++)
            {
                var t = r.tanks[i];
                if (t.BarrelLength <= 0.1f || t.Radius <= 0.05f) continue;
                var spec = Kestrel.Vehicle.TankShellSpec.Default(t.Radius * 1.005f, t.BarrelLength, seed + (ulong)i * 7919UL);
                if (spec.Validate().Count > 0) continue;
                var go = new GameObject($"TankShell_S{t.Stage + 1}{(t.StrapOn ? "_strap" + i : "")}", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(rocketGO.transform, false);
                var origin = t.Offset + Vector3.up * (t.BarrelBottom - spec.DomeHeight);
                go.transform.localPosition = origin;
                var view = go.AddComponent<Kestrel.Vehicle.TankShellView>();
                view.ViewCamera = cam;
                view.Init(spec, Kestrel.Kit.KitBudgets.Medium);
                if (plates == null || cat == null) continue;
                var part = t.StrapOn ? design.strapOns : (t.Stage >= 0 && t.Stage < design.stages.Count ? design.stages[t.Stage] : null);
                if (part == null) continue;
                bool liquid = cat.PropellantById.TryGetValue(part.propellant ?? "", out var prop) && prop.storability != "solid";
                bool gimbal = false, purge = false;
                if (part.engine != null && cat.EngineById.TryGetValue(part.engine, out var eng))
                {
                    gimbal = eng.gimbal > 0;
                    purge = liquid && eng.cycle != "pressure-fed" && eng.cycle != "solid";
                }
                var release = !liquid || t.StrapOn ? ReleaseStyle.LanyardPull
                    : t.Stage == 0 ? ReleaseStyle.TailServiceMastDrop : ReleaseStyle.SwingArmRetract;
                int index = t.StrapOn ? strapIndex++ : 0;
                plates.AddRange(Kestrel.Vehicle.TankShellUmbilicals.ForStage(spec, t.Stage, index,
                    new Vec3(origin.x, origin.y, origin.z), liquid, gimbal, purge, release));
            }
        }

        /// <summary>V1a: one instanced bell per engine, shaped by expansion ratio and cycle. The body mesh no longer
        /// carries a cloned cone. The cluster watches the body renderer so exploded view does not draw both.</summary>
        void AddBells(RocketMeshBuilder.Result r)
        {
            if (r.engines == null || r.engines.Count == 0) return;
            ulong seed = Kestrel.Vehicle.BellVariation.HashId(design.id ?? "kh");
            var mounts = new List<Kestrel.Vehicle.BellMount>(r.engines.Count);
            for (int i = 0; i < r.engines.Count; i++)
            {
                var e = r.engines[i];
                mounts.Add(new Kestrel.Vehicle.BellMount
                {
                    EngineId = e.EngineId, Cycle = e.Cycle, ExpansionRatio = e.Expansion, ExitDiameter = e.ExitDiameter,
                    X = e.Exit.x, Y = e.Exit.y, Z = e.Exit.z, Stage = e.Stage, Slot = e.Slot, Seed = seed,
                    SkinRadius = e.StageRadius, Gimbal = e.GimbalDeg > 0.01f
                });
            }
            var gate = rocketGO.GetComponent<MeshRenderer>();
            var cluster = rocketGO.AddComponent<Kestrel.Vehicle.BellCluster>();
            cluster.ViewCamera = cam;
            cluster.Gate = gate;
            cluster.Init(mounts, Kestrel.Kit.KitBudgets.Medium);
            var bay = rocketGO.AddComponent<Kestrel.Vehicle.BayCluster>();
            bay.ViewCamera = cam;
            bay.Gate = gate;
            bay.Init(mounts, Kestrel.Kit.KitBudgets.Medium);
        }

        /// <summary>Back to the title screen. In the shell, SceneFlow unloads the Design scene, which owns every
        /// object SliceApp built (camera, sun, world, UI); ShellDesignHost restores the shell camera and UI.
        /// Opened standalone (Slice.unity), load build index 0 (Boot), which rebuilds the shell and enters Home.</summary>
        void GoHome()
        {
            if (flight != null) flight.enabled = false;
            var shell = Kestrel.Game.Shell.AppShell.Instance;
            if (shell != null && shell.Flow != null) { shell.Flow.Go(Kestrel.Game.Shell.AppScreen.Home); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        /// <summary>◀ Home: 88 x 44 pt, 17 pt bold, 12 pt corners, 8 pt inset from the safe area.</summary>
        public static class HomeButtonMetrics
        {
            public const float WidthPt = 88f, HeightPt = 44f, FontPt = 17f, RadiusPt = 12f, PadPt = 8f, InsetPt = 8f;
            public static void Apply(IStyle st, float upp)
            {
                st.width = WidthPt * upp; st.height = HeightPt * upp;
                st.minWidth = WidthPt * upp; st.minHeight = HeightPt * upp;
                st.maxWidth = WidthPt * upp; st.maxHeight = HeightPt * upp;
                st.fontSize = FontPt * upp;
                st.paddingLeft = PadPt * upp; st.paddingRight = PadPt * upp; st.paddingTop = 0; st.paddingBottom = 0;
                float r = RadiusPt * upp;
                st.borderTopLeftRadius = r; st.borderTopRightRadius = r; st.borderBottomLeftRadius = r; st.borderBottomRightRadius = r;
                st.unityTextAlign = TextAnchor.MiddleCenter;
            }
        }

        void Launch()
        {
            if (designerState != null && !designerState.launchEnabled) return; // U1: blocks gate Launch
            submitted = false;
            if (explodeToggle != null) explodeToggle.SetValueWithoutNotify(false);
            if (explodedView != null) explodedView.SetExploded(false, true);
            if (hangarCam != null)
            {
                hangarCam.SetExploded(false);
                if (cam != null && vehicleRoot != null) camOffset = cam.transform.position - vehicleRoot.position;
                hangarCam.enabled = false;
            }
            SetAssembledDrawn(true);
            designPanel.AddToClassList("hidden"); homeButton.AddToClassList("hidden"); hud.RemoveFromClassList("hidden"); cam.ResetProjectionMatrix(); hMsg.text = "";
            flight.enabled = true;
            flight.Launch(design);
            var pad = tower != null ? tower.Find("Pad") : null;
            var renderer = pad != null ? pad.GetComponent<Kestrel.Pad.PadRenderer>() : null;
            if (renderer != null) { renderer.ResetSequence(); renderer.AdvanceTo(0); }
        }

        void LateUpdate()
        {
            if (sky != null && flight != null) sky.SetAltitude(flight.Altitude);
            if (explodedView != null && hangarCam != null && hangarCam.enabled)
                SetAssembledDrawn(!explodedView.IsExploded && explodedView.Shown <= 0.001f);
            if (flight != null && flight.Running && tower != null)
            {
                var pad = tower.Find("Pad");
                var renderer = pad != null ? pad.GetComponent<Kestrel.Pad.PadRenderer>() : null;
                if (renderer != null) renderer.AdvanceTo(flight.SimTime);
            }
            if (flight == null || flight.Sim == null || hud.ClassListContains("hidden")) return;
            // Chase camera, in floating-origin space (vehicle stays near the origin).
            // Smooth the offset from the vehicle, not the absolute position: the vehicle teleports on every
            // floating-origin rebase and moves ~800 m/s at max-Q (see ChaseCamera).
            var v = vehicleRoot.position; var up = vehicleRoot.up;
            camOffset = ChaseCamera.StepOffset(camOffset, ChaseCamera.DesiredOffset(up, rocketHeight, (float)flight.Altitude), 4f, Time.deltaTime);
            // Grow the far plane with altitude so the coast under the framed horizon is never clipped.
            cam.farClipPlane = SiteHorizon.ClipFor((float)flight.Altitude);
            // Sea level is the anchored site root. Do not slide it: the coast has to stay under the pad.
            cam.transform.position = PadGround.ClampAboveGround(v + camOffset, ground.position.y);
            var cp = cam.transform.position;
            cam.transform.rotation = ChaseCamera.Aim(cp, v + up * rocketHeight * 0.5f, cam.fieldOfView, (float)flight.Altitude, cp.y - ground.position.y);
            flight.Plume.SetHeightAboveDeck((float)flight.Altitude);
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

// Vertical-slice entry point: Hangar designer (UI Toolkit) -> flight liftoff..MECO -> HUD -> Game Center.
// The Hangar edits a Kestrel.Core.Hangar.HangarSession (undo/redo, validation, data sheet); HangarPanel is the view,
// HangarRocketView draws the rocket per part (paint, selection, exploded view) and HangarCamera orbits and focuses.
// Everything is created from code at runtime; the scene only needs one GameObject with this
// component (the editor bootstrap generates Assets/Scenes/Slice.unity).
using System.Collections.Generic;
using System.Linq;
using Kestrel.Core.Designer;
using Kestrel.Core.Economy;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using Kestrel.Game.Environment;
using Kestrel.Kit.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class SliceApp : MonoBehaviour
    {
        public PanelSettings PanelSettings;   // assigned by the editor bootstrap (Resources/UI/KH_PanelSettings)
        Design design; HangarSession session; DesignLibrary library; HangarPanel panel; HangarRocketView rocketView = new HangarRocketView(); HangarCamera hangarCam;
        Button undoButton, redoButton, explodeButton; bool rocketDirty = true; float lastRocketBuild = -1f; Mesh combinedMesh;
        Camera cam; Transform worldRoot, vehicleRoot, tower, ground, deck, deckTop; FlightDriver flight; GameObject rocketGO;
        VisualElement ui, designPanel, designBody, hud; Button designToggle, homeButton; bool designCollapsed; float dragStartY; Label hT, hStage, hAlt, hVel, hMach, hQ, hThr, hDr, hMsg; Button again;
        Material bodyMat, plumeMat; bool submitted; SkyRig sky;
        // U1 validation. Layer inputs are placeholders until save/Settings wiring (U4): simple by default.
        public int OrbitsCompleted; public bool EngineerEnabled;
        Label issueChip, launchReason; Button launchButton;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            bodyMat = Resources.Load<Material>("Materials/KH_Body");
            plumeMat = Resources.Load<Material>("Materials/KH_Plume");
            BuildScene();
            BuildUI();
            _ = GameCenterBridge.Impl.Authenticate();
            StartSession();
        }

        /// <summary>Open the most recent save, else the Workhorse template, else the stock preset.</summary>
        void StartSession()
        {
            var c = SliceData.Catalog;
            var h = SliceData.Hangar;
            library = SliceData.Library;
            JsonObject first = null; string id = null, name = null;
            try
            {
                var saves = library?.List();
                if (saves != null && saves.Count > 0) { id = saves[0].id; first = library.Load(id); name = saves[0].name; }
            }
            catch (System.Exception e) { Debug.LogWarning("[Hangar] could not read saves: " + e.Message); first = null; id = null; }
            if (first == null)
            {
                id = null;
                var t = h?.Template("workhorse") ?? (h != null && h.templates.Count > 0 ? h.templates[0] : null);
                if (t != null) { first = HangarEditor.ForkTemplate(t); name = t.name; }
                else first = DesignerValidation.ToJson(c.Preset("stock"));
            }
            session = new HangarSession(c, h, first);
            if (id != null) session.Load(first, id, name);
            else if (name != null) session.Load(first, null, name);
            session.Changed += OnDesignChanged;
            session.SelectionChanged += OnSelectionChanged;
            design = Design.FromJson(session.Design);
            OnDesignChanged();
            TryBuildPanel();
        }

        void TryBuildPanel()
        {
            if (session == null || ui == null) return;
            float upp = UnitsPerPoint();
            if (panel == null)
            {
                panel = new HangarPanel(session, library ?? new DesignLibrary(new Kestrel.Core.Save.NativeStore(System.IO.Path.Combine(Application.temporaryCachePath, "kh-hangar"), () => System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), () => Random.value)), designBody, upp);
                panel.FocusRequested += FocusPart;
                panel.ExplodeRequested += SetExploded;
            }
            else panel.SetScale(upp);
            ApplyDesignPanelMetrics();
        }

        /// <summary>Every document change: readout gate, chip, undo labels, and a (rate-limited) rocket rebuild.</summary>
        void OnDesignChanged()
        {
            if (session == null) return;
            try { design = Design.FromJson(session.Design); }
            catch (System.Exception e) { Debug.LogWarning("[Hangar] design parse: " + e.Message); }
            rocketDirty = true;
            ApplyValidation();
            if (designToggle != null) designToggle.text = (designCollapsed ? "▲  " : "▼  ") + (session.SavedName ?? session.Design.Str("name") ?? "Design") + (session.Dirty ? " •" : "");
        }

        void OnSelectionChanged(PartRef p)
        {
            rocketView.SetSelected(p, true);
        }

        void FocusPart(PartRef p)
        {
            if (hangarCam == null) return;
            if (rocketView.Bounds(p, out float y, out float h)) hangarCam.Focus(y, h);
        }

        void SetExploded(bool on)
        {
            bool reduce = hangarCam != null && hangarCam.ReduceMotion;
            rocketView.SetExploded(on, reduce);
            if (explodeButton != null) { explodeButton.text = on ? "Join" : "Parts"; explodeButton.tooltip = on ? "Put the rocket back together" : "Exploded view: pull the parts apart"; }
            if (hangarCam != null)
            {
                float h = on ? ExplodedView.ExplodedHeight(frameHeight, design?.stages.Count ?? 1) : frameHeight;
                hangarCam.SetVehicle(h, on ? null : framePoints);
            }
        }

        void OnTapped(Vector2 screenPx)
        {
            if (session == null || hud == null || !hud.ClassListContains("hidden")) return;
            if (PartPicker.Pick(rocketView.Project(cam), screenPx, out var part))
            {
                session.Select(part);
                panel?.Show(HangarPanel.Tab.Part);
                FocusPart(part);
                if (designCollapsed) SetDesignCollapsed(false);
            }
        }

        void BuildScene()
        {
            cam = new GameObject("Camera").AddComponent<Camera>(); cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.5f; cam.farClipPlane = 200000f; cam.fieldOfView = 50;
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.3f; sun.transform.rotation = Quaternion.Euler(40, -30, 0);
            sky = SkyRig.Attach(cam, sun);
            hangarCam = cam.gameObject.AddComponent<HangarCamera>();
            hangarCam.Tapped += OnTapped;
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
            hT = ui.Q<Label>("h-t"); hStage = ui.Q<Label>("h-stage"); hAlt = ui.Q<Label>("h-alt"); hVel = ui.Q<Label>("h-vel");
            hMach = ui.Q<Label>("h-mach"); hQ = ui.Q<Label>("h-q"); hThr = ui.Q<Label>("h-thr"); hDr = ui.Q<Label>("h-dr"); hMsg = ui.Q<Label>("h-msg");
            again = ui.Q<Button>("again");
            launchButton = ui.Q<Button>("launch");
            launchButton.clicked += Launch;
            undoButton = ui.Q<Button>("undo"); redoButton = ui.Q<Button>("redo"); explodeButton = ui.Q<Button>("explode");
            undoButton.clicked += () => { var st = session?.Undo(); if (st != null) undoButton.tooltip = "Undid: " + st.Spoken; };
            redoButton.clicked += () => { var st = session?.Redo(); if (st != null) redoButton.tooltip = "Redid: " + st.Spoken; };
            explodeButton.clicked += () => SetExploded(!rocketView.Exploded);
            explodeButton.tooltip = "Exploded view: pull the parts apart";
            issueChip = ui.Q<Label>("issue-chip"); launchReason = ui.Q<Label>("launch-reason");
            issueChip.RegisterCallback<ClickEvent>(_ => { panel?.Show(HangarPanel.Tab.Data); if (designCollapsed) SetDesignCollapsed(false); });
            again.clicked += BackToDesign;
            // Accessibility labels (VoiceOver wiring via UnityEngine.Accessibility is a listed gap; see RESULTS.md)
            launchButton.tooltip = "Roll out and launch the current design";
            SetDesignCollapsed(false);
            ui.RegisterCallback<GeometryChangedEvent>(_ => { if (session != null) TryBuildPanel(); });
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

        /// <summary>
        /// Phones: bottom sheet with a 56 pt handle, scrolling content capped so the rocket keeps at least
        /// DesignFraming.MinVisibleFraction of the screen, and the thumb row above the bottom safe area.
        /// iPad (portrait, wider than 0.6): a 400 pt panel on the right, full height inside the safe area.
        /// </summary>
        void ApplyDesignPanelMetrics()
        {
            if (ui == null || ui.layout.height <= 0) return;
            float upp = UnitsPerPoint();
            designToggle.style.height = 56f * upp;
            designToggle.style.minHeight = 44f * upp;
            designToggle.style.fontSize = 20f * upp;
            designToggle.style.paddingTop = 0; designToggle.style.paddingBottom = 0;
            designToggle.style.marginTop = 0; designToggle.style.marginBottom = 0;
            float sx = ui.layout.width / Mathf.Max(1, Screen.width), sy = ui.layout.height / Mathf.Max(1, Screen.height);
            float bottom = Screen.safeArea.yMin * sy;
            float top = (Screen.height - Screen.safeArea.yMax) * sy;
            designPanel.style.paddingTop = 0;
            designPanel.style.paddingLeft = Mathf.Max(12f * upp, Screen.safeArea.xMin * sx + 8f * upp);
            designPanel.style.paddingRight = Mathf.Max(12f * upp, (Screen.width - Screen.safeArea.xMax) * sx + 8f * upp);
            // Home control: top-left inside the safe area (+8 pt), 44 pt min target. Home is hidden whenever the HUD is shown.
            homeButton.style.left = Screen.safeArea.xMin * sx + HomeButtonMetrics.InsetPt * upp;
            homeButton.style.top = top + HomeButtonMetrics.InsetPt * upp;
            HomeButtonMetrics.Apply(homeButton.style, upp);
            // Summary chip under Home (DESIGNER_SPEC §6).
            issueChip.style.left = homeButton.style.left;
            issueChip.style.top = top + (HomeButtonMetrics.InsetPt * 2 + HomeButtonMetrics.HeightPt) * upp;
            issueChip.style.fontSize = 15f * upp;
            issueChip.style.minHeight = 32f * upp;
            issueChip.style.paddingLeft = 12f * upp; issueChip.style.paddingRight = 12f * upp;
            issueChip.style.paddingTop = 6f * upp; issueChip.style.paddingBottom = 6f * upp;
            // Thumb row: 44 pt tools and a 52 pt Launch.
            foreach (var b in new[] { undoButton, redoButton, explodeButton })
            {
                if (b == null) continue;
                b.style.height = 52f * upp; b.style.minWidth = 56f * upp; b.style.fontSize = 15f * upp;
                b.style.marginRight = 6f * upp;
                float r = 12f * upp;
                b.style.borderTopLeftRadius = r; b.style.borderTopRightRadius = r; b.style.borderBottomLeftRadius = r; b.style.borderBottomRightRadius = r;
            }
            launchButton.style.height = 52f * upp; launchButton.style.fontSize = 20f * upp;
            launchReason.style.fontSize = 14f * upp;

            bool side = HangarPanelLayout.SidePanel(Screen.width, Screen.height);
            designPanel.EnableInClassList("side", side);
            if (side)
            {
                designPanel.style.width = DesignFraming.SidePanelPoints * upp;
                designPanel.style.top = top;
                designPanel.style.paddingBottom = Mathf.Max(8f * upp, bottom + 8f * upp);
                panel?.SetContentMax(-1f);
                designBody.style.flexGrow = 1;
            }
            else
            {
                designPanel.style.width = StyleKeyword.Null;
                designPanel.style.top = StyleKeyword.Null;
                designPanel.style.paddingBottom = designCollapsed ? bottom : Mathf.Max(8f * upp, bottom + 8f * upp);
                float hPt = ui.layout.height / Mathf.Max(0.01f, upp);
                panel?.SetContentMax(HangarPanelLayout.ContentMaxPt(hPt, bottom / Mathf.Max(0.01f, upp)) * upp);
                designBody.style.flexGrow = 0;
            }
        }

        void SetDesignCollapsed(bool collapsed)
        {
            designCollapsed = collapsed;
            designPanel.EnableInClassList("collapsed", collapsed);
            if (ui != null) ApplyDesignPanelMetrics();
            var row = ui.Q("launch-row"); if (row != null) row.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            if (launchReason != null) launchReason.style.display = collapsed ? DisplayStyle.None : StyleKeyword.Null;
            string name = session != null ? (session.SavedName ?? session.Design.Str("name") ?? "Design") + (session.Dirty ? " •" : "") : "Design";
            designToggle.text = (collapsed ? "▲  " : "▼  ") + name;
            // Accessibility pattern in this project is tooltip text (VoiceOver bridge is a listed gap, RESULTS.md).
            designToggle.tooltip = collapsed ? "Design panel, collapsed. Tap to expand." : "Design panel, expanded. Tap to collapse and see the whole rocket.";
        }

        /// <summary>Frame rocket + pad deck in the band above the panel's top edge (live rect, any device).
        /// On iPad the panel is on the side, so the whole height is free and HangarCamera shifts left instead.</summary>
        void ApplyDesignFraming()
        {
            if (cam == null || ui.layout.height <= 0 || designPanel.ClassListContains("hidden")) return;
            bool side = HangarPanelLayout.SidePanel(Screen.width, Screen.height);
            float f = side ? 1f : DesignFraming.VisibleFraction(ui.layout.height, designPanel.worldBound.yMin - ui.worldBound.yMin);
            if (hangarCam != null && hangarCam.enabled) hangarCam.SetBottomVisible(f);
            else FrameCamera(frameHeight, f);
        }

        /// <summary>Vehicle + tower/arms + deck rim, refreshed in BuildRocket; the camera fits all of them.</summary>
        System.Collections.Generic.List<Vector3> framePoints;

        /// <summary>Launch gate, summary chip and reason from the session's advisories (kid words unless Engineer is on).</summary>
        void ApplyValidation()
        {
            if (session == null || issueChip == null) return;
            bool engineer = panel != null ? panel.Engineer : PlayerPrefs.GetInt(HangarPanel.EngineerPref, 0) != 0;
            var adv = session.Advisories;
            int blocks = 0, warns = 0;
            foreach (var a in adv) { if (a.severity == AdvisorySeverity.Block) blocks++; else if (a.severity == AdvisorySeverity.Warn) warns++; }
            string chip = DesignerValidation.Chip(blocks, warns, engineer ? "engineer" : "standard");
            issueChip.text = chip;
            issueChip.EnableInClassList("hidden", chip.Length == 0 || designPanel.ClassListContains("hidden"));
            issueChip.EnableInClassList("warn-only", blocks == 0);
            issueChip.tooltip = chip.Length == 0 ? "" : chip + ". " + adv[0].kid + " Tap to see them all.";
            bool ok = session.LaunchEnabled;
            launchButton.SetEnabled(ok);
            var firstBlock = adv.Find(a => a.severity == AdvisorySeverity.Block);
            string reason = ok ? "" : "Fix first: " + (firstBlock != null ? (engineer ? firstBlock.engineer : firstBlock.kid) : DesignerValidation.KidFallback);
            launchReason.text = reason;
            launchReason.EnableInClassList("hidden", ok);
            launchButton.tooltip = ok ? "Roll out and launch the current design" : "Launch unavailable. " + reason;
            if (undoButton != null)
            {
                undoButton.SetEnabled(session.history.CanUndo);
                redoButton.SetEnabled(session.history.CanRedo);
                undoButton.tooltip = session.history.CanUndo ? "Undo " + session.history.NextUndo.Spoken : "Nothing to undo";
                redoButton.tooltip = session.history.CanRedo ? "Redo " + session.history.NextRedo.Spoken : "Nothing to redo";
            }
            rocketView.SetIssues(adv);
        }

        /// <summary>
        /// Rebuild the vehicle: per-part Hangar meshes (paint, selection, exploded view), tank shells on their stage,
        /// plume, tower and framing. The combined flight mesh still sizes the plume and tower (same stations).
        /// Any failure leaves the previous rocket and logs a warning; the panel keeps working.
        /// </summary>
        void BuildRocket()
        {
            rocketDirty = false;
            lastRocketBuild = Time.unscaledTime;
            if (design == null) return;
            RocketMeshBuilder.Result r;
            List<RocketMeshBuilder.PartMesh> parts;
            try { r = RocketMeshBuilder.Build(SliceData.Catalog, design); parts = RocketMeshBuilder.BuildParts(SliceData.Catalog, design); }
            catch (System.Exception e) { Debug.LogWarning("[Hangar] rocket build skipped: " + e.Message); return; }
            rocketView.Clear();
            if (rocketGO != null) Destroy(rocketGO);
            if (combinedMesh != null) Destroy(combinedMesh);
            combinedMesh = r.mesh;
            rocketGO = new GameObject("Rocket"); rocketGO.transform.SetParent(vehicleRoot, false);
            bool wasExploded = rocketView.Exploded;
            rocketView.Build(parts, rocketGO.transform, bodyMat, design.stages.Count, r.height);
            rocketView.ApplyLook(session?.Design, SliceData.Hangar);
            if (session != null) { rocketView.SetSelected(session.Selected, true); rocketView.SetIssues(session.Advisories); }
            if (wasExploded) rocketView.SetExploded(true, true);
            var plates = new List<AttachPoint>();
            AddTankShells(r, plates);
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
            if (hangarCam != null)
            {
                bool open = rocketView.Exploded;
                hangarCam.SetVehicle(open ? ExplodedView.ExplodedHeight(frameHeight, design.stages.Count) : frameHeight, open ? null : framePoints);
            }
            ApplyDesignFraming();
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
            int strapIndex = 1, strapShell = 0;
            var cat = SliceData.Catalog;
            for (int i = 0; i < r.tanks.Count; i++)
            {
                var t = r.tanks[i];
                if (t.BarrelLength <= 0.1f || t.Radius <= 0.05f) continue;
                var spec = Kestrel.Vehicle.TankShellSpec.Default(t.Radius * 1.005f, t.BarrelLength, seed + (ulong)i * 7919UL);
                if (spec.Validate().Count > 0) continue;
                var go = new GameObject($"TankShell_S{t.Stage + 1}{(t.StrapOn ? "_strap" + i : "")}", typeof(MeshFilter), typeof(MeshRenderer));
                var origin = t.Offset + Vector3.up * (t.BarrelBottom - spec.DomeHeight);
                // Ride on the Hangar part so the shell moves with it in the exploded view.
                Transform host = rocketGO.transform; var local = origin;
                if (t.StrapOn && rocketView.TryGet(PartRef.Boosters, out var bp) && strapShell < bp.copies.Count) { host = bp.copies[strapShell]; local = origin - t.Offset; }
                else if (!t.StrapOn && rocketView.RootOf(PartRef.Stage(t.Stage)) is Transform sr) host = sr;
                if (t.StrapOn) strapShell++;
                go.transform.SetParent(host, false);
                go.transform.localPosition = local;
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
            if (session == null || !session.LaunchEnabled) return; // blocks gate Launch; warnings never do
            if (rocketDirty) BuildRocket();
            submitted = false;
            rocketView.SetExploded(false, true);
            rocketView.SetSelected(session.Selected, false);
            rocketView.SetIssues(null);
            if (hangarCam != null) { hangarCam.ClearFocus(); hangarCam.enabled = false; }
            camOffset = cam.transform.position - (vehicleRoot != null ? vehicleRoot.position : Vector3.zero);
            designPanel.AddToClassList("hidden"); homeButton.AddToClassList("hidden"); issueChip.AddToClassList("hidden");
            hud.RemoveFromClassList("hidden"); cam.ResetProjectionMatrix(); hMsg.text = "";
            flight.enabled = true;
            flight.Launch(design);
            var pad = tower != null ? tower.Find("Pad") : null;
            var renderer = pad != null ? pad.GetComponent<Kestrel.Pad.PadRenderer>() : null;
            if (renderer != null) { renderer.ResetSequence(); renderer.AdvanceTo(0); }
        }

        void BackToDesign()
        {
            flight.enabled = false; cam.ResetProjectionMatrix();
            hud.AddToClassList("hidden"); designPanel.RemoveFromClassList("hidden"); homeButton.RemoveFromClassList("hidden"); again.AddToClassList("hidden");
            if (hangarCam != null) { hangarCam.enabled = true; hangarCam.Reframe(); }
            if (session != null) rocketView.SetSelected(session.Selected, true);
            BuildRocket();
            ApplyValidation();
        }

        /// <summary>Hangar per-frame work: the explode animation and a rate-limited rebuild while a stepper is held.</summary>
        void Update()
        {
            if (hud != null && !hud.ClassListContains("hidden")) return;
            if (rocketDirty && Time.unscaledTime - lastRocketBuild >= RebuildEverySeconds) BuildRocket();
            rocketView.Step(Time.unscaledDeltaTime);
        }

        /// <summary>Longest a held stepper waits for the 3D rocket to catch up (the readout updates every tick).</summary>
        public const float RebuildEverySeconds = 0.12f;

        void LateUpdate()
        {
            if (sky != null && flight != null) sky.SetAltitude(flight.Altitude);
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
            // Keep the ground in view from above, and keep it under the vehicle (no edge, horizon at any downrange).
            ground.position = new Vector3(v.x, ground.position.y, ground.position.z);
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

// Vertical-slice entry point: Hangar designer (UI Toolkit) -> flight liftoff..MECO -> HUD -> Game Center.
// The Hangar edits a Kestrel.Core.Hangar.HangarSession (undo/redo, validation, data sheet); HangarPanel is the view,
// HangarRocketView draws the rocket per part (paint, selection, exploded view) and HangarCamera orbits and focuses.
// Everything is created from code at runtime; the scene only needs one GameObject with this
// component (the editor bootstrap generates Assets/Scenes/Slice.unity).
using System.Collections.Generic;
using System.Linq;
using Kestrel.Core.Designer;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Timeline;
using Kestrel.Core.Vehicle;
using Kestrel.Game.Environment;
using Kestrel.Kit.Contracts;
using Kestrel.Site;
using Kestrel.Slice.DesignerV3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class SliceApp : MonoBehaviour
    {
        public PanelSettings PanelSettings;   // assigned by the editor bootstrap (Resources/UI/KH_PanelSettings)
        Design design; HangarSession session; DesignLibrary library; HangarPanel panel; DesignerV3Panel designer; HangarRocketView rocketView = new HangarRocketView(); HangarCamera hangarCam;
        Button undoButton, redoButton, explodeButton; bool rocketDirty = true; float lastRocketBuild = -1f; Mesh combinedMesh; Material flightMat;
        public bool TankCutaway { get; private set; }
        HangarExplodedView explodedView; Material explodedMat;
        // Feedback (PR 3): haptics and VoiceOver announcements after edits; see HangarFeel / HangarNative.
        FeelState feelBase, feelNow; float lastSnap = -1f, announceAt = -1f; string pendingAnnounce; bool nextIsUndo, nextIsRedo, nextIsLoad = true;
        Camera cam; Transform worldRoot, vehicleRoot, tower, ground, deck, deckTop; FlightDriver flight; FlightJuice juice; FlightStageShow staging; GameObject rocketGO;
        FlightChrome chrome;
        AscentHud ascentHud; Design flyingDesign;
        BirdFlockView birds;
        VisualElement ui, designPanel, designBody, hud; Button designToggle, homeButton; bool designCollapsed; float dragStartY; Label hT, hStage, hAlt, hVel, hMach, hQ, hThr, hDr, hMsg; Button again;
        Material bodyMat, plumeMat; bool submitted; SkyRig sky;
        // U1 validation. Layer inputs are placeholders until save/Settings wiring (U4): simple by default.
        public int OrbitsCompleted; public bool EngineerEnabled;
        Label issueChip, launchReason; Button launchButton;
        LaunchSequence sequence; bool counting; string pinnedEvent, chipSig;
        double requiredMps, spareMps; double? maxQKpa; string orbitLabel = "";
        VisualElement seqRow; Label hVerdict, hPulse, hCallout; Button skipCount;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            bodyMat = Resources.Load<Material>("Materials/KH_Body");
            plumeMat = Resources.Load<Material>("Materials/KH_Plume");
            BuildScene();
            BuildUI();
            Kestrel.Game.Audio.AudioDirector.Attach(flight);
            _ = GameCenterBridge.Impl.Authenticate();
            StartSession();
            PostFlightSource.Bind(RestartFlight, ExitToHome);
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
            session.SaveStateChanged += UpdateHandle;
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
                panel.ReduceMotionChanged += _ => ApplyReduceMotion();
                panel.ExplodeRequested += SetExploded;
                panel.TankCutawayRequested += () => SetTankCutaway(!TankCutaway);
                panel.EngineerChanged += _ => ApplyValidation();
                // #213 is on main. One registration: portrait designer beside the Hangar.
                designer = DesignerV3Panel.Attach(session, ui, upp);
                if (designer != null) designer.LaunchRequested += Launch;
            }
            else
            {
                panel.SetScale(upp);
                designer?.SetScale(upp);
            }
            ApplyDesignPanelMetrics();
        }

        /// <summary>The portrait designer mounted beside the Hangar, once the design sheet exists.</summary>
        public DesignerV3Panel Designer => designer;

        /// <summary>Open the vehicle designer in the sheet, or return to the Hangar.</summary>
        public void ShowDesigner(bool on = true)
        {
            if (designer == null) TryBuildPanel();
            if (on && designCollapsed) SetDesignCollapsed(false);
            designer?.SetOpen(on);
        }

        /// <summary>Every document change: readout gate, chip, undo labels, and a (rate-limited) rocket rebuild.</summary>
        void OnDesignChanged()
        {
            if (session == null) return;
            try { design = Design.FromJson(session.Design); }
            catch (System.Exception e) { Debug.LogWarning("[Hangar] design parse: " + e.Message); }
            rocketDirty = true;
            ApplyValidation();
            Feedback();
            UpdateHandle();
        }

        /// <summary>Panel handle: collapse arrow, design name, and a dot while there are unsaved changes.</summary>
        void UpdateHandle()
        {
            if (designToggle == null) return;
            string name = session != null ? (session.SavedName ?? session.Design.Str("name") ?? "Design") + (session.Dirty ? " •" : "") : "Design";
            designToggle.text = (designCollapsed ? "▲  " : "▼  ") + name;
        }

        void OnSelectionChanged(PartRef p)
        {
            rocketView.SetSelected(p, true);
            if (session != null && session.Loading) return; // a load moved the selection; the player did not
            PlayHaptic(Haptic.Snap);
            var info = session != null ? HangarParts.List(session.Design).Find(x => x.part == p) : null;
            Queue("Selected " + (info != null ? info.label : HangarParts.Short(p)) + ".");
        }

        /// <summary>After each change: a haptic now, and one announcement once the edits settle (a held stepper speaks once).</summary>
        void Feedback()
        {
            if (session == null) return;
            var before = feelNow;
            feelNow = FeelState.Of(session);
            // A load (open a save, start a template) clears history: no step to announce, no haptic.
            if (nextIsLoad || session.Loading || (!session.history.CanUndo && !session.history.CanRedo)) { nextIsLoad = false; feelBase = feelNow; pendingAnnounce = null; return; }
            if (pendingAnnounce == null) feelBase = before;
            var step = nextIsRedo ? session.history.NextUndo : nextIsUndo ? session.history.NextRedo : session.history.NextUndo;
            PlayHaptic(HangarFeel.For(before, feelNow, stepped: true));
            Queue(HangarFeel.Announce(step, feelBase, feelNow, nextIsUndo, nextIsRedo));
        }

        void PlayHaptic(Haptic h)
        {
            if (h == Haptic.None) return;
            if (h == Haptic.Snap && Time.unscaledTime - lastSnap < HangarFeel.SnapMinSeconds) return;
            var pattern = HangarFeel.Pattern(h, SliceData.Feel);
            if (pattern.Length == 0) return; // designer-feel.json can turn a pattern off
            if (h == Haptic.Snap) lastSnap = Time.unscaledTime;
            HangarNative.Play(h, pattern);
        }

        void Queue(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            pendingAnnounce = text;
            announceAt = Time.unscaledTime + HangarFeel.AnnounceAfterSeconds;
        }

        void FlushAnnouncement()
        {
            if (pendingAnnounce == null || Time.unscaledTime < announceAt) return;
            HangarNative.Announce(pendingAnnounce);
            pendingAnnounce = null;
            feelBase = feelNow;
        }

        /// <summary>The iOS Reduce Motion switch can change while the game is in the background.</summary>
        void OnApplicationFocus(bool focused)
        {
            if (focused) ApplyReduceMotion();
        }

        /// <summary>Reduce Motion: the in-game switch or the iOS setting, whichever is on.</summary>
        void ApplyReduceMotion()
        {
            if (hangarCam == null) return;
            hangarCam.ReduceMotion = HangarNative.SystemReduceMotion
                || PlayerPrefs.GetInt(HangarCamera.ReduceMotionPref, 0) != 0
                || PlayerPrefs.GetInt(HangarCamera.ReduceFlashingPref, 0) != 0;
            if (birds != null) birds.FlapScale = hangarCam.ReduceMotion ? 0f : 1f;
        }

        void FocusPart(PartRef p)
        {
            if (hangarCam == null) return;
            if (rocketView.Bounds(p, out float y, out float h)) hangarCam.Focus(y, h);
        }

        void OnTapped(Vector2 screenPx)
        {
            if (session == null || hud == null || !hud.ClassListContains("hidden")) return;
            if (hangarCam != null && hangarCam.Exploded) return; // picking reads the assembled parts
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
            cam.gameObject.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.Skybox;
            // The coast disc has to reach the geometric horizon the chase camera frames. SkyRig still owns fog.
            cam.nearClipPlane = 0.5f; cam.farClipPlane = SiteView.FarClip; cam.fieldOfView = 50;
            hangarCam = cam.gameObject.AddComponent<HangarCamera>();
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.3f; sun.transform.rotation = Quaternion.Euler(40, -30, 0);
            sky = SkyRig.Attach(cam, sun);
            hangarCam.Tapped += OnTapped;
            ApplyReduceMotion();
            worldRoot = new GameObject("World").transform;
            vehicleRoot = new GameObject("Vehicle").transform; vehicleRoot.SetParent(worldRoot, false);
            flight = gameObject.AddComponent<FlightDriver>(); flight.World = worldRoot; flight.Vehicle = vehicleRoot;
            juice = gameObject.AddComponent<FlightJuice>();
            juice.Arm(worldRoot, bodyMat);
            // Pier 7: scrub, road, coast and curved ocean. Sea level stays at GroundY so the chase-camera
            // clamp still looks down onto the shore. The site is pad-anchored (it does not slide with the rocket).
            var site = SiteView.Create(worldRoot, SiteGenerator.Pier7Seed, SiteView.LodForQuality(QualitySettings.GetQualityLevel()));
            site.BindSun(sun);
            ground = site.transform;
            flight.Anchor(ground, 0, PadGround.GroundY, 0);
            birds = BirdFlockView.Create(worldRoot);
            birds.Bind(new BirdTrack(flight));
            birds.BindSun(sun);
            birds.FlapScale = hangarCam != null && hangarCam.ReduceMotion ? 0f : 1f;
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
            hVerdict = ui.Q<Label>("h-verdict"); hPulse = ui.Q<Label>("h-pulse"); hCallout = ui.Q<Label>("h-callout");
            seqRow = ui.Q("h-seq"); skipCount = ui.Q<Button>("skip-count");
            if (skipCount != null) skipCount.clicked += SkipCount;
            again = ui.Q<Button>("again");
            launchButton = ui.Q<Button>("launch");
            launchButton.clicked += Launch;
            undoButton = ui.Q<Button>("undo"); redoButton = ui.Q<Button>("redo"); explodeButton = ui.Q<Button>("explode");
            undoButton.clicked += () => { nextIsUndo = true; var st = session?.Undo(); nextIsUndo = false; if (st != null) undoButton.tooltip = "Undid: " + st.Spoken; };
            redoButton.clicked += () => { nextIsRedo = true; var st = session?.Redo(); nextIsRedo = false; if (st != null) redoButton.tooltip = "Redid: " + st.Spoken; };
            explodeButton.clicked += () => SetExploded(!(hangarCam != null && hangarCam.Exploded));
            explodeButton.tooltip = "Exploded view: pull the parts apart";
            issueChip = ui.Q<Label>("issue-chip"); launchReason = ui.Q<Label>("launch-reason");
            issueChip.RegisterCallback<ClickEvent>(_ => { panel?.Show(HangarPanel.Tab.Data); if (designCollapsed) SetDesignCollapsed(false); });
            again.clicked += BackToDesign;
            chrome = gameObject.AddComponent<FlightChrome>();
            chrome.Bind(hud, SetFlightPaused, RestartFlight, ExitToHome);
            chrome.Layout(UnitsPerPoint());
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
            chrome?.Layout(UnitsPerPoint());
            ascentHud?.Layout(UnitsPerPoint());
            staging?.Layout(UnitsPerPoint());
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
            var designerToggle = ui.Q<Button>(DesignerV3Panel.ToggleName);
            foreach (var b in new[] { undoButton, redoButton, explodeButton, designerToggle })
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
            float hPt = ui.layout.height / Mathf.Max(0.01f, upp);
            float safeBottomPt = bottom / Mathf.Max(0.01f, upp);
            if (side)
            {
                designPanel.style.width = DesignFraming.SidePanelPoints * upp;
                designPanel.style.top = top;
                designPanel.style.paddingBottom = Mathf.Max(8f * upp, bottom + 8f * upp);
                panel?.SetContentMax(-1f);
                designer?.SetContentMax(-1f);
                designBody.style.flexGrow = 1;
                if (designer != null) designer.Root.style.flexGrow = 1;
            }
            else
            {
                designPanel.style.width = StyleKeyword.Null;
                designPanel.style.top = StyleKeyword.Null;
                designPanel.style.paddingBottom = designCollapsed ? bottom : Mathf.Max(8f * upp, bottom + 8f * upp);
                panel?.SetContentMax(HangarPanelLayout.ContentMaxPt(hPt, safeBottomPt) * upp);
                designer?.SetContentMax(DesignerV3Layout.ContentMaxPt(hPt, safeBottomPt, true) * upp);
                designBody.style.flexGrow = 0;
                if (designer != null) designer.Root.style.flexGrow = 0;
            }
        }

        void SetDesignCollapsed(bool collapsed)
        {
            designCollapsed = collapsed;
            designPanel.EnableInClassList("collapsed", collapsed);
            designer?.SetSheetCollapsed(collapsed);
            if (ui != null) ApplyDesignPanelMetrics();
            var row = ui.Q("launch-row"); if (row != null) row.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            if (launchReason != null) launchReason.style.display = collapsed ? DisplayStyle.None : StyleKeyword.Null;
            UpdateHandle();
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
            // The combined flight mesh sits on the root, undrawn in the Hangar (the parts are the picture). Its
            // renderer is the gate the instanced bells, struts and livery marks follow, and the mesh FailureHost
            // splits in flight. Launch swaps it in for the parts.
            rocketGO.AddComponent<MeshFilter>().sharedMesh = r.mesh;
            var mr = rocketGO.AddComponent<MeshRenderer>();
            if (flightMat != null) Destroy(flightMat);
            flightMat = bodyMat != null ? new Material(bodyMat) : null;
            if (flightMat != null) { flightMat.color = HangarRocketView.Hex(design.liveryPrimary, Color.white); mr.sharedMaterial = flightMat; }
            if (juice != null) juice.BindBody(flightMat);
            mr.forceRenderingOff = true;
            rocketView.Build(parts, rocketGO.transform, bodyMat);
            rocketView.ApplyLook(session?.Design, SliceData.Hangar);
            if (session != null) { rocketView.SetSelected(session.Selected, true); rocketView.SetIssues(session.Advisories); }
            var plates = new List<AttachPoint>();
            AddTankShells(r, plates);
            rocketView.Repaint(); // the shells hide most of each tank body, so they take its paint
            AddBells(r);
            AddAirframe(r);
            AddLanding(r);
            AddLivery(r);
            AddFairing(r);
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
                hangarCam.SetVehicle(frameHeight, framePoints);
                // A focused part may have moved or gone; aim at where the selection is now.
                if (hangarCam.Focused)
                {
                    if (session != null && rocketView.Bounds(session.Selected, out float fy, out _)) hangarCam.Refocus(fy);
                    else hangarCam.ClearFocus();
                }
            }
            BuildExploded();
            ApplyTankCutaway();
            ApplyDesignFraming();
            // One registration: failure visuals and the post-flight card.
            Kestrel.Game.Failure.FailureHost.Attach(rocketGO, flight, ui, r);
            AttachStaging(r);
        }

        /// <summary>Staging picture for this rocket. A failed prepare leaves the combined mesh as the flight picture.</summary>
        void AttachStaging(RocketMeshBuilder.Result built)
        {
            if (rocketGO == null) return;
            staging = rocketGO.GetComponent<FlightStageShow>();
            if (staging == null) staging = rocketGO.AddComponent<FlightStageShow>();
            try { staging.Prepare(SliceData.Catalog, design, built, hud); }
            catch (System.Exception e) { Debug.LogWarning("[Staging] " + e.Message); }
        }

        /// <summary>R2's exploded view (HangarExplodedView), rebuilt with the rocket and painted in the livery's main colour.
        /// It keeps its open/closed state across rebuilds so a held stepper doesn't snap it shut.</summary>
        void BuildExploded()
        {
            bool open = hangarCam != null && hangarCam.Exploded;
            if (explodedMat != null) Destroy(explodedMat);
            explodedMat = bodyMat != null ? new Material(bodyMat) : null;
            if (explodedMat != null) explodedMat.color = HangarRocketView.Hex(session?.Design.Obj("livery")?.Str("primary"), Color.white);
            var explodedGo = new GameObject("Exploded");
            explodedView = explodedGo.AddComponent<HangarExplodedView>();
            explodedView.AttachTo(rocketGO.transform);
            explodedView.Build(SliceData.Catalog, design, explodedMat);
            if (hangarCam != null) hangarCam.ExplodedView = explodedView;
            if (open) explodedView.SetExploded(true, true);
            SetAssembledDrawn(!open);
        }

        /// <summary>Separated parts live on the vehicle. The assembled rocket draws only while the view is closed.</summary>
        void SetExploded(bool on) => SetExploded(on, false);

        void SetExploded(bool on, bool immediate)
        {
            if (on && TankCutaway) SetTankCutaway(false);
            if (explodeButton != null) { explodeButton.text = on ? "Join" : "Parts"; explodeButton.tooltip = on ? "Put the rocket back together" : "Exploded view: pull the parts apart"; }
            if (hangarCam != null) { hangarCam.ClearFocus(); hangarCam.SetExploded(on, immediate); }
            else if (explodedView != null) explodedView.SetExploded(on, immediate);
            // While the ease is still open, the separated parts are the picture. The assembled
            // rocket comes back only once they have seated (or immediately, when Reduce Motion snaps shut).
            if (!on && explodedView != null && explodedView.Shown > 0.001f && !immediate) return;
            SetAssembledDrawn(!on);
        }

        /// <summary>Flight draws the combined mesh (FailureHost can split it); the Hangar draws the parts.
        /// BackToDesign rebuilds the rocket, which puts the parts back.</summary>
        void ShowFlightMesh(bool on)
        {
            rocketView.SetVisible(!on);
            var mr = rocketGO != null ? rocketGO.GetComponent<MeshRenderer>() : null;
            if (mr != null) mr.forceRenderingOff = !on;
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

        /// <summary>Expose the existing tank interior and isogrid in the real designer.</summary>
        public void SetTankCutaway(bool on)
        {
            if (on) SetExploded(false, true);
            TankCutaway = on;
            panel?.SetTankCutaway(on);
            SetAssembledDrawn(true);
            ApplyTankCutaway();
        }

        void ApplyTankCutaway()
        {
            if (rocketGO == null) return;
            foreach (var shell in rocketGO.GetComponentsInChildren<Kestrel.Vehicle.TankShellView>(true))
            {
                shell.SetCutaway(TankCutaway);
                // The original solid lathe behind this overlay would fill the removed sector.
                var solid = shell.transform.parent.GetComponent<MeshRenderer>();
                if (TankCutaway && solid != null) solid.enabled = false;
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

        /// <summary>Interstage stringers and joint rings, plus one raceway on the tower side of each tank.
        /// The lathe cone and barrel stay the silhouette; these tubes sit just outside them.</summary>
        void AddAirframe(RocketMeshBuilder.Result r)
        {
            var poses = new List<Kestrel.Vehicle.StrutPose>();
            if (r.interstages != null)
                for (int i = 0; i < r.interstages.Count; i++)
                {
                    var s = r.interstages[i];
                    poses.AddRange(Kestrel.Vehicle.AirframeLayout.Interstage(s.Y0, s.Y1, s.R0, s.R1));
                }
            if (r.tanks != null)
                for (int i = 0; i < r.tanks.Count; i++)
                {
                    var t = r.tanks[i];
                    if (t.BarrelLength <= 0.2f || t.Radius <= 0.05f) continue;
                    poses.Add(Kestrel.Vehicle.AirframeLayout.RacewayRun(t.Offset.x, t.Offset.y, t.Offset.z, t.Radius, t.BarrelBottom, t.BarrelLength));
                }
            if (poses.Count == 0) return;
            var cluster = rocketGO.AddComponent<Kestrel.Vehicle.StrutCluster>();
            cluster.ViewCamera = cam;
            cluster.Gate = rocketGO.GetComponent<MeshRenderer>();
            cluster.Init(poses, Kestrel.Kit.KitBudgets.Medium);
        }

        /// <summary>Deployed legs and grid fins, only when the design asks for them. Stock Kestrel Heavy has both.</summary>
        void AddLanding(RocketMeshBuilder.Result r)
        {
            if (design == null || (!design.legs && !design.gridFins)) return;
            float radius = 0f;
            if (r.tanks != null)
                for (int i = 0; i < r.tanks.Count; i++)
                    if (r.tanks[i].Stage == 0 && !r.tanks[i].StrapOn) radius = r.tanks[i].Radius;
            if (radius < 0.05f) return;
            var gate = rocketGO.GetComponent<MeshRenderer>();
            if (design.legs)
            {
                var legs = Kestrel.Vehicle.LandingLayout.Legs(radius, r.stage1AftY);
                var cluster = rocketGO.AddComponent<Kestrel.Vehicle.StrutCluster>();
                cluster.ViewCamera = cam;
                cluster.Gate = gate;
                cluster.Init(legs, Kestrel.Kit.KitBudgets.Medium);
            }
            if (design.gridFins)
            {
                float station = r.stage1Top;
                if (r.interstages != null && r.interstages.Count > 0) station = r.interstages[0].Y0;
                var fins = Kestrel.Vehicle.LandingLayout.Fins(radius, station);
                var cluster = rocketGO.AddComponent<Kestrel.Vehicle.FinCluster>();
                cluster.ViewCamera = cam;
                cluster.Gate = gate;
                cluster.Init(fins, Kestrel.Kit.KitBudgets.Medium);
            }
        }

        /// <summary>Circumferential panel lines and two seams on every tank, plus an accent band and decal blocks
        /// on the first stage. Paint comes from the livery colours, not a stripe texture.</summary>
        void AddLivery(RocketMeshBuilder.Result r)
        {
            if (r.tanks == null || r.tanks.Count == 0) return;
            var marks = new List<Kestrel.Vehicle.LiveryMark>();
            bool accentUsed = false;
            for (int i = 0; i < r.tanks.Count; i++)
            {
                var t = r.tanks[i];
                if (t.BarrelLength <= 0.5f || t.Radius <= 0.05f) continue;
                bool accent = !accentUsed && t.Stage == 0 && !t.StrapOn;
                if (accent) accentUsed = true;
                marks.AddRange(Kestrel.Vehicle.LiveryLayout.Tank(t.Offset.x, t.Offset.y, t.Offset.z, t.Radius, t.BarrelBottom, t.BarrelLength, accent));
            }
            if (marks.Count == 0) return;
            var accentCol = new Color(0.10f, 0.24f, 0.43f);
            if (design != null) ColorUtility.TryParseHtmlString(design.liveryAccent, out accentCol);
            var cluster = rocketGO.AddComponent<Kestrel.Vehicle.LiveryCluster>();
            cluster.ViewCamera = cam;
            cluster.Gate = rocketGO.GetComponent<MeshRenderer>();
            cluster.Init(marks, Kestrel.Kit.KitBudgets.Medium, accentCol);
        }

        /// <summary>Separation seams, base ring and shoulder on the fairing. The ogive lathe is unchanged, so plume height stays put.</summary>
        void AddFairing(RocketMeshBuilder.Result r)
        {
            if (!r.fairing.Present) return;
            var f = r.fairing;
            var poses = Kestrel.Vehicle.FairingLayout.Build(f.BaseY, f.Radius, f.Length, f.NoseFraction, f.Conic);
            if (poses.Count == 0) return;
            var cluster = rocketGO.AddComponent<Kestrel.Vehicle.StrutCluster>();
            cluster.ViewCamera = cam;
            cluster.Gate = rocketGO.GetComponent<MeshRenderer>();
            cluster.Init(poses, Kestrel.Kit.KitBudgets.Medium);
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
            flyingDesign = design;
            EnterFlight();
        }

        /// <summary>Same design that left the pad. The launch gate already passed.</summary>
        void RestartFlight()
        {
            SetFlightPaused(false);
            if (flyingDesign != null) design = flyingDesign;
            if (design == null) return;
            EnterFlight();
        }

        void ExitToHome()
        {
            SetFlightPaused(false);
            GoHome();
        }

        void SetFlightPaused(bool on)
        {
            if (flight != null) flight.Hold(on);
            var audio = flight != null ? flight.GetComponent<Kestrel.Game.Audio.AudioDirector>() : null;
            if (audio != null) audio.Hold(on);
            var voice = flight != null ? flight.GetComponent<Kestrel.Game.Audio.NarrationPlayer>() : null;
            if (voice != null) voice.Hold(on);
            Kestrel.Game.Audio.MusicPlayer.Instance?.SetPaused(on);
            chrome?.Show(on);
        }

        void EnterFlight()
        {
            if (session == null || design == null) return;
            SetFlightPaused(false);
            Kestrel.Game.Audio.MusicPlayer.Instance?.Play(Kestrel.Game.Audio.MusicBed.Flight);
            submitted = false;
            SetTankCutaway(false);
            SetExploded(false, true);
            // Nothing queued from the Hangar is spoken after the flight.
            pendingAnnounce = null; feelBase = feelNow;
            if (hangarCam != null)
            {
                // Snap back to the assembled pose before sampling the chase offset, so liftoff
                // does not inherit the closer exploded camera.
                hangarCam.ApplyPose();
                hangarCam.enabled = false;
            }
            SetAssembledDrawn(true);
            if (explodeButton != null) explodeButton.text = "Parts";
            rocketView.SetSelected(session.Selected, false);
            rocketView.SetIssues(null);
            ShowFlightMesh(true);
            camOffset = cam.transform.position - (vehicleRoot != null ? vehicleRoot.position : Vector3.zero);
            designPanel.AddToClassList("hidden"); homeButton.AddToClassList("hidden"); issueChip.AddToClassList("hidden");
            hud.RemoveFromClassList("hidden"); cam.ResetProjectionMatrix(); hMsg.text = "";
            pinnedEvent = null; chipSig = null;
            var sheet = session.Sheet;
            requiredMps = sheet != null ? sheet.requiredDeltaV_mps : 0;
            spareMps = sheet != null ? sheet.totalDeltaV_mps - sheet.requiredDeltaV_mps : 0;
            maxQKpa = sheet != null ? sheet.maxQ_kPa : null;
            orbitLabel = sheet != null ? sheet.orbitLabel : "";
            sequence = OpenSequence();
            if (sequence != null && !sequence.Liftoff)
            {
                // The full count is long. Open on the last few seconds, the part a webcast actually shows.
                if (sequence.Time < -12) sequence.Step(-12 - sequence.Time);
                counting = !sequence.Liftoff;
            }
            else counting = false;
            if (skipCount != null) skipCount.EnableInClassList("hidden", !counting);
            var pad = tower != null ? tower.Find("Pad") : null;
            var renderer = pad != null ? pad.GetComponent<Kestrel.Pad.PadRenderer>() : null;
            if (renderer != null) renderer.ResetSequence();
            if (!counting) BeginAscent();
            else RefreshWebcast(sequence.Time, false);
            chrome?.OnFlightStarted();
        }

        LaunchSequence OpenSequence()
        {
            try
            {
                var cat = SliceData.Catalog;
                if (cat == null || design == null || design.stages == null || design.stages.Count == 0) return null;
                string mode = EngineerEnabled ? "engineer" : "standard";
                var tl = TimelineGenerator.Generate(cat, design, Kestrel.Core.Timeline.Pad.For(cat, design), new Mission { mode = mode }, 1);
                return LaunchSequence.Open(tl);
            }
            catch (System.Exception) { return null; }
        }

        void SkipCount()
        {
            if (!counting || sequence == null) return;
            sequence.Step(1e6);
            counting = false;
            if (skipCount != null) skipCount.AddToClassList("hidden");
            BeginAscent();
        }

        void BeginAscent()
        {
            counting = false;
            if (skipCount != null) skipCount.AddToClassList("hidden");
            if (juice != null) juice.Reset();
            flight.enabled = true;
            bool engineer = EngineerEnabled
                || PlayerPrefs.GetInt(DesignerV3Panel.EngineerPref, 0) != 0
                || PlayerPrefs.GetInt(HangarPanel.EngineerPref, 0) != 0;
            bool reduced = hangarCam != null && hangarCam.ReduceMotion;
            AscentHook hook = null;
            PostFlightSource.Arm(SliceData.Catalog, design, session != null ? session.Design : null, engineer);
            try { hook = AscentFlight.Prepare(SliceData.Catalog, design, session != null ? session.Design : null, engineer, reduced); }
            catch (System.Exception e) { Debug.LogWarning("[Ascent] " + e.Message); }
            flight.Launch(design, hook);
            if (ascentHud == null) ascentHud = new AscentHud();
            ascentHud.Open(hud, UnitsPerPoint());
            ascentHud.Present(flight);
            staging?.Arm(flight, reduced);
            staging?.Layout(UnitsPerPoint());
        }

        void BackToDesign()
        {
            SetFlightPaused(false);
            Kestrel.Game.Audio.MusicPlayer.Instance?.Select(false);
            counting = false; sequence = null; flight.enabled = false; cam.ResetProjectionMatrix();
            if (juice != null) juice.Reset();
            cam.fieldOfView = FlightJuice.BaseFov;
            ascentHud?.Close();
            hud.AddToClassList("hidden"); designPanel.RemoveFromClassList("hidden"); homeButton.RemoveFromClassList("hidden"); again.AddToClassList("hidden");
            if (hangarCam != null) { hangarCam.enabled = true; hangarCam.Reframe(); }
            if (session != null) rocketView.SetSelected(session.Selected, true);
            BuildRocket();
            ApplyValidation();
        }

        /// <summary>Hangar per-frame work: a rate-limited rocket rebuild while a stepper is held.</summary>
        void Update()
        {
            if (hud != null && !hud.ClassListContains("hidden")) { chrome?.TickInput(); return; }
            designer?.TickReference();
            if (rocketDirty && Time.unscaledTime - lastRocketBuild >= RebuildEverySeconds) BuildRocket();
            FlushAnnouncement();
        }

        /// <summary>Longest a held stepper waits for the 3D rocket to catch up (the readout updates every tick).</summary>
        public const float RebuildEverySeconds = 0.12f;

        void LateUpdate()
        {
            if (sky != null && flight != null) sky.SetAltitude(flight.Altitude);
            if (explodedView != null && hangarCam != null && hangarCam.enabled)
            {
                SetAssembledDrawn(!explodedView.IsExploded && explodedView.Shown <= 0.001f);
                ApplyTankCutaway();
            }
            if (birds != null) birds.TrackFlight = hud != null && !hud.ClassListContains("hidden");
            if (chrome != null && chrome.Paused) return;
            if (counting) { TickCount(); return; }
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
            double qLim = maxQKpa.HasValue ? maxQKpa.Value * 1000.0 : 0;
            double margin = requiredMps > 1 ? spareMps / requiredMps : 0;
            bool reduced = hangarCam != null && hangarCam.ReduceMotion;
            if (juice != null) juice.Present(flight, rocketHeight, qLim, margin, reduced, Time.deltaTime);
            var v = vehicleRoot.position; var up = vehicleRoot.up;
            camOffset = ChaseCamera.StepOffset(camOffset, ChaseCamera.DesiredOffset(up, rocketHeight, (float)flight.Altitude), 4f, Time.deltaTime);
            // Manual orbit/zoom rides on the chase offset. Follow leaves the chase pose unchanged.
            // Lead is this frame only. Folding it into camOffset would be smoothed back in and grow about 15×.
            var framed = chrome != null ? chrome.Frame(camOffset) : camOffset;
            // Grow the far plane with altitude so the coast under the framed horizon is never clipped.
            cam.farClipPlane = SiteHorizon.ClipFor((float)flight.Altitude);
            // Sea level is the anchored site root. Do not slide it: the coast has to stay under the pad.
            float fov = juice != null ? juice.Fov : cam.fieldOfView;
            cam.fieldOfView = fov;
            Vector3 lead = juice != null ? juice.Lead : Vector3.zero;
            cam.transform.position = PadGround.ClampAboveGround(v + framed + lead, ground.position.y);
            var cp = cam.transform.position;
            cam.transform.rotation = ChaseCamera.Aim(cp, v + up * rocketHeight * 0.5f, fov, (float)flight.Altitude, cp.y - ground.position.y);
            if (juice != null)
                cam.transform.position = PadGround.ClampAboveGround(
                    cam.transform.position + cam.transform.right * juice.Shake.x + cam.transform.up * juice.Shake.y,
                    ground.position.y);
            flight.Plume.SetHeightAboveDeck((float)flight.Altitude);
            staging?.Present();
            hT.text = $"T+ {flight.SimTime:F1}";
            hStage.text = $"S{flight.Sim.StageIdx + 1}";
            hAlt.text = $"ALT {flight.Altitude / 1000:F1} km";
            hVel.text = $"VEL {flight.Speed:N0} m/s";
            hMach.text = $"MACH {flight.Mach:F2}";
            hQ.text = $"Q {flight.DynPressure / 1000:F1} kPa";
            if (juice != null)
            {
                hQ.EnableInClassList("q-hot", juice.Call == "MAX-Q");
                hQ.EnableInClassList("q-crit", juice.Call == "AIRFRAME" || juice.Band == "critical" || juice.Band == "lost");
            }
            hThr.text = $"THR {flight.ThrustFraction * 100:F0}%";
            hDr.text = $"DR {flight.Downrange / 1000:F1} km";
            RefreshWebcast(flight.SimTime, true);
            ascentHud?.Present(flight);
            if (flight.Lost) { hMsg.text = "Vehicle lost"; again.RemoveFromClassList("hidden"); }
            else if (!flight.Running && !flight.Meco && flight.Sim.Done) { hMsg.text = "Flight ended before MECO"; again.RemoveFromClassList("hidden"); }
        }

        void TickCount()
        {
            if (sequence == null) { counting = false; BeginAscent(); return; }
            sequence.Step(UnityEngine.Time.deltaTime);
            var pad = tower != null ? tower.Find("Pad") : null;
            var renderer = pad != null ? pad.GetComponent<Kestrel.Pad.PadRenderer>() : null;
            if (renderer != null) renderer.AdvanceTo(TimelineClock.RealSeconds(sequence.Time, sequence.Mode));
            if (hT != null) hT.text = sequence.Clock;
            if (hStage != null) hStage.text = "COUNT";
            RefreshWebcast(sequence.Time, false);
            if (sequence.Liftoff) { counting = false; BeginAscent(); }
        }

        void RefreshWebcast(double time, bool flying)
        {
            if (hVerdict == null) return;
            double alt = flying && flight != null ? flight.Altitude : 0;
            double spd = flying && flight != null ? flight.Speed : 0;
            double q = flying && flight != null ? flight.DynPressure : 0;
            var pulse = flying && flight != null && flight.Lost
                ? WebcastBoard.Lost(flight.Result.failure.kind, alt)
                : WebcastBoard.Pulse(flying, alt, spd, requiredMps, spareMps, q, maxQKpa, orbitLabel);
            hVerdict.text = pulse.Verdict;
            hPulse.text = pulse.Detail;
            var beats = sequence != null ? sequence.Beats : null;
            var chips = WebcastBoard.Window(beats, time);
            var sig = new System.Text.StringBuilder();
            foreach (var c in chips) sig.Append(c.Id).Append((int)c.State).Append(c.Id == pinnedEvent ? "*" : "");
            if (sig.ToString() != chipSig && seqRow != null)
            {
                chipSig = sig.ToString();
                seqRow.Clear();
                foreach (var c in chips)
                {
                    var chip = c;
                    var b = new Button { text = chip.Label + "  " + chip.Clock };
                    b.AddToClassList("webcast-chip");
                    if (chip.State == ChipState.Now) b.AddToClassList("webcast-now");
                    else if (chip.State == ChipState.Past) b.AddToClassList("webcast-past");
                    if (chip.Id == pinnedEvent) b.AddToClassList("webcast-pin");
                    b.clicked += () => PinEvent(chip.Id);
                    seqRow.Add(b);
                }
            }
            if (hCallout == null) return;
            LaunchBeat pinned = null;
            if (beats != null && !string.IsNullOrEmpty(pinnedEvent))
                for (int i = 0; i < beats.Count; i++)
                    if (beats[i] != null && beats[i].Id == pinnedEvent) { pinned = beats[i]; break; }
            hCallout.text = pinned != null ? WebcastBoard.Callout(pinned, pulse, alt) : "Tap an event.";
            if (pinned == null && juice != null && !string.IsNullOrEmpty(juice.Line)) hCallout.text = juice.Line;
        }

        void PinEvent(string id)
        {
            pinnedEvent = id;
            chipSig = null;
            double t = counting && sequence != null ? sequence.Time : flight != null ? flight.SimTime : 0;
            RefreshWebcast(t, flight != null && flight.Sim != null);
        }

        async void OnMeco(FlightDriver f)
        {
            if (submitted) return; submitted = true;
            hMsg.text = $"MECO  ·  {f.Altitude / 1000:F1} km  ·  {f.Speed:N0} m/s";
            again.RemoveFromClassList("hidden");
            bool ok = await GameCenterBridge.Impl.SubmitScore(GameCenterBridge.MecoAltitudeBoard, (long)System.Math.Round(f.Altitude));
            hMsg.text += ok ? "\nScore sent to Game Center" : "";
        }

        sealed class BirdTrack : BirdFlockView.FlightDriverSource
        {
            readonly FlightDriver flight;
            public BirdTrack(FlightDriver flight) { this.flight = flight; }
            public double Downrange => flight != null ? flight.Downrange : 0;
            public double Altitude => flight != null ? flight.Altitude : 0;
            public double OriginX => flight != null ? flight.OriginX : 0;
            public double OriginY => flight != null ? flight.OriginY : 0;
        }
    }
}

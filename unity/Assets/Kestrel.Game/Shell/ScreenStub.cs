// Home is the 3D hangar menu. Flight stays a placeholder until U3/U4 land. BuildConsole is the
// flat console the EditMode tests still construct; the live Home path does not show it.
using System;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Game.MissionSelect;
using Kestrel.Game.Layout;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public sealed class ScreenStub : MonoBehaviour
    {
        static ScreenStub()
        {
            // Player builds copy data/board-starters.json into Resources. There is no repo root.
            HomeAutoBuild.BoardStarterPack = () =>
            {
                var asset = Resources.Load<TextAsset>("KestrelData/board-starters");
                return asset != null ? asset.text : null;
            };
        }

        public AppScreen Kind;
        VisualElement _view;

        void OnEnable()
        {
            var shell = AppShell.Instance;
            if (shell == null) return;
            if (Kind == AppScreen.Home)
            {
                _view = new VisualElement();
                _view.AddToClassList("kh-screen");
                BuildHome(shell);
                return;
            }
            if (Kind == AppScreen.Missions)
            {
                _view = MissionBoardScreen.Build(
                    LoadCampaign(),
                    id => shell.Flow.LaunchChosen(id),
                    id => shell.Flow.DesignChosen(id),
                    () => shell.Flow.OpenSandbox(),
                    () => shell.Flow.ExitToHome(),
                    MainMenuLayout.ReducedMotion(),
                    CanLaunch);
                FillHost(_view);
                shell.Safe.Add(_view);
                if (shell.Fill != null) shell.Fill.SpaceT = 0.35f;
                return;
            }
            if (Kind == AppScreen.Flight)
            {
                _view = BuildFlightRoutes(() => shell.Flow.OpenReport(), () => shell.Flow.ExitToMissions(), () => shell.Flow.ExitToHome());
                shell.Safe.Add(_view);
                if (shell.Fill != null) shell.Fill.SpaceT = 0f;
                return;
            }
            _view = new VisualElement();
            _view.AddToClassList("kh-screen");
            var title = new Label(Kind.ToString().ToUpperInvariant() + " (placeholder)");
            title.AddToClassList("kh-title");
            _view.Add(title);
            var back = new Button(() => shell.Flow.BackFromDesigner()) { text = "Back", tooltip = "Back to the briefing. The design stays." };
            back.AddToClassList("kh-button");
            _view.Add(back);
            shell.Safe.Add(_view);
            if (shell.Fill != null) shell.Fill.SpaceT = 1f;
        }

        MainMenuController _menu;
        VisualElement _overlay;
        VisualElement _audio;

        /// <summary>Home is the 3D hangar (MainMenuController): KH-1 on the deck, telemetry and glass
        /// panels at different depths, launch bar closest. Words are glyph meshes on those panels.
        /// The UI Toolkit view does not pick; it only holds hidden spoken labels. BuildConsole remains
        /// for the EditMode hit-target tests.</summary>
        void BuildHome(AppShell shell)
        {
            _view.AddToClassList("kh-home");
            _view.pickingMode = PickingMode.Ignore;
            _view.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0f));
            FillHost(_view);
            shell.Safe.Add(_view);
            if (shell.Fill != null) shell.Fill.SpaceT = 0.85f;
            // #224: music/sound/narration toggles live on Home in both the overlay and the 3D fallback.
            var audio = AudioBar.Build();
            if (!MainMenuLayout.UseOverlay) _view.Add(audio);
            _menu = gameObject.AddComponent<MainMenuController>();
            _menu.OverlayMode = MainMenuLayout.UseOverlay;
            _menu.Attach(shell);
            if (MainMenuLayout.UseOverlay)
            {
                // Mix overlay: LAUNCH flies the next mission. Design first opens the designer with that load.
                _view.pickingMode = PickingMode.Ignore;
                Action launch = () => shell.Flow.HomePressed("launch");
                Action design = () => shell.Flow.HomePressed("design");
                Action missions = () => shell.Flow.HomePressed("missions");
                _overlay = MainMenuOverlay.Build(shell.Root, _view, launch, design, missions, MainMenuLayout.ReducedMotion());
                try { MainMenuOverlay.SetMissionLine(_overlay, NextMissionLine()); }
                catch (Exception e) { Debug.LogWarning("[Kestrel] next mission line skipped: " + e.Message); }
                // Music sits top-left. The gear sits clear of the web EXIT. Both follow the safe inset.
                shell.Root.Add(audio);
                void Place()
                {
                    var gear = shell.Root.Q("home-settings");
                    bool web = Application.platform == RuntimePlatform.WebGLPlayer;
                    float width = shell.Root.resolvedStyle.width;
                    float top = shell.Safe.resolvedStyle.paddingTop;
                    if (!web)
                    {
                        HomeChrome.Place(gear, audio, width, top, false);
                        return;
                    }
                    // The page FULL/EXIT is 88×44 CSS pixels. Panel units are not CSS pixels, so the
                    // reserved corner is computed in CSS space and scaled onto the root.
                    float height = shell.Root.resolvedStyle.height;
                    WebViewport.Css(out float cssW, out float cssH);
                    if (width < 8f || height < 8f || cssW < 8f || cssH < 8f)
                    {
                        HomeChrome.Place(gear, audio, width, top, true);
                        return;
                    }
                    float perX = Screen.width > 1f ? Screen.width / cssW : 1f;
                    float perY = Screen.height > 1f ? Screen.height / cssH : 1f;
                    var safe = Screen.safeArea;
                    var frame = WebHomeLayout.Place(cssW, cssH,
                        (Screen.height - safe.yMax) / perY, safe.yMin / perY, safe.xMin / perX, (Screen.width - safe.xMax) / perX);
                    float ux = width / cssW;
                    float uy = height / cssH;
                    if (gear != null)
                    {
                        gear.style.position = Position.Absolute;
                        gear.style.left = frame.Gear.X * ux;
                        gear.style.top = frame.Gear.Y * uy;
                        gear.style.width = frame.Gear.W * ux;
                        gear.style.height = frame.Gear.H * uy;
                        gear.style.right = StyleKeyword.Auto;
                    }
                    AudioBar.Place(audio, frame.Audio.Y * uy, frame.Audio.X * ux);
                    FitAudioRow(audio, frame.Audio.W * ux, frame.Audio.H * uy);
                }
                audio.RegisterCallback<GeometryChangedEvent>(_ => Place());
                shell.Root.RegisterCallback<GeometryChangedEvent>(_ => Place());
                _audio = audio;
                return;
            }
            // Fallback (UseOverlay = false): the original 3D controls with hidden spoken labels.
            AddSpoken("home-launch", MainMenuLayout.LaunchLabel, MainMenuLayout.Find("launch").Spoken);
            foreach (var id in new[] { "design", "missions", "codex" })
            {
                var a = MainMenuLayout.Find(id);
                AddSpoken("home-" + id, a.Label, a.Spoken);
            }
            AddSpoken("kh1-readout", MainMenuLayout.Designation, MainMenuLayout.Find("kh1").Spoken);
        }

        static void FitAudioRow(VisualElement bar, float width, float height)
        {
            if (bar == null || !(width > 1f) || !(height > 1f)) return;
            bar.style.width = width;
            bar.style.height = height;
            float s = height / HomeCornerLayout.AudioH;
            foreach (var child in bar.Children())
            {
                child.style.width = 64f * s;
                child.style.maxWidth = 64f * s;
                child.style.minWidth = AudioBar.TouchPt * s;
                child.style.height = AudioBar.TouchPt * s;
                child.style.minHeight = AudioBar.TouchPt * s;
                child.style.fontSize = 13f * s;
            }
        }

        void AddSpoken(string name, string label, string spoken)
        {
            var line = new Label { name = name, text = label, tooltip = spoken };
            line.pickingMode = PickingMode.Ignore;
            line.style.display = DisplayStyle.None;
            _view.Add(line);
        }

        /// <summary>Console panel. Public + static so EditMode tests can build it without an AppShell.</summary>
        public static VisualElement BuildConsole(System.Action launch, System.Action hangar)
        {
            var console = new VisualElement { name = "home-console" };
            console.AddToClassList("kh-home-console");
            var top = new VisualElement();
            top.AddToClassList("kh-home-console-row");
            var patch = new MissionPatch { name = "kh1-patch", tooltip = "KH-1 mission patch." };
            patch.AddToClassList("kh-home-patch");
            top.Add(patch);
            var launchKey = new Button(launch) { name = "home-launch", text = "LAUNCH", tooltip = "Launch. Opens the rocket designer on the pad." };
            launchKey.AddToClassList("kh-button");
            launchKey.AddToClassList("kh-home-launch");
            top.Add(launchKey);
            console.Add(top);
            var grid = new VisualElement();
            grid.AddToClassList("kh-home-console-grid");
            foreach (var (id, label, live, a11y) in HomeArt.ConsoleButtons)
            {
                var b = new Button(live && id == "hangar" ? hangar : null) { name = "home-" + id, text = label, tooltip = a11y };
                b.AddToClassList("kh-button");
                b.AddToClassList("kh-home-console-button");
                var lamp = new VisualElement { name = "lamp", pickingMode = PickingMode.Ignore };
                lamp.AddToClassList("kh-home-lamp");
                lamp.AddToClassList(live ? "kh-home-lamp--on" : "kh-home-lamp--off");
                b.Insert(0, lamp);
                b.SetEnabled(live);   // Missions/Codex/Settings: no screen yet, disabled stubs.
                grid.Add(b);
            }
            console.Add(grid);
            return console;
        }

        static VisualElement BuildConsole(AppShell shell) =>
            BuildConsole(() => shell.Flow.HomePressed("launch"), () => shell.Flow.HomePressed("hangar"));

        static VisualElement shellFullLayer() => AppShell.Instance?.Root?.Q("mm-full");

        /// <summary>Repo root when data/campaign.json is on disk. Null in a player build that only has Resources.</summary>
        public static string RepoRoot() => Catalog.RepoRootOrNull();

        /// <summary>The screen fills the safe column. Absolute menu children do not give the host a height on their own.</summary>
        public static void FillHost(VisualElement view)
        {
            if (view == null) return;
            view.style.flexGrow = 1;
            view.style.flexShrink = 1;
            view.style.width = Length.Percent(100);
            view.style.height = Length.Percent(100);
            view.style.position = Position.Relative;
        }

        /// <summary>data/campaign.json, then the synced Resources copy the player build carries.</summary>
        public static Campaign LoadCampaign()
        {
            string root = RepoRoot();
            if (root != null && File.Exists(Path.Combine(root, "data", "campaign.json")))
                return Campaign.Load(root);
            var asset = Resources.Load<TextAsset>("KestrelData/campaign");
            if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
                return Campaign.FromJson(MiniJson.ParseObject(asset.text));
            return null;
        }

        /// <summary>Catalog for the home auto-build. Repo first, then the synced Resources copy.</summary>
        public static Catalog LoadCatalog()
        {
            string root = RepoRoot();
            if (root != null && File.Exists(Path.Combine(root, "data", "propellants.json")))
            {
                try { return Catalog.LoadFromRepo(root); }
                catch (Exception e) { Debug.LogWarning("[Kestrel] catalog unavailable: " + e.Message); }
            }
            try
            {
                string T(string n)
                {
                    var asset = Resources.Load<TextAsset>("KestrelData/" + n);
                    return asset != null ? asset.text : null;
                }
                string limits = T("designer-limits");
                if (!string.IsNullOrEmpty(limits))
                    DesignerLimits.Active = DesignerLimits.Parse(MiniJson.ParseObject(limits));
                string props = T("propellants"), eng = T("engines"), mat = T("materials"), pay = T("payloads"), pre = T("presets"), econ = T("constants");
                if (props == null || eng == null || mat == null || pay == null || pre == null || econ == null) return null;
                return Catalog.FromJson(props, eng, mat, pay, pre, econ);
            }
            catch (Exception e) { Debug.LogWarning("[Kestrel] catalog unavailable: " + e.Message); return null; }
        }

        public static HangarData LoadHangar()
        {
            string root = RepoRoot();
            if (root != null && File.Exists(Path.Combine(root, "data", "designer-templates.json")))
            {
                try { return HangarData.LoadFromRepo(root); }
                catch (Exception e) { Debug.LogWarning("[Kestrel] templates unavailable: " + e.Message); }
            }
            try
            {
                string T(string n)
                {
                    var asset = Resources.Load<TextAsset>("KestrelData/" + n);
                    return asset != null ? asset.text : null;
                }
                return HangarData.FromJson(T("designer-templates"), T("designer-look"), T("designer-budget"), T("finishes"), T("liveries"));
            }
            catch (Exception e) { Debug.LogWarning("[Kestrel] templates unavailable: " + e.Message); return null; }
        }

        /// <summary>True when a starter for this mission can leave the pad. Called on selection, not every frame.</summary>
        public static bool CanLaunch(string missionId)
        {
            try
            {
                var campaign = LoadCampaign();
                var mission = campaign != null ? campaign.ById(missionId) : null;
                if (mission == null) return false;
                var choice = HomeAutoBuild.Choose(LoadCatalog(), LoadHangar()?.templates, mission);
                return choice != null && choice.LaunchEnabled && choice.Design != null;
            }
            catch (Exception) { return false; }
        }

        /// <summary>Highest mission passed, from the save folder. A missing folder is a fresh career.</summary>
        public static int ClearedCount()
        {
            var campaign = LoadCampaign();
            if (campaign == null) return 0;
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "saves");
                var snap = MissionProgress.ReadSave(campaign, dir, MissionMapCopy.MinorPerKcFallback);
                return snap.Career == null || snap.Career.Cleared < 0 ? 0 : snap.Career.Cleared;
            }
            catch (Exception) { return 0; }
        }

        public static string NextMissionLine()
        {
            try
            {
                var mission = HomeRoutes.NextMission(LoadCampaign(), ClearedCount());
                if (mission == null || string.IsNullOrEmpty(mission.Name)) return null;
                return MissionContext.LabelFor(mission.Index) + " \u00B7 " + mission.Name;
            }
            catch (Exception) { return null; }
        }

        /// <summary>Flight scene stub. The live ascent stays in Design; these are the same leave routes.</summary>
        public static VisualElement BuildFlightRoutes(Action report, Action missions, Action home)
        {
            var root = new VisualElement { name = "flight-routes" };
            root.AddToClassList("kh-screen");
            var title = new Label("Flight") { name = "flight-title" };
            title.AddToClassList("kh-title");
            root.Add(title);
            root.Add(RouteButton("flight-report", "Flight report", "Flight report. What the flight did.", report));
            root.Add(RouteButton("flight-missions", "Missions", "Exit to Missions.", missions));
            root.Add(RouteButton("flight-home", "Home", "Exit to Home.", home));
            return root;
        }

        public static VisualElement BuildRouteCard(string overlay, MissionContext ctx, SceneFlow flow)
        {
            if (overlay == SessionScreens.Report)
                return BuildReport(ctx, () => flow?.ReportNext(), () => flow?.ReportRetry(), () => flow?.ExitToMissions(), () => flow?.ExitToHome());
            if (overlay == SessionScreens.Briefing)
                return BuildBriefing(ctx, () => flow?.BuildFromBriefing(), () => flow?.BackFromBriefing());
            return null;
        }

        public static VisualElement BuildBriefing(MissionContext ctx, Action build, Action back)
            => BriefingScreen.Build(ctx, build, back);

        public static VisualElement BuildReport(MissionContext ctx, Action next, Action retry, Action missions, Action home)
        {
            var card = Card("report-card", "Flight report");
            var body = card.Q("card-body");
            body.Add(Body("report-job", JobLine(ctx)));
            var outcome = FlightDebrief.Latest;
            if (outcome != null)
            {
                body.Add(Body("report-cause", outcome.CauseTitle));
                body.Add(Body("report-numbers", outcome.Numbers));
                body.Add(Body("report-stars", outcome.StarsLine));
                body.Add(Body("report-medals", outcome.MedalsLine));
                body.Add(Body("report-kc", outcome.KcLine));
                if (!string.IsNullOrEmpty(outcome.NarratorKid)) body.Add(Body("report-narrator", outcome.NarratorKid));
            }
            card.Add(RouteButton("report-next", "Next mission", "Next mission. Opens the list.", next));
            card.Add(RouteButton("report-retry", "One more launch", "One more launch. Same mission, same seed.", retry));
            card.Add(RouteButton("report-missions", "Missions", "Back to the mission list.", missions));
            card.Add(RouteButton("report-home", "Home", "Exit to Home.", home));
            return card;
        }

        static VisualElement Card(string name, string title)
        {
            var card = new VisualElement { name = name };
            card.AddToClassList("kh-screen");
            card.style.position = Position.Absolute;
            card.style.left = 8;
            card.style.right = 8;
            card.style.top = 8;
            card.style.bottom = 8;
            card.style.backgroundColor = new Color(0.106f, 0.122f, 0.165f, 0.96f);
            card.style.justifyContent = Justify.FlexStart;
            var heading = new Label(title) { name = name + "-title" };
            heading.AddToClassList("kh-title");
            heading.style.flexShrink = 0;
            card.Add(heading);
            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "card-body" };
            scroll.style.flexGrow = 1;
            card.Add(scroll);
            return card;
        }

        static Label Body(string name, string text)
        {
            var line = new Label(text) { name = name };
            line.AddToClassList("kh-caption");
            line.style.whiteSpace = WhiteSpace.Normal;
            return line;
        }

        static string JobLine(MissionContext ctx)
        {
            if (ctx == null || ctx.IsFree) return "Free build.";
            string line = ctx.ListText();
            if (ctx.Mass > 0) line += ". " + ctx.Mass.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " kg";
            if (ctx.Gentle) line += ". Gentle on.";
            else line += ". Gentle off.";
            return line;
        }

        static Button RouteButton(string name, string text, string tip, Action click)
        {
            var b = new Button(click) { name = name, text = text, tooltip = tip };
            b.AddToClassList("kh-button");
            b.style.minHeight = 44;
            b.style.flexShrink = 0;
            return b;
        }

        void OnDisable()
        {
            var root = AppShell.Instance != null ? AppShell.Instance.Root : null;
            _view?.RemoveFromHierarchy();
            _overlay?.RemoveFromHierarchy();
            _audio?.RemoveFromHierarchy();
            shellFullLayer()?.RemoveFromHierarchy();
            root?.Q("home-settings")?.RemoveFromHierarchy();
            root?.Q("home-settings-sheet")?.RemoveFromHierarchy();
            if (_menu != null) { Destroy(_menu); _menu = null; }
        }
    }

    /// <summary>Shrinks the Home wordmark so the wider line stays inside the safe column. The 52px
    /// size is the maximum (SE points, match-height panel); narrower columns scale it down. Tracking
    /// stays the 4px of the 52px size, scaled with the font, so it is not a fixed overflow.</summary>
    public static class HomeTitleFit
    {
        public const float MaxFont = 52f;
        public const float TrackingAtMax = 4f;
        const string WideLine = "KESTREL";

        public static void Bind(Label wide, Label other)
        {
            float cached = -1f;
            float applied = -1f;
            bool busy = false;
            void Fit(GeometryChangedEvent _)
            {
                if (busy) return;
                // 2px keeps the 1px text outline inside the column.
                float avail = wide.contentRect.width - 2f;
                if (avail <= 2f) return;
                if (cached <= 0f)
                {
                    // MeasureTextSize reads computed style. Wait until the stylesheet's 52px is applied,
                    // and do it before this callback overwrites the font below.
                    if (wide.resolvedStyle.fontSize < MaxFont - 1f) return;
                    float measured = wide.MeasureTextSize(WideLine, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
                    // Letter-spacing is not always included in the measure. Budget the gaps either way.
                    float width = Mathf.Max(measured, measured + TrackingAtMax * (WideLine.Length - 1));
                    if (width <= 0f) return;
                    cached = width;
                }
                // Floor at 1px so a narrow column always keeps the line inside, rather than clipping at a readability floor.
                float size = (float)PortraitLayout.FitFontSize(avail, MaxFont, 1f, cached);
                if (Mathf.Abs(applied - size) <= 0.5f) return;
                busy = true;
                applied = size;
                float tracking = TrackingAtMax * size / MaxFont;
                wide.style.fontSize = size;
                wide.style.letterSpacing = tracking;
                other.style.fontSize = size;
                other.style.letterSpacing = tracking;
                busy = false;
            }
            wide.RegisterCallback<GeometryChangedEvent>(Fit);
        }
    }
}

// Home is the 3D hangar menu. Flight stays a placeholder until U3/U4 land. BuildConsole is the
// flat console the EditMode tests still construct; the live Home path does not show it.
using System;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Game.MissionSelect;
using Kestrel.Game.Layout;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public sealed class ScreenStub : MonoBehaviour
    {
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
                _view = MissionSelectScreen.Build(LoadCampaign(), id => shell.Flow.StartMission(id), () => shell.Flow.ExitToHome());
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
                // Mix overlay: 2D title card, LAUNCH bar and tiles are the controls (UI Toolkit picks them and
                // supplies VoiceOver labels via tooltip). LAUNCH opens the mission list. HANGAR is a free build.
                _view.pickingMode = PickingMode.Ignore;
                Action launch = () => shell.Flow.HomePressed("launch");
                Action hangar = () => shell.Flow.HomePressed("hangar");
                _overlay = MainMenuOverlay.Build(shell.Root, _view, launch, hangar, MainMenuLayout.ReducedMotion());
                // The reel-bar layer (mm-full) is added to the shell root above the safe area and would hide
                // the #224 mix toggles, so they go on the root, last (topmost), and follow the safe inset.
                shell.Root.Add(audio);
                audio.RegisterCallback<GeometryChangedEvent>(_ => audio.style.top = shell.Safe.resolvedStyle.paddingTop);
                _audio = audio;
                return;
            }
            // Fallback (UseOverlay = false): the original 3D controls with hidden spoken labels.
            AddSpoken("home-launch", MainMenuLayout.LaunchLabel, MainMenuLayout.Find("launch").Spoken);
            foreach (var id in new[] { "hangar", "missions", "codex", "settings" })
            {
                var a = MainMenuLayout.Find(id);
                AddSpoken("home-" + id, a.Label, a.Spoken);
            }
            AddSpoken("kh1-readout", MainMenuLayout.Designation, MainMenuLayout.Find("kh1").Spoken);
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
        public static string RepoRoot()
        {
            try { return Catalog.FindRepoRoot(Directory.GetCurrentDirectory()); }
            catch (Exception) { return null; }
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
                return BuildBriefing(ctx, () => flow?.BuildFromBriefing(), () => flow?.ExitToMissions());
            return null;
        }

        public static VisualElement BuildBriefing(MissionContext ctx, Action build, Action back)
        {
            var card = Card("briefing-card", "Briefing");
            var body = card.Q("card-body");
            body.Add(Body("briefing-job", JobLine(ctx)));
            if (ctx != null && !string.IsNullOrEmpty(ctx.Goal)) body.Add(Body("briefing-goal", ctx.Goal));
            if (ctx != null && ctx.BudgetKc > 0)
                body.Add(Body("briefing-budget", "Budget " + Kc(ctx.BudgetKc) + " Kc. Reward " + Kc(ctx.RewardKc) + " Kc."));
            if (ctx != null && (!string.IsNullOrEmpty(ctx.Weather) || !string.IsNullOrEmpty(ctx.Site) || !string.IsNullOrEmpty(ctx.TimeOfDay)))
                body.Add(Body("briefing-pad", PadLine(ctx)));
            if (ctx != null && ctx.BonusGoals.Count > 0)
                body.Add(Body("briefing-bonus", "Bonus: " + string.Join(". ", ctx.BonusGoals) + "."));
            if (ctx != null && !string.IsNullOrEmpty(ctx.EngineerLine)) body.Add(Body("briefing-engineer", ctx.EngineerLine));
            card.Add(RouteButton("briefing-build", "Build it", "Build it. Opens the designer with this mission.", build));
            card.Add(RouteButton("briefing-back", "Back", "Back to the mission list.", back));
            return card;
        }

        public static VisualElement BuildReport(MissionContext ctx, Action next, Action retry, Action missions, Action home)
        {
            var card = Card("report-card", "Flight report");
            card.Q("card-body").Add(Body("report-job", JobLine(ctx)));
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

        static string PadLine(MissionContext ctx)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(ctx.Weather)) parts.Add("Wind " + ctx.Weather);
            if (!string.IsNullOrEmpty(ctx.TimeOfDay)) parts.Add(ctx.TimeOfDay);
            if (!string.IsNullOrEmpty(ctx.Site)) parts.Add(ctx.Site);
            return string.Join(". ", parts) + ".";
        }

        static string Kc(double value) => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture);

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
            _view?.RemoveFromHierarchy();
            _overlay?.RemoveFromHierarchy();
            _audio?.RemoveFromHierarchy();
            shellFullLayer()?.RemoveFromHierarchy();
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

// Home is the 3D hangar menu. Flight stays a placeholder until U3/U4 land. BuildConsole is the
// flat console the EditMode tests still construct; the live Home path does not show it.
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
            _view = new VisualElement();
            _view.AddToClassList("kh-screen");
            if (Kind == AppScreen.Home) { BuildHome(shell); return; }
            var title = new Label(Kind.ToString().ToUpperInvariant() + " (placeholder)");
            title.AddToClassList("kh-title");
            _view.Add(title);
            // Not on the play path: Home > Play loads Slice.unity until the real Design/Flight screens land.
            AddButton(shell, "Home", AppScreen.Home, "Back to the title screen.");
            shell.Safe.Add(_view);
            if (shell.Fill != null) shell.Fill.SpaceT = Kind == AppScreen.Flight ? 0f : 1f;
        }

        MainMenuController _menu;

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
            // Spoken strings stay on UI Toolkit labels (the project's VoiceOver stand-in) but do not
            // pick, so the raycasts hit the 3D panels instead of a flat overlay.
            AddSpoken("home-launch", MainMenuLayout.LaunchLabel, MainMenuLayout.Find("launch").Spoken);
            foreach (var id in new[] { "hangar", "missions", "codex", "settings" })
            {
                var a = MainMenuLayout.Find(id);
                AddSpoken("home-" + id, a.Label, a.Spoken);
            }
            AddSpoken("kh1-readout", MainMenuLayout.Designation, MainMenuLayout.Find("kh1").Spoken);
            _menu = gameObject.AddComponent<MainMenuController>();
            _menu.Attach(shell);
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
            BuildConsole(() => shell.Flow.Go(AppScreen.Design), () => shell.Flow.Go(AppScreen.Design));

        void AddButton(AppShell shell, string text, AppScreen target, string a11y)
        {
            var b = new Button(() => shell.Flow.Go(target)) { text = text, tooltip = a11y };
            b.AddToClassList("kh-button");
            _view.Add(b);
        }

        void OnDisable()
        {
            _view?.RemoveFromHierarchy();
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

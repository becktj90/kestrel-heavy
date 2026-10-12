// Home title screen (retro mix-v2), plus placeholder Flight screens (unused on the play path) until U3/U4 land. Shows the screen
// name inside the safe area plus nav buttons (≥44 pt), so the shell and scaling can be verified.
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

        HomeBackdrop _backdrop;

        /// <summary>Retro title screen (mix-v2): poster sky + striped sun + cream blueprint stack (HomeBackdrop),
        /// cream KESTREL / orange HEAVY lockup, and a console panel: KH-1 patch, red LAUNCH key (= the old PLAY
        /// action) and Hangar/Missions/Codex/Settings with status lamps. Static, so Reduce Motion safe.
        /// Labels double as the accessible names (tooltip carries the spoken hint, as elsewhere in the shell).</summary>
        void BuildHome(AppShell shell)
        {
            _backdrop = new HomeBackdrop();
            shell.Root.Insert(0, _backdrop);              // behind the safe-area root, edge to edge
            _view.AddToClassList("kh-home");
            var kestrel = new Label("KESTREL");
            kestrel.AddToClassList("kh-home-title");
            var heavy = new Label("HEAVY");
            heavy.AddToClassList("kh-home-title");
            heavy.AddToClassList("kh-home-title-rest");
            _view.Add(kestrel);
            _view.Add(heavy);
            HomeTitleFit.Bind(kestrel, heavy);
            var sub = new Label("DESIGN \u00B7 BUILD \u00B7 LAUNCH");
            sub.AddToClassList("kh-home-subtitle");
            _view.Add(sub);
            var bd = _backdrop;
            sub.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float top = sub.worldBound.yMax - bd.worldBound.yMin + 16f;
                if (!Mathf.Approximately(top, bd.ArtTopY)) { bd.ArtTopY = top; bd.MarkDirtyRepaint(); }
            });
            var spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.style.flexGrow = 1;
            _view.Add(spacer);
            var console = BuildConsole(shell);
            _view.Add(console);
            console.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float top = console.worldBound.yMin - bd.worldBound.yMin;
                if (!Mathf.Approximately(top, bd.ConsoleTopY)) { bd.ConsoleTopY = top; bd.MarkDirtyRepaint(); }
            });
            shell.Safe.Add(_view);
            if (shell.Fill != null) shell.Fill.SpaceT = 0.6f;
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

        void OnDisable() { _view?.RemoveFromHierarchy(); _backdrop?.RemoveFromHierarchy(); }
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

// Home title screen, plus placeholder Flight screens (unused on the play path) until U3/U4 land. Shows the screen
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

        /// <summary>Title screen: full-bleed dusk backdrop with the stack on the pad, big title, one-line
        /// subtitle, one big Play button. Static (Reduce Motion safe), no other prompts.</summary>
        void BuildHome(AppShell shell)
        {
            _backdrop = new HomeBackdrop();
            shell.Root.Insert(0, _backdrop);              // behind the safe-area root, edge to edge
            _view.AddToClassList("kh-home");
            // Two lines so white-space:nowrap cannot collapse the lockup into one overflowing line.
            // Both share a font size fitted to the wider word (KESTREL) inside the safe column.
            var kestrel = new Label("KESTREL");
            kestrel.AddToClassList("kh-home-title");
            var heavy = new Label("HEAVY");
            heavy.AddToClassList("kh-home-title");
            heavy.AddToClassList("kh-home-title-rest");
            _view.Add(kestrel);
            _view.Add(heavy);
            HomeTitleFit.Bind(kestrel, heavy);
            var sub = new Label("Design a rocket. Launch it.");
            sub.AddToClassList("kh-home-subtitle");
            _view.Add(sub);
            // Keep the stack silhouette below the title block on every device (#116-v2: title overlapped the nose).
            var bd = _backdrop;
            sub.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float top = sub.worldBound.yMax - bd.worldBound.yMin + 16f;
                if (!Mathf.Approximately(top, bd.ArtTopY)) { bd.ArtTopY = top; bd.MarkDirtyRepaint(); }
            });
            var spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.style.flexGrow = 1;
            _view.Add(spacer);
            var play = new Button(() => shell.Flow.Go(AppScreen.Design)) { text = "PLAY", tooltip = "Play. Opens the rocket designer on the pad." };
            play.AddToClassList("kh-button");
            play.AddToClassList("kh-home-play");
            _view.Add(play);
            shell.Safe.Add(_view);
            if (shell.Fill != null) shell.Fill.SpaceT = 0.6f;
        }

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

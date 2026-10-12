// Home title screen, plus placeholder Flight screens (unused on the play path) until U3/U4 land. Shows the screen
// name inside the safe area plus nav buttons (≥44 pt), so the shell and scaling can be verified.
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
            var title = new Label("KESTREL\nHEAVY");
            title.AddToClassList("kh-home-title");
            _view.Add(title);
            var sub = new Label("Design a rocket. Launch it.");
            sub.AddToClassList("kh-home-subtitle");
            _view.Add(sub);
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
}

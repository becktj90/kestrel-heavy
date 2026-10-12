// Placeholder content for generated Home/Design/Flight scenes until U3/U4 land. Shows the screen
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
            var title = new Label(Kind == AppScreen.Home ? "KESTREL HEAVY" : Kind.ToString().ToUpperInvariant());
            title.AddToClassList("kh-title");
            _view.Add(title);
            // Menu per screen: Home → Play (Design); Design → Launch (Flight) / Home; Flight → Design / Home.
            if (Kind == AppScreen.Home)
            {
                var sub = new Label("Design a rocket. Launch it.");
                sub.AddToClassList("kh-subtitle");
                _view.Add(sub);
                AddButton(shell, "Play", AppScreen.Design, "Play. Opens the rocket designer.");
            }
            else if (Kind == AppScreen.Design)
            {
                AddButton(shell, "Launch", AppScreen.Flight, "Launch the rocket.");
                AddButton(shell, "Home", AppScreen.Home, "Back to the title screen.");
            }
            else
            {
                AddButton(shell, "Back to Design", AppScreen.Design, "Back to the rocket designer.");
                AddButton(shell, "Home", AppScreen.Home, "Back to the title screen.");
            }
            shell.Safe.Add(_view);
            if (shell.Fill != null) shell.Fill.SpaceT = Kind == AppScreen.Flight ? 0f : 1f;
        }

        void AddButton(AppShell shell, string text, AppScreen target, string a11y)
        {
            var b = new Button(() => shell.Flow.Go(target)) { text = text, tooltip = a11y };
            b.AddToClassList("kh-button");
            _view.Add(b);
        }

        void OnDisable() => _view?.RemoveFromHierarchy();
    }
}

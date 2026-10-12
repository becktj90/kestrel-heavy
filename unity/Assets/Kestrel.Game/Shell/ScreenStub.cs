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
            foreach (AppScreen s in System.Enum.GetValues(typeof(AppScreen)))
            {
                if (s == Kind) continue;
                var target = s;
                var b = new Button(() => shell.Flow.Go(target)) { text = target.ToString() };
                b.AddToClassList("kh-button");
                _view.Add(b);
            }
            shell.Safe.Add(_view);
            if (shell.Fill != null) shell.Fill.SpaceT = Kind == AppScreen.Flight ? 0f : 1f;
        }

        void OnDisable() => _view?.RemoveFromHierarchy();
    }
}

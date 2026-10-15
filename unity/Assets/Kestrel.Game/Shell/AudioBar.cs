using Kestrel.Game.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    /// <summary>Home mix toggles. The 3D menu plates stay as they are; this row is the reachable control.</summary>
    public static class AudioBar
    {
        public const float TouchPt = 44f;

        public static VisualElement Build()
        {
            var row = new VisualElement { name = "audio-bar" };
            row.style.position = Position.Absolute;
            row.style.top = 0;
            row.style.right = 0;
            row.style.flexDirection = FlexDirection.Row;
            row.Add(Toggle("audio-music", "Music", () => AudioPrefs.MusicMuted, AudioPrefs.SetMusicMuted));
            row.Add(Toggle("audio-sfx", "Sound", () => AudioPrefs.SfxMuted, AudioPrefs.SetSfxMuted));
            row.Add(Toggle("audio-narration", "Voice", () => AudioPrefs.NarrationMuted, AudioPrefs.SetNarrationMuted));
            return row;
        }

        /// <summary>True when the pointer is on the mix row, so the 3D menu does not also fire.</summary>
        public static bool Blocks(Vector2 screen)
        {
            var root = AppShell.Instance != null ? AppShell.Instance.Root : null;
            if (root == null || root.panel == null) return false;
            var hit = root.panel.Pick(RuntimePanelUtils.ScreenToPanel(root.panel, screen));
            for (var ve = hit; ve != null; ve = ve.parent)
                if (ve.name == "audio-bar") return true;
            return false;
        }

        static Button Toggle(string name, string label, System.Func<bool> get, System.Action<bool> set)
        {
            var b = new Button { name = name };
            b.style.minWidth = TouchPt;
            b.style.minHeight = TouchPt;
            b.style.height = TouchPt;
            b.style.marginLeft = 4;
            b.style.marginTop = 4;
            b.style.paddingLeft = 8;
            b.style.paddingRight = 8;
            b.style.fontSize = 13;
            b.style.color = Color.white;
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            b.style.backgroundColor = new Color(0.05f, 0.08f, 0.14f, 0.86f);
            b.style.borderTopWidth = 0;
            b.style.borderRightWidth = 0;
            b.style.borderBottomWidth = 0;
            b.style.borderLeftWidth = 0;
            void Refresh()
            {
                bool off = get();
                b.text = AudioPrefs.Caption(label, off);
                b.tooltip = label + (off ? " is muted." : " is on.") + " Tap to toggle.";
            }
            b.clicked += () => { set(!get()); Refresh(); };
            Refresh();
            return b;
        }
    }

    /// <summary>Which screens bring their own <see cref="AudioListener"/>. Home and the Flight placeholder use the shell.</summary>
    public static class ShellAudio
    {
        public static bool ShellListenerOn(AppScreen screen) => screen != AppScreen.Design;
    }
}

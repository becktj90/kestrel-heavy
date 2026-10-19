// Codex reader. The home tile stays "Coming soon"; this screen is what a menu walk opens.
// An empty checkout, or a player build with no Resources copy, says so instead of throwing.
using System;
using System.IO;
using Kestrel.Core.Codex;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public static class CodexScreen
    {
        public const string RootName = "codex-screen";
        public const string EmptyLine = "The codex is not on this device.";

        public static VisualElement Build(Action back)
        {
            var root = new VisualElement { name = RootName };
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.right = 0;
            root.style.top = 0;
            root.style.bottom = 0;
            root.style.flexGrow = 1;
            root.style.flexDirection = FlexDirection.Column;
            root.style.backgroundColor = new Color(0.118f, 0.180f, 0.282f, 1f);
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;

            var title = new Label("Codex") { name = "codex-title", pickingMode = PickingMode.Ignore };
            title.AddToClassList("kh-title");
            title.style.fontSize = 18;
            title.style.flexShrink = 0;
            title.style.whiteSpace = WhiteSpace.Normal;
            Face(title, "Fraunces-Bold");
            root.Add(title);

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "codex-scroll" };
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.style.flexGrow = 1;
            var book = TryLoad();
            if (book == null || book.Entries == null || book.Entries.Count == 0)
            {
                var empty = new Label(EmptyLine) { name = "codex-empty", pickingMode = PickingMode.Ignore };
                empty.style.fontSize = 14;
                empty.style.whiteSpace = WhiteSpace.Normal;
                Face(empty, "SpaceGrotesk-Medium");
                scroll.Add(empty);
            }
            else
            {
                for (int i = 0; i < book.Entries.Count; i++)
                {
                    var entry = book.Entries[i];
                    var head = new Label(entry.Title)
                    {
                        name = "codex-entry-" + entry.Id,
                        pickingMode = PickingMode.Ignore,
                    };
                    head.style.fontSize = 16;
                    head.style.whiteSpace = WhiteSpace.Normal;
                    head.style.marginTop = 8;
                    Face(head, "Fraunces-Bold");
                    var body = new Label(entry.Kid) { pickingMode = PickingMode.Ignore };
                    body.style.fontSize = 14;
                    body.style.whiteSpace = WhiteSpace.Normal;
                    Face(body, "SpaceGrotesk-Medium");
                    scroll.Add(head);
                    scroll.Add(body);
                }
            }
            root.Add(scroll);

            var leave = new Button(back) { name = "codex-back", text = "Back", tooltip = "Back to Home." };
            leave.AddToClassList("kh-button");
            leave.style.minHeight = 44;
            leave.style.fontSize = 14;
            leave.style.flexShrink = 0;
            leave.style.whiteSpace = WhiteSpace.Normal;
            leave.userData = back;
            Face(leave, "SpaceGrotesk-Bold");
            root.Add(leave);
            root.RegisterCallback<GeometryChangedEvent>(_ => TextFit.Apply(root));
            return root;
        }

        static CodexBook TryLoad()
        {
            try
            {
                string root = ScreenStub.RepoRoot();
                if (!string.IsNullOrEmpty(root) && File.Exists(Path.Combine(root, "data", "codex.json")))
                    return CodexBook.Load(root);
                var asset = Resources.Load<TextAsset>("KestrelData/codex");
                if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
                    return CodexBook.Parse(asset.text);
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        static void Face(VisualElement el, string fontName)
        {
            if (el == null) return;
            var font = Resources.Load<Font>(MainMenuFonts.Dir + fontName);
            if (font == null) return;
            el.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
        }
    }
}

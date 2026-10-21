// Landscape web designer: parts on the left of the rocket, readouts and Launch on the right.
// Portrait (and iOS) leave the sheet on the USS path.
using Kestrel.Core.Ui;
using Kestrel.Core.Flight;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public static class WebDesignChrome
    {
        const string HostName = "web-readouts";
        static readonly string[] ReadoutNames = { "sticky-strip", "risk-chips", "launch-row", "launch-reason", "designer-launch" };

        struct Slot { public VisualElement Element, Parent; public int Index; }
        static Slot[] home;
        static bool applied;

        public static void Apply(VisualElement ui, WebDesignerFrame frame)
        {
            var design = ui != null ? ui.Q("design") : null;
            if (design == null) return;
            if (frame == null || !frame.Wide)
            {
                Clear(ui);
                return;
            }
            var host = EnsureHost(ui);
            Remember(ui);
            foreach (var name in ReadoutNames)
            {
                var el = ui.Q(name);
                if (el != null && el.parent != host) host.Add(el);
            }
            Pin(design, frame.Parts);
            Pin(host, frame.RightPanel);
            design.style.position = Position.Absolute;
            host.style.flexDirection = FlexDirection.Column;
            applied = true;
            // The host is a sibling of #design, so hiding the sheet does not hide it.
            // Keep it off the flight, the countdown, and the result card.
            if (FlightUp(ui))
            {
                Cover(ui);
                return;
            }
            host.pickingMode = PickingMode.Position;
            host.style.display = DisplayStyle.Flex;
        }

        /// <summary>Hide the landscape readout column without a sticky flag. Safe to call when the host was never built.</summary>
        public static void Cover(VisualElement ui)
        {
            var host = ui != null ? ui.Q(HostName) : null;
            if (host == null) return;
            host.style.display = DisplayStyle.None;
            host.pickingMode = PickingMode.Ignore;
        }

        static bool FlightUp(VisualElement ui)
        {
            if (ui == null) return false;
            var design = ui.Q("design");
            if (design != null && design.ClassListContains("hidden")) return true;
            var hud = ui.Q(UiIds.ScreenFlight);
            if (hud != null && !hud.ClassListContains("hidden")) return true;
            return ui.Q(ReportView.RootName) != null;
        }

        public static void Clear(VisualElement ui)
        {
            if (!applied && home == null) return;
            if (home != null)
            {
                for (int i = home.Length - 1; i >= 0; i--)
                {
                    var slot = home[i];
                    if (slot.Element == null || slot.Parent == null) continue;
                    if (slot.Element.parent == slot.Parent) continue;
                    int at = slot.Index;
                    if (at < 0 || at > slot.Parent.childCount) slot.Parent.Add(slot.Element);
                    else slot.Parent.Insert(at, slot.Element);
                }
            }
            home = null;
            var design = ui != null ? ui.Q("design") : null;
            Unpin(design);
            var host = ui != null ? ui.Q(HostName) : null;
            if (host != null) host.style.display = DisplayStyle.None;
            applied = false;
        }

        static void Remember(VisualElement ui)
        {
            if (home != null) return;
            home = new Slot[ReadoutNames.Length];
            for (int i = 0; i < ReadoutNames.Length; i++)
            {
                var el = ui.Q(ReadoutNames[i]);
                home[i] = new Slot
                {
                    Element = el,
                    Parent = el != null ? el.parent : null,
                    Index = el != null && el.parent != null ? el.parent.IndexOf(el) : -1,
                };
            }
        }

        static VisualElement EnsureHost(VisualElement ui)
        {
            var host = ui.Q(HostName);
            if (host != null) return host;
            host = new VisualElement { name = HostName, pickingMode = PickingMode.Position };
            host.style.backgroundColor = new Color(0.03f, 0.05f, 0.08f, 0.9f);
            host.style.borderTopLeftRadius = 18;
            host.style.borderBottomLeftRadius = 18;
            host.style.paddingLeft = 12;
            host.style.paddingRight = 12;
            host.style.paddingTop = 12;
            host.style.paddingBottom = 12;
            ui.Add(host);
            return host;
        }

        static void Pin(VisualElement e, HudBox b)
        {
            if (e == null || !b.Live) return;
            e.style.position = Position.Absolute;
            e.style.left = b.X;
            e.style.top = b.Y;
            e.style.width = b.W;
            e.style.height = b.H;
            e.style.right = StyleKeyword.Auto;
            e.style.bottom = StyleKeyword.Auto;
        }

        static void Unpin(VisualElement e)
        {
            if (e == null) return;
            e.style.position = StyleKeyword.Null;
            e.style.left = StyleKeyword.Null;
            e.style.top = StyleKeyword.Null;
            e.style.width = StyleKeyword.Null;
            e.style.height = StyleKeyword.Null;
            e.style.right = StyleKeyword.Null;
            e.style.bottom = StyleKeyword.Null;
        }
    }
}

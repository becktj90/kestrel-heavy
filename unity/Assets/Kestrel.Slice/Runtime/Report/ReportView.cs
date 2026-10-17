// Why-it-failed card (§3): icon, kid line, engineer line, Fix it, then the five plots.
// Child order is the VoiceOver order. Tooltips are the project's VoiceOver stand-in.
using System;
using Kestrel.Core;
using Kestrel.Core.Flight;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public static class ReportView
    {
        public const string RootName = "post-flight";
        public static readonly Color PanelInk = new Color(0.055f, 0.07f, 0.11f, 1f);

        static readonly string[] ChannelLabels = { "Altitude", "Speed", "Dynamic pressure", "g load", "Delta-v losses" };

        /// <summary>Solid sheet behind every line, in front of the designer stats.</summary>
        public static void Seal(VisualElement root)
        {
            if (root == null) return;
            root.style.backgroundColor = PanelInk;
            root.style.opacity = 1f;
            var plate = root.Q("report-plate");
            if (plate == null)
            {
                plate = new VisualElement { name = "report-plate", pickingMode = PickingMode.Ignore };
                plate.style.position = Position.Absolute;
                plate.style.left = 0;
                plate.style.right = 0;
                plate.style.top = 0;
                plate.style.bottom = 0;
                root.Insert(0, plate);
            }
            plate.style.backgroundColor = PanelInk;
            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (root.parent != null) root.BringToFront();
            });
        }

        public static VisualElement Build(FailureEvent failure, TelemetryLog log, ReportFrame frame, bool reduceMotion, Action onFix, FlightDebrief debrief = null, ReportActions actions = null)
        {
            if (debrief != null)
                return RewardsCard.Build(failure, log, frame, reduceMotion, onFix, debrief, actions);
            var link = FixField.For(failure != null ? failure.kind : "");
            var root = new VisualElement { name = RootName, pickingMode = PickingMode.Position };
            root.AddToClassList("post-flight");
            if (reduceMotion) root.AddToClassList("reduce-motion");
            root.tooltip = "Why it failed.";
            Place(root, frame.Card);
            Seal(root);
            root.style.borderTopLeftRadius = 16;
            root.style.borderTopRightRadius = 16;
            root.style.borderBottomLeftRadius = 16;
            root.style.borderBottomRightRadius = 16;

            string mode = failure != null ? failure.kind : "break";
            var icon = Icon(mode);
            Place(icon, frame.Icon);
            icon.tooltip = "Failure. " + (failure != null ? FlightDebrief.TitleOf(failure.kind) : "unknown") + ".";
            root.Add(icon);

            var kid = new Label(PlayerText.ForPlayer(failure != null ? failure.kid : "")) { name = "failure-kid" };
            Place(kid, frame.Kid);
            kid.style.color = Color.white;
            kid.style.whiteSpace = WhiteSpace.Normal;
            kid.style.fontSize = 16;
            kid.tooltip = kid.text;
            root.Add(kid);

            var engineer = new Label(PlayerText.ForPlayer(failure != null ? failure.engineer : "")) { name = "failure-engineer" };
            Place(engineer, frame.Engineer);
            engineer.style.color = new Color(0.75f, 0.82f, 0.9f);
            engineer.style.whiteSpace = WhiteSpace.Normal;
            engineer.style.fontSize = 13;
            engineer.tooltip = engineer.text;
            root.Add(engineer);

            var plots = Plots(log, frame.Plots);
            root.Add(plots);

            var fix = new Button(() => onFix?.Invoke()) { name = "failure-fix", text = "Fix it" };
            Place(fix, frame.Fix);
            fix.style.minHeight = (float)frame.Fix.H;
            fix.style.minWidth = (float)ReportLayout.MinTargetPt;
            fix.style.backgroundColor = new Color(0.18f, 0.45f, 0.86f);
            fix.style.color = Color.white;
            fix.style.fontSize = 17;
            fix.style.unityFontStyleAndWeight = FontStyle.Bold;
            fix.tooltip = link.VoiceOver;
            fix.userData = link.Uri;
            root.Add(fix);
            return root;
        }

        public static VisualElement Icon(string kind)
        {
            var icon = new VisualElement { name = "failure-icon" };
            icon.AddToClassList("failure-icon");
            icon.AddToClassList("icon-" + (string.IsNullOrEmpty(kind) ? "break" : kind));
            // Two or more shapes, so the mark is not a colour alone.
            Color ink = Ink(kind);
            icon.style.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            icon.style.borderTopLeftRadius = 8;
            icon.style.borderTopRightRadius = 8;
            icon.style.borderBottomLeftRadius = 8;
            icon.style.borderBottomRightRadius = 8;
            int shapes = Shapes(kind);
            for (int i = 0; i < shapes; i++)
            {
                var bar = new VisualElement { name = "shape-" + i };
                bar.style.position = Position.Absolute;
                bar.style.backgroundColor = ink;
                bar.style.width = 8;
                bar.style.height = i == shapes - 1 ? 18 : 10;
                bar.style.left = 6 + i * 9;
                bar.style.top = 6 + (i % 2) * 14;
                bar.style.width = i % 2 == 0 ? 14 : 6;
                icon.Add(bar);
            }
            return icon;
        }

        public static int Shapes(string kind)
        {
            switch (kind)
            {
                case "tumble":
                case "separation-collision": return 3;
                case "bad-mixture":
                case "engine-out": return 3;
                case "thermal":
                case "cryo-material": return 4;
                default: return 2;
            }
        }

        static Color Ink(string kind)
        {
            switch (kind)
            {
                case "bad-mixture": return new Color(0.45f, 0.85f, 0.35f);
                case "cryo-material": return new Color(0.7f, 0.88f, 1f);
                case "prop-depletion": return new Color(0.85f, 0.75f, 0.4f);
                case "engine-overpressure":
                case "engine-out": return new Color(1f, 0.85f, 0.45f);
                default: return new Color(1f, 0.55f, 0.28f);
            }
        }

        public static VisualElement Plots(TelemetryLog log, ReportRect rect, bool flow = false)
        {
            var wrap = new VisualElement { name = "failure-plots" };
            if (flow)
            {
                wrap.style.position = Position.Relative;
                wrap.style.height = 96;
                wrap.style.marginLeft = 12;
                wrap.style.marginRight = 12;
                wrap.style.marginBottom = 8;
            }
            else Place(wrap, rect);
            wrap.style.flexDirection = FlexDirection.Row;
            var built = ReportPlot.Build(log);
            for (int i = 0; i < built.Length; i++)
            {
                var plot = new VisualElement { name = "plot-" + built[i].Channel };
                plot.style.flexGrow = 1;
                plot.style.marginRight = 4;
                plot.style.backgroundColor = new Color(0.14f, 0.17f, 0.22f);
                plot.style.overflow = Overflow.Hidden;
                string label = i < ChannelLabels.Length ? ChannelLabels[i] : built[i].Channel;
                var caption = new Label(label) { name = "plot-label" };
                caption.style.fontSize = 9;
                caption.style.color = new Color(0.8f, 0.86f, 0.92f);
                caption.style.position = Position.Absolute;
                caption.style.left = 2;
                caption.style.top = 0;
                plot.Add(caption);
                int bars = Math.Min(24, built[i].X.Length);
                for (int b = 0; b < bars; b++)
                {
                    int src = built[i].X.Length <= 24 ? b : b * (built[i].X.Length - 1) / (bars - 1);
                    var bar = new VisualElement { name = "bar" };
                    bar.style.position = Position.Absolute;
                    bar.style.bottom = 2;
                    bar.style.width = 2;
                    bar.style.left = Length.Percent(built[i].X[src] * 100f);
                    bar.style.height = Length.Percent(Mathf.Clamp(built[i].Y[src], 0.05f, 1f) * 70f);
                    bar.style.backgroundColor = new Color(0.65f, 0.82f, 1f);
                    plot.Add(bar);
                }
                for (int m = 0; m < built[i].MarkerX.Length; m++)
                {
                    var mark = new VisualElement { name = "marker" };
                    mark.style.position = Position.Absolute;
                    mark.style.bottom = 0;
                    mark.style.top = 12;
                    mark.style.width = 2;
                    mark.style.left = Length.Percent(built[i].MarkerX[m] * 100f);
                    mark.style.backgroundColor = new Color(1f, 0.85f, 0.4f, 0.85f);
                    plot.Add(mark);
                }
                if (built[i].FailureX >= 0f)
                {
                    var fail = new VisualElement { name = "failure-marker" };
                    fail.style.position = Position.Absolute;
                    fail.style.bottom = 0;
                    fail.style.top = 10;
                    fail.style.width = 4;
                    fail.style.left = Length.Percent(built[i].FailureX * 100f);
                    fail.style.backgroundColor = new Color(1f, 0.35f, 0.25f);
                    plot.Add(fail);
                    plot.tooltip = label + ". Failure marked.";
                }
                else plot.tooltip = label + ".";
                wrap.Add(plot);
            }
            wrap.tooltip = "Altitude, speed, dynamic pressure, g load and delta-v losses.";
            return wrap;
        }

        public static void Place(VisualElement el, ReportRect r)
        {
            el.style.position = Position.Absolute;
            el.style.left = (float)r.X;
            el.style.top = (float)r.Y;
            el.style.width = (float)r.W;
            el.style.height = (float)r.H;
        }
    }
}

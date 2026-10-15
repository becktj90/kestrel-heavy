// Portrait ascent HUD (mockups 9 and 10). One thumb, bottom action zone, safe-area padded
// because it lives inside the flight HUD. Pause and the camera rig are not touched.
using System;
using System.Globalization;
using Kestrel.Core.Flight;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class AscentHud
    {
        const int CellCount = 4;

        VisualElement hud, root, actions, nudgeRow;
        Label caption;
        AscentPlot plot;
        Label code, quote, detail, bonus;
        Label[] cells = new Label[CellCount];
        Button primary, alternate, skip, assist, nudgeDown, nudgeUp;
        VisualElement marker, green;
        AscentCall shown;
        FlightDriver flight;
        float upp = 1f;

        public void Open(VisualElement flightHud, float unitsPerPoint)
        {
            Close();
            hud = flightHud;
            upp = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            if (hud == null) return;
            root = new VisualElement { name = "ascent-hud", pickingMode = PickingMode.Ignore };
            root.style.flexShrink = 0;
            BuildPlot();
            BuildActions();
            var band = hud.Q("hud-band");
            if (band != null) hud.Insert(hud.IndexOf(band) + 1, root);
            else hud.Insert(0, root);
            var pause = hud.Q("pause-menu");
            if (pause != null) hud.Insert(hud.IndexOf(pause), actions);
            else hud.Add(actions);
            pause?.BringToFront();
            Layout(upp);
        }

        public void Close()
        {
            if (root != null && root.parent != null) root.RemoveFromHierarchy();
            if (actions != null && actions.parent != null) actions.RemoveFromHierarchy();
            root = null;
            actions = null;
            hud = null;
        }

        public void Layout(float unitsPerPoint)
        {
            upp = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            if (root == null) return;
            float plotH = AscentLayout.Px(168f, upp);
            root.style.marginTop = AscentLayout.Px(8f, upp);
            root.style.height = plotH;
            if (plot != null)
            {
                plot.style.height = plotH;
                plot.style.borderTopLeftRadius = AscentLayout.Px(16f, upp);
                plot.style.borderTopRightRadius = AscentLayout.Px(16f, upp);
                plot.style.borderBottomLeftRadius = AscentLayout.Px(16f, upp);
                plot.style.borderBottomRightRadius = AscentLayout.Px(16f, upp);
            }
            if (code != null) code.style.fontSize = AscentLayout.Px(12f, upp);
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null) continue;
                cells[i].style.minHeight = AscentLayout.Px(44f, upp);
                cells[i].style.fontSize = AscentLayout.Px(13f, upp);
                cells[i].style.borderTopLeftRadius = AscentLayout.Px(10f, upp);
                cells[i].style.borderTopRightRadius = AscentLayout.Px(10f, upp);
                cells[i].style.borderBottomLeftRadius = AscentLayout.Px(10f, upp);
                cells[i].style.borderBottomRightRadius = AscentLayout.Px(10f, upp);
                cells[i].style.marginRight = AscentLayout.Px(6f, upp);
            }
            if (actions == null) return;
            actions.style.minHeight = AscentLayout.Px(AscentLayout.ActionZonePt, upp);
            actions.style.paddingLeft = AscentLayout.Px(4f, upp);
            actions.style.paddingRight = AscentLayout.Px(4f, upp);
            actions.style.paddingTop = AscentLayout.Px(8f, upp);
            actions.style.paddingBottom = AscentLayout.Px(4f, upp);
            actions.style.borderTopLeftRadius = AscentLayout.Px(18f, upp);
            actions.style.borderTopRightRadius = AscentLayout.Px(18f, upp);
            StyleButton(primary, true);
            StyleButton(alternate, false);
            StyleButton(skip, false);
            StyleButton(assist, false);
            StyleButton(nudgeDown, false);
            StyleButton(nudgeUp, false);
            if (quote != null) quote.style.fontSize = AscentLayout.Px(16f, upp);
            if (detail != null) detail.style.fontSize = AscentLayout.Px(12f, upp);
            if (caption != null) caption.style.fontSize = AscentLayout.Px(12f, upp);
            if (bonus != null) bonus.style.fontSize = AscentLayout.Px(12f, upp);
        }

        public void Present(FlightDriver driver)
        {
            flight = driver;
            if (root == null || flight == null) return;
            var director = flight.Decisions;
            if (director == null)
            {
                root.style.display = DisplayStyle.None;
                if (actions != null) actions.style.display = DisplayStyle.None;
                return;
            }
            root.style.display = DisplayStyle.Flex;
            if (actions != null) actions.style.display = DisplayStyle.Flex;
            var inv = CultureInfo.InvariantCulture;
            if (code != null)
                code.text = Clock(flight.SimTime) + "    " + director.FlightCode + "  ·  AUTO-ASSIST " + (director.AutoAssist ? "ON" : "OFF");
            double alt = director.LiveAlt > 0 ? director.LiveAlt : flight.Altitude;
            double speed = director.LiveSpeed > 0 ? director.LiveSpeed : flight.Speed;
            double q = director.LiveQ > 0 ? director.LiveQ : flight.DynPressure;
            double g = director.LiveG;
            var prompts = director.OpenPrompts();
            bool anomaly = prompts.Length > 0 && prompts[0].Call == AscentCall.EngineOut;
            SetCell(0, "ALT", (alt / 1000.0).ToString("0.0", inv) + " km", new Color(0.85f, 0.92f, 0.96f));
            SetCell(1, "SPEED", speed >= 1000 ? (speed / 1000.0).ToString("0.00", inv) + " km/s" : speed.ToString("0", inv) + " m/s", new Color(0.85f, 0.92f, 0.96f));
            if (anomaly)
            {
                SetCell(2, "TWR", director.LiveTwr.ToString("0.0", inv), new Color(0.94f, 0.63f, 0.12f));
                SetCell(3, "PROP", (director.LiveProp * 100.0).ToString("0", inv) + "%", new Color(0.30f, 0.82f, 0.55f));
            }
            else
            {
                SetCell(2, "Q", (q / 1000.0).ToString("0", inv) + " kPa", new Color(0.94f, 0.63f, 0.12f));
                SetCell(3, "G", g.ToString("0.0", inv), new Color(0.30f, 0.82f, 0.55f));
            }
            if (plot != null)
            {
                plot.Track = director.Track;
                plot.TargetAlt = director.Vehicle != null ? director.Vehicle.TargetAltM : 200000;
                plot.MarkDirtyRepaint();
            }
            var score = director.Score();
            if (prompts.Length == 0)
            {
                shown = default;
                if (quote != null) quote.text = score.Decided > 0 ? score.Summary : "Flying the plan.";
                if (detail != null) detail.text = director.Engineer ? "Engineer nudge offsets the planned pitch. The amber path is that offset." : "Auto-assist flies the timed calls. Tap one for a gold medal.";
                if (primary != null) primary.style.display = DisplayStyle.None;
                if (alternate != null) alternate.style.display = DisplayStyle.None;
                if (skip != null) skip.style.display = DisplayStyle.None;
            }
            else
            {
                var p = prompts[0];
                shown = p.Call;
                if (quote != null) quote.text = p.Quote;
                if (detail != null) detail.text = p.Eyebrow + "  ·  " + p.Detail;
                if (primary != null)
                {
                    primary.style.display = DisplayStyle.Flex;
                    primary.text = p.Primary;
                    primary.tooltip = p.Quote + " " + p.Primary;
                    primary.style.backgroundColor = p.Call == AscentCall.EngineOut || p.Call == AscentCall.FuelBalance
                        ? new Color(0.94f, 0.63f, 0.08f)
                        : new Color(0.23f, 0.83f, 0.96f);
                }
                if (alternate != null)
                {
                    alternate.style.display = p.ShowAlternate ? DisplayStyle.Flex : DisplayStyle.None;
                    alternate.text = p.Alternate ?? "";
                    alternate.tooltip = p.Alternate ?? "";
                }
                if (skip != null) skip.style.display = DisplayStyle.Flex;
                if (green != null)
                {
                    green.style.left = Length.Percent((float)(p.GreenStart * 100));
                    green.style.width = Length.Percent(Mathf.Max(4f, (float)(p.GreenEnd - p.GreenStart) * 100f));
                }
                if (marker != null) marker.style.left = Length.Percent(Mathf.Clamp01((float)p.Marker) * 100f);
            }
            if (assist != null)
            {
                assist.text = director.AutoAssist ? "Auto-assist on" : "Auto-assist";
                assist.tooltip = director.AutoAssist
                    ? "Auto-assist is on. The planned calls fly themselves. Tap to take over."
                    : "Auto-assist is off. Tap to let the planned calls fly themselves.";
            }
            if (nudgeRow != null) nudgeRow.style.display = director.Engineer ? DisplayStyle.Flex : DisplayStyle.None;
            if (nudgeDown != null) nudgeDown.text = "Nudge −1°";
            if (nudgeUp != null) nudgeUp.text = "Nudge +1°";
            if (caption != null)
                caption.text = "Perfect window = green. Auto-assist does this for you in Simple.";
            if (bonus != null) bonus.text = BonusLine(score);
        }

        static string Clock(double seconds)
        {
            int s = (int)Math.Floor(seconds);
            if (s < 0) s = 0;
            int m = s / 60;
            int r = s % 60;
            return "T+" + m.ToString("00", CultureInfo.InvariantCulture) + ":" + r.ToString("00", CultureInfo.InvariantCulture);
        }

        static string BonusLine(AscentFlightScore score)
        {
            if (score == null) return "";
            string bits = "";
            if (score.AltitudeHit) bits += "Altitude target  ·  ";
            if (score.SpeedHit) bits += "Speed target  ·  ";
            if (score.FuelHit) bits += "Fuel-efficient ascent  ·  ";
            if (score.CleanStaging) bits += "Clean staging  ·  ";
            if (score.PeakGHit) bits += "Gentle ride  ·  ";
            if (score.PayloadDamaged) bits += "Payload heat is past the limit  ·  ";
            if (bits.Length == 0) return score.Decided > 0 ? "Medal timing: " + score.Summary : "";
            return bits + score.Summary;
        }

        void SetCell(int i, string name, string value, Color color)
        {
            if (cells[i] == null) return;
            cells[i].text = name + "\n" + value;
            cells[i].style.color = color;
        }

        void BuildPlot()
        {
            plot = new AscentPlot { name = "ascent-plot", pickingMode = PickingMode.Ignore };
            plot.style.backgroundColor = new Color(0.03f, 0.06f, 0.11f, 0.92f);
            plot.style.paddingLeft = 8;
            plot.style.paddingRight = 8;
            plot.style.paddingTop = 8;
            plot.style.paddingBottom = 8;
            plot.style.flexGrow = 1;
            code = new Label { name = "ascent-code", pickingMode = PickingMode.Ignore };
            code.style.color = new Color(0.75f, 0.82f, 0.9f);
            code.style.unityFontStyleAndWeight = FontStyle.Bold;
            plot.Add(code);
            var row = new VisualElement { name = "ascent-cells", pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 6;
            for (int i = 0; i < CellCount; i++)
            {
                var cell = new Label { name = "ascent-cell-" + i, pickingMode = PickingMode.Ignore };
                cell.style.flexGrow = 1;
                cell.style.backgroundColor = new Color(0.07f, 0.1f, 0.16f, 0.95f);
                cell.style.unityTextAlign = TextAnchor.MiddleCenter;
                cell.style.whiteSpace = WhiteSpace.Normal;
                cell.style.paddingTop = 4;
                cell.style.paddingBottom = 4;
                cells[i] = cell;
                row.Add(cell);
            }
            plot.Add(row);
            var legend = new Label { name = "ascent-legend", text = "PLAN (cyan) vs NUDGED (amber)  ·  MAX-Q ZONE", pickingMode = PickingMode.Ignore };
            legend.style.color = new Color(0.65f, 0.75f, 0.82f);
            legend.style.marginTop = 4;
            legend.style.fontSize = 11;
            plot.Add(legend);
            root.Add(plot);
        }

        void BuildActions()
        {
            actions = new VisualElement { name = "ascent-actions" };
            actions.style.position = Position.Absolute;
            actions.style.left = 0;
            actions.style.right = 0;
            actions.style.bottom = 0;
            actions.style.backgroundColor = new Color(0.03f, 0.05f, 0.09f, 0.94f);
            quote = new Label { name = "ascent-quote", pickingMode = PickingMode.Ignore };
            quote.style.color = Color.white;
            quote.style.whiteSpace = WhiteSpace.Normal;
            quote.style.unityFontStyleAndWeight = FontStyle.Bold;
            detail = new Label { name = "ascent-detail", pickingMode = PickingMode.Ignore };
            detail.style.color = new Color(0.7f, 0.78f, 0.86f);
            detail.style.whiteSpace = WhiteSpace.Normal;
            detail.style.marginTop = 4;
            var bar = new VisualElement { name = "ascent-timer", pickingMode = PickingMode.Ignore };
            bar.style.height = 10;
            bar.style.marginTop = 8;
            bar.style.backgroundColor = new Color(0.15f, 0.2f, 0.28f);
            bar.style.borderTopLeftRadius = 5;
            bar.style.borderTopRightRadius = 5;
            bar.style.borderBottomLeftRadius = 5;
            bar.style.borderBottomRightRadius = 5;
            green = new VisualElement { name = "ascent-green", pickingMode = PickingMode.Ignore };
            green.style.position = Position.Absolute;
            green.style.top = 0;
            green.style.bottom = 0;
            green.style.backgroundColor = new Color(0.2f, 0.85f, 0.55f);
            marker = new VisualElement { name = "ascent-marker", pickingMode = PickingMode.Ignore };
            marker.style.position = Position.Absolute;
            marker.style.top = -3;
            marker.style.bottom = -3;
            marker.style.width = 3;
            marker.style.backgroundColor = Color.white;
            bar.Add(green);
            bar.Add(marker);
            caption = new Label { name = "ascent-caption", pickingMode = PickingMode.Ignore };
            caption.style.color = new Color(0.65f, 0.75f, 0.82f);
            caption.style.whiteSpace = WhiteSpace.Normal;
            caption.style.marginTop = 4;
            primary = Action("ascent-primary", true, () => Press(true));
            alternate = Action("ascent-alternate", false, () => Press(false));
            alternate.style.backgroundColor = new Color(0.08f, 0.1f, 0.14f, 0.2f);
            alternate.style.color = new Color(0.95f, 0.35f, 0.42f);
            alternate.style.borderTopWidth = 2;
            alternate.style.borderRightWidth = 2;
            alternate.style.borderBottomWidth = 2;
            alternate.style.borderLeftWidth = 2;
            alternate.style.borderTopColor = new Color(0.89f, 0.23f, 0.29f);
            alternate.style.borderRightColor = new Color(0.89f, 0.23f, 0.29f);
            alternate.style.borderBottomColor = new Color(0.89f, 0.23f, 0.29f);
            alternate.style.borderLeftColor = new Color(0.89f, 0.23f, 0.29f);
            var row = new VisualElement { name = "ascent-skip-row" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 6;
            skip = Action("ascent-skip", false, OnSkip);
            skip.text = "Skip";
            skip.tooltip = "Skip. The autopilot takes the planned action. This is not a fail.";
            skip.style.flexGrow = 1;
            skip.style.marginRight = 6;
            assist = Action("ascent-assist", false, OnAssist);
            assist.text = "Auto-assist";
            assist.style.flexGrow = 1;
            row.Add(skip);
            row.Add(assist);
            nudgeRow = new VisualElement { name = "ascent-nudge" };
            nudgeRow.style.flexDirection = FlexDirection.Row;
            nudgeRow.style.marginTop = 6;
            nudgeDown = Action("ascent-nudge-down", false, () => Nudge(-AscentDecisions.NudgeStepDeg));
            nudgeUp = Action("ascent-nudge-up", false, () => Nudge(AscentDecisions.NudgeStepDeg));
            nudgeDown.style.flexGrow = 1;
            nudgeUp.style.flexGrow = 1;
            nudgeDown.style.marginRight = 6;
            nudgeDown.tooltip = "Pitch the planned kick down one degree.";
            nudgeUp.tooltip = "Pitch the planned kick up one degree.";
            nudgeRow.Add(nudgeDown);
            nudgeRow.Add(nudgeUp);
            bonus = new Label { name = "ascent-bonus", pickingMode = PickingMode.Ignore };
            bonus.style.color = new Color(0.65f, 0.78f, 0.72f);
            bonus.style.whiteSpace = WhiteSpace.Normal;
            bonus.style.marginTop = 4;
            actions.Add(detail);
            actions.Add(quote);
            actions.Add(bar);
            actions.Add(caption);
            actions.Add(primary);
            actions.Add(alternate);
            actions.Add(row);
            actions.Add(nudgeRow);
            actions.Add(bonus);
        }

        void Press(bool accept)
        {
            if (shown == default || flight == null || flight.Decisions == null) return;
            flight.Decisions.Choose(shown, accept);
            Present(flight);
        }

        void OnSkip()
        {
            if (shown == default || flight == null || flight.Decisions == null) return;
            flight.Decisions.Skip(shown);
            Present(flight);
        }

        void OnAssist()
        {
            if (flight == null || flight.Decisions == null) return;
            flight.Decisions.AutoAssist = !flight.Decisions.AutoAssist;
            Present(flight);
        }

        void Nudge(double delta)
        {
            if (flight == null || flight.Decisions == null) return;
            flight.Decisions.NudgePitch(delta);
            Present(flight);
        }

        void StyleButton(Button b, bool fill)
        {
            if (b == null) return;
            float h = AscentLayout.Px(AscentLayout.ButtonMinPt, upp);
            b.style.minHeight = h;
            b.style.height = h;
            b.style.fontSize = AscentLayout.Px(16f, upp);
            b.style.marginTop = AscentLayout.Px(6f, upp);
            float r = AscentLayout.Px(12f, upp);
            b.style.borderTopLeftRadius = r;
            b.style.borderTopRightRadius = r;
            b.style.borderBottomLeftRadius = r;
            b.style.borderBottomRightRadius = r;
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            if (fill)
            {
                b.style.color = new Color(0.03f, 0.08f, 0.12f);
                b.style.backgroundColor = new Color(0.23f, 0.83f, 0.96f);
            }
        }

        static Button Action(string name, bool fill, Action click)
        {
            var b = new Button(click) { name = name };
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            b.style.borderTopWidth = 0;
            b.style.borderRightWidth = 0;
            b.style.borderBottomWidth = 0;
            b.style.borderLeftWidth = 0;
            if (!fill)
            {
                b.style.color = Color.white;
                b.style.backgroundColor = new Color(0.1f, 0.14f, 0.2f, 0.95f);
            }
            return b;
        }

        sealed class AscentPlot : VisualElement
        {
            public AscentTrack Track;
            public double TargetAlt = 200000;

            public AscentPlot()
            {
                generateVisualContent += OnPaint;
            }

            void OnPaint(MeshGenerationContext ctx)
            {
                var box = contentRect;
                if (box.width < 8 || box.height < 8) return;
                float left = 10f, right = box.width - 8f, top = 78f, bottom = box.height - 10f;
                if (bottom - top < 20f) { top = 8f; bottom = box.height - 8f; }
                var paint = ctx.painter2D;
                paint.strokeColor = new Color(0.2f, 0.32f, 0.45f, 0.8f);
                paint.lineWidth = 1f;
                paint.BeginPath();
                paint.MoveTo(new Vector2(left, bottom));
                paint.LineTo(new Vector2(right, bottom));
                paint.LineTo(new Vector2(right, top));
                paint.Stroke();
                double maxAlt = TargetAlt > 1000 ? TargetAlt : 200000;
                double maxDown = AscentTrack.PlanDownrange(maxAlt, maxAlt);
                if (Track != null)
                {
                    for (int i = 0; i < Track.Count; i++)
                    {
                        var p = Track[i];
                        if (p.AltM > maxAlt) maxAlt = p.AltM;
                        if (p.DownM > maxDown) maxDown = p.DownM;
                        double plan = AscentTrack.PlanDownrange(p.AltM, TargetAlt > 1000 ? TargetAlt : maxAlt);
                        if (plan > maxDown) maxDown = plan;
                    }
                }
                if (!(maxDown > 1)) maxDown = 1;
                Vector2 Map(double alt, double down)
                {
                    float x = left + (float)(down / maxDown) * (right - left);
                    float y = bottom - (float)(alt / maxAlt) * (bottom - top);
                    return new Vector2(x, y);
                }
                paint.strokeColor = new Color(0.24f, 0.86f, 1f, 0.95f);
                paint.lineWidth = 2.5f;
                paint.BeginPath();
                bool moved = false;
                int steps = 24;
                for (int i = 0; i <= steps; i++)
                {
                    double alt = maxAlt * i / steps;
                    var v = Map(alt, AscentTrack.PlanDownrange(alt, maxAlt));
                    if (!moved) { paint.MoveTo(v); moved = true; }
                    else paint.LineTo(v);
                }
                paint.Stroke();
                if (Track == null || Track.Count < 2) return;
                int hot = -1;
                double hotQ = 0;
                for (int i = 0; i < Track.Count; i++)
                    if (Track[i].QPa > hotQ) { hotQ = Track[i].QPa; hot = i; }
                if (hot >= 0 && hotQ >= GuidanceProfile.MaxQReferencePa * GuidanceProfile.MaxQBucketFraction)
                {
                    var c = Map(Track[hot].AltM, Track[hot].DownM);
                    paint.fillColor = new Color(0.78f, 0.48f, 0.1f, 0.35f);
                    paint.BeginPath();
                    paint.MoveTo(new Vector2(c.x - 18f, c.y - 18f));
                    paint.LineTo(new Vector2(c.x + 18f, c.y - 18f));
                    paint.LineTo(new Vector2(c.x + 18f, c.y + 18f));
                    paint.LineTo(new Vector2(c.x - 18f, c.y + 18f));
                    paint.ClosePath();
                    paint.Fill();
                }
                bool amber = Track.Nudged;
                paint.strokeColor = amber ? new Color(0.94f, 0.63f, 0.13f, 0.95f) : new Color(0.24f, 0.86f, 1f, 0.35f);
                paint.lineWidth = 2.5f;
                for (int i = 1; i < Track.Count; i++)
                {
                    if (amber && (i % 2 == 0)) continue;
                    paint.BeginPath();
                    paint.MoveTo(Map(Track[i - 1].AltM, Track[i - 1].DownM));
                    paint.LineTo(Map(Track[i].AltM, Track[i].DownM));
                    paint.Stroke();
                }
            }
        }
    }
}

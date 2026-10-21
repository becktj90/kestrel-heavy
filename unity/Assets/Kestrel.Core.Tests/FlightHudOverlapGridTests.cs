// The dotnet model grid for the two overlap kinds CI EditMode found on #332 (job 119394354782):
//   1) 335x720, 24 px pads, cruise: the pinned rail (bottom 496) ran into the flex Follow / Cam / Pause row (top 495);
//   2) 1280x800 web (panel 1214x759): the health detail, "one gap under the strip", landed on the landscape health tabs.
// Every portrait size x safe-area pad x HUD mode x banner row x nudge row x panel scale, with the flex tools row
// anywhere from the frame's row up to 8 px higher (its 8 px margin and the buttons' own margins), and every landscape
// panel: the rail fitted by FlightHudLayout.PlotAbove and the popover from FlightHudLayout.HealthPopover never touch
// the stack, in whole pixels.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FlightHudOverlapGridTests
    {
        static readonly float[] Widths = { 320f, 335f, 360f, 375f, 390f, 393f, 402f, 414f, 428f, 430f, 440f, 744f, 820f, 1032f };
        static readonly float[] Heights = { 568f, 640f, 667f, 720f, 812f, 844f, 874f, 896f, 932f, 956f, 1133f, 1180f, 1376f };
        static readonly float[][] Pads = { new[] { 0f, 0f, 0f }, new[] { 24f, 24f, 24f }, new[] { 20f, 0f, 0f }, new[] { 0f, 20f, 0f }, new[] { 47f, 34f, 0f }, new[] { 62f, 34f, 24f }, new[] { 59f, 34f, 0f } };
        static readonly FlightHudMode[] Modes = { FlightHudMode.Cruise, FlightHudMode.Notes, FlightHudMode.Prompt, FlightHudMode.PromptNotes };
        static readonly float[] Scales = { 1f, 2f, 3f };
        static readonly float[] RowLiftPx = { 0f, 1f, 4f, 5f, 8f };

        static HudBox Px(HudBox b, float upp, float ox, float oy) => new HudBox { X = b.X * upp + ox, Y = b.Y * upp + oy, W = b.W * upp, H = b.H * upp };

        static bool Whole(float v) => Math.Abs(v - Math.Round(v)) < 1e-3;

        [Test]
        public void PortraitRailEndsAboveTheMeasuredDockStackEverywhere()
        {
            int cases = 0, hidden = 0;
            var fails = new List<string>();
            foreach (float w in Widths)
            foreach (float h in Heights)
            {
                if (h <= w) continue;
                foreach (var pad in Pads)
                foreach (var mode in Modes)
                foreach (bool banner in new[] { false, true })
                foreach (bool nudges in new[] { true, false })
                foreach (float upp in Scales)
                {
                    float top = pad[0], bottom = pad[1], side = pad[2];
                    float cw = w - 2f * side, ch = h - top - bottom;
                    // AscentHud.LayoutRail: the frame in the content box, pinned from the padding edge.
                    var f = FlightHudLayout.Place(cw, ch, 0f, 0f, 0f, 0f, mode, banner ? 40f : 0f, nudges);
                    float ox = side * upp, oy = top * upp;
                    var plot = Px(f.Plot, upp, ox, oy);
                    var dock = Px(f.Dock, upp, ox, oy);
                    var row = Px(FlightHudLayout.ToolsRow(f), upp, ox, oy);
                    float gap = (float)Math.Round(FlightHudLayout.GapPt * upp);
                    bool notes = mode == FlightHudMode.Notes || mode == FlightHudMode.PromptNotes;
                    foreach (float lift in RowLiftPx)
                    {
                        cases++;
                        var flexRow = row; flexRow.Y -= lift;
                        // The notes sheet floats one gap above the real row, up to 96 pt tall (AscentHud.PlaceInfoSheet).
                        var help = notes ? new HudBox { X = dock.X, Y = flexRow.Y - gap - 96f * upp, W = dock.W, H = 96f * upp } : default;
                        float ceiling = Math.Min(flexRow.Y, dock.Y);
                        if (help.Live) ceiling = Math.Min(ceiling, help.Y);
                        var fit = FlightHudLayout.PlotAbove(plot, ceiling, gap);
                        // Too short to read, the rail is hidden (AscentHud.SeatPlot); hidden, it overlaps nothing.
                        if (!FlightHudLayout.RailShows(fit, upp)) { hidden++; if (!notes) fails.Add(w + "x" + h + " " + mode + " @" + upp + ": rail hidden without the notes"); continue; }
                        string at = w + "x" + h + " pads " + top + "/" + bottom + "/" + side + " " + mode + (banner ? " banner" : "") + (nudges ? "" : " no-nudges") + " @" + upp + "x lift " + lift;
                        if (!Whole(fit.X) || !Whole(fit.Y) || !Whole(fit.W) || !Whole(fit.H)) fails.Add(at + ": not whole pixels " + Fmt(fit));
                        if (fit.Bottom > ceiling - gap + 1e-3f) fails.Add(at + ": rail bottom " + fit.Bottom + " not one gap above " + ceiling);
                        if (fit.Y < plot.Y - 1e-3f || fit.Bottom > plot.Bottom + 1e-3f || fit.Right > plot.Right + 1e-3f) fails.Add(at + ": rail grew " + Fmt(fit) + " from " + Fmt(plot));
                        foreach (var (name, b) in new[] { ("tools row", flexRow), ("dock", dock), ("notes", help), ("health", Px(f.Health, upp, ox, oy)), ("telemetry", Px(f.Telemetry, upp, ox, oy)) })
                            if (b.Live && Hit(fit, b)) fails.Add(at + ": rail " + Fmt(fit) + " overlaps " + name + " " + Fmt(b));
                        if (f.BannerRow && Hit(fit, Px(f.Stages, upp, ox, oy))) fails.Add(at + ": rail overlaps the banner row");
                        
                    }
                }
            }
            Assert.That(cases, Is.GreaterThan(10000));
            TestContext.WriteLine(cases + " cases, rail stepped aside for the notes in " + hidden);
            Assert.That(hidden, Is.LessThan(cases / 50), "the rail stays up almost everywhere");
            Assert.That(fails, Is.Empty, fails.Count + " of " + cases + ":\n" + string.Join("\n", fails.GetRange(0, Math.Min(40, fails.Count))));
        }

        [Test]
        public void HealthDetailNeverCoversTheTabsOrTheStripAtAnySize()
        {
            var fails = new List<string>();
            float[] popH = { 24f, 40f, 64f, 120f };
            void Check(string at, HudBox health, HudBox telemetry, float screenW, float upp)
            {
                float gap = (float)Math.Round(FlightHudLayout.GapPt * upp);
                var at0 = FlightHudLayout.HealthPopover(health, telemetry, gap,
                    (float)Math.Round(FlightHudLayout.PopoverMinPt * upp), (float)Math.Round(FlightHudLayout.PopoverMaxPt * upp));
                if (!Whole(at0.X) || !Whole(at0.Y) || !Whole(at0.W)) fails.Add(at + ": not whole pixels " + Fmt(at0));
                if (at0.X < -1e-3f || at0.Right > screenW + 1e-3f) fails.Add(at + ": off screen " + Fmt(at0));
                if (at0.W < FlightHudLayout.PopoverMinPt * upp - 1f && at0.W < health.W - 1f) fails.Add(at + ": too narrow " + Fmt(at0));
                foreach (float ph in popH)
                {
                    var box = new HudBox { X = at0.X, Y = at0.Y, W = at0.W, H = ph * upp };
                    if (Hit(box, health)) fails.Add(at + " h" + ph + ": covers the health tabs " + Fmt(box) + " / " + Fmt(health));
                    if (Hit(box, telemetry)) fails.Add(at + " h" + ph + ": covers the readouts " + Fmt(box) + " / " + Fmt(telemetry));
                }
            }
            foreach (float w in Widths)
            foreach (float h in Heights)
            {
                if (h <= w) continue;
                foreach (var pad in Pads)
                foreach (var mode in Modes)
                foreach (float upp in Scales)
                {
                    var f = FlightHudLayout.Place(w - 2f * pad[2], h - pad[0] - pad[1], 0f, 0f, 0f, 0f, mode);
                    Check(w + "x" + h + " " + mode + " @" + upp, Px(f.Health, upp, 0, 0), Px(f.Telemetry, upp, 0, 0), w * upp, upp);
                }
            }
            // Landscape web panels (the scaled panel each CSS size gets) and the CI case, panel 1214 x 759.
            var wide = new List<float[]> { new[] { 1214f, 759f } };
            foreach (var s in new[] { new[] { 960f, 540f }, new[] { 1024f, 768f }, new[] { 1280f, 720f }, new[] { 1280f, 800f }, new[] { 1366f, 768f }, new[] { 1440f, 900f }, new[] { 1920f, 1080f }, new[] { 2560f, 1440f } })
            {
                WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
                float k = WebLandscapeType.PanelScale(s[0], s[1], rw, rh, m);
                wide.Add(new[] { (float)Math.Round(s[0] / k), (float)Math.Round(s[1] / k) });
                wide.Add(s);
            }
            foreach (var p in wide)
            foreach (var mode in Modes)
            {
                var f = FlightHudLayout.PlaceWide(p[0], p[1], 0f, 0f, 0f, 0f, mode);
                Check("wide " + p[0] + "x" + p[1] + " " + mode, f.Health, f.Telemetry, p[0], 1f);
            }
            Assert.That(fails, Is.Empty, fails.Count + ":\n" + string.Join("\n", fails.GetRange(0, Math.Min(40, fails.Count))));
        }

        static bool Hit(HudBox a, HudBox b) =>
            a.X < b.Right - 0.5f && a.Right > b.X + 0.5f && a.Y < b.Bottom - 0.5f && a.Bottom > b.Y + 0.5f;

        static string Fmt(HudBox b) => "(" + b.X + "," + b.Y + " " + b.W + "x" + b.H + ")";
    }
}

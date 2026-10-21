// Starter screen layout model (Phase 1 PR 5): rect-overlap, safe area, 44 pt targets and text fit at
// 335x667, 390x844 and 430x932 portrait, iPad portrait 820x1180, and landscape web 1280x720, 1366x768
// and 1920x1080 (each a 1280x720 panel after WebPanelScale). Boosters hidden, shown and locked, the
// blocked and short readiness lines, and a long readiness line. Set KH_STARTER_RECTS_DIR to dump the frames as JSON.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StarterLayoutTests
    {
        public const string LongReadiness = "Lands on Haven on its last fuel, and both boosters fly home with little to spare.";

        public struct Size
        {
            public string Name; public float W, H, Top, Bottom; public bool Wide, Exit;
            public override string ToString() => Name;
        }

        public static IEnumerable<Size> Sizes()
        {
            yield return new Size { Name = "335x667", W = 335, H = 667 };
            yield return new Size { Name = "390x844", W = 390, H = 844, Top = 47, Bottom = 34 };
            yield return new Size { Name = "430x932", W = 430, H = 932, Top = 59, Bottom = 34 };
            yield return new Size { Name = "iPad 820x1180", W = 820, H = 1180, Top = 24, Bottom = 20 };
            yield return new Size { Name = "web 335 portrait", W = 335, H = 667, Exit = true };
            foreach (var (w, h) in new[] { (1280, 720), (1366, 768), (1920, 1080) })
            {
                WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
                float s = WebLandscapeType.PanelScale(w, h, rw, rh, m);
                yield return new Size { Name = "web " + w + "x" + h, W = (float)Math.Round(w / s), H = (float)Math.Round(h / s), Wide = true, Exit = true };
            }
        }

        /// <summary>The real rocket for the mission at a control setting ("kerosene-3-f4-b0"), or its default.</summary>
        public static StarterRocket Rocket(string missionId, string key = null)
        {
            var r = StarterFixture.Rocket(missionId);
            if (key != null)
            {
                var p = key.Split('-');
                StarterRocket.TryEngine(p[0], out var e);
                r.Engine = e; r.Stages = int.Parse(p[1]); r.FuelNotch = int.Parse(p[2].Substring(1)); r.Boosters = int.Parse(p[3].Substring(1));
                Assert.AreEqual(key, r.Choice.Key, missionId + " setting");
            }
            return r;
        }

        StarterLayoutInput Input(Size s, string missionId, string readiness = null, string key = null, bool noBadge = false)
        {
            var r = Rocket(missionId, key);
            StarterScreen.Readiness(r, out var badge, out var rest);
            if (readiness != null) rest = readiness;
            if (noBadge) badge = "";
            return new StarterLayoutInput
            {
                Width = s.W, Height = s.H, SafeTop = s.Top, SafeBottom = s.Bottom,
                Wide = s.Wide, ReserveExit = s.Exit, BoostersVisible = r.BoostersVisible,
                Title = StarterScreen.Title(r.Mission), Subtitle = StarterScreen.Subtitle(r.Mission),
                Hint = StarterScreen.Hint(r, StarterKnob.None), Badge = badge, Readiness = rest,
            };
        }

        public static IEnumerable<(string name, string mission, string readiness, string key)> Cases()
        {
            yield return ("KH-1 boosters hidden", "m1", null, null);
            yield return ("KH-5 boosters shown", "m5", null, null);
            yield return ("KH-10 boosters locked", "m10", null, null);
            yield return ("KH-12 long title", "m12", null, null);
            yield return ("KH-19 long readiness", "m19", LongReadiness, null);
            yield return ("KH-7 short", "m7", null, "kerosene-3-f0-b0");
            yield return ("KH-6 too heavy", "m6", null, "methane-3-f4-b0");
        }

        [Test]
        public void EveryDeviceSize_NoOverlap_InsideSafeArea_TargetsAndTextFit()
        {
            var fails = new List<string>();
            foreach (var s in Sizes())
                foreach (var c in Cases())
                {
                    var input = Input(s, c.mission, c.readiness, c.key);
                    var f = StarterLayout.Place(input);
                    foreach (var p in StarterLayout.Problems(f, input)) fails.Add(s + " " + c.name + ": " + p);
                    Dump(s, c.name, input, f);
                }
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
        }

        [Test]
        public void Portrait_RocketKeepsAFifthOfTheScreen_AndTheSheetSitsUnderIt()
        {
            foreach (var s in Sizes())
            {
                if (s.Wide) continue;
                foreach (var c in Cases())
                {
                    var f = StarterLayout.Place(Input(s, c.mission, c.readiness, c.key));
                    string where = s + " " + c.name;
                    Assert.IsFalse(f.Wide, where);
                    // SE-height screens tighten the sheet (Compact) and keep at least 16% for the rocket; taller phones 24%.
                    Assert.AreEqual(s.H - s.Top - s.Bottom < StarterLayout.CompactBelow, f.Compact, where);
                    Assert.GreaterOrEqual(f.Rocket.H, s.H * (f.Compact ? 0.16f : 0.24f), where + " rocket " + f.Rocket.H);
                    Assert.Less(f.Rocket.Bottom, f.Sheet.Y, where);
                    Assert.Less(f.Header.Bottom, f.Rocket.Y + 0.5f, where);
                    Assert.LessOrEqual(f.Sheet.W, StarterLayout.ColumnMax + 0.5f, where);
                    Assert.AreEqual(s.W * 0.5f, f.Sheet.X + f.Sheet.W * 0.5f, 0.5f, where + " sheet centred");
                    // Back top-left and Reset top-right, like every screen; Launch is the last, full-width row.
                    Assert.Less(f.Back.X, s.W * 0.25f, where);
                    Assert.Greater(f.Reset.Right, s.W * 0.6f, where);
                    Assert.AreEqual(f.Sheet.Bottom - f.SheetPadPt, f.Launch.Bottom, 0.5f, where);
                }
            }
        }

        [Test]
        public void Wide_ThreeTypeCaps_TheBand_AndLaunchBottomRight()
        {
            foreach (var s in Sizes())
            {
                if (!s.Wide) continue;
                foreach (var c in Cases())
                {
                    var input = Input(s, c.mission, c.readiness, c.key);
                    var f = StarterLayout.Place(input);
                    string where = s + " " + c.name;
                    Assert.IsTrue(f.Wide, where);
                    Assert.AreEqual(1280f, s.W, 1f, where + " a 16:9 monitor is a 1280x720 panel");
                    Assert.LessOrEqual(f.Sheet.W, WebLandscapeType.SideCap, where);
                    Assert.GreaterOrEqual(f.Sheet.W, StarterLayout.SideMin, where);
                    Assert.GreaterOrEqual(f.Rocket.W, (s.W - 16f) * WebLandscapeType.RocketMinFraction, where);
                    Assert.LessOrEqual(f.Sheet.Right, s.W - StarterLayout.Edge + 0.5f, where);
                    Assert.Greater(f.Launch.X, s.W * 0.5f, where + " Launch on the right");
                    Assert.Greater(f.Launch.Bottom, s.H * 0.8f, where + " Launch at the bottom");
                    Assert.LessOrEqual(f.TitlePt, WebLandscapeType.TitleCap, where);
                    Assert.LessOrEqual(f.LabelPt, WebLandscapeType.TileCap, where);
                    Assert.LessOrEqual(f.ReadinessPt, WebLandscapeType.BodyCap, where);
                    Assert.LessOrEqual(f.HintPt, WebLandscapeType.BodyCap, where);
                    Assert.LessOrEqual(f.CtaPt, WebLandscapeType.CtaCap, where);
                    Assert.IsTrue(f.Exit.Live, where);
                    Assert.LessOrEqual(f.Reset.Right, f.Exit.X, where + " Reset clears the page EXIT");
                }
            }
        }

        [Test]
        public void Readiness_WrapsBeforeItShrinks()
        {
            var s = new Size { Name = "335x667", W = 335, H = 667 };
            var plain = StarterLayout.Place(Input(s, "m3"));
            Assert.AreEqual(16f, plain.ReadinessPt, "the normal line keeps body size");
            Assert.LessOrEqual(plain.ReadinessLines, 2);
            var longer = StarterLayout.Place(Input(s, "m19", LongReadiness));
            Assert.LessOrEqual(longer.ReadinessLines, StarterLayout.ReadinessMaxLines);
            Assert.GreaterOrEqual(longer.ReadinessPt, StarterLayout.ReadinessMinPt);
            Assert.Greater(longer.Readiness.H, plain.Readiness.H, "the long line takes more rows");
            var bare = StarterLayout.Place(Input(s, "m3", null, null, noBadge: true));
            Assert.IsFalse(bare.Badge.Live, "no badge box without a badge");
            Assert.AreEqual(bare.Sheet.X + bare.SheetPadPt, bare.Readiness.X, 0.01f);
        }

        [Test]
        public void Boosters_RowOnlyWhereTheMissionAllowsThem()
        {
            var s = new Size { Name = "390x844", W = 390, H = 844, Top = 47, Bottom = 34 };
            var none = StarterLayout.Place(Input(s, "m3"));
            Assert.IsNull(none.Boosters, "KH-3: whole row hidden, not disabled");
            var shown = StarterLayout.Place(Input(s, "m5"));
            Assert.IsNotNull(shown.Boosters);
            Assert.AreEqual(3, shown.Boosters.Options.Length);
            Assert.Greater(none.Rocket.H, shown.Rocket.H, "the hidden row gives its room to the rocket");
            Assert.AreEqual(StarterRocket.FuelNotches, shown.Fuel.Options.Length);
            foreach (var n in shown.Fuel.Options) Assert.GreaterOrEqual(n.W, StarterLayout.Touch);
        }

        static void Dump(Size s, string name, StarterLayoutInput i, StarterFrame f)
        {
            string dir = Environment.GetEnvironmentVariable("KH_STARTER_RECTS_DIR");
            if (string.IsNullOrEmpty(dir)) return;
            Directory.CreateDirectory(dir);
            var sb = new StringBuilder();
            sb.Append("{\"size\":\"").Append(s.Name).Append("\",\"case\":\"").Append(name).Append("\",\"w\":").Append(N(s.W)).Append(",\"h\":").Append(N(s.H));
            sb.Append(",\"safeTop\":").Append(N(s.Top)).Append(",\"safeBottom\":").Append(N(s.Bottom)).Append(",\"wide\":").Append(f.Wide ? "true" : "false");
            sb.Append(",\"sheet\":").Append(B(f.Sheet)).Append(",\"header\":").Append(B(f.Header)).Append(",\"boxes\":[");
            bool first = true;
            foreach (var kv in f.Leaves())
            {
                if (!first) sb.Append(',');
                first = false;
                string text = "";
                foreach (var t in f.Texts) if (t.Name == kv.Key) text = t.Text;
                sb.Append("{\"name\":\"").Append(kv.Key).Append("\",\"box\":").Append(B(kv.Value)).Append(",\"text\":\"").Append(text.Replace("\"", "'")).Append("\"}");
            }
            sb.Append("]}");
            string file = (s.Name + "-" + name).Replace(' ', '_') + ".json";
            File.WriteAllText(Path.Combine(dir, file), sb.ToString());
        }

        static string N(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
        static string B(HudBox b) => "[" + N(b.X) + "," + N(b.Y) + "," + N(b.W) + "," + N(b.H) + "]";
    }
}

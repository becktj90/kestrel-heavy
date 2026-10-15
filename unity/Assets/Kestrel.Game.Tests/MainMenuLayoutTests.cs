// EditMode checks for the 3D hangar menu: depths, 44 px hits, no overlapping controls,
// crisp TextMeshPro settings, and a high-poly KH-1 with true-scale crew.
using System.Collections.Generic;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    public class MainMenuLayoutTests
    {
        static readonly (string name, float w, float h, float safeTop, float safeBottom)[] Devices =
        {
            ("SE", 375f, 667f, 20f, 0f),
            ("ProMax", 440f, 956f, 62f, 34f),
            ("iPad", 1032f, 1376f, 24f, 20f),
        };

        [Test]
        public void ClickablePanelsSitAtDifferentDepthsWithLaunchClosest()
        {
            var seen = new HashSet<float>();
            float launch = float.MaxValue;
            int clicks = 0;
            foreach (var a in MainMenuLayout.Anchors())
            {
                if (!a.Clickable) continue;
                clicks++;
                Assert.That(seen.Add(a.Depth), a.Id + " shares a depth with another control");
                if (a.Id == "launch") launch = a.Depth;
            }
            Assert.That(clicks, Is.EqualTo(5));
            foreach (var a in MainMenuLayout.Anchors())
                if (a.Clickable) Assert.That(launch, Is.LessThanOrEqualTo(a.Depth), a.Id);
            Assert.That(MainMenuLayout.Find("launch").Depth, Is.LessThan(MainMenuLayout.Find("codex").Depth));
            Assert.That(MainMenuLayout.Find("codex").Depth, Is.LessThan(MainMenuLayout.Find("hangar").Depth));
            Assert.That(MainMenuLayout.Find("title").Depth, Is.GreaterThan(MainMenuLayout.Find("settings").Depth));
            Assert.That(MainMenuLayout.Find("title").Depth, Is.LessThan(MainMenuLayout.Find("hangar").Depth));
            Assert.That(MainMenuLayout.Find("tel-l").Depth, Is.LessThan(MainMenuLayout.RocketDepth));
            Assert.That(MainMenuLayout.CrewDepth, Is.LessThan(MainMenuLayout.RocketDepth));
            Assert.That(MainMenuLayout.RocketDepth, Is.LessThan(MainMenuLayout.HangarWallDepth));
        }

        [Test]
        public void OnlyLaunchAndHangarAreLive()
        {
            foreach (var a in MainMenuLayout.Anchors())
            {
                if (!a.Clickable) continue;
                bool live = a.Id == "launch" || a.Id == "hangar";
                Assert.That(a.Live, Is.EqualTo(live), a.Id);
                Assert.That(a.Spoken, Is.Not.Empty, a.Id);
                Assert.That(a.Label, Is.Not.Empty, a.Id);
            }
            Assert.That(MainMenuLayout.LaunchLabel, Is.EqualTo("INITIATE LAUNCH SEQUENCE"));
            Assert.That(MainMenuLayout.LaunchLabel.Contains("\n"), Is.False);
            Assert.That(MainMenuLayout.TitleTop, Is.EqualTo("KESTREL"));
            Assert.That(MainMenuLayout.TitleBottom, Is.EqualTo("HEAVY"));
        }

        [Test]
        public void HitsClear44pxAndStayInsideTheSafeArea()
        {
            foreach (var (n, w, h, top, bot) in Devices)
            {
                float aspect = w / h;
                float safeBottom = bot / h;
                float safeTop = 1f - top / h;
                var clickable = new List<ScreenBox>();
                foreach (var a in MainMenuLayout.Anchors())
                {
                    var box = MainMenuLayout.RestBox(a);
                    Assert.That(box.X0, Is.GreaterThan(0.02f), n + " " + a.Id);
                    Assert.That(box.X1, Is.LessThan(0.98f), n + " " + a.Id);
                    Assert.That(box.Y0, Is.GreaterThanOrEqualTo(safeBottom - 0.001f), n + " " + a.Id);
                    Assert.That(box.Y1, Is.LessThanOrEqualTo(safeTop + 0.001f), n + " " + a.Id);
                    if (a.Clickable)
                    {
                        Assert.That(box.Height * h, Is.GreaterThanOrEqualTo(MainMenuLayout.MinHitPx), n + " " + a.Id);
                        clickable.Add(box);
                    }
                }
                for (int i = 0; i < clickable.Count; i++)
                    for (int j = i + 1; j < clickable.Count; j++)
                        Assert.That(clickable[i].Overlaps(clickable[j], 0.01f), Is.False, n);
                var title = MainMenuLayout.RestBox(MainMenuLayout.Find("title"));
                var sub = MainMenuLayout.RestBox(MainMenuLayout.Find("subtitle"));
                Assert.That(title.Y0, Is.GreaterThan(sub.Y1), n + ": title overlaps the tagline");
                Assert.That(title.Y0 - sub.Y1, Is.GreaterThan(0.02f), n);
                var nose = MainMenuLayout.Viewport(
                    MainMenuLayout.World(MainMenuLayout.RocketDepth, 0f, MainMenuLayout.RocketNoseYFrac, aspect),
                    MainMenuLayout.Pose(), aspect);
                var baseP = MainMenuLayout.Viewport(MainMenuLayout.RocketBase(aspect), MainMenuLayout.Pose(), aspect);
                Assert.That(nose.y, Is.GreaterThan(baseP.y), n);
                Assert.That(sub.Y0, Is.GreaterThan(nose.y), n + ": tagline covers the nose");
            }
        }

        [Test]
        public void DriftParallaxMovesTheLaunchBarMoreThanTheRocketAndStaysOnScreen()
        {
            float[] times = { 0.2f, 0.8f, 1.4f, 2.2f, 3.5f, 5f, 8f };
            foreach (var (n, w, h, _, _) in Devices)
            {
                float aspect = w / h;
                var rest = MainMenuLayout.Pose();
                var launch = MainMenuLayout.World(MainMenuLayout.Find("launch"), aspect);
                var rocket = MainMenuLayout.RocketBase(aspect);
                float maxNear = 0f, maxFar = 0f;
                foreach (float t in times)
                {
                    var pose = MainMenuLayout.PoseAt(t, false);
                    float dNear = Mathf.Abs(MainMenuLayout.Viewport(launch, pose, aspect).x - MainMenuLayout.Viewport(launch, rest, aspect).x);
                    float dFar = Mathf.Abs(MainMenuLayout.Viewport(rocket, pose, aspect).x - MainMenuLayout.Viewport(rocket, rest, aspect).x);
                    maxNear = Mathf.Max(maxNear, dNear);
                    maxFar = Mathf.Max(maxFar, dFar);
                    foreach (var a in MainMenuLayout.Anchors())
                    {
                        Vector2 size = MainMenuLayout.WorldSize(a, aspect);
                        Vector3 center = MainMenuLayout.World(a, aspect);
                        Vector3 right = rest.Right * (size.x * 0.5f);
                        Vector3 up = rest.Up * (size.y * 0.5f);
                        foreach (var corner in new[] { center - right - up, center + right - up, center - right + up, center + right + up })
                        {
                            var vp = MainMenuLayout.Viewport(corner, pose, aspect);
                            Assert.That(vp.x, Is.InRange(-0.02f, 1.02f), n + " " + a.Id);
                            Assert.That(vp.y, Is.InRange(-0.02f, 1.02f), n + " " + a.Id);
                        }
                    }
                }
                Assert.That(maxNear, Is.GreaterThan(maxFar), n);
                var held = MainMenuLayout.PoseAt(3f, true);
                Assert.That(held.Eye, Is.EqualTo(rest.Eye));
            }
        }

        [Test]
        public void ReadoutIsSingleLineAndDoesNotDoubleLetters()
        {
            var read = MainMenuLayout.Readout(1.25f, false);
            foreach (var line in read.All())
            {
                Assert.That(line, Is.Not.Empty);
                Assert.That(line.Contains("\n"), Is.False, line);
                Assert.That(line.Contains("  "), Is.False, line);
            }
            Assert.That(read.LeftFuel, Does.StartWith("FUEL: "));
            Assert.That(read.RightThrust, Does.Contain("kN"));
            Assert.That(read.RightStatus, Is.EqualTo("STATUS: GO"));
            Assert.That(read.Designation, Is.EqualTo("KH-1"));
            var held = MainMenuLayout.Readout(0f, true);
            var later = MainMenuLayout.Readout(4f, true);
            Assert.That(later.LeftFuel, Is.EqualTo(held.LeftFuel));
            Assert.That(held.LeftFuel, Is.EqualTo("FUEL: 92%"));
            Assert.That(held.RightThrust, Is.EqualTo("THRUST: 850 kN"));
        }

        [Test]
        public void TextStylesStayCrispAndDoNotStackASecondGlyph()
        {
            foreach (MenuTextRole role in System.Enum.GetValues(typeof(MenuTextRole)))
            {
                var s = MainMenuLayout.Style(role);
                Assert.That(s.FontSize, Is.GreaterThanOrEqualTo(36f), role.ToString());
                Assert.That(s.CharacterSpacing, Is.InRange(2f, 12f), role.ToString());
                Assert.That(s.OutlineWidth, Is.InRange(0f, 0.1f), role.ToString());
                Assert.That(s.FaceDilate, Is.EqualTo(0f), role.ToString());
                Assert.That(s.ExtraPadding, Is.True, role.ToString());
                Assert.That(s.AutoSize, Is.False, role.ToString());
                Assert.That(s.WordWrap, Is.False, role.ToString());
                Assert.That(s.RichText, Is.False, role.ToString());
                Assert.That(s.Bold, Is.False, role.ToString());
                Assert.That(s.Underlay, Is.False, role.ToString());
            }
        }

        [Test]
        public void MotionHoldsStillWhenReducedAndGlassHoverBrightens()
        {
            Assert.That(MainMenuLayout.ReduceMotionPref, Is.EqualTo(HangarCamera.ReduceMotionPref));
            Assert.That(MainMenuLayout.ReduceFlashingPref, Is.EqualTo(HangarCamera.ReduceFlashingPref));
            Assert.That(MainMenuLayout.Pulse(0.4f, true), Is.EqualTo(MainMenuLayout.Pulse(9f, true)));
            float breatheA = MainMenuLayout.Pulse(0.3f, false);
            float breatheB = MainMenuLayout.Pulse(1.1f, false);
            Assert.That(Mathf.Abs(breatheA - breatheB), Is.GreaterThan(0.02f));
            Assert.That(MainMenuLayout.DialAngle(2f, true, 1f), Is.EqualTo(0f));
            Assert.That(MainMenuLayout.DialAngle(2f, false, 1f), Is.EqualTo(-MainMenuLayout.DialAngle(2f, false, -1f)));
            Assert.That(Mathf.Abs(MainMenuLayout.DialAngle(1f, false, 1f)), Is.GreaterThan(5f));
            Assert.That(MainMenuLayout.Approach(0.1f, 0.9f, 1f, true), Is.EqualTo(0.9f));
            float mid = MainMenuLayout.Approach(0f, 1f, 0.05f, false);
            Assert.That(mid, Is.GreaterThan(0f).And.LessThan(1f));
            MainMenuLayout.GlassVisual(true, false, out float restA, out float restG);
            MainMenuLayout.GlassVisual(true, true, out float hotA, out float hotG);
            Assert.That(hotA, Is.GreaterThan(restA));
            Assert.That(hotG, Is.GreaterThan(restG));
            MainMenuLayout.GlassVisual(false, true, out float deadA, out _);
            Assert.That(deadA, Is.LessThan(hotA));
        }

        [Test]
        public void RocketIsHighPolyAndCrewMatchItsScale()
        {
            var core = MainMenuMesh.CoreBody();
            var booster = MainMenuMesh.BoosterBody();
            var bell = MainMenuMesh.Bell();
            var crew = MainMenuMesh.Crew();
            try
            {
                int verts = core.vertexCount + booster.vertexCount * 2 + bell.vertexCount * 27;
                Assert.That(verts, Is.GreaterThan(8000), "showcase stack should be far denser than the flat poster");
                Assert.That(core.bounds.size.y, Is.EqualTo(MainMenuLayout.VehicleHeightM).Within(0.2f));
                Assert.That(crew.bounds.max.y, Is.InRange(1.6f, 1.9f));
                Assert.That(crew.bounds.min.y, Is.GreaterThan(-0.05f));
                var box = MainMenuMesh.UnitBox();
                try { AssertOutward(box); }
                finally { Object.DestroyImmediate(box); }
                Assert.That(MainMenuLayout.CrewHeightM / MainMenuLayout.VehicleHeightM, Is.LessThan(0.03f));
                Assert.That(MainMenuLayout.CrewCount, Is.GreaterThanOrEqualTo(4));
                float scale = MainMenuLayout.RocketScale(375f / 667f);
                Assert.That(scale, Is.GreaterThan(0.01f).And.LessThan(0.2f));
            }
            finally
            {
                Object.DestroyImmediate(core);
                Object.DestroyImmediate(booster);
                Object.DestroyImmediate(bell);
                Object.DestroyImmediate(crew);
            }
        }

        static void AssertOutward(Mesh mesh)
        {
            var v = mesh.vertices;
            var n = mesh.normals;
            var tri = mesh.triangles;
            int outward = 0, inward = 0;
            for (int i = 0; i < tri.Length; i += 3)
            {
                Vector3 cross = Vector3.Cross(v[tri[i + 1]] - v[tri[i]], v[tri[i + 2]] - v[tri[i]]);
                if (Vector3.Dot(cross, n[tri[i]]) >= 0f) outward++;
                else inward++;
            }
            Assert.That(outward, Is.GreaterThan(inward), "panel faces should point outward so the lit side faces the camera");
        }
    }
}

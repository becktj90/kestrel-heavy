// Engine-free: runs in Unity EditMode (Kestrel.Livery.Tests) and dotnet (Tests/Dotnet~).
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kestrel.Livery;
using NUnit.Framework;

namespace Kestrel.Livery.Tests
{
    public class LiveryDataTests
    {
        static string DataFile(string name)
        {
            foreach (var start in new[] { TestContext.CurrentContext.TestDirectory, Directory.GetCurrentDirectory() })
                for (var d = new DirectoryInfo(start); d != null; d = d.Parent)
                {
                    var p = Path.Combine(d.FullName, "data", name);
                    if (File.Exists(p)) return File.ReadAllText(p);
                }
            Assert.Fail("data/" + name + " not found"); return null;
        }

        [Test]
        public void ShippedFinishesAreValid()
        {
            var e = new List<string>(); var f = LiveryCatalog.LoadFinishes(DataFile("finishes.json"), e);
            Assert.That(e, Is.Empty, string.Join("\n", e));
            Assert.That(f.Count, Is.EqualTo(LiveryCatalog.FinishCount20));
            Assert.That(f.Select(x => x.Id), Is.SupersetOf(new[] { "bare_metal", "white_paint", "cork_insulation", "carbon_composite", "soot_weathered" }), "#142 finish list");
            Assert.That(f.Count(x => x.Earn.Type == EarnType.Start), Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void ShippedLiveriesAreValidAndAllEarnableOffline()
        {
            var e = new List<string>(); var l = LiveryCatalog.LoadLiveries(DataFile("liveries.json"), e);
            Assert.That(e, Is.Empty, string.Join("\n", e));
            Assert.That(l.Count, Is.EqualTo(LiveryCatalog.LiveryCount20));
            Assert.That(l.Count(x => x.Earn.Type == EarnType.Start), Is.EqualTo(4));
            // A player who has played enough earns every livery, offline, with no purchase state at all.
            var p = new PlayProgress { Orbits = 1000, Landings = 1000 };
            for (int i = 1; i <= 5; i++) p.MissionsCompleted.Add("KH-" + i);
            Assert.That(l.All(x => Unlocks.IsEarned(x.Earn, p)), Is.True);
            Assert.That(l.Select(x => x.Pattern).Distinct().Count(), Is.EqualTo(LiveryCatalog.Patterns.Length), "every pattern used");
        }

        [Test]
        public void UnlocksFollowPlayOnlyAndNeverRevoke()
        {
            var r = new EarnRule { Type = EarnType.Orbits, Count = 3 };
            var p = new PlayProgress { Orbits = 2 };
            Assert.That(Unlocks.IsEarned(r, p), Is.False);
            p.Orbits = 3; Assert.That(Unlocks.IsEarned(r, p), Is.True);
            p.Orbits = 50; Assert.That(Unlocks.IsEarned(r, p), Is.True);
            Assert.That(Unlocks.IsEarned(EarnRule.StartRule, new PlayProgress()), Is.True);
            var m = new EarnRule { Type = EarnType.Mission, MissionId = "KH-4" };
            Assert.That(Unlocks.IsEarned(m, p), Is.False); p.MissionsCompleted.Add("KH-4"); Assert.That(Unlocks.IsEarned(m, p), Is.True);
        }

        const string Good = "{\"id\":\"x_y\",\"label\":\"livery.x\",\"primary\":\"#000000\",\"accent\":\"#FFFFFF\",\"pattern\":\"solid\",\"earnRule\":{\"type\":\"start\"},\"codexText\":\"codex.x\"";
        static List<string> Errs(string entryTail) { var e = new List<string>(); LiveryCatalog.LoadLiveries("{\"schema\":\"liveries_v1\",\"liveries\":[" + Good + entryTail + "}]}", e); return e; }

        [TestCase(",\"price\":0.99")]
        [TestCase(",\"costCoins\":5")]
        [TestCase(",\"expiresAt\":\"2026-12-01\"")]
        [TestCase(",\"availableUntil\":\"x\"")]
        [TestCase(",\"seasonId\":\"s1\"")]
        [TestCase(",\"randomWeight\":0.2")]
        [TestCase(",\"dropChance\":0.1")]
        [TestCase(",\"streakDays\":3")]
        [TestCase(",\"iapSku\":\"com.x\"")]
        public void MonetisationScarcityAndChanceFieldsAreRejected(string tail)
            => Assert.That(Errs(tail).Any(x => x.Contains("forbidden")), Is.True, tail);

        [Test]
        public void SchemaIsStrict()
        {
            Assert.That(Errs(""), Is.Empty);
            Assert.That(Errs(",\"extra\":1").Any(x => x.Contains("unknown field")), Is.True);
            var e = new List<string>();
            LiveryCatalog.LoadLiveries("{\"schema\":\"liveries_v1\",\"liveries\":[{\"id\":\"Bad-Id\",\"label\":\"x\",\"primary\":\"red\",\"accent\":\"#FFFFFF\",\"pattern\":\"plaid\",\"earnRule\":{\"type\":\"daily\"},\"codexText\":\"codex.x\"}]}", e);
            Assert.That(e.Count, Is.GreaterThanOrEqualTo(5), string.Join("\n", e));
            e.Clear();
            LiveryCatalog.LoadLiveries("{\"schema\":\"liveries_v1\",\"liveries\":[" + Good + "}," + Good + "}]}", e);
            Assert.That(e.Any(x => x.Contains("duplicate id")), Is.True);
            e.Clear();
            LiveryCatalog.LoadLiveries("{\"schema\":\"liveries_v1\",\"liveries\":[{\"id\":\"a\",\"label\":\"l.a\",\"primary\":\"#000000\",\"accent\":\"#FFFFFF\",\"pattern\":\"solid\",\"earnRule\":{\"type\":\"orbits\",\"count\":0},\"codexText\":\"c.a\"}]}", e);
            Assert.That(e.Any(x => x.Contains("count")), Is.True);
        }

        [Test]
        public void FinishValidation()
        {
            var e = new List<string>();
            LiveryCatalog.LoadFinishes("{\"schema\":\"finishes_v1\",\"finishes\":[{\"id\":\"f\",\"label\":\"finish.f\",\"partTypes\":[\"tank\",\"wing\"],\"albedo\":\"#112233\",\"roughness\":1.5,\"metallic\":0}]}", e);
            Assert.That(e.Any(x => x.Contains("partType")), Is.True);
            Assert.That(e.Any(x => x.Contains("roughness")), Is.True);
            e.Clear();
            var f = LiveryCatalog.LoadFinishes("{\"schema\":\"finishes_v1\",\"finishes\":[{\"id\":\"f\",\"label\":\"finish.f\",\"partTypes\":[\"tank\"],\"albedo\":\"#112233\",\"roughness\":0.5,\"metallic\":0}]}", e);
            Assert.That(e, Is.Empty); Assert.That(f[0].Earn.Type, Is.EqualTo(EarnType.Start), "earnRule optional, defaults to start"); Assert.That(f[0].Tint, Is.Null);
        }

        [Test]
        public void MiniJsonBasics()
        {
            var o = (Dictionary<string, object>)MiniJson.Parse("{\"a\":[1,2.5,-3e2],\"b\":\"x\\u0041\\n\",\"c\":true,\"d\":null}");
            Assert.That(((List<object>)o["a"]).Cast<double>(), Is.EqualTo(new[] { 1, 2.5, -300 }));
            Assert.That(o["b"], Is.EqualTo("xA\n")); Assert.That(o["c"], Is.EqualTo(true)); Assert.That(o["d"], Is.Null);
            Assert.Throws<System.FormatException>(() => MiniJson.Parse("{\"a\":1,\"a\":2}"));
            Assert.Throws<System.FormatException>(() => MiniJson.Parse("{\"a\":1} x"));
        }
    }
}

// The Codex is data the game can show. These checks keep the eight lessons, the kid
// length, and the sim numbers from drifting, and hold the real-history guards (Trevor, 2026-11-11,
// DECISIONS #100/#101, follows PR #496): real names, history and dates are welcome, but only as
// sourced facts in an entry's History list; a real-history fact never names a KH mission, the
// Kestrel, a pad or Haven; and nothing ties our game to Blue Origin, its vehicles or sites
// (no-affiliation guard). Mirrors tests/codex.test.cjs; keep both in step.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kestrel.Core.Codex;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class CodexTests
    {
        static readonly string[] Ids = { "delta-v", "twr", "isp", "staging", "max-q", "cg-cp", "orbits", "booster-landing" };

        // NA (no-affiliation, DECISIONS #101, same rule and names as tests/narration-facts-lint.test.cjs in
        // PR #496): a string naming Blue Origin, its vehicles or sites must not also name our own world
        // (OwnWorld) or use affiliation wording. Any spacing, dot, underscore or dash variant counts.
        const string Sep = @"[\s._\-\u2010-\u2015]*";
        static readonly Regex BoNames = new Regex(
            "blue" + Sep + "origin|new" + Sep + "glenn|new" + Sep + "shepard|jack" + Sep + "lyn|lc" + Sep + @"36(?!\d)", RegexOptions.IgnoreCase);
        static readonly Regex Affiliation = new Regex(
            @"\b(our|ours|partner(s|ed|ship)?|official(ly)?|sponsor(s|ed)?|endorse[sd]?|endorsement|based on|modell?ed (on|after)|inspired by|licen[cs]ed?|affiliated?|approved by|in collaboration|version of|cop(y|ied) of|clone|replica|tribute)\b",
            RegexOptions.IgnoreCase);

        static IEnumerable<string> AffiliationErrors(string t, Regex ownWorld)
        {
            var n = BoNames.Match(t);
            if (!n.Success) yield break;
            var w = ownWorld.Match(t); var a = Affiliation.Match(t);
            if (w.Success) yield return "names " + n.Value + " in the same text as " + w.Value + " (DECISIONS #101)";
            if (a.Success) yield return "names " + n.Value + " with affiliation wording " + a.Value + " (DECISIONS #101)";
        }

        // Wording bans, unchanged, everywhere.
        static readonly string[] Wording = { "suicide", "RP-1", "\u2014", "\u2013" };
        // G2: real-world markers allowed only in a sourced History fact, never in title, kid or engineer.
        static readonly Regex RealNames = new Regex(
            @"\b(Blue Origin|New Glenn|New Shepard|Jacklyn|LC-36|SpaceX|NASA|ESA|JAXA|ISRO|Roscosmos|Boeing|Lockheed|Northrop|Ariane(space)?|Soyuz|Vostok|Sputnik|Gagarin|Goddard|Korolev|von Braun|Armstrong|Aldrin|Glenn|Tereshkova|Leonov|Falcon|Starship|Saturn V|Saturn IB?|Apollo|Gemini|Mercury program|Space Shuttle|Shuttle|N1|Mariner|Voyager|Telstar|Hubble|Rocket Lab|ULA|Delta IV|Redstone)\b");
        static readonly Regex Year = new Regex(
            @"\b(1[89]\d\d|20\d\d)\b(?!\s*(m\b|km|kg|t\b|s\b|N\b|Pa|kPa|K\b|%|\u00b0|m/s|x\b|\.\d|,\d))");
        static readonly Regex SimSource = new Regex(@"^[A-Za-z0-9._/-]+$");

        static Regex OwnWorld()
        {
            var sites = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "sites.json"))).Arr("sites")
                .Select(s => @"\b" + Regex.Escape(((JsonObject)s).Str("name")).Replace(@"\ ", Sep) + @"\b");
            return new Regex(string.Join("|", new[] { @"\bKH" + Sep + @"\d+", @"\bKestrel", @"\bHaven\b" }.Concat(sites)), RegexOptions.IgnoreCase);
        }

        static void Strings(object v, List<string> o)
        {
            if (v is string s) o.Add(s);
            else if (v is JsonObject jo) foreach (var kv in jo) { o.Add(kv.Key); Strings(kv.Value, o); }
            else if (v is List<object> l) foreach (var x in l) Strings(x, o);
        }

        // Returns every guard failure; tags NA, G2..G4 match the JS suite. Parse already throws on
        // structural G4 failures (history fact with no ref, an unknown ref, a non-https url).
        static List<string> Lint(string json)
        {
            var errs = new List<string>();
            var all = new List<string>();
            Strings(MiniJson.Parse(json), all);
            var ownWorld = OwnWorld();
            foreach (var s in all)
            {
                foreach (var a in AffiliationErrors(s, ownWorld)) errs.Add("NA no-affiliation: " + a);
                foreach (var w in Wording) if (s.Contains(w)) errs.Add("wording: " + w);
            }
            CodexBook book;
            try { book = CodexBook.Parse(json); }
            catch (FormatException ex) { errs.Add("G4 parse: " + ex.Message); return errs; }
            var used = new HashSet<string>();
            foreach (var e in book.Entries)
            {
                if (e.Sources.Count == 0) errs.Add("G4 " + e.Id + ": sim sources empty");
                foreach (var s in e.Sources) if (!SimSource.IsMatch(s)) errs.Add("G4 " + e.Id + ": bad sim source " + s);
                foreach (var (f, t) in new[] { ("title", e.Title), ("kid", e.Kid), ("engineer", e.Engineer) })
                {
                    var n = RealNames.Match(t); var y = Year.Match(t);
                    if (n.Success) errs.Add("G2 " + e.Id + "." + f + ": real name " + n.Value);
                    if (y.Success) errs.Add("G2 " + e.Id + "." + f + ": date " + y.Value);
                }
                for (int i = 0; i < e.History.Count; i++)
                {
                    var fx = ownWorld.Match(e.History[i].Text);
                    if (fx.Success) errs.Add("G3 " + e.Id + ".history[" + i + "]: names game content " + fx.Value);
                    foreach (var r in e.History[i].Refs) used.Add(r);
                }
            }
            foreach (var r in book.Refs.Values)
            {
                var fx = ownWorld.Match(r.Title);
                if (fx.Success) errs.Add("G3 ref " + r.Id + ": names game content " + fx.Value);
                if (!used.Contains(r.Id)) errs.Add("G4 ref " + r.Id + ": unused");
            }
            return errs;
        }

        static string Repo() => File.ReadAllText(Path.Combine(Golden.Root, "data", "codex.json"));

        [Test]
        public void Book_HasTheEightLessons()
        {
            var book = CodexBook.Load(Golden.Root);
            Assert.AreEqual(1, book.Version);
            Assert.AreEqual(Ids.Length, book.Entries.Count);
            var seen = new HashSet<string>();
            for (int i = 0; i < Ids.Length; i++)
            {
                var e = book.Entries[i];
                Assert.AreEqual(Ids[i], e.Id);
                Assert.IsTrue(seen.Add(e.Id));
                Assert.AreSame(e, book.ById(e.Id));
                Assert.LessOrEqual(e.Kid.Length, CodexBook.KidMaxChars, e.Id);
                Assert.GreaterOrEqual(e.Engineer.Length, 80, e.Id);
                Assert.Greater(e.Sources.Count, 0, e.Id);
            }
            Assert.IsNull(book.ById("missing"));

            Assert.IsTrue(book.ById("delta-v").Kid.Contains("8.9"));
            Assert.IsTrue(book.ById("delta-v").Engineer.Contains("8876"));
            Assert.IsTrue(book.ById("delta-v").Engineer.Contains("9.80665"));
            Assert.IsTrue(book.ById("twr").Kid.Contains("1.25"));
            Assert.IsTrue(book.ById("max-q").Kid.Contains("45"));
            Assert.IsTrue(book.ById("max-q").Engineer.Contains("maxq-high"));
            Assert.IsTrue(book.ById("cg-cp").Engineer.Contains("stabilityCal"));
            Assert.IsTrue(book.ById("orbits").Engineer.Contains("11330"));
            Assert.IsTrue(book.ById("orbits").Kid.Contains("0.41"));
            Assert.IsTrue(book.ById("booster-landing").Kid.Contains("Haven"));
            Assert.IsTrue(book.ById("booster-landing").Engineer.Contains("0.45"));
            Assert.IsTrue(book.ById("booster-landing").Engineer.Contains("landing burn"));
            Assert.IsFalse(book.ById("orbits").Engineer.Contains("9.4"));
        }

        [Test]
        public void Book_PassesTheRealHistoryGuard()
        {
            CollectionAssert.IsEmpty(Lint(Repo()));
        }

        // A well-formed sample with one real-history fact. Each negative case breaks one guard.
        static string Sample(string id = "a", string title = "Staging", string kid = "Short.", string engineer = null,
            string sources = "[\"RocketEq.DeltaV\"]", string history = null, string refs = null)
        {
            engineer ??= new string('y', 80);
            history ??= "[{\"text\":\"In 1969, Apollo 11 rode a Saturn V to the Moon after a 1926 first liquid-fuel flight.\",\"refs\":[\"nasa-goddard-1926\"]}]";
            refs ??= "{\"nasa-goddard-1926\":{\"title\":\"NASA: 95 years ago, the first liquid-fueled rocket\",\"url\":\"https://www.nasa.gov/history/95-years-ago-goddards-first-liquid-fueled-rocket/\"}}";
            return "{\"version\":1,\"refs\":" + refs + ",\"entries\":[{\"id\":\"" + id + "\",\"title\":\"" + title + "\",\"kid\":\"" + kid
                + "\",\"engineer\":\"" + engineer + "\",\"sources\":" + sources + ",\"history\":" + history + "}]}";
        }

        static string Fact(string text, string refs = "[\"nasa-goddard-1926\"]") => "[{\"text\":\"" + text + "\",\"refs\":" + refs + "}]";
        static bool Has(string json, string tag) => Lint(json).Any(x => x.StartsWith(tag, StringComparison.Ordinal));

        [Test]
        public void Guard_AllowsSaturnAndApolloInASourcedFact()
        {
            CollectionAssert.IsEmpty(Lint(Sample()));
        }

        static string Ref(string title, string url = "https://example.com/") =>
            "{\"nasa-goddard-1926\":{\"title\":\"" + title + "\",\"url\":\"" + url + "\"}}";

        [Test]
        public void GuardNA_AllowsBlueOriginNamesInRelevantSourcedHistory()
        {
            foreach (var n in new[] { "Blue Origin", "New Glenn", "Jacklyn", "LC-36" })
                CollectionAssert.IsEmpty(Lint(Sample(history: Fact("In 2025, " + n + " was in the news."))), n);
            CollectionAssert.IsEmpty(Lint(Sample(refs: Ref("NASA: 95 years ago, the first liquid-fueled rocket", "https://www.blueorigin.com/news"))));
        }

        [Test]
        public void GuardNA_FailsWhenTextTiesOurGameToBlueOrigin()
        {
            string[] names = { "Blue Origin", "blue-origin", "BlueOrigin", "NEW GLENN", "New\u2011Glenn", "newglenn", "New Shepard", "Jacklyn", "jack-lyn", "LC-36", "lc 36", "LC\u201336" };
            foreach (var n in names)
            {
                Assert.IsTrue(Has(Sample(kid: "Haven is like " + n + "."), "NA"), "kid " + n);
                Assert.IsTrue(Has(Sample(history: Fact("In 2025, Pier 7 hosted " + n + ".")), "NA"), "history " + n);
                Assert.IsTrue(Has(Sample(refs: Ref("Kestrel and " + n)), "NA"), "ref " + n);
                Assert.IsTrue(Has(Sample(history: Fact("In 2025, this game was officially licensed by " + n + ".")), "NA"), "licensed " + n);
                Assert.IsTrue(Has(Sample(history: Fact("In 2025, the design was based on " + n + ".")), "NA"), "based on " + n);
            }
            Assert.IsTrue(Has(Sample(history: Fact("In 2025, New Glenn flew. Our station is planned.")), "NA"));
            Assert.IsFalse(Has(Sample(history: Fact("In 1969, LC-360 was a test stand name, officially.")), "NA"), "LC-360 is not LC-36");
            Assert.IsTrue(Has(Sample(kid: "New Glenn lands too."), "G2"), "G2 keeps the names out of game fields");
        }

        [Test]
        public void GuardG2_FailsOnARealNameOrDateInTheGameFields()
        {
            Assert.IsTrue(Has(Sample(kid: "Apollo used stages."), "G2"));
            Assert.IsTrue(Has(Sample(title: "Saturn V"), "G2"));
            Assert.IsTrue(Has(Sample(engineer: new string('y', 80) + " NASA said so."), "G2"));
            Assert.IsTrue(Has(Sample(kid: "It flew in 1969."), "G2"));
            Assert.IsFalse(Has(Sample(engineer: new string('y', 80) + " LOSS_BUDGET of 1500 m/s and 2000 kg."), "G2"));
        }

        [Test]
        public void GuardG3_FailsWhenARealFactNamesGameContent()
        {
            foreach (var s in new[] { "KH-3", "kh 12", "KH\u20137", "Kestrel Heavy", "the Kestrel", "Pier 7", "pier-7", "Haven", "Coastal Pad B", "Salt Flat Pad", "North Reach" })
                Assert.IsTrue(Has(Sample(history: Fact("In 1969, " + s + " flew for real.")), "G3"), s);
            Assert.IsTrue(Has(Sample(refs: "{\"nasa-goddard-1926\":{\"title\":\"Kestrel launch log\",\"url\":\"https://example.com/\"}}"), "G3"));
        }

        [Test]
        public void GuardG4_FailsWhenAFactOrLessonLosesItsSource()
        {
            Assert.IsTrue(Has(Sample(history: Fact("In 1969.", "[]")), "G4"), "empty refs");
            Assert.IsTrue(Has(Sample(history: "[{\"text\":\"In 1969.\"}]"), "G4"), "missing refs");
            Assert.IsTrue(Has(Sample(history: Fact("In 1969.", "[\"made-up\"]")), "G4"), "unknown ref");
            Assert.IsTrue(Has(Sample(refs: "{\"nasa-goddard-1926\":{\"title\":\"T\",\"url\":\"http://www.nasa.gov/\"}}"), "G4"), "http url");
            Assert.IsTrue(Has(Sample(refs: "{\"nasa-goddard-1926\":{\"title\":\"T\",\"url\":\"https://a.example/\"},\"extra\":{\"title\":\"E\",\"url\":\"https://b.example/\"}}"), "G4"), "unused ref");
            Assert.IsTrue(Has(Sample(sources: "[]"), "G4"), "no sim source");
        }

        [Test]
        public void Wording_BansStillHoldEverywhere()
        {
            foreach (var w in new[] { "suicide burn", "RP-1", "a\u2014b", "a\u2013b" })
                Assert.IsTrue(Lint(Sample(history: Fact("In 1969 " + w))).Any(x => x.StartsWith("wording", StringComparison.Ordinal)), w);
        }

        [Test]
        public void Parse_RejectsADuplicateIdAndAnOverlongKidLine()
        {
            Assert.Throws<FormatException>(() => CodexBook.Parse("{\"version\":1,\"entries\":[" + Entry("a") + "," + Entry("a") + "]}"));
            string longKid = new string('x', 301);
            Assert.Throws<FormatException>(() => CodexBook.Parse(
                "{\"version\":1,\"entries\":[{\"id\":\"a\",\"title\":\"A\",\"kid\":\"" + longKid + "\",\"engineer\":\"" + new string('y', 80) + "\",\"sources\":[\"RocketEq.DeltaV\"]}]}"));
        }

        static string Entry(string id) =>
            "{\"id\":\"" + id + "\",\"title\":\"T\",\"kid\":\"Short.\",\"engineer\":\"" + new string('y', 80) + "\",\"sources\":[\"RocketEq.DeltaV\"]}";
    }
}

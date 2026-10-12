// The Codex is data the game can show. These checks keep the eight lessons, the kid
// length, and the sim numbers from drifting into flavor or a real company name.
using System;
using System.Collections.Generic;
using Kestrel.Core.Codex;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class CodexTests
    {
        static readonly string[] Ids = { "delta-v", "twr", "isp", "staging", "max-q", "cg-cp", "orbits", "booster-landing" };
        static readonly string[] Banned = {
            "Blue Origin", "New Glenn", "Jacklyn", "LC-36", "SpaceX", "NASA", "Boeing", "Lockheed",
            "Ariane", "Soyuz", "Falcon", "Starship", "Saturn", "Apollo", "suicide", "RP-1", "\u2014", "\u2013"
        };

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
                string text = e.Title + "\n" + e.Kid + "\n" + e.Engineer;
                foreach (var ban in Banned) Assert.IsFalse(text.Contains(ban), e.Id + " contains " + ban);
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

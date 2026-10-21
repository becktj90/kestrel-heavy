// dotnet-only: keeps Kestrel.UiChecks/UiClipRules (the USS overflow rules UiTreeChecks uses in place of a computed
// overflow, which Unity 6.3 does not expose) in sync with every .uss under unity/Assets, in both directions.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kestrel.Core.Data;
using Kestrel.UiChecks;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class UiClipRulesUssTests
    {
        static string Assets()
        {
            var root = Catalog.FindRepoRoot(TestContext.CurrentContext.TestDirectory);
            return Path.Combine(root, "unity", "Assets");
        }

        static void Scan(out HashSet<string> hidden, out HashSet<string> visible)
        {
            hidden = new HashSet<string>(StringComparer.Ordinal);
            visible = new HashSet<string>(StringComparer.Ordinal);
            var files = Directory.GetFiles(Assets(), "*.uss", SearchOption.AllDirectories);
            Assert.That(files.Length, Is.GreaterThan(0), "no .uss found");
            foreach (var f in files)
            {
                string s = Regex.Replace(File.ReadAllText(f), @"/\*.*?\*/", "", RegexOptions.Singleline);
                foreach (Match m in Regex.Matches(s, @"([^{}]+)\{([^}]*)\}"))
                {
                    foreach (var decl in m.Groups[2].Value.Split(';'))
                    {
                        int c = decl.IndexOf(':');
                        if (c < 0 || decl.Substring(0, c).Trim() != "overflow") continue;
                        string v = decl.Substring(c + 1).Trim();
                        var set = v == "hidden" ? hidden : v == "visible" ? visible : null;
                        Assert.IsNotNull(set, Path.GetFileName(f) + ": overflow value '" + v + "' is not modelled by UiClipRules");
                        foreach (var sel in m.Groups[1].Value.Split(','))
                            set.Add(Regex.Replace(sel.Trim(), @"\s+", " "));
                    }
                }
            }
        }

        [Test]
        public void EveryUssOverflowRuleIsMirroredAndNothingStale()
        {
            Scan(out var hidden, out var visible);
            hidden.Add("." + UiClipRules.ScrollViewport);   // Unity's default theme, not in the repo
            var listedHidden = new HashSet<string>(UiClipRules.HiddenSelectors);
            var listedVisible = new HashSet<string>(UiClipRules.VisibleSelectors);
            Assert.That(hidden.Except(listedHidden), Is.Empty, "overflow: hidden in .uss but missing from UiClipRules.HiddenSelectors");
            Assert.That(listedHidden.Except(hidden), Is.Empty, "UiClipRules.HiddenSelectors has rules no .uss declares (stale rules hide overlaps)");
            Assert.That(visible.Except(listedVisible), Is.Empty, "overflow: visible in .uss but missing from UiClipRules.VisibleSelectors");
            Assert.That(listedVisible.Except(visible), Is.Empty, "UiClipRules.VisibleSelectors has stale rules");
        }

        [Test]
        public void EverySelectorParses()
        {
            foreach (var s in UiClipRules.HiddenSelectors.Concat(UiClipRules.VisibleSelectors))
                Assert.IsTrue(UiClipRules.TryParse(s, out _), "UiClipRules cannot model selector '" + s + "'");
            Assert.AreEqual(UiClipRules.HiddenSelectors.Length, UiClipRules.Hidden.Count);
            Assert.AreEqual(UiClipRules.VisibleSelectors.Length, UiClipRules.Visible.Count);
        }

        sealed class Node
        {
            public Node Parent; public string Name; public string[] Classes = new string[0];
        }

        static bool Match(string sel, Node n)
        {
            Assert.IsTrue(UiClipRules.TryParse(sel, out var c));
            return UiClipRules.Matches(c, n, x => x.Parent, x => x.Name, (x, k) => x.Classes.Contains(k));
        }

        [Test]
        public void DescendantAndCompoundSelectorsMatchLikeUss()
        {
            var wide = new Node { Classes = new[] { "kh-mm-wide" } };
            var mid = new Node { Parent = wide, Classes = new[] { "kh-mm-grid" } };
            var tile = new Node { Parent = mid, Classes = new[] { "kh-button", "kh-mm-tile" } };
            var bare = new Node { Classes = new[] { "kh-mm-tile" } };
            var rocket = new Node { Name = "stepper-rocket" };
            Assert.IsTrue(Match(".kh-mm-wide .kh-button.kh-mm-tile", tile));
            Assert.IsFalse(Match(".kh-mm-wide .kh-button.kh-mm-tile", bare), "needs both classes");
            Assert.IsFalse(Match(".kh-mm-wide .kh-button.kh-mm-tile", new Node { Classes = tile.Classes }), "needs the ancestor");
            Assert.IsFalse(Match(".kh-mm-wide .kh-mm-thrum", wide), "the ancestor itself is not a descendant");
            Assert.IsTrue(Match("#stepper-rocket", rocket));
            Assert.IsFalse(Match("#stepper-rocket", bare));
            Assert.IsFalse(UiClipRules.TryParse(".a > .b", out _));
            Assert.IsFalse(UiClipRules.TryParse("Label", out _));
        }
    }
}

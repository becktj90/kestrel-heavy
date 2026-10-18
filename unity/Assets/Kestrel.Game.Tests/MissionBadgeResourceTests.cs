// Each baked KH badge is a Resources texture. An empty KESTREL_REPO_ROOT must not
// matter: the player build has no checkout, and these loads do not walk up to one.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    public class MissionBadgeResourceTests
    {
        string _prev;
        string _away;

        [SetUp]
        public void EmptyCheckout()
        {
            _away = Path.Combine(Path.GetTempPath(), "kh-badges-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_away);
            _prev = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", _away);
        }

        [TearDown]
        public void RestoreCheckout()
        {
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", _prev);
            MissionPatch.DiscardTextureCache();
            try { if (!string.IsNullOrEmpty(_away) && Directory.Exists(_away)) Directory.Delete(_away, true); }
            catch (Exception) { }
        }

        [Test]
        public void EachCampaignBadgeLoadsFromResources()
        {
            Assert.That(Catalog.RepoRootOrNull(), Is.Null);
            var seen = new HashSet<int>();
            for (int i = 1; i <= MissionPatchArt.CampaignCount; i++)
            {
                string path = "MissionBadges/kh-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var tex = Resources.Load<Texture2D>(path);
                Assert.That(tex, Is.Not.Null, path);
                Assert.That(tex.width, Is.EqualTo(MissionPatchArt.BakedEdge), path);
                Assert.That(tex.height, Is.EqualTo(MissionPatchArt.BakedEdge), path);
                Assert.That(tex.isReadable, Is.True, path);
                Assert.That(tex.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                Assert.That(tex.mipmapCount, Is.EqualTo(1), path);
                var pix = tex.GetPixels32();
                Assert.That(pix, Is.Not.Null, path);
                Assert.That(pix.Length, Is.EqualTo(tex.width * tex.height), path);
                int opaque = 0;
                int minSum = 255 * 3;
                int maxSum = 0;
                int hash = 17;
                for (int p = 0; p < pix.Length; p += 8)
                {
                    var c = pix[p];
                    hash = unchecked(hash * 31 + c.r + c.g * 3 + c.b * 5 + c.a * 7);
                    if (c.a < 16) continue;
                    opaque++;
                    int sum = c.r + c.g + c.b;
                    if (sum < minSum) minSum = sum;
                    if (sum > maxSum) maxSum = sum;
                }
                Assert.That(opaque, Is.GreaterThan(1000), path);
                Assert.That(maxSum - minSum, Is.GreaterThan(24), path);
                Assert.That(seen.Add(hash), Is.True, path);
            }

            Assert.DoesNotThrow(() => MissionPatch.Render("m1", 76f, PatchChrome.Open));
            Assert.DoesNotThrow(() => MissionPatch.Render("KH-7", 76f, PatchChrome.Medal));
            Assert.DoesNotThrow(() => MissionPatch.Render("m5", 76f, PatchChrome.Locked));
            Assert.DoesNotThrow(() => MissionPatch.Render("free", 76f, PatchChrome.Open));
            Assert.DoesNotThrow(() => MissionPatch.Render("kc-pier-7", 76f, PatchChrome.Open));
        }
    }
}

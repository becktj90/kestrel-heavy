using System.Collections.Generic;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public static class JsonSame
    {
        public static void Equal(object actual, object expected, string what)
        {
            if (expected == null) { Assert.IsNull(actual, what); return; }
            if (expected is double ed)
            {
                Assert.IsInstanceOf<double>(actual, what);
                double ad = (double)actual;
                if (double.IsNaN(ed)) { Assert.IsTrue(double.IsNaN(ad), what); return; }
                if (double.IsInfinity(ed)) { Assert.AreEqual(ed, ad, what); return; }
                if (ad != ed) Assert.Fail($"{what}: got {ad:R}, expected {ed:R}");
                return;
            }
            if (expected is string es) { Assert.AreEqual(es, actual as string, what); return; }
            if (expected is bool eb) { Assert.AreEqual(eb, (bool)actual, what); return; }
            if (expected is JsonObject eo)
            {
                var ao = actual as JsonObject;
                Assert.IsNotNull(ao, what);
                Assert.AreEqual(eo.Count, ao.Count, what + " key count");
                foreach (var k in eo.Order)
                {
                    Assert.IsTrue(ao.ContainsKey(k), what + " missing " + k);
                    Equal(ao[k], eo[k], what + "." + k);
                }
                return;
            }
            if (expected is List<object> ea)
            {
                var aa = actual as List<object>;
                Assert.IsNotNull(aa, what);
                Assert.AreEqual(ea.Count, aa.Count, what + " length");
                for (int i = 0; i < ea.Count; i++) Equal(aa[i], ea[i], what + "[" + i + "]");
                return;
            }
            Assert.Fail(what + " unexpected " + expected.GetType().Name);
        }
    }
}

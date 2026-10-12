using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class QuickRetryGoldenTests
    {
        [Test]
        public void ResumeClocks_MatchPace()
        {
            var retry = QuickRetry.Load(Golden.Root);
            Assert.AreEqual(1, retry.Taps);
            Assert.IsFalse(retry.Confirm);
            Assert.AreEqual(2, retry.ResultsToRelaunchSec);
            Assert.IsTrue(retry.PreserveUnlocks);
            Assert.IsTrue(retry.PreserveBests);
            int n = 0;
            foreach (var c in Golden.Cases("quick-retry"))
            {
                var plan = retry.TryPlan(c.Str("point"), c.Str("status"));
                if (c["out"] == null)
                {
                    Assert.IsNull(plan, c.Str("point") + " " + c.Str("status"));
                }
                else
                {
                    var o = (JsonObject)c["out"];
                    Assert.IsNotNull(plan);
                    Assert.AreEqual(o.Str("phase"), plan.Phase);
                    Assert.AreEqual(o.Num("tClock"), plan.TClock);
                    Assert.AreEqual(o.Num("taps"), plan.Taps);
                    Assert.AreEqual(o.Bool("confirm"), plan.Confirm);
                    Assert.AreEqual(o.Num("resultsToRelaunchSec"), plan.ResultsToRelaunchSec);
                    Assert.AreEqual(o.Bool("preserveUnlocks"), plan.PreserveUnlocks);
                    Assert.AreEqual(o.Bool("preserveBests"), plan.PreserveBests);
                    if (o["altitudeKm"] == null) Assert.IsNull(plan.AltitudeKm);
                    else Assert.AreEqual(o.Num("altitudeKm"), plan.AltitudeKm);
                }
                n++;
            }
            Assert.AreEqual(5, n);
        }
    }
}

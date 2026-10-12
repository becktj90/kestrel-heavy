// dotnet-only: prints the worst relative error per golden file after the run.
using NUnit.Framework;
namespace Kestrel.Core.Tests
{
    [SetUpFixture]
    public class ReportFixture
    {
        [OneTimeTearDown]
        public void Report()
        {
            TestContext.Progress.WriteLine($"[golden] compared {Golden.Compared} values");
            foreach (var kv in Golden.MaxRel) TestContext.Progress.WriteLine($"[golden] max rel err {kv.Key}: {kv.Value:E3}");
            if (Golden.MaxRel.Count == 0) TestContext.Progress.WriteLine("[golden] all compared values bit-identical");
        }
    }
}

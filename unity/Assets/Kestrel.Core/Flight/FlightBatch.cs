// Batch of headless ascents. The integrator allocates classes and lists, so it is not a Burst job.
// Altitude sampling (Atmosphere.SampleDensity) is the Burst-shaped path; this pool is the managed one.
using System.Collections.Generic;
using System.Threading.Tasks;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public static class FlightBatch
    {
        public const int ParallelThreshold = 8;

        public static SimResult[] Simulate(IList<SimStage[]> stages, IList<SimOptions> opts)
        {
            var r = new SimResult[stages.Count];
            if (stages.Count >= ParallelThreshold)
                Parallel.For(0, stages.Count, i => r[i] = Headless.Simulate(stages[i], opts[i]));
            else
                for (int i = 0; i < stages.Count; i++) r[i] = Headless.Simulate(stages[i], opts[i]);
            return r;
        }
    }
}

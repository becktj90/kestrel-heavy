// Burst sampling of the ISA density. Atmosphere.At uses stack locals, so this job can compile.
// The ascent integrator is not a Burst target: it builds class lists per step.
using Kestrel.Core.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Kestrel.Core.BurstCompile
{
    [BurstCompile]
    public struct AtmosphereSampleJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<double> altitudeM;
        public NativeArray<double> rho;

        public void Execute(int index)
        {
            rho[index] = Atmosphere.At(altitudeM[index]).rho;
        }
    }
}

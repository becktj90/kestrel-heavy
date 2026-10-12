// PHYSICS_CORE_AUDIT.md §P8c category 6 — Weather/LCC.
// Executable goldens live in WeatherTests (seed→day, climatology, LCC truth table, ensemble).
// Reference data (PD): NOAA NCEI–class monthly normals for Haven (LICENSES.md).
using NUnit.Framework;

namespace Kestrel.Core.Tests.Validation
{
    public class WeatherLccValidationTests
    {
        [Test]
        public void CoveredByWeatherTests()
        {
            Assert.Pass("see Kestrel.Core.Tests.WeatherTests — docs/PHYSICS.md § Weather (P4)");
        }
    }
}

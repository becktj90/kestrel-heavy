using NUnit.Framework;

namespace Kestrel.Game.Audio.Tests
{
    public class EngineMixTests
    {
        [Test]
        public void ExternalAirIsFullBelow30KmAndSilentAt80Km()
        {
            Assert.AreEqual(1f, EngineMix.ExternalGain(0f));
            Assert.AreEqual(1f, EngineMix.ExternalGain(EngineMix.AirFadeStartM));
            Assert.Greater(EngineMix.ExternalGain(55000f), 0f);
            Assert.Less(EngineMix.ExternalGain(55000f), 1f);
            Assert.AreEqual(0f, EngineMix.ExternalGain(EngineMix.VacuumAltM));
            Assert.AreEqual(0f, EngineMix.ExternalGain(100000f));
        }

        [Test]
        public void VacuumKeepsThrottleRumbleAndDropsTheAirBed()
        {
            float sea = EngineMix.Volume(0f, EngineMix.QRefPa, 1f);
            float vac = EngineMix.Volume(90000f, 0f, 1f);
            Assert.Greater(vac, 0f, "structure-borne rumble stays while throttled");
            Assert.Less(vac, sea, "the air layer is gone above 80 km");
            Assert.AreEqual(0f, EngineMix.Volume(90000f, 40000f, 0f));
            Assert.AreEqual(0f, EngineMix.AirBed(90000f, EngineMix.QRefPa));
            Assert.Greater(EngineMix.AirBed(0f, EngineMix.QRefPa), 0f);
        }

        [Test]
        public void PitchFollowsThrottleAndIgnoresQInVacuum()
        {
            Assert.Greater(EngineMix.Pitch(0f, 0f, 1f), EngineMix.Pitch(0f, 0f, 0.2f));
            Assert.AreEqual(EngineMix.Pitch(90000f, 40000f, 1f), EngineMix.Pitch(90000f, 0f, 1f), 1e-5f);
            Assert.Greater(EngineMix.Pitch(0f, EngineMix.QRefPa, 1f), EngineMix.Pitch(90000f, EngineMix.QRefPa, 1f));
        }

        [Test]
        public void StructureMatchesTheWebBedCurve()
        {
            Assert.AreEqual(0f, EngineMix.Structure(0.02f));
            Assert.AreEqual(0.14f + 0.28f, EngineMix.Structure(1f), 1e-5f);
            Assert.AreEqual(0.14f + 0.5f * 0.28f, EngineMix.Structure(0.5f), 1e-5f);
        }

        [Test]
        public void MuteZerosBusesAndVoiceDuckScalesThem()
        {
            Assert.AreEqual(0f, MixerLevels.Bus(true, MixerLevels.Engine, 1f));
            Assert.AreEqual(MixerLevels.Master, MixerLevels.Bus(false, MixerLevels.Sfx, 1f), 1e-5f);
            Assert.AreEqual(MixerLevels.Master * MixerLevels.VoiceDuck, MixerLevels.Bus(false, MixerLevels.Engine, MixerLevels.VoiceDuck), 1e-5f);
            Assert.AreEqual(0.72f * 0.85f, MixerLevels.Master * MixerLevels.Music, 1e-5f);
        }
    }
}

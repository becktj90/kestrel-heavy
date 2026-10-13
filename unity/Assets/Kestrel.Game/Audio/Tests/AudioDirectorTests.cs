using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Audio.Tests
{
    public class AudioDirectorTests
    {
        readonly List<GameObject> gos = new List<GameObject>();
        int priorMute;

        [SetUp]
        public void SetUp() => priorMute = PlayerPrefs.GetInt(AudioDirector.MutePref, 0);

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < gos.Count; i++)
                if (gos[i] != null) Object.DestroyImmediate(gos[i]);
            PlayerPrefs.SetInt(AudioDirector.MutePref, priorMute);
        }

        GameObject NewGo(string name)
        {
            var go = new GameObject(name);
            gos.Add(go);
            return go;
        }

        [Test]
        public void AttachIsTheOnlyHookAndIsIdempotent()
        {
            var go = NewGo("flight");
            var flight = go.AddComponent<Kestrel.Slice.FlightDriver>();
            var director = AudioDirector.Attach(flight);
            Assert.AreSame(director, flight.Sink);
            Assert.AreSame(director, AudioDirector.Attach(flight));
            Assert.AreEqual(1, go.GetComponents<AudioDirector>().Length);
            Assert.AreEqual(1, go.GetComponents<NarrationPlayer>().Length);
        }

        [Test]
        public void VacuumRumbleSurvivesAndMuteSilencesTheLoop()
        {
            var director = NewGo("audio").AddComponent<AudioDirector>();
            director.SetMuted(false);
            director.SetFlightState(0f, EngineMix.QRefPa, 1f);
            float sea = director.EngineVolume;
            director.SetFlightState(EngineMix.VacuumAltM + 1000f, 0f, 1f);
            Assert.Greater(director.EngineVolume, 0f);
            Assert.Less(director.EngineVolume, sea);
            Assert.AreEqual(EngineMix.Pitch(90000f, 0f, 1f), director.EnginePitch, 1e-4f);
            director.SetFlightState(90000f, 0f, 0f);
            Assert.AreEqual(0f, director.EngineVolume, 1e-5f);
            director.SetMuted(true);
            director.SetFlightState(0f, EngineMix.QRefPa, 1f);
            Assert.AreEqual(0f, director.EngineVolume, 1e-5f);
            Assert.IsTrue(director.Muted);
        }

        [Test]
        public void VoiceDuckScalesTheEngineLoop()
        {
            var director = NewGo("audio").AddComponent<AudioDirector>();
            director.SetMuted(false);
            director.SetFlightState(0f, EngineMix.QRefPa, 1f);
            float loud = director.EngineVolume;
            director.SetVoiceDuck(true);
            Assert.AreEqual(loud * MixerLevels.VoiceDuck, director.EngineVolume, 1e-4f);
            director.SetVoiceDuck(false);
            Assert.AreEqual(loud, director.EngineVolume, 1e-4f);
        }

        [Test]
        public void MaxQFiresOnceOnTheWayDownAndVacuumDropsAirOneShots()
        {
            var director = NewGo("audio").AddComponent<AudioDirector>();
            director.SetMuted(false);
            director.OnEvent("flight.begin");
            director.SetFlightState(10000f, 20000f, 1f);
            director.SetFlightState(11000f, 30000f, 1f);
            director.SetFlightState(12000f, 29700f, 1f);
            Assert.AreEqual(0, director.Accepted, "a 1% dip is not max-Q");
            director.SetFlightState(13000f, 29000f, 1f);
            Assert.AreEqual(1, director.Accepted);
            director.SetFlightState(14000f, 20000f, 1f);
            Assert.AreEqual(1, director.Accepted, "max-Q is one-shot");

            director.OnEvent("flight.begin");
            director.SetFlightState(90000f, 0f, 0f);
            int n = director.Accepted;
            director.OnEvent("whoosh");
            Assert.AreEqual(n, director.Accepted, "external air is silent above 80 km");
            director.SetFlightState(0f, 0f, 0f);
            director.OnEvent("whoosh");
            Assert.AreEqual(n + 1, director.Accepted);
            director.OnEvent("whoosh");
            Assert.AreEqual(n + 1, director.Accepted, "retrigger lock");
            director.OnEvent("not-a-cue");
            Assert.AreEqual(n + 1, director.Accepted);
        }

        [Test]
        public void UiClickIsACueAndMuteBlocksIt()
        {
            var director = NewGo("audio").AddComponent<AudioDirector>();
            director.SetMuted(true);
            director.OnEvent("ui");
            Assert.AreEqual(0, director.Accepted);
            director.SetMuted(false);
            director.OnEvent("ui");
            Assert.AreEqual(1, director.Accepted);
        }
    }
}

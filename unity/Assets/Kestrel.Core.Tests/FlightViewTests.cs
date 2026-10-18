// One saved camera, and the button that cycles it, sized for a 335 pt portrait.
using System;
using System.IO;
using Kestrel.Core.Flight;
using Kestrel.Core.Save;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FlightViewTests
    {
        [Test]
        public void CycleAndParseRoundTrip()
        {
            Assert.AreEqual(FlightViewMode.Chase, FlightView.Next(FlightViewMode.Auto));
            Assert.AreEqual(FlightViewMode.Above, FlightView.Next(FlightViewMode.Chase));
            Assert.AreEqual(FlightViewMode.Below, FlightView.Next(FlightViewMode.Above));
            Assert.AreEqual(FlightViewMode.Pad, FlightView.Next(FlightViewMode.Below));
            Assert.AreEqual(FlightViewMode.Auto, FlightView.Next(FlightViewMode.Pad));
            Assert.AreEqual(FlightViewMode.Above, FlightView.Parse(" ABOVE "));
            Assert.AreEqual(FlightViewMode.Auto, FlightView.Parse(null));
            Assert.AreEqual(FlightViewMode.Pad, FlightView.FromData(FlightView.Data(FlightViewMode.Pad)));
            Assert.AreEqual("pad", FlightView.Data(FlightViewMode.Pad).Str("camera"));
        }

        [Test]
        public void OffsetsPutPadOnTheGroundAndAutoOnTheChase()
        {
            var auto = FlightView.Of(FlightViewMode.Auto, 70);
            Assert.IsTrue(auto.UseChase);
            Assert.IsTrue(auto.OnVehicle);
            var chase = FlightView.Of(FlightViewMode.Chase, 70);
            Assert.IsTrue(chase.OnVehicle);
            Assert.IsFalse(chase.UseChase);
            Assert.That(chase.Z, Is.LessThan(0));
            var above = FlightView.Of(FlightViewMode.Above, 70);
            Assert.That(above.Y, Is.GreaterThan(70));
            var below = FlightView.Of(FlightViewMode.Below, 70);
            Assert.That(below.Y, Is.LessThan(0));
            var pad = FlightView.Of(FlightViewMode.Pad, 70);
            Assert.IsFalse(pad.OnVehicle);
            Assert.IsFalse(pad.UseChase);
            Assert.That(pad.Z, Is.LessThan(0));
            Assert.That(pad.Y, Is.GreaterThan(0));
        }

        [Test]
        public void CameraRowAndCalloutFitANarrowPortrait()
        {
            Assert.IsTrue(FlightView.FitsWidth(335f));
            Assert.IsTrue(FlightView.FitsWidth(375f));
            for (int i = 0; i < FlightView.Cycle.Length; i++)
            {
                var mode = FlightView.Cycle[i];
                string name = FlightView.Name(mode);
                float text = FlightView.LabelWidth(name, FlightView.ButtonFontPt);
                float button = FlightView.ButtonWidth(mode);
                Assert.That(text + FlightView.ButtonPadPt, Is.LessThanOrEqualTo(button + 0.05f), name);
                Assert.That(button, Is.GreaterThanOrEqualTo(FlightView.MinButtonPt));
                Assert.That(name.Length, Is.LessThan(12), name);
            }
            string line = FlightStaging.Format(FlightStaging.Title(StagingKind.FairingSep), 999.9, 999900, 9990);
            float width = FlightHudLayout.TextWidth(line, FlightHudLayout.LabelFontPt);
            Assert.That(width, Is.LessThanOrEqualTo(335f - 2f * FlightHudLayout.SideMarginPt), line);
        }

        [Test]
        public void SettingsRecordRoundTripsTheCamera()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-cam-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var store = new NativeStore(dir, () => 1, () => 0.5);
                Assert.IsNull(store.GetRecord(FlightView.RecordKey));
                store.PutRecord(FlightView.RecordKey, "settings", FlightView.Data(FlightViewMode.Below));
                Assert.AreEqual(FlightViewMode.Below, FlightView.FromData(store.GetRecord(FlightView.RecordKey)));
                store.PutRecord(FlightView.RecordKey, "settings", FlightView.Data(FlightViewMode.Pad));
                Assert.AreEqual(FlightViewMode.Pad, FlightView.FromData(store.GetRecord(FlightView.RecordKey)));
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch (Exception) { }
            }
        }
    }
}

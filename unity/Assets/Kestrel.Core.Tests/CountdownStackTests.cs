// The countdown's bottom stack and COUNT tag (TestFlight 202, #332): one card, a readable spare line, a status that
// is not a button, a 44 pt Skip count, a Follow chip that reads in both states, a tag that fits its own box.
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class CountdownStackTests
    {
        // Portrait content widths (points): the SE, 390 and 430 classes less the 8 pt side margins, and the 320 floor.
        static readonly float[] Widths = { 320f - 16f, 375f - 16f, 390f - 16f, 430f - 16f };
        static readonly string[] Spares =
        {
            "2,325 m/s to spare for Tiny Orbit", "120 m/s short of Tiny Orbit", "8,400 m/s to spare for the Geostationary Transfer",
        };
        static readonly string[] Statuses = { "Deluge", "Propellant load", "Range is go", "Terminal count" };

        [Test]
        public void OneCardFewerRowsAndEveryLineFits()
        {
            foreach (var w in Widths)
            foreach (var spare in Spares)
            foreach (var status in Statuses)
            {
                string where = w + " pt, " + spare + " / " + status;
                var fit = CountdownStack.Fit(w, "On the count", spare, status, true);
                float room = w - 2f * CountdownStack.CardPadXPt;
                // 202 had four blocks (banner, loose spare line, status banner, skip bar). Now three rows in one card on
                // every phone Trevor flies (SE 375, 390, 430); the 320 pt floor may stack a long status over Skip.
                bool phone = w >= 375f - 16f;
                Assert.That(fit.Rows, Is.LessThanOrEqualTo(phone ? 3 : 4), where);
                if (phone) Assert.That(fit.OneRow, Is.True, where + " status and Skip share a row");
                Assert.That(fit.SparePt, Is.GreaterThanOrEqualTo(CountdownStack.SpareMinPt), where + " spare face");
                Assert.That(FlightHudLayout.WrapLines(spare, room - 2f, fit.SparePt), Is.LessThanOrEqualTo(fit.SpareLines), where + " spare never cut");
                Assert.That(fit.SpareLines, Is.LessThanOrEqualTo(2), where);
                if (fit.OneRow) Assert.That(fit.StatusW + CountdownStack.RowGapPt + fit.SkipW, Is.LessThanOrEqualTo(room + 1e-3f), where + " row fits");
                else Assert.That(System.Math.Max(fit.StatusW, fit.SkipW), Is.LessThanOrEqualTo(room + 1e-3f), where + " rows fit");
                Assert.That(FlightHudLayout.BudgetWidth(status, CountdownStack.StatusPt), Is.LessThanOrEqualTo(fit.StatusW - 2f * CountdownStack.StatusPadXPt - CountdownStack.StatusDotPt - CountdownStack.StatusDotGapPt), where + " status text in its pill");
                Assert.That(fit.CardH, Is.EqualTo((float)System.Math.Ceiling(fit.CardH)), where + " whole points");
                Assert.That(fit.CardH, Is.LessThanOrEqualTo(phone ? 140f : 180f), where + " the card stays a strip, not a panel");
            }
        }

        [Test]
        public void SkipCountIsAFullHitTarget()
        {
            Assert.That(CountdownStack.SkipPt, Is.GreaterThanOrEqualTo(44f));
            Assert.That(CountdownStack.SkipWidth(), Is.GreaterThanOrEqualTo(44f));
            Assert.That(FlightHudLayout.BudgetWidth(CountdownStack.SkipText, CountdownStack.SkipFontPt),
                Is.LessThanOrEqualTo(CountdownStack.SkipWidth() - 2f * CountdownStack.SkipPadXPt), "label inside the button");
        }

        [Test]
        public void SpareLineReadsOnTheCard()
        {
            // Cream on the ink card, not cream on the sky (202: about 1.3:1 over the pale sky).
            var ink = CountdownStack.FollowingPlate;
            var cream = CountdownStack.FollowingText;
            foreach (var sky in CountdownStack.Backdrops)
                Assert.That(CountdownStack.Contrast(cream, ink.Over(sky)), Is.GreaterThanOrEqualTo(7.0), "AAA on the card");
            Assert.That(CountdownStack.Contrast(cream, CountdownStack.Rgba.Bytes(196, 214, 240)), Is.LessThan(1.6), "the old line on the sky");
        }

        [Test]
        public void FollowChipReadsInBothStates()
        {
            Assert.That(CountdownStack.WorstContrast(CountdownStack.FollowingPlate, CountdownStack.FollowingText), Is.GreaterThanOrEqualTo(4.5), "Following");
            Assert.That(CountdownStack.WorstContrast(CountdownStack.FollowPlate, CountdownStack.FollowText), Is.GreaterThanOrEqualTo(4.5), "Follow rocket");
            // The 202 Following chip: white at 12% under white text.
            var old = new CountdownStack.Rgba(1f, 1f, 1f, 0.12f);
            Assert.That(CountdownStack.WorstContrast(old, new CountdownStack.Rgba(1f, 1f, 1f)), Is.LessThan(1.5), "the old chip");
            // The two states are told apart by more than the label: plate luminance differs a lot.
            Assert.That(CountdownStack.Contrast(CountdownStack.FollowingPlate.Over(CountdownStack.Backdrops[0]), CountdownStack.FollowPlate.Over(CountdownStack.Backdrops[0])), Is.GreaterThan(3.0));
        }

        [Test]
        public void CountTagFitsItsBoxBesideTheClock()
        {
            float w = CountdownStack.TagWidth("COUNT");
            Assert.That(w, Is.EqualTo((float)System.Math.Ceiling(w)));
            Assert.That(FlightHudLayout.BudgetWidth("COUNT", CountdownStack.TagPt), Is.LessThanOrEqualTo(w - 2f * CountdownStack.TagPadXPt + 1e-3f));
            Assert.That(CountdownStack.TagPt * FlightHudLayout.LineHeight, Is.LessThanOrEqualTo(CountdownStack.TagH), "one line in 24 pt");
            // Clock (at its widest sample) + tag stays left of the 64 pt guide pop with 8 pt margins on the SE.
            float clockW = FlightHudLayout.ClockMinWidth();
            Assert.That(CountdownStack.TagRight(clockW, "COUNT"), Is.LessThanOrEqualTo(375f - 16f - 64f - 8f), "clear of the narrator portrait");
        }

        [Test]
        public void FollowTogglesAndComesBackToThePlayersView()
        {
            // #332: "tapping Follow does nothing". Following (with the pad camera in range, as on the count), a tap lets
            // go to the ground camera; the next tap chases again. Out of range: FlightCameraControlTests.
            FlightViewMode Tap(FlightViewMode view, FlightViewMode last) => FlightCameraControl.Follow(view, false, last, true).View;
            Assert.That(Tap(FlightViewMode.Chase, FlightViewMode.Chase), Is.EqualTo(FlightViewMode.Pad));
            Assert.That(Tap(FlightViewMode.Pad, FlightViewMode.Chase), Is.EqualTo(FlightViewMode.Chase));
            Assert.That(Tap(FlightViewMode.Pad, FlightViewMode.Pad), Is.EqualTo(FlightViewMode.Auto));
            foreach (var mode in new[] { FlightViewMode.Auto, FlightViewMode.Above, FlightViewMode.Below, FlightViewMode.Chase })
            {
                var off = Tap(mode, mode);
                Assert.That(FlightView.Following(true, off), Is.False, mode + " lets go");
                Assert.That(FlightView.FollowCaption(FlightView.Following(true, off)), Is.Not.EqualTo(FlightView.FollowCaption(true)), mode + " label changes");
                Assert.That(Tap(off, mode), Is.EqualTo(mode), mode + " comes back");
            }
        }

        [Test]
        public void FlightNotesAreShort()
        {
            foreach (var line in AscentNotes.All)
            {
                Assert.That(line.Length, Is.LessThanOrEqualTo(AscentNotes.MaxChars), line);
                // Two lines at 13 pt in the 320 pt phone's sheet (content less the frame's 16 pt padding).
                Assert.That(FlightHudLayout.WrapLines(line, 320f - 16f - 16f - 2f, 13f), Is.LessThanOrEqualTo(2), line);
            }
        }
    }
}

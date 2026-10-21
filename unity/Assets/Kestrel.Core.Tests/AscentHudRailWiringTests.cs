// The HUD wiring the overlap grid relies on (#332 CI job 119437176077, 1280x800 notes open): the grid proves the rail is
// clear when it is seated by FlightHudLayout.SeatRail over the frame's stack, and the notes are sized by NotesSheetPt and,
// in landscape, sit in PlaceWide's box. These checks keep AscentHud on those same calls, in portrait and landscape, so
// the model and the HUD cannot drift apart again (landscape used to re-pin the notes as the portrait sheet, unrefitted).
using System.IO;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class AscentHudRailWiringTests
    {
        static string Source() => File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Slice", "Runtime", "Ascent", "AscentHud.cs"));

        /// <summary>The body of the first method whose declaration contains <paramref name="signature"/>.</summary>
        static string Body(string src, string signature)
        {
            int at = src.IndexOf(signature, System.StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThanOrEqualTo(0), signature + " is in AscentHud.cs");
            int open = src.IndexOf('{', at);
            int depth = 0;
            for (int i = open; i < src.Length; i++)
            {
                if (src[i] == '{') depth++;
                else if (src[i] == '}' && --depth == 0) return src.Substring(open, i - open + 1);
            }
            Assert.Fail("unbalanced " + signature);
            return "";
        }

        [Test]
        public void SeatPlotUsesTheGridsSeatInBothOrientations()
        {
            string seat = Body(Source(), "void SeatPlot()");
            StringAssert.Contains("FlightHudLayout.SeatRail(", seat, "the rail is seated by the call the grid models");
            StringAssert.Contains("StackElements()", seat);
            StringAssert.DoesNotContain("widePinned", seat, "no orientation skips the refit");
            string stack = Body(Source(), "IEnumerable<VisualElement> StackElements()");
            foreach (var e in new[] { "UiIds.FlightFollow", "UiIds.FlightCamera", "UiIds.FlightPause", "help", "quote", "bar", "nudgeRow", "actions" })
                StringAssert.Contains(e, stack, e + " is under the rail's watch");
        }

        [Test]
        public void LandscapeKeepsPlaceWidesNotesAndRefitsTheRail()
        {
            string src = Source();
            string wide = Body(src, "public void LayoutWideAt(");
            StringAssert.Contains("plotPinned = Sc(f.Plot)", wide, "landscape gives SeatPlot the frame's rail");
            StringAssert.Contains("PlaceRailOrDock(help, f.Help", wide, "the notes go where PlaceWide put them");
            StringAssert.Contains("PlaceInfoSheet();", wide, "and are dressed, then the rail seated, after the pins");
            StringAssert.Contains("WatchStack();", wide);
            string sheet = Body(src, "void PlaceInfoSheet()");
            int wideAt = sheet.IndexOf("if (widePinned)", System.StringComparison.Ordinal);
            Assert.That(wideAt, Is.GreaterThanOrEqualTo(0), "PlaceInfoSheet has a landscape branch");
            string wideBranch = Body(sheet.Substring(wideAt), "if (widePinned)");
            StringAssert.Contains("SeatPlot();", wideBranch);
            StringAssert.Contains("return;", wideBranch);
            foreach (var moved in new[] { "help.style.left", "help.style.right", "help.style.bottom", "help.style.top", "hud.Add(help)", "Position.Absolute" })
                StringAssert.DoesNotContain(moved, wideBranch, "landscape must not re-pin the notes (" + moved + ")");
            Assert.That(sheet.IndexOf("hud.Add(help)", System.StringComparison.Ordinal), Is.GreaterThan(wideAt), "the portrait pin comes after the landscape return");
            StringAssert.Contains("FlightHudLayout.NotesSheetPt(", sheet, "the portrait sheet is sized by the grid's formula");
            StringAssert.Contains("SeatPlot();", Body(sheet, "if (!infoOpen)"), "closing the notes refits the rail");
        }

        [Test]
        public void NotesOpenAndCloseAndEveryStackMoveRefitTheRail()
        {
            string src = Source();
            string info = Body(src, "void OnInfo()");
            StringAssert.Contains("LayoutWide();", info, "landscape re-pins for the notes mode");
            StringAssert.Contains("PlaceInfoSheet();", info);
            StringAssert.Contains("e.RegisterCallback<GeometryChangedEvent>(OnStackMoved)", Body(src, "void WatchStack()"));
            StringAssert.Contains("SeatPlot()", Body(src, "void OnStackMoved("));
            StringAssert.Contains("WatchStack();", Body(src, "public void Open("), "watched from the start");
            StringAssert.Contains("WatchStack();", Body(src, "public void Layout("), "and every element made since");
            StringAssert.Contains("UnwatchStack();", Body(src, "public void Close()"));
        }
    }
}

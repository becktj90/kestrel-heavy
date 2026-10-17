// The post-flight card is an opaque sheet that covers the designer stats on SE, Pro Max, and iPad.
using Kestrel.Slice;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class PostFlightCardTests
    {
        static readonly object[] Devices =
        {
            new object[] { "SE", 375.0, 667.0, 20.0, 0.0, 0.0, 0.0 },
            new object[] { "Pro Max", 430.0, 932.0, 59.0, 34.0, 0.0, 0.0 },
            new object[] { "iPad", 1024.0, 1366.0, 24.0, 20.0, 0.0, 0.0 },
        };

        [TestCaseSource(nameof(Devices))]
        public void OpaqueCardCoversTheDesignerStats(string name, double w, double h, double top, double bottom, double left, double right)
        {
            var frame = ReportLayout.Solve(w, h, top, bottom, left, right, false);
            Assert.That(ReportLayout.PanelAlpha, Is.EqualTo(1), name);
            Assert.That(frame.VisualBottom - frame.VisualTop, Is.GreaterThanOrEqualTo(h * 0.28), name);
            Assert.That(ReportLayout.CoversDesignerStats(frame, h), Is.True, name);
            Assert.That(frame.Card.Y, Is.LessThanOrEqualTo(h * ReportLayout.DesignerSheetTop + 0.6), name);
            Assert.That(frame.Card.X, Is.GreaterThanOrEqualTo(left), name);
            Assert.That(frame.Card.Right, Is.LessThanOrEqualTo(w - right + 0.01), name);
            Assert.That(frame.Card.Bottom, Is.LessThanOrEqualTo(h - bottom + 0.01), name);
            Assert.That(frame.Fix.H, Is.GreaterThanOrEqualTo(ReportLayout.MinTargetPt), name);
        }
    }
}

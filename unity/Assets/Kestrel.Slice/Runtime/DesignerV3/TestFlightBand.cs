// Test Flight (DECISIONS #100): the designer's header band in a test. The "TEST FLIGHT" eyebrow and the one-line rules
// note, above the live numbers. LAUNCH keeps its own label (UX spec: no "TEST LAUNCH").
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice.DesignerV3
{
    public static class TestFlightBand
    {
        /// <summary>The band, or null when this designer is not a test flight.</summary>
        public static VisualElement For(HangarUi ui) => GameSession.IsTestFlight ? Build(ui) : null;

        public static VisualElement Build(HangarUi ui)
        {
            var band = ui.Column();
            band.name = UiIds.TestFlightBand;
            band.style.flexShrink = 0;
            band.style.width = Length.Percent(100);
            band.style.paddingLeft = 8; band.style.paddingRight = 8;
            band.style.paddingTop = 2; band.style.paddingBottom = 4;
            band.pickingMode = PickingMode.Ignore;
            var eye = ui.Text(TestFlightCopy.Eyebrow, HangarUi.SmallPt, HangarUi.Amber, bold: true);
            eye.name = UiIds.TestFlightEyebrow;
            eye.style.letterSpacing = 1.5f;
            eye.pickingMode = PickingMode.Ignore;
            var rules = ui.Text(TestFlightCopy.Rules, HangarUi.SmallPt, HangarUi.Dim);
            rules.name = UiIds.TestFlightRules;
            rules.style.whiteSpace = WhiteSpace.Normal;
            rules.pickingMode = PickingMode.Ignore;
            band.Add(eye);
            band.Add(rules);
            // Read in this order by VoiceOver: what the screen is, then what it costs.
            band.tooltip = TestFlightCopy.Name + ". " + TestFlightCopy.Rules;
            return band;
        }
    }
}

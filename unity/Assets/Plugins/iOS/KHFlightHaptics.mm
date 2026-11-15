// Kestrel Heavy flight haptics (DECISIONS #100): one Taptic impact per call, paced by Kestrel.Core.Feel.LiftoffHaptics.
// UIKit only (UIImpactFeedbackGenerator with iOS 13+ intensity), like KHHangarNative.mm: no new framework and no Xcode
// project change. Called from Kestrel.Slice.FlightHaptics. Styles match the C# HapticStyle enum:
// 0 light, 1 medium, 2 heavy, 3 soft, 4 rigid.
#import <UIKit/UIKit.h>

extern "C" {

static UIImpactFeedbackGenerator *KHFlightGenerators[5];

static UIImpactFeedbackGenerator *KHFlightGenerator(int style)
{
    if (style < 0 || style > 4) style = 0;
    if (KHFlightGenerators[style] == nil) {
        UIImpactFeedbackStyle s = UIImpactFeedbackStyleLight;
        switch (style) {
            case 1: s = UIImpactFeedbackStyleMedium; break;
            case 2: s = UIImpactFeedbackStyleHeavy; break;
            case 3: s = UIImpactFeedbackStyleSoft; break;
            case 4: s = UIImpactFeedbackStyleRigid; break;
            default: s = UIImpactFeedbackStyleLight; break;
        }
        KHFlightGenerators[style] = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
    }
    return KHFlightGenerators[style];
}

// Wake the Taptic Engine before the count's rumble so the first pulse is not late.
void KHFlightHapticsPrepare(void)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        for (int i = 0; i < 5; i++) [KHFlightGenerator(i) prepare];
    });
}

void KHFlightImpact(int style, float intensity)
{
    CGFloat k = MIN(1.0, MAX(0.0, (CGFloat)intensity));
    if (k <= 0.0) return;
    dispatch_async(dispatch_get_main_queue(), ^{
        UIImpactFeedbackGenerator *g = KHFlightGenerator(style);
        [g impactOccurredWithIntensity:k];
        [g prepare];
    });
}

}

// Kestrel Heavy Hangar: Taptic feedback, VoiceOver announcements and Reduce Motion (UIKit only).
// Called from Kestrel.Slice.HangarNative. Kinds match the C# Haptic enum: 1 snap, 2 payoff, 3 warning.
// A pattern is pulse, gap, pulse ... in milliseconds (designer-feel.json). Each pulse is one tap: the first uses the
// kind's generator (selection, success, warning); later pulses are impacts whose strength follows the pulse length.
#import <UIKit/UIKit.h>

extern "C" {

static void KHHangarTap(int kind, int pulseMs, BOOL first)
{
    if (first && kind == 1) {
        UISelectionFeedbackGenerator *g = [[UISelectionFeedbackGenerator alloc] init];
        [g selectionChanged];
    } else if (first && (kind == 2 || kind == 3)) {
        UINotificationFeedbackGenerator *g = [[UINotificationFeedbackGenerator alloc] init];
        [g notificationOccurred:(kind == 2 ? UINotificationFeedbackTypeSuccess : UINotificationFeedbackTypeWarning)];
    } else {
        UIImpactFeedbackGenerator *g = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
        CGFloat strength = MIN(1.0, MAX(0.2, pulseMs / 30.0));
        [g impactOccurredWithIntensity:strength];
    }
}

void KHHangarHaptic(int kind, const int *pattern, int count)
{
    if (pattern == NULL || count <= 0) return;
    double at = 0;
    for (int i = 0; i < count; i += 2) {
        int pulse = pattern[i] > 0 ? pattern[i] : 1;
        BOOL first = (i == 0);
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(at * NSEC_PER_MSEC)), dispatch_get_main_queue(), ^{
            KHHangarTap(kind, pulse, first);
        });
        at += pulse + (i + 1 < count && pattern[i + 1] > 0 ? pattern[i + 1] : 0);
    }
}

void KHHangarAnnounce(const char *text)
{
    if (text == NULL) return;
    NSString *s = [NSString stringWithUTF8String:text];
    if (s.length == 0) return;
    dispatch_async(dispatch_get_main_queue(), ^{
        if (UIAccessibilityIsVoiceOverRunning()) UIAccessibilityPostNotification(UIAccessibilityAnnouncementNotification, s);
    });
}

int KHHangarReduceMotion(void)
{
    return UIAccessibilityIsReduceMotionEnabled() ? 1 : 0;
}

}

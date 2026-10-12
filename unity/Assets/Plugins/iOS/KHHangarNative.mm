// Kestrel Heavy Hangar: Taptic feedback, VoiceOver announcements and Reduce Motion (UIKit only).
// Called from Kestrel.Slice.HangarNative. Kinds match the C# Haptic enum: 1 snap, 2 payoff, 3 warning.
#import <UIKit/UIKit.h>

extern "C" {

void KHHangarHaptic(int kind)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if (kind == 1) {
            UISelectionFeedbackGenerator *g = [[UISelectionFeedbackGenerator alloc] init];
            [g selectionChanged];
        } else if (kind == 2 || kind == 3) {
            UINotificationFeedbackGenerator *g = [[UINotificationFeedbackGenerator alloc] init];
            [g notificationOccurred:(kind == 2 ? UINotificationFeedbackTypeSuccess : UINotificationFeedbackTypeWarning)];
        }
    });
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

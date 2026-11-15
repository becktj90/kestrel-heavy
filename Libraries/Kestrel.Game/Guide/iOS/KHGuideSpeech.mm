// VoiceOver and the iOS system voice for the subtitle box. UIKit and AVFoundation only.
#import <AVFoundation/AVFoundation.h>
#import <UIKit/UIKit.h>

static AVSpeechSynthesizer *KHGuideSynth;

extern "C" {

void KHGuideAnnounce(const char *text)
{
    if (text == NULL) return;
    NSString *s = [NSString stringWithUTF8String:text];
    if (s.length == 0) return;
    dispatch_async(dispatch_get_main_queue(), ^{
        if (!UIAccessibilityIsVoiceOverRunning()) return;
        UIAccessibilityPostNotification(UIAccessibilityAnnouncementNotification, s);
    });
}

void KHGuideSpeak(const char *text)
{
    if (text == NULL) return;
    NSString *s = [NSString stringWithUTF8String:text];
    if (s.length == 0) return;
    dispatch_async(dispatch_get_main_queue(), ^{
        // VoiceOver is already reading the line. Don't talk over it.
        if (UIAccessibilityIsVoiceOverRunning()) return;
        if (KHGuideSynth == nil) KHGuideSynth = [[AVSpeechSynthesizer alloc] init];
        if (KHGuideSynth.isSpeaking) [KHGuideSynth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
        AVSpeechUtterance *u = [AVSpeechUtterance speechUtteranceWithString:s];
        u.rate = AVSpeechUtteranceDefaultSpeechRate;
        [KHGuideSynth speakUtterance:u];
    });
}

void KHGuideStopSpeech(void)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if (KHGuideSynth != nil && KHGuideSynth.isSpeaking)
            [KHGuideSynth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
    });
}

int KHGuideReduceMotion(void)
{
    return UIAccessibilityIsReduceMotionEnabled() ? 1 : 0;
}

}

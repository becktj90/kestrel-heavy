// Nav-harness ids for Test Flight (DECISIONS #102): Home's tile, the Starter's "Test it", the designer's test band
// and the result card. Same convention as UiIds.cs.
namespace Kestrel.Core.Ui
{
    public static partial class UiIds
    {
        /// <summary>Home: the Test Flight tile directly under LAUNCH.</summary>
        public const string HomeTestFlight = "home-testflight";
        /// <summary>Mission Starter: the secondary "Test it" button left of LAUNCH.</summary>
        public const string StarterTestIt = "starter-test-it";
        /// <summary>Designer in a test: the "TEST FLIGHT" eyebrow and the one-line rules note.</summary>
        public const string TestFlightBand = "testflight-band";
        public const string TestFlightEyebrow = "testflight-eyebrow";
        public const string TestFlightRules = "testflight-rules";

        // ---- the Test Flight result card (screen-report) ----------------------------------------
        public const string TestCard = "testflight-card";
        public const string TestCardEyebrow = "testflight-card-eyebrow";
        public const string TestCardIcon = "testflight-card-icon";
        public const string TestCardTitle = "testflight-card-title";
        public const string TestCardKid = "testflight-card-kid";
        public const string TestCardEngineer = "testflight-card-engineer";
        public const string TestCardHint = "testflight-card-hint";
        public const string TestCardBurst = "testflight-card-burst";
        /// <summary>Primary, full width: back to the designer with the same rocket.</summary>
        public const string TestRetry = "testflight-retry";
        /// <summary>Half width: a fresh starter rocket, no confirm.</summary>
        public const string TestNewDesign = "testflight-new-design";
        /// <summary>Half width: Home.</summary>
        public const string TestHome = "testflight-home";

        /// <summary>The card's three exits, in layout order.</summary>
        public static readonly string[] TestCardExits = { TestRetry, TestNewDesign, TestHome };
    }
}

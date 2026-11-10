// When Home should offer Continue. A fresh career has nothing to resume.
// CONTINUE is only for a draft or a saved rocket (NAV-10): cleared>0 alone used to show
// CONTINUE that opened an auto-built starter while speaking "the rocket you were working on".
namespace Kestrel.Core.Save
{
    public static class ResumeOffer
    {
        public const string ContinueLabel = "CONTINUE";
        public const string ContinueSpoken = "Continue. Opens the rocket you were working on.";

        /// <param name="cleared">Kept for call-site compatibility; ignored (NAV-10).</param>
        public static bool Show(bool draft, int designs, int cleared)
        {
            _ = cleared;
            return draft || designs > 0;
        }
    }
}

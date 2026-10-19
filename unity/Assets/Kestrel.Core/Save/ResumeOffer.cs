// When Home should offer Continue. A fresh career has nothing to resume.
namespace Kestrel.Core.Save
{
    public static class ResumeOffer
    {
        public const string ContinueLabel = "CONTINUE";
        public const string ContinueSpoken = "Continue. Opens the rocket you were working on.";

        public static bool Show(bool draft, int designs, int cleared)
            => draft || designs > 0 || cleared > 0;
    }
}

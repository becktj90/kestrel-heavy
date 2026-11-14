// When Home should offer Continue. A fresh career has nothing to resume.
// CONTINUE is only for a draft or a saved rocket (NAV-10): cleared>0 alone used to show
// CONTINUE that opened an auto-built starter while speaking "the rocket you were working on".
namespace Kestrel.Core.Save
{
    public static class ResumeOffer
    {
        public const string ContinueLabel = "CONTINUE";
        public const string ContinueSpoken = "Continue. Opens the rocket you were working on.";

        /// <summary>Playthrough 2026-11-11: CONTINUE read "KH-1 · First Hop" (the next mission) while it opened the newest
        /// saved rocket as a free build. The sub-line now names what CONTINUE opens (ResumeActions.Open's choice):
        /// a mission's draft by its mission line, a saved rocket by its name with the mission it carries or "free build".</summary>
        public const string FreeBuild = "free build", LastRocket = "Your last rocket";

        /// <param name="fromDraft">The rocket is the autosaved draft (else the newest saved design).</param>
        /// <param name="missionLine">"KH-n · Name" of the mission the route carries, or null for a free build.</param>
        /// <param name="rocketName">The rocket's saved name, if any.</param>
        public static string Caption(bool fromDraft, string missionLine, string rocketName)
        {
            string line = string.IsNullOrWhiteSpace(missionLine) ? null : missionLine.Trim();
            string name = string.IsNullOrWhiteSpace(rocketName) ? null : rocketName.Trim();
            if (fromDraft && line != null) return line;
            string mission = line == null ? FreeBuild : Code(line);
            return (name ?? LastRocket) + " \u00B7 " + mission;
        }

        /// <summary>"KH-3" from "KH-3 · Tiny Orbit".</summary>
        static string Code(string line)
        {
            int dot = line.IndexOf('\u00B7');
            return dot > 0 ? line.Substring(0, dot).Trim() : line;
        }

        /// <summary>The CTA's spoken form with a caption.</summary>
        public static string Spoken(string caption) =>
            string.IsNullOrWhiteSpace(caption) ? ContinueSpoken : "Continue. Opens " + caption.Replace(" \u00B7 ", ", ") + ".";

        /// <param name="cleared">Kept for call-site compatibility; ignored (NAV-10).</param>
        public static bool Show(bool draft, int designs, int cleared)
        {
            _ = cleared;
            return draft || designs > 0;
        }
    }
}

// One catch rule for the result screen and the hangar shelf.
// The medal is the shelf name of the Haven grade from LandingTune. It is not a second formula.
// Perfect is gold, Good is silver, Hard and Salvage are bronze. Tilt does not move either one.
using System.Text;

namespace Kestrel.Core.Flight
{
    public readonly struct MedalAward
    {
        public readonly string Grade;
        public readonly string Medal;

        public MedalAward(string grade, string medal)
        {
            Grade = grade;
            Medal = medal;
        }
    }

    public static class MedalShelf
    {
        public const string Rule =
            "The medal is the shelf name of the Haven grade. Perfect is gold. Good is silver. Hard and Salvage are bronze, so every deck landing keeps a medal. Water and any non-deck touchdown have no medal. Tilt is not a second test, and the older 55/40/4 and 80/75/8 fractions are retired.";

        public static MedalAward From(TouchdownGrade grade)
        {
            string medal;
            if (grade.Name == "Perfect") medal = "gold";
            else if (grade.Name == "Good") medal = "silver";
            else if (grade.Name == "Hard" || grade.Name == "Salvage") medal = "bronze";
            else medal = null;
            return new MedalAward(grade.Name, medal);
        }

        public static MedalAward Evaluate(LandingTune tune, string kind, double vy, double dx, double landingVy, double landingTol, bool tipped)
            => From(tune.Grade(kind, vy, dx, landingVy, landingTol, tipped));

        /// <summary>Best medal. Gold outranks silver, silver outranks bronze. A missing medal does not erase a kept one.</summary>
        public static string Better(string kept, string next)
        {
            return Rank(next) > Rank(kept) ? next : (string.IsNullOrEmpty(kept) ? null : kept);
        }

        public static int Rank(string medal)
        {
            if (medal == "gold") return 3;
            if (medal == "silver") return 2;
            if (medal == "bronze") return 1;
            return 0;
        }

        /// <summary>Hangar line. "KH-1 gold · KH-2 bronze · KH-3 —".</summary>
        public static string Format(string[] missionIds, string[] medals)
        {
            var sb = new StringBuilder();
            int n = missionIds == null ? 0 : missionIds.Length;
            for (int i = 0; i < n; i++)
            {
                if (i > 0) sb.Append(" · ");
                string medal = medals != null && i < medals.Length ? medals[i] : null;
                sb.Append(missionIds[i]).Append(' ').Append(string.IsNullOrEmpty(medal) ? "—" : medal);
            }
            return sb.ToString();
        }
    }
}

// First-run opening on Home: Thrum, then host/co-host subtitles, then a career CTA into KH-1.
// Text is grounded in data/campaign.json (KH-1 First Hop). Commentary clips are optional subtitles only.
using System.Globalization;
using Kestrel.Core.Flight;
using Kestrel.Core.Guide;

namespace Kestrel.Game.Guide
{
    public static class OpeningIntroSequence
    {
        public static readonly string[] BeatIds = { "first.1", "first.host", "first.cohost", "first.cta" };

        /// <summary>Optional narration manifest ids when read-aloud is added later. Subtitles do not depend on them.</summary>
        public const string HostClipId = "host/color.pad.m1.t1";
        public const string CohostClipId = "cohost/color.pad.m2.t1";

        public static void Queue(LiveGuideEngine engine)
        {
            if (engine == null || engine.State.FirstRunDone) return;
            if (!engine.State.HasSeen("first.1"))
                engine.Offer("guide.first", "1", null, "home", false);
            if (!engine.State.HasSeen("first.host"))
                engine.OfferBeat(HostedBeat(engine));
            if (!engine.State.HasSeen("first.cohost"))
                engine.OfferBeat(CohostedBeat(engine));
            if (!engine.State.HasSeen("first.cta"))
                engine.Offer("guide.first", "cta", null, "home", false);
        }

        public static bool IsComplete(GuideState state)
        {
            if (state == null) return true;
            for (int i = 0; i < BeatIds.Length; i++)
                if (!state.HasSeen(BeatIds[i])) return false;
            return true;
        }

        public static void MarkSkipped(GuideState state)
        {
            if (state == null) return;
            for (int i = 0; i < BeatIds.Length; i++)
                state.MarkSeen(BeatIds[i]);
            state.FirstRunDone = true;
        }

        public static bool Active(LiveGuideEngine engine)
        {
            if (engine == null || engine.State.FirstRunDone) return false;
            if (engine.Pending.Count > 0) return true;
            return !IsComplete(engine.State);
        }

        public static string HostKid(string repoRoot) => HostBeat(Campaign.Load(repoRoot)).Cards[0].Kid;

        public static string CohostKid(string repoRoot) => CohostBeat(Campaign.Load(repoRoot)).Cards[0].Kid;

        public static string CtaKid(GuideScript script) => script?.ById("first.cta")?.Cards[0]?.Kid ?? "";

        static GuideBeat HostedBeat(LiveGuideEngine engine) => HostBeat(Campaign.Load(engine.RepoRoot));

        static GuideBeat CohostedBeat(LiveGuideEngine engine) => CohostBeat(Campaign.Load(engine.RepoRoot));

        static GuideBeat HostBeat(Campaign campaign)
        {
            var facts = FactsFor(campaign);
            string kid = "Mission " + facts.Title + ": toss " + PebbleName(campaign)
                + ", " + facts.Mass + ", to a " + ApogeeKm(campaign) + " km apogee.";
            string eng = "KH-1 apogee is " + ApogeeKm(campaign) + " km. Pebble is 50 kg.";
            return CommentaryBeat("first.host", GuideSpeaker.Host, kid, eng);
        }

        static GuideBeat CohostBeat(Campaign campaign)
        {
            int total = campaign?.Missions?.Count ?? 20;
            string kid = total + " KH flights sit on the career board. Clear each job to unlock the next.";
            string eng = "The campaign runs KH-1 through KH-" + total + ".";
            return CommentaryBeat("first.cohost", GuideSpeaker.Cohost, kid, eng);
        }

        static GuideBeat CommentaryBeat(string id, GuideSpeaker speaker, string kid, string eng)
        {
            return new GuideBeat
            {
                Id = id,
                Speaker = speaker,
                Priority = GuidePriority.Chatter,
                StepId = "home",
                Cards = new[] { new GuideCard { Id = id + ".c1", Kid = kid, Eng = eng } },
                FromEngine = true,
            };
        }

        static MissionBoardFacts FactsFor(Campaign campaign)
        {
            CampaignMission m1 = null;
            if (campaign?.Missions != null)
            {
                for (int i = 0; i < campaign.Missions.Count; i++)
                {
                    var row = campaign.Missions[i];
                    if (row != null && row.Index == 1) { m1 = row; break; }
                }
                if (m1 == null && campaign.Missions.Count > 0) m1 = campaign.Missions[0];
            }
            return MissionBoardFacts.Of(m1, MissionBoardAccess.Open, 0, null, 0, 0, true);
        }

        static string PebbleName(Campaign campaign)
        {
            var m1 = Kh1(campaign);
            if (m1 != null && !string.IsNullOrEmpty(m1.PayloadName)) return m1.PayloadName;
            return "Pebble";
        }

        static string ApogeeKm(Campaign campaign)
        {
            var m1 = Kh1(campaign);
            if (m1 != null && m1.ApoKm > 0)
                return m1.ApoKm.ToString("0", CultureInfo.InvariantCulture);
            return "10";
        }

        /// <summary>KH-1 when the board has it, else the first row. Apo and the payload name live on the mission, not a nested target.</summary>
        static CampaignMission Kh1(Campaign campaign)
        {
            if (campaign?.Missions == null || campaign.Missions.Count == 0) return null;
            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var row = campaign.Missions[i];
                if (row != null && row.Index == 1) return row;
            }
            return campaign.Missions[0];
        }
    }
}
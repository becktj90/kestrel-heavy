// First-run opening on Home: Thrum, then host/co-host subtitles, then a close, a send-off,
// and a career CTA into KH-1.
// Text is the Trevor-approved intro draft (Nov 14, 2026), grounded in data/campaign.json
// (20 missions, KH-1 First Hop) and docs/ECONOMY.md (per-mission budget, 70% reuse refund,
// a miss pays nothing, 60,000 Kc range floor). Beats are single-speaker, so the draft's
// closing beat (Thrum + Sarah) is two beats: first.close (Thrum) then first.sendoff (Sarah).
// Card ids ARE the narration manifest clip ids, so subtitles and spoken lines never disagree.
// Thrum clips play through the guide manifest (GuideClipPlayer); host/co-host clips live in
// the narration manifest and are subtitle-first until the GuideBox routes them to audio.
using Kestrel.Core.Guide;

namespace Kestrel.Game.Guide
{
    public static class OpeningIntroSequence
    {
        public static readonly string[] BeatIds = { "first.1", "first.host", "first.cohost", "first.close", "first.sendoff", "first.cta" };

        /// <summary>Narration manifest ids for the intro lines, in card order. Subtitles do not depend on them.</summary>
        public static readonly string[] HostClipIds = { "host/intro.b2.1", "host/intro.b2.2", "host/intro.b2.3", "host/intro.b4.1" };
        public static readonly string[] CohostClipIds = { "cohost/intro.b3.1", "cohost/intro.b3.2", "cohost/intro.b3.3" };

        /// <summary>Guide manifest id for the Thrum close line. Played by GuideClipPlayer like any Thrum card.</summary>
        public const string ThrumCloseClipId = "first.close.c1";

        public static void Queue(LiveGuideEngine engine)
        {
            if (engine == null || engine.State.FirstRunDone) return;
            if (!engine.State.HasSeen("first.1"))
                engine.Offer("guide.first", "1", null, "home", false);
            if (!engine.State.HasSeen("first.host"))
                engine.OfferBeat(HostBeat());
            if (!engine.State.HasSeen("first.cohost"))
                engine.OfferBeat(CohostBeat());
            if (!engine.State.HasSeen("first.close"))
                engine.Offer("guide.first", "close", null, "home", false);
            if (!engine.State.HasSeen("first.sendoff"))
                engine.OfferBeat(SendoffBeat());
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
            // Incomplete intro beats, not a later tip. The stack stays up until those beats are seen.
            return !IsComplete(engine.State);
        }

        public static string HostKid() => HostBeat().Cards[0].Kid;

        public static string CohostKid() => CohostBeat().Cards[0].Kid;

        public static string SendoffKid() => SendoffBeat().Cards[0].Kid;

        public static string CtaKid(GuideScript script) => script?.ById("first.cta")?.Cards[0]?.Kid ?? "";

        public static string CloseKid(GuideScript script) => script?.ById("first.close")?.Cards[0]?.Kid ?? "";

        static GuideBeat HostBeat()
        {
            return CommentaryBeat("first.host", GuideSpeaker.Host, new[]
            {
                new GuideCard { Id = "host/intro.b2.1", Kid = "Twenty flights stand between this hangar and the far hold.", Eng = "Twenty missions. KH-20 Far Hold is the last." },
                new GuideCard { Id = "host/intro.b2.2", Kid = "First hop first. A small hop, then the edge of space.", Eng = "KH-1 apogee is 10 km. Pebble is 50 kg." },
                new GuideCard { Id = "host/intro.b2.3", Kid = "After that: tiny orbit, weather eye, and heavy lifting.", Eng = "KH-2 Edge of Space. KH-3 Tiny Orbit. KH-4 Weather Eye. KH-5 Heavy Lifter." },
            });
        }

        static GuideBeat CohostBeat()
        {
            return CommentaryBeat("first.cohost", GuideSpeaker.Cohost, new[]
            {
                new GuideCard { Id = "cohost/intro.b3.1", Kid = "Every mission sets a budget. Build inside it, and fly.", Eng = "Budget equals reward. KH-1 reward is 40,000 Kc." },
                new GuideCard { Id = "cohost/intro.b3.2", Kid = "Land the booster, and most of its cost comes back.", Eng = "Reuse refund is 70 percent of engines, tanks and recovery gear." },
                new GuideCard { Id = "cohost/intro.b3.3", Kid = "A miss pays nothing, and the range keeps you flying.", Eng = "A miss pays nothing. The range floor is 60,000 Kc." },
            });
        }

        static GuideBeat SendoffBeat()
        {
            return CommentaryBeat("first.sendoff", GuideSpeaker.Host, new[]
            {
                new GuideCard { Id = "host/intro.b4.1", Kid = "First hop. Let us fly.", Eng = "KH-1 First Hop. Tap LAUNCH when ready." },
            });
        }

        static GuideBeat CommentaryBeat(string id, GuideSpeaker speaker, GuideCard[] cards)
        {
            return new GuideBeat
            {
                Id = id,
                Speaker = speaker,
                Priority = GuidePriority.Chatter,
                StepId = "home",
                Cards = cards,
                FromEngine = true,
            };
        }
    }
}

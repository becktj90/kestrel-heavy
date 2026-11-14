// Stand-in for the G7 guide engine. Queues beats in memory and honours skip-for-step.
using System.Collections.Generic;

namespace Kestrel.Game.Guide
{
    public sealed class MockGuideEngine : IGuideEngine
    {
        readonly List<GuideBeat> queued = new List<GuideBeat>();
        readonly HashSet<string> skippedSteps = new HashSet<string>();

        public GuideSettings Settings { get; } = new GuideSettings();
        public List<string> Seen { get; } = new List<string>();
        public List<string> Skipped { get; } = new List<string>();
        public string LastSkippedStep { get; private set; } = "";

        public IReadOnlyList<GuideBeat> Pending
        {
            get
            {
                var list = new List<GuideBeat>();
                for (int i = 0; i < queued.Count; i++)
                {
                    var beat = queued[i];
                    if (beat == null) continue;
                    if (beat.Priority == GuidePriority.Chatter && !string.IsNullOrEmpty(beat.StepId) && skippedSteps.Contains(beat.StepId))
                        continue;
                    list.Add(beat);
                }
                return list;
            }
        }

        public void Offer(GuideBeat beat)
        {
            if (beat == null) return;
            beat.FromEngine = true;
            queued.Add(beat);
        }

        public void Complete(string beatId)
        {
            if (Remove(beatId) && !string.IsNullOrEmpty(beatId)) Seen.Add(beatId);
        }

        public void Skip(string beatId)
        {
            if (Remove(beatId) && !string.IsNullOrEmpty(beatId)) Skipped.Add(beatId);
        }

        public void SkipTipsForStep(string stepId)
        {
            LastSkippedStep = stepId ?? "";
            if (!string.IsNullOrEmpty(stepId)) skippedSteps.Add(stepId);
            for (int i = queued.Count - 1; i >= 0; i--)
            {
                var beat = queued[i];
                if (beat != null && beat.Priority == GuidePriority.Chatter && beat.StepId == stepId)
                    queued.RemoveAt(i);
            }
        }

        bool Remove(string beatId)
        {
            for (int i = 0; i < queued.Count; i++)
            {
                if (queued[i] != null && queued[i].Id == beatId)
                {
                    queued.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>One beat per opening step so the box can show 1/4 before the live engine loads.</summary>
        public static GuideBeat FirstRun()
        {
            return new GuideBeat
            {
                Id = "first.1",
                Speaker = GuideSpeaker.Thrum,
                Priority = GuidePriority.Chatter,
                Cards = new[]
                {
                    new GuideCard { Kid = "Oh! A visitor. Mind the oil. I am Thrum. I keep this hangar, and I talk to wrenches." },
                    new GuideCard { Kid = "Mission KH-1 · First Hop: toss Pebble to a 10 km apogee.", Id = "first.host.c1" },
                    new GuideCard { Kid = "Twenty KH flights sit on the career board. Clear each job to unlock the next.", Id = "first.cohost.c1" },
                    new GuideCard { Kid = "Tap LAUNCH for KH-1 First Hop, or MISSIONS to see all twenty flights.", Id = "first.cta.c1" },
                }
            };
        }
    }
}

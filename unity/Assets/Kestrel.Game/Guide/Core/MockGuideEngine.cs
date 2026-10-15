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

        /// <summary>The five-card welcome from the structure spec, so the box can show 1/5 before G7.</summary>
        public static GuideBeat FirstRun()
        {
            return new GuideBeat
            {
                Id = "first.1",
                Speaker = GuideSpeaker.Thrum,
                Priority = GuidePriority.Chatter,
                Cards = new[]
                {
                    new GuideCard { Kid = "Oh! A visitor! Mind the oil. I am Thrum. I keep this hangar, and I talk to wrenches." },
                    new GuideCard { Kid = "This is Dolores. She says hello. She also says you hold wrenches wrong. She is wrong." },
                    new GuideCard { Kid = "A rocket is just a pushy tube with a plan. Your job: build it, pick a goal, press go." },
                    new GuideCard { Kid = "I will pop in with tips. Tap to move on. Tap SKIP and I will hush. I take it well. Mostly." },
                    new GuideCard { Kid = "First job: the First Hop. Straight up ten kilometres. Easy as a sneeze. Let's go!" },
                }
            };
        }
    }
}

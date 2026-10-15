// The small surface the subtitle box needs from the G7 guide engine.
// G7 will load data/guide.json, bind MissionEvent ids, and keep seen-state.
// Until that merges, MockGuideEngine stands in. G9 adapts the real engine to this.
using System.Collections.Generic;

namespace Kestrel.Game.Guide
{
    public interface IGuideEngine
    {
        GuideSettings Settings { get; }

        /// <summary>Beats still waiting to be shown, highest priority first is not required.
        /// The box applies priority, the two-box cap, and the tips filter.</summary>
        IReadOnlyList<GuideBeat> Pending { get; }

        void Complete(string beatId);
        void Skip(string beatId);

        /// <summary>Long-press SKIP. Drop tips for this designer step.</summary>
        void SkipTipsForStep(string stepId);
    }
}

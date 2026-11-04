// Decides when a finished flight's result opens the post-flight card, exactly once.
// The results-screen first tap (QA #417, kid 040-041, engineer 104-109): "Back to designer"
// rebuilt the rocket, the new FailureHost started with no memory of the result still on the
// flight driver, and the next frame re-opened the card ("Stars saved. Kc was settled...").
// A latch armed with whatever result is already on the driver ignores it until a new one lands.
namespace Kestrel.Core.Flight
{
    public sealed class ReportLatch
    {
        object closed;

        /// <summary>True once the current result has opened its card.</summary>
        public bool Reported { get; private set; }

        /// <summary>
        /// A new or reset host. The result already on the driver (if any) belongs to a card
        /// that was already shown or dismissed, so it never opens another one.
        /// </summary>
        public void Arm(object resultOnDriver)
        {
            if (resultOnDriver != null) closed = resultOnDriver;
            Reported = false;
        }

        /// <summary>
        /// One frame. True exactly once per new result, and only after it is final
        /// (a pass waits for the flight to stop and the recovery to finish; a failure opens at once).
        /// </summary>
        public bool TryOpen(object result, bool failed, bool running, bool recovering)
        {
            if (result == null || Reported || ReferenceEquals(result, closed)) return false;
            if (!failed && (running || recovering)) return false;
            Reported = true;
            closed = result;
            return true;
        }
    }
}

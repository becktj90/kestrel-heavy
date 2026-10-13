// Post-flight telemetry (§3, §5). A fixed ring of t / alt / v / q / g / mass / Δv losses,
// plus a second ring of event marks. Push overwrites the oldest sample. The loss channels
// are the running totals from LossAccount (gravity, drag, steering), in m/s.
using System;

namespace Kestrel.Core.Flight
{
    public sealed class TelemetryLog
    {
        public const int DefaultCapacity = 240;
        public const int EventCapacity = 48;

        public struct Sample
        {
            public double T, Alt, V, Q, G, Mass, LossGravity, LossDrag, LossSteering;
            public double LossTotal => LossGravity + LossDrag + LossSteering;
        }

        public struct EventMark
        {
            public double T;
            public string Kind;
            public bool Failure;
        }

        readonly Sample[] samples;
        readonly EventMark[] marks;
        int sampleWrite, sampleCount, markWrite, markCount;

        public TelemetryLog(int capacity = DefaultCapacity)
        {
            if (capacity < 2) capacity = 2;
            samples = new Sample[capacity];
            marks = new EventMark[EventCapacity];
        }

        public int Capacity => samples.Length;
        public int Count => sampleCount;
        public int EventCount => markCount;

        /// <summary>0 is the oldest sample still in the ring.</summary>
        public Sample this[int index]
        {
            get
            {
                if (index < 0 || index >= sampleCount) throw new ArgumentOutOfRangeException(nameof(index));
                return samples[Slot(index)];
            }
        }

        public EventMark EventAt(int index)
        {
            if (index < 0 || index >= markCount) throw new ArgumentOutOfRangeException(nameof(index));
            int slot = markCount < marks.Length ? index : (markWrite + index) % marks.Length;
            return marks[slot];
        }

        public void Push(double t, double alt, double v, double q, double g, double mass, double lossGravity, double lossDrag, double lossSteering)
        {
            if (double.IsNaN(t) || double.IsInfinity(t)) return;
            samples[sampleWrite] = new Sample
            {
                T = t, Alt = alt, V = v, Q = q, G = g, Mass = mass,
                LossGravity = lossGravity, LossDrag = lossDrag, LossSteering = lossSteering,
            };
            sampleWrite = (sampleWrite + 1) % samples.Length;
            if (sampleCount < samples.Length) sampleCount++;
        }

        /// <summary>Write the finished loss totals onto the newest sample (they are only known at burnout).</summary>
        public void StampLatestLosses(double lossGravity, double lossDrag, double lossSteering)
        {
            if (sampleCount == 0) return;
            int slot = Slot(sampleCount - 1);
            var s = samples[slot];
            s.LossGravity = lossGravity;
            s.LossDrag = lossDrag;
            s.LossSteering = lossSteering;
            samples[slot] = s;
        }

        public void Mark(double t, string kind, bool failure)
        {
            if (double.IsNaN(t)) return;
            marks[markWrite] = new EventMark { T = t, Kind = kind ?? "", Failure = failure };
            markWrite = (markWrite + 1) % marks.Length;
            if (markCount < marks.Length) markCount++;
        }

        public bool TryFailure(out EventMark mark)
        {
            for (int i = markCount - 1; i >= 0; i--)
            {
                var m = EventAt(i);
                if (m.Failure) { mark = m; return true; }
            }
            mark = default;
            return false;
        }

        int Slot(int oldestIndex)
        {
            int start = sampleCount < samples.Length ? 0 : sampleWrite;
            return (start + oldestIndex) % samples.Length;
        }
    }
}

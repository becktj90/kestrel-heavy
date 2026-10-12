// G1 thermal/perf governor (GFX_AUDIT §7), engine-free so dotnet and EditMode can test it.
// Rolling window of frame times; if the p95 stays above stepDownMs for stepDownHoldSec, drop one
// preset (never below the floor). If it stays at/below recoverMs for recoverHoldSec, climb one
// preset (never above the ceiling = the preset the device/user chose). Tunables: data/quality.json.
using System;

namespace Kestrel.Game.Quality
{
    public sealed class GovernorConfig
    {
        public int WindowFrames = 120;
        public double Percentile = 0.95;
        public double StepDownMs = 20.0, StepDownHoldSec = 3.0;
        public double RecoverMs = 16.7, RecoverHoldSec = 30.0;
    }

    public sealed class QualityGovernor
    {
        readonly GovernorConfig _c;
        readonly double[] _ring, _scratch;
        int _count, _head;
        double _overSec, _underSec;

        public int Level { get; private set; }
        public int Floor { get; }
        public int Ceiling { get; private set; }
        /// <summary>Raised with (oldLevel, newLevel) whenever the governor moves.</summary>
        public event Action<int, int> Changed;

        public QualityGovernor(GovernorConfig c, int startLevel, int floor = 0)
        {
            _c = c ?? new GovernorConfig();
            if (_c.WindowFrames < 8) throw new ArgumentOutOfRangeException(nameof(c), "window too small");
            _ring = new double[_c.WindowFrames];
            _scratch = new double[_c.WindowFrames];
            Floor = floor;
            Ceiling = Math.Max(floor, startLevel);
            Level = Ceiling;
        }

        /// <summary>User/device picked a new preset: it becomes the ceiling and the current level.</summary>
        public void SetChosen(int level)
        {
            Ceiling = Math.Max(Floor, level);
            Move(Ceiling);
            Reset();
        }

        public void Reset() { _count = 0; _head = 0; _overSec = 0; _underSec = 0; }

        public double P95Ms
        {
            get
            {
                if (_count == 0) return 0;
                Array.Copy(_ring, _scratch, _count);
                Array.Sort(_scratch, 0, _count);
                int i = (int)Math.Ceiling(_c.Percentile * _count) - 1;
                return _scratch[Math.Max(0, Math.Min(_count - 1, i))];
            }
        }

        /// <summary>Feed one frame's unscaled delta (seconds). Returns true if the level changed.</summary>
        public bool Tick(double dtSec)
        {
            if (!(dtSec > 0) || dtSec > 1.0) return false; // ignore pauses/backgrounding spikes
            _ring[_head] = dtSec * 1000.0;
            _head = (_head + 1) % _ring.Length;
            if (_count < _ring.Length) { _count++; return false; }

            double p = P95Ms;
            if (p > _c.StepDownMs) { _overSec += dtSec; _underSec = 0; }
            else if (p <= _c.RecoverMs) { _underSec += dtSec; _overSec = 0; }
            else { _overSec = 0; _underSec = 0; }

            if (_overSec >= _c.StepDownHoldSec && Level > Floor) { Move(Level - 1); Reset(); return true; }
            if (_underSec >= _c.RecoverHoldSec && Level < Ceiling) { Move(Level + 1); Reset(); return true; }
            return false;
        }

        void Move(int to)
        {
            if (to == Level) return;
            int from = Level; Level = to;
            Changed?.Invoke(from, to);
        }
    }

    public static class QualityMath
    {
        /// <summary>URP render scale so the backbuffer never exceeds dprCap x points: min(1, cap/dpr).</summary>
        public static double RenderScale(double dprCap, double dpr) => dpr <= 0 ? 1 : Math.Max(0.1, Math.Min(1, dprCap / dpr));
    }
}

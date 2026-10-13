// Telemetry plots (§3): altitude, velocity, q, g and Δv losses, with event marks.
// X is normalised flight time. The failure mark sits on the nearest sample.
using System;
using Kestrel.Core.Flight;

namespace Kestrel.Slice
{
    public sealed class PlotGeometry
    {
        public string Channel;
        public float[] X = Array.Empty<float>();
        public float[] Y = Array.Empty<float>();
        public float[] MarkerX = Array.Empty<float>();
        public float FailureX = -1f;
    }

    public static class ReportPlot
    {
        public static readonly string[] Channels = { "alt", "vel", "q", "g", "losses" };

        public static PlotGeometry[] Build(TelemetryLog log)
        {
            var plots = new PlotGeometry[Channels.Length];
            int n = log == null ? 0 : log.Count;
            double t0 = n > 0 ? log[0].T : 0;
            double t1 = n > 0 ? log[n - 1].T : 1;
            if (t1 <= t0) t1 = t0 + 1;
            float[] mx = Marks(log, t0, t1, false);
            float fail = FailureX(log, t0, t1);
            for (int c = 0; c < Channels.Length; c++)
            {
                var x = new float[n];
                var y = new float[n];
                double min = 0, max = 0;
                bool any = false;
                for (int i = 0; i < n; i++)
                {
                    double v = Value(log[i], Channels[c]);
                    x[i] = (float)((log[i].T - t0) / (t1 - t0));
                    if (!any) { min = max = v; any = true; }
                    else { if (v < min) min = v; if (v > max) max = v; }
                    y[i] = (float)v;
                }
                double span = max - min;
                for (int i = 0; i < n; i++)
                    y[i] = span < 1e-9 ? 0.5f : (float)((y[i] - min) / span);
                plots[c] = new PlotGeometry { Channel = Channels[c], X = x, Y = y, MarkerX = mx, FailureX = fail };
            }
            return plots;
        }

        public static float NormTime(double t, double t0, double t1)
        {
            if (t1 <= t0) return 0f;
            double u = (t - t0) / (t1 - t0);
            if (u < 0) u = 0;
            if (u > 1) u = 1;
            return (float)u;
        }

        static float FailureX(TelemetryLog log, double t0, double t1)
        {
            if (log == null || !log.TryFailure(out var mark) || log.Count == 0) return -1f;
            int nearest = 0;
            double best = double.MaxValue;
            for (int i = 0; i < log.Count; i++)
            {
                double d = Math.Abs(log[i].T - mark.T);
                if (d < best) { best = d; nearest = i; }
            }
            return NormTime(log[nearest].T, t0, t1);
        }

        static float[] Marks(TelemetryLog log, double t0, double t1, bool failureOnly)
        {
            if (log == null || log.EventCount == 0) return Array.Empty<float>();
            var tmp = new float[log.EventCount];
            int n = 0;
            for (int i = 0; i < log.EventCount; i++)
            {
                var m = log.EventAt(i);
                if (failureOnly && !m.Failure) continue;
                tmp[n++] = NormTime(m.T, t0, t1);
            }
            if (n == tmp.Length) return tmp;
            var cut = new float[n];
            Array.Copy(tmp, cut, n);
            return cut;
        }

        static double Value(TelemetryLog.Sample s, string channel)
        {
            switch (channel)
            {
                case "alt": return s.Alt;
                case "vel": return s.V;
                case "q": return s.Q;
                case "g": return s.G;
                default: return s.LossTotal;
            }
        }
    }
}

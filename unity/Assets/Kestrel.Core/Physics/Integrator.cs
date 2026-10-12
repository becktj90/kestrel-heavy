// Fixed-step RK4 (allocation-free per step) and a render-rate-independent accumulator.
// Port of web/js/physics/integrator.js.
using System;

namespace Kestrel.Core.Physics
{
    public delegate void Deriv(double t, double[] y, double[] dydt);

    public sealed class RK4
    {
        readonly int n; readonly double[] k1, k2, k3, k4, tmp;
        public RK4(int n) { this.n = n; k1 = new double[n]; k2 = new double[n]; k3 = new double[n]; k4 = new double[n]; tmp = new double[n]; }

        public void Step(Deriv f, double t, double[] y, double dt)
        {
            f(t, y, k1);
            for (int i = 0; i < n; i++) tmp[i] = y[i] + 0.5 * dt * k1[i];
            f(t + 0.5 * dt, tmp, k2);
            for (int i = 0; i < n; i++) tmp[i] = y[i] + 0.5 * dt * k2[i];
            f(t + 0.5 * dt, tmp, k3);
            for (int i = 0; i < n; i++) tmp[i] = y[i] + dt * k3[i];
            f(t + dt, tmp, k4);
            for (int i = 0; i < n; i++) y[i] += (dt / 6) * (k1[i] + 2 * k2[i] + 2 * k3[i] + k4[i]);
        }
    }

    /// <summary>Feed real frame time; runs whole fixed physics steps (default 1/120 s).</summary>
    public sealed class FixedStepper
    {
        readonly Action<double, double> step; readonly int maxSteps;
        public readonly double Dt;
        public double Time, Accumulator, Alpha;
        public FixedStepper(Action<double, double> step, double dt = 1.0 / 120, int maxSteps = 16) { this.step = step; Dt = dt; this.maxSteps = maxSteps; }

        public int Advance(double frameDt)
        {
            Accumulator += Math.Max(0, frameDt);
            int k = 0;
            while (Accumulator >= Dt && k < maxSteps) { step(Time, Dt); Time += Dt; Accumulator -= Dt; k++; }
            if (k == maxSteps && Accumulator > Dt) Accumulator = 0; // drop backlog (spiral-of-death guard)
            Alpha = Accumulator / Dt;
            return k;
        }
        public void Reset() { Time = 0; Accumulator = 0; Alpha = 0; }
    }
}

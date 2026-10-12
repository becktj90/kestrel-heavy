// Fixed-timestep RK4 integrator with a render-rate-independent accumulator.
// State is a Float64Array; deriv(t, y, out) must write dy/dt into `out` and
// must not allocate. All scratch buffers are allocated once per integrator.

/** Create an RK4 stepper for states of length n. */
export function createRK4(n) {
  const k1 = new Float64Array(n), k2 = new Float64Array(n), k3 = new Float64Array(n), k4 = new Float64Array(n);
  const tmp = new Float64Array(n);
  /** Advance y in place by dt. Returns y. */
  return function rk4Step(deriv, t, y, dt) {
    deriv(t, y, k1);
    for (let i = 0; i < n; i++) tmp[i] = y[i] + 0.5 * dt * k1[i];
    deriv(t + 0.5 * dt, tmp, k2);
    for (let i = 0; i < n; i++) tmp[i] = y[i] + 0.5 * dt * k2[i];
    deriv(t + 0.5 * dt, tmp, k3);
    for (let i = 0; i < n; i++) tmp[i] = y[i] + dt * k3[i];
    deriv(t + dt, tmp, k4);
    for (let i = 0; i < n; i++) y[i] += (dt / 6) * (k1[i] + 2 * k2[i] + 2 * k3[i] + k4[i]);
    return y;
  };
}

/**
 * Fixed-step accumulator. Feed it real frame time; it runs whole physics steps.
 * opts: { dt = 1/120 s, maxSteps = 16 (spiral-of-death guard) }
 * step(t, dt) is called for each fixed step. Returns an object with
 * advance(frameDt) -> steps run, alpha (0..1 interpolation factor), time, reset().
 */
export function createFixedStepper(step, opts = {}) {
  const dt = opts.dt ?? 1 / 120;
  const maxSteps = opts.maxSteps ?? 16;
  const s = {
    dt, time: 0, accumulator: 0, alpha: 0,
    advance(frameDt) {
      s.accumulator += Math.max(0, frameDt);
      let n = 0;
      while (s.accumulator >= dt && n < maxSteps) {
        step(s.time, dt);
        s.time += dt; s.accumulator -= dt; n++;
      }
      if (n === maxSteps && s.accumulator > dt) s.accumulator = 0; // drop backlog
      s.alpha = s.accumulator / dt;
      return n;
    },
    reset() { s.time = 0; s.accumulator = 0; s.alpha = 0; },
  };
  return s;
}

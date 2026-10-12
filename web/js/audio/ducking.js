/**
 * Voice-bus-driven ducking. While any voice line is active the music bus is
 * held at `musicDuck` (and sfx/engine at `sfxDuck`); released after `holdMs`.
 * Plus the legacy one-shot theme dip for critical SFX (same 0.28 / 780 ms).
 */
export const DUCK = Object.freeze({ musicDuck: 0.35, sfxDuck: 0.8, holdMs: 250, criticalMul: 0.28, criticalMs: 780 });

export function createDucker(mixer, { setTimer = setTimeout, clearTimer = clearTimeout } = {}) {
  let voices = 0;
  let timer = null;
  const apply = (on) => {
    mixer.setDuck('music', on ? DUCK.musicDuck : 1);
    mixer.setDuck('sfx', on ? DUCK.sfxDuck : 1);
    mixer.setDuck('engine', on ? DUCK.sfxDuck : 1);
  };
  return {
    get active() { return voices > 0; },
    voiceStart() {
      voices += 1;
      if (timer) { clearTimer(timer); timer = null; }
      apply(true);
    },
    voiceEnd() {
      voices = Math.max(0, voices - 1);
      if (voices > 0) return;
      if (timer) clearTimer(timer);
      timer = setTimer(() => { timer = null; if (!voices) apply(false); }, DUCK.holdMs);
    },
    reset() { voices = 0; if (timer) clearTimer(timer); timer = null; apply(false); },
  };
}

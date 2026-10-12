// Weather-hold policy. Not a weather simulation: KID and Relaxed never hold.
export const DEFAULT_WIND_LIMIT_MS = 12;

export function weatherHold({ difficulty, relaxed = false, windMs = 0, limitMs = DEFAULT_WIND_LIMIT_MS } = {}) {
  if (difficulty === 'KID' || relaxed) return { hold: false, allowed: false };
  return { allowed: true, hold: windMs > limitMs };
}

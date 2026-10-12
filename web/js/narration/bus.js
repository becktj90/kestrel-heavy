/**
 * Kestrel Heavy flight event bus — the single shared stream of "what really
 * happened" in a flight. Narration (narration/director.js) and, later, the
 * physics-driven SFX system both subscribe here. Publishers today are
 * mission.js (beats, aborts, flight begin/end) and voice.js (legacy radio
 * text). In M3 the physics sim becomes the publisher of the same ids.
 *
 * Rules:
 *  - Subscribers are output-only. They never mutate sim/game state.
 *  - Every event has { type, t, seq, payload }. `t` is game seconds
 *    (mission nowSec), `seq` is a monotonically increasing emit counter, so
 *    ordering is total even when two events share a timestamp.
 *  - Only types listed in EVENTS may be emitted (typos throw in strict mode,
 *    are dropped + counted otherwise).
 *  - Payload numbers are NOT spoken in N1. `source` is 'arcade' until the
 *    real sim publishes ('sim'); number lines are gated on that.
 *
 * See docs/FLIGHT_EVENTS.md for the table.
 */

/** Event categories. */
export const CATEGORY = Object.freeze({
  FLIGHT: 'flight', // lifecycle
  COUNT: 'count', // pre-launch / terminal count beats
  ASCENT: 'ascent', // powered flight milestones
  STAGING: 'staging', // separation and upper-stage
  RECOVERY: 'recovery', // booster return
  GAMEPLAY: 'gameplay', // pickups, hits, hints (arcade only)
  ANOMALY: 'anomaly', // aborts and failures
  OUTCOME: 'outcome', // end-of-flight results
  RADIO: 'radio', // legacy text radio mirror
  ECONOMY: 'economy', // prices, purchases (fictional Kc; never real money)
});

const C = CATEGORY;

/**
 * Typed event list. `payload` documents the fields a publisher should send
 * (all optional unless noted). Ids for beats equal the voice/manifest key.
 */
export const EVENTS = Object.freeze({
  'flight.begin': { cat: C.FLIGHT, payload: 'missionId, payload, qa?' },
  'flight.status': { cat: C.FLIGHT, payload: 'status (MENU|PRELAUNCH|ASCENT|SEP|JACKLYN|SUMMARY), prev' },
  'flight.end': { cat: C.FLIGHT, payload: 'reason (rud|done|...), score' },

  pad: { cat: C.COUNT, payload: 'missionId' },
  terminal: { cat: C.COUNT, payload: 'tClock' },
  tankpress: { cat: C.COUNT, payload: 'tClock' },
  internal: { cat: C.COUNT, payload: 'tClock' },
  deluge: { cat: C.COUNT, payload: 'tClock' },
  ignition: { cat: C.COUNT, payload: 'tClock' },

  liftoff: { cat: C.ASCENT, payload: 'tClock' },
  maxq: { cat: C.ASCENT, payload: 'tClock (arcade: scripted beat, not measured q)' },
  meco: { cat: C.ASCENT, payload: 'tClock' },
  'fuel-low': { cat: C.ASCENT, payload: 'fuel' },

  sep: { cat: C.STAGING, payload: 'tClock (sep zone opens)' },
  'align-green': { cat: C.STAGING, payload: '' },
  'align-wide': { cat: C.STAGING, payload: '' },
  'sep-clear': { cat: C.STAGING, payload: '' },
  'sep-wide': { cat: C.STAGING, payload: '' },
  'sep-contact': { cat: C.STAGING, payload: '' },
  ses1: { cat: C.STAGING, payload: 'tClock' },
  fairing: { cat: C.STAGING, payload: 'tClock' },
  seco: { cat: C.STAGING, payload: 'tClock' },
  deploy: { cat: C.STAGING, payload: 'payload' },

  entry: { cat: C.RECOVERY, payload: 'tClock' },
  landing: { cat: C.RECOVERY, payload: 'tClock' },
  touchdown: { cat: C.RECOVERY, payload: 'grade?' },
  'haven-slide': { cat: C.RECOVERY, payload: '' },
  'haven-reentry': { cat: C.RECOVERY, payload: '' },
  'haven-maxq': { cat: C.RECOVERY, payload: '' },
  'haven-strakes': { cat: C.RECOVERY, payload: '' },
  'haven-straighten': { cat: C.RECOVERY, payload: '' },
  'haven-burn': { cat: C.RECOVERY, payload: '' },
  'haven-burn-hold': { cat: C.RECOVERY, payload: '' },
  'haven-burn-nag': { cat: C.RECOVERY, payload: '' },

  'hint-ascent': { cat: C.GAMEPLAY, payload: '' },
  'hint-corridor': { cat: C.GAMEPLAY, payload: '' },
  'hint-pickup': { cat: C.GAMEPLAY, payload: '' },
  'hint-sep': { cat: C.GAMEPLAY, payload: '' },
  'hint-haven': { cat: C.GAMEPLAY, payload: '' },
  'first-save': { cat: C.GAMEPLAY, payload: '' },
  'press-ok': { cat: C.GAMEPLAY, payload: '' },
  'corridor-soft': { cat: C.GAMEPLAY, payload: '' },
  'corridor-edge': { cat: C.GAMEPLAY, payload: '' },
  'pickup-shield': { cat: C.GAMEPLAY, payload: '' },
  'pickup-fuel': { cat: C.GAMEPLAY, payload: '' },
  'pickup-boost': { cat: C.GAMEPLAY, payload: '' },
  'shield-hit': { cat: C.GAMEPLAY, payload: '' },
  'close-call': { cat: C.GAMEPLAY, payload: '' },
  'struct-warn': { cat: C.GAMEPLAY, payload: '' },
  'health-graze': { cat: C.GAMEPLAY, payload: '' },
  'health-scrape': { cat: C.GAMEPLAY, payload: '' },
  'health-lethal': { cat: C.GAMEPLAY, payload: '' },

  abort: { cat: C.ANOMALY, payload: 'abortId (one of voice.js ABORTS), reason, text' },

  recovered: { cat: C.OUTCOME, payload: '' },
  salvage: { cat: C.OUTCOME, payload: '' },
  tip: { cat: C.OUTCOME, payload: '' },
  splash: { cat: C.OUTCOME, payload: 'abortId (splash|fuelHaven)' },

  'radio.line': { cat: C.RADIO, payload: 'key, text (exact legacy radio text shown)' },

  'econ.quote': { cat: C.ECONOMY, payload: 'designId, recurring, dev, perKg (integer minor units), currency ("Kc")' },
  'econ.purchase': { cat: C.ECONOMY, payload: 'item, category, amount (integer minor units), currency ("Kc"); reserved for E3, not emitted in 2.0' },
});

export const EVENT_TYPES = Object.freeze(Object.keys(EVENTS));

export function isEventType(type) {
  return Object.prototype.hasOwnProperty.call(EVENTS, type);
}

export function createBus({ strict = false, historyLimit = 256 } = {}) {
  const subs = new Map(); // type|'*' -> Set<fn>
  const history = [];
  let seq = 0;
  let dropped = 0;

  function on(type, fn) {
    if (type !== '*' && !isEventType(type)) throw new Error(`bus: unknown event type "${type}"`);
    if (!subs.has(type)) subs.set(type, new Set());
    subs.get(type).add(fn);
    return () => subs.get(type)?.delete(fn);
  }

  function emit(type, payload = {}, t = 0) {
    if (!isEventType(type)) {
      dropped += 1;
      if (strict) throw new Error(`bus: unknown event type "${type}"`);
      return null;
    }
    seq += 1;
    const ev = Object.freeze({ type, cat: EVENTS[type].cat, t: Number(t) || 0, seq, payload: payload || {} });
    history.push(ev);
    if (history.length > historyLimit) history.shift();
    for (const key of [type, '*']) {
      const set = subs.get(key);
      if (!set) continue;
      for (const fn of [...set]) {
        try { fn(ev); } catch (err) {
          // A broken subscriber must never break the flight.
          if (strict) throw err;
          if (typeof console !== 'undefined') console.warn('[bus]', type, err);
        }
      }
    }
    return ev;
  }

  return {
    on,
    emit,
    history: () => history.slice(),
    clearHistory() { history.length = 0; },
    stats: () => ({ seq, dropped }),
  };
}

/** Shared app-wide bus. */
export const flightBus = createBus();
export default flightBus;

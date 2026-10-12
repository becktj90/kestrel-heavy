/**
 * Narration script manifest (N1, text-only).
 *
 * One entry per bus event id. Mission-control text is the existing voice.js
 * LINES/ABORTS pools (single source of truth for the on-screen radio, so the
 * HUD radio and captions never disagree). Host / co-host lines are new,
 * original, number-free commentary. Coaching = gameplay instruction lines.
 *
 * Entry shape:
 *   { id, lines: [{ channel, variants: string[], priority?, neverDuck?, tags?: string[] }] }
 * Tokens: {id} {payload} {mark} (see voice.js fillLine). No spoken numbers in
 * N1: lines must not contain digits other than inside tokens/names.
 */
import { ABORTS, LINES } from '../voice.js';

export const CHANNEL = Object.freeze({
  MC: 'mc', // mission control: terse, procedural
  HOST: 'host', // webcast lead host
  COHOST: 'cohost', // webcast co-host
  COACH: 'coach', // gameplay coaching (subtitle-only)
});

export const SPEAKER = Object.freeze({
  mc: { label: 'LAUNCH CONTROL', color: '#7fd6ff' },
  host: { label: 'HOST', color: '#ffd27f' },
  cohost: { label: 'CO-HOST', color: '#b9f28a' },
  coach: { label: 'FLIGHT COACH', color: '#e0b3ff' },
});

/** Higher wins. CRITICAL preempts anything currently showing. */
export const PRIORITY = Object.freeze({ LOW: 0, NORMAL: 1, HIGH: 2, CRITICAL: 3 });

/**
 * Style-guide priority tiers (kh-narr-style callouts.json) -> queue fields.
 * P0 safety/status: critical + neverDuck (cannot be cut, never ducked by mix).
 */
export const STYLE_TIER = Object.freeze({
  P0: { priority: 3, neverDuck: true },
  P1: { priority: 2, neverDuck: false },
  P2: { priority: 1, neverDuck: false },
  P3: { priority: 0, neverDuck: false },
});

/** Normalize an external template ({channel, variants, priority:'P0'..'P3'|n, neverDuck}). */
export function lineFromTemplate(t) {
  const tier = typeof t.priority === 'string' ? STYLE_TIER[t.priority] : null;
  return {
    channel: t.channel || CHANNEL.MC,
    variants: (t.variants || [t.text]).filter(Boolean),
    priority: tier ? tier.priority : (Number.isFinite(t.priority) ? t.priority : PRIORITY.NORMAL),
    neverDuck: t.neverDuck ?? tier?.neverDuck ?? false,
    tags: t.tags || [],
  };
}

const MC_CRITICAL = new Set(['liftoff', 'maxq', 'meco', 'ignition', 'sep-contact', 'touchdown', 'splash', 'tip']);
const MC_HIGH = new Set(['terminal', 'ses1', 'fairing', 'seco', 'deploy', 'entry', 'landing', 'sep-clear', 'sep-wide', 'recovered', 'salvage', 'health-lethal']);
const MC_KEYS = new Set([
  'pad', 'terminal', 'tankpress', 'internal', 'deluge', 'ignition', 'liftoff', 'maxq', 'meco',
  'sep-clear', 'sep-wide', 'sep-contact', 'ses1', 'fairing', 'entry', 'landing', 'touchdown',
  'seco', 'deploy', 'recovered', 'salvage', 'tip', 'splash', 'fuel-low', 'struct-warn',
  'health-lethal', 'haven-maxq',
]);

/** Original host/co-host commentary. Qualitative only (no numbers yet). */
const HOST_LINES = {
  pad: {
    host: [
      'Welcome to launch coverage. {id} is on the pad and the team is working the count.',
      'Good to have you with us. {id} sits on the pad, and the count is moving.',
      'This is launch day for {id}. We will walk you through every step of the climb.',
    ],
    cohost: [
      'On board is {payload}. Our job today is to get it safely to its orbit.',
      'Up top is {payload}. Everything below it exists to get it there.',
      'And the booster is coming home after this, so watch for the landing later.',
    ],
  },
  liftoff: {
    host: [
      'And there it goes. {id} is climbing away from the pad.',
      'We have a clean departure. The vehicle is rising and rolling onto its heading.',
      'Off the pad and climbing. That is always the moment the room exhales.',
    ],
  },
  maxq: {
    cohost: [
      'That was Max-Q, the point where the air pushes hardest on the vehicle.',
      'Max-Q is the toughest moment for the structure. From here the air only gets thinner.',
      'Past the peak of aerodynamic pressure. The ride gets smoother now.',
    ],
  },
  meco: {
    host: [
      'Main engines are off. Next up, the stages part ways.',
      'The first stage has done its job. Staging comes next.',
      'Engine cutoff on the booster. Watch for separation.',
    ],
  },
  ses1: {
    cohost: [
      'The upper stage engine is lit. It carries the payload the rest of the way.',
      'Second stage is burning. The booster is on its own now and heading home.',
      'Upper stage ignition. Two vehicles, two jobs from here.',
    ],
  },
  fairing: {
    host: [
      'The nose fairing is off. We are high enough that the payload no longer needs it.',
      'Fairing separation. Those two halves protected the payload through the thick air.',
      'There goes the fairing. The payload sees space for the first time.',
    ],
  },
  entry: {
    cohost: [
      'The booster is falling back into the atmosphere, nose first into the air.',
      'Entry burn region. The booster slows itself before the thick air.',
      'Now the hard part for the booster: coming home in one piece.',
    ],
  },
  touchdown: {
    host: [
      'Touchdown on the deck. What a sight.',
      'And the booster is down. That stage will fly again.',
      'Landing confirmed. The recovery crew will take it from here.',
    ],
  },
  deploy: {
    host: [
      '{payload} is on its own now. Congratulations to the whole team on {id}.',
      'Payload deployed. That is mission success for {id}.',
      'And that is the job done. {payload} is flying free.',
    ],
  },
  abort: {
    host: [
      'We are seeing an anomaly. We will hold here while the team confirms what happened.',
      'That is not the flight we wanted. The team will work through the data.',
      'We have lost the vehicle. Every flight teaches the team something.',
    ],
  },
};

function channelFor(key) {
  return MC_KEYS.has(key) ? CHANNEL.MC : CHANNEL.COACH;
}

function priorityFor(key, channel) {
  if (channel === CHANNEL.COACH) return key.startsWith('health') || key === 'sep' ? PRIORITY.NORMAL : PRIORITY.LOW;
  if (MC_CRITICAL.has(key)) return PRIORITY.CRITICAL;
  if (MC_HIGH.has(key)) return PRIORITY.HIGH;
  return PRIORITY.NORMAL;
}

function buildManifest() {
  const out = Object.create(null);
  for (const [key, variants] of Object.entries(LINES)) {
    const channel = channelFor(key);
    out[key] = {
      id: key,
      lines: [{
        channel,
        variants: variants.slice(),
        priority: priorityFor(key, channel),
        tags: ['arcade', channel === CHANNEL.MC ? 'callout' : 'coaching'],
      }],
    };
  }
  out.abort = { id: 'abort', lines: [] };
  for (const [abortId, a] of Object.entries(ABORTS)) {
    out[`abort-${abortId}`] = {
      id: `abort-${abortId}`,
      lines: [
        { channel: CHANNEL.MC, variants: a.radio.slice(), priority: PRIORITY.CRITICAL, neverDuck: true, tags: ['anomaly', abortId] },
        { channel: CHANNEL.COACH, variants: [a.coach], priority: PRIORITY.LOW, tags: ['anomaly', 'debrief', abortId] },
      ],
    };
  }
  for (const [key, byChannel] of Object.entries(HOST_LINES)) {
    const entry = out[key] || (out[key] = { id: key, lines: [] });
    for (const [ch, variants] of Object.entries(byChannel)) {
      entry.lines.push({ channel: ch, variants, priority: PRIORITY.LOW, tags: ['commentary'] });
    }
  }
  // Host abort commentary applies to every abort-* entry.
  for (const entry of Object.values(out)) {
    if (entry.id.startsWith('abort-')) {
      entry.lines.push({ channel: CHANNEL.HOST, variants: HOST_LINES.abort.host, priority: PRIORITY.LOW, tags: ['anomaly', 'commentary'] });
    }
  }
  return Object.freeze(out);
}

export const MANIFEST = buildManifest();

/** Manifest key for a bus event (aborts fan out by abortId). */
export function manifestKeyFor(ev) {
  if (ev.type === 'abort') return `abort-${ev.payload?.abortId || 'hazard'}`;
  if (ev.type === 'splash' && ev.payload?.abortId) return `abort-${ev.payload.abortId}`;
  return ev.type;
}

export function entryFor(key) {
  return MANIFEST[key] || null;
}

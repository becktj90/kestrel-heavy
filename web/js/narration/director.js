/**
 * Narration director: bus events -> manifest lines -> priority queue.
 * Output-only: never touches game state. Text mode in N1 (captions).
 */
import { fillLine } from '../voice.js';
import { CHANNEL, PRIORITY, entryFor, manifestKeyFor } from './manifest.js';
import { createQueue } from './queue.js';

export function createDirector(bus, opts = {}) {
  const queue = opts.queue || createQueue(opts.queueOpts);
  const cursors = Object.create(null);
  const hosts = opts.hosts !== false;
  let mission = null;
  let anomaly = false;
  const offs = [];

  function pick(key, channel, variants) {
    const k = `${key}|${channel}`;
    const i = (cursors[k] ?? -1) + 1;
    cursors[k] = i % variants.length;
    return variants[cursors[k]];
  }

  function onEvent(ev) {
    if (ev.type === 'radio.line' || ev.type === 'flight.status') return;
    if (ev.type === 'flight.begin') {
      queue.clear();
      anomaly = false;
      mission = { id: ev.payload.missionId, payload: ev.payload.payload, mark: ev.payload.mark };
      return;
    }
    if (ev.type === 'flight.end') return;
    const key = manifestKeyFor(ev);
    const entry = entryFor(key);
    if (!entry) return;
    const isAnomaly = key.startsWith('abort-');
    if (isAnomaly) {
      anomaly = true;
      queue.flushBelow(PRIORITY.CRITICAL);
    }
    for (const line of entry.lines) {
      if (!hosts && (line.channel === CHANNEL.HOST || line.channel === CHANNEL.COHOST)) continue;
      // After an anomaly, only anomaly lines air (no "nominal" chatter).
      if (anomaly && !isAnomaly && line.channel !== CHANNEL.MC) continue;
      const exact = ev.payload?.text && (line.channel === CHANNEL.MC || line.channel === CHANNEL.COACH)
        && line === entry.lines[0];
      const text = exact ? ev.payload.text : fillLine(pick(key, line.channel, line.variants), mission);
      queue.push({ id: key, channel: line.channel, priority: line.priority, neverDuck: line.neverDuck, text, t: ev.t });
    }
  }

  offs.push(bus.on('*', onEvent));

  return {
    queue,
    tick: (t) => queue.tick(t),
    dispose() { offs.forEach((off) => off()); },
  };
}

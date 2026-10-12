/**
 * Narration priority queue (text mode). One line on air at a time.
 *  - Higher priority first; same priority in event order (t, then seq).
 *  - CRITICAL lines preempt whatever is on air (the cut line is dropped),
 *    except a neverDuck (style-guide P0 safety) line, which always finishes.
 *  - Lines older than their staleness window when they would start are
 *    dropped (host/co-host/coach go stale fast; MC lasts longer).
 *  - The same text (or same id+channel) never airs twice in a row inside
 *    repeatWindow seconds.
 *  - Duration comes from reading speed (captions) until real clips exist.
 */
import { CHANNEL, PRIORITY } from './manifest.js';

export const STALE_SEC = Object.freeze({
  [CHANNEL.MC]: 6,
  [CHANNEL.HOST]: 3,
  [CHANNEL.COHOST]: 3,
  [CHANNEL.COACH]: 2.5,
});

/** Reading-speed timing: ~15 chars/s, clamped. */
export function readingSeconds(text, { cps = 15, min = 1.4, max = 7 } = {}) {
  const n = String(text || '').length;
  return Math.min(max, Math.max(min, n / cps + 0.5));
}

export function createQueue(opts = {}) {
  const repeatWindow = opts.repeatWindow ?? 8;
  const staleSec = { ...STALE_SEC, ...(opts.staleSec || {}) };
  const timing = opts.readingSeconds || readingSeconds;
  const pending = [];
  const log = [];
  let current = null;
  let last = null;
  let seq = 0;

  function record(kind, item, t) {
    log.push({ kind, t, id: item.id, channel: item.channel, text: item.text });
    if (log.length > 500) log.shift();
  }

  function isRepeat(item, t) {
    if (!last || t - last.startedAt > repeatWindow) return false;
    return last.text === item.text || (last.id === item.id && last.channel === item.channel);
  }

  function start(item, t) {
    current = { ...item, startedAt: t, endsAt: t + (item.duration ?? timing(item.text)) };
    last = current;
    record('start', current, t);
    return current;
  }

  function sortPending() {
    pending.sort((a, b) => (b.priority - a.priority) || (a.t - b.t) || (a.seq - b.seq));
  }

  return {
    push(item) {
      if (!item || !item.text) return null;
      const entry = {
        id: item.id,
        channel: item.channel || CHANNEL.MC,
        priority: item.priority ?? PRIORITY.NORMAL,
        text: item.text,
        t: Number(item.t) || 0,
        duration: item.duration,
        neverDuck: item.neverDuck === true,
        seq: (seq += 1),
      };
      pending.push(entry);
      sortPending();
      return entry;
    },
    /** Advance to time t. Returns the line on air (or null). */
    tick(t) {
      if (current && t >= current.endsAt) { record('end', current, t); current = null; }
      if (current && pending.length && pending[0].priority === PRIORITY.CRITICAL
        && !current.neverDuck && pending[0].seq > current.seq) {
        record('preempt', current, t);
        current = null;
      }
      while (!current && pending.length) {
        const next = pending.shift();
        const window = next.priority === PRIORITY.CRITICAL ? Math.max(staleSec[next.channel] ?? 6, 6) : (staleSec[next.channel] ?? 3);
        if (t - next.t > window) { record('stale', next, t); continue; }
        if (isRepeat(next, t)) { record('repeat', next, t); continue; }
        start(next, t);
      }
      return current;
    },
    /** Drop everything (flight reset). */
    clear() { pending.length = 0; current = null; last = null; },
    /** Drop non-critical pending lines (e.g. after an anomaly: no "nominal" lines). */
    flushBelow(priority) {
      for (let i = pending.length - 1; i >= 0; i -= 1) if (pending[i].priority < priority) pending.splice(i, 1);
    },
    current: () => current,
    pending: () => pending.slice(),
    log: () => log.slice(),
  };
}

/**
 * Voice cap + pooling for one-shots. acquire() returns a slot or null; when
 * full, a lower-priority (then oldest) voice is stolen via its stop().
 */
export const DEFAULT_CAP = 12;

export function createVoicePool({ cap = DEFAULT_CAP } = {}) {
  const active = [];
  const free = [];
  let seq = 0;
  const stats = { started: 0, stolen: 0, rejected: 0 };

  function release(slot) {
    const i = active.indexOf(slot);
    if (i < 0) return;
    active.splice(i, 1);
    slot.stop = null;
    slot.name = '';
    free.push(slot);
  }

  return {
    cap,
    stats,
    get size() { return active.length; },
    acquire(name, priority = 0) {
      if (active.length >= cap) {
        let victim = null;
        for (const v of active) {
          if (v.priority > priority) continue;
          if (!victim || v.priority < victim.priority || (v.priority === victim.priority && v.seq < victim.seq)) victim = v;
        }
        if (!victim) { stats.rejected += 1; return null; }
        const stop = victim.stop;
        release(victim);
        stats.stolen += 1;
        try { if (stop) stop(); } catch { /* ignore */ }
      }
      const slot = free.pop() || {};
      slot.name = name;
      slot.priority = priority;
      slot.seq = ++seq;
      slot.stop = null;
      slot.release = () => release(slot);
      active.push(slot);
      stats.started += 1;
      return slot;
    },
    release,
    stopAll() { [...active].forEach((s) => { const st = s.stop; release(s); try { if (st) st(); } catch { /* */ } }); },
  };
}

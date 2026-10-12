/**
 * Kestrel Heavy audio core (S1). One instance shared by audio.js.
 * Buses (master/music/sfx/engine/voice) + limiter, voice pool, voice-driven
 * ducking, lifecycle, and the flight-bus adapter.
 */
import { createMixer, BUSES, LIMITER } from './mixer.js';
import { createVoicePool, DEFAULT_CAP } from './voices.js';
import { createDucker, DUCK } from './ducking.js';
import { attachLifecycle } from './lifecycle.js';
import { createBusAdapter } from './events.js';

export { BUSES, LIMITER, DEFAULT_CAP, DUCK };

export function createAudioCore(opts = {}) {
  const mixer = createMixer();
  const voices = createVoicePool({ cap: opts.cap || DEFAULT_CAP });
  const ducker = createDucker(mixer, opts.timers);
  const events = createBusAdapter();
  const eventCounts = Object.create(null);
  let lifecycle = null;
  const listeners = new Set();

  return {
    mixer,
    voices,
    ducker,
    events,
    eventCounts,
    /** Connect the shared flight bus; counts events for S2+ and exposes onEvent. */
    connectBus(bus, types = ['flight.begin', 'flight.end', 'flight.status', 'liftoff', 'maxq', 'meco', 'sep', 'touchdown']) {
      types.forEach((t) => events.on(t, (e) => {
        eventCounts[t] = (eventCounts[t] || 0) + 1;
        listeners.forEach((fn) => { try { fn(e); } catch { /* ignore */ } });
      }));
      return events.connect(bus);
    },
    onEvent(fn) { listeners.add(fn); return () => listeners.delete(fn); },
    attachLifecycle(args) {
      if (lifecycle) lifecycle.detach();
      lifecycle = attachLifecycle(args);
      return lifecycle;
    },
    get lifecycle() { return lifecycle; },
  };
}

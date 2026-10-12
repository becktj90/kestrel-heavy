/** Glue for mission.js: shared bus + director + caption renderer. */
import flightBus from './bus.js';
import { createDirector } from './director.js';
import { createRenderer } from '../subtitles/renderer.js';
import { loadCaptions } from '../subtitles/settings.js';

export function initNarration(bus = flightBus) {
  const prefs = loadCaptions();
  const director = createDirector(bus, { hosts: prefs.hosts });
  const renderer = createRenderer();
  renderer.apply(prefs);
  return {
    bus,
    director,
    renderer,
    tick(t) {
      const line = director.tick(t);
      if (renderer.active) renderer.show(line);
    },
  };
}

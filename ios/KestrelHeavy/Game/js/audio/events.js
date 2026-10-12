/**
 * Tiny adapter onto the shared flight event bus (web/js/narration/bus.js,
 * from N1). Works with any { on(type, fn), off(type, fn) } object; until a bus
 * is connected, subscriptions are queued. Output-only: never mutates sim state.
 */
export function createBusAdapter() {
  let bus = null;
  const subs = [];
  return {
    get connected() { return Boolean(bus); },
    connect(next) {
      if (bus) subs.forEach(([t, f]) => bus.off?.(t, f));
      bus = next && typeof next.on === 'function' ? next : null;
      if (bus) subs.forEach(([t, f]) => bus.on(t, f));
      return Boolean(bus);
    },
    on(type, fn) {
      subs.push([type, fn]);
      if (bus) bus.on(type, fn);
      return () => this.off(type, fn);
    },
    off(type, fn) {
      const i = subs.findIndex(([t, f]) => t === type && f === fn);
      if (i >= 0) subs.splice(i, 1);
      bus?.off?.(type, fn);
    },
  };
}

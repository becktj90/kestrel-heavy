// Designer experience layers. Presentation only: validateEngineering() messages stay exact.
// Simple is the default. Standard unlocks after the first orbit. Engineer is a Settings toggle.
export const LAYERS = Object.freeze(['simple', 'standard', 'engineer']);

const KID_SCHEMA = 'Some of these numbers are outside what the shop can build. Try Make it fly, or turn on Engineer in Settings to see the details.';

export function resolveLayer({ orbitsCompleted = 0, engineerEnabled = false, layer } = {}) {
  const orbits = orbitsCompleted || 0;
  let want = layer || 'simple';
  let locked = null;
  if (want === 'engineer' && !engineerEnabled) {
    locked = 'engineer-toggle';
    want = orbits >= 1 ? 'standard' : 'simple';
  }
  if (want === 'standard' && orbits < 1) {
    locked = 'first-orbit';
    want = 'simple';
  }
  return { layer: want, locked };
}

export function availableLayers({ orbitsCompleted = 0, engineerEnabled = false } = {}) {
  const layers = ['simple'];
  if ((orbitsCompleted || 0) >= 1) layers.push('standard');
  if (engineerEnabled) layers.push('engineer');
  return layers;
}

/** Kid-facing view of a validateEngineering() report. Does not change the raw report. */
export function present(report, layer) {
  const blocks = (report.blocks || []).map((b) => ({ ...b }));
  let warnings = (report.warnings || []).map((w) => ({ ...w }));
  let shown = blocks;
  if (layer !== 'engineer') {
    const nonSchema = blocks.filter((b) => b.code !== 'schema');
    shown = nonSchema.length === 0 && blocks.length > 0
      ? [{ code: 'schema', tab: 'stages', msg: KID_SCHEMA }]
      : nonSchema;
  }
  if (layer === 'simple') warnings = [];
  const next = shown[0]?.msg || warnings[0]?.msg || 'This design is ready to fly.';
  return {
    layer, ok: !!report.ok, blocks: shown, warnings, next,
    showTune: layer === 'engineer', showSchemaText: layer === 'engineer', makeItFly: true,
  };
}

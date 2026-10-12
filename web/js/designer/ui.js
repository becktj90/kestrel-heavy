// DESIGN screen: HTML overlay over the Phaser canvas. Plain DOM, no framework.
// Launch is disabled until M3 (flight from designs). Does not touch flight code.
import { ENGINES, ENGINE_BY_ID } from '../data/engines.js';
import { PROPELLANTS, PROPELLANT_BY_ID } from '../data/propellants.js';
import { MATERIALS } from '../data/materials.js';
import { PAYLOADS, PAYLOAD_BY_ID, ORBITS } from '../data/payloads.js';
import { PRESETS, PRESET_BY_ID } from '../data/presets.js';
import { NOSE_SHAPES, MAX_STAGES, STRAPON_COUNTS, defaultStage, sizeTank, migrateDesign } from '../vehicle/design.js';
import { pressureAt } from '../physics/atmosphere.js';
import { quickReadout, simReadout, thrustCurve, TUNE_LIMIT } from './readout.js';
import { validateEngineering, makeItFly } from './validate.js';
import { TIPS } from './glossary.js';
import { getDesignStore } from './store.js';
import { costPanel } from '../ui/finance/costPanel.js';
import { STORAGE_KEY } from '../config.js';
import { flightResult, hapticPattern, previewDelta, pulseHaptic } from './feel.js';

const TABS = [
  ['stages', 'Stages'], ['strapons', 'Strap-ons'], ['engines', 'Engines'], ['propellants', 'Propellants'],
  ['tanks', 'Tanks'], ['payload', 'Payload'], ['aero', 'Aero'], ['recovery', 'Recovery'],
  ['avionics', 'Avionics'], ['safety', 'Safety'], ['livery', 'Livery'],
];
const SIM_DEBOUNCE_MS = 300;

const h = (tag, attrs = {}, ...kids) => {
  const el = document.createElement(tag);
  for (const [k, v] of Object.entries(attrs)) {
    if (v == null || v === false) continue;
    if (k === 'class') el.className = v;
    else if (k.startsWith('on')) el.addEventListener(k.slice(2), v);
    else if (k === 'text') el.textContent = v;
    else el.setAttribute(k, v === true ? '' : v);
  }
  for (const c of kids.flat()) if (c != null && c !== false) el.append(c.nodeType ? c : document.createTextNode(String(c)));
  return el;
};
const fmtN = (n, d = 0) => (Number.isFinite(n) ? n.toLocaleString('en-US', { maximumFractionDigits: d, minimumFractionDigits: d }) : '—');
const kms = (v) => `${fmtN(v / 1000, 2)} km/s`;
const tonnes = (kg) => `${fmtN(kg / 1000, kg < 10000 ? 2 : 1)} t`;

function tip(key) {
  const t = TIPS[key];
  if (!t) return null;
  const b = h('button', { type: 'button', class: 'kd-tip', 'aria-label': `What is this? ${t}`, title: t, text: '?' });
  b.addEventListener('click', (e) => { e.stopPropagation(); const open = b.classList.toggle('is-open'); b.dataset.tip = open ? t : ''; });
  b.dataset.tip = '';
  return b;
}

export class DesignerScreen {
  constructor(root = document.body) {
    this.root = root;
    this.design = migrateDesign(PRESET_BY_ID.kid);
    this.savedId = null;
    this.tab = 'stages';
    this.stageIdx = 0;
    this.simTimer = 0;
    this.sim = null;
    this.el = null;
    this.message = '';
  }

  async open() {
    this.store = await getDesignStore();
    if (!this.el) this.build();
    this.el.hidden = false;
    document.body.classList.add('kd-open');
    this.refresh();
    this.el.querySelector('.kd-close')?.focus();
    if (!this.galleryShown) { this.galleryShown = true; this.showGallery(); }
  }

  // T1: "New Design" gallery (designer/gallery.js). Shown on first open; "New" reopens it.
  showGallery() { return import('./gallery.js').then((g) => g.openGallery(this)); }

  close() {
    if (!this.el) return;
    this.el.hidden = true;
    document.body.classList.remove('kd-open');
    this.onClose?.();
  }

  build() {
    this.el = h('section', { id: 'kd-designer', class: 'kd', role: 'dialog', 'aria-modal': 'true', 'aria-label': 'Rocket designer' });
    this.header = h('header', { class: 'kd-top' });
    this.schem = h('div', { class: 'kd-schem', 'aria-label': 'Vehicle schematic' });
    this.tabsEl = h('nav', { class: 'kd-tabs', role: 'tablist' });
    this.panel = h('div', { class: 'kd-panel', role: 'tabpanel' });
    this.readEl = h('aside', { class: 'kd-readout', 'aria-live': 'polite' });
    this.el.append(this.header, h('div', { class: 'kd-body' }, this.schem, h('div', { class: 'kd-edit' }, this.tabsEl, this.panel), this.readEl));
    this.root.append(this.el);
    this.el.addEventListener('keydown', (e) => { if (e.key === 'Escape') this.close(); });
  }

  // ---------- state ----------
  set(mut) {
    this._before = this.quick || null;
    mut(this.design);
    this.sim = null;
    this._snap = true;
    this.refresh();
  }
  load(design, id = null) { this.design = migrateDesign(design); this.savedId = id; this.stageIdx = 0; this.sim = null; this.refresh(); }

  refresh() {
    this.stageIdx = Math.min(this.stageIdx, this.design.stages.length - 1);
    this.validation = validateEngineering(this.design);
    this.quick = this.validation.readout;
    if (this._snap) {
      this._delta = previewDelta(this._before, this.quick);
      this._snap = false;
      this._doSnap = true;
    }
    this.renderHeader(); this.renderTabs(); this.renderPanel(); this.renderSchematic(); this.renderReadout();
    if (this._doSnap) {
      this._doSnap = false;
      this.kickSnap();
    }
    clearTimeout(this.simTimer);
    if (this.quick && this.validation.ok) this.simTimer = setTimeout(() => this.runSim(), SIM_DEBOUNCE_MS);
  }

  kickSnap() {
    const node = this.schem;
    if (node) {
      node.classList.remove('is-snap');
      void node.offsetWidth;
      node.classList.add('is-snap');
      clearTimeout(this._snapT);
      this._snapT = setTimeout(() => node.classList.remove('is-snap'), 220);
    }
    pulseHaptic(hapticPattern('snap'));
    const before = this._before;
    const next = this.quick;
    if (before && next && before.margin < 0 && next.margin >= 0) {
      pulseHaptic(hapticPattern('payoff'));
      this.toast('Clears the Δv budget. That is the payoff.');
    }
  }

  lastArcadeFlight() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      const parsed = raw ? JSON.parse(raw) : null;
      return parsed?.bestFlight || null;
    } catch {
      return null;
    }
  }

  runSim() {
    const prev = this.sim?.outcome;
    try { this.sim = simReadout(this.design, this.quick); } catch (_) { this.sim = null; }
    this.renderReadout(); this.renderSchematic();
    if (prev && prev !== 'orbit' && this.sim?.outcome === 'orbit') pulseHaptic(hapticPattern('payoff'));
  }

  // ---------- header ----------
  renderHeader() {
    const presetSel = h('select', { class: 'kd-in', 'aria-label': 'Load preset', onchange: (e) => { if (e.target.value) { this.load(PRESET_BY_ID[e.target.value]); this.toast(`Loaded ${PRESET_BY_ID[e.target.value].name}`); } } },
      h('option', { value: '', text: 'Presets…' }), PRESETS.map((p) => h('option', { value: p.id, text: p.name })));
    const name = h('input', { class: 'kd-in kd-name', value: this.design.name, 'aria-label': 'Design name', maxlength: '40', onchange: (e) => this.rename(e.target.value) });
    this.header.replaceChildren(...[
      h('div', { class: 'kd-title' }, h('span', { class: 'kd-kicker', text: 'DESIGN · BLUEPRINT' }), name),
      h('button', { type: 'button', class: 'kd-btn kd-close', 'aria-label': 'Close designer', onclick: () => this.close() }, '✕'),
      h('div', { class: 'kd-actions' },
        presetSel,
        h('button', { type: 'button', class: 'kd-btn', onclick: () => this.showGallery() }, 'New'),
        h('button', { type: 'button', class: 'kd-btn', onclick: () => this.save() }, 'Save'),
        h('button', { type: 'button', class: 'kd-btn', onclick: () => this.showLoad() }, 'Load'),
        h('button', { type: 'button', class: 'kd-btn', 'aria-label': 'Duplicate', onclick: () => this.duplicate() }, 'Copy')),
      this.message ? h('p', { class: 'kd-toast', role: 'status', text: this.message }) : null,
    ].filter(Boolean));
  }

  toast(m) { this.message = m; this.renderHeader(); clearTimeout(this.toastT); this.toastT = setTimeout(() => { this.message = ''; this.renderHeader(); }, 2500); }

  save() {
    try { const r = this.store.designs.put({ ...this.design }, { id: this.savedId || undefined, name: this.design.name }); this.savedId = r.id; this.toast(`Saved “${this.design.name}”`); }
    catch (e) { this.toast(`Couldn't save (${e.code || e.message}). Try freeing space.`); }
  }
  rename(nm) {
    const name = String(nm).trim().slice(0, 40) || 'Untitled';
    this.design.name = name;
    if (this.savedId) { try { this.store.designs.rename(this.savedId, name); } catch (_) { /* unsaved */ } }
    this.toast(`Renamed to “${name}”`);
  }
  duplicate() {
    if (!this.savedId) this.save();
    try { const r = this.store.designs.duplicate(this.savedId, `${this.design.name} copy`); this.savedId = r.id; this.design.name = `${this.design.name} copy`; this.refresh(); this.toast('Duplicated'); }
    catch (e) { this.toast(`Couldn't duplicate (${e.message})`); }
  }
  showLoad() {
    const list = this.store.designs.list();
    const sheet = h('div', { class: 'kd-sheet', role: 'dialog', 'aria-label': 'Saved designs' });
    const done = () => sheet.remove();
    sheet.append(h('div', { class: 'kd-sheet-card' },
      h('h3', { text: 'Saved designs' }),
      list.length ? h('ul', { class: 'kd-list' }, list.map((m) => h('li', {},
        h('button', { type: 'button', class: 'kd-btn kd-wide', onclick: () => { const d = this.store.designs.get(m.id); if (d) this.load({ ...d, name: m.name }, m.id); done(); this.toast(`Loaded “${m.name}”`); } }, m.name),
        h('button', { type: 'button', class: 'kd-btn', 'aria-label': `Delete ${m.name}`, onclick: () => { this.store.designs.delete(m.id); if (this.savedId === m.id) this.savedId = null; done(); this.showLoad(); } }, '🗑')))) : h('p', { text: 'No saved designs yet. Tap Save to keep this one.' }),
      h('button', { type: 'button', class: 'kd-btn', onclick: done }, 'Close')));
    this.el.append(sheet);
  }

  // ---------- tabs ----------
  renderTabs() {
    const badge = (id) => [...this.validation.blocks, ...this.validation.warnings].some((x) => x.tab === id);
    this.tabsEl.replaceChildren(...TABS.map(([id, label]) => h('button', { type: 'button', role: 'tab', class: `kd-tab${this.tab === id ? ' is-on' : ''}${badge(id) ? ' has-note' : ''}`, 'aria-selected': String(this.tab === id), onclick: () => { this.tab = id; this.refresh(); } }, label)));
  }

  stagePicker() {
    const st = this.design.stages;
    return h('div', { class: 'kd-chips', role: 'group', 'aria-label': 'Stage' },
      st.map((s, i) => h('button', { type: 'button', class: `kd-chip${i === this.stageIdx ? ' is-on' : ''}`, onclick: () => { this.stageIdx = i; this.refresh(); } }, `S${i + 1}`)));
  }

  num(label, value, onSet, { min = 0, max = 1e9, step = 1, unit = '', tipKey, scale = 1, digits = 0 } = {}) {
    const shown = value / scale;
    const commit = (v) => { const n = Math.max(min, Math.min(max, Number(v))); if (Number.isFinite(n)) this.set(() => onSet(n * scale)); };
    const input = h('input', { class: 'kd-in kd-num', type: 'number', inputmode: 'decimal', value: +shown.toFixed(digits), min, max, step, 'aria-label': label, onchange: (e) => commit(e.target.value) });
    return h('label', { class: 'kd-field' }, h('span', { class: 'kd-lab' }, label, unit ? h('small', { text: ` (${unit})` }) : null, tipKey ? tip(tipKey) : null),
      h('span', { class: 'kd-step' },
        h('button', { type: 'button', class: 'kd-btn kd-pm', 'aria-label': `Less ${label}`, onclick: (e) => { e.preventDefault(); commit(shown - step); } }, '−'),
        input,
        h('button', { type: 'button', class: 'kd-btn kd-pm', 'aria-label': `More ${label}`, onclick: (e) => { e.preventDefault(); commit(shown + step); } }, '+')));
  }
  sel(label, value, options, onSet, tipKey) {
    return h('label', { class: 'kd-field' }, h('span', { class: 'kd-lab' }, label, tipKey ? tip(tipKey) : null),
      h('select', { class: 'kd-in', 'aria-label': label, onchange: (e) => this.set(() => onSet(e.target.value)) },
        options.map(([v, t]) => h('option', { value: v, text: t, selected: String(v) === String(value) }))));
  }
  toggle(label, on, onSet, tipKey) {
    return h('label', { class: 'kd-field kd-toggle' }, h('span', { class: 'kd-lab' }, label, tipKey ? tip(tipKey) : null),
      h('button', { type: 'button', role: 'switch', 'aria-checked': String(!!on), class: `kd-btn kd-sw${on ? ' is-on' : ''}`, onclick: (e) => { e.preventDefault(); this.set(() => onSet(!on)); } }, on ? 'ON' : 'OFF'));
  }

  renderPanel() {
    const d = this.design, s = d.stages[this.stageIdx];
    const P = [];
    const engineOpts = (prop) => ENGINES.filter((e) => !prop || e.propellant === prop).map((e) => [e.id, e.label]);
    switch (this.tab) {
      case 'stages':
        P.push(h('p', { class: 'kd-hint', text: `${d.stages.length} stage${d.stages.length > 1 ? 's' : ''}. Stage 1 fires first.` }),
          h('div', { class: 'kd-row' },
            h('button', { type: 'button', class: 'kd-btn', disabled: d.stages.length >= MAX_STAGES, onclick: () => this.set((x) => { x.stages.push(defaultStage({ name: `Stage ${x.stages.length + 1}`, engine: 'gg-kerolox-vac', propMass: 15000, diameter: x.stages.at(-1).diameter })); this.stageIdx = x.stages.length - 1; }) }, '+ Add stage'),
            h('button', { type: 'button', class: 'kd-btn', disabled: d.stages.length <= 1, onclick: () => this.set((x) => { x.stages.splice(this.stageIdx, 1); }) }, `Remove S${this.stageIdx + 1}`)),
          this.stagePicker(),
          h('label', { class: 'kd-field' }, h('span', { class: 'kd-lab', text: 'Stage name' }), h('input', { class: 'kd-in', value: s.name, maxlength: '24', onchange: (e) => this.set(() => { s.name = e.target.value; }) })),
          this.num('Diameter', s.diameter, (v) => { s.diameter = v; }, { min: 0.3, max: 15, step: 0.1, unit: 'm', digits: 1 }),
          this.num('Interstage mass', s.interstageMass || 0, (v) => { s.interstageMass = v; }, { max: 50000, step: 100, unit: 'kg' }));
        break;
      case 'strapons': {
        const so = d.strapOns;
        P.push(h('p', { class: 'kd-hint' }, 'Side boosters ', tip('strapons')),
          this.sel('Count', so.count, STRAPON_COUNTS.map((n) => [n, n ? `${n} boosters` : 'None']), (v) => { so.count = Number(v); if (so.count && !(so.propMass > 0)) so.propMass = 30000; }));
        if (so.count) P.push(
          this.sel('Kind', so.kind, [['solid', 'Solid'], ['liquid', 'Liquid']], (v) => { so.kind = v; if (v === 'solid') { so.propellant = 'solid-apcp'; so.engine = 'srm-small'; so.crossfeed = false; } else { so.propellant = 'kerolox'; so.engine = 'gg-kerolox'; } }),
          this.sel('Engine', so.engine, engineOpts(so.propellant), (v) => { so.engine = v; }),
          this.num('Engines each', so.engineCount, (v) => { so.engineCount = Math.round(v); }, { min: 1, max: 40 }),
          this.num('Propellant each', so.propMass, (v) => { so.propMass = v; }, { min: 0, max: 3e6, step: 5, unit: 't', scale: 1000, digits: 1 }),
          this.num('Diameter', so.diameter, (v) => { so.diameter = v; }, { min: 0.3, max: 15, step: 0.1, unit: 'm', digits: 1 }),
          so.kind === 'liquid' ? this.toggle('Crossfeed', so.crossfeed, (v) => { so.crossfeed = v; }, 'crossfeed') : null);
        break;
      }
      case 'engines': {
        const e = ENGINE_BY_ID[s.engine];
        const tune = s.tune || {};
        P.push(this.stagePicker(),
          this.num('Engine count', s.engineCount, (v) => { s.engineCount = Math.round(v); }, { min: 1, max: 40 }),
          h('div', { class: 'kd-cards', role: 'radiogroup', 'aria-label': 'Engine catalog' }, ENGINES.filter((x) => x.propellant !== 'solid-apcp' || this.stageIdx === 0).map((x) =>
            h('button', { type: 'button', role: 'radio', 'aria-checked': String(x.id === s.engine), class: `kd-card${x.id === s.engine ? ' is-on' : ''}`, onclick: () => this.set(() => { s.engine = x.id; s.propellant = x.propellant; delete s.tune; }) },
              h('b', { text: x.label }), h('span', { text: `${fmtN(x.thrustVac / 1000)} kN vac · Isp ${x.ispVac}s · ${x.mass} kg` }), h('small', { text: `${PROPELLANT_BY_ID[x.propellant].label} · ${x.cycle}` })))),
          e ? h('div', { class: 'kd-sub' }, h('h4', {}, 'Tune ', tip('tune')),
            this.num('Thrust', tune.thrustPct || 0, (v) => { s.tune = { ...(s.tune || {}), thrustPct: v }; }, { min: -TUNE_LIMIT, max: TUNE_LIMIT, unit: '%' }),
            this.num('Isp', tune.ispPct || 0, (v) => { s.tune = { ...(s.tune || {}), ispPct: v }; }, { min: -TUNE_LIMIT, max: TUNE_LIMIT, unit: '%' }),
            h('p', { class: 'kd-hint', text: `Per engine: ${fmtN(e.thrustVac * (1 + (tune.thrustPct || 0) / 100) / 1000)} kN vac, Isp ${fmtN(e.ispVac * (1 + (tune.ispPct || 0) / 100))} s vac / ${fmtN(e.ispSL * (1 + (tune.ispPct || 0) / 100))} s SL, throttle ${Math.round(e.throttle[0] * 100)}–${Math.round(e.throttle[1] * 100)}%` })) : null);
        break;
      }
      case 'propellants':
        P.push(this.stagePicker(),
          this.sel('Propellant', s.propellant, PROPELLANTS.map((p) => [p.id, p.label]), (v) => { s.propellant = v; const e = ENGINES.find((x) => x.propellant === v && (this.stageIdx === 0 ? x.thrustSL > 0 : true)); if (ENGINE_BY_ID[s.engine]?.propellant !== v && e) s.engine = e.id; }, 'isp'),
          this.num('Propellant load', s.propMass, (v) => { s.propMass = v; }, { min: 0, max: 5e6, step: 5, unit: 't', scale: 1000, digits: 1 }),
          h('p', { class: 'kd-hint', text: (PROPELLANT_BY_ID[s.propellant]?.handling || []).join(' · ') }));
        break;
      case 'tanks': {
        const t = sizeTank(s.propellant, s.propMass, s.diameter);
        P.push(this.stagePicker(),
          this.sel('Material', s.material, MATERIALS.map((m) => [m.id, m.label]), (v) => { s.material = v; }, 'material'),
          this.num('Diameter', s.diameter, (v) => { s.diameter = v; }, { min: 0.3, max: 15, step: 0.1, unit: 'm', digits: 1 }),
          h('p', { class: 'kd-hint', text: `Tank volume ${fmtN(t.volume, 1)} m³ · length ${fmtN(t.length, 1)} m · dry ${tonnes(this.quick?.mass.stages[this.stageIdx]?.dry ?? 0)}` }));
        break;
      }
      case 'payload': {
        const pl = d.payload, f = d.fairing;
        P.push(this.sel('Payload', pl.type, PAYLOADS.map((p) => [p.id, p.label]), (v) => { pl.type = v; pl.mass = PAYLOAD_BY_ID[v].mass; pl.orbit = PAYLOAD_BY_ID[v].orbit; }),
          this.num('Payload mass', pl.mass, (v) => { pl.mass = v; }, { min: 0, max: 200000, step: 50, unit: 'kg' }),
          this.sel('Target orbit', pl.orbit, Object.values(ORBITS).map((o) => [o.id, o.label]), (v) => { pl.orbit = v; }),
          h('h4', {}, 'Fairing ', tip('fairing')),
          this.num('Fairing diameter', f.diameter, (v) => { f.diameter = v; }, { min: 0.3, max: 15, step: 0.1, unit: 'm', digits: 1 }),
          this.num('Fairing length', f.length, (v) => { f.length = v; }, { min: 0.5, max: 40, step: 0.5, unit: 'm', digits: 1 }),
          this.sel('Fairing material', f.material, MATERIALS.map((m) => [m.id, m.label]), (v) => { f.material = v; }),
          this.num('Jettison altitude', f.jettisonAltitudeKm, (v) => { f.jettisonAltitudeKm = v; }, { min: 60, max: 200, step: 5, unit: 'km' }));
        break;
      }
      case 'aero':
        P.push(this.sel('Nose shape', d.aero.nose, Object.keys(NOSE_SHAPES).map((k) => [k, `${k} (Cd ${NOSE_SHAPES[k]})`]), (v) => { d.aero.nose = v; }, 'nose'),
          this.toggle('Fins', d.aero.fins, (v) => { d.aero.fins = v; }));
        break;
      case 'recovery':
        P.push(this.toggle('Landing legs', d.landing.legs, (v) => { d.landing.legs = v; }),
          this.toggle('Grid fins', d.landing.gridFins, (v) => { d.landing.gridFins = v; }, 'gridFins'),
          this.toggle('Cold-gas RCS', d.landing.rcs, (v) => { d.landing.rcs = v; }),
          this.num('Stage 1 landing reserve', d.stages[0].landingPropMass || 0, (v) => { d.stages[0].landingPropMass = Math.min(v, d.stages[0].propMass * 0.3); }, { min: 0, max: 1e6, step: 1, unit: 't', scale: 1000, digits: 1, tipKey: 'reserve' }));
        break;
      case 'avionics':
        P.push(this.sel('Avionics', d.avionics.tier, [['basic', 'Basic (60 kg)'], ['standard', 'Standard (150 kg)'], ['redundant', 'Redundant (300 kg)']], (v) => { d.avionics.tier = v; }, 'avionics'));
        break;
      case 'safety':
        P.push(h('label', { class: 'kd-field' }, h('span', { class: 'kd-lab' }, `Safety factor ${d.safetyFactor.toFixed(2)}`, tip('safety')),
          h('input', { type: 'range', class: 'kd-range', min: '1', max: '3', step: '0.05', value: d.safetyFactor, 'aria-label': 'Safety factor', onchange: (e) => this.set(() => { d.safetyFactor = Number(e.target.value); }) })),
          h('p', { class: 'kd-hint', text: 'Real rockets fly around 1.25–1.4. Lower is lighter but riskier.' }));
        break;
      case 'livery':
        P.push(h('label', { class: 'kd-field' }, h('span', { class: 'kd-lab', text: 'Primary' }), h('input', { type: 'color', class: 'kd-in kd-color', value: d.livery.primary, onchange: (e) => this.set(() => { d.livery.primary = e.target.value; }) })),
          h('label', { class: 'kd-field' }, h('span', { class: 'kd-lab', text: 'Accent' }), h('input', { type: 'color', class: 'kd-in kd-color', value: d.livery.accent || '#1a3c6e', onchange: (e) => this.set(() => { d.livery.accent = e.target.value; }) })),
          h('label', { class: 'kd-field' }, h('span', { class: 'kd-lab', text: 'Side lettering' }), h('input', { class: 'kd-in', maxlength: '16', value: d.livery.name || '', onchange: (e) => this.set(() => { d.livery.name = e.target.value.toUpperCase(); }) })));
        break;
      default:
    }
    this.panel.replaceChildren(...P.filter(Boolean));
  }

  // ---------- schematic + plot ----------
  renderSchematic() {
    const d = this.design, NS = 'http://www.w3.org/2000/svg';
    const parts = d.stages.map((s) => ({ s, len: Math.max(1, sizeTank(s.propellant, s.propMass, s.diameter).length + (ENGINE_BY_ID[s.engine]?.diameter || 1) * 1.2) }));
    const fl = d.fairing.length;
    const total = parts.reduce((a, p) => a + p.len, 0) + fl;
    const maxD = Math.max(...d.stages.map((s) => s.diameter), d.fairing.diameter) + (d.strapOns.count ? d.strapOns.diameter * 2.2 : 0);
    const W = 120, H = 300, k = Math.min((H - 20) / total, (W - 10) / maxD);
    const svg = document.createElementNS(NS, 'svg');
    svg.setAttribute('viewBox', `0 0 ${W} ${H}`); svg.setAttribute('class', 'kd-svg'); svg.setAttribute('role', 'img');
    svg.setAttribute('aria-label', `${d.stages.length}-stage vehicle${d.strapOns.count ? ` with ${d.strapOns.count} strap-ons` : ''}`);
    if (this.sim?.outcome === 'orbit') svg.classList.add('is-orbit');
    const add = (tag, a) => { const e = document.createElementNS(NS, tag); for (const [x, v] of Object.entries(a)) e.setAttribute(x, v); svg.append(e); return e; };
    for (let gx = 0; gx <= W; gx += 10) add('line', { x1: gx, y1: 0, x2: gx, y2: H, class: 'kd-grid' });
    for (let gy = 0; gy <= H; gy += 10) add('line', { x1: 0, y1: gy, x2: W, y2: gy, class: 'kd-grid' });
    let y = H - 10; const cx = W / 2;
    parts.forEach(({ s, len }, i) => {
      const w = s.diameter * k, hh = len * k;
      add('rect', { x: cx - w / 2, y: y - hh, width: w, height: hh, class: `kd-part${i === this.stageIdx ? ' is-sel' : ''}`, fill: i === 0 ? d.livery.primary : d.livery.primary });
      const ne = Math.min(s.engineCount, 9);
      for (let j = 0; j < ne; j++) add('path', { d: `M${cx - w / 2 + (j + 0.5) * w / ne - 1.5},${y} l3,0 l1.5,4 l-6,0 z`, class: 'kd-nozzle' });
      add('text', { x: cx + w / 2 + 2, y: y - hh / 2, class: 'kd-lbl' }).textContent = `S${i + 1}`;
      if (i === 0 && d.strapOns.count) {
        const sw = d.strapOns.diameter * k, sh = hh * 0.75;
        for (const side of [-1, 1]) add('rect', { x: side < 0 ? cx - w / 2 - sw - 1 : cx + w / 2 + 1, y: y - sh, width: sw, height: sh, class: 'kd-part kd-strap' });
      }
      if (i === 0 && d.landing.legs) for (const side of [-1, 1]) add('line', { x1: cx + side * w / 2, y1: y - 6, x2: cx + side * (w / 2 + 5), y2: y + 3, class: 'kd-leg' });
      y -= hh;
    });
    const fw = d.fairing.diameter * k, fh = fl * k;
    add('path', { d: `M${cx - fw / 2},${y} L${cx - fw / 2},${y - fh * 0.45} Q${cx - fw / 2},${y - fh} ${cx},${y - fh} Q${cx + fw / 2},${y - fh} ${cx + fw / 2},${y - fh * 0.45} L${cx + fw / 2},${y} Z`, class: 'kd-part kd-fairing', fill: d.livery.accent || '#1a3c6e' });
    // thrust plot
    const plot = h('div', { class: 'kd-plot' }, h('span', { class: 'kd-kicker', text: 'THRUST vs TIME' }));
    const curve = this.sim && this.quick ? thrustCurve(this.quick, this.sim, pressureAt) : [];
    if (curve.length > 1) {
      const pw = 160, ph = 60, tMax = curve.at(-1).t || 1, fMax = Math.max(...curve.map((c) => c.F)) || 1;
      const ps = document.createElementNS(NS, 'svg'); ps.setAttribute('viewBox', `0 0 ${pw} ${ph}`); ps.setAttribute('class', 'kd-plotsvg'); ps.setAttribute('role', 'img');
      ps.setAttribute('aria-label', `Peak thrust ${fmtN(fMax / 1000)} kN over ${fmtN(tMax)} s`);
      const pl = document.createElementNS(NS, 'polyline');
      pl.setAttribute('points', curve.map((c) => `${(c.t / tMax * pw).toFixed(1)},${(ph - 2 - c.F / fMax * (ph - 6)).toFixed(1)}`).join(' '));
      ps.append(pl); plot.append(ps, h('small', { text: `peak ${fmtN(fMax / 1000)} kN · ${fmtN(tMax)} s` }));
    } else plot.append(h('small', { text: this.validation.ok ? 'computing…' : 'fix the blocks to plot' }));
    this.schem.replaceChildren(svg, plot);
  }

  // ---------- readout ----------
  renderReadout() {
    const v = this.validation, r = this.quick, sim = this.sim;
    const rows = [];
    const flight = flightResult(sim, r);
    const last = this.lastArcadeFlight();
    const delta = this._delta;
    rows.push(h('section', { class: `kd-flight${flight?.outcome === 'orbit' ? ' is-orbit' : ''}` },
      h('span', { class: 'kd-kicker', text: 'FLIGHT RESULT' }),
      h('b', { class: 'kd-outcome', text: flight ? String(flight.outcome).toUpperCase() : (v.ok ? 'SIM RUNNING' : 'GROUNDED') }),
      flight
        ? h('p', { text: `Apogee ${fmtN(flight.apogeeKm)} km · losses ${kms(flight.totalLosses)} · expended ${kms(flight.deltaVExpended)} of ${kms(flight.idealDeltaV)} ideal` })
        : h('p', { text: 'Headless sim fills this card from the integrator.' }),
      delta
        ? h('p', { class: 'kd-delta', text: `Live  Δv ${delta.deltaV >= 0 ? '+' : ''}${fmtN(delta.deltaV / 1000, 2)} km/s · TWR ${delta.twr >= 0 ? '+' : ''}${fmtN(delta.twr, 2)} · mass ${delta.mass >= 0 ? '+' : ''}${fmtN(delta.mass / 1000, 2)} t` })
        : null,
      last
        ? h('p', { class: 'kd-last', text: `Last arcade flight  ${last.mission || 'KH'}  ${last.recovered ? 'RECOVERED' : (last.reason || 'flown')}  ·  score ${Number(last.score || 0).toLocaleString()}` })
        : h('p', { class: 'kd-last', text: 'No arcade flight yet. This card is the build’s own result.' })));
    if (r) {
      const marginCls = r.margin >= 0.05 ? 'good' : r.margin >= 0 ? 'warn' : 'bad';
      rows.push(
        h('div', { class: 'kd-big' }, h('span', {}, 'Δv ', tip('dv')), h('b', { text: kms(r.totalDeltaV) })),
        h('div', { class: `kd-margin ${marginCls}` },
          h('span', {}, `Need ${kms(r.required)} for ${r.orbit.label} `, tip('margin')),
          h('div', { class: 'kd-bar', role: 'meter', 'aria-valuenow': String(Math.round(r.margin * 100)), 'aria-label': 'Delta-v margin' }, h('i', { style: `width:${Math.max(2, Math.min(100, 50 + r.margin * 250))}%` })),
          h('b', { text: `${r.margin >= 0 ? '+' : ''}${fmtN(r.margin * 100, 0)}% margin` })),
        h('div', { class: 'kd-kv' },
          h('span', {}, 'Liftoff TWR ', tip('twr')), h('b', { class: r.liftoffTWR <= 1 ? 'bad' : r.liftoffTWR < 1.25 ? 'warn' : 'good', text: fmtN(r.liftoffTWR, 2) }),
          h('span', { text: 'Liftoff mass' }), h('b', { text: tonnes(r.liftoffMass) }),
          h('span', {}, 'Losses ', tip('losses')), h('b', { text: sim ? kms(sim.totalLosses) : '…' }),
          h('span', {}, 'Sim apogee ', tip('apogee')), h('b', { text: sim ? `${fmtN(sim.apogeeKm)} km (${sim.outcome})` : '…' }),
          h('span', {}, 'Reliability ', tip('reliability')), h('b', { text: `${fmtN(r.reliability * 100, 1)}%` })),
        costPanel(h, this.design, r, { tip, rerender: () => this.renderReadout() }),
        h('table', { class: 'kd-table' },
          h('thead', {}, h('tr', {}, ['Phase', 'Δv km/s', 'TWR', 'Burn s', 'ṁ kg/s', 'MR'].map((t) => h('th', { text: t })))),
          h('tbody', {}, r.stages.map((s) => h('tr', {}, h('td', { text: s.label }), h('td', { text: fmtN(s.deltaV / 1000, 2) }), h('td', { text: fmtN(s.twr, 2) }), h('td', { text: fmtN(s.burnTime) }), h('td', { text: fmtN(s.mdot) }), h('td', { text: fmtN(s.massRatio, 2) }))))));
    }
    const notes = h('ul', { class: 'kd-notes' },
      v.blocks.map((b) => h('li', { class: 'bad' }, h('button', { type: 'button', class: 'kd-link', onclick: () => { this.tab = b.tab; if (b.index != null) this.stageIdx = b.index; this.refresh(); } }, b.msg))),
      v.warnings.map((w) => h('li', { class: 'warn' }, h('button', { type: 'button', class: 'kd-link', onclick: () => { this.tab = w.tab; if (w.index != null) this.stageIdx = w.index; this.refresh(); } }, w.msg))),
      !v.blocks.length && !v.warnings.length ? h('li', { class: 'good', text: 'Looks flyable. Nice work!' }) : null);
    const needsFix = v.blocks.length || v.warnings.some((w) => w.code === 'dv-margin' || w.code === 'twr-marginal');
    rows.push(notes,
      h('div', { class: 'kd-row kd-flyrow' },
        h('button', { type: 'button', class: 'kd-btn kd-fly', disabled: !needsFix, onclick: () => { const before = this.quick; const f = makeItFly(this.design); this._before = before; this.design = f.design; this.sim = null; this._snap = true; this.refresh(); this.toast(f.changes.length ? `Made it fly: ${f.changes.slice(0, 3).join(', ')}${f.changes.length > 3 ? '…' : ''}` : 'Already flyable!'); } }, '✨ Make it fly'),
        h('button', { type: 'button', class: 'kd-btn kd-launch', disabled: true, 'aria-disabled': 'true', title: 'Flying your own designs is coming in M3' }, 'LAUNCH')),
      h('p', { class: 'kd-hint', text: 'Flying custom designs is coming in M3.' }));
    this.readEl.replaceChildren(...rows);
  }
}

let screen = null;
export function openDesigner(opts = {}) {
  if (!screen) screen = new DesignerScreen(opts.root || document.querySelector('#arcade-fs-wrapper') || document.body);
  screen.onClose = opts.onClose;
  return screen.open().then(() => screen);
}

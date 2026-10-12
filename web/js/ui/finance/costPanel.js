// Compact funds bar + cost breakdown for the DESIGN readout (E1). Plain DOM.
// In 2.0 this is a cost readout only (Sandbox): no balance is charged.
import { CATEGORIES, formatKc, marginalHints, MINOR_PER_KC } from '../../economy/costModel.js';
import flightBus from '../../narration/bus.js';

export const CATEGORY_LABEL = Object.freeze({ engines: 'Engines', structure: 'Structure', propellant: 'Propellant', avionics: 'Avionics', recovery: 'Recovery', ops: 'Ops & range' });
export const CATEGORY_COLOR = Object.freeze({ engines: '#ff8a3d', structure: '#7fb8ff', propellant: '#59d38c', avionics: '#c792ea', recovery: '#ffd166', ops: '#9aa5b1' });

let lastQuote = null;
const sortState = { key: 'amount', dir: -1 };

function perKgText(price) {
  if (price.perKg == null) return '—';
  const kc = price.perKg / MINOR_PER_KC;
  return `${kc.toLocaleString('en-US', { maximumFractionDigits: kc < 10 ? 2 : 1, minimumFractionDigits: kc < 10 ? 2 : 1 })} Kc/kg`;
}

/** Emit econ.quote on the shared bus when the per-launch price changes. */
function quote(design, price) {
  const key = `${price.recurring}|${price.dev}`;
  if (key === lastQuote) return;
  lastQuote = key;
  flightBus.emit('econ.quote', { designId: design.id || null, recurring: price.recurring, dev: price.dev, perKg: price.perKg, currency: 'Kc' });
}

export function costPanel(h, design, readout, { tip = () => null, rerender = () => {} } = {}) {
  const price = readout.price;
  if (!price) return null;
  quote(design, price);
  const total = price.recurring || 1;
  const bar = h('div', { class: 'kf-stack', role: 'img', 'aria-label': `Per-launch cost ${formatKc(price.recurring)}: ` + CATEGORIES.filter((c) => price.byCategory[c]).map((c) => `${CATEGORY_LABEL[c]} ${Math.round(price.byCategory[c] / total * 100)}%`).join(', ') },
    CATEGORIES.filter((c) => price.byCategory[c] > 0).map((c) => h('i', { style: `flex:${price.byCategory[c]} 1 0;background:${CATEGORY_COLOR[c]}`, title: `${CATEGORY_LABEL[c]} ${formatKc(price.byCategory[c])}` })));
  const legend = h('div', { class: 'kf-legend' }, CATEGORIES.filter((c) => price.byCategory[c] > 0).map((c) =>
    h('span', {}, h('i', { style: `background:${CATEGORY_COLOR[c]}` }), `${CATEGORY_LABEL[c]} ${Math.round(price.byCategory[c] / total * 100)}%`)));
  const funds = h('div', { class: 'kf-funds' },
    h('div', { class: 'kf-top' }, h('span', {}, 'Per launch ', tip('cost')), h('b', { text: formatKc(price.recurring) })),
    bar, legend,
    h('div', { class: 'kf-sub' },
      h('span', { text: `One-time dev ${formatKc(price.dev)}` }),
      h('span', { text: perKgText(price) })),
    h('small', { class: 'kf-note', text: 'Sandbox: cost readout only. Kc are fictional in-game credits.' }));

  const rows = price.lines.map((l) => ({ ...l, share: l.amount / total }));
  const k = sortState.key, d = sortState.dir;
  rows.sort((a, b) => (k === 'label' || k === 'cat' ? String(a[k]).localeCompare(String(b[k])) : a[k] - b[k]) * d);
  const th = (key, text) => h('th', { scope: 'col', 'aria-sort': sortState.key === key ? (sortState.dir > 0 ? 'ascending' : 'descending') : 'none' },
    h('button', { type: 'button', class: 'kf-sort', onclick: () => { if (sortState.key === key) sortState.dir *= -1; else { sortState.key = key; sortState.dir = key === 'amount' ? -1 : 1; } rerender(); } }, text, sortState.key === key ? (sortState.dir > 0 ? ' ▲' : ' ▼') : ''));
  const table = h('table', { class: 'kd-table kf-table' },
    h('thead', {}, h('tr', {}, th('label', 'Item'), th('cat', 'Type'), th('amount', 'Kc'))),
    h('tbody', {}, rows.map((r) => h('tr', {},
      h('td', { text: r.label }),
      h('td', {}, h('i', { class: 'kf-dot', style: `background:${CATEGORY_COLOR[r.cat]}` }), CATEGORY_LABEL[r.cat]),
      h('td', { text: formatKc(r.amount).replace(' Kc', '') })))),
    price.devLines.length ? h('tfoot', {}, price.devLines.map((l) => h('tr', { class: 'kf-dev' }, h('td', { text: l.label }), h('td', { text: 'One-time' }), h('td', { text: formatKc(l.amount).replace(' Kc', '') })))) : null);
  const hints = marginalHints(design).slice(0, 4);
  const hintList = h('ul', { class: 'kf-hints' }, hints.map((x) => h('li', {}, h('span', { text: x.label }), h('b', { class: x.delta > 0 ? 'warn' : 'good', text: `${x.delta >= 0 ? '+' : '−'}${formatKc(Math.abs(x.delta))}` }))));
  const details = h('details', { class: 'kf-details' }, h('summary', { text: 'Cost breakdown' }), table,
    h('div', { class: 'kf-hint-title', text: 'What edits would cost (per launch)' }), hintList);
  return h('section', { class: 'kf-panel', 'aria-label': 'Cost' }, funds, details);
}

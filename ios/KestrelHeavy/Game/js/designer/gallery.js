// "New Design" gallery: starter templates as large cards, plus Blank and Continue.
// New file; the designer only calls openGallery(screen). Portrait-first, readable on iPhone SE.
import { TEMPLATES } from '../data/templates/index.js';
import { forkTemplate, lastDesign, blankDesign } from './templates-fork.js';
import { migrateDesign } from '../vehicle/design.js';

const CSS_ID = 'kd-gallery-css';
function ensureCss() {
  if (document.getElementById(CSS_ID)) return;
  const l = document.createElement('link');
  l.id = CSS_ID; l.rel = 'stylesheet'; l.href = new URL('../../css/gallery.css', import.meta.url).href;
  document.head.append(l);
}
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
const DIFF = { beginner: 1, easy: 2, medium: 3, hard: 4, expert: 5 };

/** 2D thumbnail from the designer's own schematic renderer (same drawing as the blueprint). */
function thumb(screen, design) {
  const box = h('div');
  const ctx = { design: migrateDesign(design), stageIdx: -1, sim: null, quick: null, validation: { ok: true }, schem: box };
  try { Object.getPrototypeOf(screen).renderSchematic.call(ctx); } catch (_) { return h('div', { class: 'kg-thumb' }); }
  const svg = box.querySelector('svg');
  if (svg) svg.setAttribute('aria-hidden', 'true');
  return h('div', { class: 'kg-thumb' }, svg);
}

function statLine(s) {
  if (!s) return '';
  const out = [`${s.stages} stage${s.stages > 1 ? 's' : ''}`, `${s.liftoffMassT} t`, `TWR ${s.liftoffTWR}`];
  out.push(s.payloadKg >= 1000 ? `${s.payloadKg / 1000} t → ${s.target}` : `${s.payloadKg} kg → ${s.target}`);
  return out.join(' · ');
}

function card(screen, t, pick) {
  const m = t.template, lvl = DIFF[m.difficulty] || 1;
  const more = h('details', { class: 'kg-more' },
    h('summary', { text: 'Why it’s built this way' }),
    h('p', { text: m.brief }),
    h('p', { class: 'kg-bench' }, h('b', { text: 'Real-world class: ' }), `${m.benchmark.class}. ${m.benchmark.text}`),
    h('p', { class: 'kg-try-h', text: 'Things to try' }),
    h('ul', { class: 'kg-try' }, m.thingsToTry.map((x) => h('li', { text: x }))));
  return h('article', { class: 'kg-card', 'aria-label': `${t.name}. ${m.tagline}` },
    thumb(screen, t),
    h('div', { class: 'kg-body' },
      h('div', { class: 'kg-head' }, h('h3', { text: t.name }), h('span', { class: 'kg-diff', role: 'img', 'aria-label': `Difficulty: ${m.difficulty}`, text: '●'.repeat(lvl) + '○'.repeat(5 - lvl) })),
      h('p', { class: 'kg-tag', text: m.tagline }),
      h('p', { class: 'kg-stats', text: statLine(m.stats) }),
      h('ul', { class: 'kg-tags', 'aria-label': 'Tags' }, m.tags.slice(0, 5).map((x) => h('li', { text: x }))),
      more,
      h('button', { type: 'button', class: 'kd-btn kg-go', onclick: () => pick(t) }, `Start from ${t.name}`)));
}

/** Show the gallery over the designer. screen: DesignerScreen. Resolves to the choice made. */
export function openGallery(screen) {
  ensureCss();
  screen.el.querySelector('.kg')?.remove();
  return new Promise((resolve) => {
    const store = screen.store;
    const last = store ? lastDesign(store) : null;
    const root = h('section', { class: 'kg', role: 'dialog', 'aria-modal': 'true', 'aria-label': 'New design' });
    const done = (r) => { root.remove(); resolve(r); };
    const pickTemplate = (t) => {
      try { const f = forkTemplate(store, t); screen.load(f.design, f.id); screen.toast?.(`New copy of ${t.name}. The template stays untouched.`); done({ kind: 'template', id: t.id, savedId: f.id }); }
      catch (e) { screen.toast?.(`Couldn't start from ${t.name} (${e.code || e.message})`); }
    };
    root.addEventListener('keydown', (e) => { if (e.key === 'Escape') { e.stopPropagation(); done({ kind: 'cancel' }); } });
    root.append(
      h('header', { class: 'kg-top' },
        h('div', {}, h('span', { class: 'kd-kicker', text: 'DESIGN · NEW' }), h('h2', { text: 'Start a rocket' })),
        h('button', { type: 'button', class: 'kd-btn', 'aria-label': 'Close', onclick: () => done({ kind: 'cancel' }) }, '✕')),
      h('div', { class: 'kg-quick' },
        h('button', { type: 'button', class: 'kd-btn kg-blank', onclick: () => { screen.load(blankDesign(), null); done({ kind: 'blank' }); } }, h('b', { text: '＋ Blank design' }), h('small', { text: 'One stage, your rules' })),
        h('button', { type: 'button', class: 'kd-btn kg-cont', disabled: !last, onclick: () => { screen.load(last.design, last.id); done({ kind: 'continue', savedId: last.id }); } },
          h('b', { text: '↺ Continue last design' }), h('small', { text: last ? last.name : 'Nothing saved yet' }))),
      h('h3', { class: 'kg-sec', text: 'Starter templates' }),
      h('div', { class: 'kg-grid' }, TEMPLATES.map((t) => card(screen, t, pickTemplate))));
    screen.el.append(root);
    root.querySelector('.kg-go')?.focus();
  });
}

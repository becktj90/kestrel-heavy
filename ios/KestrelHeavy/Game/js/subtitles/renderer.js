/**
 * Bottom-third caption renderer. DOM overlay above the canvas, pointer-events
 * off, safe-area aware, portrait + landscape. Max 2 lines (clamped). Speaker
 * label + color per channel. No-op without a DOM (node tests).
 */
import { SPEAKER } from '../narration/manifest.js';
import en from './strings/en.js';
import { normalizeCaptions } from './settings.js';

const STYLE_ID = 'kh-captions-style';
const CSS = `
#kh-captions{position:fixed;left:calc(env(safe-area-inset-left,0px) + 12px);right:calc(env(safe-area-inset-right,0px) + 12px);
bottom:calc(env(safe-area-inset-bottom,0px) + 18%);display:flex;justify-content:center;pointer-events:none;z-index:40}
#kh-captions[hidden]{display:none}
#kh-captions .kh-cap{max-width:min(92vw,640px);padding:6px 12px;border-radius:8px;font:600 var(--kh-cap-size,16px)/1.3 system-ui,-apple-system,sans-serif;
color:#f4f7fb;background:rgba(6,10,18,var(--kh-cap-bg,.6));text-align:center;
display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden;text-shadow:0 1px 2px #000}
#kh-captions .kh-spk{font-size:.72em;letter-spacing:.08em;margin-right:.5em}
#kh-captions.hc .kh-cap{background:#000;color:#fff;border:2px solid #fff;text-shadow:none}
#kh-captions.hc .kh-spk{color:#ff0 !important}
@media (orientation:landscape) and (max-height:500px){#kh-captions{bottom:calc(env(safe-area-inset-bottom,0px) + 22%)}}
`;
const SIZE_PX = { s: 13, m: 16, l: 20 };

export function createRenderer(doc = globalThis.document) {
  if (!doc || !doc.createElement) {
    return { show() {}, apply() {}, hide() {}, active: false };
  }
  if (!doc.getElementById(STYLE_ID)) {
    const st = doc.createElement('style');
    st.id = STYLE_ID;
    st.textContent = CSS;
    doc.head.appendChild(st);
  }
  const root = doc.createElement('div');
  root.id = 'kh-captions';
  root.setAttribute('aria-live', 'polite');
  root.hidden = true;
  const box = doc.createElement('div');
  box.className = 'kh-cap';
  const spk = doc.createElement('span');
  spk.className = 'kh-spk';
  const txt = doc.createElement('span');
  box.append(spk, txt);
  root.appendChild(box);
  doc.body.appendChild(root);
  let prefs = normalizeCaptions();
  let shownKey = null;

  return {
    active: true,
    apply(next) {
      prefs = normalizeCaptions(next);
      root.style.setProperty('--kh-cap-size', `${SIZE_PX[prefs.size]}px`);
      root.style.setProperty('--kh-cap-bg', String(prefs.bgOpacity));
      root.classList.toggle('hc', prefs.highContrast);
      if (!prefs.enabled) { root.hidden = true; shownKey = null; }
    },
    /** line = queue item or null. Cheap when unchanged (called per frame). */
    show(line) {
      if (!prefs.enabled || !line) {
        if (!root.hidden) root.hidden = true;
        shownKey = null;
        return;
      }
      if (shownKey === line) return;
      shownKey = line;
      const sp = SPEAKER[line.channel] || SPEAKER.mc;
      spk.textContent = (en.speaker[line.channel] || sp.label);
      spk.style.color = sp.color;
      txt.textContent = line.text;
      root.hidden = false;
    },
    hide() { root.hidden = true; shownKey = null; },
  };
}

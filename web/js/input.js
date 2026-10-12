import { BOOST_BUFFER_SEC, BOOST_COYOTE_SEC } from './config.js';
import { STEER } from './steer-tune.js';

/** Pixels of thumb travel before climb-pad drag arms analog steer. */
export const TOUCH_STEER_DEAD_PX = STEER.deadPx;
/** Horizontal drag that maps to full analog deflection on a normal phone. */
export const TOUCH_STEER_FULL_PX = STEER.fullPx;
/** Shorter thumb travel to full deflection on a small phone. */
export const TOUCH_STEER_FULL_PX_COMPACT = STEER.fullPxCompact;
export const TOUCH_STEER_EXPO = STEER.expo;

let steerSensitivity = STEER.sensitivity.default;

export function clampSteerSensitivity(value) {
  const n = Number(value);
  const { min, max, default: fallback } = STEER.sensitivity;
  if (!Number.isFinite(n)) return fallback;
  return Math.max(min, Math.min(max, n));
}

export function setSteerSensitivity(value) {
  steerSensitivity = clampSteerSensitivity(value);
  return steerSensitivity;
}

export function getSteerSensitivity() {
  return steerSensitivity;
}

/** Full-scale thumb travel. Small portrait phones use the shorter range. */
export function steerFullPx(viewportWidth) {
  const w = Number(viewportWidth);
  if (Number.isFinite(w) && w > 0 && w <= STEER.compactMaxWidth) return STEER.fullPxCompact;
  return STEER.fullPx;
}

/**
 * Expo curve: sign(dx) * (|dx| / (fullPx / sensitivity)) ^ expo, clamped to [-1, 1].
 * At sensitivity 1 and fullPx 110 this is (dx/110)^1.6.
 */
export function applySteerCurve(dxPx, opts = {}) {
  const sensitivity = clampSteerSensitivity(opts.sensitivity ?? steerSensitivity);
  const full = (opts.fullPx ?? steerFullPx(opts.viewportWidth)) / sensitivity;
  const span = Math.max(1, full);
  const t = Math.max(-1, Math.min(1, dxPx / span));
  const mag = Math.abs(t) ** TOUCH_STEER_EXPO;
  return Math.max(-1, Math.min(1, Math.sign(t) * mag));
}

export function createInput() {
  return {
    left: false,
    right: false,
    boostHeld: false,
    boostUntil: 0,
    boostBufferedUntil: 0,
    pointerX: null,
    touchSteer: null,
    touchSteerArmed: false,
    thumbHeld: false,
    launchArmed: false,
  };
}

export function setBoostHeld(input, held, now) {
  input.boostHeld = held;
  if (held) {
    input.boostUntil = now + BOOST_COYOTE_SEC;
    input.boostBufferedUntil = now + BOOST_BUFFER_SEC;
  }
}

export function isBoosting(input, now) {
  return input.boostHeld || now < input.boostUntil;
}

export function consumeBoostTap(input, now) {
  if (input.boostHeld || now < input.boostBufferedUntil || now < input.boostUntil) {
    input.boostBufferedUntil = 0;
    return true;
  }
  return false;
}

export function steerAxis(input) {
  return (input.right ? 1 : 0) - (input.left ? 1 : 0);
}

/**
 * One-thumb climb pad: hold still does not steal ◀ ▶ / canvas analog.
 * Past the deadzone, horizontal drag is analog steer in [-1, 1].
 */
export function peekTouchSteer(dxPx, armed = false, opts = {}) {
  if (dxPx == null || !Number.isFinite(dxPx)) return 0;
  if (!armed && Math.abs(dxPx) < TOUCH_STEER_DEAD_PX) return 0;
  return applySteerCurve(dxPx, opts);
}

export function setTouchSteer(input, dxPx) {
  if (dxPx == null || !Number.isFinite(dxPx)) {
    input.touchSteer = null;
    input.touchSteerArmed = false;
    return null;
  }
  if (!input.touchSteerArmed && Math.abs(dxPx) < TOUCH_STEER_DEAD_PX) return input.touchSteer;
  input.touchSteerArmed = true;
  const viewportWidth = typeof window !== 'undefined' ? window.innerWidth : undefined;
  input.touchSteer = peekTouchSteer(dxPx, true, { viewportWidth });
  return input.touchSteer;
}

/**
 * Only Haven's landing burn is a deliberate hold. Pad launch is a tap,
 * ascent cruise is automatic, glide is steer-only.
 */
export function climbArmedFromLabel(label) {
  return label === 'HOLD TO BURN';
}

export function isAscentCruise(status) {
  return status === 'ASCENT';
}

export function paintThumbSteer(node, analog) {
  if (!node) return;
  const axis = Math.max(-1, Math.min(1, Number(analog) || 0));
  if (node.style?.setProperty) node.style.setProperty('--kh-steer', String(axis));
  node.classList?.toggle?.('is-steer-left', axis < -0.02);
  node.classList?.toggle?.('is-steer-right', axis > 0.02);
  if (node.dataset) node.dataset.steer = axis.toFixed(2);
}

/**
 * Steer source of truth: climb-pad drag, then canvas pointerX, then A/D pads.
 */
export function flightAxis(input, rocketX, analogScale) {
  if (input.touchSteer != null) return input.touchSteer;
  if (input.pointerX != null) {
    const span = analogScale || 140;
    return Math.max(-1, Math.min(1, (input.pointerX - rocketX) / span));
  }
  return steerAxis(input);
}

export function clearFlightHolds(input) {
  input.left = false;
  input.right = false;
  input.boostHeld = false;
  input.boostUntil = 0;
  input.boostBufferedUntil = 0;
  input.pointerX = null;
  input.touchSteer = null;
  input.touchSteerArmed = false;
  input.thumbHeld = false;
  input.launchArmed = false;
}

export function bindKeyboard(input, hooks) {
  const down = (event) => {
    hooks.unlock();
    if (event.code === 'ArrowLeft' || event.code === 'KeyA') {
      event.preventDefault();
      input.left = true;
    }
    if (event.code === 'ArrowRight' || event.code === 'KeyD') {
      event.preventDefault();
      input.right = true;
    }
    if (event.code === 'Space') {
      event.preventDefault();
      if (event.repeat) {
        setBoostHeld(input, true, hooks.now());
        return;
      }
      if (hooks.onBoostTap) hooks.onBoostTap();
      setBoostHeld(input, true, hooks.now());
    }
    if (event.code === 'KeyP' || event.code === 'Escape') {
      event.preventDefault();
      hooks.togglePause();
    }
    if (event.code === 'KeyM') {
      event.preventDefault();
      hooks.toggleMute();
    }
    if (event.code === 'KeyO') {
      event.preventDefault();
      hooks.toggleSettings();
    }
  };
  const up = (event) => {
    if (event.code === 'ArrowLeft' || event.code === 'KeyA') input.left = false;
    if (event.code === 'ArrowRight' || event.code === 'KeyD') input.right = false;
    if (event.code === 'Space') setBoostHeld(input, false, hooks.now());
  };
  const loseFocus = () => {
    clearFlightHolds(input);
  };
  const onVisibility = () => {
    if (document.hidden) loseFocus();
  };
  window.addEventListener('keydown', down);
  window.addEventListener('keyup', up);
  window.addEventListener('blur', loseFocus);
  document.addEventListener('visibilitychange', onVisibility);
  return () => {
    window.removeEventListener('keydown', down);
    window.removeEventListener('keyup', up);
    window.removeEventListener('blur', loseFocus);
    document.removeEventListener('visibilitychange', onVisibility);
  };
}

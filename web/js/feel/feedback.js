// Flight-event feel and the HUD risk line. The 0.34 integrity mark matches health.js.
// DISPLAY_Q_LIM_PA is the arcade HUD's full-scale mark: compressed Max-Q reads about 25 kPa.

export const DISPLAY_Q_LIM_PA = 30000;
export const HOT = 0.85;

/** Meter announcement, 0–100. Live q may run past the limit; the meter must not. */
export function qMeterNow(qRisk) {
  const v = Number.isFinite(qRisk) ? qRisk : 0;
  return Math.round(Math.max(0, Math.min(1, v)) * 100);
}

export function flightCue(name, reduced) {
  let shake = 0;
  let flash = 0;
  let hapticMs = 0;
  let rumble = 0;
  let punch = 0;
  let hard = false;
  switch (name) {
    case 'liftoff': shake = 0.55; flash = 0.7; hapticMs = 28; rumble = 0.8; punch = 0.8; break;
    case 'maxq': shake = 0.72; flash = 0.25; hapticMs = 18; rumble = 1; punch = 0.45; break;
    case 'stage': shake = 0.4; flash = 0.35; hapticMs = 14; rumble = 0.5; punch = 0.6; break;
    case 'meco': shake = 0.22; flash = 0.1; hapticMs = 10; rumble = 0.2; punch = 0.3; break;
    case 'hit': shake = 0.85; flash = 1; hapticMs = 22; rumble = 0.4; punch = 0.5; hard = true; break;
    case 'breakup': shake = 1; flash = 1; hapticMs = 40; rumble = 0.3; punch = 1; hard = true; break;
    default: break;
  }
  if (reduced) {
    shake = 0;
    flash = 0;
    hapticMs = 0;
    punch *= 0.2;
    hard = false;
  }
  return { shake, flash, hapticMs, rumble, punch, hard };
}

export function airframeBand(integrity) {
  if (!Number.isFinite(integrity) || integrity <= 0) return 'lost';
  if (integrity < 0.34) return 'critical';
  if (integrity < 0.62) return 'scorched';
  if (integrity < 0.85) return 'warm';
  return 'nominal';
}

export function airframeTint(band) {
  switch (band) {
    case 'lost': return { r: 0.35, g: 0.32, b: 0.3 };
    case 'critical': return { r: 1, g: 0.42, b: 0.28 };
    case 'scorched': return { r: 1, g: 0.72, b: 0.38 };
    case 'warm': return { r: 1, g: 0.92, b: 0.72 };
    default: return { r: 1, g: 1, b: 1 };
  }
}

export function integrityOf(qPa, qLimPa, health01) {
  let q = 1;
  if (qLimPa > 1 && Number.isFinite(qPa)) {
    q = 1 - Math.max(0, qPa / qLimPa - 0.72) / 0.7;
    if (q < 0) q = 0;
    if (q > 1) q = 1;
  }
  let h = Number.isFinite(health01) ? health01 : 1;
  if (h < 0) h = 0;
  if (h > 1) h = 1;
  return q * h;
}

function clamp01(v) {
  if (!Number.isFinite(v) || v < 0) return 0;
  if (v > 1) return 1;
  return v;
}

export function evaluateRisk({ health01 = 1, throttle01 = 0, fuel01 = 1, qPa = 0, qLimPa = 0, dvMargin = 0 } = {}) {
  let qRisk = qLimPa > 1 && Number.isFinite(qPa) ? qPa / qLimPa : 0;
  if (!Number.isFinite(qRisk) || qRisk < 0) qRisk = 0;
  if (qRisk > 1.5) qRisk = 1.5;
  const fuelKnown = Number.isFinite(fuel01);
  const fuel = fuelKnown ? clamp01(fuel01) : 1;
  const energy = clamp01(throttle01) * fuel;
  const integrity = integrityOf(qPa, qLimPa, health01);
  const band = airframeBand(integrity);
  let call = 'NOMINAL';
  let line = '';
  if (band === 'critical' || band === 'lost') {
    call = 'AIRFRAME';
    line = 'AIRFRAME — load is high';
  } else if (qRisk >= HOT) {
    call = 'MAX-Q';
    line = 'MAX-Q — the air is heavy';
  } else if (Number.isFinite(dvMargin) && dvMargin < 0) {
    call = 'SHORT';
    line = 'SHORT — light on speed for this trip';
  } else if (fuelKnown && energy < 0.12 && fuel < 0.2) {
    call = 'PROP';
    line = 'PROP — tanks are thin';
  }
  return { integrity, energy, qRisk, margin: dvMargin, band, call, line };
}

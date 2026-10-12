// Earth rotation and launch-site velocity. SI units.
import { R_EARTH } from './gravity.js';
export const OMEGA_EARTH = 7.2921159e-5; // rad/s, sidereal rotation rate

/** Eastward surface speed from rotation at geodetic latitude (deg), m/s. Spherical Earth. */
export function siteRotationSpeed(latDeg, radius = R_EARTH) {
  return OMEGA_EARTH * radius * Math.cos(latDeg * Math.PI / 180);
}

/**
 * Inertial launch-site velocity components along local East and North for a
 * launch at latitude latDeg. Rotation is purely eastward. out = [east, north].
 */
export function siteVelocity(latDeg, out = [0, 0], radius = R_EARTH) {
  out[0] = siteRotationSpeed(latDeg, radius); out[1] = 0;
  return out;
}

/** Atmosphere co-rotates: air velocity at planar position (x,y) is ω × r. out=[vx,vy]. */
export function airVelocity2D(x, y, out, omega = OMEGA_EARTH) {
  out[0] = -omega * y; out[1] = omega * x;
  return out;
}

/** Minimum reachable inclination (deg) from a site at latDeg (no dogleg). */
export function minInclination(latDeg) { return Math.abs(latDeg); }

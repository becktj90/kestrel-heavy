// Interstages: thin loader over data/interstages.json.
// diameter and length in m, mass in kg, cost in USD, dragDeltaCd added to the stack.
import RAW from './gen/interstages.js';

export const INTERSTAGES = Object.freeze(RAW.interstages);
export const INTERSTAGE_BY_ID = Object.freeze(Object.fromEntries(INTERSTAGES.map((s) => [s.id, s])));

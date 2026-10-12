const assert = require('node:assert/strict');
const { test } = require('node:test');
const fs = require('node:fs');
const path = require('node:path');

const arcadeDir = path.join(__dirname, '..', 'web');
const arcade = path.join(arcadeDir, 'js');
const iosRoot = path.join(__dirname, '..', 'ios');
const iosGame = path.join(iosRoot, 'KestrelHeavy/Game');

test('KH-1 corridor is wider and first-flight drifts get a soft grace, not an abort', async () => {
  const c = await import(path.join(arcade, 'corridor.js'));
  const { getMission } = await import(path.join(arcade, 'missions.js'));
  const { PAD_ROCKET_Y } = await import(path.join(arcade, 'config.js'));
  const kh1 = getMission('KH-1');
  assert.ok(c.corridorWidenFor(kh1) >= 60, 'KH-1 teaches the rails wider');
  assert.equal(c.corridorWidenFor(getMission('KH-2')), 0, 'later flights keep the real rails');
  c.setCorridorWiden(0);
  const narrow = c.corridorBounds(PAD_ROCKET_Y - 400);
  c.setCorridorWiden(c.corridorWidenFor(kh1));
  const wide = c.corridorBounds(PAD_ROCKET_Y - 400);
  assert.ok(wide.left < narrow.left && wide.right > narrow.right);
  assert.ok(c.corridorFitsPlayfield());
  // A drift that is "out" on the narrow rails is now still inside on KH-1.
  assert.notEqual(c.corridorEdge(narrow.left - 20, PAD_ROCKET_Y - 400).state, 'out');
  c.setCorridorWiden(0);

  assert.equal(c.isFirstFlightGrace('KH-1', { missionBests: {} }), true);
  assert.equal(c.isFirstFlightGrace('KH-1', { missionBests: { 'KH-1': { recovered: true } } }), false);
  assert.equal(c.isFirstFlightGrace('KH-2', { missionBests: {} }), false);

  const g = c.freshCorridorGrace(true);
  // First drift out: soft for the whole grace window, then enforce.
  let soft = 0;
  let state;
  for (let t = 0; t < c.FIRST_FLIGHT.graceSec + 1; t += 0.1) {
    state = c.stepCorridorGrace(g, 'out', 0.1);
    if (state === 'soft') soft += 0.1;
    else break;
  }
  assert.ok(soft >= c.FIRST_FLIGHT.graceSec - 0.2, `soft window ${soft}s gives recovery time`);
  assert.equal(state, 'wall', 'still out after the window: rails hold, no abort on the first flight');
  // Steering back in closes the window; a second drift gets the second grace.
  assert.equal(c.stepCorridorGrace(g, 'ok', 0.1), 'ok');
  assert.equal(c.stepCorridorGrace(g, 'out', 0.1), 'soft');
  assert.equal(c.stepCorridorGrace(g, 'ok', 0.1), 'ok');
  assert.equal(c.stepCorridorGrace(g, 'out', 0.1), 'wall', 'soft windows are finite; then the rails are a wall');
  const none = c.freshCorridorGrace(false);
  assert.equal(c.stepCorridorGrace(none, 'out', 0.1), 'enforce');

  // First-flight hazard softening: less INT, one lethal save, then real.
  const fg = c.freshCorridorGrace(true);
  assert.deepEqual(c.softenFirstFlightHit(fg, 100, { health: 72, kind: 'graze' }).kind, 'graze');
  assert.ok(c.softenFirstFlightHit(fg, 100, { health: 72, kind: 'graze' }).health > 72);
  const save = c.softenFirstFlightHit(fg, 20, { health: 0, kind: 'lethal' });
  assert.deepEqual(save, { health: c.FIRST_FLIGHT.saveHealth, kind: 'graze', saved: true });
  assert.equal(c.softenFirstFlightHit(fg, 10, { health: 0, kind: 'lethal' }).kind, 'lethal', 'only one save');
  assert.equal(c.softenFirstFlightHit(c.freshCorridorGrace(false), 10, { health: 0, kind: 'lethal' }).kind, 'lethal');

  const mission = fs.readFileSync(path.join(arcade, 'mission.js'), 'utf8');
  assert.match(mission, /softenFirstFlightHit\(this\.session\.corridorGrace/);
  assert.match(mission, /stepCorridorGrace\(this\.session\.corridorGrace/);
  assert.match(mission, /setCorridorWiden\(corridorWidenFor/);
});

test('medals grade Haven landing quality and persist best per flight', async () => {
  const m = await import(path.join(arcade, 'medals.js'));
  const lim = { landingVy: 6.2, landingTol: 58 };
  assert.equal(m.gradeLanding({ kind: 'water', vy: 1, dx: 0, tilt: 0, ...lim }), null);
  assert.equal(m.gradeLanding({ kind: 'deck', vy: 9, dx: 90, tilt: 20, recovered: false, ...lim }), 'bronze');
  assert.equal(m.gradeLanding({ kind: 'deck', vy: 2, dx: 10, tilt: 2, recovered: true, ...lim }), 'gold');
  assert.equal(m.gradeLanding({ kind: 'deck', vy: 4.5, dx: 30, tilt: 6, recovered: true, ...lim }), 'silver');
  assert.equal(m.gradeLanding({ kind: 'deck', vy: 6, dx: 55, tilt: 12, recovered: true, ...lim }), 'bronze');

  const settings = { missionBests: { 'KH-1': { score: 10, recovered: true } } };
  assert.deepEqual(m.recordMedal(settings, 'KH-1', 'silver'), { best: 'silver', improved: true });
  assert.deepEqual(m.recordMedal(settings, 'KH-1', 'bronze'), { best: 'silver', improved: false });
  assert.deepEqual(m.recordMedal(settings, 'KH-1', null), { best: 'silver', improved: false });
  assert.deepEqual(m.recordMedal(settings, 'KH-1', 'gold'), { best: 'gold', improved: true });
  assert.match(m.medalShelf(settings, ['KH-1', 'KH-2']), /KH-1 🥇 {2}· {2}KH-2 —/);

  // localStorage round trip through storage.js keeps the medal.
  const store = new Map();
  global.localStorage = {
    getItem: (k) => (store.has(k) ? store.get(k) : null),
    setItem: (k, v) => store.set(k, String(v)),
  };
  const s = await import(path.join(arcade, 'storage.js'));
  const loaded = s.loadSettings();
  m.recordMedal(loaded, 'KH-2', 'bronze');
  s.saveSettings(loaded);
  const again = s.loadSettings();
  assert.equal(again.missionBests['KH-2'].medal, 'bronze');
  assert.equal(again.missionBests['KH-3'].medal, null);
  delete global.localStorage;

  const html = fs.readFileSync(path.join(arcadeDir, 'index.html'), 'utf8');
  assert.match(html, /id="ng-sum-medal"/);
  assert.match(html, /id="ng-hangar-medals"/);
  const mission = fs.readFileSync(path.join(arcade, 'mission.js'), 'utf8');
  assert.match(mission, /gradeLanding\(/);
  assert.match(mission, /recordMedal\(this\.settings/);
  assert.match(mission, /setHangarMedals\(medalShelf/);
});

test('Game Center: one leaderboard per KH-n, bridged only in the iOS app', async () => {
  const gc = await import(path.join(arcade, 'gamecenter.js'));
  const { MISSIONS } = await import(path.join(arcade, 'missions.js'));
  const ids = gc.leaderboardIds(MISSIONS);
  assert.equal(ids.length, MISSIONS.length);
  assert.equal(ids[0], 'com.beckify.kestrelheavy.lb.kh1');
  for (const id of ids) assert.match(id, /^com\.beckify\.kestrelheavy\.lb\.kh\d+$/);
  assert.equal(gc.submitFlightScore('KH-1', 1200, {}), false, 'web: silent no-op');
  const sent = [];
  const win = { webkit: { messageHandlers: { kestrelGameCenter: { postMessage: (b) => sent.push(b) } } } };
  assert.equal(gc.submitFlightScore('KH-3', 4321.7, win), true);
  assert.deepEqual(sent[0], { type: 'score', leaderboard: 'com.beckify.kestrelheavy.lb.kh3', score: 4321 });
  assert.equal(gc.submitFlightScore('KH-3', 0, win), false);
  const throwing = { webkit: { messageHandlers: { kestrelGameCenter: { postMessage: () => { throw new Error('x'); } } } } };
  assert.equal(gc.submitFlightScore('KH-1', 5, throwing), false, 'bridge errors are swallowed');

  const swift = fs.readFileSync(path.join(iosRoot, 'KestrelHeavy/KestrelHeavyGameView.swift'), 'utf8');
  assert.match(swift, /import GameKit/);
  assert.match(swift, /name: "kestrelGameCenter"/);
  assert.match(swift, /removeScriptMessageHandler\(forName: "kestrelGameCenter"\)/);
  assert.match(swift, /GKLocalPlayer\.local\.authenticateHandler/);
  assert.match(swift, /GKLeaderboard\.submitScore/);
  assert.match(swift, /com\.beckify\.kestrelheavy\.lb\./);
  const ent = fs.readFileSync(path.join(iosRoot, 'KestrelHeavy/KestrelHeavy.entitlements'), 'utf8');
  assert.match(ent, /com\.apple\.developer\.game-center/);
  const doc = fs.readFileSync(path.join(__dirname, '..', 'docs/KESTREL_APP_STORE.md'), 'utf8');
  for (const id of ids) assert.ok(doc.includes(id), `listing doc documents ${id}`);
});

test('SOUND pills no longer cover the INT bar at phone width', () => {
  const html = fs.readFileSync(path.join(arcadeDir, 'index.html'), 'utf8');
  assert.match(
    html,
    /@container cabinet \(max-width: 720px\) \{\s*body:not\(\[data-phase="MENU"\]\):not\(\[data-phase="SUMMARY"\]\) \.mc-hud \{\s*margin-top: 46px;/,
  );
});

test('iOS Game pack is in sync with the web arcade (no drift)', () => {
  const walk = (dir, base = dir) => fs.readdirSync(dir, { withFileTypes: true }).flatMap((d) => {
    const p = path.join(dir, d.name);
    return d.isDirectory() ? walk(p, base) : [path.relative(base, p)];
  });
  for (const rel of walk(path.join(arcadeDir, 'js'))) {
    const web = fs.readFileSync(path.join(arcadeDir, 'js', rel), 'utf8');
    const ios = fs.readFileSync(path.join(iosGame, 'js', rel), 'utf8');
    assert.equal(ios, web, `ios/KestrelHeavy/Game/js/${rel} drifted — run python3 ios/scripts/pack_kestrelheavy_game.py`);
  }
  const iosHtml = fs.readFileSync(path.join(iosGame, 'index.html'), 'utf8');
  assert.match(iosHtml, /id="ng-sum-medal"/);
  assert.match(iosHtml, /margin-top: 46px/);
  assert.doesNotMatch(iosHtml, /Blue Origin|New Glenn|LC-36/i);
  assert.doesNotMatch(iosHtml, /Jacklyn/);
});

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.join(__dirname, '..');
const PORTRAIT = [[375, 667], [402, 874], [440, 956], [834, 1210], [1032, 1376]];

test('iOS shell is portrait-only on iPhone and iPad (Debug + Release)', () => {
  const pbx = fs.readFileSync(path.join(root, 'ios/KestrelHeavy.xcodeproj/project.pbxproj'), 'utf8');
  for (const key of ['UISupportedInterfaceOrientations', 'UISupportedInterfaceOrientations_iPhone', 'UISupportedInterfaceOrientations_iPad']) {
    const lines = pbx.split('\n').filter((l) => l.includes(`INFOPLIST_KEY_${key} =`));
    assert.ok(lines.length >= 2, `${key} set in Debug and Release`);
    for (const l of lines) {
      assert.ok(l.includes('UIInterfaceOrientationPortrait'), `${key} portrait`);
      assert.ok(!l.includes('Landscape'), `${key} has no landscape: ${l.trim()}`);
    }
  }
});

test('Swift shell locks portrait without .supportedInterfaceOrientations modifier', () => {
  for (const f of ['KestrelHeavyApp.swift', 'KestrelHeavyRootView.swift']) {
    const src = fs.readFileSync(path.join(root, 'ios/KestrelHeavy', f), 'utf8');
    assert.ok(!src.includes('.supportedInterfaceOrientations'), `${f} must not use .supportedInterfaceOrientations`);
  }
  const app = fs.readFileSync(path.join(root, 'ios/KestrelHeavy/KestrelHeavyApp.swift'), 'utf8');
  assert.match(app, /mask: UIInterfaceOrientationMask = \[\.portrait, \.portraitUpsideDown\]/);
  assert.ok(!/allButUpsideDown|\.landscape/.test(app));
  const vc = fs.readFileSync(path.join(root, 'ios/KestrelHeavy/KestrelHeavyGameView.swift'), 'utf8');
  assert.match(vc, /shouldAutorotate: Bool \{ false \}/);
  assert.match(vc, /preferredInterfaceOrientationForPresentation: UIInterfaceOrientation \{ \.portrait \}/);
});

test('portrait device sizes keep the corridor and the Haven booster in view', async () => {
  const cam = await import(path.join(root, 'web/js/camera.js'));
  const { W, H } = await import(path.join(root, 'web/js/config.js'));
  for (const [w, h] of PORTRAIT) {
    assert.equal(cam.isPortraitPlayfield(w, h), true, `${w}x${h} portrait`);
    const z = cam.padZoomForPlayfield(w, h);
    const vis = cam.envelopVisibleWorld(w, h, W, H, z);
    assert.ok(vis.w >= cam.corridorNeedWidth() - 1, `${w}x${h} corridor fits (${vis.w})`);
    const hz = cam.havenZoomWant({ alt: 600, parentW: w, parentH: h, dx: 300 });
    const hv = cam.envelopVisibleWorld(w, h, W, H, hz);
    const look = cam.havenLookPoint(0, 0, 300, 600, hv.w, hv.h);
    assert.ok(cam.vehicleInView(0, 0, look.cx, look.cy, hv.w, hv.h, 40), `${w}x${h} booster visible on descent`);
  }
});

test('stage scale: crisp on 2x/3x, capped for GPU, logical zoom preserved', async () => {
  const { stageScaleFor, patchCameraForStageScale, STAGE_K_MAX } = await import(path.join(root, 'web/js/stage.js'));
  assert.equal(stageScaleFor({ vw: 1280, vh: 720, dpr: 1 }), 1);
  for (const [w, h, dpr] of [[375, 667, 2], [402, 874, 3], [440, 956, 3], [834, 1210, 2], [1032, 1376, 2]]) {
    const k = stageScaleFor({ vw: w, vh: h, dpr });
    assert.ok(k > 1 && k <= STAGE_K_MAX, `${w}x${h}@${dpr} k=${k}`);
    assert.ok(1280 * k <= 4096 && 720 * k <= 4096);
  }
  const cam = { zoomX: 1, zoomY: 1, setZoom(x, y = x) { this.zoomX = x; this.zoomY = y; return this; } };
  patchCameraForStageScale(cam, 2);
  cam.setZoom(0.5);
  assert.equal(cam.zoom, 0.5);
  assert.equal(cam.zoomX, 1);
  cam.zoom = 0.25;
  assert.equal(cam.zoomY, 0.5);
});

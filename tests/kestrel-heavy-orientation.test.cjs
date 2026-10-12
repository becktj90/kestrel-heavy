const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.join(__dirname, '..');
const LANDSCAPE = [[844, 390], [932, 430], [1024, 768], [1180, 820], [1366, 1024]];

test('landscape aspect ratios get a wider view than portrait and keep the corridor', async () => {
  const cam = await import(path.join(root, 'web/js/camera.js'));
  const { CAM, W, H } = await import(path.join(root, 'web/js/config.js'));
  const portraitZoom = cam.padZoomForPlayfield(390, 844);
  for (const [w, h] of LANDSCAPE) {
    assert.equal(cam.isPortraitPlayfield(w, h), false, `${w}x${h} is landscape`);
    const z = cam.padZoomForPlayfield(w, h);
    assert.ok(z >= portraitZoom, `${w}x${h} zoom ${z} should not be tighter than portrait ${portraitZoom}`);
    assert.ok(z <= CAM.pad + 1e-9);
    const vis = cam.envelopVisibleWorld(w, h, W, H, z);
    assert.ok(vis.w >= cam.corridorNeedWidth() - 1, `${w}x${h} corridor fits (${vis.w})`);
    assert.ok(vis.w > vis.h, `${w}x${h} view is wider than tall`);
    const hz = cam.havenZoomWant({ alt: 600, parentW: w, parentH: h, dx: 300 });
    const hv = cam.envelopVisibleWorld(w, h, W, H, hz);
    const look = cam.havenLookPoint(0, 0, 300, 600, hv.w, hv.h);
    assert.ok(cam.vehicleInView(0, 0, look.cx, look.cy, hv.w, hv.h, 40), `${w}x${h} booster stays visible on descent`);
  }
});

test('iOS shell allows portrait and landscape on iPhone and iPad', () => {
  const pbx = fs.readFileSync(path.join(root, 'ios/KestrelHeavy.xcodeproj/project.pbxproj'), 'utf8');
  for (const key of ['UISupportedInterfaceOrientations_iPhone', 'UISupportedInterfaceOrientations_iPad']) {
    const lines = pbx.split('\n').filter((l) => l.includes(`INFOPLIST_KEY_${key} =`));
    assert.ok(lines.length >= 2, key);
    for (const l of lines) {
      for (const o of ['Portrait', 'LandscapeLeft', 'LandscapeRight']) assert.ok(l.includes(`UIInterfaceOrientation${o}`), `${key} ${o}`);
    }
  }
  for (const f of ['KestrelHeavyApp.swift', 'KestrelHeavyRootView.swift']) {
    const src = fs.readFileSync(path.join(root, 'ios/KestrelHeavy', f), 'utf8');
    assert.ok(!src.includes('.supportedInterfaceOrientations'), `${f} must not use .supportedInterfaceOrientations`);
  }
  const app = fs.readFileSync(path.join(root, 'ios/KestrelHeavy/KestrelHeavyApp.swift'), 'utf8');
  assert.ok(!/mask.*=\s*\[\.portrait, \.portraitUpsideDown\]/.test(app));
});

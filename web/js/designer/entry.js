// Menu hook for the DESIGN screen. Loaded as its own module from index.html so flight code
// (mission.js, hud.js) is untouched. The designer module itself is loaded lazily on first tap.
function bind() {
  const btn = document.getElementById('ng-open-designer');
  if (!btn || btn.dataset.kdBound) return;
  btn.dataset.kdBound = '1';
  btn.addEventListener('click', async (e) => {
    e.preventDefault();
    const { openDesigner } = await import('./ui.js');
    openDesigner();
  });
}
if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', bind, { once: true });
else bind();

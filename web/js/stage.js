/**
 * Kestrel Heavy stage scale (portrait big-screen polish).
 * The 1280x720 design world is ENVELOP-scaled into a portrait cabinet. On 3x
 * phones and iPad, one design pixel covers ~2-4 device pixels, so a 1280x720
 * backing store looks soft. We enlarge the backing store by k and multiply the
 * main camera zoom by k. Gameplay code keeps reading/writing logical zoom, so
 * world coordinates, Matter physics and camera beats are unchanged.
 */

/** Hard cap: 2560x1440 backing store (3.7 MP) keeps fill rate / thermal sane in WKWebView. */
export const STAGE_K_MAX = 2;
/** Max canvas edge we ever request (well inside iOS WebGL MAX_TEXTURE_SIZE 4096+). */
export const STAGE_EDGE_MAX = 4096;

export function stageScaleFor({ vw, vh, dpr = 1, W = 1280, H = 720, override = null } = {}) {
  let k;
  if (override && Number.isFinite(override) && override > 0) {
    k = override;
  } else {
    if (!(vw > 0 && vh > 0)) return 1;
    const envelop = Math.max(vw / W, vh / H); // CSS px per design px
    k = envelop * Math.max(1, dpr); // device px per design px
  }
  k = Math.min(k, STAGE_K_MAX, STAGE_EDGE_MAX / W, STAGE_EDGE_MAX / H);
  k = Math.max(1, Math.round(k * 4) / 4); // quarter steps; never below design size
  return k;
}

/** Make cam.zoom / setZoom / zoomTo speak logical (design) zoom while rendering at k. */
export function patchCameraForStageScale(cam, k) {
  if (!cam || !(k > 1) || cam.__khStageK) return cam;
  cam.__khStageK = k;
  Object.defineProperty(cam, 'zoom', {
    configurable: true,
    get() { return (this.zoomX + this.zoomY) / 2 / k; },
    set(v) { this.zoomX = v * k; this.zoomY = v * k; this.dirty = true; },
  });
  const setZoom = cam.setZoom.bind(cam);
  cam.setZoom = (x = 1, y = x) => setZoom(x * k, y * k);
  cam.zoom = 1;
  return cam;
}

// Clear colour behind everything = themed fill (never black, never bars). Also sets the camera to
// "cover" the 1280x720 world in portrait: orthographic half-height 360 du keeps 720 du vertically and
// crops the sides (VISUAL_SPEC §5 envelop). Flight/sky renderers (U4) drive SpaceT and TodId.
using UnityEngine;

namespace Kestrel.Game.Shell
{
    [RequireComponent(typeof(Camera))]
    public sealed class ThemedFillCamera : MonoBehaviour
    {
        public VisualSpecData Spec;
        public string TodId = "noon";
        [Range(0, 1)] public float SpaceT;
        Camera _cam;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.orthographic = true;
        }

        void LateUpdate()
        {
            if (Spec == null) return;
            _cam.backgroundColor = Spec.ThemedFill(TodId, SpaceT);
            float halfH = (float)Spec.Portrait.WorldH * 0.5f;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            // Envelop: if a window were ever wider than 16:9, cover by width instead (crop top/bottom).
            float worldAspect = (float)(Spec.Portrait.WorldW / Spec.Portrait.WorldH);
            _cam.orthographicSize = aspect > worldAspect ? (float)(Spec.Portrait.WorldW / aspect) * 0.5f : halfH;
            _cam.rect = new Rect(0, 0, 1, 1); // full viewport, always
        }
    }
}

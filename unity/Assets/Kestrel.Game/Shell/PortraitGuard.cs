// Portrait-only at runtime, belt and braces with PlayerSettings (set by ShellSetup).
// Recomputes the PortraitFrame whenever the screen size or safe area changes (rotation is locked,
// but iPad Stage Manager / Split View resize windows unless UIRequiresFullScreen is set: gate G5).
using System;
using Kestrel.Game.Layout;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public sealed class PortraitGuard : MonoBehaviour
    {
        public VisualSpecData Spec;
        public PortraitFrame Frame { get; private set; }
        public event Action<PortraitFrame> FrameChanged;

        Rect _safe; int _w, _h;

        public static float Dpr
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return Screen.dpi >= 400 ? 3f : 2f;
#else
                return Screen.dpi > 0 ? Mathf.Max(1f, Screen.dpi / 160f) : 1f;
#endif
            }
        }

        void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // The browser owns the window. Landscape is a layout mode; iOS stays portrait-only.
#else
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
#endif
        }

        void Start() => Recompute(force: true);
        void Update() => Recompute(force: false);

        void Recompute(bool force)
        {
            if (Spec == null) return;
            var safe = Screen.safeArea;
            if (!force && safe == _safe && Screen.width == _w && Screen.height == _h) return;
            _safe = safe; _w = Screen.width; _h = Screen.height;
            Frame = PortraitLayout.Compute(_w, _h, new RectPx(safe.x, safe.y, safe.width, safe.height), Dpr, Spec.Portrait);
#if !(UNITY_WEBGL && !UNITY_EDITOR)
            if (!PortraitLayout.IsPortrait(_w, _h))
                Debug.LogWarning($"[Kestrel] non-portrait window {_w}x{_h}; still envelop-filling (no letterbox).");
#endif
            FrameChanged?.Invoke(Frame);
        }
    }
}

// Reusable mission patch. MissionPatch.Render(missionId, size) draws the embroidered disc
// for a campaign flight, a contract, or a free flight. The texture is point-sampled when
// small and bilinear when it is large enough that the pixel font stays on whole texels.
using Kestrel.Core.Flight;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public sealed class MissionPatch : VisualElement
    {
        Texture2D _tex;
        readonly Image _image;
        string _id = "m1";
        float _size = 76f;
        PatchChrome _chrome = PatchChrome.Open;
        int _px = -1;

        public MissionPatch()
        {
            pickingMode = PickingMode.Ignore;
            style.flexShrink = 0;
            _image = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
            _image.style.flexGrow = 1;
            _image.style.width = Length.Percent(100);
            _image.style.height = Length.Percent(100);
            Add(_image);
            RegisterCallback<GeometryChangedEvent>(OnGeo);
            RegisterCallback<DetachFromPanelEvent>(_ => Release());
            Set("m1", 76f, PatchChrome.Open);
        }

        public static MissionPatch Render(string missionId, float size, PatchChrome chrome = PatchChrome.Open)
        {
            var patch = new MissionPatch();
            patch.Set(missionId, size, chrome);
            return patch;
        }

        public void Set(string missionId, float size, PatchChrome chrome)
        {
            _id = string.IsNullOrEmpty(missionId) ? "free" : missionId;
            if (size >= 8f) _size = size;
            _chrome = chrome;
            var spec = MissionPatchArt.Resolve(_id);
            tooltip = spec.Flight + ". " + spec.Motto;
            Rebuild(force: true);
        }

        void OnGeo(GeometryChangedEvent evt)
        {
            float s = Mathf.Min(contentRect.width, contentRect.height);
            if (s < 8f || Mathf.Abs(s - _size) <= 0.5f) return;
            _size = s;
            Rebuild(force: false);
        }

        void Rebuild(bool force)
        {
            float ppp = 1f;
            if (panel != null && panel.scaledPixelsPerPoint >= 1f) ppp = panel.scaledPixelsPerPoint;
            int px = Mathf.Clamp(Mathf.RoundToInt(_size * ppp), 32, MissionPatchArt.UiTextureCap);
            if (!force && _tex != null && px == _px) return;
            _px = px;
            byte[] bytes = MissionPatchArt.Rgba(_id, px, _chrome);
            var cols = new Color32[px * px];
            for (int y = 0; y < px; y++)
            for (int x = 0; x < px; x++)
            {
                int s = (y * px + x) * 4;
                cols[(px - 1 - y) * px + x] = new Color32(bytes[s], bytes[s + 1], bytes[s + 2], bytes[s + 3]);
            }
            Release();
            _tex = new Texture2D(px, px, TextureFormat.RGBA32, false)
            {
                filterMode = px >= 160 ? FilterMode.Bilinear : FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "mission-patch",
            };
            _tex.SetPixels32(cols);
            _tex.Apply(false, true);
            _image.image = _tex;
            _px = px;
        }

        void Release()
        {
            if (_tex == null) return;
            _image.image = null;
            if (Application.isPlaying) Object.Destroy(_tex);
            else Object.DestroyImmediate(_tex);
            _tex = null;
        }
    }
}

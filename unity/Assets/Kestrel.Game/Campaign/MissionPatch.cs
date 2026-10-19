// Reusable mission patch. MissionPatch.Render(missionId, size) draws a baked KH badge when
// Resources has one, and the procedural embroidery for a contract, a free flight, or a
// missing file. The texture is point-sampled when small and bilinear when it is large.
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

        static readonly System.Collections.Generic.Dictionary<string, Texture2D> TexCache = new System.Collections.Generic.Dictionary<string, Texture2D>();
        const int TexCacheCap = 96;

        /// <summary>Textures allocated since the last <see cref="ResetTextureStats"/>. The second collection build should stay at zero.</summary>
        public static int TexturesCreated { get; private set; }

        public static void ResetTextureStats() => TexturesCreated = 0;

        /// <summary>Drop cached discs. Edit Mode destroys them now so the next build measures a cold cache.</summary>
        public static void DiscardTextureCache()
        {
            foreach (var kv in TexCache)
                DestroyTex(kv.Value);
            TexCache.Clear();
        }

        static void DestroyTex(Texture2D tex)
        {
            if (tex == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(tex);
            else UnityEngine.Object.DestroyImmediate(tex);
        }

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

        static byte[] FromResources(string missionId, int px, PatchChrome chrome)
        {
            var spec = MissionPatchArt.Resolve(missionId);
            if (spec.Index < 1 || spec.Index > MissionPatchArt.CampaignCount) return null;
            Texture2D tex = null;
            bool treated = false;
            if (chrome != PatchChrome.Open)
            {
                tex = Resources.Load<Texture2D>(MissionPatchArt.BakedResource(spec.Index, chrome));
                treated = tex != null;
            }
            if (tex == null)
                tex = Resources.Load<Texture2D>(MissionPatchArt.BakedResource(spec.Index, PatchChrome.Open));
            if (tex == null || tex.width < 1 || tex.height < 1 || !tex.isReadable) return null;
            Color32[] pix;
            try { pix = tex.GetPixels32(); }
            catch (System.Exception) { return null; }
            if (pix == null || pix.Length < tex.width * tex.height) return null;
            int w = tex.width;
            int h = tex.height;
            var rgba = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
            {
                int src = (h - 1 - y) * w;
                int dst = y * w * 4;
                for (int x = 0; x < w; x++)
                {
                    var c = pix[src + x];
                    rgba[dst] = c.r;
                    rgba[dst + 1] = c.g;
                    rgba[dst + 2] = c.b;
                    rgba[dst + 3] = c.a;
                    dst += 4;
                }
            }
            return MissionPatchArt.Present(rgba, w, h, px, treated ? PatchChrome.Open : chrome);
        }

        void Rebuild(bool force)
        {
            float ppp = 1f;
            if (panel != null && panel.scaledPixelsPerPoint >= 1f) ppp = panel.scaledPixelsPerPoint;
            int px = Mathf.Clamp(Mathf.RoundToInt(_size * ppp), 32, MissionPatchArt.UiTextureCap);
            if (!force && _tex != null && px == _px) return;
            _px = px;
            string key = _id + "\n" + px.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" + ((int)_chrome).ToString(System.Globalization.CultureInfo.InvariantCulture);
            Release();
            if (TexCache.TryGetValue(key, out var shared) && shared != null)
            {
                _tex = shared;
                _image.image = shared;
                return;
            }
            byte[] bytes = FromResources(_id, px, _chrome) ?? MissionPatchArt.Rgba(_id, px, _chrome);
            var cols = new Color32[px * px];
            for (int y = 0; y < px; y++)
            for (int x = 0; x < px; x++)
            {
                int s = (y * px + x) * 4;
                cols[(px - 1 - y) * px + x] = new Color32(bytes[s], bytes[s + 1], bytes[s + 2], bytes[s + 3]);
            }
            var tex = new Texture2D(px, px, TextureFormat.RGBA32, false)
            {
                filterMode = px >= 160 ? FilterMode.Bilinear : FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "mission-patch",
            };
            tex.SetPixels32(cols);
            tex.Apply(false, true);
            TexturesCreated++;
            if (TexCache.Count >= TexCacheCap) TexCache.Clear();
            TexCache[key] = tex;
            _tex = tex;
            _image.image = tex;
            _px = px;
        }

        void Release()
        {
            // Cached textures are shared across cells. Drop the image reference only.
            if (_image != null) _image.image = null;
            _tex = null;
        }
    }
}

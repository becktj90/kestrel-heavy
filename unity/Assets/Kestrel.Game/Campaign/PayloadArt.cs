// Payload card art and the URP mesh. Both are built from PayloadForms. The card art is the lit CPU render
// (PayloadRender via PayloadThumbnails, at physical pixels); a mission list must not spawn a camera. The flat
// portrait stays as the instant placeholder while a render is time-sliced.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.MissionSelect
{
    public static class PayloadArt
    {
        static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        /// <summary>A square lit payload render <paramref name="size"/> points across, on a clear backdrop.</summary>
        public static VisualElement Element(string name, string form, int size) =>
            Element(name, form, size, PayloadBackdrop.Clear, urgent: false);

        public static PayloadThumb Element(string name, string form, int size, PayloadBackdrop backdrop, bool urgent)
        {
            if (size < 32) size = 32;
            var el = new PayloadThumb(name, form, backdrop, size, urgent);
            el.style.width = size;
            el.style.height = size;
            el.style.flexShrink = 0;
            el.style.alignSelf = Align.Center;
            return el;
        }

        /// <summary>The lit render as a texture <paramref name="size"/> pixels square (cached; renders now if new).</summary>
        public static Texture Texture(string form, int size, PayloadBackdrop backdrop = PayloadBackdrop.Clear) =>
            PayloadThumbnails.RenderNow(form, size, backdrop);

        /// <summary>The flat painted portrait (instant): the placeholder until the lit render lands.</summary>
        public static Texture2D Portrait(string form, int size)
        {
            string key = (string.IsNullOrEmpty(form) ? "box" : form) + ":" + size.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            byte[] rgba = PayloadPortrait.Rgba(form, size);
            byte[] upright = FlipVertical(rgba, size);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "KH_Payload_" + (string.IsNullOrEmpty(form) ? "box" : form),
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixelData(upright, 0);
            tex.Apply(false, true);
            Cache[key] = tex;
            return tex;
        }

        /// <summary>CPU rows start at the top. Unity pixel data starts at the bottom.</summary>
        static byte[] FlipVertical(byte[] src, int size)
        {
            int stride = size * 4;
            var dst = new byte[src.Length];
            for (int y = 0; y < size; y++)
                Buffer.BlockCopy(src, y * stride, dst, (size - 1 - y) * stride, stride);
            return dst;
        }
    }

    /// <summary>Lit URP mesh of the same parts the card paints. Not used by the mission list.</summary>
    public static class PayloadMesh
    {
        public static GameObject Build(string form, Transform parent)
        {
            var root = new GameObject("payload-" + (string.IsNullOrEmpty(form) ? "box" : form));
            if (parent != null) root.transform.SetParent(parent, false);
            var shared = Resources.Load<Material>("Materials/KH_LitEmission");
            Shader lit = shared == null ? Shader.Find("Universal Render Pipeline/Lit") : null;
            var parts = PayloadForms.Of(form);
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "part-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(part.X, part.Y, part.Z);
                go.transform.localScale = new Vector3(Mathf.Max(0.02f, part.W), Mathf.Max(0.02f, part.H), Mathf.Max(0.02f, part.D));
                var col = go.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(col);
                    else UnityEngine.Object.DestroyImmediate(col);
                }
                var renderer = go.GetComponent<MeshRenderer>();
                Material mat = shared != null ? new Material(shared) : (lit != null ? new Material(lit) : renderer.sharedMaterial);
                var color = new Color(part.R / 255f, part.G / 255f, part.B / 255f, 1f);
                if (mat != null)
                {
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                    mat.color = color;
                    renderer.sharedMaterial = mat;
                }
            }
            return root;
        }
    }
}

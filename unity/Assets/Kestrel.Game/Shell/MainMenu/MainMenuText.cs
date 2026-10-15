// World-space TextMeshPro for the hangar HUD. Glyphs are drawn once, at a high atlas point size,
// with padding so the SDF edge stays sharp when the canvas is scaled down to the panel.
// Underlay stays off: it reprints the glyph and was the doubled-letter look on the flat menu.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Kestrel.Game.Shell
{
    public sealed class MainMenuLabel : MonoBehaviour
    {
        public TMP_Text Text;
        public Material Face;
        public MenuTextRole Role;

        public void SetGlow(float power)
        {
            if (Face != null && Face.HasProperty(ShaderUtilities.ID_GlowPower))
                Face.SetFloat(ShaderUtilities.ID_GlowPower, power);
        }

        void OnDestroy()
        {
            // fontMaterial is a per-label instance. TMP does not destroy it, and it is not the
            // font asset's shared material (that name does not end in "(Instance)").
            if (Face == null || Face.name == null || !Face.name.EndsWith("(Instance)")) return;
            var face = Face;
            Face = null;
            if (Application.isPlaying) Destroy(face);
            else DestroyImmediate(face);
        }
    }

    public static class MainMenuText
    {
        const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:./%+, \u00B7-";
        static TMP_FontAsset _font;
        static bool _failed;

        public static Canvas CreateCanvas(Transform parent, float worldW, float worldH, float pixelsWide, int sortOrder, Camera cam)
        {
            var go = new GameObject("Canvas");
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortOrder;
            // TMP SDF needs the extra channels or the outline/glow samples the wrong data and fringes.
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            var rt = canvas.GetComponent<RectTransform>();
            float pxH = Mathf.Max(64f, pixelsWide * Mathf.Max(0.05f, worldH / Mathf.Max(0.01f, worldW)));
            rt.sizeDelta = new Vector2(pixelsWide, pxH);
            float scale = worldW / pixelsWide;
            rt.localScale = new Vector3(scale, scale, scale);
            rt.localPosition = new Vector3(0f, 0f, 0.02f);
            rt.localRotation = Quaternion.identity;
            return canvas;
        }

        public static void FitCanvas(Canvas canvas, float worldW, float worldH, float pixelsWide)
        {
            if (canvas == null) return;
            var rt = canvas.GetComponent<RectTransform>();
            float pxH = Mathf.Max(64f, pixelsWide * Mathf.Max(0.05f, worldH / Mathf.Max(0.01f, worldW)));
            rt.sizeDelta = new Vector2(pixelsWide, pxH);
            float scale = worldW / pixelsWide;
            rt.localScale = new Vector3(scale, scale, scale);
        }

        /// <summary>One line, anchored in the canvas (0 center, y -0.5..0.5). Returns null if the font cannot be built.</summary>
        public static MainMenuLabel Line(Transform canvas, string text, MenuTextRole role, float yAnchor, TextAlignmentOptions align, float leftInset)
        {
            var font = FontAsset();
            if (font == null) return null;
            var go = new GameObject(text);
            go.transform.SetParent(canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f + yAnchor);
            rt.anchorMax = new Vector2(1f, 0.5f + yAnchor);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, StyleHeight(role));
            rt.anchoredPosition = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            Apply(tmp, font, text, role, align, leftInset);
            var label = go.AddComponent<MainMenuLabel>();
            label.Text = tmp;
            label.Role = role;
            label.Face = tmp.fontMaterial;
            return label;
        }

        public static void Apply(TMP_Text tmp, TMP_FontAsset font, string text, MenuTextRole role, TextAlignmentOptions align, float leftInset)
        {
            var style = MainMenuLayout.Style(role);
            tmp.font = font;
            tmp.text = text ?? "";
            tmp.fontSize = style.FontSize;
            tmp.enableAutoSizing = style.AutoSize;
            tmp.extraPadding = style.ExtraPadding;
            tmp.richText = style.RichText;
            tmp.parseCtrlCharacters = false;
            tmp.textWrappingMode = style.WordWrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.characterSpacing = style.CharacterSpacing;
            tmp.wordSpacing = 0f;
            tmp.lineSpacing = 0f;
            tmp.paragraphSpacing = 0f;
            tmp.fontStyle = style.Bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment = align;
            tmp.color = role == MenuTextRole.Launch ? Color.white : MainMenuMaterials.Cream;
            tmp.raycastTarget = false;
            tmp.margin = new Vector4(leftInset, 0f, 16f, 0f);
            var mat = tmp.fontMaterial;
            if (mat == null) return;
            var shader = MainMenuMaterials.TmpShader();
            if (shader != null) mat.shader = shader;
            SetFloat(mat, ShaderUtilities.ID_OutlineWidth, style.OutlineWidth);
            SetFloat(mat, ShaderUtilities.ID_FaceDilate, style.FaceDilate);
            SetFloat(mat, ShaderUtilities.ID_OutlineSoftness, 0.02f);
            if (mat.HasProperty(ShaderUtilities.ID_OutlineColor))
                mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.15f, 0.85f, 1f, 0.9f));
            // Glow is a halo in the SDF, not a second glyph. Underlay stays disabled on purpose.
            mat.DisableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            mat.EnableKeyword(ShaderUtilities.Keyword_Glow);
            if (mat.HasProperty(ShaderUtilities.ID_GlowColor))
                mat.SetColor(ShaderUtilities.ID_GlowColor, new Color(0.35f, 0.95f, 1f, 0.85f));
            SetFloat(mat, ShaderUtilities.ID_GlowOffset, 0f);
            SetFloat(mat, ShaderUtilities.ID_GlowInner, 0.05f);
            SetFloat(mat, ShaderUtilities.ID_GlowOuter, style.GlowOuter);
            SetFloat(mat, ShaderUtilities.ID_GlowPower, style.GlowPower);
            tmp.UpdateMeshPadding();
        }

        static float StyleHeight(MenuTextRole role) => MainMenuLayout.Style(role).FontSize * 1.35f;

        public static TMP_FontAsset FontAsset()
        {
            if (_font != null) return _font;
            if (_failed) return null;
            try
            {
                var os = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (os == null) os = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (os == null) { _failed = true; return null; }
                // 96 px sampling with 9 px padding is the SDF ratio that stays crisp on a small panel.
                _font = TMP_FontAsset.CreateFontAsset(os, 96, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                if (_font == null) { _failed = true; return null; }
                var shader = MainMenuMaterials.TmpShader();
                if (shader != null && _font.material != null) _font.material.shader = shader;
                if (_font.material == null || !MainMenuMaterials.Usable(_font.material.shader))
                {
                    _failed = true;
                    _font = null;
                    return null;
                }
                _font.TryAddCharacters(Alphabet, out string _);
                return _font;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Kestrel] TextMeshPro font was not created: " + e.Message);
                _failed = true;
                return null;
            }
        }

        /// <summary>Bitmap quads from the OS font when the SDF shader is missing, so the labels still read.</summary>
        public static Renderer Fallback(Transform parent, string text, float worldW, float worldH, int pixels, List<Object> owned)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null || string.IsNullOrEmpty(text)) return null;
            font.RequestCharactersInTexture(text, pixels, FontStyle.Normal);
            float width = 0f, height = 0f;
            var infos = new CharacterInfo[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                font.GetCharacterInfo(text[i], out infos[i], pixels, FontStyle.Normal);
                width += infos[i].advance;
                height = Mathf.Max(height, infos[i].glyphHeight);
            }
            if (width < 1f) return null;
            float fit = worldW * 0.86f / width;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            float pen = -width * 0.5f * fit;
            for (int i = 0; i < text.Length; i++)
            {
                var info = infos[i];
                float x0 = pen + info.minX * fit, x1 = pen + info.maxX * fit;
                float y0 = info.minY * fit, y1 = info.maxY * fit;
                int b = v.Count;
                v.Add(new Vector3(x0, y0, 0f)); v.Add(new Vector3(x1, y0, 0f));
                v.Add(new Vector3(x0, y1, 0f)); v.Add(new Vector3(x1, y1, 0f));
                uv.Add(info.uvBottomLeft); uv.Add(info.uvBottomRight); uv.Add(info.uvTopLeft); uv.Add(info.uvTopRight);
                tri.Add(b); tri.Add(b + 2); tri.Add(b + 1); tri.Add(b + 2); tri.Add(b + 3); tri.Add(b + 1);
                pen += info.advance * fit;
            }
            var mesh = new Mesh { name = "KH_FallbackText" };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            if (owned != null) owned.Add(mesh);
            var go = new GameObject("Fallback " + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.03f);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            var mat = MainMenuMaterials.Glass(new Color(0.95f, 0.95f, 0.92f, 1f), 0.4f)
                ?? MainMenuMaterials.Body(MainMenuMaterials.Cream, 0f, 0.2f, MainMenuMaterials.Cyan * 0.3f);
            if (mat != null)
            {
                var tex = font.material != null ? font.material.mainTexture : null;
                if (tex != null) { mat.SetTexture("_BaseMap", tex); mat.mainTexture = tex; }
                rend.sharedMaterial = mat;
                if (owned != null) owned.Add(mat);
            }
            return rend;
        }

        static void SetFloat(Material mat, int id, float value)
        {
            if (mat != null && mat.HasProperty(id)) mat.SetFloat(id, value);
        }
    }
}

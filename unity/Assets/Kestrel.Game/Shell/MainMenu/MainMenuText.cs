// Hangar labels are glyph quads from the OS font, not TextMeshPro. A runtime SDF atlas
// filled solid on device: the wordmark became a white bar, telemetry frames stayed empty,
// and the launch slab had no words. The panel faces the camera, so local +X is screen-left
// and font-right is stored as local −X.
using System.Collections.Generic;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public sealed class MainMenuLabel : MonoBehaviour
    {
        public MenuTextRole Role;
        public Color Tint = Color.white;

        MeshFilter _filter;
        MeshRenderer _rend;
        Mesh _mesh;
        Material _mat;
        string _text = "";
        float _worldW, _worldH;

        public void Bind(MeshFilter filter, MeshRenderer rend, Mesh mesh, Material mat, MenuTextRole role, Color tint)
        {
            _filter = filter;
            _rend = rend;
            _mesh = mesh;
            _mat = mat;
            Role = role;
            Tint = tint;
            ApplyTint(Tint);
        }

        public void Place(Vector3 localPos, float worldW, float worldH)
        {
            transform.localPosition = localPos;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            _worldW = worldW;
            _worldH = worldH;
            Rebuild();
        }

        public void SetText(string text)
        {
            text = text ?? "";
            if (_text == text) return;
            _text = text;
            Rebuild();
        }

        public void SetGlow(float power)
        {
            float boost = Mathf.Lerp(0.9f, 1.2f, Mathf.Clamp01(power));
            var c = Tint;
            c.r = Mathf.Clamp01(Tint.r * boost);
            c.g = Mathf.Clamp01(Tint.g * boost);
            c.b = Mathf.Clamp01(Tint.b * boost);
            ApplyTint(c);
        }

        void ApplyTint(Color c)
        {
            if (_mat == null) return;
            _mat.SetColor("_BaseColor", c);
            if (_mat.HasProperty("_Color")) _mat.SetColor("_Color", c);
        }

        void Rebuild()
        {
            if (_mesh == null || _rend == null) return;
            bool drew = _mat != null && MainMenuText.Fill(_text, _worldW, _worldH, Role, _mesh, _mat);
            _rend.enabled = drew;
        }
    }

    public static class MainMenuText
    {
        public const int PixelSize = 48;   // atlas size close to the on-screen size: 96 px minified 3x+ shimmered on phones
        /// <summary>Longest telemetry line. HUD lines are laid out as if this wide so every line gets the same glyph size.</summary>
        public const string HudWidest = "THRUST: 851 kN";
        public const float FaceClearance = 0.02f;
        public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789:./%+, \u00B7-";

        static Font _font;

        /// <summary>Telemetry and the KH-1 plate use the bold face: the normal weight is hairline at ~15 px on a phone.</summary>
        public static FontStyle StyleFor(MenuTextRole role) => role == MenuTextRole.Hud || role == MenuTextRole.Dial ? FontStyle.Bold : FontStyle.Normal;

        /// <summary>One glyph in font pixels, origin at the pen, y up the way CharacterInfo is.</summary>
        public readonly struct GlyphStamp
        {
            public readonly float MinX, MaxX, MinY, MaxY, Advance;
            public readonly Vector2 UvBL, UvBR, UvTL, UvTR;

            public GlyphStamp(float minX, float maxX, float minY, float maxY, float advance,
                Vector2 uvBL, Vector2 uvBR, Vector2 uvTL, Vector2 uvTR)
            {
                MinX = minX; MaxX = maxX; MinY = minY; MaxY = maxY; Advance = advance;
                UvBL = uvBL; UvBR = uvBR; UvTL = uvTL; UvTR = uvTR;
            }

            public static GlyphStamp From(CharacterInfo info) => new GlyphStamp(
                info.minX, info.maxX, info.minY, info.maxY, info.advance,
                info.uvBottomLeft, info.uvBottomRight, info.uvTopLeft, info.uvTopRight);

            public bool Visible => MaxX - MinX >= 0.5f && MaxY - MinY >= 0.5f;
        }

        /// <summary>Distance in front of a slab's center. The slab's near face is at half its thickness.</summary>
        public static float FaceZ(float slabThickness) => slabThickness * 0.5f + FaceClearance;

        /// <summary>Font x grows to the right. On a camera-facing panel that is local −X, which is screen-right.</summary>
        public static float LocalX(float fontX, float fit) => -fontX * fit;

        /// <summary>Which channel holds glyph coverage: 1 (red) only for formats with no alpha channel, else 0 (alpha).</summary>
        public static float CoverageModeFor(TextureFormat f)
        {
            switch (f)
            {
                case TextureFormat.R8: case TextureFormat.R16: case TextureFormat.RFloat: case TextureFormat.RHalf:
                case TextureFormat.RGB24: case TextureFormat.RGB565: case TextureFormat.RGB9e5Float: case TextureFormat.DXT1: case TextureFormat.ETC_RGB4: case TextureFormat.ETC2_RGB:
                    return 1f;
                default: return 0f;
            }
        }

        public static void Warmup()
        {
            var font = OsFont();
            if (font == null) return;
            var read = MainMenuLayout.Readout(0f, true);
            string sample = Alphabet + MainMenuLayout.LaunchLabel + MainMenuLayout.TitleTop + MainMenuLayout.TitleBottom
                + MainMenuLayout.Tagline + MainMenuLayout.Designation;
            for (int i = 0; i < read.All().Length; i++) sample += read.All()[i];
            font.RequestCharactersInTexture(sample, PixelSize, FontStyle.Normal);
            font.RequestCharactersInTexture(sample, PixelSize, FontStyle.Bold);
        }

        public static bool Fill(string text, float worldW, float worldH, MenuTextRole role, Mesh mesh, Material mat)
        {
            if (mesh == null) return false;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var colors = new List<Color>();
            bool ok = Build(text, worldW, worldH, role, verts, uvs, tris);
            mesh.Clear();
            if (!ok)
            {
                return false;
            }
            for (int i = 0; i < verts.Count; i++) colors.Add(Color.white);
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            var font = OsFont();
            var tex = font != null && font.material != null ? font.material.mainTexture : null;
            if (mat != null && tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.mainTexture = tex;
                if (mat.HasProperty("_CoverageMode")) mat.SetFloat("_CoverageMode", CoverageModeFor(tex is Texture2D t2 ? t2.format : TextureFormat.Alpha8));
            }
            return tex != null;
        }

        public static bool Build(string text, float worldW, float worldH, MenuTextRole role,
            List<Vector3> verts, List<Vector2> uvs, List<int> tris)
        {
            var glyphs = new List<GlyphStamp>();
            if (!string.IsNullOrEmpty(text))
            {
                var font = OsFont();
                if (font != null)
                {
                    var fs = StyleFor(role);
                    font.RequestCharactersInTexture(text, PixelSize, fs);
                    for (int i = 0; i < text.Length; i++)
                    {
                        if (font.GetCharacterInfo(text[i], out var info, PixelSize, fs))
                            glyphs.Add(GlyphStamp.From(info));
                        else
                            glyphs.Add(new GlyphStamp(0f, 0f, 0f, 0f, PixelSize * 0.33f, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero));
                    }
                }
            }
            float extra = MainMenuLayout.Style(role).CharacterSpacing * PixelSize * 0.01f;
            float minPxW = 0f;
            if (role == MenuTextRole.Hud && glyphs.Count > 0)
            {
                var font = OsFont();
                if (font != null)
                {
                    font.RequestCharactersInTexture(HudWidest, PixelSize, StyleFor(role));
                    for (int i = 0; i < HudWidest.Length; i++)
                        if (font.GetCharacterInfo(HudWidest[i], out var info, PixelSize, StyleFor(role))) minPxW += Mathf.Max(0f, info.advance) + extra;
                }
            }
            return Layout(glyphs, worldW, worldH, extra, verts, uvs, tris, minPxW);
        }

        /// <summary>Writes one quad per visible glyph, centered in the box. False when there is nothing to draw.</summary>
        public static bool Layout(IList<GlyphStamp> glyphs, float worldW, float worldH, float extraPx,
            List<Vector3> verts, List<Vector2> uvs, List<int> tris, float minPxW = 0f)
        {
            if (verts == null || uvs == null || tris == null) return false;
            verts.Clear();
            uvs.Clear();
            tris.Clear();
            if (glyphs == null || glyphs.Count == 0 || worldW <= 0.001f || worldH <= 0.001f) return false;

            float pxW = 0f, minY = float.MaxValue, maxY = float.MinValue;
            int drawn = 0;
            for (int i = 0; i < glyphs.Count; i++)
            {
                var g = glyphs[i];
                pxW += Mathf.Max(0f, g.Advance);
                if (i + 1 < glyphs.Count) pxW += extraPx;
                if (!g.Visible) continue;
                drawn++;
                minY = Mathf.Min(minY, g.MinY);
                maxY = Mathf.Max(maxY, g.MaxY);
            }
            pxW = Mathf.Max(pxW, minPxW);
            float pxH = maxY - minY;
            if (drawn == 0 || pxW < 1f || pxH < 0.5f) return false;
            float fit = Mathf.Min(worldW / pxW, worldH / pxH);
            if (fit <= 0f) return false;

            float mid = (minY + maxY) * 0.5f;
            float pen = -pxW * 0.5f;
            for (int i = 0; i < glyphs.Count; i++)
            {
                var g = glyphs[i];
                if (g.Visible)
                {
                    float x0 = LocalX(pen + g.MinX, fit);
                    float x1 = LocalX(pen + g.MaxX, fit);
                    float y0 = (g.MinY - mid) * fit;
                    float y1 = (g.MaxY - mid) * fit;
                    int b = verts.Count;
                    verts.Add(new Vector3(x0, y0, 0f));
                    verts.Add(new Vector3(x1, y0, 0f));
                    verts.Add(new Vector3(x0, y1, 0f));
                    verts.Add(new Vector3(x1, y1, 0f));
                    uvs.Add(g.UvBL);
                    uvs.Add(g.UvBR);
                    uvs.Add(g.UvTL);
                    uvs.Add(g.UvTR);
                    tris.Add(b);
                    tris.Add(b + 2);
                    tris.Add(b + 1);
                    tris.Add(b + 2);
                    tris.Add(b + 3);
                    tris.Add(b + 1);
                }
                pen += Mathf.Max(0f, g.Advance);
                if (i + 1 < glyphs.Count) pen += extraPx;
            }
            return verts.Count > 0;
        }

        static Font OsFont()
        {
            if (_font != null) return _font;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _font;
        }
    }
}

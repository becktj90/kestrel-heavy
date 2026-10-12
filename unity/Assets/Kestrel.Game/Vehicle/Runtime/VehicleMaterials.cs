// Vehicle-side material access. Everything derives from Kestrel.Kit.MaterialLibrary; nothing here invents a look.
// STOPGAP (flagged in the PR): the kit has no "tank skin with isogrid normal" role yet, so the shell uses ONE cached
// variant of the kit PaintedSteel material with the generated isogrid normal map. Same shader, so the SRP Batcher
// still groups it. Replace with MaterialLibrary.Get(<vehicle role>) once Graphics Dev adds vehicle roles.
using Kestrel.Kit;
using UnityEngine;

namespace Kestrel.Vehicle
{
    public static class VehicleMaterials
    {
        public const int IsogridTileWidth = 128, IsogridTileHeight = 222;   // ~ 1 : sqrt(3) tile
        static Material _shell; static Texture2D _isoNormal;

        public static Material ShellIsogrid(float isogridCell = 0.30f)
        {
            if (_shell != null) return _shell;
            var baseMat = MaterialLibrary.Get(MaterialRole.PaintedSteel);
            if (baseMat == null) return null;
            _shell = new Material(baseMat) { name = "KH_Vehicle_TankShell_Isogrid", enableInstancing = true };
            _shell.SetTexture("_BumpMap", IsogridNormal());
            _shell.SetFloat("_BumpScale", 0.6f);
            _shell.EnableKeyword("_NORMALMAP");
            // Shell UVs are metres (around, along); one tile = cell wide, 2 rows (sqrt 3 * cell) tall.
            _shell.SetTextureScale("_BaseMap", new Vector2(1f / isogridCell, 1f / (isogridCell * Mathf.Sqrt(3f))));
            return _shell;
        }

        public static Texture2D IsogridNormal()
        {
            if (_isoNormal != null) return _isoNormal;
            _isoNormal = new Texture2D(IsogridTileWidth, IsogridTileHeight, TextureFormat.RGBA32, true, true)
            { name = "KH_IsogridNormal", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            _isoNormal.SetPixelData(Isogrid.NormalTileRgba(IsogridTileWidth, IsogridTileHeight), 0);
            _isoNormal.Apply(true, true);   // mips, then free the CPU copy
            return _isoNormal;
        }

        /// <summary>Generated texture bytes held (RGBA32 + mips ~ 1.33x).</summary>
        public static long GeneratedTextureBytes => _isoNormal == null ? 0 : (long)(IsogridTileWidth * IsogridTileHeight * 4 * 1.34f);

        public static void ClearCache()
        {
            if (_shell != null) Object.DestroyImmediate(_shell);
            if (_isoNormal != null) Object.DestroyImmediate(_isoNormal);
            _shell = null; _isoNormal = null;
        }
    }
}

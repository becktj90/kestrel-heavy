// Marketing export. Menu: Kestrel/Export Mission Patches.
// Headless: Unity -executeMethod Kestrel.Game.Editor.MissionPatchMenu.Export
// with KESTREL_PATCH_DIR set, or the dotnet CLI: kestrel patches <dir> <size>.
using Kestrel.Core.Flight;
using UnityEditor;
using UnityEngine;

namespace Kestrel.Game.Editor
{
    public static class MissionPatchMenu
    {
        [MenuItem("Kestrel/Export Mission Patches")]
        public static void Export()
        {
            string dir = System.Environment.GetEnvironmentVariable("KESTREL_PATCH_DIR");
            if (string.IsNullOrEmpty(dir))
                dir = EditorUtility.OpenFolderPanel("Export mission patches", "", "");
            if (string.IsNullOrEmpty(dir)) return;
            int n = MissionPatchArt.WriteAll(dir, 512);
            Debug.Log("Wrote " + n + " mission patches to " + dir);
        }
    }
}

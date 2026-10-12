// Hosts the real Design screen (SliceApp: design panel, pad, vehicle, framing, flight) inside the shell's
// additive Design scene. Generated next to SliceApp by ShellSetup (via SliceBootstrap's populate hook);
// nothing is placed by hand. While this scene is loaded the persistent shell camera and shell UI are
// hidden so SliceApp renders alone (no double clear, no stacked panels); they come back on unload.
using Kestrel.Game.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    [DefaultExecutionOrder(-500)]   // after AppShell (-1000), before SliceApp builds its camera
    public sealed class ShellDesignHost : MonoBehaviour
    {
        Camera shellCam; DisplayStyle prevDisplay;

        void OnEnable()
        {
            var shell = AppShell.Instance;
            if (shell == null) return;   // Slice.unity opened directly: nothing to hide
            if (shell.Fill != null) { shellCam = shell.Fill.GetComponent<Camera>(); if (shellCam != null) shellCam.enabled = false; }
            if (shell.Root != null) { prevDisplay = shell.Root.resolvedStyle.display; shell.Root.style.display = DisplayStyle.None; }
        }

        void OnDisable()
        {
            if (shellCam != null) shellCam.enabled = true;
            var shell = AppShell.Instance;
            if (shell != null && shell.Root != null) shell.Root.style.display = prevDisplay;
        }
    }
}

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
        AudioListener shellListener; bool priorListener;
        Camera shellCam; DisplayStyle prevDisplay;

        void OnEnable()
        {
            var shell = AppShell.Instance;
            if (shell == null) return;   // Slice.unity opened directly: nothing to hide
            if (shell.Fill != null) { shellCam = shell.Fill.GetComponent<Camera>(); if (shellCam != null) shellCam.enabled = false; }
            if (shellCam != null)
            {
                shellListener = shellCam.GetComponent<AudioListener>();
                if (shellListener != null) { priorListener = shellListener.enabled; shellListener.enabled = false; }
            }
            if (shell.Root != null) { prevDisplay = shell.Root.resolvedStyle.display; shell.Root.style.display = DisplayStyle.None; }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>DEV ONLY (shot capture via CLI): collapse/expand the Design panel of the live SliceApp.
        /// Calls SliceApp's private SetDesignCollapsed via SendMessage so SliceApp itself is untouched. Returns false when no SliceApp is loaded.</summary>
        public static bool SetPanelCollapsed(bool collapsed)
        {
            var app = Object.FindAnyObjectByType<SliceApp>();
            if (app == null) return false;
            app.SendMessage("SetDesignCollapsed", collapsed, SendMessageOptions.DontRequireReceiver);
            return true;
        }
#endif

        void OnDisable()
        {
            if (shellCam != null) shellCam.enabled = true;
            if (shellListener != null) shellListener.enabled = priorListener;
            var shell = AppShell.Instance;
            if (shell != null && shell.Root != null) shell.Root.style.display = prevDisplay;
        }
    }
}

// Landscape web panel scale (LANDSCAPE_WEB_BRIEF §1). The shell asset is a 375x667 match-height
// phone reference, so a 1080p monitor drew phone type 1.6x and a 1440p one 2.2x. In the WebGL player
// on a landscape viewport the shell and Slice panels switch to 1280x720 matched halfway, which makes
// every 16:9 monitor a 1280x720 panel and lets WebLandscapeType's caps be panel points. Portrait,
// iOS and the editor never take this path, so the committed assets and the phone column are untouched.
using System.Collections.Generic;
using Kestrel.Core.Flight;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    public static class WebPanelScale
    {
        struct Saved { public PanelScaleMode mode; public Vector2Int reference; public float match; public PanelScreenMatchMode screen; }

        static readonly Dictionary<PanelSettings, Saved> Original = new Dictionary<PanelSettings, Saved>();

        /// <summary>The runtime gate: WebGL player and a landscape viewport.</summary>
        public static bool Wide => WebLayoutMode.UseWide(Screen.width, Screen.height);

        /// <summary>Switch <paramref name="ps"/> to the landscape reference, or back to the asset's own. Returns true on a change.</summary>
        public static bool Apply(PanelSettings ps, bool wide)
        {
            if (ps == null) return false;
            if (!Original.TryGetValue(ps, out var saved))
            {
                if (!wide) return false;
                saved = new Saved { mode = ps.scaleMode, reference = ps.referenceResolution, match = ps.match, screen = ps.screenMatchMode };
                Original[ps] = saved;
            }
            if (wide)
            {
                var reference = new Vector2Int((int)WebLandscapeType.RefW, (int)WebLandscapeType.RefH);
                if (ps.scaleMode == PanelScaleMode.ScaleWithScreenSize && Same(ps.referenceResolution, reference)
                    && Mathf.Approximately(ps.match, WebLandscapeType.Match)) return false;
                ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                ps.referenceResolution = reference;
                ps.match = WebLandscapeType.Match;
                return true;
            }
            if (Same(ps.referenceResolution, saved.reference) && Mathf.Approximately(ps.match, saved.match)) return false;
            ps.scaleMode = saved.mode;
            ps.screenMatchMode = saved.screen;
            ps.referenceResolution = saved.reference;
            ps.match = saved.match;
            return true;
        }

        static bool Same(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;

        /// <summary>Polled from Update: re-applies only when the viewport size changed.</summary>
        public sealed class Watch
        {
            int w = -1, h = -1;
            public bool Tick(PanelSettings ps)
            {
                if (Screen.width == w && Screen.height == h) return false;
                w = Screen.width; h = Screen.height;
                return Apply(ps, Wide);
            }
        }
    }
}

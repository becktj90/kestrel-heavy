// Test Flight (DECISIONS #102) on the Sandbox free-build path: the rocket a test opens with. The last test rocket
// (DesignLibrary.LoadTestFlight, its own save record), else a starter template, so the designer never opens empty.
using System;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public static class TestFlightHome
    {
        /// <summary>The last Test Flight rocket on this device, or null.</summary>
        public static JsonObject Last()
        {
            try
            {
                CreditsPlay.Boot();
                var store = CreditsPlay.Store;
                if (store == null) return null;
                return new DesignLibrary(store).LoadTestFlight();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Kestrel] test flight rocket: " + e.Message);
                return null;
            }
        }

        /// <summary>New Design: the same starter the free designer opens with (Hopper, else Workhorse, else the first
        /// template). Null when no hangar data loads; the designer then keeps its own fallback.</summary>
        public static JsonObject Starter()
        {
            try
            {
                var h = ScreenStub.LoadHangar();
                var t = h?.Template("hopper") ?? h?.Template("workhorse") ?? (h != null && h.templates.Count > 0 ? h.templates[0] : null);
                return t != null ? HangarEditor.ForkTemplate(t) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Kestrel] test flight starter: " + e.Message);
                return null;
            }
        }

        /// <summary>The rocket a new test opens with: the last test rocket, else the starter.</summary>
        public static JsonObject Open() => Last() ?? Starter();
    }
}

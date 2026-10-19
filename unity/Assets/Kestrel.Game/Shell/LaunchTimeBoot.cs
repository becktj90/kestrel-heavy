// Player boot for the launch time-of-day table. A device build has no repo checkout, so
// LaunchTime.PickTimeOfDay cannot walk up to data/time-of-day.json there. CI (and Kestrel > Sync Data)
// copies data/*.json into Assets/Resources/KestrelData; this installs that TextAsset before the
// first scene loads. In the editor and dotnet tests nothing is installed and Core reads the checkout.
using System;
using Kestrel.Core.Flight;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public static class LaunchTimeBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Boot()
        {
            if (LaunchTime.HasTable) return;
            var asset = Resources.Load<TextAsset>(LaunchTime.ResourcePath);
            if (asset == null || string.IsNullOrEmpty(asset.text))
            {
                if (!Application.isEditor) Debug.LogWarning("[Kestrel] Resources/" + LaunchTime.ResourcePath + " missing; launch lighting falls back to Day");
                return;
            }
            try { LaunchTime.UseJson(asset.text); }
            catch (Exception e) { Debug.LogWarning("[Kestrel] time-of-day table: " + e.Message); }
        }
    }
}

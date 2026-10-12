// Loads data/quality.json (synced into Resources/KestrelData by Kestrel > Sync Data).
using System;
using UnityEngine;

namespace Kestrel.Game.Quality
{
    [Serializable] public sealed class QualityPreset
    {
        public string id, label, plume, padLayers, release;
        public float renderScaleDprCap = 2f;
        public int msaa = 1, particleBudget = 400, targetFps = 60, fallbackFps = 30;
        public bool hdr, bloom, shimmer, shadows, selectable = true;
    }
    [Serializable] public sealed class DefaultRule { public int minMemoryMb; public string preset; }
    [Serializable] public sealed class QualityDefaults { public DefaultRule[] rules; public int batterySaverFps = 30; }
    [Serializable] public sealed class GovernorJson
    {
        public int windowFrames = 120; public float percentile = 0.95f;
        public float stepDownMs = 20f, stepDownHoldSec = 3f, recoverMs = 16.7f, recoverHoldSec = 30f;
        public string floor = "low";
    }

    [Serializable] public sealed class QualityTable
    {
        public int version;
        public QualityPreset[] presets;
        public QualityDefaults defaults;
        public GovernorJson governor;

        public const string ResourcePath = "KestrelData/quality";

        public static QualityTable Load()
        {
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null) throw new InvalidOperationException("[Kestrel] Resources/" + ResourcePath + ".json missing. Run Kestrel > Sync Data.");
            return Parse(ta.text);
        }

        public static QualityTable Parse(string json) => JsonUtility.FromJson<QualityTable>(json);

        public int IndexOf(string id) => Array.FindIndex(presets, p => p.id == id);

        /// <summary>Shipping (2.0) presets only: Ultra is "release: 1.1" and not selectable.</summary>
        public int MaxSelectable()
        {
            int m = 0;
            for (int i = 0; i < presets.Length; i++) if (presets[i].selectable) m = i;
            return m;
        }

        public int DefaultFor(int systemMemoryMb)
        {
            foreach (var r in defaults.rules)
                if (systemMemoryMb >= r.minMemoryMb) { int i = IndexOf(r.preset); if (i >= 0) return Mathf.Min(i, MaxSelectable()); }
            return 0;
        }

        public GovernorConfig ToGovernorConfig() => new GovernorConfig
        {
            WindowFrames = governor.windowFrames, Percentile = governor.percentile,
            StepDownMs = governor.stepDownMs, StepDownHoldSec = governor.stepDownHoldSec,
            RecoverMs = governor.recoverMs, RecoverHoldSec = governor.recoverHoldSec,
        };
    }
}

// Ref-counted cache for generated vehicle meshes. Same key -> same Mesh (no duplicate builds); the Mesh is destroyed
// when the last user releases it, so rebuilds, cutaway toggles and destroyed views never leak.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kestrel.Vehicle
{
    public static class SharedMeshes
    {
        sealed class Entry { public Mesh Mesh; public int Refs; }
        static readonly Dictionary<string, Entry> Map = new Dictionary<string, Entry>();

        public static int Count => Map.Count;

        public static Mesh Acquire(string key, Func<Mesh> build)
        {
            if (Map.TryGetValue(key, out var e) && e.Mesh != null) { e.Refs++; return e.Mesh; }
            var m = build();
            Map[key] = new Entry { Mesh = m, Refs = 1 };
            return m;
        }

        public static void Release(string key)
        {
            if (key == null || !Map.TryGetValue(key, out var e)) return;
            if (--e.Refs > 0) return;
            Map.Remove(key);
            TankShellBuilder.Kill(e.Mesh);
        }

        public static int RefCount(string key) => key != null && Map.TryGetValue(key, out var e) ? e.Refs : 0;

        /// <summary>Tests / domain reload: destroy everything.</summary>
        public static void Clear() { foreach (var e in Map.Values) TankShellBuilder.Kill(e.Mesh); Map.Clear(); }
    }
}

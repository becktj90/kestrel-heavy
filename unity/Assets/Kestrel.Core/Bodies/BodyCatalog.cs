// Earth record for P1. Loaded from data/bodies.json. μ, radius and ω must match
// Gravity / Earth. J2 is stored for P2 and is not applied here.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Bodies
{
    public sealed class CelestialBody
    {
        public string id, label, role;
        public bool gravity;
        public double mu, radius, inverseFlattening, omega, j2;
    }

    public static class BodyCatalog
    {
        public static List<CelestialBody> Parse(string json)
        {
            var list = new List<CelestialBody>();
            var arr = MiniJson.ParseObject(json).Arr("bodies");
            if (arr == null) return list;
            foreach (JsonObject o in arr)
            {
                list.Add(new CelestialBody
                {
                    id = o.Str("id"),
                    label = o.Str("label"),
                    role = o.Str("role"),
                    gravity = o.Bool("gravity"),
                    mu = o.Num("mu"),
                    radius = o.Num("radius"),
                    inverseFlattening = o.Num("inverseFlattening"),
                    omega = o.Num("omega"),
                    j2 = o.Num("j2"),
                });
            }
            return list;
        }

        public static List<CelestialBody> LoadFromRepo(string root)
            => Parse(File.ReadAllText(Path.Combine(root, "data", "bodies.json")));

        public static CelestialBody Require(IReadOnlyList<CelestialBody> all, string id)
        {
            for (int i = 0; i < all.Count; i++)
                if (all[i].id == id) return all[i];
            throw new InvalidOperationException("no body " + id);
        }
    }
}

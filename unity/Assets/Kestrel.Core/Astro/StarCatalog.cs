// R2/S1 star catalogue loader. Format (CSV, one star per line, '#' comments):
//   id,raDeg,decDeg,vmag[,bv]
// Positions are J2000 mean. The shipped catalogue file is added only after its licence is verified and
// logged in LICENSES.md (DECISIONS #42); this loader and its tests use a small hand-written fixture.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Kestrel.Core.Astro
{
    public struct Star { public int Id; public double RaDeg, DecDeg, VMag, BV; }

    public static class StarCatalog
    {
        /// <summary>Parse the CSV, keep stars at or brighter than maxMag, sorted brightest first then id (deterministic).</summary>
        public static List<Star> Parse(string csv, double maxMag = double.PositiveInfinity, int maxCount = int.MaxValue)
        {
            var list = new List<Star>();
            var inv = CultureInfo.InvariantCulture;
            int lineNo = 0;
            foreach (var raw in csv.Split('\n'))
            {
                lineNo++;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                var f = line.Split(',');
                if (f.Length < 4) throw new FormatException("star catalogue line " + lineNo + ": expected id,ra,dec,vmag[,bv]");
                var s = new Star
                {
                    Id = int.Parse(f[0], inv), RaDeg = double.Parse(f[1], inv), DecDeg = double.Parse(f[2], inv),
                    VMag = double.Parse(f[3], inv), BV = f.Length > 4 && f[4].Trim().Length > 0 ? double.Parse(f[4], inv) : 0.6
                };
                if (s.RaDeg < 0 || s.RaDeg >= 360 || s.DecDeg < -90 || s.DecDeg > 90) throw new FormatException("star catalogue line " + lineNo + ": position out of range");
                if (s.VMag <= maxMag) list.Add(s);
            }
            list.Sort((a, b) => a.VMag != b.VMag ? a.VMag.CompareTo(b.VMag) : a.Id.CompareTo(b.Id));
            if (list.Count > maxCount) list.RemoveRange(maxCount, list.Count - maxCount);
            return list;
        }

        /// <summary>Tier star budgets (DECISIONS #39, Space #6): Low 2000, Medium 5000, High 9000.</summary>
        public static int BudgetForTier(string tier) => tier == "low" ? 2000 : tier == "medium" ? 5000 : 9000;
    }
}

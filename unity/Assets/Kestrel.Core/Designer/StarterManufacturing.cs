// Starter manufacturing grades: spend less and hope it holds, or spend more so it is less likely to blow up.
// Shop / Standard / Flight rewrite avionics, safety factor and tank material on top of the mission winner.
// Standard leaves the winner alone (EconomyBalanceTests / DECISIONS #87). Cost is CostModel; hold-together is Reliability.Of.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer
{
    /// <summary>How carefully the shop builds this rocket. Cheaper grades cut corners; Flight spends for strength and backups.</summary>
    public enum StarterBuild
    {
        /// <summary>Workshop scrap: basic computers, thin safety margin, cheaper alloy. Cheap, likelier to blow up.</summary>
        Shop = 0,
        /// <summary>The mission winner as flown (no rewrite). Default.</summary>
        Standard = 1,
        /// <summary>Flight hardware: redundant computers, thicker margins, titanium. Costs more Kc, holds together better.</summary>
        Flight = 2,
    }

    /// <summary>Apply a manufacturing grade and read the spend / hold-together numbers the Starter screen shows.</summary>
    public static class StarterManufacturing
    {
        public const double ShopSafety = 1.15;
        public const double FlightSafety = 1.6;
        public const string ShopMaterial = "al-cu";
        public const string FlightMaterial = "titanium";
        public const string ShopAvionics = "basic";
        public const string FlightAvionics = "redundant";

        /// <summary>Per-engine reliability scale inside FailureModel. Empty/standard leave catalog reliability alone.</summary>
        public static double EngineReliabilityScale(string manufacturingId)
        {
            if (manufacturingId == "shop") return 0.92;
            if (manufacturingId == "flight") return 1.0;
            return 1.0;
        }

        public static string Id(StarterBuild b) =>
            b == StarterBuild.Shop ? "shop" : b == StarterBuild.Flight ? "flight" : "standard";

        public static bool TryParse(string id, out StarterBuild b)
        {
            b = StarterBuild.Standard;
            if (id == "shop") { b = StarterBuild.Shop; return true; }
            if (id == "flight") { b = StarterBuild.Flight; return true; }
            if (string.IsNullOrEmpty(id) || id == "standard") { b = StarterBuild.Standard; return true; }
            return false;
        }

        public static string Label(StarterBuild b) =>
            b == StarterBuild.Shop ? StarterScreen.ShopBuild : b == StarterBuild.Flight ? StarterScreen.FlightBuild : StarterScreen.StandardBuild;

        /// <summary>Rewrite materials, avionics and safety for Shop / Flight. Standard is a no-op (winner stays byte-stable).</summary>
        public static void Apply(Catalog c, JsonObject d, StarterBuild build)
        {
            if (d == null) return;
            // Standard leaves the winner byte-identical (no manufacturing key). Shop / Flight stamp the grade.
            if (build == StarterBuild.Standard) return;
            JsonMut.Set(d, "manufacturing", Id(build));

            string material = build == StarterBuild.Shop ? ShopMaterial : FlightMaterial;
            string avionics = build == StarterBuild.Shop ? ShopAvionics : FlightAvionics;
            double sf = build == StarterBuild.Shop ? ShopSafety : FlightSafety;

            HangarEditor.SetAvionics(d, avionics);
            HangarEditor.SetSafetyFactor(d, sf);

            var stages = d.Arr("stages");
            if (stages != null)
            {
                for (int i = 0; i < stages.Count; i++)
                {
                    if (!(stages[i] is JsonObject)) continue;
                    // Hydrolox stays off non-cryo shop alloys; Flight titanium is cryo-OK. Glass/phenolic solids keep their case.
                    var s = (JsonObject)stages[i];
                    string prop = s.Str("propellant") ?? "";
                    string cur = s.Str("material") ?? "";
                    if (prop == "hydrolox" && build == StarterBuild.Shop) continue;
                    if (cur == "glass-phenolic") continue;
                    HangarEditor.SetMaterial(c, d, PartRef.Stage(i), material);
                }
            }
            var so = d.Obj("strapOns");
            if (so != null && so.Num("count") > 0)
            {
                string cur = so.Str("material") ?? "";
                if (cur != "glass-phenolic")
                    HangarEditor.SetMaterial(c, d, PartRef.Boosters, material);
            }
        }

        /// <summary>Recurring hardware quote in Kc for the built design (designer strip units).</summary>
        public static double CostKc(Catalog c, JsonObject design)
        {
            if (c == null || design == null) return 0;
            long per = CostModel.MinorPerKc(c);
            if (per <= 0) per = CostModel.DefaultMinorPerKc;
            return CostModel.PriceDesign(c, Design.FromJson(design)).recurring / (double)per;
        }

        /// <summary>Vehicle hold-together from Reliability.Of (0..1).</summary>
        public static double HoldTogether(Catalog c, JsonObject design)
        {
            if (c == null || design == null) return 0;
            return Reliability.Of(c, Design.FromJson(design)).reliability;
        }

        /// <summary>One kid-readable spend line: "4.8k Kc · Holds 94%" or "4.8k / 40k Kc · Holds 94%".</summary>
        public static string SpendLine(Catalog c, JsonObject design, double budgetKc)
        {
            double cost = CostKc(c, design);
            double hold = HoldTogether(c, design) * 100.0;
            string money = budgetKc > 0
                ? CompactKc(cost) + " / " + CompactKc(budgetKc) + " Kc"
                : CompactKc(cost) + " Kc";
            return money + " · Holds " + hold.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%";
        }

        public static string CompactKc(double kc)
        {
            double a = Math.Abs(kc);
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            if (a >= 1e6) return (kc / 1e6).ToString("0.#", ci) + "M";
            if (a >= 1e3) return (kc / 1e3).ToString("0.#", ci) + "k";
            return kc.ToString("0", ci);
        }
    }
}

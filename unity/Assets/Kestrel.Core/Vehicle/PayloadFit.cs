// Fairing fit (§2.9). The one fit rule (B-04): Validate, Readout, the stepper, LaunchGate,
// FlightBatch and the debrief all use it.
//   usable = π (0.45 · D_f)² · k(nose) · L_f
//   fits when volume ≤ usable and cargo diameter ≤ 0.9 · D_f
// Both use the same envelope: a usable inner radius of 0.45 · D_f (diameter 0.9 · D_f).
// 0.9 is an approximate game number; real fairings offer about 0.85–0.885 of their outer diameter.
// L_f is the whole fairing length, nose included (the nose takes 0.25–0.45 of it, docs/PHYSICS.md).
// k depends on the nose shape (#302): the fill fraction over the whole length, from the nose profile
// integral — ogive 0.85, conic 0.733, blunt 0.917, von Kármán 0.762 (see LengthFactorFor). The old flat
// 0.8 overstated the volume for conic/von Kármán noses and understated it for ogive/blunt.
// A negative margin is how far the payload overruns that limit.
// The cargo comes from SpecFor: the mission's cargo (campaign.json volumeM3/diameterM) when the
// design carries a missionId, otherwise the catalog payload (payloads.json volume and diameter).
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;

namespace Kestrel.Core.Vehicle
{
    public sealed class PayloadSpec
    {
        public string id;
        public double volume;
        public double diameter;

        public static PayloadSpec FromJson(JsonObject o)
        {
            if (o == null) return null;
            return new PayloadSpec { id = o.Str("id") ?? o.Str("type"), volume = o.Num("volume"), diameter = o.Num("diameter") };
        }
    }

    /// <summary>fit plus the volume (m³) and diameter (m, 0.9·D_f − cargo diameter) margins. Positive means spare room.</summary>
    public struct PayloadFitResult
    {
        public bool fits;
        public double usableVolume;
        public double volumeMargin;
        public double diameterMargin;
    }

    public static class PayloadFit
    {
        public const double RadiusFactor = 0.45;
        /// <summary>The old flat fill fraction (0.8), kept for reference. Superseded by <see cref="LengthFactorFor"/>:
        /// the fill fraction depends on the nose shape (#302), and 0.8 is only correct for no nose in particular.</summary>
        public const double LengthFactor = 0.8;
        /// <summary>Usable inner diameter as a fraction of the fairing diameter (2 × <see cref="RadiusFactor"/>).</summary>
        public const double DiameterFactor = 2 * RadiusFactor;
        /// <summary>
        /// The diameter check allows 1e-9 m (a nanometre) of float noise, so cargo exactly at 0.9 · D_f
        /// (2.70 m in 3 m, 3.6 m in 4 m) fits on every platform instead of hanging on the last ulp.
        /// </summary>
        public const double DiameterToleranceM = 1e-9;

        /// <summary>
        /// #302: the nose's own volume as a fraction of a cylinder of the same length and base radius — the
        /// profile integral ∫(r(x)/R)² dx over the nose, from the game's drawn nose profiles. A cone is exactly
        /// 1/3; a tangent ogive and a half-ellipsoid (blunt) are both 2/3; a von Kármán ogive is ~0.47.
        /// </summary>
        public static double ProfileFraction(string nose)
        {
            if (nose == "conic") return 1.0 / 3.0;
            if (nose == "von-karman") return 0.47;
            // ogive, blunt (elliptical), and anything unrecognised (the default nose is ogive).
            return 2.0 / 3.0;
        }

        /// <summary>
        /// #302: the fill fraction k over the whole fairing length, nose included, for a nose shape. The fairing is
        /// a cylinder for (1 − noseFrac) of its length (k = 1 there) and the nose profile for noseFrac of it
        /// (k = <see cref="ProfileFraction"/> there), so k = 1 − noseFrac × (1 − profile). This is the one place
        /// the per-shape k is computed; <see cref="Designer.Balance.NoseFraction"/> is the nose-length split it
        /// uses, and the JS mirror is fitLengthFor in web/js/designer/validate.js. Values: ogive 0.85,
        /// conic 0.733, blunt 0.917, von Kármán 0.762. The old flat 0.8 was ~6% conservative for the ogive every
        /// golden uses, so ogive/blunt designs gain usable volume and still fit; conic/von Kármán lose a little.
        /// </summary>
        public static double LengthFactorFor(string nose)
        {
            double noseFrac = Designer.Balance.NoseFraction(nose);
            return 1 - noseFrac * (1 - ProfileFraction(nose));
        }

        public static double UsableVolume(double fairingDiameterM, double fairingLengthM, string nose = "ogive")
        {
            double radius = RadiusFactor * fairingDiameterM;
            return Math.PI * radius * radius * LengthFactorFor(nose) * fairingLengthM;
        }

        public static PayloadFitResult Check(Design design, PayloadSpec payload)
        {
            if (design == null) throw new ArgumentNullException(nameof(design));
            return Check(design.fairingDiameter, design.fairingLength, design.nose, payload);
        }

        /// <summary>The ogive-nose form of <see cref="Check(double, double, string, PayloadSpec)"/> (the default nose).</summary>
        public static PayloadFitResult Check(double fairingDiameterM, double fairingLengthM, PayloadSpec payload)
            => Check(fairingDiameterM, fairingLengthM, "ogive", payload);

        public static PayloadFitResult Check(double fairingDiameterM, double fairingLengthM, string nose, PayloadSpec payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            double usable = UsableVolume(fairingDiameterM, fairingLengthM, nose);
            double volumeMargin = usable - payload.volume;
            double diameterMargin = DiameterFactor * fairingDiameterM - payload.diameter;
            return new PayloadFitResult
            {
                fits = volumeMargin >= 0 && diameterMargin >= -DiameterToleranceM,
                usableVolume = usable,
                volumeMargin = volumeMargin,
                diameterMargin = diameterMargin,
            };
        }

        /// <summary>Shortest fairing of this diameter whose usable volume holds the cargo.</summary>
        public static double RequiredLength(PayloadSpec payload, double fairingDiameterM, string nose = "ogive")
        {
            if (payload == null || !(fairingDiameterM > 0)) return 0;
            double radius = RadiusFactor * fairingDiameterM;
            return payload.volume / (Math.PI * radius * radius * LengthFactorFor(nose));
        }

        /// <summary>Narrowest fairing that clears the cargo diameter.</summary>
        public static double RequiredDiameter(PayloadSpec payload) => payload == null ? 0 : payload.diameter / DiameterFactor;

        public static PayloadSpec MissionSpec(CampaignMission mission)
            => mission == null ? null : new PayloadSpec { id = mission.PayloadId, volume = mission.VolumeM3, diameter = mission.DiameterM };

        /// <summary>
        /// The cargo a design is fitted against. A design with a missionId registered on the catalog
        /// (HomeAutoBuild.InstallMissionOrbits) uses the mission cargo. Otherwise the catalog payload:
        /// volume × min(1.5, (m / m_catalog)^(2/3)) and the catalog diameter. Null when there is no known payload.
        /// </summary>
        public static PayloadSpec SpecFor(Catalog catalog, string missionId, string payloadType, double payloadMass)
        {
            if (catalog == null) return null;
            if (!string.IsNullOrEmpty(missionId) && catalog.MissionPayloads.TryGetValue(missionId, out var m) && m != null) return m;
            if (payloadType == null || !catalog.PayloadById.TryGetValue(payloadType, out var pl)) return null;
            double mass = payloadMass > 0 ? payloadMass : pl.mass;
            double volume = pl.mass > 0 ? pl.volume * Math.Min(1.5, Math.Pow(mass / pl.mass, 2.0 / 3.0)) : pl.volume;
            return new PayloadSpec { id = pl.id, volume = volume, diameter = pl.diameter };
        }

        public static PayloadSpec SpecFor(Catalog catalog, JsonObject design)
        {
            var p = design?.Obj("payload");
            return SpecFor(catalog, design?.Str("missionId"), p?.Str("type"), p != null ? p.Num("mass") : 0);
        }

        public static PayloadSpec SpecFor(Catalog catalog, Design design)
            => design == null ? null : SpecFor(catalog, design.missionId, design.payloadType, design.payloadMass);

        /// <summary>Fit of a design JSON against <see cref="SpecFor(Catalog, JsonObject)"/>. Null when there is no cargo or fairing.</summary>
        public static PayloadFitResult? Check(Catalog catalog, JsonObject design)
        {
            var spec = SpecFor(catalog, design);
            var f = design?.Obj("fairing");
            if (spec == null || f == null) return null;
            return Check(f.Num("diameter"), f.Num("length"), design?.Obj("aero")?.Str("nose", "ogive"), spec);
        }
    }
}

// Fairing fit (§2.9). The one fit rule (B-04): Validate, Readout, the stepper, LaunchGate,
// FlightBatch and the debrief all use it.
//   usable = π (0.45 · D_f)² · 0.8 · L_f
//   fits when volume ≤ usable and cargo diameter ≤ 0.9 · D_f
// Both use the same envelope: a usable inner radius of 0.45 · D_f (diameter 0.9 · D_f).
// 0.9 is an approximate game number; real fairings offer about 0.85–0.885 of their outer diameter.
// L_f is the whole fairing length, nose included (the nose takes 0.25–0.45 of it, docs/PHYSICS.md).
// 0.8 is optimistic: because L_f includes the nose, it overstates the real volume for conic and ogive
// noses (geometric fractions are about 0.73 conic, 0.78 otherwise, 0.90 blunt). It stays 0.8 until the
// follow-up issue #302 "Payload volume packing retune: nose-inclusive L" (https://github.com/becktj90/kestrel-heavy/issues/302) (after Phase 1, before Phase 2 Builder).
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
        public const double LengthFactor = 0.8;
        /// <summary>Usable inner diameter as a fraction of the fairing diameter (2 × <see cref="RadiusFactor"/>).</summary>
        public const double DiameterFactor = 2 * RadiusFactor;

        public static double UsableVolume(double fairingDiameterM, double fairingLengthM)
        {
            double radius = RadiusFactor * fairingDiameterM;
            return Math.PI * radius * radius * LengthFactor * fairingLengthM;
        }

        public static PayloadFitResult Check(Design design, PayloadSpec payload)
        {
            if (design == null) throw new ArgumentNullException(nameof(design));
            return Check(design.fairingDiameter, design.fairingLength, payload);
        }

        public static PayloadFitResult Check(double fairingDiameterM, double fairingLengthM, PayloadSpec payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            double usable = UsableVolume(fairingDiameterM, fairingLengthM);
            double volumeMargin = usable - payload.volume;
            double diameterMargin = DiameterFactor * fairingDiameterM - payload.diameter;
            return new PayloadFitResult
            {
                fits = volumeMargin >= 0 && diameterMargin >= 0,
                usableVolume = usable,
                volumeMargin = volumeMargin,
                diameterMargin = diameterMargin,
            };
        }

        /// <summary>Shortest fairing of this diameter whose usable volume holds the cargo.</summary>
        public static double RequiredLength(PayloadSpec payload, double fairingDiameterM)
        {
            if (payload == null || !(fairingDiameterM > 0)) return 0;
            double radius = RadiusFactor * fairingDiameterM;
            return payload.volume / (Math.PI * radius * radius * LengthFactor);
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
            return Check(f.Num("diameter"), f.Num("length"), spec);
        }
    }
}

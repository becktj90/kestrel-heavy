// Fairing fit (§2.9). Payload volume and diameter are catalog fields. The check is pure:
//   usable = π (0.45 · D_f)² · 0.8 · L_f
//   fits when volume ≤ usable and diameter + 0.2 m ≤ D_f
// A negative margin is how far the payload overruns that limit.
using System;
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

    /// <summary>fit plus the volume (m³) and diameter (m) margins. Positive means spare room.</summary>
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
        public const double DiameterClearanceM = 0.2;

        public static double UsableVolume(double fairingDiameterM, double fairingLengthM)
        {
            double radius = RadiusFactor * fairingDiameterM;
            return Math.PI * radius * radius * LengthFactor * fairingLengthM;
        }

        public static PayloadFitResult Check(Design design, PayloadSpec payload)
        {
            if (design == null) throw new ArgumentNullException(nameof(design));
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            double usable = UsableVolume(design.fairingDiameter, design.fairingLength);
            double volumeMargin = usable - payload.volume;
            double diameterMargin = design.fairingDiameter - payload.diameter - DiameterClearanceM;
            return new PayloadFitResult
            {
                fits = volumeMargin >= 0 && diameterMargin >= 0,
                usableVolume = usable,
                volumeMargin = volumeMargin,
                diameterMargin = diameterMargin,
            };
        }
    }
}

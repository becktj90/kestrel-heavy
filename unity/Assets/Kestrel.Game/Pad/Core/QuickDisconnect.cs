// Quick-disconnect plates: a skin plate that stays on the vehicle and a pad plate that pulls off along the
// outward normal, then rides away with the arm. Generic discs — no facility markings.
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public static class QuickDisconnect
    {
        /// <summary>Plate thickness, metres. Mated centres sit this far apart so the two discs touch.</summary>
        public const float Thickness = 0.05f;
        /// <summary>Seconds from Disconnect until the pad plate is clear of the skin.</summary>
        public const float SeparateSeconds = 0.35f;

        /// <summary>Distance from the skin plate centre to the pad plate centre along the outward normal.</summary>
        public static float Separation(float plateRadius, UmbilicalState state, double sinceDisconnect)
        {
            float seat = Thickness;
            if (state != UmbilicalState.Released && state != UmbilicalState.Retracted) return seat;
            float u = state == UmbilicalState.Retracted ? 1f : SwingArmSolver.RetractProgress(sinceDisconnect, SeparateSeconds);
            return seat + u * (0.18f + (plateRadius > 0.05f ? plateRadius : 0.05f));
        }

        public static Vec3 PadPlate(Vec3 skinPlate, Vec3 outward, float separation) =>
            ContractMath.Add(skinPlate, ContractMath.Scale(outward, separation));
    }
}

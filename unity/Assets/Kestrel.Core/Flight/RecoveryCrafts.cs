// The first stage, after it has let go of the upper stack. Thrust is that stage's own
// engines. The landing reserve is propellant, and the dry mass does not count it twice.
using Kestrel.Core.Data;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public static class RecoveryCrafts
    {
        public static bool TryFrom(Catalog catalog, Design design, out RecoveryCraft craft)
        {
            craft = default;
            if (design == null || !RecoverySim.Offered(design.recoveryMode)) return false;
            return Build(catalog, design, design.recoveryMode, out craft);
        }

        /// <summary>
        /// The same first stage when the design picked no landing (expendable). It heads for Haven
        /// (no boostback needed) with only what it really kept: its landing reserve, legs and fins.
        /// An empty reserve falls to the sea. The flight shows that attempt and says why.
        /// </summary>
        public static bool TryAnyway(Catalog catalog, Design design, out RecoveryCraft craft)
        {
            craft = default;
            if (design == null) return false;
            string mode = RecoverySim.Offered(design.recoveryMode) ? design.recoveryMode : "barge";
            return Build(catalog, design, mode, out craft);
        }

        static bool Build(Catalog catalog, Design design, string mode, out RecoveryCraft craft)
        {
            craft = default;
            if (catalog == null || design == null || design.stages == null || design.stages.Count < 2)
                return false;
            var raw = Stack.ToSimStages(catalog, design);
            StackEntry core = null;
            for (int i = 0; i < raw.Count; i++)
            {
                if (raw[i] != null && raw[i].role == "core" && raw[i].index == 0)
                {
                    core = raw[i];
                    break;
                }
            }
            if (core == null) return false;
            var serial = Stack.SerialStages(catalog, design).stages;
            SerialStage landed = null;
            if (serial != null)
            {
                for (int i = 0; i < serial.Count; i++)
                {
                    var s = serial[i];
                    if (s != null && s.index == 0 && s.role == "core") landed = s;
                }
            }
            double reserve = core.reserveMass;
            if (double.IsNaN(reserve) || reserve < 0) reserve = 0;
            double dry = landed != null ? landed.dryMass - reserve : core.dryMass;
            if (!(dry > 1)) dry = core.dryMass > 1 ? core.dryMass : 1;
            double dia = design.stages[0].diameter;
            if (!(dia > 0.2)) dia = core.diameter;
            craft = new RecoveryCraft
            {
                DryKg = dry,
                ReserveKg = reserve,
                ThrustVacN = landed != null ? landed.thrustVac : core.thrustVac,
                ThrustSlN = landed != null ? landed.thrustSL : core.thrustSL,
                IspSec = landed != null && landed.ispVac > 1 ? landed.ispVac : core.ispVac,
                DiameterM = dia,
                Cd = landed != null && landed.cd > 0.02 ? landed.cd : (core.Cd > 0.02 ? core.Cd : 0.45),
                GridFins = design.gridFins,
                Legs = design.legs,
                LegsKind = string.IsNullOrEmpty(design.legsKind) ? "light" : design.legsKind,
                Mode = mode,
            };
            return craft.ThrustVacN > 1 || craft.ThrustSlN > 1;
        }
    }
}

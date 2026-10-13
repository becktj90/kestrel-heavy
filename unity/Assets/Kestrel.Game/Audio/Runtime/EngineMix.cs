namespace Kestrel.Game.Audio
{
    /// <summary>
    /// Engine-loop gain. Structure-borne rumble follows throttle and stays in vacuum.
    /// External air follows dynamic pressure and fades to silence by 80 km.
    /// Volumes match the web bed: 0.14 + min(0.32, throttle * 0.28) above a 0.02 throttle floor.
    /// </summary>
    public static class EngineMix
    {
        public const float VacuumAltM = 80000f;
        public const float AirFadeStartM = 30000f;
        /// <summary>About a medium-kerolox max-Q, so qn ≈ 1 through the peak.</summary>
        public const float QRefPa = 35000f;
        /// <summary>Arm the one-shot max-Q call once q has actually climbed.</summary>
        public const float MaxQArmPa = 15000f;
        public const float MaxQDrop = 0.98f;

        public static float ExternalGain(float altM)
        {
            if (altM >= VacuumAltM) return 0f;
            if (altM <= AirFadeStartM) return 1f;
            return (VacuumAltM - altM) / (VacuumAltM - AirFadeStartM);
        }

        public static float Structure(float throttle)
        {
            float t = Clamp01(throttle);
            if (t <= 0.02f) return 0f;
            float scaled = t * 0.28f;
            if (scaled > 0.32f) scaled = 0.32f;
            return 0.14f + scaled;
        }

        /// <summary>Airborne roar layered on the structure bed. Zero at and above 80 km.</summary>
        public static float AirBed(float altM, float qPa)
        {
            float qn = qPa <= 0f ? 0f : qPa / QRefPa;
            if (qn > 1f) qn = 1f;
            return 0.36f * ExternalGain(altM) * qn;
        }

        public static float Volume(float altM, float qPa, float throttle)
        {
            float v = Structure(throttle) + AirBed(altM, qPa);
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        public static float Pitch(float altM, float qPa, float throttle)
        {
            float t = Clamp01(throttle);
            float qn = qPa <= 0f ? 0f : qPa / QRefPa;
            if (qn > 1f) qn = 1f;
            return 0.85f + 0.25f * t + 0.10f * qn * ExternalGain(altM);
        }

        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}

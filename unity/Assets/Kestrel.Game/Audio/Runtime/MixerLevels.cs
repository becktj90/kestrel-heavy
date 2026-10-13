namespace Kestrel.Game.Audio
{
    /// <summary>S1 bus levels (web mixer.js). effective = master * bus * duck, or 0 when muted.</summary>
    public static class MixerLevels
    {
        public const float Master = 0.72f;
        public const float Music = 0.85f;
        public const float Sfx = 1f;
        public const float Engine = 1f;
        public const float Voice = 1f;
        /// <summary>sfx and engine while a narration line is playing (DUCK.sfxDuck).</summary>
        public const float VoiceDuck = 0.8f;

        public static float Bus(bool muted, float bus, float duck)
        {
            if (muted) return 0f;
            if (duck < 0f) duck = 0f;
            if (duck > 1f) duck = 1f;
            return Master * bus * duck;
        }
    }
}

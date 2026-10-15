namespace Kestrel.Game.Audio
{
    public enum MusicBed { Menu, Hangar, Flight }

    /// <summary>Menu and hangar are the original score. Flight is the approved Gemini bed. Mix stays under SFX and narration.</summary>
    public static class MusicBeds
    {
        public const float FadeSeconds = 0.75f;
        /// <summary>Narration duck from the music bible (music × 0.35 while a voice line plays).</summary>
        public const float NarrationDuck = 0.35f;
        /// <summary>Pause bed, about −6 dB. The score keeps playing; engines and one-shots do not.</summary>
        public const float PauseDuck = 0.5f;

        public static string Resource(MusicBed bed)
        {
            switch (bed)
            {
                case MusicBed.Menu: return "Music/kestrel-title";
                case MusicBed.Hangar: return "Music/kestrel-designer";
                default: return "Music/kestrel-flight";
            }
        }

        /// <summary>Linear gain. Zero when music is muted or the bed is suspended.</summary>
        public static float Gain(bool musicMuted, bool suspended, bool voice, bool performancePaused)
        {
            if (musicMuted || suspended) return 0f;
            float duck = 1f;
            if (voice) duck *= NarrationDuck;
            if (performancePaused) duck *= PauseDuck;
            return MixerLevels.Bus(false, MixerLevels.Music, duck);
        }
    }
}

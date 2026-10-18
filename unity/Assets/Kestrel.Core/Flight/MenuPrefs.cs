// In-memory copy of the menu toggles the shell persists. Guide and the mission board
// read this type so they do not take a dependency on PlayerPrefs. Defaults match a fresh save:
// subtitles on, sandbox available. AudioPrefs.Load is what copies the disk values in.

namespace Kestrel.Core.Flight
{
    public static class MenuPrefs
    {
        public static bool SubtitlesOn = true;
        public static bool SandboxUnlocked = true;
    }
}

// Loads the G7 script, keeps kh.save.v1.guide, and points the corner portrait at that engine.
// Text is the line. A clip plays only when the guide manifest has that card.
using System;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Guide;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Guide
{
    public static class GuideHost
    {
        public const string DesignerDepthPref = "kh.designer.v3.engineer";
        public const string HangarDepthPref = "kh.hangar.engineer";

        public static bool EngineerDepth()
            => PlayerPrefs.GetInt(DesignerDepthPref, 0) != 0 || PlayerPrefs.GetInt(HangarDepthPref, 0) != 0;

        public static void Open(UIDocument doc, GuidePlace place, bool compact)
        {
            try
            {
                GuideClipPlayer.SetInFlight(place == GuidePlace.Ascent);
                var engine = EnsureEngine();
                if (engine == null || doc == null) return;
                ApplyDepth(engine);
                var driver = doc.GetComponent<GuideBoxDriver>();
                if (driver == null) driver = doc.gameObject.AddComponent<GuideBoxDriver>();
                driver.Document = doc;
                driver.Place = place;
                driver.Compact = compact;
                driver.HeaderPoints = compact ? GuidePacing.AscentHeaderPt : 0f;
                driver.Use(engine);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Thrum] " + e.Message);
            }
        }

        public static LiveGuideEngine EnsureEngine()
        {
            if (GuideSession.Engine != null) return GuideSession.Engine;
            var engine = TryLoad();
            if (engine != null) GuideSession.Bind(engine);
            return engine;
        }

        public static LiveGuideEngine TryLoad()
        {
            Kestrel.Game.Audio.AudioPrefs.Load();
            string json = ReadScript();
            if (string.IsNullOrEmpty(json)) return null;
            string root = RepoRoot() ?? "";
            int found = GuideClipPlayer.Announce(root);
            bool clips = found > 0;
            bool designs = HasDesigns();
            var state = GuideState.Load(SaveDir(), clips, designs);
            state.Settings.VoiceAvailable = clips;
            // Clips ship with voice on. Settings > Voice is the narration mute, not this flag.
            // A save written when the clips were missing had voice off; that is not a choice.
            state.Settings.VoiceOn = clips;
            return new LiveGuideEngine(json, state, root, clips);
        }

        public static void ApplyDepth(LiveGuideEngine engine)
        {
            if (engine == null) return;
            engine.SetEngineer(EngineerDepth());
        }

        public static void Save(LiveGuideEngine engine)
        {
            if (engine == null) return;
            try
            {
                engine.PushSettings();
                string dir = SaveDir();
                if (string.IsNullOrEmpty(dir)) return;
                engine.State.Save(dir);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Thrum] save skipped: " + e.Message);
            }
        }

        static string ReadScript()
        {
            string root = RepoRoot();
            if (!string.IsNullOrEmpty(root))
            {
                string path = Path.Combine(root, "data", "guide.json");
                if (File.Exists(path)) return File.ReadAllText(path);
            }
            var asset = Resources.Load<TextAsset>("KestrelData/guide");
            return asset != null ? asset.text : null;
        }

        static string RepoRoot()
        {
            try { return Catalog.FindRepoRoot(Directory.GetCurrentDirectory()); }
            catch (Exception) { return null; }
        }

        static string SaveDir()
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "saves");
                Directory.CreateDirectory(dir);
                return dir;
            }
            catch (Exception) { return null; }
        }

        static bool HasDesigns()
        {
            try
            {
                string dir = SaveDir();
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;
                var files = Directory.GetFiles(dir);
                for (int i = 0; i < files.Length; i++)
                    if (Path.GetFileName(files[i]).StartsWith("kh.save.v1.design", StringComparison.Ordinal)) return true;
            }
            catch (Exception) { }
            return false;
        }
    }
}

// App scene flow: Boot (persistent shell) → Home → Missions → Design, with Flight still a stub.
// Briefing and Report are overlays on the shell, not extra scenes. Boot never unloads.
using System;
using System.Collections;
using Kestrel.Core.Flight;
using Kestrel.Game.MissionSelect;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kestrel.Game.Shell
{
    public enum AppScreen { Home, Design, Flight, Missions }

    public sealed class SceneFlow : MonoBehaviour
    {
        public static readonly string[] SceneNames = { "Home", "Design", "Flight", "Missions" };
        public const string BootScene = "Boot";

        public AppScreen? Current { get; private set; }
        public bool Busy { get; private set; }
        public string Overlay { get; private set; } = SessionScreens.None;
        public event Action<AppScreen> Entered;
        public event Action Leaving;
        public event Action<string> OverlayChanged;

        public static bool TryHomeControl(string controlId, out RouteId route)
        {
            if (controlId == "launch") { route = RouteId.HomeLaunch; return true; }
            if (controlId == "hangar") { route = RouteId.HomeHangar; return true; }
            route = default;
            return false;
        }

        public static bool TryScreen(string name, out AppScreen screen)
        {
            for (int i = 0; i < SceneNames.Length; i++)
            {
                if (SceneNames[i] == name) { screen = (AppScreen)i; return true; }
            }
            screen = default;
            return false;
        }

        /// <summary>LAUNCH opens the mission list. HANGAR opens the designer as a free build.</summary>
        public void HomePressed(string controlId)
        {
            if (Busy || !TryHomeControl(controlId, out var route)) return;
            MissionBriefing card = null;
            if (route == RouteId.HomeHangar) card = TryFreeBriefing();
            Present(GameSession.Apply(route, null, null, card));
        }

        /// <summary>The chosen KH-n is carried, then the briefing overlay opens on the map. Build it enters the designer.</summary>
        public void StartMission(string missionId)
        {
            if (Busy) return;
            var campaign = ScreenStub.LoadCampaign();
            MissionBriefing card = null;
            var mission = campaign != null ? campaign.ById(missionId) : null;
            string root = ScreenStub.RepoRoot();
            if (mission != null && root != null)
            {
                try { card = MissionBriefing.Load(root, mission.Index, 0, MissionContext.DefaultSeed(campaign)); }
                catch (Exception e) { Debug.LogWarning("[Kestrel] briefing unavailable: " + e.Message); }
            }
            Present(BriefingBoard.BeforeDesign(GameSession.Apply(RouteId.MissionChosen, campaign, missionId, card)));
        }

        public void BuildFromBriefing() => Route(RouteId.BriefingBuild);
        public void BackFromBriefing() => Route(RouteId.BriefingBack);
        public void BackFromDesigner() => Route(RouteId.DesignerBack);
        public void OpenReport() => Route(RouteId.OpenReport);
        public void ReportNext() => Route(RouteId.ReportNext, ScreenStub.LoadCampaign());
        public void ReportRetry() => Route(RouteId.ReportRetry);

        /// <summary>Same exit the in-flight pause menu reaches via Go(Home): title screen, session cleared.</summary>
        public void ExitToHome() => Route(RouteId.ExitHome);

        public void ExitToMissions() => Route(RouteId.ExitMissions);

        void Route(RouteId id, Campaign campaign = null)
        {
            if (Busy) return;
            Present(GameSession.Apply(id, campaign));
        }

        public void Present(RouteResult route)
        {
            // A route out of a paused ascent must not leave the music ducked.
            Kestrel.Game.Audio.MusicPlayer.Instance?.SetPaused(false);
            Overlay = string.IsNullOrEmpty(route.Overlay) ? SessionScreens.None : route.Overlay;
            if (string.IsNullOrEmpty(route.Screen) || !TryScreen(route.Screen, out var screen))
            {
                OverlayChanged?.Invoke(Overlay);
                return;
            }
            if (Current == screen && !Busy)
            {
                if (screen == AppScreen.Home) GameSession.Clear();
                OverlayChanged?.Invoke(Overlay);
                return;
            }
            Go(screen);
        }

        public void Go(AppScreen to)
        {
            if (Busy || Current == to) return;
            if (to == AppScreen.Home)
            {
                GameSession.Clear();
                Overlay = SessionScreens.None;
            }
            StartCoroutine(Switch(to));
        }

        static MissionBriefing TryFreeBriefing()
        {
            string root = ScreenStub.RepoRoot();
            if (root == null) return null;
            try { return MissionBriefing.Free(root, 0, 1); }
            catch (Exception e)
            {
                Debug.LogWarning("[Kestrel] free-build briefing unavailable: " + e.Message);
                return null;
            }
        }

        IEnumerator Switch(AppScreen to)
        {
            Busy = true;
            Leaving?.Invoke();
            if (Current.HasValue)
            {
                var op = SceneManager.UnloadSceneAsync(SceneNames[(int)Current.Value]);
                while (op != null && !op.isDone) yield return null;
            }
            var load = SceneManager.LoadSceneAsync(SceneNames[(int)to], LoadSceneMode.Additive);
            while (!load.isDone) yield return null;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneNames[(int)to]));
            Current = to;
            Busy = false;
            Entered?.Invoke(to);
        }
    }
}

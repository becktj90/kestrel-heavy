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
            if (controlId == "hangar") { route = RouteId.HomeHangar; return true; }
            route = default;
            return false;
        }

        public static bool IsHomeControl(string controlId) =>
            controlId == "launch" || controlId == "design" || controlId == "missions" || controlId == "hangar";

        public static bool TryScreen(string name, out AppScreen screen)
        {
            for (int i = 0; i < SceneNames.Length; i++)
            {
                if (SceneNames[i] == name) { screen = (AppScreen)i; return true; }
            }
            screen = default;
            return false;
        }

        /// <summary>LAUNCH briefs the next mission and arms the pad. Design first opens the designer with that load.</summary>
        public void HomePressed(string controlId)
        {
            if (Busy) return;
            if (controlId == "launch") { Present(HomeLaunchRoute()); return; }
            if (controlId == "design") { Present(HomeDesignRoute()); return; }
            if (controlId == "missions")
            {
                PadHandoff.Clear();
                Go(AppScreen.Missions);
                return;
            }
            if (!TryHomeControl(controlId, out var route)) return;
            MissionBriefing card = null;
            if (route == RouteId.HomeHangar) card = TryFreeBriefing();
            Present(GameSession.Apply(route, null, null, card));
        }

        /// <summary>The chosen KH-n is carried, then the briefing overlay opens on the map. Build it enters the designer.</summary>
        public void StartMission(string missionId)
        {
            if (Busy) return;
            PadHandoff.Clear();
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
        public void BackFromBriefing()
        {
            if (Busy) return;
            if (PadHandoff.FromHome)
            {
                PadHandoff.Clear();
                Route(RouteId.ExitHome);
                return;
            }
            Route(RouteId.BriefingBack);
        }
        public void BackFromDesigner() => Route(RouteId.DesignerBack);
        public void OpenReport() => Route(RouteId.OpenReport);
        public void ReportNext() => Route(RouteId.ReportNext, ScreenStub.LoadCampaign());
        public void ReportRetry() => Route(RouteId.ReportRetry);

        /// <summary>Same exit the in-flight pause menu reaches via Go(Home): title screen, session cleared.</summary>
        public void ExitToHome() => Route(RouteId.ExitHome);

        public void ExitToMissions() => Route(RouteId.ExitMissions);

        /// <summary>Fly from the board. A starter that can launch opens the briefing with the pad armed. Otherwise the designer.</summary>
        public void LaunchChosen(string missionId)
        {
            if (Busy || string.IsNullOrEmpty(missionId)) return;
            var campaign = ScreenStub.LoadCampaign();
            var mission = campaign != null ? campaign.ById(missionId) : null;
            if (mission == null || mission.Index > ScreenStub.ClearedCount() + 1) return;
            var choice = HomeAutoBuild.Choose(ScreenStub.LoadCatalog(), ScreenStub.LoadHangar()?.templates, mission);
            if (choice != null && choice.LaunchEnabled && choice.Design != null)
            {
                var ctx = MissionContext.FromMission(campaign, mission);
                if (ctx != null)
                {
                    MissionBriefing card = null;
                    string root = ScreenStub.RepoRoot();
                    if (root != null)
                    {
                        try { card = MissionBriefing.Load(root, mission.Index, 0, MissionContext.DefaultSeed(campaign)); }
                        catch (Exception e) { Debug.LogWarning("[Kestrel] briefing unavailable: " + e.Message); }
                    }
                    ctx.ApplyBriefing(card);
                    ctx.VehicleLine = choice.VehicleLine;
                    ctx.ReachLine = choice.ReachLine;
                    ctx.AutoPad = true;
                    PadHandoff.Arm(choice.Design, true, fromHome: false);
                    GameSession.Carry(ctx);
                    Present(new RouteResult(SessionScreens.Missions, SessionScreens.Briefing, ctx));
                    return;
                }
            }
            DesignChosen(missionId);
        }

        /// <summary>Open the designer with this mission's starter. No starter falls through to the briefing.</summary>
        public void DesignChosen(string missionId)
        {
            if (Busy || string.IsNullOrEmpty(missionId)) return;
            var campaign = ScreenStub.LoadCampaign();
            var mission = campaign != null ? campaign.ById(missionId) : null;
            if (mission == null || mission.Index > ScreenStub.ClearedCount() + 1) return;
            var choice = HomeAutoBuild.Choose(ScreenStub.LoadCatalog(), ScreenStub.LoadHangar()?.templates, mission);
            if (choice == null || choice.Design == null)
            {
                StartMission(missionId);
                return;
            }
            var ctx = MissionContext.FromMission(campaign, mission);
            if (ctx == null)
            {
                StartMission(missionId);
                return;
            }
            ctx.VehicleLine = choice.VehicleLine;
            ctx.ReachLine = choice.ReachLine;
            ctx.AutoPad = false;
            PadHandoff.Arm(choice.Design, false, fromHome: false);
            GameSession.Carry(ctx);
            Present(new RouteResult(SessionScreens.Design, SessionScreens.None, ctx));
        }

        /// <summary>Free build. The existing hangar route, not a new flight.</summary>
        public void OpenSandbox()
        {
            if (!Busy) HomePressed("hangar");
        }

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
                if (screen == AppScreen.Home)
                {
                    GameSession.Clear();
                    PadHandoff.Clear();
                }
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
                PadHandoff.Clear();
                Overlay = SessionScreens.None;
            }
            StartCoroutine(Switch(to));
        }

        static RouteResult HomeLaunchRoute()
        {
            var campaign = ScreenStub.LoadCampaign();
            int cleared = ScreenStub.ClearedCount();
            return HomeRoutes.Launch(campaign, cleared, ScreenStub.LoadCatalog(), ScreenStub.LoadHangar()?.templates, TryMissionBriefing(campaign, cleared));
        }

        static RouteResult HomeDesignRoute()
        {
            var campaign = ScreenStub.LoadCampaign();
            return HomeRoutes.DesignFirst(campaign, ScreenStub.ClearedCount(), ScreenStub.LoadCatalog(), ScreenStub.LoadHangar()?.templates);
        }

        static MissionBriefing TryMissionBriefing(Campaign campaign, int cleared)
        {
            var mission = HomeRoutes.NextMission(campaign, cleared);
            string root = ScreenStub.RepoRoot();
            if (mission == null || root == null) return null;
            try { return MissionBriefing.Load(root, mission.Index, 0, MissionContext.DefaultSeed(campaign)); }
            catch (Exception e)
            {
                Debug.LogWarning("[Kestrel] briefing unavailable: " + e.Message);
                return null;
            }
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

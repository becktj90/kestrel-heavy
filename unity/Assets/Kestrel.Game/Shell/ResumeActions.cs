// Continue from Home: the draft first, then the newest saved rocket, then Design first.
using System;
using System.IO;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public static class ResumeActions
    {
        public static bool HasContinue()
        {
            bool draft = false;
            int designs = 0;
            string dir = CreditsPlay.SaveDir();
            if (Directory.Exists(dir))
            {
                try
                {
                    var library = new DesignLibrary(Open(dir));
                    draft = library.HasDraft();
                    designs = library.Count;
                }
                catch (Exception) { }
            }
            return ResumeOffer.Show(draft, designs, ScreenStub.ClearedCount());
        }

        public static long Credits()
        {
            if (CreditsPlay.Current != null) return CreditsPlay.Current.Balance < 0 ? 0 : CreditsPlay.Current.Balance;
            string dir = CreditsPlay.SaveDir();
            if (!Directory.Exists(dir)) return 0;
            try
            {
                var text = Resources.Load<TextAsset>("KestrelData/credits");
                if (text == null || string.IsNullOrEmpty(text.text)) return 0;
                var wallet = CreditsWallet.Load(Open(dir), CreditsTable.Parse(text.text));
                return wallet.Balance < 0 ? 0 : wallet.Balance;
            }
            catch (Exception) { return 0; }
        }

        /// <summary>The rocket CONTINUE opens and the mission it carries: the draft, else the newest save; the draft's
        /// mission, else the career's last mission. One choice for <see cref="Open"/> and <see cref="Caption"/>.</summary>
        static JsonObject Pick(out string missionId, out string name, out bool fromDraft)
        {
            JsonObject design = null;
            missionId = null;
            name = null;
            fromDraft = false;
            string dir = CreditsPlay.SaveDir();
            if (!Directory.Exists(dir)) return null;
            try
            {
                var store = Open(dir);
                var library = new DesignLibrary(store);
                design = library.LoadDraft(out missionId, out name);
                fromDraft = design != null;
                if (design == null)
                {
                    var list = library.List();
                    if (list.Count > 0)
                    {
                        design = library.Load(list[0].id);
                        name = list[0].name;
                    }
                }
                if (string.IsNullOrEmpty(missionId))
                {
                    var progress = store.GetProgress();
                    missionId = progress != null ? progress.Str("lastMissionId") : null;
                }
                if (design != null && string.IsNullOrEmpty(name)) name = design.Str("name");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Kestrel] continue: " + e.Message);
                design = null;
            }
            return design;
        }

        /// <summary>CONTINUE's sub-line: what <see cref="Open"/> will open (ResumeOffer.Caption). Null with nothing saved.</summary>
        public static string Caption()
        {
            try
            {
                var design = Pick(out var missionId, out var name, out var fromDraft);
                if (design == null) return null;
                var campaign = ScreenStub.LoadCampaign();
                var mission = campaign != null && !string.IsNullOrEmpty(missionId) ? campaign.ById(missionId) : null;
                string line = mission != null && !string.IsNullOrEmpty(mission.Name) ? MissionContext.LabelFor(mission.Index) + " \u00B7 " + mission.Name : null;
                return ResumeOffer.Caption(fromDraft, line, name);
            }
            catch (Exception) { return null; }
        }

        /// <summary>Arm the pad with the rocket to resume and open the designer. A fresh career uses Design first.</summary>
        public static RouteResult Open()
        {
            var design = Pick(out var missionId, out _, out _);
            if (design == null)
                return HomeRoutes.DesignFirst(ScreenStub.LoadCampaign(), ScreenStub.ClearedCount(), ScreenStub.LoadCatalog(), ScreenStub.LoadHangar()?.templates);

            PadHandoff.Arm(design, false, true);
            var campaign = ScreenStub.LoadCampaign();
            int cleared = ScreenStub.ClearedCount();
            var mission = campaign != null && !string.IsNullOrEmpty(missionId) ? campaign.ById(missionId) : null;
            // After a career restart, a draft may still name a later KH. Soft-linear only opens Cleared+1.
            if (mission != null && mission.Index > cleared + 1)
                mission = HomeRoutes.NextMission(campaign, cleared);
            if (mission != null)
            {
                var ctx = MissionContext.FromMission(campaign, mission);
                if (ctx != null)
                {
                    GameSession.Carry(ctx);
                    return new RouteResult(SessionScreens.Design, SessionScreens.None, ctx);
                }
            }
            GameSession.Clear();
            return new RouteResult(SessionScreens.Design, SessionScreens.None, null);
        }

        static NativeStore Open(string dir)
            => new NativeStore(dir, () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), () => 0.5);
    }
}

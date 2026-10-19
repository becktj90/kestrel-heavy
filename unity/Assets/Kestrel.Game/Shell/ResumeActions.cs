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

        /// <summary>Arm the pad with the rocket to resume and open the designer. A fresh career uses Design first.</summary>
        public static RouteResult Open()
        {
            JsonObject design = null;
            string missionId = null;
            string dir = CreditsPlay.SaveDir();
            if (Directory.Exists(dir))
            {
                try
                {
                    var store = Open(dir);
                    var library = new DesignLibrary(store);
                    design = library.LoadDraft(out missionId, out _);
                    if (design == null)
                    {
                        var list = library.List();
                        if (list.Count > 0) design = library.Load(list[0].id);
                    }
                    if (string.IsNullOrEmpty(missionId))
                    {
                        var progress = store.GetProgress();
                        missionId = progress != null ? progress.Str("lastMissionId") : null;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Kestrel] continue: " + e.Message);
                    design = null;
                }
            }
            if (design == null)
                return HomeRoutes.DesignFirst(ScreenStub.LoadCampaign(), ScreenStub.ClearedCount(), ScreenStub.LoadCatalog(), ScreenStub.LoadHangar()?.templates);

            PadHandoff.Arm(design, false, true);
            var campaign = ScreenStub.LoadCampaign();
            var mission = campaign != null && !string.IsNullOrEmpty(missionId) ? campaign.ById(missionId) : null;
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

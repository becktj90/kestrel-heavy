// Player boot for credits, the settings file, and the WebGL disk flush.
// EditMode tests do not enter play mode, so the wallet hook stays unset there.
using System;
using System.IO;
using System.Runtime.InteropServices;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Save;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public static class CreditsPlay
    {
        public static CreditsTable Table { get; private set; }
        public static CreditsWallet Current { get; private set; }
        public static NativeStore Store { get; private set; }

        public static string SaveDir() => Path.Combine(Application.persistentDataPath, "saves");

        public static void Boot()
        {
            if (Store != null) return;
            var text = Resources.Load<TextAsset>("KestrelData/credits");
            if (text != null && !string.IsNullOrEmpty(text.text))
            {
                try { Table = CreditsTable.Parse(text.text); }
                catch (Exception e) { Debug.LogWarning("[Kestrel] credits table: " + e.Message); }
            }
            Store = new NativeStore(SaveDir(), () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), () => (double)UnityEngine.Random.value);
            if (Table != null) Current = CreditsWallet.Load(Store, Table);
            CareerStore.WalletSource = _ => Current;
        }

        public static void Flush()
        {
            if (flushing) return;
            flushing = true;
            try
            {
                try { PlayerPrefs.Save(); }
                catch (Exception) { }
                try
                {
                    if (Store != null)
                        SettingsMirror.Save(Store, PlayerPrefs.HasKey, key => PlayerPrefs.GetInt(key));
                }
                catch (Exception) { }
                SyncFs();
            }
            finally { flushing = false; }
        }

        static bool flushing;

        static void SyncFs()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { KhSyncFs(); }
            catch (Exception) { }
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void KhSyncFs();
#endif
    }

    /// <summary>Designer hook: autosave the draft and refuse an edit the wallet cannot pay.</summary>
    public static class SaveHooks
    {
        public static void Bind(HangarSession session, DesignLibrary library)
        {
            if (session == null) return;
            if (library != null)
                library.WatchDraft(session, () => GameSession.Current != null ? GameSession.Current.MissionId : null, () => GameSession.IsTestFlight);
            var wallet = CreditsPlay.Current;
            if (wallet == null || wallet.Table == null || session.Design == null) return;
            // Test Flight: no Kc spent (DECISIONS #100). The wallet is not settled for a test rocket, and an edit made
            // while a test is open is free (checked per edit: the same designer can switch between a test and a mission).
            if (!GameSession.IsTestFlight)
            {
                long quote = wallet.Table.Quote(session.Design);
                if (!wallet.HasRecord) wallet.GiftBaseline(quote);
                else wallet.CatchUp(quote);
                wallet.Save();
            }
            session.Afford = (_, after) => GameSession.IsTestFlight || wallet.TryApply(wallet.Table.Quote(after));
        }
    }

    static class SaveBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (!Application.isPlaying) return;
            CreditsPlay.Boot();
            if (CreditsPlay.Store != null)
            {
                try { SettingsMirror.Restore(CreditsPlay.Store, PlayerPrefs.HasKey, (key, value) => PlayerPrefs.SetInt(key, value)); }
                catch (Exception e) { Debug.LogWarning("[Kestrel] settings restore: " + e.Message); }
            }
            NativeStore.Wrote -= OnWrote;
            NativeStore.Wrote += OnWrote;
            var host = new GameObject("KhSaveFlush");
            host.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<SaveFlush>();
        }

        static void OnWrote() => CreditsPlay.Flush();
    }

    sealed class SaveFlush : MonoBehaviour
    {
        void OnApplicationPause(bool pause) { if (pause) CreditsPlay.Flush(); }
        void OnApplicationQuit() => CreditsPlay.Flush();
    }
}
